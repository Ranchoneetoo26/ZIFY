using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyft.Application.Account;

namespace Zyft.Api.Controllers;

/// <summary>Conta da pessoa: dados, exportação e exclusão (RF-08).</summary>
[Route("api/v1/me")]
public sealed class MeController : ApiControllerBase
{
    /// <summary>Dados da conta autenticada (web ou extensão).</summary>
    [HttpGet]
    [Authorize(Policy = Policies.ExtensionOrWeb)]
    public Task<MeDto> Get(CancellationToken ct) => Sender.Send(new GetMeQuery(), ct);

    /// <summary>Exporta todos os dados em JSON legível. Só pela plataforma web.</summary>
    /// <response code="403">Token de extensão não pode exportar.</response>
    [HttpGet("export")]
    [Authorize(Policy = Policies.WebUser)]
    [ProducesResponseType(typeof(AccountExport), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var data = await Sender.Send(new ExportMeQuery(), ct);
        Response.Headers.ContentDisposition = "attachment; filename=\"zyft-meus-dados.json\"";
        return Ok(data);
    }

    /// <summary>Exclui a conta e tudo o que depende dela; encerra o pareamento das extensões.</summary>
    [HttpDelete]
    [Authorize(Policy = Policies.WebUser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(CancellationToken ct)
    {
        await Sender.Send(new DeleteMeCommand(), ct);
        return NoContent();
    }
}
