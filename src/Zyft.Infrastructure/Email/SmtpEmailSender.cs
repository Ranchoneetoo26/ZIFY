using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zyft.Application.Common.Interfaces;

namespace Zyft.Infrastructure.Email;

/// <summary>
/// Envio real por SMTP (seção <c>Email:Smtp</c>). Ligado quando <c>Email:Smtp:Host</c> está preenchido; sem ele a API
/// usa <see cref="LoggingEmailSender"/>. A senha vem só de configuração externa (variável <c>Email__Smtp__Password</c>
/// ou cofre do ambiente), nunca do repositório.
/// </summary>
public sealed class SmtpOptions
{
    public const string Section = "Email:Smtp";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }

    /// <summary>TLS na conexão (STARTTLS na 587). Desligue só para um servidor local de teste, como o Mailpit.</summary>
    public bool EnableSsl { get; set; } = true;

    public string? FromAddress { get; set; }
    public string FromName { get; set; } = "Zyft";

    /// <summary>Tempo máximo de uma entrega, em segundos. Passou disso, o envio conta como falha.</summary>
    public int TimeoutSeconds { get; set; } = 15;

    public bool Enabled => !string.IsNullOrWhiteSpace(Host);
}

/// <summary>Valida <see cref="SmtpOptions"/> na subida da API quando o SMTP está ligado, com mensagem clara.</summary>
public sealed class SmtpOptionsValidator : IValidateOptions<SmtpOptions>
{
    public ValidateOptionsResult Validate(string? name, SmtpOptions o)
    {
        if (!o.Enabled) return ValidateOptionsResult.Success;
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(o.FromAddress))
            errors.Add("Email:Smtp:FromAddress é obrigatório quando Email:Smtp:Host está configurado (variável Email__Smtp__FromAddress).");
        else if (!MailAddress.TryCreate(o.FromAddress, out _))
            errors.Add("Email:Smtp:FromAddress não é um endereço de e-mail válido.");
        if (o.Port is < 1 or > 65535)
            errors.Add("Email:Smtp:Port deve estar entre 1 e 65535.");
        if (o.TimeoutSeconds < 1)
            errors.Add("Email:Smtp:TimeoutSeconds deve ser pelo menos 1.");
        if (!string.IsNullOrWhiteSpace(o.Username) && string.IsNullOrEmpty(o.Password))
            errors.Add("Email:Smtp:Password é obrigatório quando Email:Smtp:Username está configurado (variável Email__Smtp__Password; nunca no repositório).");
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

/// <summary>
/// Envia os e-mails da conta por SMTP (<see cref="SmtpClient"/>), em texto simples com alternativa em HTML.
/// Falha de envio não interrompe o cadastro nem a recuperação de senha: registra um aviso sem endereço, nome ou token
/// (RNF-09) e segue. Cancelamento pedido pelo chamador continua propagando.
/// </summary>
public sealed class SmtpEmailSender(IOptions<AppOptions> app, IOptions<SmtpOptions> smtp, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public Task SendEmailVerificationAsync(string toEmail, string name, string token, CancellationToken ct = default) =>
        SendAsync(EmailKind.Verification, toEmail, name, token, ct);

    public Task SendPasswordResetAsync(string toEmail, string name, string token, CancellationToken ct = default) =>
        SendAsync(EmailKind.PasswordReset, toEmail, name, token, ct);

    private async Task SendAsync(EmailKind kind, string toEmail, string name, string token, CancellationToken ct)
    {
        var o = smtp.Value;
        var link = EmailMessages.Link(app.Value.WebBaseUrl, EmailMessages.PathFor(kind), token);
        var content = EmailMessages.Build(kind, name, link);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(o.TimeoutSeconds));
        try
        {
            using var message = CreateMessage(o, toEmail, content);
            using var client = new SmtpClient(o.Host!, o.Port)
            {
                EnableSsl = o.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = o.TimeoutSeconds * 1000,
            };
            if (!string.IsNullOrWhiteSpace(o.Username))
                client.Credentials = new NetworkCredential(o.Username, o.Password);
            else
                client.UseDefaultCredentials = false;

            await client.SendMailAsync(message, timeout.Token);
            logger.LogInformation("E-mail de {Kind} enviado.", EmailMessages.Describe(kind));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Qualquer falha (conexão, TLS, autenticação, recusa, tempo esgotado) vira aviso: o cadastro e o
            // "esqueci minha senha" respondem igual com ou sem envio (sem enumeração de contas).
            // Só o tipo e o código: a mensagem da exceção pode conter o endereço do destinatário.
            var status = ex is SmtpException se ? se.StatusCode.ToString() : "-";
            logger.LogWarning("Falha ao enviar o e-mail de {Kind} por SMTP ({ErrorType}, status {SmtpStatus}). O fluxo seguiu sem o envio.",
                EmailMessages.Describe(kind), ex.GetType().Name, status);
        }
    }

    internal static MailMessage CreateMessage(SmtpOptions o, string toEmail, EmailContent content)
    {
        var message = new MailMessage
        {
            From = new MailAddress(o.FromAddress!, o.FromName, Encoding.UTF8),
            Subject = content.Subject,
            SubjectEncoding = Encoding.UTF8,
            HeadersEncoding = Encoding.UTF8,
            Body = content.Text,
            BodyEncoding = Encoding.UTF8,
            BodyTransferEncoding = TransferEncoding.QuotedPrintable,
            IsBodyHtml = false,
        };
        message.To.Add(new MailAddress(toEmail));
        var html = AlternateView.CreateAlternateViewFromString(content.Html, Encoding.UTF8, MediaTypeNames.Text.Html);
        html.TransferEncoding = TransferEncoding.QuotedPrintable;
        message.AlternateViews.Add(html);
        return message;
    }
}
