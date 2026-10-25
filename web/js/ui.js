// Componentes do design system Zyft em JavaScript puro (mesma marcação e classes zy-* do bundle).
// Cada função devolve um nó do DOM. Ver ref. design-system: Button, StatusBadge, Stamp, TextField,
// Switch, ConsentRow, PairingCode, AppNav, SensitiveValue, WhyThis, UsageMeter, ReviewItem, Icon.
import { h, nextId, clear } from './dom.js';

const PATHS = {
  check: 'M3.5 8.5l3 3 6-7',
  alert: 'M8 4.5v4.5M8 11.5v.01M8 1.75l6.5 12H1.5z',
  hand: 'M8 1.75a6.25 6.25 0 1 0 0 12.5A6.25 6.25 0 0 0 8 1.75zM3.6 3.6l8.8 8.8',
  draft: 'M2.5 4h8M2.5 7.5h6M2.5 11h3.5M8.5 13.5l.6-2.1 4.1-4.1 1.5 1.5-4.1 4.1z',
  unlock: 'M3.5 7.5h9v6.5h-9zM5.5 7.5V5a2.5 2.5 0 0 1 4.8-1',
  help: 'M8 14.5a6.5 6.5 0 1 0 0-13 6.5 6.5 0 0 0 0 13zM6.2 6.2a1.9 1.9 0 0 1 3.7.6c0 1.3-1.9 1.6-1.9 2.8M8 11.5v.01',
  edit: 'M10.5 2.5l3 3L6 13H3v-3z',
  skip: 'M4 4l8 8M12 4l-8 8',
  copy: 'M5.5 5.5h7v8h-7zM3.5 10.5v-8h7',
  refresh: 'M13 3.5v3.5H9.5M12.6 7A5 5 0 1 0 13 9.5',
  user: 'M8 8a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM2.5 14.5c.8-2.8 3-4 5.5-4s4.7 1.2 5.5 4',
  upload: 'M8 10.5V2.5M4.5 6L8 2.5 11.5 6M2.5 10.5v3h11v-3',
  book: 'M2.5 3h4a1.5 1.5 0 0 1 1.5 1.5v9A1.5 1.5 0 0 0 6.5 12h-4zM13.5 3h-4A1.5 1.5 0 0 0 8 4.5v9A1.5 1.5 0 0 1 9.5 12h4z',
  briefcase: 'M2 5h12v8H2zM5.5 5V3h5v2M2 8.5h12',
  plug: 'M6 1.5v3M10 1.5v3M4 4.5h8v3a4 4 0 0 1-8 0zM8 11.5v3',
  shield: 'M8 1.5l5.5 2v4c0 3.5-2.4 5.8-5.5 7-3.1-1.2-5.5-3.5-5.5-7v-4z',
  list: 'M5.5 4h8M5.5 8h8M5.5 12h8M2.5 4h.01M2.5 8h.01M2.5 12h.01',
  lock: 'M3.5 7.5h9v6.5h-9zM5.5 7.5V5a2.5 2.5 0 0 1 5 0v2.5',
  eye: 'M1.5 8s2.4-4.5 6.5-4.5S14.5 8 14.5 8 12.1 12.5 8 12.5 1.5 8 1.5 8zM8 10a2 2 0 1 0 0-4 2 2 0 0 0 0 4z',
  eyeOff: 'M2 2l12 12M6.6 3.7A6.6 6.6 0 0 1 8 3.5c4.1 0 6.5 4.5 6.5 4.5a11 11 0 0 1-1.8 2.3M10.1 12.1A6 6 0 0 1 8 12.5C3.9 12.5 1.5 8 1.5 8a11 11 0 0 1 2.7-3.1M6.6 6.6a2 2 0 0 0 2.8 2.8',
  globe: 'M8 14.5a6.5 6.5 0 1 0 0-13 6.5 6.5 0 0 0 0 13zM1.5 8h13M8 1.5c1.8 1.8 2.6 4 2.6 6.5S9.8 12.7 8 14.5C6.2 12.7 5.4 10.5 5.4 8S6.2 3.3 8 1.5z',
  target: 'M8 14.5a6.5 6.5 0 1 0 0-13 6.5 6.5 0 0 0 0 13zM8 11a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM8 8h.01',
  gear: 'M8 10a2 2 0 1 0 0-4 2 2 0 0 0 0 4zM8 1.5v2M8 12.5v2M1.5 8h2M12.5 8h2M3.4 3.4l1.4 1.4M11.2 11.2l1.4 1.4M3.4 12.6l1.4-1.4M11.2 4.8l1.4-1.4',
  close: 'M4 4l8 8M12 4l-8 8',
  plus: 'M8 3v10M3 8h10',
  menu: 'M2.5 4h11M2.5 8h11M2.5 12h11',
  logout: 'M6 2.5H3v11h3M10.5 5L13.5 8l-3 3M13.5 8H6',
};

