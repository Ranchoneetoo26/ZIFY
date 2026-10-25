// Google AdSense SIMULADO (ADR-0021, status Proposta).
// Quando GET /ads devolve format "network", o espaço é da rede de anúncios. Nesta versão NADA é carregado
// do Google: desenhamos o mesmo <ins class="adsbygoogle"> que o AdSense usa, com uma arte neutra dentro.
// Regras do Zyft que continuam valendo: o selo "Patrocinado", o "Por que estou vendo isso?" e o "Ocultar"
// ficam FORA do bloco (são do Zyft, não da rede) e nenhum dado do perfil vai para a rede.
// Não usamos logos nem marca do Google: só o texto "Anúncio do Google (simulado)".
import { h } from './dom.js';

/** Tamanho de cada variante do Zyft no AdSense. */
const SIZES = {
  banner: { cls: 'adsense--banner', label: 'Banner responsivo · 728×90' },
  feed: { cls: 'adsense--feed', label: 'In-feed horizontal responsivo' },
  native: { cls: 'adsense--rect', label: 'Retângulo médio · 300×250' },
};

let loaded = false;

/**
 * Carrega o script do AdSense uma única vez. Em modo mock (padrão e única opção nesta versão) NÃO carrega nada
 * e devolve false.
 *
 * Em produção seria (NÃO ATIVAR sem a ADR-0021 aceita):
 *
 *   const s = document.createElement('script');
 *   s.async = true;
 *   s.crossOrigin = 'anonymous';
 *   s.src = `https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=${encodeURIComponent(client)}`;
 *   document.head.append(s);
 *   // e, para cada <ins class="adsbygoogle"> montado:
 *   (window.adsbygoogle = window.adsbygoogle || []).push({});
 *   // anúncios não personalizados: (window.adsbygoogle).requestNonPersonalizedAds = 1 (antes do push)
 *
 * Antes disso é preciso:
 *  - liberar na CSP de web/index.html os domínios do Google (script-src pagead2.googlesyndication.com
 *    e partner.googleadservices.com; frame-src googleads.g.doubleclick.net e tpc.googlesyndication.com;
 *    connect-src e img-src correspondentes) — hoje a CSP é script-src 'self' e bloquearia o script;
 *  - um CMP (ferramenta de consentimento de cookies) certificado: pela LGPD, cookies de publicidade só
 *    depois do consentimento; sem consentimento, nada de script do Google;
 *  - conta AdSense aprovada (ca-pub real) e revisão das políticas do programa.
 */
export function loadAdSense(client, { mock = true } = {}) {
  if (mock || loaded) return false;
  // Produção ainda não existe: mantemos desligado mesmo com mock=false até a ADR-0021 ser aceita.
  void client;
  return false;
}

/** Bloco do AdSense no formato real (<ins class="adsbygoogle">) com arte simulada. variant: banner | feed | native. */
export function adsenseUnit(ad, variant = 'banner') {
  const n = ad.network || {};
  const size = SIZES[variant] || SIZES.banner;
  const format = n.adFormat || (variant === 'native' ? 'auto' : 'horizontal');
  const ins = h('ins', {
    class: `adsbygoogle adsense ${size.cls}`,
    // Estilo por CSSOM (CSP sem 'unsafe-inline'); vira style="display: block;" igual ao snippet do AdSense.
    style: { display: 'block' },
    'data-ad-client': n.client || 'ca-pub-0000000000000000',
    'data-ad-slot': n.slot || '',
    'data-ad-format': format,
    'data-full-width-responsive': 'true',
    'data-adtest': 'on',
  },
  h('span', { class: 'adsense__art' },
    h('span', { class: 'adsense__title' }, 'Anúncio do Google (simulado)'),
    h('span', { class: 'adsense__meta' }, `${size.label} · nada é carregado do Google`)));
  if (!n.mock) loadAdSense(n.client, { mock: false });
  return ins;
}
