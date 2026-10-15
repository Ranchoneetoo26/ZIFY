using Zyft.Domain.Common;

namespace Zyft.Domain.Profiles;

public sealed class Skill : Entity
{
    private Skill() { }

    public Guid ProfileId { get; private set; }
    public string Name { get; private set; } = default!;
    public SkillLevel? Level { get; private set; }

    internal static Skill Create(Guid profileId, string name, SkillLevel? level)
    {
        var s = new Skill { ProfileId = profileId };
        s.Update(name, level);
        return s;
    }

    internal void Update(string name, SkillLevel? level)
    {
        Name = Guard.Required(name, "skill.name_required", "Informe a competência.");
        Level = level;
    }
}
