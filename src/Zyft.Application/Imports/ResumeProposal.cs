namespace Zyft.Application.Imports;

/// <summary>Proposta de perfil extraída do currículo (RF-03). Nada entra no perfil sem aprovação (RF-04).</summary>
public sealed record ResumeProposal
{
    /// <summary>Campos básicos propostos (mesmos nomes de <c>PUT /profile</c>).</summary>
    public ProposalProfile Profile { get; init; } = new();
    public List<ProposalExperience> Experiences { get; init; } = new();
    public List<ProposalEducation> Educations { get; init; } = new();
    public List<ProposalCourse> Courses { get; init; } = new();
    public List<ProposalSkill> Skills { get; init; } = new();
    public List<ProposalLanguage> Languages { get; init; } = new();

    /// <summary>Avisos para a revisão (ex.: "IA indisponível").</summary>
    public List<string> Warnings { get; init; } = new();
}

public sealed record ProposalProfile
{
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? LinkedinUrl { get; init; }
    public string? GithubUrl { get; init; }
    public string? PortfolioUrl { get; init; }
    public string? Headline { get; init; }
    public string? Summary { get; init; }
    public string? DesiredRoles { get; init; }
}

/// <summary>Itens de lista usam os mesmos nomes das entidades do perfil; enums vão como nome (ex.: "Undergraduate").
/// Datas como texto (AAAA-MM, MM/AAAA ou AAAA) porque vêm do currículo e podem ser editadas na revisão.</summary>
/// <para><c>missingFields</c> (só na proposta): campos obrigatórios que o currículo não trouxe ou que não foram
/// reconhecidos (ex.: formação sem "status"). O Zyft não inventa esses valores: a revisão pede para a pessoa escolher.</para>
public sealed record ProposalExperience(string? Company, string? Role, string? StartDate, string? EndDate, bool IsCurrent, string? Activities, string? Tools, string? Results,
    IReadOnlyList<string>? MissingFields = null);

public sealed record ProposalEducation(string? Institution, string? Course, string? Level, string? Status, string? StartDate, string? EndDate,
    IReadOnlyList<string>? MissingFields = null);

public sealed record ProposalCourse(string? Name, string? Institution, int? Hours, string? CompletedAt, string? Type = "Course");

public sealed record ProposalSkill(string? Name, string? Level = null);

public sealed record ProposalLanguage(string? Name, string? Proficiency, IReadOnlyList<string>? MissingFields = null);

/// <summary>Marca, na proposta, os itens incompletos (campos obrigatórios ausentes ou não reconhecidos). Não preenche nada.</summary>
public static class ProposalCompleteness
{
    public static ResumeProposal Mark(ResumeProposal p) => p with
    {
        Experiences = p.Experiences.Select(e => e with { MissingFields = Missing(
            ("company", !string.IsNullOrWhiteSpace(e.Company)),
            ("role", !string.IsNullOrWhiteSpace(e.Role)),
            ("startDate", Common.Dates.TryParseLoose(e.StartDate, out _))) }).ToList(),
        Educations = p.Educations.Select(e =>
        {
            // Rótulo não reconhecido não vira palpite: fica nulo e marcado para a pessoa escolher.
            var level = Common.DisplayNames.TryParse<Domain.Profiles.EducationLevel>(e.Level, out var lv) ? lv.ToString() : null;
            var status = Common.DisplayNames.TryParse<Domain.Profiles.EducationStatus>(e.Status, out var st) ? st.ToString() : null;
            return e with
            {
                Level = level,
                Status = status,
                MissingFields = Missing(
                    ("institution", !string.IsNullOrWhiteSpace(e.Institution)),
                    ("course", !string.IsNullOrWhiteSpace(e.Course)),
                    ("level", level is not null),
                    ("status", status is not null),
                    ("startDate", Common.Dates.TryParseLoose(e.StartDate, out _))),
            };
        }).ToList(),
        Languages = p.Languages.Select(l =>
        {
            var prof = Common.DisplayNames.TryParse<Domain.Profiles.LanguageProficiency>(l.Proficiency, out var pr) ? pr.ToString() : null;
            return l with { Proficiency = prof, MissingFields = Missing(("proficiency", prof is not null)) };
        }).ToList(),
    };

    private static IReadOnlyList<string>? Missing(params (string Field, bool Ok)[] checks)
    {
        var m = checks.Where(c => !c.Ok).Select(c => c.Field).ToList();
        return m.Count == 0 ? null : m;
    }
}
