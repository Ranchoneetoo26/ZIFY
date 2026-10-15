using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Zyft.Application.Common.Exceptions;
using Zyft.Domain.Common;

namespace Zyft.Api.Infrastructure;

/// <summary>
/// Middleware global: converte exceções em ProblemDetails pt-BR. 400 validação (com <c>errors</c>), 401, 403, 404, 409,
/// 413, 422 regra de negócio e 500 sem stack trace. Loga só o tipo da exceção e o caminho (nunca o corpo).
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex) when (!ctx.RequestAborted.IsCancellationRequested || ex is not OperationCanceledException)
        {
            await HandleAsync(ctx, ex);
        }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        switch (ex)
        {
            case ValidationException ve:
                var errors = ve.Errors
                    .GroupBy(e => Problems.CamelPath(string.IsNullOrEmpty(e.PropertyName) ? "body" : e.PropertyName))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
                await Problems.WriteAsync(ctx, 400, "Um ou mais campos são inválidos.", "validation_failed", errors);
                break;
            case UnauthorizedException ue:
                await Problems.WriteAsync(ctx, 401, ue.Message, "unauthorized");
                break;
            case ForbiddenException fe:
                await Problems.WriteAsync(ctx, 403, fe.Message, fe.Code);
                break;
            case NotFoundException nf:
                await Problems.WriteAsync(ctx, 404, nf.Message, "not_found");
                break;
            case ConflictException ce:
                await Problems.WriteAsync(ctx, 409, ce.Message, "conflict");
                break;
            case DbUpdateException:
                logger.LogWarning("Conflito de persistência em {Path}", ctx.Request.Path);
                await Problems.WriteAsync(ctx, 409, "Não foi possível salvar: o registro conflita com outro já existente.", "conflict");
                break;
            case DomainException de:
                await Problems.WriteAsync(ctx, 422, de.Message, de.Code);
                break;
            case BadHttpRequestException bre when bre.StatusCode == StatusCodes.Status413PayloadTooLarge:
                await Problems.WriteAsync(ctx, 413, "O arquivo ou corpo enviado é grande demais (máximo 10 MB).", "payload_too_large");
                break;
            case BadHttpRequestException bre:
                await Problems.WriteAsync(ctx, bre.StatusCode, "Requisição malformada.", "bad_request");
                break;
            default:
                logger.LogError(ex, "Erro não tratado em {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
                await Problems.WriteAsync(ctx, 500, "Ocorreu um erro inesperado. Tente novamente; se persistir, informe o código de rastreio.", "internal_error");
                break;
        }
    }
}
