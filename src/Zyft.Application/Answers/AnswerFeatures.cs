using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Zyft.Application.Common;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Answers;

namespace Zyft.Application.Answers;

public sealed record AnswerDto(Guid Id, string Question, string Text, DateTime ApprovedAt, DateTime? LastUsedAt)
{
    public static AnswerDto From(ApprovedAnswer a) => new(a.Id, a.Question, a.Text, a.ApprovedAt, a.LastUsedAt);
}

public record AnswerInput
{
    public string Question { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
}

public sealed class AnswerInputValidator<T> : AbstractValidator<T> where T : AnswerInput
{
    public AnswerInputValidator()
    {
        RuleFor(x => x.Question).Visible().WithMessage("Informe a pergunta.").MaximumLength(500);
        RuleFor(x => x.Text).Visible().WithMessage("Informe a resposta.").MaximumLength(5000);
    }
}

public sealed record ListAnswersQuery : IRequest<IReadOnlyList<AnswerDto>>;
public sealed record GetAnswerQuery(Guid Id) : IRequest<AnswerDto>;
public sealed record CreateAnswerCommand : AnswerInput, IRequest<AnswerDto>;
public sealed record UpdateAnswerCommand : AnswerInput, IRequest<AnswerDto> { [JsonIgnore] public Guid Id { get; init; } }
public sealed record DeleteAnswerCommand(Guid Id) : IRequest<Unit>;
public sealed class CreateAnswerValidator : AbstractValidator<CreateAnswerCommand> { public CreateAnswerValidator() => Include(new AnswerInputValidator<CreateAnswerCommand>()); }
public sealed class UpdateAnswerValidator : AbstractValidator<UpdateAnswerCommand> { public UpdateAnswerValidator() => Include(new AnswerInputValidator<UpdateAnswerCommand>()); }

/// <summary>Respostas aprovadas (RF-06). Aprovar a mesma pergunta de novo substitui o texto.</summary>
public sealed class AnswerHandlers(IRepository<ApprovedAnswer> answers, ICurrentUser current, IUnitOfWork uow, IClock clock, Billing.IUsageMeter meter) :
    IRequestHandler<ListAnswersQuery, IReadOnlyList<AnswerDto>>,
    IRequestHandler<GetAnswerQuery, AnswerDto>,
    IRequestHandler<CreateAnswerCommand, AnswerDto>,
    IRequestHandler<UpdateAnswerCommand, AnswerDto>,
    IRequestHandler<DeleteAnswerCommand, Unit>
{
    public async Task<IReadOnlyList<AnswerDto>> Handle(ListAnswersQuery q, CancellationToken ct)
    {
        var uid = current.UserId;
        return (await answers.ListAsync(a => a.UserId == uid, ct)).OrderByDescending(a => a.ApprovedAt).Select(AnswerDto.From).ToList();
    }

    public async Task<AnswerDto> Handle(GetAnswerQuery q, CancellationToken ct) => AnswerDto.From(await Load(q.Id, ct));

    public async Task<AnswerDto> Handle(CreateAnswerCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var question = cmd.Question.Trim();
        var existing = await answers.FindAsync(a => a.UserId == uid && a.Question == question, ct);
        if (existing is not null)
        {
            existing.Update(question, cmd.Text, clock.UtcNow);
        }
        else
        {
            await meter.EnsureAnswerCapacityAsync(uid, ct); // banco de respostas: até 15 no Gratuito (402)
            existing = ApprovedAnswer.Approve(uid, question, cmd.Text, clock.UtcNow);
            answers.Add(existing);
        }

        await uow.SaveChangesAsync(ct);
        return AnswerDto.From(existing);
    }

    public async Task<AnswerDto> Handle(UpdateAnswerCommand cmd, CancellationToken ct)
    {
        var a = await Load(cmd.Id, ct);
        var uid = current.UserId;
        var question = cmd.Question.Trim();
        // Mudar a pergunta para a de outra resposta deixaria dois registros para o mesmo upsert (POST /answers).
        if (question != a.Question && await answers.AnyAsync(x => x.UserId == uid && x.Id != a.Id && x.Question == question, ct))
            throw new ConflictException("Você já tem uma resposta para esta pergunta. Edite a existente ou exclua uma delas.", "answer.duplicated");

        a.Update(question, cmd.Text, clock.UtcNow);
        await uow.SaveChangesAsync(ct);
        return AnswerDto.From(a);
    }

    public async Task<Unit> Handle(DeleteAnswerCommand cmd, CancellationToken ct)
    {
        answers.Remove(await Load(cmd.Id, ct));
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private async Task<ApprovedAnswer> Load(Guid id, CancellationToken ct)
    {
        var uid = current.UserId;
        return await answers.FindAsync(a => a.Id == id && a.UserId == uid, ct) ?? throw new NotFoundException("Resposta não encontrada.");
    }
}
