using Zyft.Domain.Common;

namespace Zyft.Domain.Ads;

public enum AdHideScope { Ad = 1, Topic = 2 }

/// <summary>"Ocultar" de um anúncio: o anúncio por 30 dias ou o assunto para sempre. Guarda só ids do catálogo.</summary>
public sealed class AdHide : Entity
{
    public const int AdHideDays = 30;

    private AdHide() { }

    public Guid UserId { get; private set; }
    public AdHideScope Scope { get; private set; }

    /// <summary>Id do anúncio (escopo Ad) — também guardado no escopo Topic, só como referência.</summary>
    public string AdId { get; private set; } = default!;

    /// <summary>Assunto do catálogo (escopo Topic), ex.: <c>cursos-tecnologia</c>.</summary>
    public string? Topic { get; private set; }

    public DateTime HiddenAt { get; private set; }

    /// <summary>Null = para sempre.</summary>
    public DateTime? ExpiresAt { get; private set; }

    public static AdHide Create(Guid userId, string adId, string topic, AdHideScope scope, DateTime now) => new()
    {
        UserId = userId,
        AdId = Guard.Required(adId, "ads.id_required", "Anúncio obrigatório."),
        Topic = scope == AdHideScope.Topic ? Guard.Required(topic, "ads.topic_required", "Assunto obrigatório.") : null,
        Scope = scope,
        HiddenAt = now,
        ExpiresAt = scope == AdHideScope.Ad ? now.AddDays(AdHideDays) : null,
    };

    public bool IsActive(DateTime now) => ExpiresAt is null || ExpiresAt > now;
}

/// <summary>Contadores agregados por anúncio (exibições e cliques). Sem usuário, sem PII.</summary>
public sealed class AdStat : Entity
{
    private AdStat() { }

    public string AdId { get; private set; } = default!;
    public long Impressions { get; private set; }
    public long Clicks { get; private set; }

    public static AdStat For(string adId) => new() { AdId = Guard.Required(adId, "ads.id_required", "Anúncio obrigatório.") };

    public void Impression() => Impressions++;

    public void Click() => Clicks++;
}
