using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Zyft.Application.Applications;
using Zyft.Application.Forms;

namespace Zyft.Api.Controllers;

/// <summary>
/// Formulários do portal (extensão). A API nunca envia nada ao portal e nunca devolve valor para senha, CAPTCHA,
/// arquivo ou declaração; dado sensível só com consentimento e sempre desmarcado (RN-01, RF-18).
/// </summary>
[Route("api/v1/forms")]
[Authorize(Policy = Policies.ExtensionOrWeb)]
[EnableRateLimiting(Policies.AiRateLimit)]
public sealed class FormsController : ApiControllerBase
{
    /// <summary>De-para campo → Perfil Mestre: regras, IA que só aponta chave, validação (RF-12). Devolve um item por campo enviado.</summary>
    [HttpPost("mappings")]
    [ProducesResponseType(typeof(MappingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<MappingResult> Map(MapFieldsCommand cmd, CancellationToken ct) => Sender.Send(cmd, ct);

    /// <summary>Sugestão de texto para pergunta aberta, só com fatos do perfil (RF-15). Exige consentimento AiText (403).</summary>
    [HttpPost("open-answers")]
    [ProducesResponseType(typeof(OpenAnswerResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public Task<OpenAnswerResult> OpenAnswer(OpenAnswerCommand cmd, CancellationToken ct) => Sender.Send(cmd, ct);

    /// <summary>Registra que a pessoa concluiu a candidatura no portal POR CONTA PRÓPRIA (RF-19). Cria a candidatura e o evento de auditoria.</summary>
    [HttpPost("completions")]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Complete(CompleteFormCommand cmd, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Sender.Send(cmd, ct));
}
