using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Zyft.Application.Common;
using Zyft.Application.Common.Interfaces;
using static Zyft.Application.Common.Text.TextTools;

namespace Zyft.Application.Jobs;

public sealed record JobInfo(string? Title, string? Company, string? Location, string? ContractType, string? WorkModel, string? Schedule, string? Salary, string? Summary);

/// <summary>Requisito avaliado. "atende" e "parcial" sempre têm <see cref="Evidence"/> = linha real do perfil (ADR-0013).</summary>
public sealed record RequirementResult(string Text, string Kind, string Status, string Evidence, int? EvidenceLine, string? Analysis);

/// <summary>Ponto que pede confirmação da pessoa. Devolve só se o perfil tem valor, não o valor (mínimo retorno).</summary>
public sealed record ConfirmItem(string Label, string ProfileKey, bool HasValue);

/// <summary>Contagens (nunca nota ou porcentagem — RN-06).</summary>
public sealed record JobStats(int Atende, int Parcial, int NaoEncontrado, int Confirmar, int Total, int RequiredTotal, int RequiredMet);

public sealed record JobAnalysisResult(JobInfo Job, IReadOnlyList<RequirementResult> Requirements, IReadOnlyList<ConfirmItem> ToConfirm, JobStats Stats, bool AiUsed, string Disclaimer);

public static class RequirementStatus
{
    public const string Meets = "atende";
    public const string Partial = "parcial";
    public const string NotFound = "nao_encontrado";
    public const string Confirm = "confirmar";
    public static readonly string[] All = { Meets, Partial, NotFound, Confirm };
}

public interface IJobAnalyzer
{
    Task<JobAnalysisResult> AnalyzeAsync(string? url, string? title, string text, IReadOnlyList<string> profileLines, Func<string, bool> profileHasKey, CancellationToken ct = default);
}

/// <summary>
/// Análise da vaga visível (RF-17) — porte de <c>jobs.js</c>. A IA extrai requisitos e aponta o NÚMERO da linha
/// do perfil que comprova cada um; a evidência exibida é a própria linha (não um texto da IA). Se a linha não
/// existe, "atende/parcial" vira "nao_encontrado". Sem IA, usa heurística determinística.
/// </summary>
public sealed partial class JobAnalyzer(ILlmClient llm, ILogger<JobAnalyzer> logger) : IJobAnalyzer
{
    public const string DisclaimerText = "Análise baseada apenas no que está visível na página e no seu Perfil Mestre. Não é garantia de aprovação.";
    private const int MaxRequirements = 20;

    private static readonly (Regex Re, string Label, string Key)[] ConfirmRules =
    {
        (new(@"\bcnh\b|carteira de (motorista|habilitacao)|habilitacao categoria"), "CNH", "preferences.driverLicense"),
        (new(@"disponibilidade (de|para) (horario|turno)|escala 6x1|escala 12x36|finais de semana|\bturnos?\b"), "Disponibilidade de horário/turnos", "preferences.availability"),
        (new(@"viage(m|ns)|viajar"), "Disponibilidade para viagens", "preferences.travel"),
        (new(@"pretensao salarial|faixa salarial|expectativa salarial"), "Pretensão salarial", "preferences.salaryExpectation"),
        (new(@"\bpresencial\b|\bhibrido\b|\bremoto\b|home office"), "Modalidade de trabalho", "objective.workModel"),
        (new(@"mudanca|reside(nte|ncia) em|morar em"), "Local de residência / mudança", "preferences.relocation"),
        (new(@"\bclt\b|\bpj\b|estagio|temporario|freelance"), "Tipo de contrato", "preferences.contractTypes"),
    };

