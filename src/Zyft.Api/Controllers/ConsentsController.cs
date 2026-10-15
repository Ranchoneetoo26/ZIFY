using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Consents;
using Zyft.Domain.Consents;

namespace Zyft.Api.Controllers;

/// <summary>Consentimentos por finalidade (RF-07): AiText, JobAnalysis, SensitiveData.</summary>
[Route("api/v1/consents")]
[Authorize(Policy = Policies.WebUser)]
public sealed class ConsentsController : ApiControllerBase
{
    /// <summary>Lista as finalidades e se estão concedidas.</summary>
    [HttpGet]
    public Task<IReadOnlyList<ConsentDto>> List(CancellationToken ct) => Sender.Send(new GetConsentsQuery(), ct);

    /// <summary>Liga/desliga uma finalidade. Desligar SensitiveData apaga os dados sensíveis guardados.</summary>
    /// <param name="purpose">AiText | JobAnalysis | SensitiveData (aceita também ai-text, jobAnalysis…).</param>
    /// <param name="body">{ "granted": true|false }</param>
    /// <param name="ct">Cancelamento.</param>
    [HttpPut("{purpose}")]
    [ProducesResponseType(typeof(ConsentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ConsentDto> Set(string purpose, SetConsentBody body, CancellationToken ct)
    {
        var normalized = purpose.Replace("-", string.Empty).Replace("_", string.Empty);
        if (!Enum.TryParse<ConsentPurpose>(normalized, true, out var p) || !Enum.IsDefined(p))
            throw new NotFoundException($"Finalidade de consentimento desconhecida: {purpose}.");
        return Sender.Send(new SetConsentCommand(p, body.Granted), ct);
    }

    /// <summary>Corpo de PUT /consents/{purpose}.</summary>
    /// <param name="Granted">Se a finalidade fica ligada.</param>
    public sealed record SetConsentBody(bool Granted);
}
