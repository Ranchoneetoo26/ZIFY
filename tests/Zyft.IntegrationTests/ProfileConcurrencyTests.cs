using System.Net;
using System.Net.Http.Json;
using Zyft.IntegrationTests.Support;

namespace Zyft.IntegrationTests;

/// <summary>Concorrência otimista do Perfil Mestre: ETag no GET, If-Match nas alterações, 409 profile.conflict.</summary>
public class ProfileConcurrencyTests(ZyftApiFactory factory) : IClassFixture<ZyftApiFactory>
{
    private async Task<(HttpClient A, HttpClient B)> TwoSessionsAsync()
    {
        var c = factory.CreateClient();
        var s = await TestApi.RegisterAsync(c);
        var login = await (await c.PostAsJsonAsync("/api/v1/auth/login", new { email = s.Email, password = "Senha123" })).JsonAsync();
        return (factory.Authed(s.AccessToken), factory.Authed(login.GetProperty("accessToken").GetString()!));
    }

    private static HttpRequestMessage Put(string url, object body, string? ifMatch)
    {
        var req = new HttpRequestMessage(HttpMethod.Put, url) { Content = JsonContent.Create(body) };
        if (ifMatch is not null) req.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        return req;
    }

    [Fact]
    public async Task GET_devolve_ETag_igual_a_version_do_corpo()
    {
        var (a, _) = await TwoSessionsAsync();
        var res = await a.GetAsync("/api/v1/profile");
        var version = (await res.JsonAsync()).GetProperty("version").GetInt32();
        res.Headers.ETag!.Tag.Should().Be($"\"{version}\"");
    }

    [Fact]
    public async Task Duas_sessoes_a_segunda_com_versao_velha_recebe_409_com_a_versao_atual()
    {
        var (a, b) = await TwoSessionsAsync();
        var etagA = (await a.GetAsync("/api/v1/profile")).Headers.ETag!.Tag;
        var etagB = (await b.GetAsync("/api/v1/profile")).Headers.ETag!.Tag;
        etagA.Should().Be(etagB);

        var saveA = await a.SendAsync(Put("/api/v1/profile", new { fullName = "Sessão A", city = "Maringá" }, etagA));
        saveA.StatusCode.Should().Be(HttpStatusCode.OK);
        var newVersion = (await saveA.JsonAsync()).GetProperty("version").GetInt32();
        saveA.Headers.ETag!.Tag.Should().Be($"\"{newVersion}\"").And.NotBe(etagA);

        var saveB = await b.SendAsync(Put("/api/v1/profile", new { fullName = "Sessão B" }, etagB));
        saveB.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await saveB.JsonAsync();
        problem.GetProperty("code").GetString().Should().Be("profile.conflict");
        problem.GetProperty("currentVersion").GetInt32().Should().Be(newVersion);
        problem.GetProperty("detail").GetString().Should().Contain("outra sessão");

        // Nada da sessão B foi gravado.
        (await (await a.GetAsync("/api/v1/profile")).JsonAsync()).GetProperty("fullName").GetString().Should().Be("Sessão A");

        // Com a versão nova, B consegue salvar.
        (await b.SendAsync(Put("/api/v1/profile", new { fullName = "Sessão B" }, $"W/\"{newVersion}\""))).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Sem_If_Match_continua_salvando_como_antes()
    {
        var (a, b) = await TwoSessionsAsync();
        (await a.PutAsJsonAsync("/api/v1/profile", new { fullName = "A" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await b.PutAsJsonAsync("/api/v1/profile", new { fullName = "B" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await a.SendAsync(Put("/api/v1/profile", new { fullName = "C" }, "*"))).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Itens_do_perfil_sobem_a_versao_e_respeitam_If_Match()
    {
        var (a, b) = await TwoSessionsAsync();
        var etag = (await a.GetAsync("/api/v1/profile")).Headers.ETag!.Tag;

        var addA = await a.PostAsJsonAsync("/api/v1/profile/skills", new { name = "SQL" });
        addA.StatusCode.Should().Be(HttpStatusCode.Created);
        addA.Headers.ETag!.Tag.Should().NotBe(etag);

        var stale = new HttpRequestMessage(HttpMethod.Post, "/api/v1/profile/experiences")
        {
            Content = JsonContent.Create(new { company = "Empresa Y", role = "Assistente", startDate = "2023-07", isCurrent = true }),
        };
        stale.Headers.TryAddWithoutValidation("If-Match", etag);
        var res = await b.SendAsync(stale);
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await res.JsonAsync()).GetProperty("code").GetString().Should().Be("profile.conflict");
        (await (await a.GetAsync("/api/v1/profile/experiences")).JsonAsync()).GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task If_Match_mal_formado_e_400()
    {
        var (a, _) = await TwoSessionsAsync();
        var res = await a.SendAsync(Put("/api/v1/profile", new { fullName = "A" }, "\"abc\""));
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await res.JsonAsync()).GetProperty("detail").GetString().Should().StartWith("Cabeçalho If-Match inválido");
    }
}
