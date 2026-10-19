using Zyft.Domain.Profiles;

namespace Zyft.Application.Profiles;

public sealed record ExperienceDto(Guid Id, string Company, string Role, DateOnly StartDate, DateOnly? EndDate, bool IsCurrent, string? Activities, string? Tools, string? Results)
{
    public static ExperienceDto From(Experience e) => new(e.Id, e.Company, e.Role, e.StartDate, e.EndDate, e.IsCurrent, e.Activities, e.Tools, e.Results);
}

public sealed record EducationDto(Guid Id, string Institution, string Course, EducationLevel Level, EducationStatus Status, DateOnly StartDate, DateOnly? EndDate)
{
    public static EducationDto From(Education e) => new(e.Id, e.Institution, e.Course, e.Level, e.Status, e.StartDate, e.EndDate);
}

public sealed record CourseDto(Guid Id, string Name, string? Institution, int? Hours, DateOnly? CompletedAt, CourseType Type)
{
    public static CourseDto From(Course c) => new(c.Id, c.Name, c.Institution, c.Hours, c.CompletedAt, c.Type);
}

public sealed record SkillDto(Guid Id, string Name, SkillLevel? Level)
{
    public static SkillDto From(Skill s) => new(s.Id, s.Name, s.Level);
}

public sealed record LanguageDto(Guid Id, string Name, LanguageProficiency Proficiency)
{
    public static LanguageDto From(Language l) => new(l.Id, l.Name, l.Proficiency);
}

/// <summary>Perfil Mestre completo (só para a plataforma web — a extensão recebe apenas valores por campo, RNF-02).</summary>
public sealed record ProfileDto
{
    public Guid Id { get; init; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public string? ZipCode { get; init; }
    public string? LinkedinUrl { get; init; }
    public string? GithubUrl { get; init; }
    public string? PortfolioUrl { get; init; }
    public string? Headline { get; init; }
    public string? Summary { get; init; }
    public string? DesiredRoles { get; init; }
    public string? Areas { get; init; }
    public string? Seniority { get; init; }
    public WorkModel? WorkModel { get; init; }
    public string? LocationPreference { get; init; }
    public string? Availability { get; init; }
    public decimal? SalaryExpectation { get; init; }
    public bool? AvailableToTravel { get; init; }
    public bool? AvailableToRelocate { get; init; }
    public string? ContractTypes { get; init; }
    public bool? HasDriverLicense { get; init; }
    public string? DriverLicenseCategory { get; init; }
    public string? WorkArea { get; init; }
    public string? ProfessionalRegistration { get; init; }
    public string? SalaryType { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<ExperienceDto> Experiences { get; init; } = Array.Empty<ExperienceDto>();
    public IReadOnlyList<EducationDto> Educations { get; init; } = Array.Empty<EducationDto>();
    public IReadOnlyList<CourseDto> Courses { get; init; } = Array.Empty<CourseDto>();
    public IReadOnlyList<SkillDto> Skills { get; init; } = Array.Empty<SkillDto>();
    public IReadOnlyList<LanguageDto> Languages { get; init; } = Array.Empty<LanguageDto>();
    public IReadOnlyList<CustomFieldDto> CustomFields { get; init; } = Array.Empty<CustomFieldDto>();

    public static ProfileDto From(Profile p) => new()
    {
        Id = p.Id,
        FullName = p.FullName,
        Email = p.Email,
        Phone = p.Phone,
        BirthDate = p.BirthDate,
        City = p.City,
        State = p.State,
        Country = p.Country,
        ZipCode = p.ZipCode,
        LinkedinUrl = p.LinkedinUrl,
        GithubUrl = p.GithubUrl,
        PortfolioUrl = p.PortfolioUrl,
        Headline = p.Headline,
        Summary = p.Summary,
        DesiredRoles = p.DesiredRoles,
        Areas = p.Areas,
        Seniority = p.Seniority,
        WorkModel = p.WorkModel,
        LocationPreference = p.LocationPreference,
        Availability = p.Availability,
        SalaryExpectation = p.SalaryExpectation,
        AvailableToTravel = p.AvailableToTravel,
        AvailableToRelocate = p.AvailableToRelocate,
        ContractTypes = p.ContractTypes,
        HasDriverLicense = p.HasDriverLicense,
        DriverLicenseCategory = p.DriverLicenseCategory,
        WorkArea = p.WorkArea,
        ProfessionalRegistration = p.ProfessionalRegistration,
        SalaryType = p.SalaryType,
        UpdatedAt = p.UpdatedAt,
        Experiences = ProfileFacts.OrderedExperiences(p).Select(ExperienceDto.From).ToList(),
        Educations = ProfileFacts.OrderedEducations(p).Select(EducationDto.From).ToList(),
        Courses = p.Courses.OrderBy(c => c.Type).ThenByDescending(c => c.CompletedAt ?? DateOnly.MinValue).Select(CourseDto.From).ToList(),
        Skills = p.Skills.OrderBy(s => s.Name).Select(SkillDto.From).ToList(),
        Languages = ProfileFacts.OrderedLanguages(p).Select(LanguageDto.From).ToList(),
        CustomFields = ProfileFacts.OrderedCustomFields(p).Select(CustomFieldDto.From).ToList(),
    };
}

/// <summary>Pergunta curta sobre dado que falta (RF-05).</summary>
public sealed record MissingItem(string Key, string Question);
