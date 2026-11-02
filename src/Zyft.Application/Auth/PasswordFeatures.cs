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
