using System.Text.Json;
using Zyft.Application.Common;
using Zyft.Application.Common.Interfaces;
using static Zyft.Application.Common.Text.TextTools;

namespace Zyft.Application.Writing;

public sealed record JobContext(string? Title, string? Company, string? Text);

public sealed record GeneratedAnswer(string Answer, IReadOnlyList<string> UsedFacts, string MissingInfo);

public interface IAnswerWriter
{
    /// <summary>Gera texto livre só com fatos do perfil (RN-03). Lança <see cref="LlmUnavailableException"/> sem IA.</summary>
    Task<GeneratedAnswer> GenerateAsync(string question, IReadOnlyList<string> profileLines, JobContext? job, int? maxLength, CancellationToken ct = default);
}

/// <summary>
/// Redação assistida (RF-15) — porte de <c>writer.js</c>. Depois da IA, o servidor confere que cada "fato usado"
/// aparece de verdade no perfil (descarta os que não aparecem) e corta no limite de caracteres do campo.
/// </summary>
public sealed class AnswerWriter(ILlmClient llm) : IAnswerWriter
{
    public async Task<GeneratedAnswer> GenerateAsync(string question, IReadOnlyList<string> profileLines, JobContext? job, int? maxLength, CancellationToken ct = default)
    {
        if (!llm.IsEnabled) throw new LlmUnavailableException("IA desativada.");

        var limit = maxLength is > 0 ? $"O texto deve ter no máximo {maxLength} caracteres." : "Seja conciso (até ~700 caracteres).";
        var jobBlock = job is null
            ? string.Empty
            : $"\nVaga (contexto, NÃO são qualificações do candidato):\nCargo: {job.Title}\nEmpresa: {job.Company}\n{(job.Text is { Length: > 2000 } t ? t[..2000] : job.Text)}";
        var prompt = $"""
            Perfil Mestre do candidato (únicos fatos que você pode usar):
            {string.Join('\n', profileLines.Select(l => "- " + l))}
            {jobBlock}

            Pergunta / campo do formulário: "{question}"

            Escreva a resposta em primeira pessoa, usando apenas fatos do perfil. {limit}
            Se o perfil não tiver fatos suficientes, deixe "answer" vazio e explique em "missingInfo" o que falta.
            Em "usedFacts", liste literalmente os fatos do perfil que você usou.
            Responda em JSON: {"{"}"answer": string, "usedFacts": string[], "missingInfo": string{"}"}
            """;
        var schema = new
        {
            type = "object",
            properties = new
            {
                answer = new { type = "string" },
                usedFacts = new { type = "array", items = new { type = "string" } },
                missingInfo = new { type = "string" },
            },
            required = new[] { "answer", "usedFacts", "missingInfo" },
        };

        var r = await llm.CompleteJsonAsync(new LlmJsonRequest(prompt, schema, LlmPrompts.SystemRules, 0.3, TimeSpan.FromSeconds(90)), ct);
        var answer = r.TryGetProperty("answer", out var a) && a.ValueKind == JsonValueKind.String ? a.GetString()!.Trim() : string.Empty;
        var missing = r.TryGetProperty("missingInfo", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString()!.Trim() : string.Empty;
        var used = new List<string>();
        if (r.TryGetProperty("usedFacts", out var u) && u.ValueKind == JsonValueKind.Array)
        {
            var all = string.Join('\n', profileLines);
            foreach (var f in u.EnumerateArray())
            {
                if (f.ValueKind != JsonValueKind.String) continue;
                var fact = f.GetString()!.Trim();
                // Só mantém "fatos" que realmente existem no perfil.
                if (fact.Length > 0 && (profileLines.Any(l => Grounded(fact, l) || Grounded(l, fact)) || Grounded(fact, all, 0.8))) used.Add(fact);
            }
        }

        if (maxLength is > 0) answer = Truncate(answer, maxLength.Value);
        if (answer.Length > 0 && used.Count == 0)
            missing = string.IsNullOrEmpty(missing) ? "Não foi possível confirmar no perfil os fatos usados — revise com atenção." : missing;
        return new GeneratedAnswer(answer, used, missing);
    }
}
