using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyft.Application.Audit;

namespace Zyft.Api.Controllers;

/// <summary>Registro das ações (RF-20): data, ação, portal e quantidade de campos — nunca valores.</summary>
[Route("api/v1/audit-events")]
[Authorize(Policy = Policies.WebUser)]
public sealed class AuditEventsController : ApiControllerBase
{
    /// <summary>Últimos eventos (padrão 200, máx. 1000).</summary>
    [HttpGet]
    public Task<IReadOnlyList<AuditEventDto>> List([FromQuery] int limit = 200, CancellationToken ct = default) => Sender.Send(new ListAuditEventsQuery(limit), ct);
}
