using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyft.Application.Ads;
using Zyft.Application.Billing;

namespace Zyft.Api.Controllers;

/// <summary>
/// Anúncios (contrato v2). Escolhidos só com área, cargos desejados e texto do requisito da vaga — nunca nome, contato
/// ou dados sensíveis. 204 para Plus, espaço proibido, nenhum anúncio ou tudo oculto.
/// </summary>
[Route("api/v1/ads")]
[Authorize(Policy = Policies.ExtensionOrWeb)]
public sealed class AdsController : ApiControllerBase
{
    /// <summary>Um anúncio para o espaço (placement) ou 204.</summary>
    /// <param name="placement">web.profile.rail | web.applications.feed | web.banner | ext.panel.banner | ext.job.contextual | ext.after-complete.</param>
    /// <param name="topic">Texto do requisito da vaga (opcional; obrigatório na prática para ext.job.contextual).</param>
    /// <param name="ct">Cancelamento.</param>
    [HttpGet]
    [ProducesResponseType(typeof(AdDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Get([FromQuery] string? placement, [FromQuery] string? topic, CancellationToken ct)
    {
        var ad = await Sender.Send(new GetAdQuery(placement, topic), ct);
        return ad is null ? NoContent() : Ok(ad);
    }

    /// <summary>Oculta o anúncio (scope = ad, por 30 dias) ou o assunto (scope = topic, para sempre).</summary>
    [HttpPost("{id}/hide")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Hide(string id, [FromBody] HideAdCommand? cmd, CancellationToken ct)
    {
        await Sender.Send((cmd ?? new HideAdCommand()) with { Id = id }, ct);
        return NoContent();
    }

    /// <summary>Clique: soma no contador agregado do anúncio (não guarda quem clicou).</summary>
    [HttpPost("{id}/click")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Click(string id, CancellationToken ct)
    {
        await Sender.Send(new ClickAdCommand(id), ct);
        return NoContent();
    }

    /// <summary>Oferta recompensada vista: +3 candidaturas assistidas no mês (até 3 vezes). 409 ads.rewarded_exhausted.</summary>
    [HttpPost("rewarded/claim")]
    [ProducesResponseType(typeof(BillingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<BillingDto> ClaimRewarded(CancellationToken ct) => Sender.Send(new ClaimRewardedCommand(), ct);
}
