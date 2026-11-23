using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zyft.Application.Auth;
using Zyft.Application.Common.Interfaces;
using Zyft.Infrastructure;
using Zyft.Infrastructure.Email;
using Zyft.UnitTests.Support;

namespace Zyft.UnitTests.Infrastructure;

/// <summary>Envio de e-mail: textos, links, escolha da implementação e entrega por SMTP (servidor falso em processo).</summary>
public class EmailTests
{
    private const string Base = "https://zyft.exemplo/";
    private const string To = "pessoa@privado.com";
    private const string Name = "Nome Privado";

    // ------------------------------------------------------------------ textos e links

    [Fact]
    public void Link_junta_a_base_sem_barra_dupla_e_escapa_o_token()
    {
        EmailMessages.Link(Base, EmailMessages.VerificationPath, "a+b/c=").Should().Be("https://zyft.exemplo/#/verificar?token=a%2Bb%2Fc%3D");
        EmailMessages.Link("http://localhost:5080", EmailMessages.PasswordResetPath, "t").Should().Be("http://localhost:5080/#/redefinir-senha?token=t");
    }

    [Fact]
    public void Verificacao_tem_assunto_link_validade_e_diz_que_nada_fica_bloqueado()
    {
        var link = EmailMessages.Link(Base, EmailMessages.VerificationPath, "tok123");
        var m = EmailMessages.Verification(Name, link);

        m.Subject.Should().Be("Confirme seu e-mail no Zyft");
        m.Text.Should().StartWith("Olá, Nome Privado.").And.Contain(link).And.Contain("24 horas").And.Contain("nada fica bloqueado");
        m.Html.Should().Contain($"href=\"{link}\"").And.Contain("24 horas").And.Contain("lang=\"pt-BR\"");
        AssertBrandVoice(m);
    }

    [Fact]
    public void Redefinicao_tem_assunto_link_validade_e_orienta_quem_nao_pediu()
    {
        var link = EmailMessages.Link(Base, EmailMessages.PasswordResetPath, "tok456");
        var m = EmailMessages.PasswordReset(Name, link);

        m.Subject.Should().Be("Redefinir sua senha do Zyft");
        m.Text.Should().Contain(link).And.Contain("30 minutos").And.Contain("Se não foi você, ignore este e-mail; sua senha continua a mesma.");
        m.Html.Should().Contain($"href=\"{link}\"").And.Contain("sua senha continua a mesma");
        AssertBrandVoice(m);
    }

