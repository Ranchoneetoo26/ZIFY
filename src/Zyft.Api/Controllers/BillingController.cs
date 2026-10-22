using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyft.Application.Billing;

namespace Zyft.Api.Controllers;

/// <summary>
/// Planos e uso (contrato v2). Pagamento SIMULADO — ambiente de demonstração, nenhuma cobrança real.
/// Limite estourado em qualquer rota → 402 <c>plan.limit_reached</c> com <c>resource</c>, <c>used</c> e <c>limit</c>.
/// </summary>
[Route("api/v1/billing")]
public sealed class BillingController : ApiControllerBase
{
    /// <summary>
    /// Plano, preços, uso do mês, limites, bônus recompensado e pacotes ativos. <c>trialAvailable</c> diz se a próxima
    /// assinatura ganha os <c>trialDays</c> dias grátis (só na primeira vez por conta).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Policies.ExtensionOrWeb)]
    public Task<BillingDto> Get(CancellationToken ct) => Sender.Send(new GetBillingQuery(), ct);

    /// <summary>Assina o Plus (period = monthly | yearly). Checkout simulado; 7 dias grátis na primeira vez.</summary>
    [HttpPost("subscribe")]
    [Authorize(Policy = Policies.WebUser)]
    [ProducesResponseType(typeof(BillingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<BillingDto> Subscribe(SubscribeCommand cmd, CancellationToken ct) => Sender.Send(cmd, ct);

    /// <summary>Cancela o Plus. Na demonstração volta ao Gratuito na hora.</summary>
    [HttpPost("cancel")]
    [Authorize(Policy = Policies.WebUser)]
    public Task<BillingDto> Cancel(CancellationToken ct) => Sender.Send(new CancelSubscriptionCommand(), ct);

    /// <summary>Compra (simulada) um pacote avulso: +15 candidaturas assistidas e +10 sugestões de IA, válido por 60 dias.</summary>
    [HttpPost("packs")]
    [Authorize(Policy = Policies.WebUser)]
    [ProducesResponseType(typeof(BillingDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> BuyPack(CancellationToken ct) => StatusCode(StatusCodes.Status201Created, await Sender.Send(new BuyPackCommand(), ct));
}
