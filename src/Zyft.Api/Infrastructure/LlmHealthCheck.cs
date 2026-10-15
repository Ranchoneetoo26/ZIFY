using Microsoft.Extensions.Diagnostics.HealthChecks;
using Zyft.Application.Common.Interfaces;
using Zyft.Infrastructure.Ai;

namespace Zyft.Api.Infrastructure;

/// <summary>IA fora não derruba a API: o status fica "Degraded" e as regras seguem funcionando (RNF-12).</summary>
public sealed class LlmHealthCheck(ILlmClient llm) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        if (!llm.IsEnabled) return HealthCheckResult.Healthy("IA desativada (somente regras).");
        if (llm is OllamaLlmClient ollama)
            return await ollama.PingAsync(ct) ? HealthCheckResult.Healthy("IA local disponível.") : HealthCheckResult.Degraded("IA local indisponível: somente regras.");
        return HealthCheckResult.Healthy();
    }
}
