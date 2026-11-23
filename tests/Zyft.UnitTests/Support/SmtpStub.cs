using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Zyft.UnitTests.Support;

/// <summary>
/// Servidor SMTP mínimo em processo (só para testes, sem Docker): aceita EHLO, MAIL, RCPT, DATA e QUIT em
/// 127.0.0.1 numa porta livre e guarda as mensagens. Com <see cref="RejectData"/> recusa a entrega com 554.
/// </summary>
public sealed class SmtpStub : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _loop;

    public SmtpStub()
    {
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _loop = Task.Run(AcceptLoop);
    }

    public int Port { get; }
    public bool RejectData { get; set; }
    public ConcurrentQueue<SmtpReceived> Messages { get; } = new();

    /// <summary>Espera chegar pelo menos <paramref name="count"/> mensagens (até 10 s).</summary>
    public async Task<SmtpReceived[]> WaitForAsync(int count)
    {
        var until = DateTime.UtcNow.AddSeconds(10);
        while (Messages.Count < count && DateTime.UtcNow < until) await Task.Delay(20);
        return Messages.ToArray();
    }

    /// <summary>Porta em que nada escuta (conexão recusada na hora).</summary>
    public static int ClosedPort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    private async Task AcceptLoop()
    {
        while (!_stop.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(_stop.Token); }
            catch { return; }
            _ = Task.Run(() => Handle(client));
        }
    }

    private async Task Handle(TcpClient client)
    {
        using var _ = client;
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };
        try
        {
            await writer.WriteLineAsync("220 stub ESMTP");
            string? from = null;
            var rcpt = new List<string>();
            while (await reader.ReadLineAsync() is { } line)
            {
                var cmd = line.ToUpperInvariant();
                if (cmd.StartsWith("EHLO") || cmd.StartsWith("HELO")) await writer.WriteLineAsync("250 stub");
                else if (cmd.StartsWith("MAIL FROM:")) { from = line[10..].Trim(); rcpt.Clear(); await writer.WriteLineAsync("250 OK"); }
                else if (cmd.StartsWith("RCPT TO:")) { rcpt.Add(line[8..].Trim()); await writer.WriteLineAsync("250 OK"); }
                else if (cmd == "DATA")
                {
                    await writer.WriteLineAsync("354 fim com <CRLF>.<CRLF>");
                    var data = new StringBuilder();
                    while (await reader.ReadLineAsync() is { } d && d != ".")
                        data.Append(d.StartsWith("..") ? d[1..] : d).Append("\r\n");
                    if (RejectData) { await writer.WriteLineAsync("554 recusado"); continue; }
                    Messages.Enqueue(new SmtpReceived(from ?? "", rcpt.ToArray(), data.ToString()));
                    await writer.WriteLineAsync("250 OK");
                }
                else if (cmd is "RSET" or "NOOP") await writer.WriteLineAsync("250 OK");
                else if (cmd == "QUIT") { await writer.WriteLineAsync("221 tchau"); return; }
                else await writer.WriteLineAsync("502 comando não suportado");
            }
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        _listener.Stop();
        try { await _loop; } catch { }
        _stop.Dispose();
    }
}

/// <summary>Mensagem recebida pelo <see cref="SmtpStub"/>, com leitura simples de cabeçalhos e partes MIME.</summary>
public sealed record SmtpReceived(string MailFrom, IReadOnlyList<string> RcptTo, string Raw)
{
    private string HeaderBlock => Raw[..Math.Max(0, Raw.IndexOf("\r\n\r\n", StringComparison.Ordinal))];

    /// <summary>Valor do cabeçalho (com dobras desfeitas e palavras codificadas RFC 2047 decodificadas).</summary>
    public string? Header(string name)
    {
        var unfolded = Regex.Replace(HeaderBlock, "\r\n[ \t]+", " ");
        var m = Regex.Match(unfolded, $"^{Regex.Escape(name)}:\\s*(.*)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        return m.Success ? DecodeWords(m.Groups[1].Value.TrimEnd('\r')) : null;
    }

    /// <summary>Partes de texto decodificadas, por tipo (<c>text/plain</c>, <c>text/html</c>).</summary>
    public Dictionary<string, string> Parts()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var boundary = Regex.Match(Regex.Replace(HeaderBlock, "\r\n[ \t]+", " "), "boundary=\"?([^\";\r\n]+)\"?", RegexOptions.IgnoreCase);
        var chunks = boundary.Success
            ? Raw.Split("--" + boundary.Groups[1].Value).Skip(1).Where(c => !c.StartsWith("--"))
            : new[] { Raw };
        foreach (var chunk in chunks)
        {
            var part = chunk.TrimStart('\r', '\n');
            var split = part.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (split < 0) continue;
            var headers = Regex.Replace(part[..split], "\r\n[ \t]+", " ");
            var body = part[(split + 4)..];
            var type = Regex.Match(headers, @"Content-Type:\s*([^;\r\n]+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
            var enc = Regex.Match(headers, @"Content-Transfer-Encoding:\s*(\S+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim().ToLowerInvariant();
            result[type] = enc switch
            {
                "base64" => Encoding.UTF8.GetString(Convert.FromBase64String(Regex.Replace(body, @"\s", ""))),
                "quoted-printable" => DecodeQuotedPrintable(body),
                _ => body,
            };
        }
        return result;
    }

    private static string DecodeQuotedPrintable(string s)
    {
        s = s.Replace("=\r\n", "");
        var bytes = new List<byte>();
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == '=' && i + 2 < s.Length && Uri.IsHexDigit(s[i + 1]) && Uri.IsHexDigit(s[i + 2]))
            {
                bytes.Add(Convert.ToByte(s.Substring(i + 1, 2), 16));
                i += 2;
            }
            else bytes.AddRange(Encoding.UTF8.GetBytes(s[i].ToString()));
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static string DecodeWords(string value) =>
        Regex.Replace(value, @"=\?([^?]+)\?([BbQq])\?([^?]*)\?=\s*", m =>
        {
            var text = m.Groups[3].Value;
            return m.Groups[2].Value.ToUpperInvariant() == "B"
                ? Encoding.UTF8.GetString(Convert.FromBase64String(text))
                : DecodeQuotedPrintable(text.Replace('_', ' '));
        });
}
