using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Zyft.Application.Common;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Audit;
using Zyft.Domain.Consents;
using Zyft.Domain.Sensitive;

namespace Zyft.Application.Sensitive;

public sealed record SensitiveDataDto(Guid Id, SensitiveDataKind Kind, string Value, DateTime UpdatedAt);

public record SensitiveDataInput
{
    public SensitiveDataKind? Kind { get; init; }
    public string Value { get; init; } = string.Empty;
}

public sealed class SensitiveDataInputValidator<T> : AbstractValidator<T> where T : SensitiveDataInput
{
    public SensitiveDataInputValidator()
    {
        RuleFor(x => x.Kind).NotNull().WithMessage("Informe o tipo de dado.").IsInEnum().WithMessage("Tipo de dado inválido.");
        RuleFor(x => x.Value).NotEmpty().WithMessage("Informe o valor.").MaximumLength(200);
    }
}

public sealed record ListSensitiveDataQuery : IRequest<IReadOnlyList<SensitiveDataDto>>;
public sealed record CreateSensitiveDataCommand : SensitiveDataInput, IRequest<SensitiveDataDto>;
public sealed record UpdateSensitiveDataCommand : SensitiveDataInput, IRequest<SensitiveDataDto> { [JsonIgnore] public Guid Id { get; init; } }
public sealed record DeleteSensitiveDataCommand(Guid Id) : IRequest<Unit>;
public sealed class CreateSensitiveDataValidator : AbstractValidator<CreateSensitiveDataCommand> { public CreateSensitiveDataValidator() => Include(new SensitiveDataInputValidator<CreateSensitiveDataCommand>()); }
public sealed class UpdateSensitiveDataValidator : AbstractValidator<UpdateSensitiveDataCommand> { public UpdateSensitiveDataValidator() => Include(new SensitiveDataInputValidator<UpdateSensitiveDataCommand>()); }

/// <summary>
/// Dados sensíveis (RN-04, ADR-0014): toda operação exige o consentimento (c); o valor é cifrado (AES-GCM) antes
/// de ir ao banco e nunca é enviado à IA. Logs e auditoria registram só que houve alteração.
/// </summary>
public sealed class SensitiveDataHandlers(
    IRepository<SensitiveData> repo,
    IRepository<AuditEvent> audit,
    ISensitiveDataProtector protector,
    IConsentGuard consents,
    ICurrentUser current,
    IUnitOfWork uow,
    IClock clock) :
    IRequestHandler<ListSensitiveDataQuery, IReadOnlyList<SensitiveDataDto>>,
    IRequestHandler<CreateSensitiveDataCommand, SensitiveDataDto>,
    IRequestHandler<UpdateSensitiveDataCommand, SensitiveDataDto>,
    IRequestHandler<DeleteSensitiveDataCommand, Unit>
{
    public async Task<IReadOnlyList<SensitiveDataDto>> Handle(ListSensitiveDataQuery q, CancellationToken ct)
    {
        var uid = await EnsureConsent(ct);
        return (await repo.ListAsync(s => s.UserId == uid, ct)).OrderBy(s => s.Kind).Select(ToDto).ToList();
    }

    public async Task<SensitiveDataDto> Handle(CreateSensitiveDataCommand cmd, CancellationToken ct)
    {
        var uid = await EnsureConsent(ct);
        var kind = cmd.Kind!.Value;
        if (await repo.AnyAsync(s => s.UserId == uid && s.Kind == kind, ct))
            throw new ConflictException("Já existe um valor para este tipo de dado. Edite o existente.");
        var s = SensitiveData.Create(uid, kind, protector.Protect(cmd.Value.Trim()));
        repo.Add(s);
        audit.Add(AuditEvent.Record(uid, AuditActions.SensitiveDataChanged, clock.UtcNow));
        await uow.SaveChangesAsync(ct);
        return ToDto(s);
    }

    public async Task<SensitiveDataDto> Handle(UpdateSensitiveDataCommand cmd, CancellationToken ct)
    {
        var uid = await EnsureConsent(ct);
        var s = await repo.FindAsync(x => x.Id == cmd.Id && x.UserId == uid, ct) ?? throw new NotFoundException("Dado não encontrado.");
        if (s.Kind != cmd.Kind) throw new Zyft.Domain.Common.DomainException("sensitive.kind_immutable", "O tipo do dado não pode ser alterado; exclua e cadastre de novo.");
        s.Replace(protector.Protect(cmd.Value.Trim()));
        audit.Add(AuditEvent.Record(uid, AuditActions.SensitiveDataChanged, clock.UtcNow));
        await uow.SaveChangesAsync(ct);
        return ToDto(s);
    }

    public async Task<Unit> Handle(DeleteSensitiveDataCommand cmd, CancellationToken ct)
    {
        var uid = await EnsureConsent(ct);
        var s = await repo.FindAsync(x => x.Id == cmd.Id && x.UserId == uid, ct) ?? throw new NotFoundException("Dado não encontrado.");
        repo.Remove(s);
        audit.Add(AuditEvent.Record(uid, AuditActions.SensitiveDataChanged, clock.UtcNow));
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private async Task<Guid> EnsureConsent(CancellationToken ct)
    {
        var uid = current.UserId;
        await consents.EnsureGrantedAsync(uid, ConsentPurpose.SensitiveData, ct);
        return uid;
    }

    private SensitiveDataDto ToDto(SensitiveData s) => new(s.Id, s.Kind, protector.Unprotect(s.EncryptedValue), s.UpdatedAt);
}
