using MediatR;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Audit;

namespace Zyft.Application.Audit;

/// <summary>Evento de auditoria (RF-20): data, ação, portal e quantidade — nunca valores.</summary>
public sealed record AuditEventDto(Guid Id, string Action, string? Portal, int? FieldCount, DateTime OccurredAt);

public sealed record ListAuditEventsQuery(int Limit = 200) : IRequest<IReadOnlyList<AuditEventDto>>;

public sealed class ListAuditEventsHandler(IRepository<AuditEvent> repo, ICurrentUser current) : IRequestHandler<ListAuditEventsQuery, IReadOnlyList<AuditEventDto>>
{
    public async Task<IReadOnlyList<AuditEventDto>> Handle(ListAuditEventsQuery q, CancellationToken ct)
    {
        var uid = current.UserId;
        var limit = Math.Clamp(q.Limit, 1, 1000);
        return (await repo.ListAsync(e => e.UserId == uid, ct))
            .OrderByDescending(e => e.OccurredAt).Take(limit)
            .Select(e => new AuditEventDto(e.Id, e.Action, e.Portal, e.FieldCount, e.OccurredAt)).ToList();
    }
}
