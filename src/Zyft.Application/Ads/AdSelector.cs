using Zyft.Application.Catalog;
using Zyft.Application.Common.Text;

namespace Zyft.Application.Ads;

/// <summary>Formatos e espaços de anúncio (contrato v2).</summary>
public static class AdPlacements
{
    public const string ProfileRail = "web.profile.rail";
    public const string ApplicationsFeed = "web.applications.feed";
    public const string WebBanner = "web.banner";
    public const string PanelBanner = "ext.panel.banner";
    public const string JobContextual = "ext.job.contextual";
    public const string AfterComplete = "ext.after-complete";

    /// <summary>Únicos espaços com anúncio. Qualquer outro (onboarding, entrar, importar, sensíveis, conta, "Ler formulário"…) é proibido → 204.</summary>
    public static readonly IReadOnlyDictionary<string, string> Allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [ProfileRail] = "native",
        [ApplicationsFeed] = "native",
        [WebBanner] = "banner",
        [PanelBanner] = "banner",
        [JobContextual] = "contextual",
        [AfterComplete] = "native",
    };

    public static bool IsAllowed(string? placement) => placement is not null && Allowed.ContainsKey(placement);
}

/// <summary>
/// Únicos sinais permitidos para escolher anúncio: área, cargos desejados e texto do requisito da vaga.
/// Por construção NÃO há nome, contato, documento nem dado sensível aqui (regra inegociável 4 e 5).
/// </summary>
public sealed record AdTargeting(string? WorkArea, string? DesiredRoles, string? Topic);

public sealed record AdChoice(AdDefinition Ad, string Reason, string? Topic);

public static class AdSelector
{
    private const string Footer = " Usamos só a sua área, os cargos que você busca e o texto da vaga — nunca nome, contato ou dados sensíveis.";

    /// <summary>
    /// Escolhe um anúncio: filtra ocultos, pontua (requisito da vaga &gt; cargos &gt; área &gt; geral) e, entre os de maior
    /// pontuação, roda pelo que teve menos exibições. Null = nenhum anúncio adequado.
    /// </summary>
    public static AdChoice? Select(
        string placement,
        AdTargeting t,
        ISet<string> hiddenAdIds,
        ISet<string> hiddenTopics,
        Func<string, long> impressions,
        IReadOnlyList<AdDefinition>? catalog = null)
    {
        if (!AdPlacements.IsAllowed(placement)) return null;
        var area = AreaCatalog.Resolve(t.WorkArea);
        var topic = TextTools.Norm(t.Topic);
        var roles = TextTools.Norm(t.DesiredRoles);
        var contextual = string.Equals(placement, AdPlacements.JobContextual, StringComparison.OrdinalIgnoreCase);

        var scored = new List<(AdDefinition Ad, int Score, string Reason)>();
        foreach (var ad in catalog ?? AdCatalog.All)
        {
            if (hiddenAdIds.Contains(ad.Id) || hiddenTopics.Contains(ad.Topic)) continue;
            var topicHit = topic.Length > 0 && ad.Keywords.Any(k => ContainsWord(topic, k));
            var roleHit = roles.Length > 0 && ad.Keywords.Any(k => ContainsWord(roles, k));
            var areaHit = area is not null && ad.AreaId == area.Id;

            // Card contextual só aparece ligado ao requisito ("Sobre: …"); sem relação, nada.
            if (contextual && !topicHit) continue;
            // Anúncio de outra área só entra se o requisito da vaga pedir aquele assunto.
            if (ad.AreaId is not null && !areaHit && !topicHit) continue;

            var score = (topicHit ? 10 : 0) + (roleHit ? 3 : 0) + (areaHit ? 5 : 0) + (ad.AreaId is null ? 1 : 0);
            string reason;
            if (topicHit)
                reason = contextual
                    ? $"Porque a vaga pede algo que não aparece no seu perfil, e este anúncio é sobre {ad.TopicLabel}."
                    : $"Porque o texto da vaga que você está vendo tem relação com {ad.TopicLabel}.";
            else if (roleHit)
                reason = $"Porque os cargos que você busca têm relação com {ad.TopicLabel}.";
            else if (areaHit)
                reason = $"Porque você escolheu a área {area!.Name} no seu perfil.";
            else
                reason = "Anúncio geral do plano Gratuito, mostrado para qualquer área.";
            scored.Add((ad, score, reason + Footer));
        }

        if (scored.Count == 0) return null;
        var best = scored.Max(s => s.Score);
        var pick = scored.Where(s => s.Score == best).OrderBy(s => impressions(s.Ad.Id)).ThenBy(s => s.Ad.Id, StringComparer.Ordinal).First();
        var echoTopic = contextual && t.Topic is { } raw ? TextTools.Truncate(raw.Trim(), 80) : null;
        return new AdChoice(pick.Ad, pick.Reason, echoTopic);
    }

    private static bool ContainsWord(string normalizedHaystack, string keyword)
    {
        var k = TextTools.Norm(keyword);
        return k.Length > 0 && $" {normalizedHaystack} ".Contains($" {k} ", StringComparison.Ordinal);
    }
}
