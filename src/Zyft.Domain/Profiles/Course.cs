using Zyft.Domain.Common;

namespace Zyft.Domain.Profiles;

public sealed record CourseData(string Name, string? Institution, int? Hours, DateOnly? CompletedAt, CourseType Type = CourseType.Course);

/// <summary>Curso livre/complementar ou certificação (distinguidos por <see cref="Type"/>).</summary>
public sealed class Course : Entity
{
    private Course() { }

    public Guid ProfileId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Institution { get; private set; }
    public int? Hours { get; private set; }
    public DateOnly? CompletedAt { get; private set; }
    public CourseType Type { get; private set; }

    internal static Course Create(Guid profileId, CourseData d)
    {
        var c = new Course { ProfileId = profileId };
        c.Update(d);
        return c;
    }

    internal void Update(CourseData d)
    {
        if (d.Hours is <= 0)
            throw new DomainException("course.invalid_hours", "A carga horária deve ser maior que zero.");
        Name = Guard.Required(d.Name, "course.name_required", "Informe o nome do curso.");
        Institution = Guard.Optional(d.Institution);
        Hours = d.Hours;
        CompletedAt = d.CompletedAt;
        Type = d.Type;
    }
}
