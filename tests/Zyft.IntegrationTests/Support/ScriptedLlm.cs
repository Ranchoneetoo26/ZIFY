using System.Text.Json;
using Zyft.Application.Common.Interfaces;

namespace Zyft.IntegrationTests.Support;

/// <summary>IA de mentira com respostas por "tipo" de prompt (de-para, extração de vaga, avaliação de requisito).</summary>
public sealed class FakeLlm(Func<LlmJsonRequest, string> respond) : ILlmClient
{
    public List<string> Prompts { get; } = new();
    public bool IsEnabled => true;

    public Task<JsonElement> CompleteJsonAsync(LlmJsonRequest request, CancellationToken ct = default)
    {
        lock (Prompts) Prompts.Add(request.Prompt);
        return Task.FromResult(JsonDocument.Parse(respond(request)).RootElement.Clone());
    }
}
