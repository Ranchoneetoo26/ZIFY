using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zyft.Application.Common.Interfaces;

namespace Zyft.Infrastructure.Ai;

public sealed class LlmOptions
{
    public const string Section = "Llm";

    /// <summary>Ollama | None.</summary>
    public string Provider { get; set; } = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "qwen2.5:7b";
    public int TimeoutSeconds { get; set; } = 120;
    public int NumCtx { get; set; } = 8192;
}

/// <summary>
/// Adaptador Ollama (ADR-0005): <c>/api/chat</c> com <c>format</c> = JSON Schema, para que o resto do sistema valide
/// a resposta em vez de confiar em texto livre. Qualquer falha vira <see cref="LlmUnavailableException"/>.
/// Prompts e respostas nunca são logados (podem conter dados do perfil).
/// </summary>
public sealed class OllamaLlmClient(HttpClient http, IOptions<LlmOptions> options, ILogger<OllamaLlmClient> logger) : ILlmClient
{
    private readonly LlmOptions _o = options.Value;

    public bool IsEnabled => true;

    public async Task<JsonElement> CompleteJsonAsync(LlmJsonRequest request, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(request.Timeout ?? TimeSpan.FromSeconds(_o.TimeoutSeconds));
        var body = new
        {
            model = _o.Model,
            stream = false,
            format = request.Schema,
            options = new { temperature = request.Temperature, num_ctx = _o.NumCtx },
            messages = new object[]
            {
                new { role = "system", content = request.System ?? string.Empty },
                new { role = "user", content = request.Prompt },
            },
        };

        HttpResponseMessage res;
        try
        {
            res = await http.PostAsJsonAsync("api/chat", body, cts.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            if (ct.IsCancellationRequested) throw;
            logger.LogWarning("Ollama indisponível ({Error})", ex.GetType().Name);
            throw new LlmUnavailableException("IA local indisponível.", ex);
        }

        using (res)
        {
            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning("Ollama respondeu {Status}", (int)res.StatusCode);
                throw new LlmUnavailableException($"IA respondeu {(int)res.StatusCode}.");
            }

            try
            {
                using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(cts.Token), cancellationToken: cts.Token);
                var content = doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
                using var parsed = JsonDocument.Parse(content);
                return parsed.RootElement.Clone();
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
            {
                throw new LlmUnavailableException("A IA devolveu um JSON inválido.", ex);
            }
        }
    }

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            using var res = await http.GetAsync("api/tags", cts.Token);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>Usado quando não há IA configurada: o sistema funciona só com regras (RNF-12).</summary>
public sealed class NullLlmClient : ILlmClient
{
    public bool IsEnabled => false;

    public Task<JsonElement> CompleteJsonAsync(LlmJsonRequest request, CancellationToken ct = default) =>
        throw new LlmUnavailableException("IA desativada nesta instalação.");
}
