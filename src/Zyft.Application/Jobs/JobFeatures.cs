using FluentValidation;
using MediatR;
using Zyft.Application.Common;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Application.Common.Text;
using Zyft.Application.Profiles;
using Zyft.Domain.Audit;
using Zyft.Domain.Consents;

namespace Zyft.Application.Jobs;

/// <summary>Analisa a vaga visível (RF-17). Exige o consentimento (b) — bloqueado na API, não só na interface.</summary>
public sealed record AnalyzeJobCommand(string Text, string? Url = null, string? Title = null) : IRequest<JobAnalysisResult>;

public sealed class AnalyzeJobValidator : AbstractValidator<AnalyzeJobCommand>
{
    public AnalyzeJobValidator()
    {
        RuleFor(x => x.Text).NotEmpty().WithMessage("Envie o texto visível da vaga.").MaximumLength(50000);
        RuleFor(x => x.Url).MaximumLength(2000);
        RuleFor(x => x.Title).MaximumLength(500);
    }
}

public sealed class AnalyzeJobHandler(
    IProfileRepository profiles,
    IRepository<AuditEvent> audit,
    IConsentGuard consents,
    IJobAnalyzer analyzer,
    ICurrentUser current,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<AnalyzeJobCommand, JobAnalysisResult>
{
    public async Task<JobAnalysisResult> Handle(AnalyzeJobCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        await consents.EnsureGrantedAsync(uid, ConsentPurpose.JobAnalysis, ct);
        var profile = await profiles.GetByUserIdAsync(uid, ct) ?? throw new NotFoundException("Perfil não encontrado.");
        var lines = ProfileFacts.Lines(profile, DateOnly.FromDateTime(clock.UtcNow));
        var facts = ProfileFacts.Build(profile);

        var result = await analyzer.AnalyzeAsync(cmd.Url, cmd.Title, cmd.Text, lines, facts.Contains, ct);
        audit.Add(AuditEvent.Record(uid, AuditActions.JobAnalyzed, clock.UtcNow, TextTools.HostOf(cmd.Url), result.Requirements.Count));
        await uow.SaveChangesAsync(ct);
        return result;
    }
}
