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

    /// <summary>
    /// Troca o período do Plus ativo (mensal ⇄ anual) com efeito imediato e devolve o rateio (simulado):
    /// <list type="bullet">
    /// <item>Em período grátis: só troca o período; nada é cobrado e a renovação continua no fim do teste.</item>
    /// <item>Pago: crédito = preço do período atual × (dias que faltam do ciclo ÷ dias do ciclo); valor = preço do
    /// período novo − crédito (positivo = cobrança, negativo = crédito a favor da pessoa). O ciclo novo começa agora.</item>
    /// </list>
    /// Valores arredondados a 2 casas. Nenhuma cobrança real acontece (checkout simulado).
    /// </summary>
    public SubscriptionPeriodChange ChangePeriod(BillingPeriod to, DateTime now)
    {
        if (!Enum.IsDefined(to)) throw new DomainException("billing.invalid_period", "Período inválido. Use monthly ou yearly.");
        if (Plan != PlanKind.Plus || Period is null)
            throw new DomainException("billing.not_subscribed", "A troca de período vale só para quem assina o Plus.");
        if (Period == to)
            throw new DomainException("billing.same_period", "Seu Plus já está neste período.");

        var from = Period.Value;
        var inTrial = TrialEndsAt is { } trialEnd && trialEnd > now;
        decimal unusedCredit = 0, newPrice = 0;
        if (inTrial)
        {
            RenewsAt = TrialEndsAt;
        }
        else
        {
            var cycleEnd = NextRenewal(now) ?? Advance(now, from);
            var cycleStart = Back(cycleEnd, from);
            var total = (cycleEnd - cycleStart).TotalSeconds;
            var fraction = total <= 0 ? 0 : Math.Clamp((cycleEnd - now).TotalSeconds / total, 0, 1);
            unusedCredit = Math.Round(PlanRules.PlusPrice(from) * (decimal)fraction, 2, MidpointRounding.AwayFromZero);
            newPrice = PlanRules.PlusPrice(to);
            RenewsAt = Advance(now, to);
        }

        Period = to;
        return SubscriptionPeriodChange.Record(UserId, from, to, now, unusedCredit, newPrice, inTrial);
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

    private static DateTime Back(DateTime from, BillingPeriod p) => p == BillingPeriod.Yearly ? from.AddYears(-1) : from.AddMonths(-1);
}

/// <summary>Registro (simulado) de uma troca de período do Plus com o rateio calculado. Sem cobrança real.</summary>
public sealed class SubscriptionPeriodChange : Entity
{
    private SubscriptionPeriodChange() { }

    public Guid UserId { get; private set; }
    public BillingPeriod FromPeriod { get; private set; }
    public BillingPeriod ToPeriod { get; private set; }
    public DateTime ChangedAt { get; private set; }

    /// <summary>Valor não usado do ciclo atual, abatido do período novo.</summary>
    public decimal UnusedCredit { get; private set; }

    /// <summary>Preço do período novo (0 durante o teste grátis).</summary>
    public decimal NewPrice { get; private set; }

    /// <summary><c>NewPrice − UnusedCredit</c>: positivo = cobrado agora; negativo = crédito a favor da pessoa.</summary>
    public decimal Amount { get; private set; }

    public bool DuringTrial { get; private set; }

    public static SubscriptionPeriodChange Record(Guid userId, BillingPeriod from, BillingPeriod to, DateTime now, decimal unusedCredit, decimal newPrice, bool duringTrial) => new()
    {
        UserId = userId,
        FromPeriod = from,
        ToPeriod = to,
        ChangedAt = now,
        UnusedCredit = unusedCredit,
        NewPrice = newPrice,
        Amount = newPrice - unusedCredit,
        DuringTrial = duringTrial,
    };
}
