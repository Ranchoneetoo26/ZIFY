using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyft.Application.Answers;

namespace Zyft.Api.Controllers;

/// <summary>Respostas aprovadas para perguntas recorrentes (RF-06).</summary>
[Route("api/v1/answers")]
[Authorize(Policy = Policies.WebUser)]
public sealed class AnswersController : ApiControllerBase
{
    /// <summary>Lista as respostas aprovadas.</summary>
    [HttpGet]
    public Task<IReadOnlyList<AnswerDto>> List(CancellationToken ct) => Sender.Send(new ListAnswersQuery(), ct);

    /// <summary>Detalhe de uma resposta.</summary>
    [HttpGet("{id:guid}")]
    public Task<AnswerDto> Get(Guid id, CancellationToken ct) => Sender.Send(new GetAnswerQuery(id), ct);

    /// <summary>Aprova uma resposta (a extensão também pode aprovar o texto revisado no painel). Mesma pergunta → substitui.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.ExtensionOrWeb)]
    [ProducesResponseType(typeof(AnswerDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateAnswerCommand cmd, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Sender.Send(cmd, ct));

    /// <summary>Edita pergunta e texto.</summary>
    [HttpPut("{id:guid}")]
    public Task<AnswerDto> Update(Guid id, UpdateAnswerCommand cmd, CancellationToken ct) => Sender.Send(cmd with { Id = id }, ct);

    /// <summary>Exclui a resposta.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await Sender.Send(new DeleteAnswerCommand(id), ct); return NoContent(); }
}
