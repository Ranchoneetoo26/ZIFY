using FluentValidation;
using MediatR;
using Zyft.Application.Applications;
using Zyft.Application.Billing;
using Zyft.Application.Common;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Application.Common.Text;
using Zyft.Application.Profiles;
using Zyft.Application.Writing;
using Zyft.Domain.Answers;
using Zyft.Domain.Applications;
using Zyft.Domain.Audit;
using Zyft.Domain.Billing;
using Zyft.Domain.Consents;
using Zyft.Domain.Sensitive;

namespace Zyft.Application.Forms;

// ================================================================== POST /forms/mappings

/// <summary>Pede sugestões só para os campos enviados (RNF-02: nunca devolve o perfil inteiro).</summary>
public sealed record MapFieldsCommand(string? PageUrl, IReadOnlyList<FormField> Fields) : IRequest<MappingResult>;

public sealed class MapFieldsValidator : AbstractValidator<MapFieldsCommand>
{
    public const int MaxFields = 300;

    public MapFieldsValidator()
    {
        RuleFor(x => x.PageUrl).MaximumLength(2000)
            .Must(u => u is null || Uri.TryCreate(u, UriKind.Absolute, out _)).WithMessage("pageUrl deve ser uma URL absoluta.");
        RuleFor(x => x.Fields).NotNull().WithMessage("Envie os campos do formulário.")
            .Must(f => f is { Count: > 0 }).WithMessage("Envie pelo menos um campo.")
            .Must(f => f is null || f.Count <= MaxFields).WithMessage($"No máximo {MaxFields} campos por requisição.")
            .Must(f => f is null || f.Select(x => x.Id).Distinct().Count() == f.Count).WithMessage("Os ids dos campos devem ser únicos.");
        RuleForEach(x => x.Fields).ChildRules(f =>
        {
            f.RuleFor(x => x.Id).NotEmpty().WithMessage("Todo campo precisa de id.").MaximumLength(200);
            f.RuleFor(x => x.Label).MaximumLength(2000);
            f.RuleFor(x => x.Section).MaximumLength(2000);
            f.RuleFor(x => x.CurrentValue).MaximumLength(10000);
            f.RuleFor(x => x.Options).Must(o => o is null || o.Count <= 500).WithMessage("No máximo 500 opções por campo.");
        });
    }
}

public sealed class MapFieldsHandler(
    IProfileRepository profiles,
    IRepository<ApprovedAnswer> answers,
    IRepository<SensitiveData> sensitive,
    IRepository<AuditEvent> audit,
    IConsentGuard consents,
    ISensitiveDataProtector protector,
    IFieldMapper mapper,
    ICurrentUser current,
    IUnitOfWork uow,
    IClock clock,
    IUsageMeter meter) : IRequestHandler<MapFieldsCommand, MappingResult>
{
    public async Task<MappingResult> Handle(MapFieldsCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var profile = await profiles.GetByUserIdAsync(uid, ct) ?? throw new NotFoundException("Perfil não encontrado.");
        // Candidatura assistida: conta 1x por (pessoa, origem+caminho da página) por mês (402 se acabou o limite).
        await meter.ConsumeAsync(uid, UsageResource.AssistedApplications, PageKey(cmd.PageUrl), ct);
        var approved = (await answers.ListAsync(a => a.UserId == uid, ct)).Select(a => new ApprovedAnswerRef(a.Id, a.Question, a.Text)).ToList();

        // Dados sensíveis só são decifrados quando há consentimento (c) — e nunca passam pela IA.
        var sensitiveConsent = await consents.IsGrantedAsync(uid, ConsentPurpose.SensitiveData, ct);
        var sensitiveValues = new Dictionary<SensitiveDataKind, string>();
        if (sensitiveConsent)
            foreach (var s in await sensitive.ListAsync(x => x.UserId == uid, ct))
                sensitiveValues[s.Kind] = protector.Unprotect(s.EncryptedValue);

        var result = await mapper.MapAsync(new MappingInput
        {
            Fields = cmd.Fields,
            Profile = ProfileFacts.Build(profile),
            ApprovedAnswers = approved,
            SensitiveConsent = sensitiveConsent,
            SensitiveValues = sensitiveValues,
        }, ct);

        audit.Add(AuditEvent.Record(uid, AuditActions.FormMapped, clock.UtcNow, TextTools.HostOf(cmd.PageUrl), cmd.Fields.Count));
        await uow.SaveChangesAsync(ct);
        return result;
    }

    /// <summary>Origem + caminho (sem query nem fragmento). Só o hash disso é guardado.</summary>
    internal static string? PageKey(string? pageUrl) =>
        Uri.TryCreate(pageUrl, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps)
            ? $"{u.Scheme}://{u.Authority}{u.AbsolutePath}".TrimEnd('/')
            : null;
}

