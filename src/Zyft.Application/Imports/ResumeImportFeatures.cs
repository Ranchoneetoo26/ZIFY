using System.Text.Json;
using FluentValidation;
using MediatR;
using Zyft.Application.Common;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Application.Profiles;
using Zyft.Domain.Audit;
using Zyft.Domain.Common;
using Zyft.Domain.Imports;
using Zyft.Domain.Profiles;

namespace Zyft.Application.Imports;

public sealed record ResumeImportDto(Guid Id, string FileName, ResumeImportStatus Status, DateTime CreatedAt, ResumeProposal? Proposal, bool AiUsed);

internal static class ProposalJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Write(ResumeProposal p, bool aiUsed) => JsonSerializer.Serialize(new Stored(p, aiUsed), Options);

    public static (ResumeProposal? Proposal, bool AiUsed) Read(string json)
    {
        try
        {
            var s = JsonSerializer.Deserialize<Stored>(json, Options);
            return (s?.Proposal, s?.AiUsed ?? false);
        }
        catch (JsonException)
        {
            return (null, false);
        }
    }

    public static ResumeImportDto ToDto(ResumeImport i)
    {
        var (proposal, ai) = Read(i.ProposalJson);
        return new ResumeImportDto(i.Id, i.FileName, i.Status, i.CreatedAt, i.Status == ResumeImportStatus.Pending ? proposal : null, ai);
    }

    private sealed record Stored(ResumeProposal? Proposal, bool AiUsed);
}

// ------------------------------------------------------------------ POST /resume-imports

/// <summary>Upload de currículo PDF/DOCX (≤ 10 MB). O arquivo NÃO é guardado; vira uma proposta para revisão (RF-03).</summary>
public sealed record CreateResumeImportCommand(string FileName, string? ContentType, byte[] Content) : IRequest<ResumeImportDto>;

public sealed class CreateResumeImportValidator : AbstractValidator<CreateResumeImportCommand>
{
    public const long MaxBytes = 10 * 1024 * 1024;

    public CreateResumeImportValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().WithMessage("Envie um arquivo.")
            .Must(n => n.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || n.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Formato não suportado. Envie PDF ou DOCX.");
        RuleFor(x => x.Content).NotNull().Must(c => c is { Length: > 0 }).WithMessage("O arquivo está vazio.")
            .Must(c => c is null || c.LongLength <= MaxBytes).WithMessage("O arquivo deve ter no máximo 10 MB.");
        RuleFor(x => x).Must(HaveMatchingSignature).WithName("file").WithMessage("O conteúdo do arquivo não corresponde à extensão (PDF ou DOCX).")
            .When(x => x.Content is { Length: > 4 });
    }

    /// <summary>Confere a assinatura real do arquivo: %PDF ou ZIP (PK) para DOCX.</summary>
    private static bool HaveMatchingSignature(CreateResumeImportCommand x)
    {
        var c = x.Content;
        if (x.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return c[0] == '%' && c[1] == 'P' && c[2] == 'D' && c[3] == 'F';
        if (x.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)) return c[0] == 'P' && c[1] == 'K';
        return true;
    }
}

public sealed class CreateResumeImportHandler(
    IResumeTextExtractor extractor,
    IResumeParser parser,
    IRepository<ResumeImport> imports,
    IRepository<AuditEvent> audit,
    ICurrentUser current,
    IUnitOfWork uow,
    IClock clock,
    Billing.IUsageMeter meter) : IRequestHandler<CreateResumeImportCommand, ResumeImportDto>
{
    public async Task<ResumeImportDto> Handle(CreateResumeImportCommand cmd, CancellationToken ct)
    {
        await meter.ConsumeAsync(current.UserId, Domain.Billing.UsageResource.ResumeImports, null, ct);
        var text = await extractor.ExtractAsync(cmd.Content, cmd.FileName, ct);
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 20)
            throw new DomainException("import.no_text", "Não encontramos texto no arquivo. PDF escaneado (imagem) não é suportado: envie um PDF com texto ou um DOCX.");

        var (proposal, aiUsed) = await parser.ParseAsync(text, ct);
        var import = ResumeImport.Create(current.UserId, Path.GetFileName(cmd.FileName), ProposalJson.Write(proposal, aiUsed));
        imports.Add(import);
        audit.Add(AuditEvent.Record(current.UserId, AuditActions.ResumeImported, clock.UtcNow));
        await uow.SaveChangesAsync(ct);
        return ProposalJson.ToDto(import);
    }
}

// ------------------------------------------------------------------ GET /resume-imports/{id}

public sealed record GetResumeImportQuery(Guid Id) : IRequest<ResumeImportDto>;

