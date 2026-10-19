namespace Zyft.Domain.Profiles;

/// <summary>Dados básicos do Perfil Mestre (contato, objetivo, links e preferências). Nenhum é obrigatório (RF-02).</summary>
public sealed record ProfileBasicData
{
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

    /// <summary>Área escolhida no onboarding (ex.: "Saúde", "Tecnologia").</summary>
    public string? WorkArea { get; init; }

    /// <summary>Registro profissional ou certificação obrigatória (ex.: "COREN-MG 123456", "OAB/MG 123.456").</summary>
    public string? ProfessionalRegistration { get; init; }

    /// <summary>Tipo da pretensão salarial: "CLT (bruto)", "PJ", "Por hora" ou "Por plantão".</summary>
    public string? SalaryType { get; init; }
}
