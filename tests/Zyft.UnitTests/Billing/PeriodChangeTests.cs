using Zyft.Domain.Billing;
using Zyft.Domain.Common;

namespace Zyft.UnitTests.Billing;

/// <summary>Troca de período do Plus (rateio simulado) e pacote avulso no Plus.</summary>
public class PeriodChangeTests
{
    private static readonly DateTime Oct1 = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Assinatura paga (sem teste grátis): o teste já foi usado numa assinatura anterior.</summary>
    private static Subscription Paid(BillingPeriod period, DateTime start)
    {
        var s = Subscription.StartFree(Guid.NewGuid());
        s.Subscribe(BillingPeriod.Monthly, start.AddDays(-60));
        s.Cancel(start.AddDays(-30));
        s.Subscribe(period, start);
        s.TrialEndsAt.Should().BeNull();
        return s;
    }

    [Fact]
    public void Mensal_para_anual_no_meio_do_ciclo_abate_o_que_falta_do_mes()
    {
        var s = Paid(BillingPeriod.Monthly, Oct1);
        var at = Oct1.AddDays(15); // faltam 16 de 31 dias

        var c = s.ChangePeriod(BillingPeriod.Yearly, at);

        c.UnusedCredit.Should().Be(5.11m); // 9,90 × 16/31 = 5,1096
        c.NewPrice.Should().Be(99.00m);
        c.Amount.Should().Be(93.89m);
        c.DuringTrial.Should().BeFalse();
        s.Period.Should().Be(BillingPeriod.Yearly);
        s.RenewsAt.Should().Be(at.AddYears(1));
    }

    [Fact]
    public void Anual_para_mensal_gera_credito_a_favor_da_pessoa()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var s = Paid(BillingPeriod.Yearly, start);

        var c = s.ChangePeriod(BillingPeriod.Monthly, start.AddDays(182)); // faltam 183 de 365 dias

        c.UnusedCredit.Should().Be(49.64m); // 99 × 183/365 = 49,6356
        c.Amount.Should().Be(-39.74m); // 9,90 − 49,64
        s.Period.Should().Be(BillingPeriod.Monthly);
        s.RenewsAt.Should().Be(start.AddDays(182).AddMonths(1));
    }

    [Fact]
    public void Ciclo_que_ja_renovou_usa_o_ciclo_atual()
    {
        var s = Paid(BillingPeriod.Monthly, Oct1);
        var at = Oct1.AddMonths(1).AddDays(15); // 16/nov: ciclo de 1/nov a 1/dez (30 dias), faltam 15
        s.ChangePeriod(BillingPeriod.Yearly, at).UnusedCredit.Should().Be(4.95m); // 9,90 × 15/30
    }

    [Fact]
    public void No_teste_gratis_so_troca_o_periodo_sem_valor()
    {
        var s = Subscription.StartFree(Guid.NewGuid());
        s.Subscribe(BillingPeriod.Monthly, Oct1);
        var trialEnd = s.TrialEndsAt;

        var c = s.ChangePeriod(BillingPeriod.Yearly, Oct1.AddDays(3));

        c.DuringTrial.Should().BeTrue();
        c.Amount.Should().Be(0);
        s.Period.Should().Be(BillingPeriod.Yearly);
        s.RenewsAt.Should().Be(trialEnd);
        s.TrialEndsAt.Should().Be(trialEnd);
    }

    [Fact]
    public void Sem_Plus_ou_mesmo_periodo_viola_regra()
    {
        var free = Subscription.StartFree(Guid.NewGuid());
        free.Invoking(s => s.ChangePeriod(BillingPeriod.Yearly, Oct1)).Should().Throw<DomainException>().Which.Code.Should().Be("billing.not_subscribed");

        var plus = Paid(BillingPeriod.Monthly, Oct1);
        plus.Invoking(s => s.ChangePeriod(BillingPeriod.Monthly, Oct1)).Should().Throw<DomainException>().Which.Code.Should().Be("billing.same_period");
        plus.Cancel(Oct1.AddDays(1));
        plus.Invoking(s => s.ChangePeriod(BillingPeriod.Yearly, Oct1.AddDays(2))).Should().Throw<DomainException>().Which.Code.Should().Be("billing.not_subscribed");
    }

    [Fact]
    public void No_Plus_os_pacotes_nao_sao_gastos_e_ficam_para_depois()
    {
        var uid = Guid.NewGuid();
        var packs = new[] { UsagePack.Purchase(uid, Oct1), UsagePack.Purchase(uid, Oct1.AddDays(1)) };
        var usage = MonthlyUsage.Start(uid, "2026-10");

        for (var i = 0; i < 20; i++)
            usage.Consume(UsageResource.AssistedApplications, PlanRules.MonthlyLimit(PlanKind.Plus, UsageResource.AssistedApplications), packs, Oct1.AddDays(2))
                .Should().Be(UsageSource.Plan);
        packs.Sum(p => p.AssistedRemaining).Should().Be(2 * PlanRules.PackAssisted);

        // De volta ao Gratuito, depois do limite do mês os créditos somados dos pacotes são usados.
        var free = MonthlyUsage.Start(uid, "2026-11");
        var limit = PlanRules.MonthlyLimit(PlanKind.Free, UsageResource.AssistedApplications);
        for (var i = 0; i < limit + 2 * PlanRules.PackAssisted; i++)
            free.Consume(UsageResource.AssistedApplications, limit, packs, Oct1.AddDays(32));
        packs.Sum(p => p.AssistedRemaining).Should().Be(0);
        free.Invoking(u => u.Consume(UsageResource.AssistedApplications, limit, packs, Oct1.AddDays(32))).Should().Throw<PlanLimitReachedException>();
    }
}