const cx = (...a) => a.filter(Boolean).join(' ');

export function Icon(name, { size = 16, label, className } = {}) {
  return h('svg', {
    class: cx('zy-icon', className), width: size, height: size, viewBox: '0 0 16 16', fill: 'none', stroke: 'currentColor',
    'stroke-width': 1.75, 'stroke-linecap': 'round', 'stroke-linejoin': 'round',
    'aria-hidden': label ? null : 'true', role: label ? 'img' : null, 'aria-label': label || null, focusable: 'false',
  }, h('path', { d: PATHS[name] || '' }));
}

/** Button({ variant, size, icon, onClick, ...attrs }, ...children) */
export function Button({ variant = 'primary', size = 'md', icon, onClick, className, type = 'button', ...attrs } = {}, ...children) {
  return h('button', { type, class: cx('zy-btn', `zy-btn--${variant}`, `zy-btn--${size}`, className), onclick: onClick, ...attrs },
    icon ? Icon(icon, { size: size === 'sm' ? 14 : 16 }) : null, children);
}

/** Link com aparência de botão (navegação real com <a>). */
export function LinkButton(href, { variant = 'primary', size = 'md', icon, className, ...attrs } = {}, ...children) {
  return h('a', { href, class: cx('zy-btn', `zy-btn--${variant}`, `zy-btn--${size}`, className), ...attrs },
    icon ? Icon(icon, { size: size === 'sm' ? 14 : 16 }) : null, children);
}

const STATUS = {
  ready: { label: 'Pronto', icon: 'check' },
  needs: { label: 'Precisa de você', icon: 'alert' },
  blocked: { label: 'Ação sua', icon: 'hand' },
  ai: { label: 'Sugestão da IA – revise', icon: 'draft' },
};
export function StatusBadge({ status = 'needs', label, size } = {}) {
  const s = STATUS[status] || STATUS.needs;
  return h('span', { class: cx('zy-badge', `zy-badge--${status}`, size === 'sm' && 'zy-badge--sm') },
    Icon(s.icon, { size: size === 'sm' ? 12 : 14 }), h('span', {}, label || s.label));
}

export function Stamp({ label = 'Carimbado por você', at, size, className } = {}) {
  return h('span', { class: cx('zy-stamp', size === 'sm' && 'zy-stamp--sm', className) },
    h('span', { class: 'zy-stamp__tick', 'aria-hidden': 'true' }, Icon('check', { size: size === 'sm' ? 10 : 12 })),
    h('span', {}, label + (at ? ` · ${at}` : '')));
}

