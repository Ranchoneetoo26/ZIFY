using FluentValidation;

namespace Zyft.Application.Common;

public static class ValidationExtensions
{
    /// <summary>Senha: mínimo 8 caracteres, com pelo menos uma letra e um número.</summary>
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Informe a senha.")
            .MinimumLength(8).WithMessage("A senha deve ter pelo menos 8 caracteres.")
            .MaximumLength(128).WithMessage("A senha deve ter no máximo 128 caracteres.")
            .Matches("[A-Za-z]").WithMessage("A senha deve ter pelo menos uma letra.")
            .Matches("[0-9]").WithMessage("A senha deve ter pelo menos um número.");

    public static IRuleBuilderOptions<T, string?> OptionalUrl<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(v => string.IsNullOrWhiteSpace(v) || (Uri.TryCreate(v, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("Informe um endereço http(s) válido.");
}