public sealed class GetResumeImportHandler(IRepository<ResumeImport> imports, ICurrentUser current) : IRequestHandler<GetResumeImportQuery, ResumeImportDto>
{
    public async Task<ResumeImportDto> Handle(GetResumeImportQuery q, CancellationToken ct)
    {
        var uid = current.UserId;
        var i = await imports.FindAsync(x => x.Id == q.Id && x.UserId == uid, ct) ?? throw new NotFoundException("Importação não encontrada.");
        return ProposalJson.ToDto(i);
    }
}

// ------------------------------------------------------------------ GET /resume-imports/pending

/// <summary>Importação pendente mais recente da pessoa, para retomar a revisão sem gastar outra importação. Nula se não houver.</summary>
public sealed record GetPendingResumeImportQuery : IRequest<ResumeImportDto?>;

public sealed class GetPendingResumeImportHandler(IRepository<ResumeImport> imports, ICurrentUser current) : IRequestHandler<GetPendingResumeImportQuery, ResumeImportDto?>
{
    public async Task<ResumeImportDto?> Handle(GetPendingResumeImportQuery q, CancellationToken ct)
    {
        var uid = current.UserId;
        var pending = await imports.ListAsync(x => x.UserId == uid && x.Status == ResumeImportStatus.Pending, ct);
        var latest = pending.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        return latest is null ? null : ProposalJson.ToDto(latest);
    }
}

// ------------------------------------------------------------------ POST /resume-imports/{id}/apply

/// <summary>
/// Aplica SÓ os itens aceitos/editados pela pessoa (RF-04). O corpo tem o mesmo formato da proposta, já filtrado:
/// campos pessoais nulos ou vazios não mudam o perfil, e os demais dados básicos ficam como estão; itens de lista são
/// acrescentados (duplicatas são ignoradas e contadas em <c>skipped</c>). Valem as mesmas regras e limites de <c>PUT /profile</c>.
/// </summary>
public sealed record ApplyResumeImportCommand : IRequest<ApplyResumeImportResult>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid Id { get; init; }

    public ProposalProfile? Profile { get; init; }
    public List<ProposalExperience> Experiences { get; init; } = new();
    public List<ProposalEducation> Educations { get; init; } = new();
    public List<ProposalCourse> Courses { get; init; } = new();
    public List<ProposalSkill> Skills { get; init; } = new();
    public List<ProposalLanguage> Languages { get; init; } = new();
}

/// <param name="Profile">Perfil depois de aplicar.</param>
/// <param name="Missing">O que ainda falta no perfil.</param>
/// <param name="Added">Itens de lista gravados ou alterados.</param>
/// <param name="Skipped">Itens de lista ignorados porque já estavam no perfil.</param>
public sealed record ApplyResumeImportResult(ProfileDto Profile, IReadOnlyList<MissingItem> Missing, int Added, int Skipped);

public sealed class ProposalProfileValidator : AbstractValidator<ProposalProfile>
{
    public ProposalProfileValidator()
    {
        Include(new ProfileContactRules<ProposalProfile>(completeUrls: true));
        RuleFor(x => x.FullName).Visible().When(x => x.FullName is not null).WithMessage("Informe o nome completo.");
    }
}

