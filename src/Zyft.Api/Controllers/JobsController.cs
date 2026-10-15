using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Zyft.Application.Jobs;

namespace Zyft.Api.Controllers;

/// <summary>Análise da vaga visível (RF-17).</summary>
[Route("api/v1/jobs")]
[Authorize(Policy = Policies.ExtensionOrWeb)]
[EnableRateLimiting(Policies.AiRateLimit)]
public sealed class JobsController : ApiControllerBase
{
    /// <summary>
    /// Requisitos com status atende/parcial/nao_encontrado/confirmar e evidência = linha real numerada do perfil
    /// (ADR-0013). Nunca nota ou porcentagem. Exige consentimento JobAnalysis (403).
    /// </summary>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(JobAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public Task<JobAnalysisResult> Analyze(AnalyzeJobCommand cmd, CancellationToken ct) => Sender.Send(cmd, ct);
}
