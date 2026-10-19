using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Audit;
using Zyft.Domain.Billing;

namespace Zyft.Application.Billing;

// Contrato v2 — Planos e uso. Pagamento SIMULADO: nenhuma cobrança real (demo = true em toda resposta).

public sealed record UsageItemDto(int Used, [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] int? Limit, int Bonus);

public sealed record UsageDto(string Month, UsageItemDto AssistedApplications, UsageItemDto AiSuggestions, UsageItemDto JobAnalyses, UsageItemDto ResumeImports);

public sealed record PricesDto(decimal PlusMonthly, decimal PlusYearly, decimal Pack);

public sealed record RewardedDto(int ClaimedThisMonth, int MaxPerMonth, int CreditsPerClaim);

public sealed record PackDto(int AssistedRemaining, int AiRemaining, DateTime ExpiresAt);

public sealed record BillingDto(
    string Plan,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? Period,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] DateTime? TrialEndsAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] DateTime? RenewsAt,
    bool Demo,
    PricesDto Prices,
    UsageDto Usage,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] int? AnswersLimit,
    int DevicesLimit,
    RewardedDto Rewarded,
    IReadOnlyList<PackDto> Packs,
    bool AdsEnabled,
    bool TrialAvailable,
    int TrialDays)
{
    public static BillingDto Build(Subscription? sub, MonthlyUsage? usage, IEnumerable<UsagePack> packs, DateTime now)
    {
        var plan = sub?.Plan ?? PlanKind.Free;
        var month = PlanRules.MonthOf(now);
        UsageItemDto Item(UsageResource r) => new(
            usage?.UsedOf(r) ?? 0,
            PlanRules.MonthlyLimit(plan, r),
            r == UsageResource.AssistedApplications ? usage?.BonusRemaining ?? 0 : 0);

        return new BillingDto(
            plan == PlanKind.Plus ? "plus" : "free",
            plan == PlanKind.Plus ? sub!.Period switch { BillingPeriod.Yearly => "yearly", BillingPeriod.Monthly => "monthly", _ => null } : null,
            plan == PlanKind.Plus ? sub!.TrialEndsAt : null,
            plan == PlanKind.Plus ? sub!.NextRenewal(now) : null,
            true,
            new PricesDto(PlanRules.PlusMonthlyPrice, PlanRules.PlusYearlyPrice, PlanRules.PackPrice),
            new UsageDto(month, Item(UsageResource.AssistedApplications), Item(UsageResource.AiSuggestions), Item(UsageResource.JobAnalyses), Item(UsageResource.ResumeImports)),
            PlanRules.AnswersLimit(plan),
            PlanRules.DevicesLimit(plan),
            new RewardedDto(usage?.RewardedClaims ?? 0, PlanRules.RewardedMaxPerMonth, PlanRules.RewardedCreditsPerClaim),
            packs.Where(p => p.IsActive(now)).OrderBy(p => p.ExpiresAt).Select(p => new PackDto(p.AssistedRemaining, p.AiRemaining, p.ExpiresAt)).ToList(),
            PlanRules.AdsEnabled(plan),
            // Os 7 dias grátis valem uma vez por conta: quem já testou (mesmo tendo cancelado) não ganha de novo.
            sub is null || !sub.TrialUsed,
            PlanRules.TrialDays);
    }
}

/// <summary>Carrega o estado de cobrança da pessoa (sem criar nada).</summary>
public sealed class BillingState(IRepository<Subscription> subscriptions, IRepository<MonthlyUsage> usages, IRepository<UsagePack> packs, IClock clock)
{
    public async Task<(Subscription? Sub, MonthlyUsage? Usage, List<UsagePack> Packs)> LoadAsync(Guid userId, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var month = PlanRules.MonthOf(now);
        var sub = await subscriptions.FindAsync(s => s.UserId == userId, ct);
        var usage = await usages.FindAsync(u => u.UserId == userId && u.Month == month, ct);
        var ps = await packs.ListAsync(p => p.UserId == userId && p.ExpiresAt > now, ct);
        return (sub, usage, ps);
    }

    public async Task<Subscription> GetOrStartSubscriptionAsync(Guid userId, CancellationToken ct)
    {
        var sub = await subscriptions.FindAsync(s => s.UserId == userId, ct);
        if (sub is null)
        {
            sub = Subscription.StartFree(userId);
            subscriptions.Add(sub);
        }

        return sub;
    }
}

// ------------------------------------------------------------------ GET /billing

public sealed record GetBillingQuery : IRequest<BillingDto>;

