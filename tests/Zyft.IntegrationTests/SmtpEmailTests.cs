using System.Net;
using System.Net.Http.Json;
using Zyft.IntegrationTests.Support;
using Zyft.UnitTests.Support;

namespace Zyft.IntegrationTests;

/// <summary>API real com envio por SMTP (<c>Email:Smtp:*</c>) contra um servidor SMTP falso em processo, sem Docker.</summary>
public class SmtpEmailTests
{
    private static Dictionary<string, string?> Smtp(int port) => new()
    {
        ["Email:Smtp:Host"] = "127.0.0.1",
        ["Email:Smtp:Port"] = port.ToString(),
        ["Email:Smtp:EnableSsl"] = "false",
        ["Email:Smtp:FromAddress"] = "nao-responda@zyft.local",
        ["Email:Smtp:TimeoutSeconds"] = "5",
        ["App:WebBaseUrl"] = "https://zyft.exemplo",
    };

    [Fact]
    public async Task Cadastro_e_esqueci_a_senha_entregam_os_emails_com_links_validos()
    {
        await using var smtp = new SmtpStub();
        using var f = new ZyftApiFactory { Settings = Smtp(smtp.Port) };
        var c = f.CreateClient();

        var s = await TestApi.RegisterAsync(c);
        (await c.PostAsJsonAsync("/api/v1/auth/password/forgot", new { email = s.Email })).StatusCode.Should().Be(HttpStatusCode.Accepted);

        var msgs = await smtp.WaitForAsync(2);
        msgs.Should().HaveCount(2);
        msgs.Should().OnlyContain(m => m.RcptTo.Single() == $"<{s.Email}>");

        var verifyToken = f.Emails.LastToken(s.Email, "verify");
        var resetToken = f.Emails.LastToken(s.Email, "reset");
        msgs[0].Header("Subject").Should().Be("Confirme seu e-mail no Zyft");
        msgs[0].Parts()["text/plain"].Should().Contain($"https://zyft.exemplo/#/verificar?token={Uri.EscapeDataString(verifyToken)}");
        msgs[1].Header("Subject").Should().Be("Redefinir sua senha do Zyft");
        msgs[1].Parts()["text/html"].Should().Contain($"https://zyft.exemplo/#/redefinir-senha?token={Uri.EscapeDataString(resetToken)}");

        // Os links funcionam: confirmar o e-mail e redefinir a senha.
        (await c.PostAsJsonAsync("/api/v1/auth/verify", new { token = verifyToken })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await c.PostAsJsonAsync("/api/v1/auth/password/reset", new { token = resetToken, newPassword = "Redefinida789" })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var logs = string.Join('\n', f.Logs.Lines);
        logs.Should().Contain("E-mail de verificação enviado.").And.Contain("E-mail de redefinição de senha enviado.");
        logs.Should().NotContain(s.Email).And.NotContain(verifyToken).And.NotContain(resetToken).And.NotContain("Pessoa Teste");
    }

    [Fact]
    public async Task SMTP_fora_do_ar_nao_quebra_o_cadastro_e_esqueci_a_senha_responde_igual()
    {
        using var f = new ZyftApiFactory { Settings = Smtp(SmtpStub.ClosedPort()) };
        var c = f.CreateClient();

        var s = await TestApi.RegisterAsync(c); // EnsureSuccessStatusCode
        var unknown = $"ninguem{Guid.NewGuid():N}@exemplo.com";
        var known = await c.PostAsJsonAsync("/api/v1/auth/password/forgot", new { email = s.Email });
        var missing = await c.PostAsJsonAsync("/api/v1/auth/password/forgot", new { email = unknown });
        known.StatusCode.Should().Be(HttpStatusCode.Accepted);
        missing.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await known.Content.ReadAsStringAsync()).Should().Be(await missing.Content.ReadAsStringAsync());

        var logs = string.Join('\n', f.Logs.Lines);
        logs.Should().Contain("Falha ao enviar o e-mail de verificação por SMTP").And.Contain("Falha ao enviar o e-mail de redefinição de senha por SMTP");
        logs.Should().NotContain(s.Email).And.NotContain(unknown).And.NotContain(f.Emails.LastToken(s.Email, "reset"));
    }

    [Fact]
    public void Host_SMTP_sem_remetente_impede_a_subida_da_API()
    {
        var settings = Smtp(SmtpStub.ClosedPort());
        settings.Remove("Email:Smtp:FromAddress");
        using var f = new ZyftApiFactory { Settings = settings };

        var act = () => f.CreateClient();
        act.Should().Throw<Exception>().Which.ToString().Should().Contain("Email:Smtp:FromAddress é obrigatório");
    }
}