// ================================================================== POST /forms/open-answers

/// <summary>Sugestão de texto para pergunta aberta (RF-15). Exige o consentimento "IA para textos livres".</summary>
public sealed record OpenAnswerCommand(string Question, int? MaxLength, string? PageUrl = null, string? JobTitle = null, string? Company = null, string? JobText = null)
    : IRequest<OpenAnswerResult>;

/// <summary>
/// <c>kind</c>: approved (resposta aprovada reaproveitada) | generated (IA, sempre revisar) | missing (perfil sem fatos
/// suficientes ou IA devolveu texto vazio) | unknown (IA indisponível). Nunca vem pré-aprovado (<c>selected=false</c>).
/// Garantia do contrato: <c>value</c> nunca vem vazio ou só com espaços — ou há texto, ou <c>value</c> é nulo e
/// <c>note</c> explica o que fazer.
/// </summary>
public sealed record OpenAnswerResult(string Question, string Kind, SuggestionSource Source, string? Value, IReadOnlyList<string> UsedFacts, string Note, bool Selected, Guid? ApprovedAnswerId);

public sealed class OpenAnswerValidator : AbstractValidator<OpenAnswerCommand>
{
    public OpenAnswerValidator()
    {
        RuleFor(x => x.Question).NotEmpty().WithMessage("Informe a pergunta.").MaximumLength(1000);
        // null ou 0 = campo sem limite (D-03); com limite, entre 20 e 10000.
        RuleFor(x => x.MaxLength).InclusiveBetween(20, 10000).When(x => x.MaxLength is > 0).WithMessage("maxLength deve estar entre 20 e 10000 (ou 0/null para sem limite).")
            .GreaterThanOrEqualTo(0).WithMessage("maxLength não pode ser negativo.");
        RuleFor(x => x.JobText).MaximumLength(20000);
    }
}

public static class OpenAnswerNotes
{
    public const string NoDraft = "A IA não conseguiu escrever um rascunho com os fatos do seu perfil; escreva do seu jeito.";
}

