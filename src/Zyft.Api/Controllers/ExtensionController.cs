using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Zyft.Application.Extension;

namespace Zyft.Api.Controllers;

/// <summary>Pareamento da extensão por código curto (RF-09).</summary>
[Route("api/v1/extension")]
public sealed class ExtensionController : ApiControllerBase
{
    /// <summary>Gera um código de 6 dígitos (válido por 10 min, uso único). Só a plataforma web logada.</summary>
    [HttpPost("pairing-codes")]
    [Authorize(Policy = Policies.WebUser)]
    [ProducesResponseType(typeof(PairingCodeDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePairingCode(CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Sender.Send(new CreatePairingCodeCommand(), ct));

    /// <summary>A extensão troca o código por tokens com <c>scope=extension</c>. Anônimo e limitado por IP.</summary>
    /// <response code="401">Código inválido ou expirado.</response>
    [HttpPost("pair")]
    [AllowAnonymous]
    [EnableRateLimiting(Policies.AuthRateLimit)]
    [ProducesResponseType(typeof(PairResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired)]
    public Task<PairResult> Pair(PairDeviceCommand cmd, CancellationToken ct) => Sender.Send(cmd, ct);

    /// <summary>Lista extensões pareadas.</summary>
    [HttpGet("devices")]
    [Authorize(Policy = Policies.WebUser)]
    public Task<IReadOnlyList<DeviceDto>> Devices(CancellationToken ct) => Sender.Send(new ListDevicesQuery(), ct);

    /// <summary>A extensão se desconecta: revoga o próprio dispositivo e libera a vaga de navegador do plano.</summary>
    [HttpDelete("devices/current")]
    [Authorize(Policy = Policies.ExtensionOrWeb)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeCurrent(CancellationToken ct) { await Sender.Send(new RevokeCurrentDeviceCommand(), ct); return NoContent(); }

    /// <summary>Revoga uma extensão (tokens dela deixam de valer imediatamente).</summary>
    [HttpDelete("devices/{id:guid}")]
    [Authorize(Policy = Policies.WebUser)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct) { await Sender.Send(new RevokeDeviceCommand(id), ct); return NoContent(); }
}
