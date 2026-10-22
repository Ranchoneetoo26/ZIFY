using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Answers;
using Zyft.Domain.Applications;
using Zyft.Domain.Audit;
using Zyft.Domain.Billing;
using Zyft.Domain.Consents;
using Zyft.Domain.Onboarding;
using Zyft.Domain.Profiles;
using Zyft.Domain.Users;

namespace Zyft.Infrastructure.Persistence;

/// <summary>
/// Contas de demonstração (só em Development, <c>Seed:DemoUser</c>). Dados 100% fictícios, para os vídeos:
/// Rafael Moreira (Tecnologia, perfil completo) e Beatriz Lima (Saúde, onboarding com passos pulados).
/// </summary>
internal static class DemoSeed
{
    public static async Task SeedAsync(ZyftDbContext db, IPasswordHasher hasher, ILogger logger, string password, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(u => u.Email == DatabaseInitializer.DemoEmail, ct))
        {
            await Rafael(db, hasher, password, ct);
            logger.LogInformation("Usuário de demonstração (Tecnologia) criado");
        }

        if (!await db.Users.AnyAsync(u => u.Email == DatabaseInitializer.DemoHealthEmail, ct))
        {
            await Beatriz(db, hasher, password, ct);
            logger.LogInformation("Usuário de demonstração (Saúde) criado");
        }
    }

    private static async Task Rafael(ZyftDbContext db, IPasswordHasher hasher, string password, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var user = User.Create(DatabaseInitializer.DemoEmail, "Rafael Moreira", hasher.Hash(password));
        db.Users.Add(user);
        db.Consents.Add(Consent.Create(user.Id, ConsentPurpose.AiText, true));
        db.Consents.Add(Consent.Create(user.Id, ConsentPurpose.JobAnalysis, true));
        db.Consents.Add(Consent.Create(user.Id, ConsentPurpose.SensitiveData, false));

        var p = Profile.CreateFor(user.Id);
        p.UpdateBasics(new ProfileBasicData
        {
            FullName = "Rafael Moreira", Email = "rafael.moreira@email.com", Phone = "(41) 99876-5432", BirthDate = new DateOnly(1996, 8, 22),
            City = "Curitiba", State = "PR", Country = "Brasil", ZipCode = "80010-000",
            LinkedinUrl = "https://www.linkedin.com/in/rafaelmoreira-exemplo", GithubUrl = "https://github.com/rafaelmoreira-exemplo",
            PortfolioUrl = "https://rafaelmoreira.exemplo.dev",
            Headline = "Desenvolvedor back-end .NET com foco em APIs e dados",
            Summary = "Desenvolvedor com 5 anos de experiência em C#/.NET, APIs REST e bancos SQL. Trabalhei em fintech e varejo, " +
                      "com integrações de pagamento, filas e testes automatizados. Gosto de transformar processos manuais em sistemas simples de manter.",
            DesiredRoles = "Desenvolvimento, Dados", Areas = "Fintech, Varejo, Agro", Seniority = "Pleno ou analista",
            WorkModel = WorkModel.Remote, LocationPreference = "Remoto ou híbrido em Curitiba", Availability = "Em 30 dias",
            SalaryExpectation = 9500m, SalaryType = "CLT (bruto)", AvailableToTravel = true, AvailableToRelocate = false,
            ContractTypes = "CLT, PJ", HasDriverLicense = true, DriverLicenseCategory = "B", WorkArea = "Tecnologia",
        });
        p.AddExperience(new ExperienceData("PagFácil Pagamentos", "Desenvolvedor back-end pleno", new DateOnly(2023, 3, 1), null, true,
            "Manutenção e evolução de APIs REST em .NET 8 para conciliação de pagamentos; integração com adquirentes via filas (RabbitMQ); revisão de código e plantão de sustentação.",
            "C#, .NET 8, PostgreSQL, RabbitMQ, Docker, GitHub Actions",
            "Reduzi o tempo da conciliação diária de 3 horas para 20 minutos reescrevendo o processamento em lotes."));
        p.AddExperience(new ExperienceData("Rede Varejo Sul", "Desenvolvedor júnior", new DateOnly(2021, 1, 1), new DateOnly(2023, 2, 1), false,
            "Desenvolvimento de relatórios e APIs internas para estoque e vendas; suporte a usuários das lojas; automação de cargas de dados.",
            "C#, ASP.NET Core, SQL Server, Power BI",
            "Automatizei o relatório semanal de ruptura de estoque usado por 40 lojas."));
        p.AddExperience(new ExperienceData("Universidade Federal do Paraná", "Estagiário de suporte técnico", new DateOnly(2019, 3, 1), new DateOnly(2020, 12, 1), false,
            "Atendimento a usuários, manutenção de laboratórios e scripts de inventário.", "Windows Server, PowerShell, Python",
            "Criei um script de inventário que substituiu a planilha manual dos laboratórios."));
        p.AddEducation(new EducationData("Universidade Federal do Paraná", "Tecnologia em Análise e Desenvolvimento de Sistemas", EducationLevel.Undergraduate, EducationStatus.Completed, new DateOnly(2017, 2, 1), new DateOnly(2020, 12, 1)));
        p.AddEducation(new EducationData("PUCPR", "Especialização em Engenharia de Dados", EducationLevel.Postgraduate, EducationStatus.InProgress, new DateOnly(2025, 3, 1), new DateOnly(2026, 12, 1)));
        p.AddCourse(new CourseData("Arquitetura de microsserviços com .NET", "Alura", 40, new DateOnly(2024, 5, 1)));
        p.AddCourse(new CourseData("Docker e Kubernetes na prática", "Udemy", 24, new DateOnly(2023, 9, 1)));
        p.AddCourse(new CourseData("Microsoft Certified: Azure Developer Associate (AZ-204)", "Microsoft", null, new DateOnly(2024, 11, 1), CourseType.Certification));
        foreach (var (s, l) in new (string, SkillLevel?)[]
                 {
                     ("C#", SkillLevel.Advanced), (".NET", SkillLevel.Advanced), ("APIs", SkillLevel.Advanced), ("SQL", SkillLevel.Advanced),
                     ("Banco de dados", SkillLevel.Intermediate), ("Docker", SkillLevel.Intermediate), ("Testes", SkillLevel.Intermediate),
                     ("Metodologias ágeis", SkillLevel.Intermediate), ("Git", SkillLevel.Advanced), ("Python", SkillLevel.Basic),
                 })
            p.AddSkill(s, l);
        p.AddLanguage("Português", LanguageProficiency.Native);
        p.AddLanguage("Inglês", LanguageProficiency.Advanced);
        p.AddLanguage("Espanhol", LanguageProficiency.Basic);
        p.AddCustomField("Stack principal", "C# / .NET 8, PostgreSQL e RabbitMQ");
        p.AddCustomField("Disponível para sobreaviso", "Sim, uma semana por mês");
        db.Profiles.Add(p);

        db.ApprovedAnswers.Add(ApprovedAnswer.Approve(user.Id, "Por que você quer trabalhar conosco?",
            "Quero continuar construindo APIs e integrações que resolvem problemas reais de negócio, em um time que valoriza testes e código simples de manter.", now.AddDays(-40)));
        db.ApprovedAnswers.Add(ApprovedAnswer.Approve(user.Id, "Conte sobre um desafio técnico que você resolveu.",
            "Na PagFácil, a conciliação diária levava 3 horas. Reescrevi o processamento em lotes com filas e reduzi o tempo para 20 minutos, sem mudar o contrato das APIs.", now.AddDays(-35)));
        db.ApprovedAnswers.Add(ApprovedAnswer.Approve(user.Id, "Qual é a sua pretensão salarial?",
            "Minha pretensão é de R$ 9.500 (CLT, bruto), aberta a conversar conforme benefícios.", now.AddDays(-20)));
        db.ApprovedAnswers.Add(ApprovedAnswer.Approve(user.Id, "Você tem experiência com trabalho remoto?",
            "Sim. Trabalho remoto desde 2023, com rituais ágeis, documentação assíncrona e plantões combinados com o time.", now.AddDays(-10)));

        var apps = new (string Portal, string Title, string Company, int Days, ApplicationStatus Status, string? Notes, int Fields)[]
        {
            ("vagas.exemplo.com", "Desenvolvedor .NET Pleno", "Fintech Aurora", 2, ApplicationStatus.Confirmed, null, 18),
            ("carreiras.exemplo.com", "Engenheiro de Software Back-end", "Agro Conecta", 6, ApplicationStatus.InReview, "Retorno prometido em até 10 dias.", 24),
            ("vagas.exemplo.com", "Desenvolvedor C# (Remoto)", "LogiTrack", 12, ApplicationStatus.Interview, "Entrevista técnica marcada.", 21),
            ("talentos.exemplo.com", "Analista de Dados Jr.", "Mercado Bom Preço", 20, ApplicationStatus.Test, "Teste prático de SQL enviado.", 15),
            ("vagas.exemplo.com", "Desenvolvedor Back-end", "Saúde Digital Ltda.", 33, ApplicationStatus.Rejected, null, 19),
            ("carreiras.exemplo.com", "Desenvolvedor .NET Sênior", "Banco Horizonte", 45, ApplicationStatus.NoResponse, null, 26),
        };
        foreach (var a in apps)
        {
            var app = JobApplication.Confirm(user.Id, a.Portal, a.Title, a.Company, $"https://{a.Portal}/vaga/{1000 + (a.Days * 37)}", a.Fields, now.AddDays(-a.Days));
            app.Update(a.Status, a.Notes);
            db.Applications.Add(app);
        }

        var onboarding = OnboardingProgress.Start(user.Id);
        onboarding.Update(OnboardingProgress.Steps, Array.Empty<string>(), "combinados", true, now.AddDays(-60));
        db.OnboardingProgress.Add(onboarding);

        var usage = MonthlyUsage.Start(user.Id, PlanRules.MonthOf(now));
        for (var i = 0; i < 7; i++) usage.Consume(UsageResource.AssistedApplications, PlanRules.MonthlyLimit(PlanKind.Free, UsageResource.AssistedApplications), Array.Empty<UsagePack>(), now);
        for (var i = 0; i < 2; i++) usage.Consume(UsageResource.AiSuggestions, PlanRules.MonthlyLimit(PlanKind.Free, UsageResource.AiSuggestions), Array.Empty<UsagePack>(), now);
        for (var i = 0; i < 4; i++) usage.Consume(UsageResource.JobAnalyses, PlanRules.MonthlyLimit(PlanKind.Free, UsageResource.JobAnalyses), Array.Empty<UsagePack>(), now);
        usage.Consume(UsageResource.ResumeImports, PlanRules.MonthlyLimit(PlanKind.Free, UsageResource.ResumeImports), Array.Empty<UsagePack>(), now);
        db.MonthlyUsages.Add(usage);
        db.Subscriptions.Add(Subscription.StartFree(user.Id));

        var events = new (int Days, string Action, string? Portal, int? Count)[]
        {
            (60, AuditActions.AccountCreated, null, null), (60, AuditActions.ConsentChanged, null, null), (60, AuditActions.OnboardingCompleted, null, 26),
            (59, AuditActions.ResumeImported, null, null), (59, AuditActions.ResumeApplied, null, null), (58, AuditActions.ProfileUpdated, null, null),
            (55, AuditActions.CustomFieldChanged, null, null), (50, AuditActions.DevicePaired, null, null),
            (45, AuditActions.FormMapped, "carreiras.exemplo.com", 28), (45, AuditActions.ApplicationConfirmed, "carreiras.exemplo.com", 26),
            (33, AuditActions.JobAnalyzed, "vagas.exemplo.com", 8), (33, AuditActions.FormMapped, "vagas.exemplo.com", 22), (33, AuditActions.ApplicationConfirmed, "vagas.exemplo.com", 19),
            (20, AuditActions.FormMapped, "talentos.exemplo.com", 17), (20, AuditActions.OpenAnswerSuggested, "talentos.exemplo.com", null), (20, AuditActions.ApplicationConfirmed, "talentos.exemplo.com", 15),
            (14, AuditActions.AdHidden, null, null), (12, AuditActions.JobAnalyzed, "vagas.exemplo.com", 7), (12, AuditActions.FormMapped, "vagas.exemplo.com", 23),
            (12, AuditActions.ApplicationConfirmed, "vagas.exemplo.com", 21), (6, AuditActions.FormMapped, "carreiras.exemplo.com", 25),
            (6, AuditActions.ApplicationConfirmed, "carreiras.exemplo.com", 24), (2, AuditActions.JobAnalyzed, "vagas.exemplo.com", 9),
            (2, AuditActions.FormMapped, "vagas.exemplo.com", 20), (2, AuditActions.ApplicationConfirmed, "vagas.exemplo.com", 18),
        };
        foreach (var e in events) db.AuditEvents.Add(AuditEvent.Record(user.Id, e.Action, now.AddDays(-e.Days), e.Portal, e.Count));
        await db.SaveChangesAsync(ct);
    }

    private static async Task Beatriz(ZyftDbContext db, IPasswordHasher hasher, string password, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var user = User.Create(DatabaseInitializer.DemoHealthEmail, "Beatriz Lima", hasher.Hash(password));
        db.Users.Add(user);
        db.Consents.Add(Consent.Create(user.Id, ConsentPurpose.AiText, true));
        db.Consents.Add(Consent.Create(user.Id, ConsentPurpose.JobAnalysis, true));
        db.Consents.Add(Consent.Create(user.Id, ConsentPurpose.SensitiveData, false));

        // Mesmos dados do mockup BoasVindas (área Saúde). Links, pretensão salarial e cursos ficaram para depois.
        var p = Profile.CreateFor(user.Id);
        p.UpdateBasics(new ProfileBasicData
        {
            FullName = "Beatriz Lima", Email = "beatriz.lima@email.com", Phone = "(31) 98877-1020", BirthDate = new DateOnly(2001, 5, 2),
            City = "Belo Horizonte", State = "MG", Country = "Brasil", ZipCode = "30140-000",
            WorkArea = "Saúde", ProfessionalRegistration = "COREN-MG 123456",
            DesiredRoles = "Técnico(a) de enfermagem", Areas = "Hospital, Clínica", Seniority = "Júnior ou assistente",
            WorkModel = WorkModel.OnSite, LocationPreference = "Belo Horizonte e região", Availability = "Imediatamente",
            AvailableToTravel = false, ContractTypes = "CLT, Plantão / escala", HasDriverLicense = true, DriverLicenseCategory = "B",
        });
        p.AddExperience(new ExperienceData("Hospital Santa Clara", "Técnica de enfermagem", new DateOnly(2023, 3, 1), null, true,
            "Atendimento na triagem e apoio à equipe do plantão noturno.", "Prontuário eletrônico",
            "Organizei a escala de materiais e acabamos com a falta de insumos no fim do mês."));
        p.AddEducation(new EducationData("Senac MG", "Técnico em Enfermagem", EducationLevel.Technical, EducationStatus.Completed, new DateOnly(2020, 2, 1), new DateOnly(2022, 7, 1)));
        p.AddSkill("Atendimento humanizado", null);
        p.AddSkill("Administração de medicamentos", null);
        p.AddSkill("Coleta de sangue", null);
        p.AddLanguage("Português", LanguageProficiency.Native);
        p.AddCustomField("Disponível para plantão noturno", "Sim, escala 12x36");
        p.AddCustomField("Experiência em UTI", "Estágio de 6 meses em UTI adulto");
        db.Profiles.Add(p);

        // Passos respondidos até "competencias"; links, salário e cursos pulados ("Preencher mais tarde").
        var steps = OnboardingProgress.Steps;
        var upTo = steps.ToList().IndexOf("competencias");
        var skipped = new[] { "links", "salario", "cursos" };
        var answered = steps.Take(upTo).Where(s => !skipped.Contains(s));
        var onboarding = OnboardingProgress.Start(user.Id);
        onboarding.Update(answered, skipped, "competencias", false, now.AddDays(-1));
        db.OnboardingProgress.Add(onboarding);

        var usage = MonthlyUsage.Start(user.Id, PlanRules.MonthOf(now));
        for (var i = 0; i < 2; i++) usage.Consume(UsageResource.AssistedApplications, PlanRules.MonthlyLimit(PlanKind.Free, UsageResource.AssistedApplications), Array.Empty<UsagePack>(), now);
        db.MonthlyUsages.Add(usage);

        foreach (var (days, action) in new[] { (2, AuditActions.AccountCreated), (2, AuditActions.ConsentChanged), (1, AuditActions.ProfileUpdated), (1, AuditActions.CustomFieldChanged) })
            db.AuditEvents.Add(AuditEvent.Record(user.Id, action, now.AddDays(-days)));
        await db.SaveChangesAsync(ct);
    }
}
