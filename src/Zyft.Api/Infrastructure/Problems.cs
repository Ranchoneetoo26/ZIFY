using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Zyft.Api.Infrastructure;

/// <summary>Cria respostas RFC 7807 (ProblemDetails) em pt-BR, com <c>code</c>, <c>traceId</c> e <c>errors</c> por campo.</summary>
public static class Problems
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static readonly Dictionary<int, string> Titles = new()
    {
        [400] = "Requisição inválida",
        [401] = "Não autenticado",
        [403] = "Acesso negado",
        [404] = "Não encontrado",
        [409] = "Conflito",
        [413] = "Conteúdo grande demais",
        [415] = "Formato não suportado",
        [422] = "Regra de negócio violada",
        [429] = "Muitas requisições",
        [500] = "Erro interno",
    };

    public static ProblemDetails Create(HttpContext ctx, int status, string detail, string? code = null, IDictionary<string, string[]>? errors = null)
    {
        var pd = errors is null ? new ProblemDetails() : new ValidationProblemDetails(errors);
        pd.Status = status;
        pd.Title = Titles.GetValueOrDefault(status, "Erro");
        pd.Detail = detail;
        pd.Type = $"https://httpstatuses.io/{status}";
        pd.Instance = ctx.Request.Path;
        pd.Extensions["traceId"] = ctx.TraceIdentifier;
        if (code is not null) pd.Extensions["code"] = code;
        return pd;
    }

    public static async Task WriteAsync(HttpContext ctx, int status, string detail, string? code = null, IDictionary<string, string[]>? errors = null)
    {
        if (ctx.Response.HasStarted) return;
        var pd = Create(ctx, status, detail, code, errors);
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json; charset=utf-8";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize<object>(pd, Json));
    }

    /// <summary>"Experiences[0].StartDate" → "experiences[0].startDate".</summary>
    public static string CamelPath(string path) =>
        string.Join('.', path.Split('.').Select(p => p.Length == 0 ? p : char.ToLowerInvariant(p[0]) + p[1..]));
}
