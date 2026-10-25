using Zyft.Domain.Common;

namespace Zyft.Domain.Billing;

/// <summary>
/// Uso do mês (contadores agregados, sem conteúdo). <c>*Used</c> conta o que saiu do limite do mês (no Plus, o total);
/// o bônus vem das ofertas recompensadas e só vale para candidaturas assistidas.
/// </summary>
public sealed class MonthlyUsage : Entity
{
    private MonthlyUsage() { }

    public Guid UserId { get; private set; }

    /// <summary>AAAA-MM (UTC).</summary>
    public string Month { get; private set; } = default!;

    public int AssistedUsed { get; private set; }
    public int AiUsed { get; private set; }
    public int JobAnalysesUsed { get; private set; }
    public int ResumeImportsUsed { get; private set; }
    public int BonusGranted { get; private set; }
    public int BonusUsed { get; private set; }
    public int RewardedClaims { get; private set; }

    public int BonusRemaining => Math.Max(0, BonusGranted - BonusUsed);

    public static MonthlyUsage Start(Guid userId, string month) => new() { UserId = userId, Month = month };

    public int UsedOf(UsageResource r) => r switch
    {
        UsageResource.AssistedApplications => AssistedUsed,
        UsageResource.AiSuggestions => AiUsed,
        UsageResource.JobAnalyses => JobAnalysesUsed,
        UsageResource.ResumeImports => ResumeImportsUsed,
        _ => 0,
    };

    /// <summary>
    /// Consome um crédito na ordem: limite do mês → bônus recompensado (só candidaturas) → pacote que vence primeiro.
    /// Sem crédito: <see cref="PlanLimitReachedException"/>. <paramref name="limit"/> null = sem limite (Plus).
    /// </summary>
    public UsageSource Consume(UsageResource r, int? limit, IEnumerable<UsagePack> packs, DateTime now)
    {
        if (limit is null || UsedOf(r) < limit.Value)
        {
            Increment(r);
            return UsageSource.Plan;
        }

        if (r == UsageResource.AssistedApplications && BonusRemaining > 0)
        {
            BonusUsed++;
            return UsageSource.Bonus;
        }

        var pack = packs.Where(p => p.IsActive(now) && p.RemainingOf(r) > 0).OrderBy(p => p.ExpiresAt).FirstOrDefault();
        if (pack is not null)
        {
            pack.ConsumeOne(r);
            return UsageSource.Pack;
        }

        throw new PlanLimitReachedException(PlanRules.ApiName(r), UsedOf(r), limit.Value);
    }

    /// <summary>Oferta recompensada vista: +3 candidaturas assistidas no mês, até 3 vezes.</summary>
    public void ClaimRewarded()
    {
        if (RewardedClaims >= PlanRules.RewardedMaxPerMonth)
            throw new RewardedExhaustedException($"Você já ganhou o bônus máximo deste mês (+{PlanRules.RewardedMaxPerMonth * PlanRules.RewardedCreditsPerClaim}). Ele volta no mês que vem.");
        RewardedClaims++;
        BonusGranted += PlanRules.RewardedCreditsPerClaim;
    }

    private void Increment(UsageResource r)
    {
        switch (r)
        {
            case UsageResource.AssistedApplications: AssistedUsed++; break;
            case UsageResource.AiSuggestions: AiUsed++; break;
            case UsageResource.JobAnalyses: JobAnalysesUsed++; break;
            case UsageResource.ResumeImports: ResumeImportsUsed++; break;
        }
    }
}

/// <summary>Pacote avulso (R$ 4,90, simulado): +15 candidaturas assistidas e +10 sugestões de IA, válido por 60 dias.</summary>
public sealed class UsagePack : Entity
{
    private UsagePack() { }

    public Guid UserId { get; private set; }
    public int AssistedRemaining { get; private set; }
    public int AiRemaining { get; private set; }
    public DateTime PurchasedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public static UsagePack Purchase(Guid userId, DateTime now) => new()
    {
        UserId = userId,
        AssistedRemaining = PlanRules.PackAssisted,
        AiRemaining = PlanRules.PackAi,
        PurchasedAt = now,
        ExpiresAt = now.AddDays(PlanRules.PackValidityDays),
    };

    public bool IsActive(DateTime now) => ExpiresAt > now && (AssistedRemaining > 0 || AiRemaining > 0);

    public int RemainingOf(UsageResource r) => r switch
    {
        UsageResource.AssistedApplications => AssistedRemaining,
        UsageResource.AiSuggestions => AiRemaining,
        _ => 0,
    };

    internal void ConsumeOne(UsageResource r)
    {
        if (RemainingOf(r) <= 0) throw new DomainException("billing.pack_empty", "Pacote sem créditos para este recurso.");
        if (r == UsageResource.AssistedApplications) AssistedRemaining--;
        else AiRemaining--;
    }
}

/// <summary>
/// Marca "esta página já contou neste mês" para candidaturas assistidas. Guarda só o HASH de origem+caminho
/// (nunca a URL), para não guardar identificadores que a URL possa ter.
/// </summary>
public sealed class UsageMark : Entity
{
    private UsageMark() { }

    public Guid UserId { get; private set; }
    public string Month { get; private set; } = default!;
    public UsageResource Resource { get; private set; }
    public string KeyHash { get; private set; } = default!;

    public static UsageMark Create(Guid userId, string month, UsageResource resource, string keyHash) => new()
    {
        UserId = userId,
        Month = month,
        Resource = resource,
        KeyHash = Guard.Required(keyHash, "usage.key_required", "Chave obrigatória."),
    };
}
