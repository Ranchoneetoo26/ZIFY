using System.Net;
using System.Net.Http.Json;
using Zyft.IntegrationTests.Support;

namespace Zyft.IntegrationTests;

/// <summary>Verificação de e-mail, termos de uso, troca e recuperação de senha pela API real.</summary>
public class AccountSecurityTests(ZyftApiFactory factory) : IClassFixture<ZyftApiFactory>
{
    // ------------------------------------------------------------------ verificação de e-mail

    [Fact]
    public async Task Cadastro_envia_link_e_verificar_marca_o_email_como_verificado()
    {
        var c = factory.CreateClient();
        var s = await TestApi.RegisterAsync(c);
        var web = factory.Authed(s.AccessToken);
        (await (await web.GetAsync("/api/v1/me")).JsonAsync()).GetProperty("emailVerified").GetBoolean().Should().BeFalse();

        var token = factory.Emails.LastToken(s.Email, "verify");
        (await c.PostAsJsonAsync("/api/v1/auth/verify", new { token })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var me = await (await web.GetAsync("/api/v1/me")).JsonAsync();
        me.GetProperty("emailVerified").GetBoolean().Should().BeTrue();
        me.TryGetProperty("emailVerifiedAt", out _).Should().BeTrue();

        // Uso único.
        var again = await c.PostAsJsonAsync("/api/v1/auth/verify", new { token });
        again.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await again.JsonAsync()).GetProperty("errors").GetProperty("token")[0].GetString().Should().Contain("inválido ou expirou");

        // Já verificado: login devolve emailVerified=true e reenviar é 409.
        var login = await (await c.PostAsJsonAsync("/api/v1/auth/login", new { email = s.Email, password = "Senha123" })).JsonAsync();
        login.GetProperty("user").GetProperty("emailVerified").GetBoolean().Should().BeTrue();
        var resend = await web.PostAsync("/api/v1/auth/verify/resend", null);
        resend.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await resend.JsonAsync()).GetProperty("code").GetString().Should().Be("email.already_verified");
    }

    [Fact]
    public async Task Email_nao_verificado_nao_bloqueia_rotas()
    {
        var s = await TestApi.RegisterAsync(factory.CreateClient());
        var web = factory.Authed(s.AccessToken);
        (await web.GetAsync("/api/v1/profile")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await web.PutAsJsonAsync("/api/v1/profile", new { fullName = "Pessoa" })).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Token_invalido_e_400_e_reenviar_exige_login_e_troca_o_link()
    {
        var c = factory.CreateClient();
        (await c.PostAsJsonAsync("/api/v1/auth/verify", new { token = "nao-existe" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var empty = await c.PostAsJsonAsync("/api/v1/auth/verify", new { token = "" });
        (await empty.JsonAsync()).GetProperty("errors").GetProperty("token")[0].GetString().Should().Be("Informe o código de verificação.");
        (await c.PostAsync("/api/v1/auth/verify/resend", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var s = await TestApi.RegisterAsync(c);
        var first = factory.Emails.LastToken(s.Email, "verify");
        (await factory.Authed(s.AccessToken).PostAsync("/api/v1/auth/verify/resend", null)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        var second = factory.Emails.LastToken(s.Email, "verify");
        second.Should().NotBe(first);

        (await c.PostAsJsonAsync("/api/v1/auth/verify", new { token = first })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await c.PostAsJsonAsync("/api/v1/auth/verify", new { token = second })).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Reenviar_tem_limite_de_tentativas()
    {
        using var f = new ZyftApiFactory { AuthPermitLimit = 3 };
        var s = await TestApi.RegisterAsync(f.CreateClient()); // 1 do limite (mesmo IP)
        var web = f.Authed(s.AccessToken);
        HttpResponseMessage? last = null;
        for (var i = 0; i < 3; i++) last = await web.PostAsync("/api/v1/auth/verify/resend", null);
        last!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Logs_do_link_nao_contem_o_email()
    {
        var email = $"verifica{Guid.NewGuid():N}@segredo.com";
        await TestApi.RegisterAsync(factory.CreateClient(), email);
        var token = factory.Emails.LastToken(email, "verify");
        var all = string.Join('\n', factory.Logs.Lines);
        all.Should().Contain("/#/verificar?token=" + token);
        all.Should().NotContain(email);
    }

    // ------------------------------------------------------------------ aceite dos termos

    [Fact]
    public async Task Cadastro_sem_aceite_dos_termos_e_400_pt_BR()
    {
        var c = factory.CreateClient();
        var email = $"termos{Guid.NewGuid():N}@exemplo.com";
        var res = await c.PostAsJsonAsync("/api/v1/auth/register", new { name = "Pessoa", email, password = "Senha123" });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await res.JsonAsync()).GetProperty("errors").GetProperty("acceptTerms")[0].GetString().Should().Be("Aceite os termos de uso e a política de privacidade.");

        var no = await c.PostAsJsonAsync("/api/v1/auth/register", new { name = "Pessoa", email, password = "Senha123", acceptTerms = false, termsVersion = TestApi.TermsVersion });
        no.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var old = await c.PostAsJsonAsync("/api/v1/auth/register", new { name = "Pessoa", email, password = "Senha123", acceptTerms = true, termsVersion = "2020-01-01" });
        old.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await old.JsonAsync()).GetProperty("errors").GetProperty("termsVersion")[0].GetString().Should().StartWith("Os termos foram atualizados");

        var missing = await c.PostAsJsonAsync("/api/v1/auth/register", new { name = "Pessoa", email, password = "Senha123", acceptTerms = true });
        (await missing.JsonAsync()).GetProperty("errors").GetProperty("termsVersion")[0].GetString().Should().Be("Informe a versão dos termos aceita.");
    }

    [Fact]
    public async Task Cadastro_com_aceite_guarda_versao_data_e_auditoria()
    {
        var c = factory.CreateClient();
        (await (await c.GetAsync("/api/v1/auth/terms")).JsonAsync()).GetProperty("version").GetString().Should().Be(TestApi.TermsVersion);

        var s = await TestApi.RegisterAsync(c);
        var web = factory.Authed(s.AccessToken);
        var me = await (await web.GetAsync("/api/v1/me")).JsonAsync();
        me.GetProperty("termsVersion").GetString().Should().Be(TestApi.TermsVersion);
        me.GetProperty("termsAcceptedAt").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        var audit = await (await web.GetAsync("/api/v1/audit-events")).Content.ReadAsStringAsync();
        audit.Should().Contain("termos.aceitos");
    }

    // ------------------------------------------------------------------ troca de senha

    private async Task<string> PairExtensionAsync(HttpClient web)
    {
        var code = (await (await web.PostAsync("/api/v1/extension/pairing-codes", null)).JsonAsync()).GetProperty("code").GetString()!;
        var pair = await (await factory.CreateClient().PostAsJsonAsync("/api/v1/extension/pair", new { code, deviceName = "Chrome" })).JsonAsync();
        return pair.GetProperty("tokens").GetProperty("accessToken").GetString()!;
    }

    [Fact]
    public async Task Trocar_senha_valida_a_atual_e_a_nova()
    {
        var s = await TestApi.RegisterAsync(factory.CreateClient());
        var web = factory.Authed(s.AccessToken);

        var wrong = await web.PostAsJsonAsync("/api/v1/auth/password", new { currentPassword = "Errada123", newPassword = "NovaSenha456" });
        wrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await wrong.JsonAsync()).GetProperty("errors").GetProperty("currentPassword")[0].GetString().Should().Be("Senha atual incorreta.");

        var weak = await web.PostAsJsonAsync("/api/v1/auth/password", new { currentPassword = "Senha123", newPassword = "curta" });
        (await weak.JsonAsync()).GetProperty("errors").GetProperty("newPassword").EnumerateArray().Select(e => e.GetString())
            .Should().Contain("A senha deve ter pelo menos 8 caracteres.");

        var same = await web.PostAsJsonAsync("/api/v1/auth/password", new { currentPassword = "Senha123", newPassword = "Senha123" });
        same.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await same.JsonAsync()).GetProperty("errors").GetProperty("newPassword")[0].GetString().Should().Be("A nova senha deve ser diferente da senha atual.");

        (await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/password", new { currentPassword = "Senha123", newPassword = "NovaSenha456" }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Trocar_senha_encerra_as_outras_sessoes_e_mantem_a_atual()
    {
        var c = factory.CreateClient();
        var s = await TestApi.RegisterAsync(c);
        var other = await (await c.PostAsJsonAsync("/api/v1/auth/login", new { email = s.Email, password = "Senha123" })).JsonAsync();
        var web = factory.Authed(s.AccessToken);
        var extAccess = await PairExtensionAsync(web);

        var res = await web.PostAsJsonAsync("/api/v1/auth/password", new { currentPassword = "Senha123", newPassword = "NovaSenha456" });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var fresh = await res.JsonAsync();
        var freshRefresh = fresh.GetProperty("refreshToken").GetString()!;

        // Outras sessões: refresh revogado; extensão desconectada na hora.
        (await c.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = other.GetProperty("refreshToken").GetString() })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await c.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = s.RefreshToken })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await factory.Authed(extAccess).GetAsync("/api/v1/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Sessão atual continua: novo par funciona.
        (await factory.Authed(fresh.GetProperty("accessToken").GetString()!).GetAsync("/api/v1/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await c.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = freshRefresh })).StatusCode.Should().Be(HttpStatusCode.OK);

        // Senha nova vale, antiga não.
        (await c.PostAsJsonAsync("/api/v1/auth/login", new { email = s.Email, password = "Senha123" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await c.PostAsJsonAsync("/api/v1/auth/login", new { email = s.Email, password = "NovaSenha456" })).StatusCode.Should().Be(HttpStatusCode.OK);

        var audit = await (await web.GetAsync("/api/v1/audit-events")).Content.ReadAsStringAsync();
        audit.Should().Contain("senha.alterada");
        string.Join('\n', factory.Logs.Lines).Should().NotContain("NovaSenha456");
    }
}
