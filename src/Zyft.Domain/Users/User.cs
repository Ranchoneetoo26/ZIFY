using Zyft.Domain.Common;

namespace Zyft.Domain.Users;

/// <summary>Conta da pessoa candidata. A exclusão (RF-08) remove a conta e tudo o que depende dela.</summary>
public sealed class User : Entity
{
    private User() { }

    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string Name { get; private set; } = default!;

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

    public void Rename(string name) => Name = Guard.Required(name, "user.name_required", "Informe o nome.");
}