    public async Task<JobAnalysisResult> AnalyzeAsync(string? url, string? title, string text, IReadOnlyList<string> profileLines, Func<string, bool> profileHasKey, CancellationToken ct = default)
    {
        var jobText = Regex.Replace(text ?? string.Empty, @"\n{3,}", "\n\n");
        if (jobText.Length > 7000) jobText = jobText[..7000];

        var aiUsed = false;
        JobInfo info;
        List<(string Text, string Kind)> reqs;
        try
        {
            if (!llm.IsEnabled) throw new LlmUnavailableException("IA desativada.");
            (info, reqs) = await ExtractWithAiAsync(url, title, jobText, ct);
            aiUsed = true;
        }
        catch (LlmUnavailableException ex)
        {
            logger.LogWarning("IA indisponível na análise de vaga; usando heurística: {Reason}", ex.Message);
            (info, reqs) = ExtractHeuristic(title, jobText);
        }

        var numbered = string.Join('\n', profileLines.Select((l, i) => $"[{i + 1}] {l}"));
        var evaluated = new List<RequirementResult>();
        foreach (var (reqText, kind) in reqs.Take(MaxRequirements))
        {
            (string Status, int Line, string? Analysis) e;
            if (aiUsed)
            {
                try
                {
                    e = await EvaluateWithAiAsync(numbered, reqText, ct);
                }
                catch (LlmUnavailableException ex)
                {
                    e = EvaluateHeuristic(reqText, profileLines);
                    e.Analysis = $"IA indisponível ({ex.Message}); avaliação por palavras-chave.";
                }
            }
            else
            {
                e = EvaluateHeuristic(reqText, profileLines);
            }

            evaluated.Add(Guarded(reqText, kind, e.Status, e.Line, e.Analysis, profileLines));
        }

        var nText = Norm(jobText);
        var toConfirm = ConfirmRules.Where(r => r.Re.IsMatch(nText))
            .Select(r => new ConfirmItem(r.Label, r.Key, profileHasKey(r.Key)))
            .ToList();

        var required = evaluated.Where(x => x.Kind == "obrigatório").ToList();
        var stats = new JobStats(
            evaluated.Count(x => x.Status == RequirementStatus.Meets),
            evaluated.Count(x => x.Status == RequirementStatus.Partial),
            evaluated.Count(x => x.Status == RequirementStatus.NotFound),
            evaluated.Count(x => x.Status == RequirementStatus.Confirm),
            evaluated.Count,
            required.Count,
            required.Count(x => x.Status == RequirementStatus.Meets));

        return new JobAnalysisResult(info, evaluated, toConfirm, stats, aiUsed, DisclaimerText);
    }

    /// <summary>Guarda contra alucinação: "atende/parcial" exige uma linha real do perfil; evidência = a linha.</summary>
    internal static RequirementResult Guarded(string text, string kind, string status, int line, string? analysis, IReadOnlyList<string> lines)
    {
        if (!RequirementStatus.All.Contains(status)) status = RequirementStatus.Confirm;
        var evidence = line >= 1 && line <= lines.Count ? lines[line - 1] : string.Empty;
        if (status is RequirementStatus.Meets or RequirementStatus.Partial && evidence.Length == 0) status = RequirementStatus.NotFound;
        var keep = status is RequirementStatus.Meets or RequirementStatus.Partial;
        return new RequirementResult(text, kind, status, keep ? evidence : string.Empty, keep ? line : null, analysis);
    }

    private static string CleanRequirement(string t) =>
        Regex.Replace(Regex.Replace(t, @"\s*\((obrigat[óo]rio|desej[áa]vel|diferencial)\)\s*", " ", RegexOptions.IgnoreCase), @"[;.]\s*$", string.Empty).Trim();

    // ------------------------------------------------------------ IA

