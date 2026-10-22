using Microsoft.Extensions.Options;
using Zyft.Application.Ads;

namespace Zyft.Infrastructure.Ads;

/// <summary>Configuração da rede de anúncios (seção <c>Ads:Network</c>). Os padrões ligam o AdSense simulado.</summary>
public sealed class AdNetworkOptions
{
    public const string Section = "Ads:Network";

    public bool Enabled { get; set; } = true;

    public string Provider { get; set; } = "Google AdSense";

    /// <summary>true = simulação: o cliente nunca carrega o script do Google. Produção exige ADR-0021 aceita, CMP de consentimento e conta aprovada.</summary>
    public bool Mock { get; set; } = true;

    /// <summary>Id de publicador fictício (formato do AdSense). Trocar pelo real só em produção.</summary>
    public string ClientId { get; set; } = "ca-pub-0000000000000000";

    /// <summary>Bloco de anúncio (data-ad-slot) por espaço. Espaço sem bloco não recebe anúncio da rede.</summary>
    public Dictionary<string, string> Slots { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [AdPlacements.WebBanner] = "1000000001",
        [AdPlacements.ProfileRail] = "1000000002",
        [AdPlacements.ApplicationsFeed] = "1000000003",
        [AdPlacements.PanelBanner] = "2000000001",
        [AdPlacements.AfterComplete] = "2000000002",
    };

    /// <summary>Espaços em que a rede vem antes do anúncio direto.</summary>
    public List<string> Preferred { get; set; } = new() { AdPlacements.WebBanner, AdPlacements.PanelBanner };
}

/// <summary>Google AdSense (simulado por padrão). Só devolve dados de bloco: nada do perfil chega à rede.</summary>
public sealed class AdSenseNetwork(IOptions<AdNetworkOptions> options) : IAdNetwork
{
    private readonly AdNetworkOptions _o = options.Value;

    public AdNetworkSlot? SlotFor(string placement)
    {
        if (!_o.Enabled || !AdPlacements.IsAllowed(placement)) return null;
        if (!_o.Slots.TryGetValue(placement, out var slot) || string.IsNullOrWhiteSpace(slot)) return null;
        var format = AdPlacements.Allowed.TryGetValue(placement, out var f) && f == "banner" ? "horizontal" : "auto";
        return new AdNetworkSlot(_o.Provider, _o.ClientId, slot, format, _o.Mock);
    }

    public bool Prefers(string placement) => _o.Enabled && _o.Preferred.Contains(placement, StringComparer.OrdinalIgnoreCase);
}
