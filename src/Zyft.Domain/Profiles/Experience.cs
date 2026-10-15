using Zyft.Domain.Common;

namespace Zyft.Domain.Profiles;

public sealed record ExperienceData(
    string Company,
    string Role,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    string? Activities,
    string? Tools,
    string? Results);

/// <summary>Experiência profissional. Invariantes: término ≥ início; experiência atual não tem término.</summary>
public sealed class Experience : Entity
{
    private Experience() { }

    public Guid ProfileId { get; private set; }
    public string Company { get; private set; } = default!;
    public string Role { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsCurrent { get; private set; }
    public string? Activities { get; private set; }
    public string? Tools { get; private set; }
    public string? Results { get; private set; }

    internal static Experience Create(Guid profileId, ExperienceData d)
    {
        var e = new Experience { ProfileId = profileId };
        e.Update(d);
        return e;
    }

    internal void Update(ExperienceData d)
    {
        if (d.EndDate is { } end && end < d.StartDate)
            throw new DomainException("experience.invalid_period", "A data de término não pode ser anterior à data de início.");
        if (d.IsCurrent && d.EndDate is not null)
            throw new DomainException("experience.current_with_end", "Uma experiência atual não pode ter data de término.");

        Company = Guard.Required(d.Company, "experience.company_required", "Informe a organização.");
        Role = Guard.Required(d.Role, "experience.role_required", "Informe o cargo.");
        StartDate = d.StartDate;
        EndDate = d.EndDate;
        IsCurrent = d.IsCurrent;
        Activities = Guard.Optional(d.Activities);
        Tools = Guard.Optional(d.Tools);
        Results = Guard.Optional(d.Results);
    }
}
