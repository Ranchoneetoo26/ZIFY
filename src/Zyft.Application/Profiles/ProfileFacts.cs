using System.Globalization;
using Zyft.Application.Common;
using Zyft.Domain.Profiles;

namespace Zyft.Application.Profiles;

/// <summary>Um fato do perfil "achatado": chave estável (ex.: <c>experiences[0].company</c>), rótulo legível e valor.</summary>
public sealed record ProfileFact(string Key, string Label, string Value);

/// <summary>
/// Conjunto de fatos do perfil usado no de-para (porte de <c>flattenProfile</c>). Só contém valores preenchidos
/// e nunca contém dados sensíveis (RN-04): eles ficam em outra tabela e não passam por aqui.
/// </summary>
public sealed class ProfileFactSet
{
    private readonly Dictionary<string, ProfileFact> _byKey;

    public ProfileFactSet(IEnumerable<ProfileFact> facts)
    {
        Facts = facts.ToList();
        _byKey = Facts.GroupBy(f => f.Key).ToDictionary(g => g.Key, g => g.First());
    }

    public IReadOnlyList<ProfileFact> Facts { get; }

    public ProfileFact? Get(string key) => _byKey.GetValueOrDefault(key);

    public bool Contains(string key) => _byKey.ContainsKey(key);

    /// <summary>Rótulos de todas as chaves conhecidas (inclusive vazias) para mensagens "Sem X no perfil".</summary>
    public static string LabelOf(string key)
    {
        if (ProfileFacts.TopLabels.TryGetValue(key, out var l)) return l;
        var m = System.Text.RegularExpressions.Regex.Match(key, @"^(\w+)\[(\d+)\]\.(\w+)$");
        if (m.Success && ProfileFacts.GroupLabels.TryGetValue(m.Groups[1].Value, out var g) && g.Fields.TryGetValue(m.Groups[3].Value, out var fl))
            return $"{g.Label} {int.Parse(m.Groups[2].Value) + 1} — {fl}";
        return key;
    }
}

