using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Zyft.Application.Billing;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Ads;
using Zyft.Domain.Audit;
using Zyft.Domain.Billing;

namespace Zyft.Application.Ads;

/// <summary>Anúncio servido. Sempre "Patrocinado" na interface; <c>demo</c> = anunciante fictício.</summary>
public sealed record AdDto(
    string Id,
    string Placement,
    string Format,
    string Title,
    string Advertiser,
    string Body,
    string Cta,
    string Url,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? ImageUrl,
    string Reason,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? Topic,
    bool Demo,
    AdNetworkDto? Network = null);

/// <summary>Presente quando o espaço é preenchido pela rede (format = "network"): o cliente monta o bloco do AdSense.</summary>
public sealed record AdNetworkDto(string Provider, string Client, string Slot, string AdFormat, bool Mock);

// ------------------------------------------------------------------ GET /ads

/// <summary>Null = 204 (Plus, espaço proibido, nenhum anúncio ou tudo oculto).</summary>
public sealed record GetAdQuery(string? Placement, string? Topic) : IRequest<AdDto?>;

public sealed class GetAdValidator : AbstractValidator<GetAdQuery>
{
    public GetAdValidator()
    {
        RuleFor(x => x.Placement).NotEmpty().WithMessage("Informe placement.").MaximumLength(60);
        RuleFor(x => x.Topic).MaximumLength(500);
    }
}

public sealed class GetAdHandler(
    IProfileRepository profiles,
    IRepository<AdHide> hides,
    IRepository<AdStat> stats,
    IUsageMeter meter,
    IAdNetwork network,
    ICurrentUser current,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<GetAdQuery, AdDto?>
{
    public async Task<AdDto?> Handle(GetAdQuery q, CancellationToken ct)
    {
        var uid = current.UserId;
        var placement = q.Placement!.Trim();
        if (!AdPlacements.Allowed.TryGetValue(placement, out var format)) return null;
        if (await meter.GetPlanAsync(uid, ct) == PlanKind.Plus) return null;

        var now = clock.UtcNow;
        var profile = await profiles.GetByUserIdAsync(uid, ct);
        // Só área e cargos saem do perfil — nada de nome, contato ou sensíveis.
        var targeting = new AdTargeting(profile?.WorkArea, profile?.DesiredRoles, q.Topic);
        var active = (await hides.ListAsync(h => h.UserId == uid, ct)).Where(h => h.IsActive(now)).ToList();
        var hiddenAds = active.Where(h => h.Scope == AdHideScope.Ad).Select(h => h.AdId).ToHashSet();
        var hiddenTopics = active.Where(h => h.Scope == AdHideScope.Topic && h.Topic is not null).Select(h => h.Topic!).ToHashSet();
        var counters = (await stats.ListAsync(s => true, ct)).ToDictionary(s => s.AdId);

        // Rede (AdSense simulado): vem antes nos banners padrão e preenche os demais espaços sem anúncio direto.
        // A rede não recebe nada do perfil: o bloco é o mesmo para todos (anúncios não personalizados).
        var net = network.SlotFor(placement);
        if (net is not null && hiddenAds.Contains(net.AdId)) net = null;
        if (net is not null && network.Prefers(placement)) return NetworkAd(placement, net);

        var choice = AdSelector.Select(placement, targeting, hiddenAds, hiddenTopics, id => counters.TryGetValue(id, out var s) ? s.Impressions : 0);
        if (choice is null) return net is null ? null : NetworkAd(placement, net);

        if (!counters.TryGetValue(choice.Ad.Id, out var stat))
        {
            stat = AdStat.For(choice.Ad.Id);
            stats.Add(stat);
        }

        stat.Impression();
        await uow.SaveChangesAsync(ct);
        var a = choice.Ad;
        return new AdDto(a.Id, placement, format, a.Title, AdCatalog.Advertiser, a.Body, a.Cta, a.Url, null, choice.Reason, choice.Topic, true);
    }

    private static AdDto NetworkAd(string placement, AdNetworkSlot n) => new(
        n.AdId, placement, "network", "Anúncio do Google", n.Provider, string.Empty, string.Empty, string.Empty, null,
        n.Mock
            ? "Espaço da rede de anúncios do Google (AdSense). Nesta versão é uma simulação: nada é enviado ao Google. Em produção, só anúncios não personalizados, sem nenhum dado do seu perfil."
            : "Espaço da rede de anúncios do Google (AdSense), com anúncios não personalizados: o Zyft não envia nenhum dado do seu perfil.",
        null, n.Mock, new AdNetworkDto(n.Provider, n.Client, n.Slot, n.AdFormat, n.Mock));
}

// ------------------------------------------------------------------ POST /ads/{id}/hide

public sealed record HideAdCommand : IRequest<Unit>
{
    [JsonIgnore] public string Id { get; init; } = string.Empty;

    /// <summary>"ad" (este anúncio por 30 dias, padrão) ou "topic" (o assunto para sempre).</summary>
    public string? Scope { get; init; }
}

public sealed class HideAdValidator : AbstractValidator<HideAdCommand>
{
    public HideAdValidator() =>
        RuleFor(x => x.Scope).Must(s => s is null or "ad" or "topic").WithMessage("scope deve ser \"ad\" ou \"topic\".");
}

public sealed class HideAdHandler(IRepository<AdHide> hides, IRepository<AuditEvent> audit, ICurrentUser current, IUnitOfWork uow, IClock clock)
    : IRequestHandler<HideAdCommand, Unit>
{
    public async Task<Unit> Handle(HideAdCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var now = clock.UtcNow;
        if (cmd.Id.StartsWith("adsense-", StringComparison.Ordinal))
        {
            // Bloco da rede: só existe "ocultar este espaço" (30 dias); a rede não tem assunto.
            hides.Add(AdHide.Create(uid, cmd.Id, "rede", AdHideScope.Ad, now));
        }
        else
        {
            var ad = AdCatalog.Find(cmd.Id) ?? throw new NotFoundException("Anúncio não encontrado.");
            var scope = cmd.Scope == "topic" ? AdHideScope.Topic : AdHideScope.Ad;
            hides.Add(AdHide.Create(uid, ad.Id, ad.Topic, scope, now));
        }
        audit.Add(AuditEvent.Record(uid, AuditActions.AdHidden, now));
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ------------------------------------------------------------------ POST /ads/{id}/click

/// <summary>Clique: só soma no contador agregado do anúncio. Não guarda quem clicou.</summary>
public sealed record ClickAdCommand(string Id) : IRequest<Unit>;

public sealed class ClickAdHandler(IRepository<AdStat> stats, IUnitOfWork uow) : IRequestHandler<ClickAdCommand, Unit>
{
    public async Task<Unit> Handle(ClickAdCommand cmd, CancellationToken ct)
    {
        if (cmd.Id.StartsWith("adsense-", StringComparison.Ordinal)) return Unit.Value; // cliques da rede são contados pela rede
        var ad = AdCatalog.Find(cmd.Id) ?? throw new NotFoundException("Anúncio não encontrado.");
        var stat = await stats.FindAsync(s => s.AdId == ad.Id, ct);
        if (stat is null)
        {
            stat = AdStat.For(ad.Id);
            stats.Add(stat);
        }

        stat.Click();
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
