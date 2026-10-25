using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyft.Application.Sites;

namespace Zyft.Api.Controllers;

/// <summary>Sites em que a extensão pode atuar (RF-10).</summary>
[Route("api/v1/authorized-sites")]
public sealed class AuthorizedSitesController : ApiControllerBase
{
    /// <summary>Lista as origens autorizadas (a extensão também consulta).</summary>
    [HttpGet]
    [Authorize(Policy = Policies.ExtensionOrWeb)]
    public Task<IReadOnlyList<AuthorizedSiteDto>> List(CancellationToken ct) => Sender.Send(new ListAuthorizedSitesQuery(), ct);

    /// <summary>
    /// Autoriza uma origem (aceita URL completa; guarda só esquema://host[:porta]). A extensão também pode chamar
    /// ("Usar neste site mesmo assim" no painel, sempre por clique da pessoa) — D-02.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.ExtensionOrWeb)]
    [ProducesResponseType(typeof(AuthorizedSiteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add(AddAuthorizedSiteCommand cmd, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Sender.Send(cmd, ct));

    /// <summary>Remove a autorização.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.WebUser)]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct) { await Sender.Send(new RemoveAuthorizedSiteCommand(id), ct); return NoContent(); }
}
