using FluentValidation;
using MediatR;
using Zyft.Application.Common;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Audit;
using Zyft.Domain.Consents;
using Zyft.Domain.Extension;
using Zyft.Domain.Profiles;
using Zyft.Domain.Users;

namespace Zyft.Application.Auth;

// ------------------------------------------------------------------ cadastro

/// <summary>
/// Cria a conta, o Perfil Mestre vazio e os consentimentos padrão; já devolve tokens (scope=web) e envia o link de
/// confirmação de e-mail (o e-mail não verificado não bloqueia nenhuma rota).
/// </summary>
/// <param name="AcceptTerms">Precisa ser <c>true</c>: aceite dos termos de uso e da política de privacidade.</param>
/// <param name="TermsVersion">Versão aceita; precisa ser a vigente (<c>GET /auth/terms</c>).</param>
public sealed record RegisterCommand(string Name, string Email, string Password, bool AcceptTerms = false, string? TermsVersion = null) : IRequest<AuthResult>;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Name).Visible().WithMessage("Informe o nome.").MaximumLength(120).WithMessage("O nome deve ter no máximo 120 caracteres.");
        RuleFor(x => x.Email).NotEmpty().WithMessage("Informe o e-mail.").ValidEmail().WithMessage("Informe um e-mail válido.").MaximumLength(254);
        RuleFor(x => x.Password).StrongPassword();
        RuleFor(x => x.AcceptTerms).Equal(true).WithMessage("Aceite os termos de uso e a política de privacidade.");
        RuleFor(x => x.TermsVersion)
            .NotEmpty().WithMessage("Informe a versão dos termos aceita.")
            .Equal(Terms.CurrentVersion).WithMessage("Os termos foram atualizados. Recarregue a página e aceite a versão atual.")
            .When(x => x.AcceptTerms);
    }
}

public sealed class RegisterHandler(
    IRepository<User> users,
    IRepository<Consent> consents,
    IRepository<AuditEvent> audit,
    IProfileRepository profiles,
    IRepository<UserToken> userTokens,
    IPasswordHasher hasher,
    IAuthTokenIssuer issuer,
    ITokenService tokenService,
    IEmailSender emailSender,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<RegisterCommand, AuthResult>
{
    public async Task<AuthResult> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        var email = User.NormalizeEmail(cmd.Email);
        if (await users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Já existe uma conta com este e-mail.");

        var user = User.Create(email, cmd.Name, hasher.Hash(cmd.Password));
        user.AcceptTerms(cmd.TermsVersion ?? Terms.CurrentVersion, clock.UtcNow);
        users.Add(user);
        profiles.Add(Profile.CreateFor(user.Id, user.Name, user.Email));
        consents.AddRange(Consent.Defaults(user.Id));
        audit.Add(AuditEvent.Record(user.Id, AuditActions.AccountCreated, clock.UtcNow));
        audit.Add(AuditEvent.Record(user.Id, AuditActions.TermsAccepted, clock.UtcNow));
        var verification = await UserTokens.IssueAsync(userTokens, tokenService, clock.UtcNow, user.Id, UserTokenPurpose.EmailVerification, UserTokens.EmailVerificationLifetime, ct);
        var result = issuer.Issue(user);
        await uow.SaveChangesAsync(ct);
        // E-mail não verificado não bloqueia nada (só informativo): a conta já sai com tokens.
        await emailSender.SendEmailVerificationAsync(user.Email, user.Name, verification, ct);
        return result;
    }
}

/// <summary>Versão vigente dos termos de uso e da política de privacidade, para o cadastro enviar no aceite.</summary>
public sealed record TermsInfo(string Version);

// ------------------------------------------------------------------ login

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResult>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Informe o e-mail.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Informe a senha.");
    }
}

public sealed class LoginHandler(IRepository<User> users, IPasswordHasher hasher, IAuthTokenIssuer issuer, IUnitOfWork uow) : IRequestHandler<LoginCommand, AuthResult>
{
    // Hash descartável para gastar o mesmo tempo quando o e-mail não existe (evita enumeração por tempo de resposta).
    private static string? _dummyHash;

    public async Task<AuthResult> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var email = cmd.Email.Trim().ToLowerInvariant();
        var user = await users.FindAsync(u => u.Email == email, ct);
        var ok = hasher.Verify(cmd.Password, user?.PasswordHash ?? (_dummyHash ??= hasher.Hash(Guid.NewGuid().ToString())));
        if (user is null || !ok) throw new UnauthorizedException("E-mail ou senha incorretos.");

        var result = issuer.Issue(user);
        await uow.SaveChangesAsync(ct);
        return result;
    }
}

// ------------------------------------------------------------------ refresh (rotação + detecção de reuso)

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResult>;

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator() => RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Informe o refresh token.");
}

public sealed class RefreshTokenHandler(
    IRepository<RefreshToken> tokens,
    IRepository<User> users,
    IRepository<ExtensionDevice> devices,
    ITokenService tokenService,
    IAuthTokenIssuer issuer,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    public async Task<AuthResult> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var hash = tokenService.Hash(cmd.RefreshToken);
        var token = await tokens.FindAsync(t => t.TokenHash == hash, ct) ?? throw new UnauthorizedException("Sessão inválida. Faça login novamente.");

        if (token.WasRotated)
        {
            // Reuso de token já trocado: possível roubo. Revoga toda a cadeia que descende dele.
            await RevokeChainAsync(token, now, ct);
            await uow.SaveChangesAsync(ct);
            throw new UnauthorizedException("Sessão encerrada por segurança (token reutilizado). Faça login novamente.");
        }

        if (!token.IsActive(now)) throw new UnauthorizedException("Sessão expirada. Faça login novamente.");

        var user = await users.FindAsync(u => u.Id == token.UserId, ct) ?? throw new UnauthorizedException("Sessão inválida. Faça login novamente.");
        if (token.DeviceId is { } deviceId)
        {
            var device = await devices.FindAsync(d => d.Id == deviceId && d.UserId == user.Id, ct);
            if (device is null || !device.IsActive) throw new UnauthorizedException("Esta extensão foi desconectada. Pareie novamente.");
            device.Touch(now);
        }

        var result = issuer.Issue(user, token.DeviceId);
        token.Revoke(now, tokenService.Hash(result.RefreshToken));
        await uow.SaveChangesAsync(ct);
        return result;
    }

    private async Task RevokeChainAsync(RefreshToken start, DateTime now, CancellationToken ct)
    {
        var current = start;
        var guard = 0;
        while (current.ReplacedByHash is { } next && guard++ < 1000)
        {
            var child = await tokens.FindAsync(t => t.TokenHash == next, ct);
            if (child is null) break;
            child.Revoke(now);
            current = child;
        }
    }
}

// ------------------------------------------------------------------ logout

public sealed record LogoutCommand(string RefreshToken) : IRequest<Unit>;

public sealed class LogoutHandler(IRepository<RefreshToken> tokens, ITokenService tokenService, IUnitOfWork uow, IClock clock) : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.RefreshToken)) return Unit.Value;
        var hash = tokenService.Hash(cmd.RefreshToken);
        var token = await tokens.FindAsync(t => t.TokenHash == hash, ct);
        if (token is not null)
        {
            token.Revoke(clock.UtcNow);
            await uow.SaveChangesAsync(ct);
        }

        return Unit.Value;
    }
}
