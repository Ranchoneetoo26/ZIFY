using Zyft.Domain.Common;

namespace Zyft.Domain.Extension;

/// <summary>Extensão de navegador pareada à conta (RF-09).</summary>
public sealed class ExtensionDevice : Entity
{
    private ExtensionDevice() { }

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = default!;
    public DateTime PairedAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    public static ExtensionDevice Pair(Guid userId, string name, DateTime now) => new()
    {
        UserId = userId,
        Name = Guard.Required(name, "device.name_required", "Informe um nome para o dispositivo."),
        PairedAt = now,
        LastSeenAt = now,
    };

    public void Touch(DateTime now) => LastSeenAt = now;

    public void Revoke(DateTime now) => RevokedAt ??= now;
}
