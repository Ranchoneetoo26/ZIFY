using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zyft.Application.Common.Interfaces;

namespace Zyft.Infrastructure.Email;

public sealed class AppOptions
{
    public const string Section = "App";

    /// <summary>Endereço da plataforma web usado nos links enviados por e-mail (ex.: <c>https://zyft.app</c>).</summary>
    public string WebBaseUrl { get; set; } = "http://localhost:5080";
}

public sealed class EmailOptions
{
    public const string Section = "Email";

    /// <summary>
    /// Registra no log o link completo (com o token) dos e-mails gerados. Só para desenvolvimento local, onde não há
    /// envio real: o token dá acesso à conta (redefinir senha). Padrão <c>false</c>; <c>true</c> só em
    /// <c>appsettings.Development.json</c>.
    /// </summary>
    public bool LogLinks { get; set; }
}

/// <summary>
/// Envio de e-mail enquanto não há provedor configurado: não envia nada. Com <see cref="EmailOptions.LogLinks"/>
/// (só desenvolvimento) registra o link com o token; fora disso registra apenas que o e-mail foi gerado, sem token.
/// Nunca registra o endereço nem o nome da pessoa (RNF-09).
/// </summary>
public sealed class LoggingEmailSender(IOptions<AppOptions> options, IOptions<EmailOptions> emailOptions, ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendEmailVerificationAsync(string toEmail, string name, string token, CancellationToken ct = default)
    {
        if (emailOptions.Value.LogLinks)
            logger.LogInformation("Link de verificação gerado: {Link}", Link("/#/verificar", token));
        else
            logger.LogInformation("E-mail de verificação gerado (envio não configurado).");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string name, string token, CancellationToken ct = default)
    {
        if (emailOptions.Value.LogLinks)
            logger.LogInformation("Link de redefinição de senha gerado: {Link}", Link("/#/redefinir-senha", token));
        else
            logger.LogInformation("E-mail de redefinição de senha gerado (envio não configurado).");
        return Task.CompletedTask;
    }

    internal string Link(string path, string token) =>
        $"{options.Value.WebBaseUrl.TrimEnd('/')}{path}?token={Uri.EscapeDataString(token)}";
}