public sealed class ApplyResumeImportValidator : AbstractValidator<ApplyResumeImportCommand>
{
    public ApplyResumeImportValidator()
    {
        RuleFor(x => x.Profile!).SetValidator(new ProposalProfileValidator()).When(x => x.Profile is not null);
        RuleForEach(x => x.Experiences).ChildRules(e =>
        {
            e.RuleFor(x => x.Company).Visible().WithMessage("Informe a organização.").MaximumLength(ProfileLimits.Organization);
            e.RuleFor(x => x.Role).Visible().WithMessage("Informe o cargo.").MaximumLength(ProfileLimits.Organization);
            e.RuleFor(x => x.StartDate).Must(BeDate).WithMessage("Informe a data de início (AAAA-MM).");
            e.RuleFor(x => x.EndDate).Must(BeOptionalDate).WithMessage("Data de término inválida (AAAA-MM).")
                .Must((x, end) => x.IsCurrent || NotBefore(x.StartDate, end)).WithMessage("A data de término não pode ser anterior à data de início.");
            e.RuleFor(x => x.Activities).MaximumLength(ProfileLimits.LongText);
            e.RuleFor(x => x.Tools).MaximumLength(ProfileLimits.Tools);
            e.RuleFor(x => x.Results).MaximumLength(ProfileLimits.LongText);
        });
        // Mensagens dizem QUAL item está incompleto (ex.: a formação sem situação que a IA propôs). Nada é inventado.
        RuleForEach(x => x.Educations).ChildRules(e =>
        {
            e.RuleFor(x => x.Institution).Visible().WithMessage(x => $"Formação {Name(x.Course, x.Institution)}: informe a instituição.").MaximumLength(ProfileLimits.Organization);
            e.RuleFor(x => x.Course).Visible().WithMessage(x => $"Formação {Name(x.Course, x.Institution)}: informe o curso.").MaximumLength(ProfileLimits.Organization);
            e.RuleFor(x => x.Level).Must(l => DisplayNames.TryParse<EducationLevel>(l, out _))
                .WithMessage(x => $"Formação {Name(x.Course, x.Institution)}: escolha o nível (ex.: Graduação, Técnico).");
            e.RuleFor(x => x.Status).Must(s => DisplayNames.TryParse<EducationStatus>(s, out _))
                .WithMessage(x => $"Formação {Name(x.Course, x.Institution)}: escolha a situação (Cursando, Concluído, Trancado ou Incompleto). O currículo não dizia.");
            e.RuleFor(x => x.StartDate).Must(BeDate).WithMessage(x => $"Formação {Name(x.Course, x.Institution)}: informe o mês de início (AAAA-MM).");
            e.RuleFor(x => x.EndDate).Must(BeOptionalDate).WithMessage(x => $"Formação {Name(x.Course, x.Institution)}: data de conclusão inválida (AAAA-MM).")
                .Must((x, end) => NotBefore(x.StartDate, end)).WithMessage("A data de conclusão não pode ser anterior à data de início.");
        });
        RuleForEach(x => x.Courses).ChildRules(c =>
        {
            c.RuleFor(x => x.Name).Visible().WithMessage("Informe o nome do curso.").MaximumLength(ProfileLimits.Organization);
            c.RuleFor(x => x.Institution).MaximumLength(ProfileLimits.Organization);
            c.RuleFor(x => x.Hours).GreaterThan(0).WithMessage("Informe um número de horas maior que zero.")
                .LessThanOrEqualTo(ProfileLimits.MaxHours).WithMessage("Carga horária muito alta.");
            c.RuleFor(x => x.CompletedAt).Must(BeOptionalDate).WithMessage("Data de conclusão inválida (AAAA-MM).");
        });
        RuleForEach(x => x.Skills).ChildRules(k => k.RuleFor(x => x.Name).Visible().WithMessage("Informe a competência.").MaximumLength(ProfileLimits.SkillName));
        RuleForEach(x => x.Languages).ChildRules(l =>
        {
            l.RuleFor(x => x.Name).Visible().WithMessage("Informe o idioma.").MaximumLength(ProfileLimits.LanguageName);
            l.RuleFor(x => x.Proficiency).Must(p => DisplayNames.TryParse<LanguageProficiency>(p, out _)).WithMessage(x => $"Idioma {Name(x.Name, null)}: escolha o nível (ex.: Básico, Fluente).");
        });
    }

    private static bool BeDate(string? s) => Dates.TryParseLoose(s, out _);

    private static bool BeOptionalDate(string? s) => string.IsNullOrWhiteSpace(s) || BeDate(s);

    private static bool NotBefore(string? start, string? end) =>
        !Dates.TryParseLoose(start, out var s) || !Dates.TryParseLoose(end, out var e) || e >= s;

