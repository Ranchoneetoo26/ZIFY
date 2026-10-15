using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyft.Application.Applications;

namespace Zyft.Api.Controllers;

/// <summary>Registro mínimo de candidaturas confirmadas (RF-19, ADR-0012).</summary>
[Route("api/v1/applications")]
public sealed class ApplicationsController : ApiControllerBase
{
    /// <summary>Lista as candidaturas (mais recentes primeiro).</summary>
    [HttpGet]
    [Authorize(Policy = Policies.ExtensionOrWeb)]
    public Task<IReadOnlyList<ApplicationDto>> List(CancellationToken ct) => Sender.Send(new ListApplicationsQuery(), ct);

    /// <summary>Detalhe.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.ExtensionOrWeb)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ApplicationDto> Get(Guid id, CancellationToken ct) => Sender.Send(new GetApplicationQuery(id), ct);

    /// <summary>Atualiza status e/ou notas.</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Policies.WebUser)]
    public Task<ApplicationDto> Patch(Guid id, PatchApplicationCommand cmd, CancellationToken ct) => Sender.Send(cmd with { Id = id }, ct);

    /// <summary>Exclui o registro.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.WebUser)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await Sender.Send(new DeleteApplicationCommand(id), ct); return NoContent(); }
}
