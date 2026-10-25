using Zyft.Domain.Common;

namespace Zyft.Domain.Users;

/// <summary>
/// Refresh token opaco. Só o hash SHA-256 é guardado. Ao renovar, o token é revogado e aponta
/// para o hash do sucessor (<see cref="ReplacedByHash"/>), formando uma cadeia; o reuso de um
/// token já trocado revoga a cadeia inteira.
/// </summary>
public sealed class RefreshToken : Entity
{
    private RefreshToken() { }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByHash { get; private set; }

    /// <summary>Preenchido quando o token pertence a uma extensão pareada (scope=extension).</summary>
    public Guid? DeviceId { get; private set; }

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTime expiresAt, Guid? deviceId = null) => new()
    {
        UserId = userId,
        TokenHash = tokenHash,
        ExpiresAt = expiresAt,
        DeviceId = deviceId,
    };

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;

    public bool WasRotated => ReplacedByHash is not null;

    public void Revoke(DateTime now, string? replacedByHash = null)
    {
        RevokedAt ??= now;
        if (replacedByHash is not null) ReplacedByHash = replacedByHash;
    }
}
