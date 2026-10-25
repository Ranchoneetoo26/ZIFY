using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Audit;
using Zyft.Domain.Profiles;

namespace Zyft.Application.Profiles;

// Campos personalizados ("Informações da sua área"). Rótulo sensível → 422 profile.custom_field_sensitive (domínio).

public sealed record CustomFieldDto(Guid Id, string Label, string Value, DateTime UpdatedAt)
{
    public static CustomFieldDto From(CustomField f) => new(f.Id, f.Label, f.Value, f.UpdatedAt);
}

public record CustomFieldInput
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

public sealed class CustomFieldInputValidator<T> : AbstractValidator<T> where T : CustomFieldInput
{
    public CustomFieldInputValidator()
    {
        RuleFor(x => x.Label).NotEmpty().WithMessage("Informe o nome do campo.")
            .MaximumLength(CustomField.MaxLabel).WithMessage($"O nome do campo deve ter no máximo {CustomField.MaxLabel} caracteres.");
        RuleFor(x => x.Value).NotEmpty().WithMessage("Informe o valor do campo.")
            .MaximumLength(CustomField.MaxValue).WithMessage($"O valor do campo deve ter no máximo {CustomField.MaxValue} caracteres.");
    }
}

public sealed record ListCustomFieldsQuery : IRequest<IReadOnlyList<CustomFieldDto>>;
public sealed record AddCustomFieldCommand : CustomFieldInput, IRequest<CustomFieldDto>;
public sealed record UpdateCustomFieldCommand : CustomFieldInput, IRequest<CustomFieldDto> { [JsonIgnore] public Guid Id { get; init; } }
public sealed record DeleteCustomFieldCommand(Guid Id) : IRequest<Unit>;
public sealed class AddCustomFieldValidator : AbstractValidator<AddCustomFieldCommand> { public AddCustomFieldValidator() => Include(new CustomFieldInputValidator<AddCustomFieldCommand>()); }
public sealed class UpdateCustomFieldValidator : AbstractValidator<UpdateCustomFieldCommand> { public UpdateCustomFieldValidator() => Include(new CustomFieldInputValidator<UpdateCustomFieldCommand>()); }

public sealed class CustomFieldHandlers(IProfileRepository profiles, IRepository<AuditEvent> audit, ICurrentUser current, IUnitOfWork uow, IClock clock) :
    IRequestHandler<ListCustomFieldsQuery, IReadOnlyList<CustomFieldDto>>,
    IRequestHandler<AddCustomFieldCommand, CustomFieldDto>,
    IRequestHandler<UpdateCustomFieldCommand, CustomFieldDto>,
    IRequestHandler<DeleteCustomFieldCommand, Unit>
{
    public async Task<IReadOnlyList<CustomFieldDto>> Handle(ListCustomFieldsQuery q, CancellationToken ct) =>
        ProfileFacts.OrderedCustomFields(await profiles.LoadAsync(current, ct)).Select(CustomFieldDto.From).ToList();

    public async Task<CustomFieldDto> Handle(AddCustomFieldCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        var f = p.AddCustomField(cmd.Label, cmd.Value);
        await SaveAsync(ct);
        return CustomFieldDto.From(f);
    }

    public async Task<CustomFieldDto> Handle(UpdateCustomFieldCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        var f = p.UpdateCustomField(cmd.Id, cmd.Label, cmd.Value) ?? throw new NotFoundException("Campo personalizado não encontrado.");
        await SaveAsync(ct);
        return CustomFieldDto.From(f);
    }

    public async Task<Unit> Handle(DeleteCustomFieldCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        if (!p.RemoveCustomField(cmd.Id)) throw new NotFoundException("Campo personalizado não encontrado.");
        await SaveAsync(ct);
        return Unit.Value;
    }

    // Auditoria só com a ação (nunca rótulo nem valor).
    private async Task SaveAsync(CancellationToken ct)
    {
        audit.Add(AuditEvent.Record(current.UserId, AuditActions.CustomFieldChanged, clock.UtcNow));
        await uow.SaveChangesAsync(ct);
    }
}
