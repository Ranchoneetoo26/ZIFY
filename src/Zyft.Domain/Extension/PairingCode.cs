using Zyft.Domain.Common;

namespace Zyft.Domain.Extension;

/// <summary>Código curto (6 dígitos) gerado na plataforma logada; vale 10 minutos e uma única vez.</summary>
public sealed class PairingCode : Entity
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private PairingCode() { }

    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public static PairingCode Create(Guid userId, string codeHash, DateTime now) => new()
    {
        UserId = userId,
        CodeHash = codeHash,
        ExpiresAt = now.Add(Lifetime),
    };

    public bool IsValid(DateTime now) => UsedAt is null && ExpiresAt > now;

    public void Use(DateTime now)
    {
        if (!IsValid(now))
            throw new DomainException("pairing.invalid_code", "Código de pareamento inválido ou expirado.");
        UsedAt = now;
    }
}