public sealed class OpenAnswerHandler(
    IProfileRepository profiles,
    IRepository<ApprovedAnswer> answers,
    IRepository<AuditEvent> audit,
    IConsentGuard consents,
    IAnswerWriter writer,
    ICurrentUser current,
    IUnitOfWork uow,
    IClock clock,
    IUsageMeter meter) : IRequestHandler<OpenAnswerCommand, OpenAnswerResult>
{
    public async Task<OpenAnswerResult> Handle(OpenAnswerCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        await consents.EnsureGrantedAsync(uid, ConsentPurpose.AiText, ct);
        await meter.ConsumeAsync(uid, UsageResource.AiSuggestions, null, ct);
        var profile = await profiles.GetByUserIdAsync(uid, ct) ?? throw new NotFoundException("Perfil não encontrado.");
        var now = clock.UtcNow;
        audit.Add(AuditEvent.Record(uid, AuditActions.OpenAnswerSuggested, now, TextTools.HostOf(cmd.PageUrl)));

        // 1) Reaproveita resposta aprovada para pergunta parecida (RF-06).
        var best = (await answers.ListAsync(a => a.UserId == uid, ct))
            .Select(a => (a, s: TextTools.Jaccard(a.Question, cmd.Question)))
            .OrderByDescending(x => x.s).FirstOrDefault();
        if (best.a is not null && best.s >= 0.6 && !string.IsNullOrWhiteSpace(best.a.Text))
        {
            best.a.MarkUsed(now);
            await uow.SaveChangesAsync(ct);
            var text = cmd.MaxLength is > 0 ? TextTools.Truncate(best.a.Text, cmd.MaxLength.Value) : best.a.Text;
            return new OpenAnswerResult(cmd.Question, SuggestionKinds.Approved, SuggestionSource.Answer, text, Array.Empty<string>(),
                $"Resposta aprovada anteriormente para \"{best.a.Question}\". Revise antes de usar.", false, best.a.Id);
        }

        await uow.SaveChangesAsync(ct);

        // 2) IA com fatos do perfil (sem dados sensíveis — eles nem estão nas linhas).
        try
        {
            var lines = ProfileFacts.Lines(profile, DateOnly.FromDateTime(now));
            var job = cmd.JobTitle is null && cmd.JobText is null ? null : new JobContext(cmd.JobTitle, cmd.Company, cmd.JobText);
            var g = await writer.GenerateAsync(cmd.Question, lines, job, cmd.MaxLength is > 0 ? cmd.MaxLength : null, ct);
            var answer = g.Answer?.Trim() ?? string.Empty;
            if (answer.Length == 0)
                return new OpenAnswerResult(cmd.Question, SuggestionKinds.Missing, SuggestionSource.None, null, Array.Empty<string>(),
                    string.IsNullOrWhiteSpace(g.MissingInfo) ? OpenAnswerNotes.NoDraft : $"{OpenAnswerNotes.NoDraft} O que faltou: {g.MissingInfo.Trim()}", false, null);
            return new OpenAnswerResult(cmd.Question, "generated", SuggestionSource.Ai, answer, g.UsedFacts,
                string.IsNullOrEmpty(g.MissingInfo) ? "Texto gerado a partir do seu perfil — revise antes de usar." : $"Atenção: {g.MissingInfo}", false, null);
        }
        catch (LlmUnavailableException)
        {
            return new OpenAnswerResult(cmd.Question, SuggestionKinds.Unknown, SuggestionSource.None, null, Array.Empty<string>(),
                "IA indisponível no momento. Responda manualmente ou tente de novo mais tarde.", false, null);
        }
    }
}

// ================================================================== POST /forms/completions

/// <summary>
/// Registro mínimo de conclusão (RF-19, ADR-0012): chamado DEPOIS que a pessoa enviou no portal por conta própria.
/// O Zyft nunca envia (RN-01). Cria a candidatura e um evento de auditoria sem valores.
/// </summary>
public sealed record CompleteFormCommand(string Portal, string JobTitle, string? Company, string? JobUrl, int FieldsFilled) : IRequest<ApplicationDto>;

public sealed class CompleteFormValidator : AbstractValidator<CompleteFormCommand>
{
    public CompleteFormValidator()
    {
        RuleFor(x => x.Portal).NotEmpty().WithMessage("Informe o portal.").MaximumLength(200);
        RuleFor(x => x.JobTitle).NotEmpty().WithMessage("Informe o cargo.").MaximumLength(300);
        RuleFor(x => x.Company).MaximumLength(200);
        RuleFor(x => x.JobUrl).MaximumLength(2000).Must(u => u is null || Uri.TryCreate(u, UriKind.Absolute, out _)).WithMessage("jobUrl deve ser uma URL absoluta.");
        RuleFor(x => x.FieldsFilled).InclusiveBetween(0, 1000).WithMessage("fieldsFilled deve estar entre 0 e 1000.");
    }
}

public sealed class CompleteFormHandler(IRepository<JobApplication> applications, IRepository<AuditEvent> audit, ICurrentUser current, IUnitOfWork uow, IClock clock)
    : IRequestHandler<CompleteFormCommand, ApplicationDto>
{
    public async Task<ApplicationDto> Handle(CompleteFormCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var now = clock.UtcNow;
        var portal = TextTools.HostOf(cmd.Portal) ?? cmd.Portal.Trim();
        var a = JobApplication.Confirm(uid, portal, cmd.JobTitle, cmd.Company, cmd.JobUrl, cmd.FieldsFilled, now);
        applications.Add(a);
        audit.Add(AuditEvent.Record(uid, AuditActions.ApplicationConfirmed, now, portal, cmd.FieldsFilled));
        await uow.SaveChangesAsync(ct);
        return ApplicationDto.From(a);
    }
}
