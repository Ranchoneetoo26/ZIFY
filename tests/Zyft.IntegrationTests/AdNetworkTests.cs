using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Zyft.IntegrationTests.Support;

namespace Zyft.IntegrationTests;

/// <summary>Rede de anúncios (Google AdSense simulado, ADR-0021): formato "network", Plus, ocultar e espaços proibidos.</summary>
public class AdNetworkTests(ZyftApiFactory factory) : IClassFixture<ZyftApiFactory>
{
    private async Task<(HttpClient Web, TestApi.Session S)> NewUser()
    {
        var s = await TestApi.RegisterAsync(factory.CreateClient());
        return (factory.Authed(s.AccessToken), s);
    }

    [Fact]
    public async Task Banner_web_vem_da_rede_simulada_sem_dados_do_perfil()
    {
        var (c, s) = await NewUser();
        await c.PutAsJsonAsync("/api/v1/profile", new { fullName = "Rede Privada Souza", phone = "(31) 97766-5544", workArea = "Saúde" });

        var res = await c.GetAsync("/api/v1/ads?placement=web.banner");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().NotContain("Rede Privada").And.NotContain("97766").And.NotContain(s.Email);

        var ad = JsonDocument.Parse(body).RootElement;
        ad.GetProperty("format").GetString().Should().Be("network");
        ad.GetProperty("placement").GetString().Should().Be("web.banner");
        ad.GetProperty("id").GetString().Should().StartWith("adsense-");
        ad.GetProperty("reason").GetString().Should().Contain("simulação");
        var net = ad.GetProperty("network");
        net.GetProperty("client").GetString().Should().StartWith("ca-pub-");
        net.GetProperty("slot").GetString().Should().NotBeNullOrWhiteSpace();
        net.GetProperty("adFormat").GetString().Should().Be("horizontal");
        net.GetProperty("mock").GetBoolean().Should().BeTrue();
        ad.GetProperty("id").GetString().Should().Be("adsense-" + net.GetProperty("slot").GetString());

        // Clique da rede não é contado pelo Zyft, mas a rota responde sem erro.
        (await c.PostAsync($"/api/v1/ads/{ad.GetProperty("id").GetString()}/click", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Plus_nunca_recebe_anuncio_da_rede()
    {
        var (c, _) = await NewUser();
        (await c.PostAsJsonAsync("/api/v1/billing/subscribe", new { period = "monthly" })).StatusCode.Should().Be(HttpStatusCode.OK);
        foreach (var placement in new[] { "web.banner", "web.profile.rail", "web.applications.feed" })
            (await c.GetAsync($"/api/v1/ads?placement={placement}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Ocultar_bloco_da_rede_cai_para_anuncio_direto_ou_204()
    {
        var (c, _) = await NewUser();
        await c.PutAsJsonAsync("/api/v1/profile", new { workArea = "Tecnologia" });
        var ad = await (await c.GetAsync("/api/v1/ads?placement=web.banner")).JsonAsync();
        var id = ad.GetProperty("id").GetString()!;
        id.Should().StartWith("adsense-");

        (await c.PostAsJsonAsync($"/api/v1/ads/{id}/hide", new { scope = "ad" })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var next = await c.GetAsync("/api/v1/ads?placement=web.banner");
        next.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        if (next.StatusCode == HttpStatusCode.OK)
        {
            var direct = await next.JsonAsync();
            direct.GetProperty("format").GetString().Should().NotBe("network");
            direct.GetProperty("id").GetString().Should().NotStartWith("adsense-");
        }

        (await (await c.GetAsync("/api/v1/audit-events")).Content.ReadAsStringAsync()).Should().Contain("ad.hidden");
    }

    [Theory]
    [InlineData("web.onboarding")]
    [InlineData("web.sensitive-data")]
    [InlineData("web.account")]
    [InlineData("ext.form.review")]
    public async Task Rede_nunca_preenche_espaco_proibido(string placement)
    {
        var (c, _) = await NewUser();
        (await c.GetAsync($"/api/v1/ads?placement={placement}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
