using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Zyft.Domain.Common;

namespace Zyft.Domain.Profiles;

/// <summary>
/// Campo personalizado do Perfil Mestre ("Informações da sua área"): nome + valor livres criados pela pessoa
/// (ex.: "Disponível para plantão noturno"). Entra nos fatos da IA e no de-para com a chave <c>custom:&lt;id&gt;</c>.
/// Nunca aceita rótulos que pareçam dados sensíveis (RN-04): esses ficam em Dados sensíveis.
/// </summary>
public sealed class CustomField : Entity
{
    public const int MaxLabel = 80;
    public const int MaxValue = 500;

    private CustomField() { }

    public Guid ProfileId { get; private set; }
    public string Label { get; private set; } = default!;
    public string Value { get; private set; } = default!;

    /// <summary>Chave estável usada nos fatos do perfil e no de-para.</summary>
    public string Key => $"custom:{Id}";

    internal static CustomField Create(Guid profileId, string label, string value)
    {
        var f = new CustomField { ProfileId = profileId };
        f.Update(label, value);
        return f;
    }

    internal void Update(string label, string value)
    {
        var l = Guard.Required(label, "profile.custom_field_label_required", "Informe o nome do campo.");
        var v = Guard.Required(value, "profile.custom_field_value_required", "Informe o valor do campo.");
        if (l.Length > MaxLabel) throw new DomainException("profile.custom_field_label_too_long", $"O nome do campo deve ter no máximo {MaxLabel} caracteres.");
        if (v.Length > MaxValue) throw new DomainException("profile.custom_field_value_too_long", $"O valor do campo deve ter no máximo {MaxValue} caracteres.");
        if (SensitiveLabels.LooksSensitive(l))
            throw new DomainException("profile.custom_field_sensitive",
                "Este campo parece pedir um dado sensível ou um documento (ex.: raça, religião, saúde, deficiência, gênero, CPF, RG, senha). " +
                "Use a área Dados sensíveis, que fica trancada e fora da IA.");
        Label = l;
        Value = v;
    }
}

/// <summary>Detecta rótulos que pedem dado sensível (LGPD art. 5º, II), documento ou senha. Compara sem acento e por palavra.</summary>
public static class SensitiveLabels
{
    private static readonly Regex Pattern = new(
        @"\b(raca|racial|etnia|etnica|etnico|cor da pele|cor raca|religiao|religioso|religiosa|crenca|culto|saude|doenca|doencas|laudo|cid|hiv|deficiencia|deficiente|pcd|neurodivergente|genero|sexo|sexual|orientacao sexual|identidade de genero|transgenero|trans|lgbt\w*|gestante|gravidez|gravida|nome social|biometria|biometrico|genetico|genetica|sindicato|sindical|partido|filiacao|cpf|rg|carteira de identidade|documento de identidade|passaporte|senha|password|pin)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool LooksSensitive(string? label) => Pattern.IsMatch(Normalize(label));

    internal static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var d = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var ch in d)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ');
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }
}
