using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyft.Application.Sensitive;

namespace Zyft.Api.Controllers;

/// <summary>Dados sensíveis segregados e cifrados (RN-04, ADR-0014). Toda rota exige o consentimento SensitiveData (senão 403).</summary>
[Route("api/v1/sensitive-data")]
[Authorize(Policy = Policies.WebUser)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class SensitiveDataController : ApiControllerBase
{
    /// <summary>Lista os dados sensíveis (decifrados só para a própria pessoa).</summary>
    [HttpGet]
    public Task<IReadOnlyList<SensitiveDataDto>> List(CancellationToken ct) => Sender.Send(new ListSensitiveDataQuery(), ct);

    /// <summary>Cadastra um dado (kind: Gender, Ethnicity, Disability, Religion, SexualOrientation, Health, SocialName, Other).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SensitiveDataDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateSensitiveDataCommand cmd, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Sender.Send(cmd, ct));

    /// <summary>Altera o valor.</summary>
    [HttpPut("{id:guid}")]
    public Task<SensitiveDataDto> Update(Guid id, UpdateSensitiveDataCommand cmd, CancellationToken ct) => Sender.Send(cmd with { Id = id }, ct);

    /// <summary>Exclui o dado.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await Sender.Send(new DeleteSensitiveDataCommand(id), ct); return NoContent(); }
}