    private async Task<(JobInfo, List<(string, string)>)> ExtractWithAiAsync(string? url, string? title, string jobText, CancellationToken ct)
    {
        var str = new { type = "string" };
        var schema = new
        {
            type = "object",
            properties = new
            {
                title = str, company = str, location = str, contractType = str, schedule = str, salary = str, workModel = str, summary = str,
                requirements = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new { text = str, kind = new { type = "string", @enum = new[] { "obrigatório", "desejável" } } },
                        required = new[] { "text", "kind" },
                    },
                },
            },
            required = new[] { "title", "company", "location", "contractType", "schedule", "salary", "workModel", "summary", "requirements" },
        };
        var prompt = $"""
            Extraia as informações VISÍVEIS desta página de vaga. Não deduza o que não está escrito (use "" se não houver).
            Título da página: {title}
            URL: {url}
            Texto:
            "{jobText}"

            JSON com: "title" (cargo), "company", "location" (cidade/UF), "contractType" (CLT, PJ, estágio…), "workModel" (presencial, híbrido ou remoto), "schedule" (jornada), "salary", "summary" (1-2 frases), "requirements": [{"{"}"text": requisito curto sem a palavra obrigatório/desejável, "kind": "obrigatório"|"desejável"{"}"}].
            Requisitos sem indicação explícita de "desejável"/"diferencial" são "obrigatório".
            """;
        var r = await llm.CompleteJsonAsync(new LlmJsonRequest(prompt, schema, LlmPrompts.SystemRules, 0.1, TimeSpan.FromSeconds(120)), ct);
        string? S(string name) => r.ValueKind == JsonValueKind.Object && r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && v.GetString()!.Trim().Length > 0 ? v.GetString()!.Trim() : null;
        var info = new JobInfo(S("title") ?? title, S("company"), S("location"), S("contractType"), S("workModel"), S("schedule"), S("salary"), S("summary"));
        var reqs = new List<(string, string)>();
        if (r.ValueKind == JsonValueKind.Object && r.TryGetProperty("requirements", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var q in arr.EnumerateArray())
            {
                if (q.ValueKind != JsonValueKind.Object || !q.TryGetProperty("text", out var t) || t.ValueKind != JsonValueKind.String) continue;
                var kind = q.TryGetProperty("kind", out var k) && k.GetString() == "desejável" ? "desejável" : "obrigatório";
                var clean = CleanRequirement(t.GetString()!);
                if (clean.Length > 0) reqs.Add((clean, kind));
            }
        }

        return (info, reqs);
    }

    private async Task<(string, int, string?)> EvaluateWithAiAsync(string numbered, string requirement, CancellationToken ct)
    {
        var schema = new
        {
            type = "object",
            properties = new
            {
                analysis = new { type = "string" },
                status = new { type = "string", @enum = RequirementStatus.All },
                line = new { type = "integer" },
            },
            required = new[] { "analysis", "status", "line" },
        };
        var prompt = $"""
            Perfil Mestre do candidato (linhas numeradas):
            {numbered}

            Requisito da vaga: "{requirement}"

            O perfil comprova este requisito? Raciocine em "analysis" (1 frase) e depois responda:
            - "status": "atende" (uma linha do perfil comprova), "parcial" (há algo relacionado, mas incompleto), "nao_encontrado" (nada no perfil) ou "confirmar" (é condição/decisão do candidato e o perfil não responde).
            - "line": número da linha que comprova (0 se nenhuma).
            Considere equivalências: um sistema específico (Totvs, SAP) comprova "sistema ERP"; o tempo total calculado comprova tempo mínimo de experiência; formação com situação "Cursando" comprova "cursando"; "Sim" em viagens comprova disponibilidade para viagens.
            """;
        var r = await llm.CompleteJsonAsync(new LlmJsonRequest(prompt, schema, LlmPrompts.SystemRules, 0.1, TimeSpan.FromSeconds(60)), ct);
        var status = r.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString()! : RequirementStatus.Confirm;
        var line = r.TryGetProperty("line", out var l) && l.ValueKind == JsonValueKind.Number && l.TryGetInt32(out var li) ? li : 0;
        var analysis = r.TryGetProperty("analysis", out var a) && a.ValueKind == JsonValueKind.String ? a.GetString() : null;
        return (status, line, analysis);
    }

    // ------------------------------------------------------------ heurística (sem IA)

    [GeneratedRegex(@"^\s*([-•*·▪●]|\d+[.)])\s+")]
    private static partial Regex Bullet();

    [GeneratedRegex(@"\b(requisitos?|qualificac\w*|exigencias|requirements|o que (buscamos|esperamos)|pre requisitos|perfil desejado|diferenciais|desejave(l|is)|nice to have)\b")]
    private static partial Regex RequirementHeader();

    [GeneratedRegex(@"\b(diferencia\w*|desejave(l|is)|nice to have|sera um plus|plus)\b")]
    private static partial Regex Desirable();

    [GeneratedRegex(@"\b(atividades|responsabilidades|beneficios|sobre a empresa|sobre nos|o que oferecemos|responsibilities|benefits)\b")]
    private static partial Regex OtherHeader();

    internal static (JobInfo, List<(string, string)>) ExtractHeuristic(string? title, string jobText)
    {
        var lines = jobText.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        var reqs = new List<(string, string)>();
        var inReq = false;
        var desirable = false;
        var hasHeader = lines.Any(l => !Bullet().IsMatch(l) && l.Length < 60 && RequirementHeader().IsMatch(Norm(l)));
        foreach (var l in lines)
        {
            var n = Norm(l);
            if (!Bullet().IsMatch(l) && l.Length < 60)
            {
                if (RequirementHeader().IsMatch(n)) { inReq = true; desirable = Desirable().IsMatch(n); continue; }
                if (OtherHeader().IsMatch(n)) { inReq = false; continue; }
            }

            if (!Bullet().IsMatch(l) || (hasHeader && !inReq)) continue;
            var item = CleanRequirement(Bullet().Replace(l, string.Empty));
            if (item.Length < 3) continue;
            reqs.Add((item, desirable || Desirable().IsMatch(Norm(item)) ? "desejável" : "obrigatório"));
        }

        return (new JobInfo(title, null, null, null, null, null, null, null), reqs);
    }

    internal static (string Status, int Line, string? Analysis) EvaluateHeuristic(string requirement, IReadOnlyList<string> profileLines)
    {
        var best = profileLines.Select((l, i) => (cov: Coverage(requirement, l), line: i + 1)).OrderByDescending(x => x.cov).FirstOrDefault();
        if (best.line > 0 && best.cov >= 0.6) return (RequirementStatus.Meets, best.line, "Termos do requisito aparecem nesta linha do perfil.");
        if (best.line > 0 && best.cov >= 0.3) return (RequirementStatus.Partial, best.line, "Parte dos termos aparece nesta linha do perfil.");
        if (ConfirmRules.Any(r => r.Re.IsMatch(Norm(requirement)))) return (RequirementStatus.Confirm, 0, "Condição que só você pode confirmar.");
        return (RequirementStatus.NotFound, 0, null);
    }
}
