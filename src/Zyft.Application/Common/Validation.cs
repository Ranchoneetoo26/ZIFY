using System.Text.RegularExpressions;
using FluentValidation;
using Zyft.Domain.Common;

namespace Zyft.Application.Common;

/// <summary>Limites de tamanho do Perfil Mestre, os mesmos no CRUD e na importação de currículo.</summary>
public static class ProfileLimits
{
    public const int Organization = 200; // empresa, cargo, instituição, curso
    public const int LongText = 4000; // atividades, resultados, resumo
    public const int Tools = 1000;
    public const int SkillName = 100;
    public const int LanguageName = 60;
    public const int MaxHours = 10000;
    public const int Url = 300;
    public const int PhoneLength = 40;
    public const decimal MaxSalary = 999_999_999.99m;
}

/// <summary>E-mail com formato utilizável. A regra padrão do FluentValidation só confere a presença de @.</summary>
public static class Emails
{
    private const string LocalSymbols = "!#$%&'*+/=?^_`{|}~-.";

    public static bool IsValid(string? value)
    {
        var v = value?.Trim() ?? string.Empty;
        var at = v.IndexOf('@');
        if (v.Length > 254 || at is < 1 or > 64 || at != v.LastIndexOf('@')) return false;

        var local = v[..at];
        if (local[0] == '.' || local[^1] == '.' || local.Contains("..", StringComparison.Ordinal)) return false;
        if (!local.All(c => char.IsLetterOrDigit(c) || LocalSymbols.Contains(c))) return false;

        var labels = v[(at + 1)..].Split('.');
        if (labels.Length < 2 || !labels.All(IsDomainLabel)) return false;
        var top = labels[^1];
        return top.Length >= 2 && (top.All(char.IsLetter) || top.StartsWith("xn--", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDomainLabel(string label) =>
        label.Length is >= 1 and <= 63 && label[0] != '-' && label[^1] != '-' && label.All(c => char.IsLetterOrDigit(c) || c == '-');
}

public static partial class Urls
{
    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9+.\-]*:(?!\d)")]
    private static partial Regex HasScheme();

    /// <summary>"linkedin.com/in/ana" → "https://linkedin.com/in/ana". Outro esquema fica como veio, para a validação recusar.</summary>
    public static string? Complete(string? value)
    {
        var v = value?.Trim();
        return string.IsNullOrEmpty(v) || HasScheme().IsMatch(v) ? v : $"https://{v}";
    }

    public static bool IsHttp(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);

    /// <summary>Endereço de perfil: http(s) com domínio de verdade ("https://x" não serve). Igual a <c>webUrl</c> do front.</summary>
    public static bool IsWebAddress(string? value) => IsHttp(value) && new Uri(value!).Host.Contains('.');
}

public static class ValidationExtensions
{
    /// <summary>Senha: de 8 a 128 caracteres, com pelo menos uma letra (de qualquer alfabeto, com ou sem acento) e um número.</summary>
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Informe a senha.")
            .MinimumLength(8).WithMessage("A senha deve ter pelo menos 8 caracteres.")
            .MaximumLength(128).WithMessage("A senha deve ter no máximo 128 caracteres.")
            .Matches(@"\p{L}").WithMessage("A senha deve ter pelo menos uma letra.")
            .Matches("[0-9]").WithMessage("A senha deve ter pelo menos um número.");

    public static IRuleBuilderOptions<T, string?> OptionalUrl<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(v => string.IsNullOrWhiteSpace(v) || Urls.IsWebAddress(v)).WithMessage("Informe um endereço http(s) válido.");

    /// <summary>Texto obrigatório: recusa vazio, espaços e caracteres invisíveis (zero-width).</summary>
    public static IRuleBuilderOptions<T, TText> Visible<T, TText>(this IRuleBuilder<T, TText> rule) where TText : class? =>
        rule.Must(v => !VisibleText.IsBlank(v as string));

    public static IRuleBuilderOptions<T, TText> ValidEmail<T, TText>(this IRuleBuilder<T, TText> rule) where TText : class? =>
        rule.Must(v => Emails.IsValid(v as string));
}
