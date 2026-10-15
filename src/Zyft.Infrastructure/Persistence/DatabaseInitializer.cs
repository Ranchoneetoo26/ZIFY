using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Answers;
using Zyft.Domain.Audit;
using Zyft.Domain.Consents;
using Zyft.Domain.Profiles;
using Zyft.Domain.Users;

namespace Zyft.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// PostgreSQL: aplica as migrations (<c>Database.Migrate</c>). SQLite: <c>EnsureCreated</c> — as migrations são
    /// específicas do provedor (tipos, índices) e o SQLite serve só para desenvolvimento rápido e testes.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config, bool seedDemo, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZyftDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Zyft.Database");

        if (db.Database.IsSqlite()) await db.Database.EnsureCreatedAsync(ct);
        else await db.Database.MigrateAsync(ct);
        logger.LogInformation("Banco pronto ({Provider})", db.Database.ProviderName);

        if (seedDemo) await SeedDemoAsync(scope.ServiceProvider, db, logger, ct);
    }

    public const string DemoEmail = "demo@zyft.local";
    public const string DemoPassword = "Zyft@2026";

    /// <summary>Usuário de demonstração (só em Development) com o perfil de exemplo do protótipo (<c>server/examples/perfil-exemplo.json</c>). Dados fictícios.</summary>
    private static async Task SeedDemoAsync(IServiceProvider sp, ZyftDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Email == DemoEmail, ct)) return;
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var now = DateTime.UtcNow;

        var user = User.Create(DemoEmail, "Ana Paula Souza", hasher.Hash(DemoPassword));
        db.Users.Add(user);
        db.Consents.AddRange(Consent.Defaults(user.Id));

        var p = Profile.CreateFor(user.Id);
        p.UpdateBasics(new ProfileBasicData
        {
            FullName = "Ana Paula Souza", Email = "ana.souza@email.com", Phone = "(44) 99999-2222", BirthDate = new DateOnly(1998, 3, 14),
            City = "Maringá", State = "PR", Country = "Brasil", ZipCode = "87000-000", LinkedinUrl = "https://www.linkedin.com/in/anapaulasouza-exemplo",
            Headline = "Assistente Administrativa",
            Summary = "Profissional administrativa com 4 anos de experiência em rotinas de escritório, contas a pagar e atendimento a clientes e fornecedores.",
            DesiredRoles = "Assistente Administrativo, Auxiliar Financeiro", Areas = "Administrativo, Financeiro", Seniority = "Júnior",
            WorkModel = WorkModel.OnSite, LocationPreference = "Maringá e região", Availability = "Imediata", SalaryExpectation = 2500m,
            AvailableToTravel = true, AvailableToRelocate = false, ContractTypes = "CLT", HasDriverLicense = true, DriverLicenseCategory = "B",
        });
        p.AddExperience(new ExperienceData("Empresa Y Logística", "Assistente Administrativo", new DateOnly(2023, 7, 1), null, true,
            "Lançamento de notas fiscais no ERP Totvs Protheus; controle de contas a pagar; atendimento a fornecedores por telefone e e-mail; elaboração de relatórios semanais em Excel.",
            "Excel (PROCV, tabelas dinâmicas), Totvs Protheus, Google Planilhas",
            "Reduzi em 30% o tempo de conciliação de pagamentos criando uma planilha de controle padronizada."));
        p.AddExperience(new ExperienceData("Empresa X Comércio", "Auxiliar Administrativo", new DateOnly(2021, 2, 1), new DateOnly(2023, 6, 1), false,
            "Atendimento ao cliente no balcão e telefone; organização de arquivos físicos e digitais; emissão de boletos.",
            "Excel, sistema próprio", "Digitalizei o arquivo de contratos (cerca de 1.200 documentos)."));
        p.AddEducation(new EducationData("Universidade Estadual de Maringá", "Administração", EducationLevel.Undergraduate, EducationStatus.InProgress, new DateOnly(2022, 2, 1), new DateOnly(2026, 12, 1)));
        p.AddEducation(new EducationData("Colégio Estadual Central", "Ensino Médio", EducationLevel.HighSchool, EducationStatus.Completed, new DateOnly(2013, 2, 1), new DateOnly(2015, 12, 1)));
        p.AddCourse(new CourseData("Excel Intermediário", "SENAC", 40, new DateOnly(2024, 6, 1)));
        foreach (var s in new[] { "Excel intermediário", "Contas a pagar", "Atendimento ao cliente", "Organização de documentos", "Totvs Protheus" }) p.AddSkill(s, null);
        p.AddLanguage("Inglês", LanguageProficiency.Basic);
        db.Profiles.Add(p);
        db.ApprovedAnswers.Add(ApprovedAnswer.Approve(user.Id, "Por que você quer trabalhar conosco?",
            "Busco uma empresa onde eu possa aplicar minha experiência em contas a pagar e atendimento a fornecedores, e continuar crescendo na área administrativa.", now));
        db.AuditEvents.Add(AuditEvent.Record(user.Id, AuditActions.AccountCreated, now));
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Usuário de demonstração criado");
    }
}
