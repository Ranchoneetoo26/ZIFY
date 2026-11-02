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

/// <summary>
/// Envio de e-mail de desenvolvimento: não envia nada, só registra o link gerado. Nunca registra o endereço nem o nome
/// da pessoa (RNF-09); o token não é dado pessoal e é de uso único com validade curta.
/// </summary>
public sealed class LoggingEmailSender(IOptions<AppOptions> options, ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendEmailVerificationAsync(string toEmail, string name, string token, CancellationToken ct = default)
    {
        logger.LogInformation("Link de verificação gerado: {Link}", Link("/#/verificar", token));
        return Task.CompletedTask;
    }

    internal string Link(string path, string token) =>
        $"{options.Value.WebBaseUrl.TrimEnd('/')}{path}?token={Uri.EscapeDataString(token)}";
}
