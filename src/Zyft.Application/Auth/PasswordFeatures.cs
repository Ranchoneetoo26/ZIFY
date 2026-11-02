using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Zyft.Application.Common;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Audit;
using Zyft.Domain.Extension;
using Zyft.Domain.Users;

namespace Zyft.Application.Auth;

/// <summary>Encerra as sessões da conta: revoga os refresh tokens ativos e desconecta as extensões pareadas.</summary>
public static class Sessions
{
    public static async Task RevokeAllAsync(IRepository<RefreshToken> tokens, IRepository<ExtensionDevice> devices, IRepository<AuditEvent> audit, Guid userId, DateTime now, CancellationToken ct)
    {
        foreach (var t in await tokens.ListAsync(t => t.UserId == userId && t.RevokedAt == null, ct))
            t.Revoke(now);
        foreach (var d in await devices.ListAsync(d => d.UserId == userId && d.RevokedAt == null, ct))
        {
            d.Revoke(now); // o access token da extensão deixa de valer na hora (OnTokenValidated)
            audit.Add(AuditEvent.Record(userId, AuditActions.DeviceRevoked, now));
        }
    }
}

// ------------------------------------------------------------------ trocar a senha (logado)

/// <summary>
/// Troca a senha da pessoa logada. Encerra todas as outras sessões (refresh tokens e extensões pareadas) e devolve
/// um par de tokens novo para a sessão atual continuar.
/// </summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<AuthResult>;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Informe a senha atual.");
        RuleFor(x => x.NewPassword).StrongPassword()
            .NotEqual(x => x.CurrentPassword).WithMessage("A nova senha deve ser diferente da senha atual.");
    }
}

public sealed class ChangePasswordHandler(
    IRepository<User> users,
    IRepository<RefreshToken> tokens,
    IRepository<ExtensionDevice> devices,
    IRepository<AuditEvent> audit,
    IPasswordHasher hasher,
    IAuthTokenIssuer issuer,
    ICurrentUser current,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<ChangePasswordCommand, AuthResult>
{
    public async Task<AuthResult> Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        var id = current.UserId;
        var user = await users.FindAsync(u => u.Id == id, ct) ?? throw new UnauthorizedException("Conta não encontrada.");
        if (!hasher.Verify(cmd.CurrentPassword, user.PasswordHash))
            throw new ValidationException(new[] { new ValidationFailure(nameof(cmd.CurrentPassword), "Senha atual incorreta.") });

        var now = clock.UtcNow;
        user.ChangePassword(hasher.Hash(cmd.NewPassword));
        await Sessions.RevokeAllAsync(tokens, devices, audit, user.Id, now, ct);
        audit.Add(AuditEvent.Record(user.Id, AuditActions.PasswordChanged, now));
        var result = issuer.Issue(user); // sessão atual segue com um par novo
        await uow.SaveChangesAsync(ct);
        return result;
    }
}

// ------------------------------------------------------------------ esqueci minha senha

/// <summary>
/// Pede o link de redefinição. A resposta é sempre a mesma, exista ou não conta com o e-mail (não revela cadastro).
/// </summary>
public sealed record ForgotPasswordCommand(string Email) : IRequest<Unit>;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator() =>
        RuleFor(x => x.Email).NotEmpty().WithMessage("Informe o e-mail.").ValidEmail().WithMessage("Informe um e-mail válido.").MaximumLength(254);
}

public sealed class ForgotPasswordHandler(
    IRepository<User> users,
    IRepository<UserToken> userTokens,
    ITokenService tokenService,
    IEmailSender email,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<ForgotPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ForgotPasswordCommand cmd, CancellationToken ct)
    {
        var normalized = cmd.Email.Trim().ToLowerInvariant();
        var user = await users.FindAsync(u => u.Email == normalized, ct);
        if (user is null) return Unit.Value; // mesma resposta: sem enumeração de contas

        var raw = await UserTokens.IssueAsync(userTokens, tokenService, clock.UtcNow, user.Id, UserTokenPurpose.PasswordReset, UserTokens.PasswordResetLifetime, ct);
        await uow.SaveChangesAsync(ct);
        await email.SendPasswordResetAsync(user.Email, user.Name, raw, ct);
        return Unit.Value;
    }
}

// ------------------------------------------------------------------ redefinir a senha com o link

/// <summary>Define a senha nova com o token do link (uso único, 30 min) e encerra todas as sessões da conta.</summary>
public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Unit>;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Informe o código de redefinição.").MaximumLength(200).WithMessage(ResetPasswordHandler.InvalidToken);
        RuleFor(x => x.NewPassword).StrongPassword();
    }
}

public sealed class ResetPasswordHandler(
    IRepository<UserToken> userTokens,
    IRepository<User> users,
    IRepository<RefreshToken> tokens,
    IRepository<ExtensionDevice> devices,
    IRepository<AuditEvent> audit,
    ITokenService tokenService,
    IPasswordHasher hasher,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<ResetPasswordCommand, Unit>
{
    public const string InvalidToken = "O link de redefinição é inválido ou expirou. Peça um novo link.";

    public async Task<Unit> Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var token = await UserTokens.FindUsableAsync(userTokens, tokenService, now, cmd.Token, UserTokenPurpose.PasswordReset, InvalidToken, ct);
        var user = await users.FindAsync(u => u.Id == token.UserId, ct)
            ?? throw new ValidationException(new[] { new ValidationFailure("Token", InvalidToken) });

        foreach (var pending in await userTokens.ListAsync(t => t.UserId == user.Id && t.Purpose == UserTokenPurpose.PasswordReset && t.UsedAt == null, ct))
            pending.Consume(now); // este e qualquer outro link de redefinição em aberto
        user.ChangePassword(hasher.Hash(cmd.NewPassword));
        user.MarkEmailVerified(now); // abrir o link prova o acesso à caixa de e-mail
        await Sessions.RevokeAllAsync(tokens, devices, audit, user.Id, now, ct);
        audit.Add(AuditEvent.Record(user.Id, AuditActions.PasswordReset, now));
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
