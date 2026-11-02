using Zyft.Domain.Common;

namespace Zyft.Domain.Users;

/// <summary>Termos de uso e política de privacidade vigentes (docs/07-legal). Mudar a versão exige novo aceite no cadastro.</summary>
public static class Terms
{
    public const string CurrentVersion = "2026-10-22";
}

/// <summary>Conta da pessoa candidata. A exclusão (RF-08) remove a conta e tudo o que depende dela.</summary>
public sealed class User : Entity
{
    private User() { }

    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    /// <summary>Quando o e-mail foi confirmado pelo link enviado. Nulo = não verificado (só informativo, não bloqueia nada).</summary>
    public DateTime? EmailVerifiedAt { get; private set; }

    public bool IsEmailVerified => EmailVerifiedAt is not null;

    /// <summary>Versão dos termos de uso e da política de privacidade aceita no cadastro (ex.: <c>2026-10-22</c>).</summary>
    public string? TermsVersion { get; private set; }

    public DateTime? TermsAcceptedAt { get; private set; }

    /// <summary>Reservado para exclusão lógica; o MVP 1 faz exclusão física (cascade).</summary>
    public DateTime? DeletedAt { get; private set; }

    public static User Create(string email, string name, string passwordHash) => new()
    {
        Email = NormalizeEmail(email),
        Name = Guard.Required(name, "user.name_required", "Informe o nome."),
        PasswordHash = Guard.Required(passwordHash, "user.password_required", "Senha inválida."),
    };

    public static string NormalizeEmail(string email) =>
        Guard.Required(email, "user.email_required", "Informe o e-mail.").ToLowerInvariant();

    /// <summary>Marca o e-mail como verificado. Idempotente: mantém a primeira data.</summary>
    public void MarkEmailVerified(DateTime now) => EmailVerifiedAt ??= now;

    /// <summary>Registra o aceite dos termos de uso e da política de privacidade na versão informada.</summary>
    public void AcceptTerms(string version, DateTime now)
    {
        TermsVersion = Guard.Required(version, "user.terms_version_required", "Informe a versão dos termos aceita.");
        TermsAcceptedAt = now;
    }

    public void ChangePassword(string passwordHash) =>
        PasswordHash = Guard.Required(passwordHash, "user.password_required", "Senha inválida.");

    public void Rename(string name) => Name = Guard.Required(name, "user.name_required", "Informe o nome.");
}
