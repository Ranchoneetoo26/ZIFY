using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Zyft.Application.Common.Interfaces;
using Zyft.Application.Profiles;

namespace Zyft.Api.Infrastructure;

/// <summary>
/// Concorrência otimista do Perfil Mestre. Lê <c>If-Match</c> (ETag de <c>GET /profile</c>, ex.: <c>"7"</c>) nas
/// rotas que alteram o perfil e devolve <c>ETag</c> com a versão atual. Sem <c>If-Match</c> nada muda (a extensão e
/// clientes antigos continuam funcionando). <c>If-Match: *</c> também não verifica nada.
/// </summary>
public sealed class ProfileVersionFilter(IProfileConcurrency concurrency) : IAsyncActionFilter
{
    public static string ETagFor(int version) => $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!HttpMethods.IsGet(http.Request.Method) && http.Request.Headers.IfMatch.Count > 0)
        {
            var raw = http.Request.Headers.IfMatch.ToString().Trim();
            if (raw != "*")
            {
                if (!TryParse(raw, out var expected))
                {
                    context.Result = new ObjectResult(Problems.Create(http, 400, "Cabeçalho If-Match inválido. Envie o ETag recebido em GET /profile, como \"3\".", "bad_request"))
                    { StatusCode = 400, ContentTypes = { "application/problem+json" } };
                    return;
                }

                concurrency.ExpectedVersion = expected;
            }
        }

        var executed = await next();
        if (executed.Exception is not null && !executed.ExceptionHandled) return;
        var version = executed.Result is ObjectResult { Value: ProfileDto dto } ? dto.Version : concurrency.SavedVersion;
        if (version is { } v) http.Response.Headers.ETag = ETagFor(v);
    }

    /// <summary>Aceita <c>"7"</c>, <c>W/"7"</c> ou <c>7</c>; com vários valores, usa o primeiro.</summary>
    internal static bool TryParse(string header, out int version)
    {
        var first = header.Split(',')[0].Trim();
        if (first.StartsWith("W/", StringComparison.Ordinal)) first = first[2..];
        return int.TryParse(first.Trim('"'), NumberStyles.None, CultureInfo.InvariantCulture, out version);
    }
}
