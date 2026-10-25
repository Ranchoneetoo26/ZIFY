using Zyft.Domain.Common;

namespace Zyft.Domain.Profiles;

public sealed record EducationData(
    string Institution,
    string Course,
    EducationLevel Level,
    EducationStatus Status,
    DateOnly StartDate,
    DateOnly? EndDate);

public sealed class Education : Entity
{
    private Education() { }

    public Guid ProfileId { get; private set; }
    public string Institution { get; private set; } = default!;
    public string Course { get; private set; } = default!;
    public EducationLevel Level { get; private set; }
    public EducationStatus Status { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    internal static Education Create(Guid profileId, EducationData d)
    {
        var e = new Education { ProfileId = profileId };
        e.Update(d);
        return e;
    }

    internal void Update(EducationData d)
    {
        if (d.EndDate is { } end && end < d.StartDate)
            throw new DomainException("education.invalid_period", "A data de conclusão não pode ser anterior à data de início.");

        Institution = Guard.Required(d.Institution, "education.institution_required", "Informe a instituição.");
        Course = Guard.Required(d.Course, "education.course_required", "Informe o curso.");
        Level = d.Level;
        Status = d.Status;
        StartDate = d.StartDate;
        EndDate = d.EndDate;
    }
}
