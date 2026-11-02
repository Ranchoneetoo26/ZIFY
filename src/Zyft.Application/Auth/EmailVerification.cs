using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Audit;
using Zyft.Domain.Users;

namespace Zyft.Application.Auth;

/// <summary>Emissão e consumo de tokens de uso único enviados por e-mail (só o hash é persistido).</summary>
public static class UserTokens
{
    public static readonly TimeSpan EmailVerificationLifetime = TimeSpan.FromHours(24);
    public static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gera um token novo para a finalidade e invalida os anteriores ainda não usados (só o último link vale).
    /// Devolve o valor aberto, que vai só no link do e-mail. O chamador salva a unidade de trabalho.
    /// </summary>
    public static async Task<string> IssueAsync(
        IRepository<UserToken> tokens, ITokenService tokenService, DateTime now, Guid userId, UserTokenPurpose purpose, TimeSpan lifetime, CancellationToken ct)
    {
        foreach (var old in await tokens.ListAsync(t => t.UserId == userId && t.Purpose == purpose && t.UsedAt == null, ct))
            old.Consume(now);
        var raw = tokenService.GenerateOpaqueToken();
        tokens.Add(UserToken.Issue(userId, purpose, tokenService.Hash(raw), now.Add(lifetime)));
        return raw;
    }

    /// <summary>Busca um token utilizável (existe, é da finalidade, não expirou, não foi usado); senão, 400 no campo <c>token</c>.</summary>
    public static async Task<UserToken> FindUsableAsync(
        IRepository<UserToken> tokens, ITokenService tokenService, DateTime now, string raw, UserTokenPurpose purpose, string invalidMessage, CancellationToken ct)
    {
        var hash = tokenService.Hash(raw.Trim());
        var token = await tokens.FindAsync(t => t.TokenHash == hash && t.Purpose == purpose, ct);
        if (token is null || !token.IsUsable(now))
            throw new ValidationException(new[] { new ValidationFailure("Token", invalidMessage) });
        return token;
    }
}

// ------------------------------------------------------------------ confirmar e-mail

/// <summary>Confirma o e-mail com o token do link. Não exige login: o link pode ser aberto em outro navegador.</summary>
public sealed record VerifyEmailCommand(string Token) : IRequest<Unit>;

public sealed class VerifyEmailValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailValidator() => RuleFor(x => x.Token).NotEmpty().WithMessage("Informe o código de verificação.").MaximumLength(200).WithMessage(VerifyEmailHandler.InvalidToken);
}

public sealed class VerifyEmailHandler(
    IRepository<UserToken> tokens,
    IRepository<User> users,
    IRepository<AuditEvent> audit,
    ITokenService tokenService,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<VerifyEmailCommand, Unit>
{
    public const string InvalidToken = "O link de verificação é inválido ou expirou. Peça um novo link.";

    public async Task<Unit> Handle(VerifyEmailCommand cmd, CancellationToken ct)
    {
        var now = clock.UtcNow;
        if (await IsAlreadyConfirmedByAsync(cmd.Token, ct)) return Unit.Value;
        var token = await UserTokens.FindUsableAsync(tokens, tokenService, now, cmd.Token, UserTokenPurpose.EmailVerification, InvalidToken, ct);
        var user = await users.FindAsync(u => u.Id == token.UserId, ct)
            ?? throw new ValidationException(new[] { new ValidationFailure("Token", InvalidToken) });

        token.Consume(now);
        if (!user.IsEmailVerified)
        {
            user.MarkEmailVerified(now);
            audit.Add(AuditEvent.Record(user.Id, AuditActions.EmailVerified, now));
        }

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }

    /// <summary>
    /// Reabrir o link que já confirmou o e-mail é idempotente: vale só para o token de verificação que foi de fato usado
    /// na confirmação (<c>UsedAt</c> igual a <c>EmailVerifiedAt</c> da conta). Token invalidado por um link mais novo,
    /// expirado sem uso ou desconhecido continua 400.
    /// </summary>
    private async Task<bool> IsAlreadyConfirmedByAsync(string raw, CancellationToken ct)
    {
        var hash = tokenService.Hash(raw.Trim());
        var token = await tokens.FindAsync(t => t.TokenHash == hash && t.Purpose == UserTokenPurpose.EmailVerification, ct);
        if (token?.UsedAt is null) return false;
        var user = await users.FindAsync(u => u.Id == token.UserId, ct);
        return user?.EmailVerifiedAt is not null && user.EmailVerifiedAt == token.UsedAt;
    }
}

// ------------------------------------------------------------------ reenviar link

/// <summary>Gera e envia um novo link de confirmação para a pessoa logada (o anterior deixa de valer).</summary>
public sealed record ResendEmailVerificationCommand : IRequest<Unit>;

public sealed class ResendEmailVerificationHandler(
    IRepository<UserToken> tokens,
    IRepository<User> users,
    ITokenService tokenService,
    IEmailSender email,
    ICurrentUser current,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<ResendEmailVerificationCommand, Unit>
{
    public async Task<Unit> Handle(ResendEmailVerificationCommand cmd, CancellationToken ct)
    {
        var id = current.UserId;
        var user = await users.FindAsync(u => u.Id == id, ct) ?? throw new UnauthorizedException("Conta não encontrada.");
        if (user.IsEmailVerified) throw new ConflictException("Seu e-mail já está verificado.", "email.already_verified");

        var raw = await UserTokens.IssueAsync(tokens, tokenService, clock.UtcNow, user.Id, UserTokenPurpose.EmailVerification, UserTokens.EmailVerificationLifetime, ct);
        await uow.SaveChangesAsync(ct);
        await email.SendEmailVerificationAsync(user.Email, user.Name, raw, ct);
        return Unit.Value;
    }
}