    [Fact]
    public void Validade_nos_textos_bate_com_a_dos_tokens()
    {
        UserTokens.EmailVerificationLifetime.Should().Be(TimeSpan.FromHours(24));
        UserTokens.PasswordResetLifetime.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void Nome_e_codificado_no_HTML_e_nome_vazio_vira_so_Ola()
    {
        var m = EmailMessages.Verification("<script>alert(1)</script> & Cia", "https://x/#/verificar?token=t&x=1");
        m.Html.Should().NotContain("<script>").And.Contain("&lt;script&gt;alert(1)&lt;/script&gt; &amp; Cia");
        m.Html.Should().Contain("href=\"https://x/#/verificar?token=t&amp;x=1\"");
        m.Text.Should().Contain("<script>alert(1)</script> & Cia"); // texto simples não é HTML

        EmailMessages.PasswordReset("  ", "https://x").Text.Should().StartWith("Olá.\n");
    }

    private static void AssertBrandVoice(EmailContent m)
    {
        foreach (var s in new[] { m.Subject, m.Text, m.Html.Replace("<!doctype html>", "") })
            s.Should().NotContain("!", "a voz da marca não usa exclamação");
        m.Text.Should().Contain("você");
        m.Text.Any(c => char.IsSurrogate(c) || c > 0x2000).Should().BeFalse("sem emoji ou símbolos");
    }

    // ------------------------------------------------------------------ escolha da implementação

    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        settings["ConnectionStrings:Default"] = "Data Source=:memory:";
        settings["Database:Provider"] = "Sqlite";
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddLogging();
        services.AddInfrastructure(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Sem_host_SMTP_usa_o_envio_que_so_registra()
    {
        using var sp = Build(new() { ["Email:Smtp:FromAddress"] = "nao-responda@zyft.local" });
        sp.GetRequiredService<IEmailSender>().Should().BeOfType<LoggingEmailSender>();
    }

    [Fact]
    public void Com_host_SMTP_usa_o_envio_por_SMTP_com_padroes()
    {
        using var sp = Build(new() { ["Email:Smtp:Host"] = "smtp.exemplo", ["Email:Smtp:FromAddress"] = "nao-responda@zyft.local" });
        sp.GetRequiredService<IEmailSender>().Should().BeOfType<SmtpEmailSender>();
        var o = sp.GetRequiredService<IOptions<SmtpOptions>>().Value;
        (o.Port, o.EnableSsl, o.FromName).Should().Be((587, true, "Zyft"));
    }

    [Fact]
    public void Com_host_SMTP_sem_remetente_falha_na_subida_com_mensagem_clara()
    {
        using var sp = Build(new() { ["Email:Smtp:Host"] = "smtp.exemplo" });
        var act = () => sp.GetRequiredService<IOptions<SmtpOptions>>().Value;
        act.Should().Throw<OptionsValidationException>().Which.Message.Should().Contain("Email:Smtp:FromAddress é obrigatório");
    }

    [Fact]
    public void Usuario_SMTP_sem_senha_e_remetente_invalido_falham_na_subida()
    {
        using var sp = Build(new() { ["Email:Smtp:Host"] = "smtp.exemplo", ["Email:Smtp:FromAddress"] = "sem-arroba", ["Email:Smtp:Username"] = "conta" });
        var act = () => sp.GetRequiredService<IOptions<SmtpOptions>>().Value;
        act.Should().Throw<OptionsValidationException>().Which.Failures.Should().HaveCount(2)
            .And.Contain(f => f.Contains("Email__Smtp__Password")).And.Contain(f => f.Contains("não é um endereço"));
    }

    // ------------------------------------------------------------------ entrega por SMTP

    private static SmtpEmailSender Sender(int port, ListLogger<SmtpEmailSender> logger) => new(
        Options.Create(new AppOptions { WebBaseUrl = Base }),
        Options.Create(new SmtpOptions { Host = "127.0.0.1", Port = port, EnableSsl = false, FromAddress = "nao-responda@zyft.local", TimeoutSeconds = 5 }),
        logger);

    [Fact]
    public async Task Entrega_por_SMTP_com_texto_e_HTML_e_log_sem_dados_pessoais()
    {
        await using var smtp = new SmtpStub();
        var logger = new ListLogger<SmtpEmailSender>();
        var sender = Sender(smtp.Port, logger);

        await sender.SendEmailVerificationAsync(To, "Ana <b>", "tok123");
        await sender.SendPasswordResetAsync(To, "Ana <b>", "tok456");

        var msgs = await smtp.WaitForAsync(2);
        msgs.Should().HaveCount(2);
        var (verify, reset) = (msgs[0], msgs[1]);

        verify.MailFrom.Should().Be("<nao-responda@zyft.local>");
        verify.RcptTo.Should().Equal($"<{To}>");
        verify.Header("Subject").Should().Be("Confirme seu e-mail no Zyft");
        verify.Header("From").Should().Contain("Zyft").And.Contain("nao-responda@zyft.local");
        verify.Header("Content-Type").Should().StartWith("multipart/alternative");
        var parts = verify.Parts();
        parts["text/plain"].Should().Contain("https://zyft.exemplo/#/verificar?token=tok123").And.Contain("Olá, Ana <b>.");
        parts["text/html"].Should().Contain("href=\"https://zyft.exemplo/#/verificar?token=tok123\"").And.Contain("Olá, Ana &lt;b&gt;.");

        reset.Header("Subject").Should().Be("Redefinir sua senha do Zyft");
        reset.Parts()["text/plain"].Should().Contain("https://zyft.exemplo/#/redefinir-senha?token=tok456");

        logger.Lines.Should().Equal("E-mail de verificação enviado.", "E-mail de redefinição de senha enviado.");
        AssertNoPersonalData(logger);
    }

    [Fact]
    public async Task Servidor_fora_do_ar_nao_quebra_o_fluxo_e_avisa_sem_dados_pessoais()
    {
        var logger = new ListLogger<SmtpEmailSender>();
        var sender = Sender(SmtpStub.ClosedPort(), logger);

        await sender.Invoking(s => s.SendPasswordResetAsync(To, Name, "tok456")).Should().NotThrowAsync();

        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning)
            .Which.Message.Should().StartWith("Falha ao enviar o e-mail de redefinição de senha por SMTP");
        AssertNoPersonalData(logger);
    }

    [Fact]
    public async Task Recusa_do_servidor_nao_quebra_o_fluxo_e_avisa_com_o_status()
    {
        await using var smtp = new SmtpStub { RejectData = true };
        var logger = new ListLogger<SmtpEmailSender>();

        await Sender(smtp.Port, logger).Invoking(s => s.SendEmailVerificationAsync(To, Name, "tok123")).Should().NotThrowAsync();

        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning)
            .Which.Message.Should().Contain("SmtpException").And.Contain("TransactionFailed");
        AssertNoPersonalData(logger);
    }

    [Fact]
    public async Task Cancelamento_pedido_pelo_chamador_continua_propagando()
    {
        await using var smtp = new SmtpStub();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Sender(smtp.Port, new ListLogger<SmtpEmailSender>())
            .Invoking(s => s.SendEmailVerificationAsync(To, Name, "tok", cts.Token)).Should().ThrowAsync<OperationCanceledException>();
    }

    private static void AssertNoPersonalData(ListLogger<SmtpEmailSender> logger)
    {
        var all = string.Join('\n', logger.Lines);
        all.Should().NotContain(To).And.NotContain("privado").And.NotContain("Ana").And.NotContain(Name)
            .And.NotContain("tok123").And.NotContain("tok456").And.NotContain("token");
    }

    public sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();
        public List<string> Lines => Entries.Select(e => e.Message).ToList();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullLogger.Instance.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception) + (exception is null ? "" : " " + exception)));
    }
}