/** TextField do design system. Devolve o wrapper; o controle fica em `.control`. */
export function TextField({ label, name, type = 'text', value = '', hint, error, optional, multiline, placeholder, id, rows, attrs = {}, className } = {}) {
  const fid = id || nextId('zy-tf');
  const describedBy = [hint ? `${fid}-hint` : null, `${fid}-err`].filter(Boolean).join(' ');
  const common = { id: fid, name, placeholder, class: 'zy-tf__input', 'aria-describedby': describedBy, 'aria-invalid': error ? 'true' : null, ...attrs };
  const control = multiline ? h('textarea', { ...common, rows: rows || 4 }) : h('input', { ...common, type, value });
  if (multiline) control.value = value ?? '';
  const wrap = h('div', { class: cx('zy-tf', error && 'has-error', className) },
    h('label', { class: 'zy-tf__label', for: fid }, label, optional ? h('span', { class: 'zy-tf__opt' }, ' (opcional)') : null),
    control,
    hint ? h('p', { class: 'zy-tf__hint', id: `${fid}-hint` }, hint) : null,
    h('p', { class: 'zy-tf__error', id: `${fid}-err`, hidden: !error }, error ? [Icon('alert', { size: 14 }), error] : null));
  wrap.control = control;
  return wrap;
}

/** Switch acessível (role="switch"). */
export function Switch({ checked = false, label, disabled = false, onChange } = {}) {
  const btn = h('button', { type: 'button', role: 'switch', 'aria-checked': String(Boolean(checked)), 'aria-label': label, disabled, class: cx('zy-switch', checked && 'is-on') },
    h('span', { class: 'zy-switch__thumb' }));
  btn.setChecked = (v) => { btn.setAttribute('aria-checked', String(Boolean(v))); btn.classList.toggle('is-on', Boolean(v)); };
  btn.addEventListener('click', () => { if (!btn.disabled) onChange?.(btn.getAttribute('aria-checked') !== 'true', btn); });
  return btn;
}

export function ConsentRow({ title, purpose, basis, updatedAt, checked, required, onChange, className } = {}) {
  const meta = h('p', { class: 'zy-consent__meta' }, [basis, updatedAt].filter(Boolean).join(' · '));
  const sw = Switch({ checked, disabled: required, label: title, onChange: (next, el) => onChange?.(next, el, meta) });
  return h('div', { class: cx('zy-consent', className) },
    h('div', { class: 'zy-consent__text' },
      h('div', { class: 'zy-consent__title' }, title, required ? h('span', { class: 'zy-consent__req' }, 'Necessário') : null),
      h('p', { class: 'zy-consent__purpose' }, purpose),
      meta),
    sw);
}

export function PairingCode({ code, expiresIn = '10:00', hint, onCopy, onRenew } = {}) {
  const c = String(code || '').toUpperCase();
  const exp = h('b', {}, expiresIn);
  const el = h('div', { class: 'zy-pair' },
    h('div', { class: 'zy-pair__label' }, 'Código de pareamento'),
    h('div', { class: 'zy-pair__code', role: 'img', 'aria-label': `Código: ${c.split('').filter((x) => x !== '-').join(' ')}` },
      c.split('').map((ch) => (ch === '-' ? h('span', { class: 'zy-pair__sep', 'aria-hidden': 'true' }, '–') : h('span', { class: 'zy-pair__char', 'aria-hidden': 'true' }, ch)))),
    h('p', { class: 'zy-pair__hint' }, hint || 'Digite este código no painel da extensão Zyft. Ele vale só para este navegador.'),
    h('div', { class: 'zy-pair__foot' },
      h('span', { class: 'zy-pair__exp' }, 'Expira em ', exp),
      h('div', { class: 'zy-pair__actions' },
        Button({ variant: 'quiet', size: 'sm', icon: 'refresh', onClick: onRenew }, 'Gerar outro'),
        Button({ variant: 'secondary', size: 'sm', icon: 'copy', onClick: onCopy }, 'Copiar'))));
  el.setExpires = (t) => { exp.textContent = t; };
  return el;
}

