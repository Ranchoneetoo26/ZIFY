using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Zyft.Application.Common.Behaviors;

/// <summary>
/// Registra o nome do caso de uso e a duração. Nunca registra o conteúdo do request,
/// que pode ter dados pessoais (RNF-09, ADR-0015).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await next();
        }
        finally
        {
            logger.LogInformation("Caso de uso {UseCase} concluído em {ElapsedMs} ms", typeof(TRequest).Name, sw.ElapsedMilliseconds);
        }
    }
}
