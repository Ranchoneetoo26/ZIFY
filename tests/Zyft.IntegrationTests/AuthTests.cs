using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Zyft.IntegrationTests.Support;

namespace Zyft.IntegrationTests;

public class AuthTests(ZyftApiFactory factory) : IClassFixture<ZyftApiFactory>
{
    [Fact]
    public async Task Cadastro_devolve_201_com_tokens_e_scope_web()
    {
        var c = factory.CreateClient();
        var res = await c.PostAsJsonAsync("/api/v1/auth/register", new { name = "Ana", email = $"ana{Guid.NewGuid():N}@x.com", password = "Senha123" });
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var j = await res.JsonAsync();
        j.GetProperty("scope").GetString().Should().Be("web");
        j.GetProperty("expiresIn").GetInt32().Should().BeInRange(890, 900);
        j.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Cadastro_duplicado_e_409_ProblemDetails_pt_BR()
    {
        var c = factory.CreateClient();
        var s = await TestApi.RegisterAsync(c);
        var res = await c.PostAsJsonAsync("/api/v1/auth/register", new { name = "Outra", email = s.Email.ToUpperInvariant(), password = "Senha123" });
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
        res.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var j = await res.JsonAsync();
        j.GetProperty("status").GetInt32().Should().Be(409);
        j.GetProperty("detail").GetString().Should().Be("Já existe uma conta com este e-mail.");
        j.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Cadastro_invalido_e_400_com_errors_por_campo()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new { name = "", email = "nao-e-email", password = "fraca" });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await res.JsonAsync()).GetProperty("errors");
        errors.GetProperty("email")[0].GetString().Should().Be("Informe um e-mail válido.");
        errors.GetProperty("password").EnumerateArray().Select(e => e.GetString()).Should().Contain("A senha deve ter pelo menos 8 caracteres.");
        errors.GetProperty("name")[0].GetString().Should().Be("Informe o nome.");
    }

    [Fact]
    public async Task JSON_malformado_e_400_pt_BR()
    {
        var content = new StringContent("{ isso não é json", System.Text.Encoding.UTF8, "application/json");
        var res = await factory.CreateClient().PostAsync("/api/v1/auth/login", content);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await res.JsonAsync()).GetProperty("detail").GetString().Should().Be("Um ou mais campos são inválidos.");
    }

    [Fact]
    public async Task Login_errado_e_401_e_rota_protegida_sem_token_e_401()
    {
        var c = factory.CreateClient();
        var s = await TestApi.RegisterAsync(c);
        var res = await c.PostAsJsonAsync("/api/v1/auth/login", new { email = s.Email, password = "Errada123" });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await res.JsonAsync()).GetProperty("detail").GetString().Should().Be("E-mail ou senha incorretos.");

        var anon = await c.GetAsync("/api/v1/profile");
        anon.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anon.JsonAsync()).GetProperty("code").GetString().Should().Be("unauthorized");
    }

    [Fact]
    public async Task Token_adulterado_e_401()
    {
        var s = await TestApi.RegisterAsync(factory.CreateClient());
        var tampered = s.AccessToken[..^3] + (s.AccessToken.EndsWith("abc") ? "xyz" : "abc");
        (await factory.Authed(tampered).GetAsync("/api/v1/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_rotaciona_e_reuso_revoga_a_cadeia()
    {
        var c = factory.CreateClient();
        var s = await TestApi.RegisterAsync(c);

        var r1 = await c.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = s.RefreshToken });
        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        var newRefresh = (await r1.JsonAsync()).GetProperty("refreshToken").GetString();
        newRefresh.Should().NotBe(s.RefreshToken);

        // Reuso do token antigo: 401 e o sucessor também deixa de valer.
        var reuse = await c.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = s.RefreshToken });
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await reuse.JsonAsync()).GetProperty("detail").GetString().Should().Contain("token reutilizado");
        (await c.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = newRefresh })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_revoga_o_refresh_token()
    {
        var c = factory.CreateClient();
        var s = await TestApi.RegisterAsync(c);
        (await c.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = s.RefreshToken })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await c.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = s.RefreshToken })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public class RateLimitTests
{
    [Fact]
    public async Task Rotas_de_IA_tem_limite_por_pessoa_e_nao_por_IP()
    {
        using var f = new ZyftApiFactory { AiPermitPerMinute = 2 };
        var a = f.Authed((await TestApi.RegisterAsync(f.CreateClient())).AccessToken);
        var b = f.Authed((await TestApi.RegisterAsync(f.CreateClient())).AccessToken);
        var body = new { fields = new[] { new { id = "a", label = "Nome" } } };
        (await a.PostAsJsonAsync("/api/v1/forms/mappings", body)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await a.PostAsJsonAsync("/api/v1/forms/mappings", body)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await a.PostAsJsonAsync("/api/v1/forms/mappings", body)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        // Outra pessoa (mesmo IP no TestServer) não é afetada.
        (await b.PostAsJsonAsync("/api/v1/forms/mappings", body)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_acima_do_limite_e_429_com_Retry_After()
    {
        using var f = new ZyftApiFactory { AuthPermitLimit = 3 };
        var c = f.CreateClient();
        HttpResponseMessage? last = null;
        for (var i = 0; i < 4; i++) last = await c.PostAsJsonAsync("/api/v1/auth/login", new { email = "x@y.com", password = "Senha123" });
        last!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        last.Headers.RetryAfter.Should().NotBeNull();
        (await last.JsonAsync()).GetProperty("detail").GetString().Should().StartWith("Muitas tentativas");
    }
}
