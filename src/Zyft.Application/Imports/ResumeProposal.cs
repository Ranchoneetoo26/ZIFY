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
public sealed record ProposalExperience(string? Company, string? Role, string? StartDate, string? EndDate, bool IsCurrent, string? Activities, string? Tools, string? Results);

public sealed record ProposalEducation(string? Institution, string? Course, string? Level, string? Status, string? StartDate, string? EndDate);

public sealed record ProposalCourse(string? Name, string? Institution, int? Hours, string? CompletedAt, string? Type = "Course");

public sealed record ProposalSkill(string? Name, string? Level = null);

public sealed record ProposalLanguage(string? Name, string? Proficiency);
