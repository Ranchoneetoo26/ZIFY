using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Zyft.Application.Imports;

namespace Zyft.Api.Controllers;

/// <summary>Importação de currículo com revisão (RF-03, RF-04). Nada entra no perfil sem aprovação.</summary>
[Route("api/v1/resume-imports")]
[Authorize(Policy = Policies.WebUser)]
public sealed class ResumeImportsController : ApiControllerBase
{
    /// <summary>Envia um currículo PDF (com texto) ou DOCX, até 10 MB, e recebe a proposta para revisão. O arquivo não é guardado.</summary>
    /// <param name="file">Campo multipart <c>file</c>.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <response code="201">Proposta criada (status Pending).</response>
    /// <response code="400">Arquivo ausente, formato não suportado ou maior que 10 MB.</response>
    /// <response code="422">PDF sem texto (escaneado) ou ilegível.</response>
    [HttpPost]
    [EnableRateLimiting(Policies.AiRateLimit)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 12 * 1024 * 1024)]
    [ProducesResponseType(typeof(ResumeImportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(IFormFile? file, CancellationToken ct)
    {
        byte[] content = Array.Empty<byte>();
        if (file is not null)
        {
            using var ms = new MemoryStream();
            // Lê no máximo 10 MB + 1 byte: o validador acusa "maior que 10 MB" sem carregar arquivos enormes.
            await file.OpenReadStream().CopyToAsync(ms, ct);
            content = ms.ToArray();
        }

        var dto = await Sender.Send(new CreateResumeImportCommand(file?.FileName ?? string.Empty, file?.ContentType, content), ct);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    /// <summary>Consulta a importação e a proposta (enquanto pendente).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResumeImportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ResumeImportDto> Get(Guid id, CancellationToken ct) => Sender.Send(new GetResumeImportQuery(id), ct);

    /// <summary>
    /// Aplica só os itens aceitos/editados: corpo no mesmo formato da proposta (<c>profile</c>, <c>experiences</c>,
    /// <c>educations</c>, <c>courses</c>, <c>skills</c>, <c>languages</c>). O que não vier é descartado.
    /// </summary>
    /// <response code="422">Importação já aplicada ou descartada.</response>
    [HttpPost("{id:guid}/apply")]
    [ProducesResponseType(typeof(ApplyResumeImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public Task<ApplyResumeImportResult> Apply(Guid id, ApplyResumeImportCommand cmd, CancellationToken ct) => Sender.Send(cmd with { Id = id }, ct);

    /// <summary>Descarta a proposta; nada muda no perfil.</summary>
    [HttpPost("{id:guid}/discard")]
    [ProducesResponseType(typeof(ResumeImportDto), StatusCodes.Status200OK)]
    public Task<ResumeImportDto> Discard(Guid id, CancellationToken ct) => Sender.Send(new DiscardResumeImportCommand(id), ct);
}