/** AppNav: grupos e itens com ícone; item ativo em amarelo, contador em petróleo. */
export function AppNav({ items, active, logo, className, footer } = {}) {
  return h('nav', { class: cx('zy-nav', className), 'aria-label': 'Principal' },
    logo ? h('div', { class: 'zy-nav__logo' }, logo) : null,
    items.map((it) => {
      if (it.group) return h('div', { class: 'zy-nav__group' }, it.group);
      const on = it.id === active;
      return h('a', { href: it.href || '#', class: cx('zy-nav__item', on && 'is-active'), 'aria-current': on ? 'page' : null, dataset: { nav: it.id } },
        Icon(it.icon), h('span', {}, it.label),
        it.count != null ? h('span', { class: 'zy-nav__count', 'aria-label': `${it.count} registradas` }, String(it.count)) : null);
    }),
    footer || null);
}

/** SensitiveValue: trancado por padrão; destranca e tranca de novo. */
export function SensitiveValue({ value, masked, unlocked = false, onToggle } = {}) {
  const wrap = h('span', {});
  let open = unlocked;
  const paint = (focus) => {
    const btn = h('button', { type: 'button', class: 'zy-sens__toggle', onclick: () => { open = !open; onToggle?.(open); paint(true); } },
      open ? 'Trancar de novo' : 'Destrancar para conferir');
    wrap.className = cx('zy-sens', !open && 'is-locked');
    clear(wrap, Icon(open ? 'unlock' : 'lock', { size: 14 }), h('span', { class: 'zy-sens__val' }, open ? value : (masked || 'Trancado')), btn);
    if (focus) btn.focus();
  };
  paint(false);
  return wrap;
}

/** WhyThis: botão que expande a explicação. */
export function WhyThis({ label = 'Por que o Zyft sugeriu isso?', open = false, className } = {}, ...children) {
  const bodyId = nextId('why');
  const body = h('div', { class: 'zy-why__body', id: bodyId, hidden: !open }, children);
  const btn = h('button', { type: 'button', class: 'zy-why__btn', 'aria-expanded': String(open), 'aria-controls': bodyId },
    Icon('help', { size: 14 }), label);
  btn.addEventListener('click', () => {
    const next = btn.getAttribute('aria-expanded') !== 'true';
    btn.setAttribute('aria-expanded', String(next));
    body.hidden = !next;
  });
  return h('div', { class: cx('zy-why', className) }, btn, body);
}

/** UsageMeter: limite com calma (sem vermelho). limit null = sem limite. */
export function UsageMeter({ used = 0, limit, label = 'Candidaturas assistidas este mês', note, bonus = 0, className } = {}) {
  const unlimited = limit === null || limit === undefined;
  const total = unlimited ? null : limit + (bonus || 0);
  const pct = unlimited ? 0 : Math.min(100, Math.round((used / Math.max(1, total)) * 100));
  return h('div', { class: cx('zy-usage', className) },
    h('div', { class: 'zy-usage__row' }, h('span', { class: 'zy-usage__label' }, label),
      h('b', {}, unlimited ? `${used} · sem limite` : `${used} de ${total}`)),
    unlimited ? null : h('div', { class: 'zy-usage__bar', role: 'progressbar', 'aria-label': label, 'aria-valuenow': used, 'aria-valuemin': 0, 'aria-valuemax': total },
      h('span', { style: { width: `${pct}%` } })),
    note ? h('p', { class: 'zy-usage__note' }, note) : null);
}