public sealed class GetBillingHandler(BillingState state, ICurrentUser current, IClock clock) : IRequestHandler<GetBillingQuery, BillingDto>
{
    public async Task<BillingDto> Handle(GetBillingQuery q, CancellationToken ct)
    {
        var (sub, usage, packs) = await state.LoadAsync(current.UserId, ct);
        return BillingDto.Build(sub, usage, packs, clock.UtcNow);
    }
}

// ------------------------------------------------------------------ POST /billing/subscribe

public sealed record SubscribeCommand(string? Period) : IRequest<BillingDto>;

public sealed class SubscribeValidator : AbstractValidator<SubscribeCommand>
{
    public SubscribeValidator() =>
        RuleFor(x => x.Period).Must(p => p is "monthly" or "yearly").WithMessage("Informe period: monthly ou yearly.");
}

public sealed class SubscribeHandler(BillingState state, IRepository<AuditEvent> audit, ICurrentUser current, IUnitOfWork uow, IClock clock)
    : IRequestHandler<SubscribeCommand, BillingDto>
{
    public async Task<BillingDto> Handle(SubscribeCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var now = clock.UtcNow;
        var sub = await state.GetOrStartSubscriptionAsync(uid, ct);
        sub.Subscribe(cmd.Period == "yearly" ? BillingPeriod.Yearly : BillingPeriod.Monthly, now);
        audit.Add(AuditEvent.Record(uid, AuditActions.PlanChanged, now));
        await uow.SaveChangesAsync(ct);
        var (_, usage, packs) = await state.LoadAsync(uid, ct);
        return BillingDto.Build(sub, usage, packs, now);
    }
}

// ------------------------------------------------------------------ POST /billing/cancel

public sealed record CancelSubscriptionCommand : IRequest<BillingDto>;

public sealed class CancelSubscriptionHandler(BillingState state, IRepository<AuditEvent> audit, ICurrentUser current, IUnitOfWork uow, IClock clock)
    : IRequestHandler<CancelSubscriptionCommand, BillingDto>
{
    public async Task<BillingDto> Handle(CancelSubscriptionCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var now = clock.UtcNow;
        var sub = await state.GetOrStartSubscriptionAsync(uid, ct);
        sub.Cancel(now);
        audit.Add(AuditEvent.Record(uid, AuditActions.PlanChanged, now));
        await uow.SaveChangesAsync(ct);
        var (_, usage, packs) = await state.LoadAsync(uid, ct);
        return BillingDto.Build(sub, usage, packs, now);
    }
}

// ------------------------------------------------------------------ POST /billing/packs

public sealed record BuyPackCommand : IRequest<BillingDto>;

public sealed class BuyPackHandler(BillingState state, IRepository<UsagePack> packs, IRepository<AuditEvent> audit, ICurrentUser current, IUnitOfWork uow, IClock clock)
    : IRequestHandler<BuyPackCommand, BillingDto>
{
    public async Task<BillingDto> Handle(BuyPackCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var now = clock.UtcNow;
        packs.Add(UsagePack.Purchase(uid, now));
        audit.Add(AuditEvent.Record(uid, AuditActions.PackPurchased, now));
        await uow.SaveChangesAsync(ct);
        var (sub, usage, ps) = await state.LoadAsync(uid, ct);
        return BillingDto.Build(sub, usage, ps, now);
    }
}

// ------------------------------------------------------------------ POST /ads/rewarded/claim

/// <summary>Oferta recompensada vista: +3 candidaturas assistidas de bônus no mês (máx. 3 vezes). 409 quando esgotado.</summary>
public sealed record ClaimRewardedCommand : IRequest<BillingDto>;

public sealed class ClaimRewardedHandler(BillingState state, IRepository<MonthlyUsage> usages, IRepository<AuditEvent> audit, ICurrentUser current, IUnitOfWork uow, IClock clock)
    : IRequestHandler<ClaimRewardedCommand, BillingDto>
{
    public async Task<BillingDto> Handle(ClaimRewardedCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var now = clock.UtcNow;
        var (sub, _, packs) = await state.LoadAsync(uid, ct);
        if (sub?.Plan == PlanKind.Plus)
            throw new ConflictException("No Plus não há limite de candidaturas: a oferta recompensada não se aplica.", "ads.rewarded_unavailable");

        var usage = await UsageMeter.GetOrStartUsageAsync(usages, uid, PlanRules.MonthOf(now), ct);
        usage.ClaimRewarded();
        audit.Add(AuditEvent.Record(uid, AuditActions.RewardedClaimed, now, fieldCount: PlanRules.RewardedCreditsPerClaim));
        await uow.SaveChangesAsync(ct);
        return BillingDto.Build(sub, usage, packs, now);
    }
}
