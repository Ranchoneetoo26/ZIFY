using Zyft.Domain.Common;

namespace Zyft.Domain.Billing;

public enum PlanKind { Free = 1, Plus = 2 }

public enum BillingPeriod { Monthly = 1, Yearly = 2 }

/// <summary>Recursos com limite mensal no plano Gratuito.</summary>
public enum UsageResource
{
    /// <summary>POST /forms/mappings (1x por página por mês).</summary>
    AssistedApplications = 1,

    /// <summary>POST /forms/open-answers.</summary>
    AiSuggestions = 2,

    /// <summary>POST /jobs/analyze.</summary>
    JobAnalyses = 3,

    /// <summary>POST /resume-imports.</summary>
    ResumeImports = 4,
}

/// <summary>De onde saiu o crédito consumido: limite do mês → bônus recompensado → pacote avulso.</summary>
public enum UsageSource { Plan = 1, Bonus = 2, Pack = 3 }

/// <summary>Regras dos planos (valores aprovados pelo dono do produto). Pagamento simulado: nenhuma cobrança real.</summary>
public static class PlanRules
{
    public const decimal PlusMonthlyPrice = 9.90m;
    public const decimal PlusYearlyPrice = 99.00m;
    public const decimal PackPrice = 4.90m;

    public const int TrialDays = 7;

    public const int PackAssisted = 15;
    public const int PackAi = 10;
    public const int PackValidityDays = 60;

    public const int RewardedMaxPerMonth = 3;
    public const int RewardedCreditsPerClaim = 3;

    public const int FreeAnswersLimit = 15;
    public const int FreeDevicesLimit = 1;
    public const int PlusDevicesLimit = 3;

    /// <summary>Limite mensal do recurso (null = sem limite).</summary>
    public static int? MonthlyLimit(PlanKind plan, UsageResource resource) => plan == PlanKind.Plus ? null : resource switch
    {
        UsageResource.AssistedApplications => 10,
        UsageResource.AiSuggestions => 5,
        UsageResource.JobAnalyses => 10,
        UsageResource.ResumeImports => 2,
        _ => 0,
    };

    public static int? AnswersLimit(PlanKind plan) => plan == PlanKind.Plus ? null : FreeAnswersLimit;

    public static int DevicesLimit(PlanKind plan) => plan == PlanKind.Plus ? PlusDevicesLimit : FreeDevicesLimit;

    public static bool AdsEnabled(PlanKind plan) => plan == PlanKind.Free;

    /// <summary>Nome do recurso na API (camelCase), usado em <c>extensions.resource</c> do 402.</summary>
    public static string ApiName(UsageResource r) => r switch
    {
        UsageResource.AssistedApplications => "assistedApplications",
        UsageResource.AiSuggestions => "aiSuggestions",
        UsageResource.JobAnalyses => "jobAnalyses",
        UsageResource.ResumeImports => "resumeImports",
        _ => r.ToString(),
    };

    /// <summary>Mês de referência do uso (UTC), no formato AAAA-MM.</summary>
    public static string MonthOf(DateTime utc) => utc.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>Limite do plano atingido (HTTP 402 <c>plan.limit_reached</c>, com <c>resource</c>, <c>used</c> e <c>limit</c>).</summary>
public sealed class PlanLimitReachedException(string resource, int used, int limit)
    : DomainException("plan.limit_reached", LimitMessage(resource, limit))
{
    public string Resource { get; } = resource;
    public int Used { get; } = used;
    public int Limit { get; } = limit;

    private static string LimitMessage(string resource, int limit) => resource switch
    {
        "assistedApplications" => $"Você usou as {limit} candidaturas assistidas do plano Gratuito este mês. Ganhe +3 vendo uma oferta, compre um pacote avulso ou conheça o Plus.",
        "aiSuggestions" => $"Você usou as {limit} sugestões de texto com IA do plano Gratuito este mês. Compre um pacote avulso ou conheça o Plus.",
        "jobAnalyses" => $"Você usou as {limit} análises de vaga do plano Gratuito este mês. Conheça o Plus para analisar sem limite.",
        "resumeImports" => $"Você usou as {limit} importações de currículo do plano Gratuito este mês. Conheça o Plus para importar sem limite.",
        "answers" => $"O plano Gratuito guarda até {limit} respostas aprovadas. Apague uma resposta ou conheça o Plus.",
        "devices" => $"O plano Gratuito permite {limit} navegador pareado. Desconecte o outro em Extensão ou conheça o Plus (até {PlanRules.PlusDevicesLimit}).",
        _ => "Limite do plano atingido.",
    };
}

/// <summary>Ofertas recompensadas do mês esgotadas (HTTP 409 <c>ads.rewarded_exhausted</c>).</summary>
public sealed class RewardedExhaustedException(string message)
    : DomainException("ads.rewarded_exhausted", message);
