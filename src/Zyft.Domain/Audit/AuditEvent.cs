using Zyft.Domain.Common;

namespace Zyft.Domain.Audit;

/// <summary>Registro de ação (RF-20). Nunca guarda valores preenchidos: só ação, portal (host) e quantidade.</summary>
public sealed class AuditEvent : Entity
{
    private AuditEvent() { }

    public Guid UserId { get; private set; }
    public string Action { get; private set; } = default!;
    public string? Portal { get; private set; }
    public int? FieldCount { get; private set; }
    public DateTime OccurredAt { get; private set; }

    public static AuditEvent Record(Guid userId, string action, DateTime now, string? portal = null, int? fieldCount = null) => new()
    {
        UserId = userId,
        Action = Guard.Required(action, "audit.action_required", "Ação obrigatória."),
        Portal = Guard.Optional(portal),
        FieldCount = fieldCount,
        OccurredAt = now,
    };
}

/// <summary>Lista fechada de ações auditáveis.</summary>
public static class AuditActions
{
    public const string AccountCreated = "conta.criada";
    public const string DataExported = "dados.exportados";
    public const string ConsentChanged = "consentimento.alterado";
    public const string ProfileUpdated = "perfil.atualizado";
    public const string ResumeImported = "curriculo.importado";
    public const string ResumeApplied = "curriculo.aplicado";
    public const string ResumeDiscarded = "curriculo.descartado";
    public const string DevicePaired = "extensao.pareada";
    public const string DeviceRevoked = "extensao.revogada";
    public const string FormMapped = "formulario.mapeado";
    public const string OpenAnswerSuggested = "texto.sugerido";
    public const string JobAnalyzed = "vaga.analisada";
    public const string ApplicationConfirmed = "candidatura.confirmada";
    public const string SensitiveDataChanged = "sensivel.alterado";
    public const string SensitiveDataErased = "sensivel.apagado";

    // v2 (planos, anúncios, onboarding e campos personalizados) — nomes definidos no contrato v2.
    public const string PlanChanged = "plan.changed";
    public const string PackPurchased = "pack.purchased";
    public const string AdHidden = "ad.hidden";
    public const string RewardedClaimed = "rewarded.claimed";
    public const string OnboardingCompleted = "onboarding.completed";
    public const string CustomFieldChanged = "custom_field.changed";
}