public static class ProfileFacts
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    internal static readonly Dictionary<string, string> TopLabels = new()
    {
        ["personal.fullName"] = "Nome completo",
        ["personal.firstName"] = "Primeiro nome",
        ["personal.lastName"] = "Sobrenome",
        ["personal.email"] = "E-mail",
        ["personal.phone"] = "Telefone/celular",
        ["personal.birthDate"] = "Data de nascimento",
        ["personal.cpf"] = "CPF",
        ["personal.city"] = "Cidade",
        ["personal.state"] = "Estado/UF",
        ["personal.country"] = "País",
        ["personal.address"] = "Endereço",
        ["personal.zipCode"] = "CEP",
        ["personal.linkedin"] = "LinkedIn",
        ["personal.github"] = "GitHub",
        ["personal.portfolio"] = "Portfólio/site",
        ["personal.professionalRegistration"] = "Registro profissional (conselho/órgão de classe)",
        ["objective.headline"] = "Título profissional",
        ["objective.desiredRoles"] = "Cargos desejados",
        ["objective.areas"] = "Áreas de interesse",
        ["objective.workArea"] = "Área de atuação",
        ["objective.seniority"] = "Senioridade",
        ["objective.workModel"] = "Modelo de trabalho (presencial/híbrido/remoto)",
        ["objective.locationPreference"] = "Preferência de localidade",
        ["objective.summary"] = "Resumo/objetivo profissional",
        ["skills"] = "Competências/habilidades",
        ["preferences.availability"] = "Disponibilidade para início",
        ["preferences.salaryExpectation"] = "Pretensão salarial",
        ["preferences.salaryType"] = "Tipo de remuneração pretendida",
        ["preferences.travel"] = "Disponibilidade para viagens",
        ["preferences.relocation"] = "Disponibilidade para mudança",
        ["preferences.contractTypes"] = "Tipos de contrato aceitos",
        ["preferences.driverLicense"] = "CNH (categoria)",
    };

    internal static readonly Dictionary<string, (string Label, Dictionary<string, string> Fields)> GroupLabels = new()
    {
        ["experiences"] = ("Experiência", new() { ["company"] = "empresa", ["role"] = "cargo", ["start"] = "início", ["end"] = "fim", ["activities"] = "atividades/atribuições", ["tools"] = "ferramentas", ["results"] = "resultados/realizações" }),
        ["education"] = ("Formação", new() { ["institution"] = "instituição", ["course"] = "curso", ["degree"] = "grau/nível", ["start"] = "início", ["end"] = "conclusão", ["status"] = "situação" }),
        ["courses"] = ("Curso", new() { ["name"] = "nome", ["institution"] = "instituição", ["year"] = "ano", ["hours"] = "carga horária" }),
        ["certifications"] = ("Certificação", new() { ["name"] = "nome", ["issuer"] = "emissor", ["year"] = "ano" }),
        ["languages"] = ("Idioma", new() { ["language"] = "idioma", ["level"] = "nível" }),
    };

    /// <summary>Ordem estável dos itens repetidos (o índice <c>[i]</c> das chaves depende dela).</summary>
    public static IReadOnlyList<Experience> OrderedExperiences(Profile p) =>
        p.Experiences.OrderByDescending(e => e.IsCurrent).ThenByDescending(e => e.StartDate).ThenBy(e => e.Company).ToList();

    public static IReadOnlyList<Education> OrderedEducations(Profile p) =>
        p.Educations.OrderByDescending(e => e.StartDate).ThenBy(e => e.Institution).ToList();

    public static IReadOnlyList<Course> OrderedCourses(Profile p, CourseType type) =>
        p.Courses.Where(c => c.Type == type).OrderByDescending(c => c.CompletedAt ?? DateOnly.MinValue).ThenBy(c => c.Name).ToList();

    public static IReadOnlyList<Language> OrderedLanguages(Profile p) => p.Languages.OrderBy(l => l.Name).ToList();

    public static IReadOnlyList<CustomField> OrderedCustomFields(Profile p) => p.CustomFields.OrderBy(f => f.Label, StringComparer.OrdinalIgnoreCase).ToList();

    public static string FormatMoney(decimal v) => v.ToString("C", PtBr);

    public static ProfileFactSet Build(Profile p)
    {
        var list = new List<ProfileFact>();
        void Push(string key, string? value)
        {
            var v = value?.Trim();
            if (!string.IsNullOrEmpty(v)) list.Add(new ProfileFact(key, ProfileFactSet.LabelOf(key), v));
        }

        Push("personal.fullName", p.FullName);
        var parts = (p.FullName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1)
        {
            Push("personal.firstName", parts[0]);
            Push("personal.lastName", string.Join(' ', parts[1..]));
        }

        Push("personal.email", p.Email);
        Push("personal.phone", p.Phone);
        Push("personal.birthDate", p.BirthDate is { } bd ? Dates.Full(bd) : null);
        Push("personal.city", p.City);
        Push("personal.state", p.State);
        Push("personal.country", p.Country);
        Push("personal.zipCode", p.ZipCode);
        Push("personal.linkedin", p.LinkedinUrl);
        Push("personal.github", p.GithubUrl);
        Push("personal.portfolio", p.PortfolioUrl);
        Push("personal.professionalRegistration", p.ProfessionalRegistration);
        Push("objective.headline", p.Headline);
        Push("objective.desiredRoles", p.DesiredRoles);
        Push("objective.areas", p.Areas);
        Push("objective.workArea", p.WorkArea);
        Push("objective.seniority", p.Seniority);
        Push("objective.workModel", p.WorkModel is { } wm ? DisplayNames.Of(wm) : null);
        Push("objective.locationPreference", p.LocationPreference);
        Push("objective.summary", p.Summary);
        Push("skills", string.Join(", ", p.Skills.Select(s => s.Name)));
        Push("preferences.availability", p.Availability);
        Push("preferences.salaryType", p.SalaryType);
        Push("preferences.salaryExpectation", p.SalaryExpectation is { } sal ? FormatMoney(sal) : null);
        Push("preferences.travel", p.AvailableToTravel is { } t ? DisplayNames.YesNo(t) : null);
        Push("preferences.relocation", p.AvailableToRelocate is { } r ? DisplayNames.YesNo(r) : null);
        Push("preferences.contractTypes", p.ContractTypes);
        Push("preferences.driverLicense", p.HasDriverLicense switch
        {
            true => p.DriverLicenseCategory ?? "Sim",
            false => "Não possuo",
            null => null,
        });

        var exps = OrderedExperiences(p);
        for (var i = 0; i < exps.Count; i++)
        {
            var e = exps[i];
            Push($"experiences[{i}].company", e.Company);
            Push($"experiences[{i}].role", e.Role);
            Push($"experiences[{i}].start", Dates.MonthYear(e.StartDate));
            Push($"experiences[{i}].end", e.IsCurrent ? "Atual" : e.EndDate is { } end ? Dates.MonthYear(end) : null);
            Push($"experiences[{i}].activities", e.Activities);
            Push($"experiences[{i}].tools", e.Tools);
            Push($"experiences[{i}].results", e.Results);
        }

        var edus = OrderedEducations(p);
        for (var i = 0; i < edus.Count; i++)
        {
            var e = edus[i];
            Push($"education[{i}].institution", e.Institution);
            Push($"education[{i}].course", e.Course);
            Push($"education[{i}].degree", DisplayNames.Of(e.Level));
            Push($"education[{i}].status", DisplayNames.Of(e.Status));
            Push($"education[{i}].start", Dates.MonthYear(e.StartDate));
            Push($"education[{i}].end", e.EndDate is { } end ? Dates.MonthYear(end) : null);
        }

        var courses = OrderedCourses(p, CourseType.Course);
        for (var i = 0; i < courses.Count; i++)
        {
            var c = courses[i];
            Push($"courses[{i}].name", c.Name);
            Push($"courses[{i}].institution", c.Institution);
            Push($"courses[{i}].year", c.CompletedAt?.Year.ToString(CultureInfo.InvariantCulture));
            Push($"courses[{i}].hours", c.Hours is { } h ? $"{h}h" : null);
        }

        var certs = OrderedCourses(p, CourseType.Certification);
        for (var i = 0; i < certs.Count; i++)
        {
            var c = certs[i];
            Push($"certifications[{i}].name", c.Name);
            Push($"certifications[{i}].issuer", c.Institution);
            Push($"certifications[{i}].year", c.CompletedAt?.Year.ToString(CultureInfo.InvariantCulture));
        }

        var langs = OrderedLanguages(p);
        for (var i = 0; i < langs.Count; i++)
        {
            Push($"languages[{i}].language", langs[i].Name);
            Push($"languages[{i}].level", DisplayNames.Of(langs[i].Proficiency));
        }

        // Campos personalizados: chave custom:<id>, rótulo dado pela pessoa (rótulos sensíveis são recusados no domínio).
        foreach (var f in OrderedCustomFields(p))
            if (!string.IsNullOrWhiteSpace(f.Value)) list.Add(new ProfileFact(f.Key, f.Label, f.Value.Trim()));

        return new ProfileFactSet(list);
    }

    /// <summary>
    /// Fatos em linhas legíveis, com fatos derivados calculados aqui (tempo de experiência) — porte de
    /// <c>profileFacts</c>. É a base das evidências numeradas (ADR-0013) e do contexto do texto livre.
    /// </summary>
    public static IReadOnlyList<string> Lines(Profile p, DateOnly today)
    {
        var lines = new List<string>();
        void Add(string label, string? v)
        {
            if (!string.IsNullOrWhiteSpace(v)) lines.Add($"{label}: {v.Trim()}");
        }

        var facts = Build(p);
        foreach (var (key, label) in TopLabels)
        {
            if (key is "personal.firstName" or "personal.lastName") continue;
            Add(label, facts.Get(key)?.Value);
        }

        var total = 0;
        var exps = OrderedExperiences(p);
        for (var i = 0; i < exps.Count; i++)
        {
            var e = exps[i];
            var endMonth = e.IsCurrent || e.EndDate is null ? today : e.EndDate.Value;
            int? dur = e.IsCurrent || e.EndDate is not null ? Months(e.StartDate, endMonth) : null;
            if (dur is > 0) total += dur.Value;
            var period = $"{Dates.MonthYear(e.StartDate)} a {(e.IsCurrent ? "atual" : e.EndDate is { } end ? Dates.MonthYear(end) : "?")}";
            lines.Add($"Experiência {i + 1}: {e.Role} na {e.Company} ({period}{(dur is > 0 ? $", {Duration(dur.Value)}" : string.Empty)})");
            Add($"Atividades (exp. {i + 1})", e.Activities);
            Add($"Ferramentas (exp. {i + 1})", e.Tools);
            Add($"Resultados (exp. {i + 1})", e.Results);
        }

        if (total > 0) lines.Add($"Tempo total de experiência profissional (calculado): {Duration(total)}");

        var edus = OrderedEducations(p);
        for (var i = 0; i < edus.Count; i++)
        {
            var e = edus[i];
            lines.Add($"Formação {i + 1}: {DisplayNames.Of(e.Level)} — {e.Course} em {e.Institution} ({DisplayNames.Of(e.Status)}, {e.StartDate.Year}–{(e.EndDate?.Year.ToString(CultureInfo.InvariantCulture) ?? "?")})");
        }

        var courses = OrderedCourses(p, CourseType.Course);
        for (var i = 0; i < courses.Count; i++)
        {
            var c = courses[i];
            var extra = new[] { c.Institution, c.CompletedAt?.Year.ToString(CultureInfo.InvariantCulture), c.Hours is { } h ? $"{h}h" : null }.Where(x => x is not null);
            lines.Add($"Curso {i + 1}: {c.Name}{(extra.Any() ? $" ({string.Join(", ", extra)})" : string.Empty)}");
        }

        var certs = OrderedCourses(p, CourseType.Certification);
        for (var i = 0; i < certs.Count; i++)
        {
            var c = certs[i];
            lines.Add($"Certificação {i + 1}: {c.Name}{(c.Institution is null ? string.Empty : $" — {c.Institution}")}{(c.CompletedAt is { } d ? $" ({d.Year})" : string.Empty)}");
        }

        foreach (var l in OrderedLanguages(p)) lines.Add($"Idioma: {l.Name} — {DisplayNames.Of(l.Proficiency)}");
        foreach (var f in OrderedCustomFields(p)) Add(f.Label, f.Value);
        return lines;
    }

    private static int Months(DateOnly start, DateOnly end) =>
        end < start ? 0 : (end.Year * 12 + end.Month) - (start.Year * 12 + start.Month) + 1;

    public static string Duration(int months)
    {
        var y = months / 12;
        var m = months % 12;
        var parts = new List<string>();
        if (y > 0) parts.Add($"{y} ano{(y > 1 ? "s" : string.Empty)}");
        if (m > 0) parts.Add($"{m} {(m > 1 ? "meses" : "mês")}");
        return parts.Count == 0 ? "menos de 1 mês" : string.Join(" e ", parts);
    }
}