    /// <summary>"“Administração – UEM”" para identificar o item na mensagem.</summary>
    private static string Name(string? a, string? b)
    {
        var n = string.Join(" – ", new[] { a, b }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
        return n.Length == 0 ? "sem nome" : $"“{(n.Length > 80 ? n[..80] + "…" : n)}”";
    }
}

public sealed class ApplyResumeImportHandler(
    IRepository<ResumeImport> imports,
    IProfileRepository profiles,
    IRepository<AuditEvent> audit,
    ICurrentUser current,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<ApplyResumeImportCommand, ApplyResumeImportResult>
{
    public async Task<ApplyResumeImportResult> Handle(ApplyResumeImportCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var import = await imports.FindAsync(x => x.Id == cmd.Id && x.UserId == uid, ct) ?? throw new NotFoundException("Importação não encontrada.");
        var profile = await profiles.GetByUserIdAsync(uid, ct) ?? throw new NotFoundException("Perfil não encontrado.");

        import.MarkApplied();
        if (cmd.Profile is { } pe)
        {
            // Parte dos dados atuais: o que a proposta não traz, inclusive campo novo do perfil, continua como está.
            var d = profile.ToBasicData();
            profile.UpdateBasics(d with
            {
                FullName = Keep(pe.FullName, d.FullName), Email = Keep(pe.Email, d.Email), Phone = Keep(pe.Phone, d.Phone),
                City = Keep(pe.City, d.City), State = Keep(pe.State, d.State),
                LinkedinUrl = Keep(Urls.Complete(pe.LinkedinUrl), d.LinkedinUrl), GithubUrl = Keep(Urls.Complete(pe.GithubUrl), d.GithubUrl),
                PortfolioUrl = Keep(Urls.Complete(pe.PortfolioUrl), d.PortfolioUrl),
                Headline = Keep(pe.Headline, d.Headline), Summary = Keep(pe.Summary, d.Summary), DesiredRoles = Keep(pe.DesiredRoles, d.DesiredRoles),
            });
        }

        int added = 0, skipped = 0;
        void Tally(bool isNew) { if (isNew) added++; else skipped++; }

        foreach (var e in cmd.Experiences)
        {
            Dates.TryParseLoose(e.StartDate, out var start);
            DateOnly? end = e.EndDate is not null && Dates.TryParseLoose(e.EndDate, out var ed) ? ed : null;
            var dup = profile.Experiences.Any(x => string.Equals(x.Company, e.Company?.Trim(), StringComparison.OrdinalIgnoreCase)
                                                  && string.Equals(x.Role, e.Role?.Trim(), StringComparison.OrdinalIgnoreCase) && x.StartDate == start);
            if (!dup) profile.AddExperience(new ExperienceData(e.Company!, e.Role!, start, e.IsCurrent ? null : end, e.IsCurrent, e.Activities, e.Tools, e.Results));
            Tally(!dup);
        }

        foreach (var e in cmd.Educations)
        {
            Dates.TryParseLoose(e.StartDate, out var start);
            DateOnly? end = e.EndDate is not null && Dates.TryParseLoose(e.EndDate, out var ed) ? ed : null;
            DisplayNames.TryParse<EducationLevel>(e.Level, out var level);
            DisplayNames.TryParse<EducationStatus>(e.Status, out var status);
            var dup = profile.Educations.Any(x => string.Equals(x.Institution, e.Institution?.Trim(), StringComparison.OrdinalIgnoreCase)
                                                 && string.Equals(x.Course, e.Course?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!dup) profile.AddEducation(new EducationData(e.Institution!, e.Course!, level, status, start, end));
            Tally(!dup);
        }

        foreach (var c in cmd.Courses)
        {
            DateOnly? done = c.CompletedAt is not null && Dates.TryParseLoose(c.CompletedAt, out var cd) ? cd : null;
            var dup = profile.Courses.Any(x => string.Equals(x.Name, c.Name?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!dup)
                profile.AddCourse(new CourseData(c.Name!, c.Institution, c.Hours, done,
                    string.Equals(c.Type, "Certification", StringComparison.OrdinalIgnoreCase) || c.Type == "2" ? CourseType.Certification : CourseType.Course));
            Tally(!dup);
        }

        var named = cmd.Skills.Where(s => !VisibleText.IsBlank(s.Name)).ToList();
        var skills = named.DistinctBy(s => s.Name!.Trim().ToLowerInvariant()).ToList();
        skipped += named.Count - skills.Count;
        foreach (var s in skills)
        {
            SkillLevel? level = DisplayNames.TryParse<SkillLevel>(s.Level, out var lv) ? lv : null;
            var isNew = !profile.HasSkill(s.Name!);
            if (isNew) profile.AddSkill(s.Name!, level);
            Tally(isNew);
        }

        foreach (var l in cmd.Languages)
        {
            DisplayNames.TryParse<LanguageProficiency>(l.Proficiency, out var prof);
            var existing = profile.Languages.FirstOrDefault(x => string.Equals(x.Name, l.Name?.Trim(), StringComparison.OrdinalIgnoreCase));
            var changes = existing is null || existing.Proficiency != prof;
            if (existing is null) profile.AddLanguage(l.Name!, prof);
            else if (changes) profile.UpdateLanguage(existing.Id, existing.Name, prof);
            Tally(changes);
        }

        audit.Add(AuditEvent.Record(uid, AuditActions.ResumeApplied, clock.UtcNow));
        await uow.SaveChangesAsync(ct);
        return new ApplyResumeImportResult(ProfileDto.From(profile), GetMissingHandler.Compute(profile), added, skipped);
    }

    private static string? Keep(string? proposed, string? current) => VisibleText.IsBlank(proposed) ? current : proposed;
}

// ------------------------------------------------------------------ POST /resume-imports/{id}/discard

public sealed record DiscardResumeImportCommand(Guid Id) : IRequest<ResumeImportDto>;

public sealed class DiscardResumeImportHandler(IRepository<ResumeImport> imports, IRepository<AuditEvent> audit, ICurrentUser current, IUnitOfWork uow, IClock clock)
    : IRequestHandler<DiscardResumeImportCommand, ResumeImportDto>
{
    public async Task<ResumeImportDto> Handle(DiscardResumeImportCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var i = await imports.FindAsync(x => x.Id == cmd.Id && x.UserId == uid, ct) ?? throw new NotFoundException("Importação não encontrada.");
        i.Discard();
        audit.Add(AuditEvent.Record(uid, AuditActions.ResumeDiscarded, clock.UtcNow));
        await uow.SaveChangesAsync(ct);
        return ProposalJson.ToDto(i);
    }
}
