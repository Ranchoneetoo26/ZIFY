// Anúncios (contrato v2): GET /ads?placement=…; 204 = nada a mostrar. Sempre "Patrocinado",
// com "Por que estou vendo isso?" e "Ocultar". No máximo 1 por tela; nunca nas telas proibidas
// (onboarding, entrar/cadastro, importar, dados sensíveis, conta/privacidade) nem para Plus.
// Se a API falhar, o espaço simplesmente some: a tela nunca quebra por causa de anúncio.
import { api } from './api.js';
import { h, clear, announce, safeUrl } from './dom.js';
import { Button, Icon, WhyThis, SponsoredTag } from './ui.js';
import { adsenseUnit } from './adsense.js';

export const PLACEMENTS = {
  profileRail: 'web.profile.rail',
  applicationsFeed: 'web.applications.feed',
  banner: 'web.banner',
};

const imgOk = (u) => u && (/^https?:\/\//i.test(u) || /^\/(?!\/)/.test(u));

export async function fetchAd(placement, topic) {
  const q = new URLSearchParams({ placement });
  if (topic) q.set('topic', topic);
  try {
    // RNF-15: anúncio que demora mais de 1,5 s não aparece (nunca atrasa a tela).
    const ad = await Promise.race([api(`/ads?${q}`), new Promise((_, no) => setTimeout(() => no(new Error('timeout')), 1500))]);
    return ad && ad.id && ad.title ? ad : null;
  } catch {
    return null;
  }
}

function track(ad) {
  api(`/ads/${encodeURIComponent(ad.id)}/click`, { method: 'POST', body: {} }).catch(() => {});
}

/** Link da chamada: abre o anunciante em nova aba e registra o clique (contador agregado). */
function ctaLink(ad, variant = 'quiet', size = 'sm') {
  const url = safeUrl(ad.url);
  return h('a', {
    href: url, target: '_blank', rel: 'noopener noreferrer sponsored', class: `zy-btn zy-btn--${variant} zy-btn--${size}`,
    onclick: () => track(ad),
  }, ad.cta || 'Conhecer', h('span', { class: 'sr-only' }, ` (abre ${ad.advertiser || 'o anunciante'} em nova aba)`));
}

function why(ad, onHide) {
  return WhyThis({ label: 'Por que estou vendo isso?', className: 'ad__why' },
    h('p', {}, ad.reason || 'O anúncio usa só a sua área e os cargos que você busca, nunca seus dados de contato ou dados sensíveis.'),
    ad.topic ? Button({ variant: 'ghost', size: 'sm', onClick: () => onHide('topic') }, `Não mostrar anúncios sobre ${ad.topic}`) : null);
}

/**
 * Espaço de anúncio. variant: 'native' (card da coluna), 'feed' (faixa) ou 'banner' (horizontal).
 * Devolve um <aside> que se preenche sozinho e some se não houver anúncio.
 */
export function adSlot(placement, { variant = 'banner', topic } = {}) {
  const slot = h('div', { class: `ad-slot ad-slot--${variant}`, hidden: true });

  const hide = async (ad, scope) => {
    try {
      await api(`/ads/${encodeURIComponent(ad.id)}/hide`, { method: 'POST', body: { scope } });
    } catch { /* mesmo se falhar, some da tela */ }
    const msg = scope === 'topic'
      ? `Pronto. Você não verá mais anúncios sobre ${ad.topic || 'este assunto'}.`
      : 'Pronto. Este anúncio não aparece para você pelos próximos 30 dias.';
    clear(slot, h('p', { class: 'ad__hidden', role: 'status' }, Icon('check', { size: 14 }), msg));
    announce(msg);
    setTimeout(() => { slot.hidden = true; }, 6000);
  };

  (async () => {
    const ad = await fetchAd(placement, topic);
    if (!ad) { slot.remove(); return; }
    const onHide = (scope) => hide(ad, scope);
    const label = 'Conteúdo patrocinado';
    const meta = [ad.advertiser, ad.body].filter(Boolean).join(' · ');
    let el;
    if (ad.format === 'network') {
      // Rede de anúncios (Google AdSense simulado, ADR-0021): o bloco é da rede; selo, motivo e Ocultar são do Zyft
      // e ficam fora do bloco. Sem link nem contagem de clique: na rede, o clique é do Google.
      el = h('aside', { class: `ad ad--network ad--network-${variant}`, 'aria-label': variant === 'banner' ? 'Publicidade' : label },
        h('div', { class: 'ad__row' }, SponsoredTag(variant === 'banner'), h('span', { class: 'ad__meta' }, 'Rede de anúncios do Google')),
        adsenseUnit(ad, variant),
        h('div', { class: 'ad__row ad__row--between' },
          why(ad, onHide),
          Button({ variant: 'ghost', size: 'sm', onClick: () => onHide('ad'), 'aria-label': 'Ocultar este anúncio' }, 'Ocultar')));
    } else if (variant === 'native') {
      el = h('aside', { class: 'ad ad--native', 'aria-label': label },
        imgOk(ad.imageUrl) ? h('img', { class: 'ad__img', src: ad.imageUrl, alt: '' }) : null,
        h('div', { class: 'ad__body' },
          SponsoredTag(),
          h('span', { class: 'ad__title' }, ad.title),
          meta ? h('span', { class: 'ad__meta' }, meta) : null,
          why(ad, onHide),
          h('div', { class: 'ad__actions' }, ctaLink(ad), Button({ variant: 'ghost', size: 'sm', onClick: () => onHide('ad') }, 'Não tenho interesse'))));
    } else if (variant === 'feed') {
      el = h('aside', { class: 'ad ad--feed', 'aria-label': label },
        h('div', { class: 'ad__logo', 'aria-hidden': 'true' }, imgOk(ad.imageUrl) ? h('img', { src: ad.imageUrl, alt: '' }) : (ad.advertiser || 'Z').slice(0, 1)),
        h('div', { class: 'ad__body' },
          h('div', { class: 'ad__row' }, SponsoredTag()),
          h('span', { class: 'ad__title ad__title--lg' }, ad.title),
          meta ? h('span', { class: 'ad__meta' }, meta) : null,
          why(ad, onHide)),
        h('div', { class: 'ad__actions' }, ctaLink(ad), Button({ variant: 'ghost', size: 'sm', onClick: () => onHide('ad') }, 'Ocultar')));
    } else {
      el = h('aside', { class: 'ad ad--banner', 'aria-label': 'Publicidade' },
        h('div', { class: 'ad__media', 'aria-hidden': 'true' }, imgOk(ad.imageUrl) ? h('img', { src: ad.imageUrl, alt: '' }) : h('span', {}, (ad.advertiser || 'Z').slice(0, 1))),
        h('div', { class: 'ad__body' },
          SponsoredTag(true),
          h('span', { class: 'ad__title' }, ad.title),
          meta ? h('span', { class: 'ad__meta' }, meta) : null,
          why(ad, onHide)),
        ctaLink(ad),
        h('button', { type: 'button', class: 'ad__x', 'aria-label': 'Ocultar este anúncio', onclick: () => onHide('ad') }, Icon('close', { size: 14 })));
    }
    clear(slot, el);
    slot.hidden = false;
  })();

  return slot;
}
