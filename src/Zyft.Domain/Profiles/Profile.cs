using Zyft.Domain.Common;

namespace Zyft.Domain.Profiles;

/// <summary>
/// Perfil Profissional Mestre (RF-02). Raiz de agregado: experiências, formações, cursos,
/// competências e idiomas só mudam por métodos daqui, que garantem as invariantes.
/// Dados sensíveis NÃO ficam aqui (RN-04, ver <c>SensitiveData</c>).
/// </summary>
public sealed class Profile : Entity
{
    private readonly List<Experience> _experiences = new();
    private readonly List<Education> _educations = new();
    private readonly List<Course> _courses = new();
    private readonly List<Skill> _skills = new();
    private readonly List<Language> _languages = new();
    private readonly List<CustomField> _customFields = new();

    /// <summary>Tipos de pretensão salarial aceitos.</summary>
    public static readonly IReadOnlyList<string> SalaryTypes = new[] { "CLT (bruto)", "PJ", "Por hora", "Por plantão" };

    /// <summary>Máximo de campos personalizados por perfil.</summary>
    public const int MaxCustomFields = 50;

    private Profile() { }

    public Guid UserId { get; private set; }

    public string? FullName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? Country { get; private set; }
    public string? ZipCode { get; private set; }
    public string? LinkedinUrl { get; private set; }
    public string? GithubUrl { get; private set; }
    public string? PortfolioUrl { get; private set; }
    public string? Headline { get; private set; }
    public string? Summary { get; private set; }
    public string? DesiredRoles { get; private set; }
    public string? Areas { get; private set; }
    public string? Seniority { get; private set; }
    public WorkModel? WorkModel { get; private set; }
    public string? LocationPreference { get; private set; }
    public string? Availability { get; private set; }
    public decimal? SalaryExpectation { get; private set; }
    public bool? AvailableToTravel { get; private set; }
    public bool? AvailableToRelocate { get; private set; }
    public string? ContractTypes { get; private set; }
    public bool? HasDriverLicense { get; private set; }
    public string? DriverLicenseCategory { get; private set; }
    public string? WorkArea { get; private set; }
    public string? ProfessionalRegistration { get; private set; }
    public string? SalaryType { get; private set; }

    public IReadOnlyCollection<Experience> Experiences => _experiences.AsReadOnly();
    public IReadOnlyCollection<Education> Educations => _educations.AsReadOnly();
    public IReadOnlyCollection<Course> Courses => _courses.AsReadOnly();
    public IReadOnlyCollection<Skill> Skills => _skills.AsReadOnly();
    public IReadOnlyCollection<Language> Languages => _languages.AsReadOnly();
    public IReadOnlyCollection<CustomField> CustomFields => _customFields.AsReadOnly();

    public static Profile CreateFor(Guid userId, string? fullName = null, string? email = null) => new()
    {
        UserId = userId,
        FullName = Guard.Optional(fullName),
        Email = Guard.Optional(email),
    };

    public void UpdateBasics(ProfileBasicData d)
    {
        if (d.SalaryExpectation is < 0)
            throw new DomainException("profile.invalid_salary", "A pretensão salarial não pode ser negativa.");
        if (d.HasDriverLicense == false && !string.IsNullOrWhiteSpace(d.DriverLicenseCategory))
            throw new DomainException("profile.invalid_driver_license", "Informe a categoria da CNH só se você possui CNH.");
        var salaryType = Guard.Optional(d.SalaryType);
        if (salaryType is not null)
        {
            salaryType = SalaryTypes.FirstOrDefault(t => string.Equals(t, salaryType, StringComparison.OrdinalIgnoreCase))
                ?? throw new DomainException("profile.invalid_salary_type", $"Tipo de pretensão inválido. Use: {string.Join(", ", SalaryTypes)}.");
        }

        FullName = Guard.Optional(d.FullName);
        Email = Guard.Optional(d.Email)?.ToLowerInvariant();
        Phone = Guard.Optional(d.Phone);
        BirthDate = d.BirthDate;
        City = Guard.Optional(d.City);
        State = Guard.Optional(d.State);
        Country = Guard.Optional(d.Country);
        ZipCode = Guard.Optional(d.ZipCode);
        LinkedinUrl = Guard.Optional(d.LinkedinUrl);
        GithubUrl = Guard.Optional(d.GithubUrl);
        PortfolioUrl = Guard.Optional(d.PortfolioUrl);
        Headline = Guard.Optional(d.Headline);
        Summary = Guard.Optional(d.Summary);
        DesiredRoles = Guard.Optional(d.DesiredRoles);
        Areas = Guard.Optional(d.Areas);
        Seniority = Guard.Optional(d.Seniority);
        WorkModel = d.WorkModel;
        LocationPreference = Guard.Optional(d.LocationPreference);
        Availability = Guard.Optional(d.Availability);
        SalaryExpectation = d.SalaryExpectation;
        AvailableToTravel = d.AvailableToTravel;
        AvailableToRelocate = d.AvailableToRelocate;
        ContractTypes = Guard.Optional(d.ContractTypes);
        HasDriverLicense = d.HasDriverLicense;
        DriverLicenseCategory = d.HasDriverLicense == true ? Guard.Optional(d.DriverLicenseCategory)?.ToUpperInvariant() : null;
        WorkArea = Guard.Optional(d.WorkArea);
        ProfessionalRegistration = Guard.Optional(d.ProfessionalRegistration);
        SalaryType = salaryType;
    }

