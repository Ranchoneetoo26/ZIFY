using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Applications;

namespace Zyft.Application.Applications;

public sealed record ApplicationDto(Guid Id, string Portal, string JobTitle, string? Company, string? JobUrl, ApplicationStatus Status, int FieldsFilled, DateTime ConfirmedAt, string? Notes)
{
    public static ApplicationDto From(JobApplication a) => new(a.Id, a.Portal, a.JobTitle, a.Company, a.JobUrl, a.Status, a.FieldsFilled, a.ConfirmedAt, a.Notes);
}

public sealed record ListApplicationsQuery : IRequest<IReadOnlyList<ApplicationDto>>;
public sealed record GetApplicationQuery(Guid Id) : IRequest<ApplicationDto>;

public sealed record PatchApplicationCommand : IRequest<ApplicationDto>
{
    [JsonIgnore] public Guid Id { get; init; }
    public ApplicationStatus? Status { get; init; }
    public string? Notes { get; init; }
}

public sealed record DeleteApplicationCommand(Guid Id) : IRequest<Unit>;

public sealed class PatchApplicationValidator : AbstractValidator<PatchApplicationCommand>
{
    public PatchApplicationValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Status inválido.");
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x).Must(x => x.Status is not null || x.Notes is not null).WithName("body").WithMessage("Informe status e/ou notes.");
    }
}

public sealed class ApplicationHandlers(IRepository<JobApplication> repo, ICurrentUser current, IUnitOfWork uow) :
    IRequestHandler<ListApplicationsQuery, IReadOnlyList<ApplicationDto>>,
    IRequestHandler<GetApplicationQuery, ApplicationDto>,
    IRequestHandler<PatchApplicationCommand, ApplicationDto>,
    IRequestHandler<DeleteApplicationCommand, Unit>
{
    public async Task<IReadOnlyList<ApplicationDto>> Handle(ListApplicationsQuery q, CancellationToken ct)
    {
        var uid = current.UserId;
        return (await repo.ListAsync(a => a.UserId == uid, ct)).OrderByDescending(a => a.ConfirmedAt).Select(ApplicationDto.From).ToList();
    }

    public async Task<ApplicationDto> Handle(GetApplicationQuery q, CancellationToken ct) => ApplicationDto.From(await Load(q.Id, ct));

    public async Task<ApplicationDto> Handle(PatchApplicationCommand cmd, CancellationToken ct)
    {
        var a = await Load(cmd.Id, ct);
        a.Update(cmd.Status, cmd.Notes);
        await uow.SaveChangesAsync(ct);
        return ApplicationDto.From(a);
    }

    public async Task<Unit> Handle(DeleteApplicationCommand cmd, CancellationToken ct)
    {
        repo.Remove(await Load(cmd.Id, ct));
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private async Task<JobApplication> Load(Guid id, CancellationToken ct)
    {
        var uid = current.UserId;
        return await repo.FindAsync(a => a.Id == id && a.UserId == uid, ct) ?? throw new NotFoundException("Candidatura não encontrada.");
    }
}
