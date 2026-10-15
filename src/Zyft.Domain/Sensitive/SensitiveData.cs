using Zyft.Domain.Common;

namespace Zyft.Domain.Sensitive;

public enum SensitiveDataKind
{
    Gender = 1,
    Ethnicity = 2,
    Disability = 3,
    Religion = 4,
    SexualOrientation = 5,
    Health = 6,
    SocialName = 7,
    Other = 99,
}

/// <summary>
/// Dado sensível segregado do perfil (RN-04, ADR-0014): valor sempre cifrado, nunca enviado à IA,
/// só usado com consentimento (c) e marcação manual do campo.
/// </summary>
public sealed class SensitiveData : Entity
{
    private SensitiveData() { }

    public Guid UserId { get; private set; }
    public SensitiveDataKind Kind { get; private set; }
    public string EncryptedValue { get; private set; } = default!;

    public static SensitiveData Create(Guid userId, SensitiveDataKind kind, string encryptedValue) => new()
    {
        UserId = userId,
        Kind = kind,
        EncryptedValue = Guard.Required(encryptedValue, "sensitive.value_required", "Informe o valor."),
    };

    public void Replace(string encryptedValue) =>
        EncryptedValue = Guard.Required(encryptedValue, "sensitive.value_required", "Informe o valor.");
}
