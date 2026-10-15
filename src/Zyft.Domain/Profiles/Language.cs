using Zyft.Domain.Common;

namespace Zyft.Domain.Profiles;

public sealed class Language : Entity
{
    private Language() { }

    public Guid ProfileId { get; private set; }
    public string Name { get; private set; } = default!;
    public LanguageProficiency Proficiency { get; private set; }

    internal static Language Create(Guid profileId, string name, LanguageProficiency proficiency)
    {
        var l = new Language { ProfileId = profileId };
        l.Update(name, proficiency);
        return l;
    }

    internal void Update(string name, LanguageProficiency proficiency)
    {
        Name = Guard.Required(name, "language.name_required", "Informe o idioma.");
        Proficiency = proficiency;
    }
}
