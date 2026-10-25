using Zyft.Application.Common.Text;
using Zyft.Domain.Profiles;

namespace Zyft.Application.Common;

/// <summary>Textos pt-BR dos enums (usados no de-para com opções de portais e nos fatos do perfil).</summary>
public static class DisplayNames
{
    public static string Of(WorkModel v) => v switch
    {
        WorkModel.OnSite => "Presencial",
        WorkModel.Hybrid => "Híbrido",
        WorkModel.Remote => "Remoto",
        _ => v.ToString(),
    };

    public static string Of(EducationLevel v) => v switch
    {
        EducationLevel.ElementarySchool => "Ensino fundamental",
        EducationLevel.HighSchool => "Ensino médio",
        EducationLevel.Technical => "Técnico",
        EducationLevel.Undergraduate => "Graduação",
        EducationLevel.Postgraduate => "Pós-graduação",
        EducationLevel.Masters => "Mestrado",
        EducationLevel.Doctorate => "Doutorado",
        _ => v.ToString(),
    };

    public static string Of(EducationStatus v) => v switch
    {
        EducationStatus.InProgress => "Cursando",
        EducationStatus.Completed => "Concluído",
        EducationStatus.Paused => "Trancado",
        EducationStatus.Incomplete => "Incompleto",
        _ => v.ToString(),
    };

    public static string Of(LanguageProficiency v) => v switch
    {
        LanguageProficiency.Basic => "Básico",
        LanguageProficiency.Intermediate => "Intermediário",
        LanguageProficiency.Advanced => "Avançado",
        LanguageProficiency.Fluent => "Fluente",
        LanguageProficiency.Native => "Nativo",
        _ => v.ToString(),
    };

    public static string Of(SkillLevel v) => v switch
    {
        SkillLevel.Basic => "Básico",
        SkillLevel.Intermediate => "Intermediário",
        SkillLevel.Advanced => "Avançado",
        _ => v.ToString(),
    };

    public static string YesNo(bool v) => v ? "Sim" : "Não";

    /// <summary>Converte texto livre (nome do enum em inglês ou rótulo pt-BR) no enum correspondente.</summary>
    public static bool TryParse<TEnum>(string? text, out TEnum value) where TEnum : struct, Enum
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (Enum.TryParse(text.Trim(), ignoreCase: true, out value) && Enum.IsDefined(value)) return true;
        var n = TextTools.Norm(text);
        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            var label = TextTools.Norm(Label(candidate));
            if (label == n || (n.Length > 3 && (label.StartsWith(n) || n.StartsWith(label))))
            {
                value = candidate;
                return true;
            }
        }

        // Sinônimos comuns em currículos.
        // Ordem importa: "incomplet" antes de "complet" (senão "incompleto" viraria "Concluído").
        var synonyms = new List<(string Key, string Label)>
        {
            ("incomplet", "Incompleto"), ("superior", "Graduação"), ("bacharel", "Graduação"), ("licenciatura", "Graduação"), ("tecnolog", "Graduação"),
            ("mba", "Pós-graduação"), ("especializacao", "Pós-graduação"), ("em andamento", "Cursando"), ("cursando", "Cursando"),
            ("conclu", "Concluído"), ("complet", "Concluído"), ("trancad", "Trancado"), ("fluen", "Fluente"),
            ("basic", "Básico"), ("intermediat", "Intermediário"), ("advanced", "Avançado"), ("native", "Nativo"),
        };
        foreach (var (k, label) in synonyms)
        {
            if (!n.Contains(k)) continue;
            foreach (var candidate in Enum.GetValues<TEnum>())
                if (Label(candidate) == label) { value = candidate; return true; }
        }

        return false;
    }

    private static string Label<TEnum>(TEnum v) where TEnum : struct, Enum => v switch
    {
        WorkModel w => Of(w),
        EducationLevel e => Of(e),
        EducationStatus s => Of(s),
        LanguageProficiency l => Of(l),
        SkillLevel k => Of(k),
        _ => v.ToString(),
    };
}