    // ---------- Campos personalizados (nome único por perfil; rótulos sensíveis bloqueados) ----------
    public CustomField AddCustomField(string label, string value)
    {
        if (_customFields.Count >= MaxCustomFields)
            throw new DomainException("profile.custom_field_limit", $"Você pode ter até {MaxCustomFields} campos personalizados.");
        EnsureUniqueCustomField(label, null);
        var f = CustomField.Create(Id, label, value);
        _customFields.Add(f);
        return f;
    }

    public CustomField? UpdateCustomField(Guid id, string label, string value)
    {
        var f = _customFields.FirstOrDefault(x => x.Id == id);
        if (f is null) return null;
        EnsureUniqueCustomField(label, id);
        f.Update(label, value);
        return f;
    }

    public bool RemoveCustomField(Guid id) => _customFields.RemoveAll(x => x.Id == id) > 0;

    private void EnsureUniqueCustomField(string label, Guid? ignoreId)
    {
        if (_customFields.Any(f => f.Id != ignoreId && string.Equals(f.Label, label?.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("profile.custom_field_duplicated", $"Já existe um campo chamado \"{label?.Trim()}\".");
    }

    // ---------- Experiências ----------
    public Experience AddExperience(ExperienceData data)
    {
        var e = Experience.Create(Id, data);
        _experiences.Add(e);
        return e;
    }

    public Experience? UpdateExperience(Guid id, ExperienceData data)
    {
        var e = _experiences.FirstOrDefault(x => x.Id == id);
        e?.Update(data);
        return e;
    }

    public bool RemoveExperience(Guid id) => _experiences.RemoveAll(x => x.Id == id) > 0;

    // ---------- Formação ----------
    public Education AddEducation(EducationData data)
    {
        var e = Education.Create(Id, data);
        _educations.Add(e);
        return e;
    }

    public Education? UpdateEducation(Guid id, EducationData data)
    {
        var e = _educations.FirstOrDefault(x => x.Id == id);
        e?.Update(data);
        return e;
    }

    public bool RemoveEducation(Guid id) => _educations.RemoveAll(x => x.Id == id) > 0;

    // ---------- Cursos e certificações ----------
    public Course AddCourse(CourseData data)
    {
        var c = Course.Create(Id, data);
        _courses.Add(c);
        return c;
    }

    public Course? UpdateCourse(Guid id, CourseData data)
    {
        var c = _courses.FirstOrDefault(x => x.Id == id);
        c?.Update(data);
        return c;
    }

    public bool RemoveCourse(Guid id) => _courses.RemoveAll(x => x.Id == id) > 0;

    // ---------- Competências (nome único por perfil) ----------
    public Skill AddSkill(string name, SkillLevel? level)
    {
        EnsureUniqueSkill(name, null);
        var s = Skill.Create(Id, name, level);
        _skills.Add(s);
        return s;
    }

    public Skill? UpdateSkill(Guid id, string name, SkillLevel? level)
    {
        var s = _skills.FirstOrDefault(x => x.Id == id);
        if (s is null) return null;
        EnsureUniqueSkill(name, id);
        s.Update(name, level);
        return s;
    }

    public bool RemoveSkill(Guid id) => _skills.RemoveAll(x => x.Id == id) > 0;

    public bool HasSkill(string name) =>
        _skills.Any(s => string.Equals(s.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase));

    private void EnsureUniqueSkill(string name, Guid? ignoreId)
    {
        if (_skills.Any(s => s.Id != ignoreId && string.Equals(s.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("skill.duplicated", $"A competência \"{name?.Trim()}\" já está no perfil.");
    }

    // ---------- Idiomas (um por nome) ----------
    public Language AddLanguage(string name, LanguageProficiency proficiency)
    {
        EnsureUniqueLanguage(name, null);
        var l = Language.Create(Id, name, proficiency);
        _languages.Add(l);
        return l;
    }

    public Language? UpdateLanguage(Guid id, string name, LanguageProficiency proficiency)
    {
        var l = _languages.FirstOrDefault(x => x.Id == id);
        if (l is null) return null;
        EnsureUniqueLanguage(name, id);
        l.Update(name, proficiency);
        return l;
    }

    public bool RemoveLanguage(Guid id) => _languages.RemoveAll(x => x.Id == id) > 0;

    private void EnsureUniqueLanguage(string name, Guid? ignoreId)
    {
        if (_languages.Any(s => s.Id != ignoreId && string.Equals(s.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("language.duplicated", $"O idioma \"{name?.Trim()}\" já está no perfil.");
    }
}
