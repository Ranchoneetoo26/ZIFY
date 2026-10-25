using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Audit;
using Zyft.Domain.Onboarding;

namespace Zyft.Application.Onboarding;

/// <summary>Progresso do onboarding (só ids de passos). As respostas vão para o perfil pelos endpoints do perfil.</summary>
public sealed record OnboardingDto(
    IReadOnlyList<string> Answered,
    IReadOnlyList<string> Skipped,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? LastStep,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] DateTime? CompletedAt)
{
    public static readonly OnboardingDto Empty = new(Array.Empty<string>(), Array.Empty<string>(), null, null);

    public static OnboardingDto From(OnboardingProgress p) => new(p.AnsweredSteps, p.SkippedSteps, p.LastStep, p.CompletedAt);
}

public sealed record GetOnboardingQuery : IRequest<OnboardingDto>;

public sealed class GetOnboardingHandler(IRepository<OnboardingProgress> repo, ICurrentUser current) : IRequestHandler<GetOnboardingQuery, OnboardingDto>
{
    public async Task<OnboardingDto> Handle(GetOnboardingQuery q, CancellationToken ct)
    {
        var uid = current.UserId;
        var p = await repo.FindAsync(x => x.UserId == uid, ct);
        return p is null ? OnboardingDto.Empty : OnboardingDto.From(p);
    }
}

public sealed record UpdateOnboardingCommand : IRequest<OnboardingDto>
{
    public IReadOnlyList<string>? Answered { get; init; }
    public IReadOnlyList<string>? Skipped { get; init; }
    public string? LastStep { get; init; }
    public bool Completed { get; init; }
}

public sealed class UpdateOnboardingValidator : AbstractValidator<UpdateOnboardingCommand>
{
    private static bool Known(string? id) => id is not null && OnboardingProgress.Steps.Contains(id.Trim().ToLowerInvariant());

    public UpdateOnboardingValidator()
    {
        var msg = $"Passo desconhecido. Use: {string.Join(", ", OnboardingProgress.Steps)}.";
        RuleForEach(x => x.Answered).Must(Known).WithMessage(msg);
        RuleForEach(x => x.Skipped).Must(Known).WithMessage(msg);
        RuleFor(x => x.LastStep).Must(s => string.IsNullOrWhiteSpace(s) || Known(s)).WithMessage(msg);
    }
}

public sealed class UpdateOnboardingHandler(IRepository<OnboardingProgress> repo, IRepository<AuditEvent> audit, ICurrentUser current, IUnitOfWork uow, IClock clock)
    : IRequestHandler<UpdateOnboardingCommand, OnboardingDto>
{
    public async Task<OnboardingDto> Handle(UpdateOnboardingCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var now = clock.UtcNow;
        var p = await repo.FindAsync(x => x.UserId == uid, ct);
        if (p is null)
        {
            p = OnboardingProgress.Start(uid);
            repo.Add(p);
        }

        if (p.Update(cmd.Answered, cmd.Skipped, cmd.LastStep, cmd.Completed, now))
            audit.Add(AuditEvent.Record(uid, AuditActions.OnboardingCompleted, now, fieldCount: p.AnsweredSteps.Count));
        await uow.SaveChangesAsync(ct);
        return OnboardingDto.From(p);
    }
}
