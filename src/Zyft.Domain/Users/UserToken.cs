using Zyft.Domain.Common;

namespace Zyft.Domain.Users;

/// <summary>Finalidade de um <see cref="UserToken"/> (gravada como texto).</summary>
public enum UserTokenPurpose
{
    /// <summary>Confirmação do e-mail da conta (validade de 24 h).</summary>
    EmailVerification = 1,

    /// <summary>Redefinição de senha pelo link "esqueci minha senha" (validade de 30 min).</summary>
    PasswordReset = 2,
}

/// <summary>
/// Token de uso único enviado por e-mail (confirmação de e-mail e recuperação de senha). Só o hash SHA-256
/// é guardado; o valor aberto vai apenas no link. Expira e não pode ser usado duas vezes.
/// </summary>
public sealed class UserToken : Entity
{
    private UserToken() { }

    public Guid UserId { get; private set; }
    public UserTokenPurpose Purpose { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Quando o token foi usado ou invalidado (por um token mais novo, por exemplo).</summary>
    public DateTime? UsedAt { get; private set; }

    public static UserToken Issue(Guid userId, UserTokenPurpose purpose, string tokenHash, DateTime expiresAt) => new()
    {
        UserId = userId,
        Purpose = purpose,
        TokenHash = Guard.Required(tokenHash, "user_token.hash_required", "Token inválido."),
        ExpiresAt = expiresAt,
    };

    public bool IsUsable(DateTime now) => UsedAt is null && ExpiresAt > now;

    /// <summary>Marca como usado. Idempotente: mantém a primeira data.</summary>
    public void Consume(DateTime now) => UsedAt ??= now;
}