/** ReviewItem: item do currículo importado com Editar/Aprovar (e Descartar). */
export function ReviewItem({ label, value, source, state = 'pending', approvedAt, onApprove, onEdit, onDiscard, onUndo, className, approveDisabled = false, extra = null } = {}) {
  let right;
  if (state === 'pending') {
    right = h('div', { class: 'zy-review__actions' },
      onDiscard ? Button({ variant: 'ghost', size: 'sm', onClick: onDiscard, 'aria-label': `Descartar ${label}` }, 'Descartar') : null,
      Button({ variant: 'quiet', size: 'sm', icon: 'edit', onClick: onEdit, 'aria-label': `Editar ${label}` }, 'Editar'),
      Button({ variant: 'primary', size: 'sm', icon: 'check', onClick: onApprove, 'aria-label': `Aprovar ${label}`, disabled: approveDisabled }, 'Aprovar'));
  } else if (state === 'discarded') {
    right = h('div', { class: 'zy-review__actions' }, h('span', { class: 'muted small' }, 'Descartado'),
      Button({ variant: 'ghost', size: 'sm', onClick: onUndo }, 'Desfazer'));
  } else {
    right = h('div', { class: 'zy-review__actions' },
      Stamp({ label: state === 'edited' ? 'Corrigido e carimbado por você' : 'Carimbado por você', at: approvedAt }),
      onUndo ? Button({ variant: 'ghost', size: 'sm', onClick: onUndo, 'aria-label': `Desfazer ${label}` }, 'Desfazer') : null);
  }
  return h('div', { class: cx('zy-review', `zy-review--${state}`, className) },
    h('div', { class: 'zy-review__main' },
      h('div', { class: 'zy-review__label' }, label),
      h('div', { class: 'zy-review__value' }, value),
      source ? h('div', { class: 'zy-review__source' }, source) : null,
      extra),
    right);
}

// ---------- Peças de layout usadas pelas telas ----------

/** Cabeçalho de página: overline opcional, h1 e texto de apoio. */
export function PageHead({ title, lead, overline, crumb, actions, wide } = {}) {
  return h('header', { class: cx('page-head', wide && 'page-head--wide') },
    h('div', { class: 'page-head__text' },
      crumb || null,
      overline ? h('div', { class: 'overline' }, overline) : null,
      h('h1', { class: 'page-title' }, title),
      lead ? h('p', { class: 'lead' }, lead) : null),
    actions ? h('div', { class: 'page-head__actions' }, actions) : null);
}

/** Cartão com título e ação no canto. */
export function Card({ title, titleId, action, className, label, tag = 'section', icon } = {}, ...children) {
  const id = titleId || (title ? nextId('card') : null);
  return h(tag, { class: cx('card', className), 'aria-labelledby': id, 'aria-label': id ? null : label },
    title ? h('div', { class: 'card__head' }, h('h2', { id, class: 'card__title', tabindex: '-1' }, icon ? Icon(icon) : null, title), action || null) : null,
    children);
}

/** Chip selecionável (aria-pressed) — sugestões e listas livres. */
export function Chip({ label, pressed, mine, onClick, size = 'md', dashed } = {}) {
  return h('button', {
    type: 'button', class: cx('chip', `chip--${size}`, pressed && 'is-on', (mine || dashed) && 'is-dashed'),
    'aria-pressed': pressed === undefined ? null : String(Boolean(pressed)), onclick: onClick,
  }, label);
}

/** Selo "Patrocinado". */
export const SponsoredTag = (small) => h('span', { class: cx('ad__tag', small && 'ad__tag--sm') }, 'Patrocinado');

/** Iniciais para avatar. */
export const initials = (name) => String(name || '').trim().split(/\s+/).filter(Boolean).map((p) => p[0]).slice(0, 2).join('').toUpperCase() || 'Z';

/** Logo do Zyft com troca automática no tema escuro. */
export function Logo({ onDark = false, height = 28, alt = 'Zyft' } = {}) {
  if (onDark) return h('img', { src: 'assets/zyft-lockup-light.svg', alt, class: 'logo', style: { height: `${height}px` } });
  return h('picture', { class: 'logo-pic' },
    h('source', { srcset: 'assets/zyft-lockup-light.svg', media: '(prefers-color-scheme: dark)' }),
    h('img', { src: 'assets/zyft-lockup.svg', alt, class: 'logo', style: { height: `${height}px` } }));
}
