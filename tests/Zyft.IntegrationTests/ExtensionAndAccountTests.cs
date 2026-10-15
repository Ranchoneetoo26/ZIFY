using System.Net;
using System.Net.Http.Json;
using Zyft.IntegrationTests.Support;

namespace Zyft.IntegrationTests;

public class ExtensionAndAccountTests(ZyftApiFactory factory) : IClassFixture<ZyftApiFactory>
{
    private async Task<(HttpClient Web, string ExtAccess, string ExtRefresh, Guid DeviceId)> Paired()
    {
        var web = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var codeRes = await web.PostAsync("/api/v1/extension/pairing-codes", null);
        codeRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var code = (await codeRes.JsonAsync()).GetProperty("code").GetString()!;
        code.Should().MatchRegex(@"^\d{6}$");
        var pair = await factory.CreateClient().PostAsJsonAsync("/api/v1/extension/pair", new { code, deviceName = "Chrome do notebook" });
        pair.StatusCode.Should().Be(HttpStatusCode.OK);
        var j = await pair.JsonAsync();
        j.GetProperty("tokens").GetProperty("scope").GetString().Should().Be("extension");
        return (web, j.GetProperty("tokens").GetProperty("accessToken").GetString()!, j.GetProperty("tokens").GetProperty("refreshToken").GetString()!, j.GetProperty("device").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Codigo_de_pareamento_e_de_uso_unico()
    {
        var web = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var code = (await (await web.PostAsync("/api/v1/extension/pairing-codes", null)).JsonAsync()).GetProperty("code").GetString();
        var anon = factory.CreateClient();
        (await anon.PostAsJsonAsync("/api/v1/extension/pair", new { code, deviceName = "A" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await anon.PostAsJsonAsync("/api/v1/extension/pair", new { code, deviceName = "B" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_de_extensao_nao_acessa_exportacao_nem_perfil_inteiro()
    {
        var (_, ext, _, _) = await Paired();
        var c = factory.Authed(ext);
        (await c.GetAsync("/api/v1/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        var export = await c.GetAsync("/api/v1/me/export");
        export.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await export.JsonAsync()).GetProperty("status").GetInt32().Should().Be(403);
        (await c.GetAsync("/api/v1/profile")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await c.DeleteAsync("/api/v1/me")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await c.PutAsJsonAsync("/api/v1/consents/SensitiveData", new { granted = true })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await c.PostAsJsonAsync("/api/v1/forms/mappings", new { fields = new[] { new { id = "a", label = "Nome" } } })).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Revogar_extensao_derruba_access_e_refresh_na_hora()
    {
        var (web, ext, refresh, deviceId) = await Paired();
        (await web.DeleteAsync($"/api/v1/extension/devices/{deviceId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await factory.Authed(ext).GetAsync("/api/v1/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = refresh })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var devices = await (await web.GetAsync("/api/v1/extension/devices")).JsonAsync();
        devices[0].GetProperty("active").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Extensao_pode_autorizar_site_mas_nao_remover()
    {
        var (web, ext, _, _) = await Paired();
        var c = factory.Authed(ext);
        var r = await c.PostAsJsonAsync("/api/v1/authorized-sites", new { origin = "https://vagas.exemplo.com/form" });
        r.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await r.JsonAsync()).GetProperty("id").GetGuid();
        (await c.GetAsync("/api/v1/authorized-sites")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await c.DeleteAsync($"/api/v1/authorized-sites/{id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await web.DeleteAsync($"/api/v1/authorized-sites/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_de_extensao_mantem_scope_extension()
    {
        var (_, _, refresh, _) = await Paired();
        var j = await (await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = refresh })).JsonAsync();
        j.GetProperty("scope").GetString().Should().Be("extension");
    }

    [Fact]
    public async Task Conclusao_cria_candidatura_e_auditoria_sem_valores()
    {
        var (web, ext, _, _) = await Paired();
        var c = factory.Authed(ext);
        await c.PostAsJsonAsync("/api/v1/forms/mappings", new { pageUrl = "https://vagas.exemplo.com/candidatura/1?cpf=123", fields = new[] { new { id = "a", label = "Nome" } } });
        var created = await c.PostAsJsonAsync("/api/v1/forms/completions", new { portal = "https://vagas.exemplo.com/x", jobTitle = "Assistente", company = "Z", fieldsFilled = 12 });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var app = await created.JsonAsync();
        app.GetProperty("status").GetString().Should().Be("Confirmed");
        app.GetProperty("portal").GetString().Should().Be("vagas.exemplo.com");

        var id = app.GetProperty("id").GetGuid();
        (await (await web.PatchAsJsonAsync($"/api/v1/applications/{id}", new { status = "Interview", notes = "Entrevista sexta" })).JsonAsync())
            .GetProperty("status").GetString().Should().Be("Interview");
        (await c.PatchAsJsonAsync($"/api/v1/applications/{id}", new { status = "Hired" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var audit = await (await web.GetAsync("/api/v1/audit-events")).Content.ReadAsStringAsync();
        audit.Should().Contain("candidatura.confirmada").And.Contain("formulario.mapeado").And.Contain("vagas.exemplo.com");
        audit.Should().NotContain("cpf=123").And.NotContain("/candidatura/");
    }

    [Fact]
    public async Task Exportacao_traz_os_dados_e_exclusao_apaga_tudo()
    {
        var c = factory.CreateClient();
        var s = await TestApi.RegisterAsync(c);
        var web = factory.Authed(s.AccessToken);
        await web.PostAsJsonAsync("/api/v1/profile/skills", new { name = "Excel" });
        await web.PostAsJsonAsync("/api/v1/answers", new { question = "Q?", text = "A." });

        var export = await web.GetAsync("/api/v1/me/export");
        export.StatusCode.Should().Be(HttpStatusCode.OK);
        export.Content.Headers.ContentDisposition!.FileName.Should().Contain("zyft-meus-dados.json");
        var j = await export.JsonAsync();
        j.GetProperty("profile").GetProperty("skills")[0].GetProperty("name").GetString().Should().Be("Excel");
        j.GetProperty("approvedAnswers").GetArrayLength().Should().Be(1);
        j.GetProperty("consents").GetArrayLength().Should().Be(3);
        j.GetProperty("auditEvents").GetArrayLength().Should().BeGreaterThan(0);

        (await web.DeleteAsync("/api/v1/me")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await c.PostAsJsonAsync("/api/v1/auth/login", new { email = s.Email, password = "Senha123" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await c.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = s.RefreshToken })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        // Mesmo e-mail pode criar conta nova (nada sobrou).
        (await c.PostAsJsonAsync("/api/v1/auth/register", new { name = "De novo", email = s.Email, password = "Senha123" })).StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
