using Zyft.Domain.Common;

namespace Zyft.Domain.Billing;

/// <summary>
/// Plano da pessoa. Ausência de assinatura = Gratuito. O checkout é SIMULADO (ambiente de demonstração, sem gateway):
/// assinar ativa o Plus com 7 dias grátis na primeira vez.
/// </summary>
public sealed class Subscription : Entity
{
    private Subscription() { }

    public Guid UserId { get; private set; }
    public PlanKind Plan { get; private set; }
    public BillingPeriod? Period { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? TrialEndsAt { get; private set; }
    public DateTime? RenewsAt { get; private set; }
    public DateTime? CanceledAt { get; private set; }

    /// <summary>O período grátis vale uma vez por conta.</summary>
    public bool TrialUsed { get; private set; }

    public static Subscription StartFree(Guid userId) => new() { UserId = userId, Plan = PlanKind.Free };

    public void Subscribe(BillingPeriod period, DateTime now)
    {
        if (!Enum.IsDefined(period)) throw new DomainException("billing.invalid_period", "Período inválido. Use monthly ou yearly.");
        if (Plan == PlanKind.Plus && Period == period)
            throw new DomainException("billing.already_subscribed", "Você já está no Plus com este período.");

        var startTrial = !TrialUsed;
        Plan = PlanKind.Plus;
        Period = period;
        StartedAt = now;
        CanceledAt = null;
        TrialUsed = true;
        TrialEndsAt = startTrial ? now.AddDays(PlanRules.TrialDays) : null;
        RenewsAt = startTrial ? TrialEndsAt : Advance(now, period);
    }

    /// <summary>Cancela. Na demonstração volta ao Gratuito na hora (em produção seria ao fim do período pago).</summary>
    public void Cancel(DateTime now)
    {
        if (Plan == PlanKind.Free) throw new DomainException("billing.not_subscribed", "Você já está no plano Gratuito.");
        Plan = PlanKind.Free;
        Period = null;
        TrialEndsAt = null;
        RenewsAt = null;
        CanceledAt = now;
    }

    /// <summary>Próxima renovação simulada a partir de <paramref name="now"/> (rola o ciclo para frente).</summary>
    public DateTime? NextRenewal(DateTime now)
    {
        if (Plan != PlanKind.Plus || RenewsAt is null || Period is null) return RenewsAt;
        var r = RenewsAt.Value;
        while (r < now) r = Advance(r, Period.Value);
        return r;
    }

    private static DateTime Advance(DateTime from, BillingPeriod p) => p == BillingPeriod.Yearly ? from.AddYears(1) : from.AddMonths(1);
}
