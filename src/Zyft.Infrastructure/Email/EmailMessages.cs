using System.Net;

namespace Zyft.Infrastructure.Email;

/// <summary>Tipo de e-mail da conta. Usado também nos logs, que nunca levam endereço, nome ou token (RNF-09).</summary>
public enum EmailKind
{
    Verification,
    PasswordReset,
}

/// <summary>Conteúdo pronto de um e-mail: assunto, texto simples e a alternativa em HTML.</summary>
public sealed record EmailContent(string Subject, string Text, string Html);

/// <summary>
/// Monta os links e os textos dos e-mails da conta (pt-BR, voz da marca: calma, sem exclamação, sem emoji, "você").
/// O token só aparece no link; o nome é codificado no HTML.
/// </summary>
public static class EmailMessages
{
    public const string VerificationPath = "/#/verificar";
    public const string PasswordResetPath = "/#/redefinir-senha";

    public const string VerificationSubject = "Confirme seu e-mail no Zyft";
    public const string PasswordResetSubject = "Redefinir sua senha do Zyft";

    /// <summary>Link para a plataforma web: <c>{WebBaseUrl}{path}?token={token}</c>, com o token escapado.</summary>
    public static string Link(string webBaseUrl, string path, string token) =>
        $"{webBaseUrl.TrimEnd('/')}{path}?token={Uri.EscapeDataString(token)}";

    public static string PathFor(EmailKind kind) => kind == EmailKind.Verification ? VerificationPath : PasswordResetPath;

    /// <summary>Descrição do tipo para os logs (sem dados da pessoa).</summary>
    public static string Describe(EmailKind kind) => kind == EmailKind.Verification ? "verificação" : "redefinição de senha";

    public static EmailContent Build(EmailKind kind, string name, string link) =>
        kind == EmailKind.Verification ? Verification(name, link) : PasswordReset(name, link);

    public static EmailContent Verification(string name, string link)
    {
        string[] paragraphs =
        [
            "Para confirmar este endereço na sua conta do Zyft, abra o link abaixo.",
            "O link vale por 24 horas. Sem a confirmação nada fica bloqueado: você pode continuar usando o Zyft normalmente.",
            "Se você não criou uma conta no Zyft, ignore este e-mail.",
        ];
        return Compose(VerificationSubject, name, paragraphs[0], link, "Confirmar e-mail", paragraphs[1..]);
    }

    public static EmailContent PasswordReset(string name, string link)
    {
        string[] paragraphs =
        [
            "Recebemos um pedido para redefinir a senha da sua conta do Zyft. Para escolher uma senha nova, abra o link abaixo.",
            "O link vale por 30 minutos e só pode ser usado uma vez. Ao redefinir, as sessões abertas da sua conta são encerradas.",
            "Se não foi você, ignore este e-mail; sua senha continua a mesma.",
        ];
        return Compose(PasswordResetSubject, name, paragraphs[0], link, "Redefinir senha", paragraphs[1..]);
    }

    private static EmailContent Compose(string subject, string name, string intro, string link, string button, string[] after)
    {
        var trimmed = (name ?? "").Trim();
        var greeting = trimmed.Length == 0 ? "Olá." : $"Olá, {trimmed}.";
        var htmlGreeting = trimmed.Length == 0 ? "Olá." : $"Olá, {WebUtility.HtmlEncode(trimmed)}.";

        var text = string.Join("\n\n", new[] { greeting, intro, link }.Concat(after).Append("Equipe Zyft")) + "\n";

        var href = WebUtility.HtmlEncode(link);
        var p = "style=\"margin:0 0 16px\"";
        var html =
            "<!doctype html>\n<html lang=\"pt-BR\"><head><meta charset=\"utf-8\"><title>" + WebUtility.HtmlEncode(subject) + "</title></head>\n" +
            "<body style=\"margin:0;padding:24px;background:#f6f6f4;font-family:Arial,Helvetica,sans-serif;color:#1f2328;font-size:16px;line-height:1.5\">\n" +
            "<div style=\"max-width:520px;margin:0 auto;background:#ffffff;padding:24px;border-radius:8px\">\n" +
            $"<p {p}>{htmlGreeting}</p>\n" +
            $"<p {p}>{WebUtility.HtmlEncode(intro)}</p>\n" +
            $"<p {p}><a href=\"{href}\" style=\"display:inline-block;padding:10px 16px;background:#1f2328;color:#ffffff;text-decoration:none;border-radius:6px\">{button}</a></p>\n" +
            $"<p style=\"margin:0 0 16px;font-size:14px;color:#57606a\">Se o botão não abrir, copie este endereço no navegador:<br><a href=\"{href}\" style=\"color:#57606a;word-break:break-all\">{href}</a></p>\n" +
            string.Concat(after.Select(a => $"<p {p}>{WebUtility.HtmlEncode(a)}</p>\n")) +
            "<p style=\"margin:0\">Equipe Zyft</p>\n" +
            "</div>\n</body></html>\n";

        return new EmailContent(subject, text, html);
    }
}
