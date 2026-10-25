using FluentValidation;
using MediatR;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Audit;
using Zyft.Domain.Consents;
using Zyft.Domain.Sensitive;

namespace Zyft.Application.Consents;

public sealed record ConsentDto(ConsentPurpose Purpose, bool Granted, DateTime UpdatedAt);

public sealed record GetConsentsQuery : IRequest<IReadOnlyList<ConsentDto>>;

public sealed class GetConsentsHandler(IRepository<Consent> consents, ICurrentUser current) : IRequestHandler<GetConsentsQuery, IReadOnlyList<ConsentDto>>
{
    public async Task<IReadOnlyList<ConsentDto>> Handle(GetConsentsQuery q, CancellationToken ct)
    {
        var id = current.UserId;
        var list = await consents.ListAsync(c => c.UserId == id, ct);
        // Finalidades sem registro contam como não concedidas.
        return Enum.GetValues<ConsentPurpose>()
            .Select(p => list.FirstOrDefault(c => c.Purpose == p) is { } c ? new ConsentDto(p, c.Granted, c.UpdatedAt) : new ConsentDto(p, false, default))
            .ToList();
    }
}

/// <summary>
/// Liga/desliga uma finalidade. Desligar "dados sensíveis" também APAGA os dados sensíveis guardados
/// (minimização — LGPD; ADR-0014).
/// </summary>
public sealed record SetConsentCommand(ConsentPurpose Purpose, bool Granted) : IRequest<ConsentDto>;

public sealed class SetConsentValidator : AbstractValidator<SetConsentCommand>
{
    public SetConsentValidator() => RuleFor(x => x.Purpose).IsInEnum().WithMessage("Finalidade de consentimento desconhecida.");
}

public sealed class SetConsentHandler(
    IRepository<Consent> consents,
    IRepository<SensitiveData> sensitive,
    IRepository<AuditEvent> audit,
    ICurrentUser current,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<SetConsentCommand, ConsentDto>
{
    public async Task<ConsentDto> Handle(SetConsentCommand cmd, CancellationToken ct)
    {
        var id = current.UserId;
        var c = await consents.FindAsync(x => x.UserId == id && x.Purpose == cmd.Purpose, ct);
        if (c is null)
        {
            c = Consent.Create(id, cmd.Purpose, cmd.Granted);
            consents.Add(c);
        }
        else
        {
            c.Set(cmd.Granted);
        }

        audit.Add(AuditEvent.Record(id, AuditActions.ConsentChanged, clock.UtcNow));
        if (cmd.Purpose == ConsentPurpose.SensitiveData && !cmd.Granted)
        {
            var stored = await sensitive.ListAsync(s => s.UserId == id, ct);
            if (stored.Count > 0)
            {
                sensitive.RemoveRange(stored);
                audit.Add(AuditEvent.Record(id, AuditActions.SensitiveDataErased, clock.UtcNow, fieldCount: stored.Count));
            }
        }

        await uow.SaveChangesAsync(ct);
        return new ConsentDto(c.Purpose, c.Granted, c.UpdatedAt);
    }
}
