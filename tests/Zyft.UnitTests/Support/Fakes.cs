using System.Linq.Expressions;
using System.Text.Json;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Common;
using Zyft.Domain.Profiles;

namespace Zyft.UnitTests.Support;

/// <summary>Repositório em memória que executa as mesmas expressões dos handlers.</summary>
public sealed class InMemoryRepository<T> : IRepository<T> where T : Entity
{
    public List<T> Items { get; } = new();

    public Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(predicate.Compile()));
    public Task<List<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) => Task.FromResult(Items.Where(predicate.Compile()).ToList());
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) => Task.FromResult(Items.Any(predicate.Compile()));
    public void Add(T entity) => Items.Add(entity);
    public void AddRange(IEnumerable<T> entities) => Items.AddRange(entities);
    public void Remove(T entity) => Items.Remove(entity);
    public void RemoveRange(IEnumerable<T> entities) { foreach (var e in entities.ToList()) Items.Remove(e); }
}

public sealed class InMemoryProfileRepository : IProfileRepository
{
    public List<Profile> Items { get; } = new();
    public Task<Profile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(p => p.UserId == userId));
    public void Add(Profile profile) => Items.Add(profile);
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken ct = default) { Saves++; return Task.FromResult(1); }
}

public sealed class FixedClock(DateTime now) : IClock
{
    public DateTime UtcNow { get; set; } = now;
}

public sealed class FakeCurrentUser(Guid id, string scope = "web") : ICurrentUser
{
    public Guid? UserIdOrNull => id;
    public Guid UserId => id;
    public string? Scope => scope;
    public Guid? DeviceId => null;
}

/// <summary>IA de mentira: devolve respostas JSON programadas na ordem e guarda os prompts recebidos.</summary>
public sealed class ScriptedLlm : ILlmClient
{
    private readonly Queue<string> _responses;

    public ScriptedLlm(params string[] responses) => _responses = new Queue<string>(responses);

    public List<LlmJsonRequest> Requests { get; } = new();
    public bool IsEnabled { get; init; } = true;
    public bool Fail { get; init; }

    public Task<JsonElement> CompleteJsonAsync(LlmJsonRequest request, CancellationToken ct = default)
    {
        Requests.Add(request);
        if (Fail || _responses.Count == 0) throw new LlmUnavailableException("offline");
        return Task.FromResult(JsonDocument.Parse(_responses.Dequeue()).RootElement.Clone());
    }
}

public static class Profiles
{
    /// <summary>Perfil de exemplo (mesmo do protótipo: server/examples/perfil-exemplo.json).</summary>
    public static Profile Ana()
    {
        var p = Profile.CreateFor(Guid.NewGuid());
        p.UpdateBasics(new ProfileBasicData
        {
            FullName = "Ana Paula Souza", Email = "ana.souza@email.com", Phone = "(44) 99999-2222", BirthDate = new DateOnly(1998, 3, 14),
            City = "Maringá", State = "PR", ZipCode = "87000-000", Summary = "Profissional administrativa com 4 anos de experiência.",
            DesiredRoles = "Assistente Administrativo", WorkModel = WorkModel.OnSite, Availability = "Imediata", SalaryExpectation = 2500m,
            AvailableToTravel = true, AvailableToRelocate = false, ContractTypes = "CLT", HasDriverLicense = true, DriverLicenseCategory = "B",
        });
        p.AddExperience(new ExperienceData("Empresa Y Logística", "Assistente Administrativo", new DateOnly(2023, 7, 1), null, true,
            "Lançamento de notas fiscais no ERP Totvs Protheus; controle de contas a pagar", "Excel, Totvs Protheus", "Reduzi em 30% o tempo de conciliação"));
        p.AddExperience(new ExperienceData("Empresa X Comércio", "Auxiliar Administrativo", new DateOnly(2021, 2, 1), new DateOnly(2023, 6, 1), false,
            "Atendimento ao cliente no balcão", "Excel", "Digitalizei 1.200 contratos"));
        p.AddEducation(new EducationData("Universidade Estadual de Maringá", "Administração", EducationLevel.Undergraduate, EducationStatus.InProgress, new DateOnly(2022, 2, 1), null));
        p.AddCourse(new CourseData("Excel Intermediário", "SENAC", 40, new DateOnly(2024, 6, 1)));
        p.AddSkill("Excel intermediário", null);
        p.AddSkill("Contas a pagar", null);
        p.AddLanguage("Inglês", LanguageProficiency.Basic);
        return p;
    }
}
