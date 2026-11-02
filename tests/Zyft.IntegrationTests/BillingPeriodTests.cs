using System.Net;
using System.Net.Http.Json;
using Zyft.IntegrationTests.Support;

namespace Zyft.IntegrationTests;

/// <summary>Troca de período do Plus e pacote avulso no Plus (pagamento simulado).</summary>
public class BillingPeriodTests(ZyftApiFactory factory) : IClassFixture<ZyftApiFactory>
{
    private async Task<HttpClient> NewUser() => factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);

    [Fact]
    public async Task Troca_de_periodo_exige_Plus_ativo_e_periodo_diferente()
    {
        var c = await NewUser();
        var free = await c.PostAsJsonAsync("/api/v1/billing/subscription/period", new { period = "yearly" });
        free.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await free.JsonAsync()).GetProperty("code").GetString().Should().Be("billing.not_subscribed");

        (await c.PostAsJsonAsync("/api/v1/billing/subscription/period", new { period = "semanal" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await c.PostAsJsonAsync("/api/v1/billing/subscribe", new { period = "monthly" })).EnsureSuccessStatusCode();
        var same = await c.PostAsJsonAsync("/api/v1/billing/subscription/period", new { period = "monthly" });
        same.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await same.JsonAsync()).GetProperty("code").GetString().Should().Be("billing.same_period");
    }

    [Fact]
    public async Task Mensal_para_anual_no_teste_gratis_troca_sem_valor_e_fica_registrado()
    {
        var c = await NewUser();
        var sub = await (await c.PostAsJsonAsync("/api/v1/billing/subscribe", new { period = "monthly" })).JsonAsync();
        var trialEnds = sub.GetProperty("trialEndsAt").GetDateTime();

        var res = await c.PostAsJsonAsync("/api/v1/billing/subscription/period", new { period = "yearly" });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var j = await res.JsonAsync();
        j.GetProperty("billing").GetProperty("plan").GetString().Should().Be("plus");
        j.GetProperty("billing").GetProperty("period").GetString().Should().Be("yearly");
        j.GetProperty("billing").GetProperty("renewsAt").GetDateTime().Should().Be(trialEnds);
        var p = j.GetProperty("proration");
        p.GetProperty("from").GetString().Should().Be("monthly");
        p.GetProperty("to").GetString().Should().Be("yearly");
        p.GetProperty("duringTrial").GetBoolean().Should().BeTrue();
        p.GetProperty("amount").GetDecimal().Should().Be(0);
        p.GetProperty("demo").GetBoolean().Should().BeTrue();

        // E volta para o mensal.
        (await c.PostAsJsonAsync("/api/v1/billing/subscription/period", new { period = "monthly" })).StatusCode.Should().Be(HttpStatusCode.OK);

        (await (await c.GetAsync("/api/v1/audit-events")).Content.ReadAsStringAsync()).Should().Contain("plan.period_changed");
        var export = await (await c.GetAsync("/api/v1/me/export")).JsonAsync();
        export.GetProperty("billing").GetProperty("periodChanges").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Assinante_Plus_compra_pacote_e_os_creditos_se_somam()
    {
        var c = await NewUser();
        (await c.PostAsJsonAsync("/api/v1/billing/subscribe", new { period = "yearly" })).EnsureSuccessStatusCode();

        (await c.PostAsync("/api/v1/billing/packs", null)).StatusCode.Should().Be(HttpStatusCode.Created);
        var second = await c.PostAsync("/api/v1/billing/packs", null);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        var b = await second.JsonAsync();
        b.GetProperty("plan").GetString().Should().Be("plus");
        b.GetProperty("packs").GetArrayLength().Should().Be(2);
        b.GetProperty("packTotals").GetProperty("assistedRemaining").GetInt32().Should().Be(30);
        b.GetProperty("packTotals").GetProperty("aiRemaining").GetInt32().Should().Be(20);

        // No Plus o uso sai do plano (sem limite); os pacotes continuam intactos depois de cancelar.
        (await c.PostAsJsonAsync("/api/v1/forms/mappings", new { pageUrl = "https://vagas.exemplo.com/1", fields = new[] { new { id = "a", label = "Nome" } } }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var canceled = await (await c.PostAsync("/api/v1/billing/cancel", null)).JsonAsync();
        canceled.GetProperty("plan").GetString().Should().Be("free");
        canceled.GetProperty("packTotals").GetProperty("assistedRemaining").GetInt32().Should().Be(30);
    }
}
