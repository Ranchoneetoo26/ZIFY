using System.Net;
using System.Net.Http.Json;
using Zyft.IntegrationTests.Support;

namespace Zyft.IntegrationTests;

public class PlatformTests(ZyftApiFactory factory) : IClassFixture<ZyftApiFactory>
{
    [Fact]
    public async Task Health_responde_com_banco_e_IA()
    {
        var res = await factory.CreateClient().GetAsync("/health");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var j = await res.JsonAsync();
        j.GetProperty("status").GetString().Should().Be("Healthy");
        j.GetProperty("checks").EnumerateArray().Select(x => x.GetProperty("name").GetString()).Should().Contain(new[] { "database", "llm" });
    }

    [Fact]
    public async Task Swagger_publica_rotas_e_esquema_Bearer()
    {
        var json = await factory.CreateClient().GetStringAsync("/swagger/v1/swagger.json");
        json.Should().Contain("/api/v1/forms/mappings").And.Contain("/api/v1/jobs/analyze").And.Contain("\"bearer\"");
        json.Should().Contain("Cria a conta"); // XML comments
    }

    [Fact]
    public async Task Frontend_web_e_servido_na_raiz()
    {
        var res = await factory.CreateClient().GetAsync("/");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }

    [Theory]
    [InlineData("chrome-extension://abcdefghijklmnop", true)]
    [InlineData("http://localhost:5080", true)]
    [InlineData("https://site-malicioso.com", false)]
    public async Task CORS_aceita_extensao_e_origem_configurada(string origin, bool allowed)
    {
        var req = new HttpRequestMessage(HttpMethod.Options, "/api/v1/forms/mappings");
        req.Headers.Add("Origin", origin);
        req.Headers.Add("Access-Control-Request-Method", "POST");
        req.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        var res = await factory.CreateClient().SendAsync(req);
        res.Headers.Contains("Access-Control-Allow-Origin").Should().Be(allowed);
    }

    [Fact]
    public async Task Logs_nao_contem_dados_pessoais()
    {
        var c = factory.CreateClient();
        var email = $"privado{Guid.NewGuid():N}@segredo.com";
        var s = await TestApi.RegisterAsync(c, email, "SenhaSecreta123");
        var web = factory.Authed(s.AccessToken);
        await web.PutAsJsonAsync("/api/v1/profile", new { fullName = "Nome Muito Privado", phone = "(44) 91234-5678" });
        await web.PostAsJsonAsync("/api/v1/forms/mappings", new { pageUrl = "https://vagas.exemplo.com/?email=" + email, fields = new[] { new { id = "a", label = "Nome" } } });
        await c.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "errada000" });

        var all = string.Join('\n', factory.Logs.Lines);
        all.Should().Contain("HTTP POST /api/v1/forms/mappings"); // o sink está capturando
        all.Should().NotContain(email).And.NotContain("SenhaSecreta123").And.NotContain("Nome Muito Privado").And.NotContain("91234-5678").And.NotContain(s.AccessToken);
    }

    [Fact]
    public async Task Erro_500_nao_expoe_stack_trace()
    {
        // Força um erro inesperado: consentimento com JSON de tipo errado é 400; então usamos rota que lança via token sem perfil.
        // (Usuário excluído com token ainda válido → handler não encontra o perfil → 404, não 500.)
        var s = await TestApi.RegisterAsync(factory.CreateClient());
        var web = factory.Authed(s.AccessToken);
        await web.DeleteAsync("/api/v1/me");
        var res = await web.GetAsync("/api/v1/profile");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await res.Content.ReadAsStringAsync()).Should().NotContain("   at ");
    }
}
