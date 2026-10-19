using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Answers;
using Zyft.Domain.Billing;
using Zyft.Domain.Extension;

namespace Zyft.Application.Billing;

/// <summary>
/// Planos e limites de uso (contrato v2). Verifica o limite e registra o consumo nas entidades rastreadas; o consumo
/// só fica gravado quando o caso de uso chama <c>SaveChanges</c> — se a ação falhar antes, nada é cobrado.
/// </summary>
public interface IUsageMeter
{
    Task<PlanKind> GetPlanAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Consome 1 do recurso. <paramref name="dedupKey"/> (ex.: origem+caminho da página) faz o recurso contar 1x por mês
    /// por chave: a chave é guardada só como hash. Lança <see cref="PlanLimitReachedException"/> (402).
    /// </summary>
    Task<UsageSource?> ConsumeAsync(Guid userId, UsageResource resource, string? dedupKey = null, CancellationToken ct = default);

    /// <summary>402 <c>answers</c> se o banco de respostas estiver cheio no plano atual.</summary>
    Task EnsureAnswerCapacityAsync(Guid userId, CancellationToken ct = default);

    /// <summary>402 <c>devices</c> se já houver o máximo de navegadores pareados ativos.</summary>
    Task EnsureDeviceCapacityAsync(Guid userId, CancellationToken ct = default);
}

public sealed class UsageMeter(
    IRepository<Subscription> subscriptions,
    IRepository<MonthlyUsage> usages,
    IRepository<UsagePack> packs,
    IRepository<UsageMark> marks,
    IRepository<ApprovedAnswer> answers,
    IRepository<ExtensionDevice> devices,
    ITokenService hasher,
    IClock clock) : IUsageMeter
{
    public async Task<PlanKind> GetPlanAsync(Guid userId, CancellationToken ct = default) =>
        (await subscriptions.FindAsync(s => s.UserId == userId, ct))?.Plan ?? PlanKind.Free;

    public async Task<UsageSource?> ConsumeAsync(Guid userId, UsageResource resource, string? dedupKey = null, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var month = PlanRules.MonthOf(now);
        string? keyHash = null;
        if (!string.IsNullOrWhiteSpace(dedupKey))
        {
            keyHash = hasher.Hash(dedupKey.Trim().ToLowerInvariant());
            if (await marks.AnyAsync(m => m.UserId == userId && m.Month == month && m.Resource == resource && m.KeyHash == keyHash, ct))
                return null; // mesma página no mesmo mês: não conta de novo
        }

        var plan = await GetPlanAsync(userId, ct);
        var usage = await GetOrStartUsageAsync(usages, userId, month, ct);
        var activePacks = await packs.ListAsync(p => p.UserId == userId && p.ExpiresAt > now, ct);
        var source = usage.Consume(resource, PlanRules.MonthlyLimit(plan, resource), activePacks, now);
        if (keyHash is not null) marks.Add(UsageMark.Create(userId, month, resource, keyHash));
        return source;
    }

    public async Task EnsureAnswerCapacityAsync(Guid userId, CancellationToken ct = default)
    {
        var limit = PlanRules.AnswersLimit(await GetPlanAsync(userId, ct));
        if (limit is null) return;
        var count = (await answers.ListAsync(a => a.UserId == userId, ct)).Count;
        if (count >= limit) throw new PlanLimitReachedException("answers", count, limit.Value);
    }

    public async Task EnsureDeviceCapacityAsync(Guid userId, CancellationToken ct = default)
    {
        var limit = PlanRules.DevicesLimit(await GetPlanAsync(userId, ct));
        var active = (await devices.ListAsync(d => d.UserId == userId && d.RevokedAt == null, ct)).Count;
        if (active >= limit) throw new PlanLimitReachedException("devices", active, limit);
    }

    internal static async Task<MonthlyUsage> GetOrStartUsageAsync(IRepository<MonthlyUsage> usages, Guid userId, string month, CancellationToken ct)
    {
        var usage = await usages.FindAsync(u => u.UserId == userId && u.Month == month, ct);
        if (usage is null)
        {
            usage = MonthlyUsage.Start(userId, month);
            usages.Add(usage);
        }

        return usage;
    }
}
