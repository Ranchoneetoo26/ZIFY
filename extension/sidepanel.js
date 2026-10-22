// Painel lateral do Zyft: mostra e sugere; a pessoa decide.
// Todas as chamadas à API passam pelo service worker (background.js), que guarda os tokens.
// O painel nunca envia, salva ou avança formulários no portal (RN-01, ADR-0009).
// Anúncios: nunca no estado "não conectada" nem na aba Formulário (revisão dos campos).

const $ = (s, el = document) => el.querySelector(s);

// ---------- DOM seguro (sem innerHTML com dados da API ou da página) ----------
function h(tag, attrs = {}, ...children) {
  const el = document.createElement(tag);
  for (const [k, v] of Object.entries(attrs || {})) {
    if (v == null || v === false) continue;
    if (k === 'class') el.className = v;
    else if (k === 'text') el.textContent = v;
    else if (k.startsWith('on') && typeof v === 'function') el.addEventListener(k.slice(2), v);
    else if (k in el && typeof v !== 'string') el[k] = v;
    else el.setAttribute(k, v === true ? '' : String(v));
  }
  for (const c of children.flat(Infinity)) {
    if (c == null || c === false) continue;
    el.append(c instanceof Node ? c : document.createTextNode(String(c)));
  }
  return el;
}
const clear = (el) => { el.replaceChildren(); return el; };
const norm = (s) => String(s ?? '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().replace(/\s+/g, ' ').trim();
let uid = 0;
const nextId = (p) => `${p}-${++uid}`;
const plural = (n, one, many) => (n === 1 ? one : many);
const hhmm = (d = new Date()) => d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });

// ---------- Ícones do design system (traço 1.75, 16px, currentColor) ----------
const PATHS = {
  check: 'M3.5 8.5l3 3 6-7',
  alert: 'M8 4.5v4.5M8 11.5v.01M8 1.75l6.5 12H1.5z',
  hand: 'M8 1.75a6.25 6.25 0 1 0 0 12.5A6.25 6.25 0 0 0 8 1.75zM3.6 3.6l8.8 8.8',
  draft: 'M2.5 4h8M2.5 7.5h6M2.5 11h3.5M8.5 13.5l.6-2.1 4.1-4.1 1.5 1.5-4.1 4.1z',
  unlock: 'M3.5 7.5h9v6.5h-9zM5.5 7.5V5a2.5 2.5 0 0 1 4.8-1',
  lock: 'M3.5 7.5h9v6.5h-9zM5.5 7.5V5a2.5 2.5 0 0 1 5 0v2.5',
  help: 'M8 14.5a6.5 6.5 0 1 0 0-13 6.5 6.5 0 0 0 0 13zM6.2 6.2a1.9 1.9 0 0 1 3.7.6c0 1.3-1.9 1.6-1.9 2.8M8 11.5v.01',
  edit: 'M10.5 2.5l3 3L6 13H3v-3z',
  skip: 'M4 4l8 8M12 4l-8 8',
  refresh: 'M13 3.5v3.5H9.5M12.6 7A5 5 0 1 0 13 9.5',
  plug: 'M6 1.5v3M10 1.5v3M4 4.5h8v3a4 4 0 0 1-8 0zM8 11.5v3',
  shield: 'M8 1.5l5.5 2v4c0 3.5-2.4 5.8-5.5 7-3.1-1.2-5.5-3.5-5.5-7v-4z',
  globe: 'M8 14.5a6.5 6.5 0 1 0 0-13 6.5 6.5 0 0 0 0 13zM1.5 8h13M8 1.5c1.8 1.8 2.6 4 2.6 6.5S9.8 12.7 8 14.5C6.2 12.7 5.4 10.5 5.4 8S6.2 3.3 8 1.5z',
  target: 'M8 14.5a6.5 6.5 0 1 0 0-13 6.5 6.5 0 0 0 0 13zM8 11a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM8 8h.01',
  gear: 'M8 10a2 2 0 1 0 0-4 2 2 0 0 0 0 4zM8 1.5v2M8 12.5v2M1.5 8h2M12.5 8h2M3.4 3.4l1.4 1.4M11.2 11.2l1.4 1.4M3.4 12.6l1.4-1.4M11.2 4.8l1.4-1.4',
  briefcase: 'M2 5h12v8H2zM5.5 5V3h5v2M2 8.5h12',
  x: 'M12 4 4 12M4 4l8 8',
};
const SVGNS = 'http://www.w3.org/2000/svg';
function icon(name, size = 16, cls = '', label = '') {
  const svg = document.createElementNS(SVGNS, 'svg');
  svg.setAttribute('class', `zy-icon ${cls}`.trim());
  for (const [k, v] of Object.entries({ width: size, height: size, viewBox: '0 0 16 16', fill: 'none', stroke: 'currentColor', 'stroke-width': 1.75, 'stroke-linecap': 'round', 'stroke-linejoin': 'round' })) svg.setAttribute(k, v);
  if (label) { svg.setAttribute('role', 'img'); svg.setAttribute('aria-label', label); } else svg.setAttribute('aria-hidden', 'true');
  const p = document.createElementNS(SVGNS, 'path');
  p.setAttribute('d', PATHS[name] || '');
  svg.append(p);
  return svg;
}

// ---------- Componentes do design system (versão DOM) ----------
function Button({ variant = 'primary', size = 'md', iconName, wide, k, ...rest }, ...children) {
  return h('button', { type: 'button', class: `zy-btn zy-btn--${variant} zy-btn--${size}${wide ? ' zy-btn--wide' : ''}`, 'data-k': k, ...rest },
    iconName ? icon(iconName, size === 'sm' ? 14 : 16) : null, ...children);
}
const STATUS = {
  ready: { label: 'Pronto', icon: 'check' },
  needs: { label: 'Precisa de você', icon: 'alert' },
  blocked: { label: 'Ação sua', icon: 'hand' },
  ai: { label: 'Sugestão da IA – revise', icon: 'draft' },
};
function StatusBadge(status, label, small = true) {
  const s = STATUS[status] || STATUS.needs;
  return h('span', { class: `zy-badge zy-badge--${status}${small ? ' zy-badge--sm' : ''}` }, icon(s.icon, small ? 12 : 14), h('span', {}, label || s.label));
}
function Stamp(label = 'Carimbado por você', at = '', small = false) {
  return h('span', { class: `zy-stamp${small ? ' zy-stamp--sm' : ''}` },
    h('span', { class: 'zy-stamp__tick', 'aria-hidden': 'true' }, icon('check', small ? 10 : 12)),
    h('span', {}, label + (at ? ` · ${at}` : '')));
}
function WhyThis(label, body) {
  const bodyId = nextId('why');
  const content = h('div', { class: 'zy-why__body', id: bodyId, hidden: true }, body);
  const btn = h('button', { type: 'button', class: 'zy-why__btn', 'aria-expanded': 'false', 'aria-controls': bodyId }, icon('help', 14), label || 'Por que o Zyft sugeriu isso?');
  btn.addEventListener('click', () => {
    content.hidden = !content.hidden;
    btn.setAttribute('aria-expanded', String(!content.hidden));
  });
  return h('div', { class: 'zy-why' }, btn, content);
}
function ProgressSummary(c) {
  const order = ['ready', 'needs', 'ai', 'blocked'];
  const total = order.reduce((a, k) => a + (c[k] || 0), 0) || 1;
  return h('div', { class: 'zy-progress' },
    h('div', { class: 'zy-progress__bar', role: 'img', 'aria-label': `${c.ready || 0} de ${total} campos prontos` },
      order.map((k) => (c[k] ? h('span', { class: `zy-progress__seg zy-progress__seg--${k}`, style: `flex-grow:${c[k]}` }) : null))),
    h('ul', { class: 'zy-progress__legend' }, order.map((k) => h('li', {},
      icon(STATUS[k].icon, 12, `zy-c-${k}`), h('b', {}, String(c[k] || 0)), ' ', k === 'ai' ? 'Sugestões da IA' : STATUS[k].label))));
}
const SITE = {
  full: { icon: 'check', text: (n) => `Compatível com ${n}` },
  partial: { icon: 'alert', text: (n) => `Compatibilidade parcial com ${n}` },
  none: { icon: 'skip', text: (n) => `O Zyft ainda não lê ${n}` },
};
function SiteSupport(level, site, detail) {
  const l = SITE[level] || SITE.full;
  return h('div', { class: `zy-site zy-site--${level}` },
    icon('globe', 14), h('span', {}, l.text(site || 'este site')), icon(l.icon, 12, 'zy-site__mark'),
    detail ? h('span', { class: 'zy-site__detail' }, detail) : null);
}
const MATCH = { match: 'Aparece no seu perfil', partial: 'Aparece em parte', missing: 'Não encontramos no seu perfil' };
function RequirementMatch(items, footnote) {
  const n = items.filter((i) => i.level === 'match').length;
  return h('section', { class: 'zy-match', 'aria-labelledby': 'req-title' },
    h('header', { class: 'zy-match__head' },
      h('div', { class: 'zy-match__title', id: 'req-title' }, icon('target', 16), 'O que a vaga pede'),
      h('div', { class: 'zy-match__sum' }, `${n} de ${items.length} aparecem no seu perfil`)),
    items.length
      ? h('ul', { class: 'zy-match__list' }, items.map((it) => h('li', { class: `zy-match__item zy-match__item--${it.level}` },
        h('span', { class: 'zy-match__dot', role: 'img', 'aria-label': MATCH[it.level] }),
        h('div', {},
          h('div', { class: 'zy-match__req' }, it.requirement, it.required === false ? h('span', { class: 'zy-match__opt' }, 'desejável') : null),
          h('div', { class: 'zy-match__ev' }, it.evidence || MATCH[it.level])))))
      : h('p', { class: 'empty' }, 'Não encontramos requisitos no texto desta página.'),
    footnote ? h('p', { class: 'zy-match__foot' }, footnote) : null);
}
function UsageMeter(used, limit, label, note) {
  const pct = limit ? Math.min(100, Math.round((used / limit) * 100)) : 0;
  return h('div', { class: 'zy-usage' },
    h('div', { class: 'zy-usage__row' }, h('span', { class: 'zy-usage__label' }, label), h('b', {}, limit ? `${used} de ${limit}` : String(used))),
    limit ? h('div', { class: 'zy-usage__bar', role: 'progressbar', 'aria-label': label, 'aria-valuenow': used, 'aria-valuemin': 0, 'aria-valuemax': limit }, h('span', { style: `width:${pct}%` })) : null,
    note ? h('p', { class: 'zy-usage__note' }, note) : null);
}
function TextField({ label, hint, value = '', id = nextId('tf'), multiline, ...rest }) {
  const errId = `${id}-err`, hintId = `${id}-hint`;
  const input = h(multiline ? 'textarea' : 'input', { id, class: 'zy-tf__input', 'aria-describedby': `${hint ? hintId : ''} ${errId}`.trim(), ...rest });
  input.value = value;
  const err = h('p', { class: 'zy-tf__error', id: errId, role: 'alert' });
  const wrap = h('div', { class: 'zy-tf' }, h('label', { class: 'zy-tf__label', for: id }, label), input,
    hint ? h('p', { class: 'zy-tf__hint', id: hintId }, hint) : null, err);
  return { wrap, input, setError: (msg) => setFieldError(input, err, msg) };
}
function setFieldError(input, errEl, msg) {
  clear(errEl);
  if (msg) errEl.append(icon('alert', 14), msg);
  if (msg) input.setAttribute('aria-invalid', 'true');
  else input.removeAttribute('aria-invalid');
}

// ---------- Comunicação com o service worker ----------
class ApiError extends Error {
  constructor(r) {
    super(problemText(r.problem) || `Erro ${r.status}`);
    this.status = r.status;
    this.problem = r.problem || {};
    this.code = this.problem.code || this.problem.extensions?.code || '';
  }
}
function problemText(p) {
  if (!p) return '';
  const parts = [];
  if (p.title) parts.push(p.title);
  if (p.detail && p.detail !== p.title) parts.push(p.detail);
  if (p.errors && typeof p.errors === 'object') {
    for (const [k, v] of Object.entries(p.errors)) parts.push(`${k}: ${[].concat(v).join(' ')}`);
  }
  return parts.join(' — ');
}
const bg = (msg) => chrome.runtime.sendMessage(msg);
async function apiRaw(method, path, body) {
  const r = await bg({ type: 'api', method, path: `/api/v1${path}`, body });
  if (!r) return { ok: false, status: 0, problem: { title: 'Erro interno', detail: 'O serviço da extensão não respondeu.' } };
  if (r.paired === false) showPair();
  return r;
}
async function api(method, path, body) {
  const r = await apiRaw(method, path, body);
  if (!r.ok) throw new ApiError(r);
  return r.data;
}
const isLimit = (err) => err?.status === 402 || err?.code === 'plan.limit_reached';

// ---------- Estado ----------
const S = {
  apiBase: 'http://localhost:5080',
  tab: null,
  page: null,          // { url, origin, host, path, http, level: full|partial|none, siteName, authorized }
  view: 'vaga',        // vaga | form | conclude
  job: null,           // { url, info, reqs, disclaimer }
  lastJob: null,       // { title, company, url } (chrome.storage.session)
  jobBusy: false,
  items: [],
  formRead: false,
  formBusy: false,
  step: 0,
  lastFill: null,      // { ok, failed: [{label, error}], skipped, manual }
  billing: undefined,  // undefined = carregando; null = indisponível; objeto = GET /billing
  limit: null,         // { where: 'job'|'form'|'ai', problem }
  rewardOpen: false,
  registered: null,    // { jobTitle, company, at }
  fromSubmit: false,
};
const adCache = new Map();

// ?tabId=N abre o painel numa aba comum apontando para outra aba (testes manuais e E2E).
const FORCED_TAB = Number(new URLSearchParams(location.search).get('tabId')) || null;

// ---------- Status e erros (aria-live) ----------
function status(text, kind = '') {
  const s = clear($('#status'));
  s.className = `status ${kind}`;
  if (!text) return;
  if (kind === 'busy') s.append(h('span', { class: 'spinner', 'aria-hidden': 'true' }));
  s.append(text);
}
function showError(err, retry) {
  const s = clear($('#status'));
  s.className = 'status err';
  const consent = err.status === 403;
  s.append(
    h('p', { class: 'err-title' }, icon('alert', 14), err.message || 'Algo não saiu como esperado.'),
    consent ? h('p', { class: 'small' }, 'Este recurso depende de um consentimento. Ative em "Conta e privacidade" na plataforma do Zyft e tente de novo.') : null,
    retry ? Button({ variant: 'quiet', size: 'sm', iconName: 'refresh', onclick: () => { status(''); retry(); } }, 'Tentar de novo') : null,
  );
}
function openUrl(url) {
  try {
    const u = new URL(url);
    if (!/^https?:$/.test(u.protocol)) return;
    chrome.tabs.create({ url: u.href });
  } catch { /* URL inválida: ignora */ }
}
const platformUrl = (hash) => `${S.apiBase}/#/${hash}`;

// ---------- Cabeçalho ----------
function setTop(text, dot = false) {
  const t = clear($('#top-status'));
  if (dot) t.append(h('span', { class: 'top__dot', 'aria-hidden': 'true' }));
  t.append(h('span', {}, text));
}

// ---------- Ajustes ----------
function settingsForm({ withUnpair }) {
  const tf = TextField({ label: 'Endereço da API', value: S.apiBase, type: 'url', placeholder: 'http://localhost:5080', hint: 'Padrão: http://localhost:5080. Endereços fora do localhost pedem permissão do navegador.' });
  const form = h('form', { class: 'stack', novalidate: true }, tf.wrap,
    h('div', { class: 'row' },
      Button({ variant: 'quiet', size: 'sm', type: 'submit' }, 'Salvar endereço'),
      withUnpair ? Button({ variant: 'ghost', size: 'sm', onclick: unpair }, 'Desconectar extensão') : null));
  form.addEventListener('submit', (e) => {
    e.preventDefault();
    let url;
    try {
      url = new URL(tf.input.value.trim() || 'http://localhost:5080');
      if (!/^https?:$/.test(url.protocol)) throw new Error();
    } catch {
      return tf.setError('Informe um endereço completo, como http://localhost:5080.');
    }
    tf.setError('');
    const base = url.origin;
    const local = /^(localhost|127\.0\.0\.1)$/.test(url.hostname);
    // permissions.request precisa ser chamado direto do gesto do usuário (antes de qualquer await).
    const perm = local ? Promise.resolve(true) : chrome.permissions.request({ origins: [`${base}/*`] });
    perm.then(async (granted) => {
      if (!granted) return tf.setError('Sem permissão do navegador para esse endereço.');
      await bg({ type: 'setApiBase', url: base });
      S.apiBase = base;
      authorizedCache = null;
      status(`Endereço da API salvo: ${base}`, 'ok');
    });
  });
  return form;
}
$('#btn-settings').append(icon('gear', 16));
$('#btn-settings').addEventListener('click', () => {
  const s = $('#settings');
  s.hidden = !s.hidden;
  $('#btn-settings').setAttribute('aria-expanded', String(!s.hidden));
  if (!s.hidden) {
    clear(s).append(
      h('div', { class: 'overline' }, 'Ajustes'),
      settingsForm({ withUnpair: true }),
      h('button', { type: 'button', class: 'zy-link', onclick: () => openUrl(platformUrl('extensao')) }, 'Abrir a plataforma do Zyft'),
    );
    s.querySelector('input')?.focus();
  }
});
async function unpair() {
  if (!confirm('Desconectar esta extensão da sua conta Zyft? Você poderá conectar de novo com um novo código.')) return;
  await bg({ type: 'unpair' });
  resetForm(); S.job = null; S.billing = undefined; adCache.clear();
  $('#settings').hidden = true;
  status('Extensão desconectada.', 'ok');
  showPair();
}

// ---------- Não conectada (pareamento) ----------
function defaultDeviceName() {
  const brands = navigator.userAgentData?.brands?.map((b) => b.brand) || [];
  const ua = navigator.userAgent;
  const browser = brands.find((b) => /Edge/i.test(b)) || /Edg\//.test(ua) ? 'Edge' : 'Chrome';
  const plat = navigator.userAgentData?.platform || navigator.platform || '';
  const os = /win/i.test(plat) ? 'Windows' : /mac/i.test(plat) ? 'macOS' : /cros/i.test(plat) ? 'ChromeOS' : /linux/i.test(plat) ? 'Linux' : plat || 'este computador';
  return `${browser} no ${os}`;
}

function showPair() {
  $('#view-main').hidden = true;
  $('#btn-settings').hidden = true;
  $('#settings').hidden = true;
  setTop('Não conectada');
  const v = clear($('#view-pair'));
  v.hidden = false;
  const codeId = nextId('code');
  const code = h('input', { id: codeId, class: 'code-input', inputmode: 'numeric', maxlength: '6', autocomplete: 'one-time-code', required: true, 'aria-describedby': `${codeId}-err` });
  const codeErr = h('p', { class: 'zy-tf__error', id: `${codeId}-err`, role: 'alert' });
  code.addEventListener('input', () => { code.value = code.value.replace(/\D/g, '').slice(0, 6); });
  const name = TextField({ label: 'Nome deste navegador', value: defaultDeviceName(), maxlength: '80', required: true, hint: 'Aparece na lista de navegadores conectados.' });
  const submit = Button({ variant: 'primary', iconName: 'plug', type: 'submit', wide: true }, 'Conectar');
  const form = h('form', { class: 'stack', novalidate: true, style: 'gap:16px' },
    h('div', { class: 'zy-tf' }, h('label', { class: 'zy-tf__label', for: codeId }, 'Código de 6 dígitos'), code, codeErr),
    name.wrap, submit);
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const c = code.value.replace(/\D/g, '');
    const deviceName = name.input.value.trim();
    setFieldError(code, codeErr, /^\d{6}$/.test(c) ? '' : 'Digite os 6 números do código.');
    name.setError(deviceName ? '' : 'Dê um nome a este navegador (ex.: Notebook de casa).');
    if (!/^\d{6}$/.test(c)) return code.focus();
    if (!deviceName) return name.input.focus();
    submit.disabled = true;
    status('Conectando…', 'busy');
    const r = await bg({ type: 'pair', code: c, deviceName });
    submit.disabled = false;
    if (r?.status === 402) {
      // Limite de navegadores do plano (plan.limit_reached, resource=devices): calmo e com caminho.
      setFieldError(code, codeErr, '');
      const st = clear($('#status'));
      st.className = 'status info';
      st.append(h('p', {}, r.problem?.detail || 'O seu plano já tem o número máximo de navegadores conectados.'),
        h('button', { type: 'button', class: 'zy-link', onclick: () => openUrl(platformUrl('extensao')) }, 'Ver navegadores conectados na plataforma'),
        ' ', h('button', { type: 'button', class: 'zy-link', onclick: () => openUrl(platformUrl('planos')) }, 'Conhecer o Plus'));
      return;
    }
    if (!r?.ok) {
      const msg = r?.status === 0 ? problemText(r.problem) : (r?.problem?.detail || r?.problem?.title || 'Código inválido ou expirado. Gere um novo na plataforma.');
      setFieldError(code, codeErr, msg);
      status('');
      return code.focus();
    }
    status('Extensão conectada à sua conta.', 'ok');
    showMain();
  });
  v.append(
    h('div', { class: 'pad grow', style: 'padding:24px 16px; gap:16px' },
      h('h1', { class: 'h1', id: 'pair-title' }, 'Conecte este navegador à sua conta'),
      h('p', { class: 'muted' }, 'Na plataforma, abra Extensão e gere um código de pareamento. Ele vale por 10 minutos.'),
      form,
      h('details', { class: 'adjust', style: 'margin-top:auto' }, h('summary', {}, 'Ajustes'), h('div', { class: 'stack' }, settingsForm({ withUnpair: false })))),
    h('p', { class: 'note-shield' }, icon('shield', 14), h('span', {}, 'O Zyft nunca pede a senha dos portais. A extensão só lê o formulário da página aberta, quando você pede.')),
  );
}

// ---------- Conectada ----------
function showMain() {
  $('#view-pair').hidden = true;
  $('#view-main').hidden = false;
  $('#btn-settings').hidden = false;
  loadBilling();
  refreshTab();
}

let authorizedCache = null;
async function authorizedOrigins() {
  if (authorizedCache) return authorizedCache;
  const data = await api('GET', '/authorized-sites');
  const list = Array.isArray(data) ? data : data?.items || data?.sites || [];
  authorizedCache = list.map((s) => (typeof s === 'string' ? s : s.origin)).filter(Boolean).map((o) => o.replace(/\/+$/, ''));
  return authorizedCache;
}

// Conectores conhecidos (nome exibido). O genérico vale para qualquer site autorizado.
const SITE_NAMES = { localhost: 'o portal de demonstração', '127.0.0.1': 'o portal de demonstração' };

function resetForm(keepStep = false) {
  S.items = []; S.formRead = false; S.lastFill = null; S.limit = S.limit?.where === 'job' ? S.limit : null;
  if (!keepStep) S.step = 0;
}

async function refreshTab() {
  if (FORCED_TAB) S.tab = await chrome.tabs.get(FORCED_TAB).catch(() => null);
  else [S.tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  const url = S.tab?.url || '';
  const keepConclude = S.view === 'conclude';
  if (S.page?.url !== url && !keepConclude) {
    // Nova página: a leitura anterior não vale mais. No mesmo site (formulário em etapas) mantém a aba e a etapa.
    let sameSite = false;
    try { sameSite = !!S.page?.origin && new URL(url).origin === S.page.origin; } catch { /* não é http */ }
    resetForm(sameSite);
    S.registered = null;
    if (!sameSite) S.view = 'vaga';
  }
  if (S.job && S.job.url !== url) S.job = null;
  S.lastJob = (await chrome.storage.session.get('lastJob')).lastJob || null;
  if (!url.startsWith('http')) {
    S.page = { url, http: false, level: 'none' };
    return render();
  }
  const u = new URL(url);
  S.page = { url, origin: u.origin, host: u.hostname, path: `${u.host}${u.pathname}`.replace(/\/$/, ''), http: true, level: 'none', siteName: SITE_NAMES[u.hostname] || u.hostname, authorized: false };
  if (SITE_NAMES[u.hostname]) { S.page.level = 'full'; S.page.authorized = true; }
  else {
    try {
      const origins = await authorizedOrigins();
      S.page.authorized = origins.includes(u.origin) && (await chrome.permissions.contains({ origins: [`${u.origin}/*`] }));
      S.page.level = 'partial';
    } catch (err) {
      if (err.status !== 401) showError(err, refreshTab);
    }
  }
  render();
}

function authorizeSite() {
  const { origin, host } = S.page;
  // Pedido de permissão direto do clique (gesto do usuário).
  chrome.permissions.request({ origins: [`${origin}/*`] }).then(async (granted) => {
    if (!granted) return status('Permissão negada. O Zyft não vai ler esta página.', 'info');
    try { await api('POST', '/authorized-sites', { origin }); }
    catch (err) { if (err.status !== 409) return showError(err); }
    authorizedCache = null;
    status(`${host} autorizado. O Zyft continua lendo só quando você pede.`, 'ok');
    refreshTab();
  });
}

async function send(msg) {
  try { return await chrome.tabs.sendMessage(S.tab.id, msg); }
  catch {
    await chrome.scripting.executeScript({ target: { tabId: S.tab.id }, files: ['content.js'] });
    return chrome.tabs.sendMessage(S.tab.id, msg);
  }
}

// ---------- Plano e uso ----------
async function loadBilling() {
  const r = await apiRaw('GET', '/billing');
  S.billing = r.ok && r.data ? r.data : null; // 404 (API sem v2) ou erro: some o medidor, nada quebra
  if (!$('#view-main').hidden) render();
}
const MONTHS = ['janeiro', 'fevereiro', 'março', 'abril', 'maio', 'junho', 'julho', 'agosto', 'setembro', 'outubro', 'novembro', 'dezembro'];
function monthInfo() {
  const m = /^(\d{4})-(\d{2})$/.exec(S.billing?.usage?.month || '');
  const idx = m ? Number(m[2]) - 1 : new Date().getMonth();
  return { name: MONTHS[idx], next: MONTHS[(idx + 1) % 12] };
}
const isPlus = () => S.billing?.plan === 'plus';
const adsAllowed = () => !(S.billing && (S.billing.plan === 'plus' || S.billing.adsEnabled === false));
function rewardedAvailable() {
  const b = S.billing;
  if (!b || isPlus() || b.adsEnabled === false || !b.rewarded) return false;
  return (b.rewarded.claimedThisMonth ?? 0) < (b.rewarded.maxPerMonth ?? 3);
}
const money = (v) => (typeof v === 'number' ? v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }) : '');

function usageBlock() {
  const b = S.billing;
  if (!b) return null;
  const { name, next } = monthInfo();
  const a = b.usage?.assistedApplications || {};
  const wrap = h('div', { class: 'stack', style: 'gap:12px' });
  if (isPlus() || a.limit == null) {
    wrap.append(UsageMeter(a.used ?? 0, null, `Candidaturas assistidas em ${name}`, 'Zyft Plus: sem limite e sem anúncios.'));
    return wrap;
  }
  const bonus = a.bonus || 0;
  const packs = (b.packs || []).reduce((s, p) => s + (p.assistedRemaining || 0), 0);
  const notes = [`O contador recomeça em 1º de ${next}.`];
  if (bonus) notes.push(`Inclui +${bonus} de bônus.`);
  if (packs) notes.push(`Pacote avulso: +${packs} disponíveis.`);
  wrap.append(UsageMeter(a.used ?? 0, (a.limit || 0) + bonus, `Candidaturas assistidas em ${name}`, notes.join(' ')));
  if (rewardedAvailable()) {
    const n = b.rewarded.creditsPerClaim || 3;
    wrap.append(S.rewardOpen ? rewardOffer() : h('div', { class: 'reward' },
      h('span', {}, h('b', {}, `+${n} candidaturas`), ' este mês ao ver uma oferta patrocinada'),
      Button({ variant: 'secondary', size: 'sm', k: 'reward', onclick: () => { S.rewardOpen = true; render(); } }, 'Ganhar')));
  }
  wrap.append(h('button', { type: 'button', class: 'zy-link zy-link--center', onclick: () => openUrl(platformUrl('planos')) }, 'Sem anúncios e sem limite com o Zyft Plus'));
  return wrap;
}

// Oferta recompensada: a pessoa vê uma oferta patrocinada e escolhe ganhar o bônus.
function rewardOffer() {
  const n = S.billing?.rewarded?.creditsPerClaim || 3;
  const slot = h('div', {});
  mountAd(slot, 'ext.panel.banner', null, 'banner');
  const claim = Button({ variant: 'secondary', size: 'sm', iconName: 'check', k: 'reward' }, `Vi a oferta: ganhar +${n}`);
  claim.addEventListener('click', async () => {
    claim.disabled = true;
    const r = await apiRaw('POST', '/ads/rewarded/claim');
    S.rewardOpen = false;
    if (r.ok) status(`+${n} candidaturas assistidas adicionadas a este mês.`, 'ok');
    else if (r.status === 409 || r.problem?.code === 'ads.rewarded_exhausted') status('Você já usou as ofertas deste mês. Elas voltam no mês que vem.', 'info');
    else if (r.status === 404) status('Esta oferta ainda não está disponível neste ambiente.', 'info');
    else showError(new ApiError(r));
    if (S.limit?.where !== 'ai') S.limit = null;
    await loadBilling();
  });
  return h('div', { class: 'reward', style: 'flex-direction:column; align-items:stretch' },
    h('span', {}, h('b', {}, 'Oferta patrocinada'), ' · veja e, se quiser, ganhe o bônus. Você não precisa clicar no anúncio.'),
    slot,
    h('div', { class: 'row' }, claim, Button({ variant: 'ghost', size: 'sm', onclick: () => { S.rewardOpen = false; render(); } }, 'Agora não')));
}

// Limite do plano atingido (HTTP 402 plan.limit_reached): mensagem calma e opções.
const RESOURCE = {
  assistedApplications: (n) => `Você usou as ${n} candidaturas assistidas deste mês.`,
  aiSuggestions: (n) => `Você usou as ${n} sugestões de texto da IA deste mês.`,
  jobAnalyses: (n) => `Você usou as ${n} análises de vaga deste mês.`,
};
function limitCard() {
  const p = S.limit.problem || {};
  const resource = p.resource || p.extensions?.resource || (S.limit.where === 'ai' ? 'aiSuggestions' : S.limit.where === 'job' ? 'jobAnalyses' : 'assistedApplications');
  const limit = p.limit ?? p.extensions?.limit;
  const title = RESOURCE[resource] ? RESOURCE[resource](limit ?? 'todas as') : 'Você chegou ao limite do plano gratuito deste mês.';
  const { next } = monthInfo();
  const prices = S.billing?.prices || {};
  const opts = h('div', { class: 'stack' });
  if (resource === 'assistedApplications' && rewardedAvailable()) {
    opts.append(Button({ variant: 'secondary', size: 'sm', onclick: () => { S.rewardOpen = true; S.view = 'vaga'; render(); } }, `Ganhar +${S.billing.rewarded.creditsPerClaim || 3} vendo uma oferta`));
  }
  opts.append(
    Button({ variant: 'quiet', size: 'sm', onclick: () => openUrl(platformUrl('planos')) }, `Pacote avulso${prices.pack ? ` ${money(prices.pack)}` : ''}: +15 candidaturas e +10 sugestões`),
    Button({ variant: 'quiet', size: 'sm', onclick: () => openUrl(platformUrl('planos')) }, `Zyft Plus${prices.plusMonthly ? ` ${money(prices.plusMonthly)}/mês` : ''}: sem limite`),
  );
  return h('section', { class: 'limit', role: 'status' },
    h('p', { class: 'h' }, title),
    h('p', { class: 'small muted' }, `Tudo o que está no seu perfil continua salvo. O contador recomeça em 1º de ${next}.`,
      resource === 'aiSuggestions' ? ' Você ainda pode escrever a resposta com as suas palavras.' : ''),
    opts,
    h('p', { class: 'demo-note' }, 'Ambiente de demonstração: nenhuma cobrança real.'));
}

// ---------- Anúncios (sempre "Patrocinado", com Ocultar; 204 = não mostrar) ----------
async function fetchAd(placement, topic) {
  if (!adsAllowed()) return null;
  const key = `${placement}|${topic || ''}|${S.page?.url || ''}`;
  if (adCache.has(key)) return adCache.get(key);
  const q = new URLSearchParams({ placement });
  if (topic) q.set('topic', topic.slice(0, 200));
  // RNF-15: anúncio que demora mais de 1,5 s não aparece (nunca atrasa o painel).
  const r = await Promise.race([apiRaw('GET', `/ads?${q}`), new Promise((ok) => setTimeout(() => ok({ ok: false, status: 0 }), 1500))]);
  const ad = r.ok && r.status !== 204 && r.data && r.data.id ? r.data : null; // 404 (API sem v2) = sem anúncio
  adCache.set(key, ad);
  return ad;
}
function mountAd(slot, placement, topic, format) {
  fetchAd(placement, topic).then((ad) => {
    if (!ad || !slot.isConnected) return;
    slot.replaceChildren(renderAd(ad, format, topic, () => {
      for (const [k, v] of adCache) if (v?.id === ad.id) adCache.set(k, null);
      clear(slot);
    }));
  }).catch(() => {});
}
// Aba da vaga: tenta o contextual para os requisitos ausentes (até 3); se nenhum vier (204), mostra o banner.
async function mountJobAd(slot, topics) {
  for (const t of topics) {
    if (await fetchAd('ext.job.contextual', t)) return mountAd(slot, 'ext.job.contextual', t, 'contextual');
  }
  mountAd(slot, 'ext.panel.banner', null, 'banner');
}
function adImage(ad, cls) {
  const box = h('div', { class: cls });
  let src = null;
  try { if (ad.imageUrl) { const u = new URL(ad.imageUrl, S.apiBase); if (/^https?:$/.test(u.protocol)) src = u.href; } } catch { /* sem imagem */ }
  const ph = () => h('img', { src: 'icons/zyft-mark.svg', alt: '', class: 'ph' });
  if (src) {
    const img = h('img', { src, alt: '', referrerpolicy: 'no-referrer', loading: 'lazy' });
    img.addEventListener('error', () => box.replaceChildren(ph()));
    box.append(img);
  } else box.append(ph());
  return box;
}
function renderAd(ad, format, topic, onHidden) {
  const click = () => { apiRaw('POST', `/ads/${encodeURIComponent(ad.id)}/click`); openUrl(ad.url); };
  // scope 'ad' = este anúncio por 30 dias; 'topic' = o tema para sempre (igual à plataforma web).
  const hide = async (scope = 'ad') => {
    const r = await apiRaw('POST', `/ads/${encodeURIComponent(ad.id)}/hide`, { scope });
    onHidden();
    status(!r.ok ? 'Anúncio oculto neste painel.'
      : scope === 'topic' ? `Pronto. Você não verá mais anúncios sobre ${theme || 'este tema'}.`
        : 'Anúncio oculto. Ele não aparece de novo por 30 dias.', 'ok');
  };
  const hideAd = () => hide('ad');
  const hideTopic = () => hide('topic');
  const theme = ad.topic || topic;
  const topicBtn = theme ? Button({ variant: 'ghost', size: 'sm', onclick: hideTopic, 'aria-label': `Não tenho interesse no tema ${theme}` }, 'Não tenho interesse no tema') : null;
  const why = ad.reason || theme
    ? WhyThis('Por que estou vendo isso?', h('div', { class: 'stack', style: 'gap:6px' },
      h('p', {}, ad.reason || 'O anúncio usa só a sua área, os cargos que você busca e o tema desta vaga, nunca seus dados de contato ou sensíveis.'),
      format === 'banner' ? topicBtn : null))
    : null;
  if (ad.format === 'network') return networkAd(ad, format, why, hideAd);
  const sub = [ad.advertiser, ad.body].filter(Boolean).join(' · ');
  if (format === 'contextual') {
    return h('section', { class: 'ad-ctx', 'aria-label': 'Conteúdo patrocinado' },
      h('div', { class: 'row between' }, h('span', { class: 'ad-tag' }, 'Patrocinado'), topic || ad.topic ? h('span', { class: 'small muted' }, `Sobre: ${topic || ad.topic}`) : null),
      h('span', { class: 'ad-ctx__title' }, ad.title),
      sub ? h('span', { class: 'ad-ctx__sub' }, sub) : null,
      why,
      h('div', { class: 'row', style: 'gap:6px;flex-wrap:wrap' },
        Button({ variant: 'quiet', size: 'sm', onclick: click }, ad.cta || 'Conhecer'),
        Button({ variant: 'ghost', size: 'sm', onclick: hideAd }, 'Ocultar'),
        topicBtn));
  }
  if (format === 'next') {
    return h('section', { class: 'ad-next', 'aria-label': 'Conteúdo patrocinado' },
      adImage(ad, 'ad-next__img'),
      h('div', { class: 'ad-next__body' },
        h('div', { class: 'row between' }, h('span', { class: 'ad-tag' }, 'Patrocinado'), h('span', { class: 'small muted' }, 'Próximo passo')),
        h('span', { class: 'ad-next__title' }, ad.title),
        sub ? h('span', { class: 'ad-ctx__sub', style: 'font-size:13px;line-height:18px' }, sub) : null,
        why,
        h('div', { class: 'row', style: 'gap:6px;flex-wrap:wrap' },
          Button({ variant: 'quiet', size: 'sm', onclick: click }, ad.cta || 'Ver oferta'),
          Button({ variant: 'ghost', size: 'sm', onclick: hideAd }, 'Ocultar'),
          topicBtn)));
  }
  // Banner do painel (368×64)
  return h('div', { class: 'stack', style: 'gap:4px' },
    h('aside', { class: 'ad-banner', 'aria-label': 'Publicidade' },
      adImage(ad, 'ad-banner__img'),
      h('div', { class: 'ad-banner__txt' },
        h('span', { class: 'ad-label' }, 'Patrocinado'),
        h('span', { class: 'ad-banner__title', title: ad.title }, ad.title),
        h('span', { class: 'ad-banner__sub' }, ad.advertiser || '')),
      h('button', { type: 'button', class: 'ad-cta', title: ad.cta || null, 'aria-label': ad.cta ? `${ad.cta}: ${ad.title}` : `Ver: ${ad.title}`, onclick: click }, 'Ver'),
      h('button', { type: 'button', class: 'ad-x', 'aria-label': 'Ocultar este anúncio', title: 'Ocultar este anúncio', onclick: hideAd }, icon('x', 14))),
    why);
}

// Rede de anúncios (Google AdSense SIMULADO, ADR-0021). A extensão nunca carrega script remoto (MV3 proíbe
// código remoto e a CSP do manifesto é script-src 'self'): só desenhamos o <ins class="adsbygoogle"> com uma arte
// neutra. Selo "Patrocinado", motivo e Ocultar ficam fora do bloco. Sem clique: na rede, o clique seria do Google.
// Para produção a extensão fica só com anúncios diretos (políticas do AdSense e MV3) — ver ADR-0021.
function networkAd(ad, format, why, hideAd) {
  const n = ad.network || {};
  const next = format === 'next';
  const ins = h('ins', {
    class: `adsbygoogle adsense ${next ? 'adsense--card' : 'adsense--banner'}`,
    style: 'display:block',
    'data-ad-client': n.client || 'ca-pub-0000000000000000',
    'data-ad-slot': n.slot || '',
    'data-ad-format': n.adFormat || (next ? 'auto' : 'horizontal'),
    'data-full-width-responsive': 'true',
    'data-adtest': 'on',
  }, h('span', { class: 'adsense__art' },
    h('span', { class: 'adsense__title' }, 'Anúncio do Google (simulado)'),
    h('span', { class: 'adsense__meta' }, next ? 'Nada é carregado do Google' : '368×64 · nada é carregado do Google')));
  if (next) {
    return h('section', { class: 'ad-next ad-next--network', 'aria-label': 'Conteúdo patrocinado' },
      h('div', { class: 'ad-next__body' },
        h('div', { class: 'row between' }, h('span', { class: 'ad-tag' }, 'Patrocinado'), h('span', { class: 'small muted' }, 'Rede de anúncios')),
        ins,
        why,
        h('div', { class: 'row', style: 'gap:6px;flex-wrap:wrap' }, Button({ variant: 'ghost', size: 'sm', onclick: hideAd }, 'Ocultar'))));
  }
  return h('div', { class: 'stack', style: 'gap:4px' },
    h('div', { class: 'row between' },
      h('span', { class: 'ad-label' }, 'Patrocinado'),
      h('button', { type: 'button', class: 'ad-x', 'aria-label': 'Ocultar este anúncio', title: 'Ocultar este anúncio', onclick: hideAd }, icon('x', 14))),
    h('aside', { class: 'ad-network', 'aria-label': 'Publicidade' }, ins),
    why);
}

// ---------- Renderização principal ----------
function render() {
  if ($('#view-main').hidden) return;
  const active = document.activeElement?.getAttribute?.('data-k');
  const y = window.scrollY;
  const v = clear($('#view-main'));
  if (S.view === 'conclude') renderConclude(v);
  else {
    v.append(pageSection());
    if (S.page?.http && !S.page.authorized && S.page.level !== 'full') v.append(authorizeBox());
    v.append(tabs());
    const panel = h('div', { id: `panel-${S.view}`, role: 'tabpanel', 'aria-labelledby': `tab-${S.view}`, class: 'view' });
    if (S.view === 'vaga') renderVaga(panel); else renderForm(panel);
    v.append(panel);
  }
  if (S.view === 'form') setTop('Lendo só este formulário', true);
  else if (S.view === 'vaga') setTop(S.page?.http ? 'Lendo só esta vaga' : 'Conectada');
  if (active) document.querySelector(`[data-k="${CSS.escape(active)}"]`)?.focus({ preventScroll: true });
  window.scrollTo(0, y);
}

function pageSection() {
  const sec = h('div', { class: 'section', style: 'gap:12px' });
  const p = S.page;
  if (!p?.http) {
    sec.append(h('div', { class: 'overline' }, 'Página atual'), h('p', { class: 'muted' }, 'Abra uma página de vaga ou um formulário de candidatura para começar.'));
    return sec;
  }
  const ctxJob = S.job?.info?.title ? { title: S.job.info.title, company: S.job.info.company } : S.lastJob;
  if (S.view === 'form' && S.formRead) {
    sec.append(h('div', { class: 'stack', style: 'gap:2px' },
      h('div', { class: 'overline' }, `Candidatura em andamento · etapa ${S.step || 1}`),
      h('div', { class: 'h2' }, ctxJob?.title || S.tab?.title || p.host),
      h('div', { class: 'small muted', style: 'font-size:13px' }, [ctxJob?.company, `via ${p.host}`].filter(Boolean).join(' · '))));
  } else {
    sec.append(h('div', { class: 'stack', style: 'gap:2px' }, h('div', { class: 'overline' }, 'Página atual'), h('div', { class: 'url', title: p.url }, p.path)));
  }
  if (p.level === 'full') sec.append(SiteSupport('full', p.siteName));
  else sec.append(SiteSupport('partial', 'este site', 'Alguns campos podem não ser reconhecidos. Confira antes de enviar.'));
  if (S.view === 'form' && S.items.length) sec.append(ProgressSummary(counts()));
  return sec;
}

function authorizeBox() {
  return h('div', { class: 'sunken' },
    h('div', { class: 'title' }, icon('globe'), 'Site ainda não autorizado'),
    h('p', { class: 'small muted', style: 'font-size:13px;line-height:18px' }, 'O navegador vai pedir permissão para a extensão ler esta página. Ela continua lendo só quando você pede.'),
    h('div', {}, Button({ variant: 'quiet', size: 'sm', onclick: authorizeSite }, 'Usar neste site mesmo assim')));
}

function tabs() {
  const defs = [['vaga', 'A vaga e você'], ['form', S.items.length ? `Formulário (${S.items.length})` : 'Formulário']];
  const list = h('div', { class: 'tabs', role: 'tablist', 'aria-label': 'Painel' });
  defs.forEach(([id, label], i) => {
    const on = S.view === id;
    const b = h('button', { type: 'button', role: 'tab', class: 'tab', id: `tab-${id}`, 'data-k': `tab-${id}`, 'aria-selected': String(on), 'aria-controls': `panel-${id}`, tabindex: on ? '0' : '-1' }, label);
    b.addEventListener('click', () => { if (S.view !== id) { S.view = id; render(); } });
    b.addEventListener('keydown', (e) => {
      if (e.key !== 'ArrowRight' && e.key !== 'ArrowLeft') return;
      e.preventDefault();
      S.view = defs[(i + (e.key === 'ArrowRight' ? 1 : defs.length - 1)) % defs.length][0];
      render();
      $(`#tab-${S.view}`)?.focus();
    });
    list.append(b);
  });
  return list;
}

const canRead = () => !!(S.page?.http && S.page.authorized);

// ---------- Aba "A vaga e você" (RF-17) ----------
const REQ_LEVEL = { atende: 'match', parcial: 'partial', confirmar: 'partial', naoencontrado: 'missing' };
const reqKey = (s) => norm(s).replace(/[_\s-]/g, '');
function evidenceText(ev) {
  if (!ev) return '';
  if (typeof ev === 'string') return ev;
  return ev.text ?? ev.content ?? '';
}

function renderVaga(panel) {
  const body = h('div', { class: 'pad grow' });
  if (S.limit?.where === 'job') body.append(limitCard());
  let topics = [];
  if (S.job) {
    const i = S.job.info || {};
    const facts = [['Cargo', i.title], ['Empresa', i.company], ['Modalidade', i.workModel], ['Contrato', i.contractType], ['Local', i.location], ['Salário', i.salary]].filter(([, val]) => val);
    if (facts.length) body.append(h('dl', { class: 'facts', style: 'margin:0' }, facts.map(([k, val]) => h('div', {}, h('dt', {}, k), h('dd', {}, String(val))))));
    body.append(RequirementMatch(S.job.reqs, 'Requisito que não aparece não quer dizer que você não tem: talvez só falte no seu perfil.'));
    body.append(h('p', { class: 'small muted' }, 'Isto não é uma nota e não garante aprovação. Serve para você entender o que a vaga pede.'));
    body.append(Button({ variant: 'ghost', size: 'sm', iconName: 'refresh', k: 'job', disabled: S.jobBusy || !canRead(), onclick: analyzeJob, style: 'align-self:flex-start' }, 'Analisar de novo'));
    topics = S.job.reqs.filter((r) => r.level === 'missing').map((r) => r.requirement).filter(Boolean).slice(0, 3);
  } else {
    body.append(h('div', { class: 'box' },
      h('p', {}, 'Veja o que a vaga pede e onde isso aparece no seu Perfil Mestre.'),
      h('p', { class: 'small muted' }, 'O Zyft lê só o texto desta página, quando você pede. Sem nota e sem porcentagem.'),
      h('div', {}, Button({ variant: 'quiet', size: 'sm', iconName: 'target', k: 'job', disabled: S.jobBusy || !canRead(), onclick: analyzeJob }, S.jobBusy ? 'Lendo a vaga…' : 'Analisar esta vaga'))));
    if (S.lastJob?.title) {
      body.append(h('p', { class: 'small muted' }, 'Última vaga analisada: ', h('b', {}, S.lastJob.title), S.lastJob.company ? ` · ${S.lastJob.company}` : '', ' ',
        h('button', { type: 'button', class: 'zy-link', style: 'font-size:12px', onclick: async () => { await chrome.storage.session.remove('lastJob'); S.lastJob = null; render(); } }, 'esquecer')));
    }
  }
  // Um anúncio por tela: o contextual (requisito ausente) substitui o banner.
  const slot = h('div', {});
  body.append(slot);
  if (!S.rewardOpen) mountJobAd(slot, topics);
  const foot = h('div', { class: 'foot foot--static', style: 'gap:12px' },
    Button({ variant: 'primary', wide: true, k: 'read', disabled: !canRead(), onclick: () => { S.view = 'form'; render(); readForm(); } }, 'Ler o formulário desta página'),
    usageBlock());
  panel.append(body, foot);
}

async function analyzeJob() {
  S.jobBusy = true; render();
  status('Lendo a vaga e comparando com o seu Perfil Mestre…', 'busy');
  try {
    const page = await send({ cmd: 'job' });
    if (!page?.text?.trim()) { status('Não encontramos texto de vaga nesta página.', 'info'); return; }
    const r = await api('POST', '/jobs/analyze', { text: page.text, url: page.url, title: page.title });
    const info = { ...(r?.job || {}), title: r?.job?.title || page.title || '', company: r?.job?.company || '' };
    const reqs = (r?.requirements || []).map((q) => {
      const level = REQ_LEVEL[reqKey(q.status)] || 'missing';
      const ev = evidenceText(q.evidence);
      return {
        requirement: q.text || q.requirement || '',
        level,
        required: !/desej/i.test(q.kind || ''),
        evidence: level === 'missing' ? '' : reqKey(q.status) === 'confirmar' ? 'Confirme você: o perfil não deixa claro' : ev ? `No perfil: ${ev}` : '',
      };
    });
    S.job = { url: page.url, info, reqs, disclaimer: r?.disclaimer };
    S.lastJob = { title: info.title, company: info.company, url: page.url };
    await chrome.storage.session.set({ lastJob: S.lastJob });
    if (S.limit?.where === 'job') S.limit = null;
    status('Análise pronta. Ela não é nota nem garantia de aprovação.', 'ok');
    loadBilling();
  } catch (err) {
    if (isLimit(err)) { S.limit = { where: 'job', problem: err.problem }; status(''); }
    else showError(err, analyzeJob);
  } finally { S.jobBusy = false; render(); }
}

// ---------- Aba Formulário (RF-11 a RF-18) ----------
// Os 5 estados da API viram os 4 do design system:
//   ready → ready (Pronto) · ambiguous/missing/divergent → needs (Escolha / Falta no perfil / Já preenchido no site)
//   blocked → blocked (Ação sua) · rascunho de texto da IA → ai (Sugestão da IA)
const STATES = ['ready', 'ambiguous', 'missing', 'blocked', 'divergent'];
const SOURCES = ['rule', 'ai', 'answer', 'none'];
const SENSITIVE_KEYS = /^personal\.(cpf|rg|address|birthDate)$|^sensitive\./; // trancados no painel: destrancar antes de aprovar, nunca em lote

function toItem(f, m) {
  m = m || {};
  let state = typeof m.state === 'number' ? STATES[m.state] : norm(m.state || 'missing').replace(/[_\s-]/g, '');
  if (!STATES.includes(state)) state = 'missing';
  const source = typeof m.source === 'number' ? SOURCES[m.source] || 'none' : norm(m.source || 'none');
  const kind = String(m.kind || '');
  let value = m.value ?? '';
  // Sensível com consentimento e valor cadastrado: a API manda como "blocked" desmarcado.
  // No painel ele vira Pronto + trancado, aprovado só um a um, depois de destrancar.
  const sensitiveFill = kind === 'sensitive' && state === 'blocked' && value !== '';
  if (sensitiveFill) state = 'ready';
  // Defesa extra: anexo e captcha nunca são preenchíveis, mesmo se a API errar.
  if (f.type === 'file' || f.type === 'password' || /captcha|c[oó]digo de seguran/i.test(`${f.label} ${f.name}`)) { state = 'blocked'; value = ''; }
  const it = {
    ...f,
    label: f.label || m.label || f.name || '(campo sem rótulo)',
    state, source, kind,
    profileKey: m.profileKey || '',
    profileLabel: m.profileLabel || '',
    value: state === 'blocked' ? '' : value,
    optionText: m.optionText || '',
    profileValue: m.profileValue ?? '',
    note: m.note || '',
    sensitive: kind === 'sensitive' || SENSITIVE_KEYS.test(m.profileKey || ''),
    openQuestion: kind === 'openQuestion' || (state === 'missing' && f.type === 'textarea'),
    approved: false, approvedAt: '', skipped: false, unlocked: false, editing: false, saveToBank: true,
    draft: null, useProfile: false, aiBusy: false, aiNote: '',
  };
  if (it.options?.length && it.value !== '') it.value = matchOption(it.options, it.value) ?? it.value;
  if (state === 'ambiguous' && it.options?.length && it.profileValue) it.value = matchOption(it.options, it.profileValue) ?? '';
  return it;
}
function matchOption(options, v) {
  const o = options.find((x) => String(x.value) === String(v)) || options.find((x) => norm(x.text) === norm(v));
  return o ? o.value : null;
}
function optionText(it, v) {
  const o = it.options?.find((x) => String(x.value) === String(v));
  return o ? o.text : v;
}
function displayValue(it) {
  if (it.type === 'checkbox') return it.value === 'true' || it.value === true ? 'Marcar' : 'Desmarcar';
  return it.optionText || optionText(it, it.value) || '';
}
function ds(it) {
  if (it.draft) return 'ai';
  if (it.state === 'blocked') return 'blocked';
  if (it.state === 'ready') return 'ready';
  return 'needs';
}
function badgeLabel(it) {
  if (it.draft) {
    if (it.approved) return it.draft.reused ? 'Resposta aprovada por você' : 'Rascunho aprovado por você';
    if (it.draft.text !== it.draft.original) return 'Rascunho editado por você';
    return it.draft.reused ? 'Resposta já aprovada – confira' : 'Sugestão da IA – revise';
  }
  return { ready: 'Pronto', ambiguous: 'Escolha', missing: it.openQuestion ? 'Precisa de você' : 'Falta no perfil', divergent: 'Já preenchido no site', blocked: 'Ação sua' }[it.state];
}
function counts() {
  const c = { ready: 0, needs: 0, ai: 0, blocked: 0 };
  for (const it of S.items) c[ds(it)]++;
  return c;
}
const isCompact = (it) => ds(it) === 'ready' && !it.sensitive;
const fillValue = (it) => it.value;
const willFill = (it) => it.approved && !it.skipped && it.state !== 'blocked' && fillValue(it) !== '' && fillValue(it) != null;

function sourceText(it) {
  if (it.source === 'answer') return 'Banco de respostas';
  const k = it.profileKey || '';
  if (!k && it.kind !== 'sensitive') return '';
  let sec = '';
  if (it.kind === 'sensitive' || SENSITIVE_KEYS.test(k)) sec = 'Dados sensíveis';
  else if (k.startsWith('custom:')) sec = 'Informações da sua área';
  else if (/^personal\.(email|phone|linkedin|github|portfolio)/.test(k)) sec = 'Contato';
  else if (/^personal\.(city|state|country|zipCode)/.test(k)) sec = 'Endereço';
  else if (k.startsWith('personal.')) sec = 'Dados pessoais';
  else if (k.startsWith('objective.')) sec = 'O que você procura';
  else if (k.startsWith('preferences.')) sec = 'Preferências';
  else if (k.startsWith('experiences')) sec = 'Experiências';
  else if (k.startsWith('education')) sec = 'Formação';
  else if (/^(courses|certifications)/.test(k)) sec = 'Cursos e certificações';
  else if (k.startsWith('skills')) sec = 'Competências';
  else if (k.startsWith('languages')) sec = 'Idiomas';
  let s = `Perfil Mestre${sec ? ` · ${sec}` : ''}`;
  if (k.startsWith('custom:') && it.profileLabel && it.profileLabel !== k) s += ` · ${it.profileLabel}`;
  if (it.source === 'ai') s += ' · a IA indicou o campo; o valor vem do seu perfil';
  return s;
}
function fieldIdText(it) {
  const tag = it.tag === 'select' || it.type === 'select' ? 'select' : it.tag === 'textarea' ? 'textarea' : 'input';
  if (it.type === 'file') return 'input[type=file]';
  if (it.htmlId) return `${tag}#${it.htmlId}`;
  if (it.name) return `${tag}[name=${it.name}]`;
  return tag;
}
function mask(v) {
  const s = String(v ?? '');
  if (!/\d/.test(s) || s.length < 6) return 'Trancado';
  const keep = 6;
  return s.slice(0, -keep).replace(/[0-9A-Za-z]/g, '•') + s.slice(-keep);
}

async function readForm() {
  if (S.formBusy) return;
  S.formBusy = true; S.limit = S.limit?.where === 'form' ? null : S.limit;
  render();
  status('Lendo os campos visíveis desta página…', 'busy');
  try {
    const page = await send({ cmd: 'scan' });
    const fields = page?.fields || [];
    if (!fields.length) { status('Nenhum campo de formulário visível nesta página.', 'info'); return; }
    status(`Comparando ${fields.length} campos com o seu Perfil Mestre…`, 'busy');
    const payload = fields.map((f) => ({
      fieldId: f.fieldId, id: f.fieldId, tag: f.tag, type: f.type, name: f.name, htmlId: f.htmlId,
      label: f.label, placeholder: f.placeholder, section: f.section, required: !!f.required,
      maxLength: f.maxLength || 0, options: f.options || [], currentValue: f.currentValue || '',
    }));
    const r = await api('POST', '/forms/mappings', { pageUrl: page.url, fields: payload });
    const list = Array.isArray(r) ? r : r?.mappings || r?.suggestions || r?.items || [];
    const byId = new Map(list.map((m) => [String(m.fieldId ?? m.id), m]));
    S.items = fields.map((f) => toItem(f, byId.get(String(f.fieldId))));
    S.formRead = true; S.lastFill = null; S.step += 1;
    status('Confira e aprove. Nada é preenchido sem a sua aprovação e nada é enviado.', 'ok');
    loadBilling();
  } catch (err) {
    if (isLimit(err)) { S.limit = { where: 'form', problem: err.problem }; status(''); }
    else showError(err, readForm);
  } finally { S.formBusy = false; render(); }
}

function renderForm(panel) {
  const body = h('div', { class: 'pad grow' });
  panel.append(body);
  if (S.limit?.where === 'form') body.append(limitCard());
  if (!S.formRead) {
    body.append(h('div', { class: 'box' },
      h('p', {}, 'O Zyft lê só os campos visíveis deste formulário e compara com o seu Perfil Mestre.'),
      h('p', { class: 'small muted' }, 'Nada é preenchido sem a sua aprovação. O botão de enviar é sempre seu, no site.'),
      h('div', {}, Button({ variant: 'primary', size: 'sm', k: 'read', disabled: S.formBusy || !canRead(), onclick: readForm }, S.formBusy ? 'Lendo…' : 'Ler o formulário desta página'))));
    return;
  }
  if (S.lastFill) body.append(fillSummary());

  const compact = S.items.filter(isCompact);
  const rest = [...S.items.filter((i) => !isCompact(i))].sort((a, b) => rank(a) - rank(b));
  if (compact.length) {
    const pending = compact.filter((i) => !i.approved);
    body.append(h('div', { class: 'row between' },
      h('div', { class: 'overline' }, 'Prontos · confira de relance'),
      pending.length ? Button({ variant: 'quiet', size: 'sm', iconName: 'check', k: 'approve-all', onclick: () => { const at = hhmm(); pending.forEach((i) => { i.approved = true; i.approvedAt = at; }); render(); } }, `Aprovar os ${pending.length}`) : null));
    body.append(h('div', { class: 'frows' }, compact.map(compactRow)));
  }
  if (rest.length) {
    body.append(h('div', { class: 'overline', style: 'margin-top:8px' }, 'Um a um'));
    rest.forEach((it) => body.append(it.draft ? draftCard(it) : fieldCard(it)));
  }

  const n = S.items.filter(willFill).length;
  panel.append(h('div', { class: 'foot' },
    Button({ variant: 'primary', wide: true, k: 'fill', disabled: n === 0, onclick: fillApproved }, n ? `Preencher ${n} ${plural(n, 'campo aprovado', 'campos aprovados')}` : 'Aprove campos para preencher'),
    h('div', { class: 'row' },
      Button({ variant: 'quiet', size: 'sm', iconName: 'refresh', k: 'reread', disabled: S.formBusy, onclick: readForm }, 'Ler novamente (nova etapa)'),
      h('button', { type: 'button', class: 'zy-btn zy-btn--link', 'data-k': 'conclude', onclick: () => { S.view = 'conclude'; S.fromSubmit = false; render(); } }, 'Concluí a candidatura')),
    h('p', { class: 'small muted' }, 'O Zyft só preenche o que você aprovou e nunca envia a candidatura. O botão de enviar é sempre seu, no site.')));
}
function rank(it) {
  if (it.sensitive && it.state === 'ready') return 0;
  return { ai: 1, needs: 2, ready: 2, blocked: 3 }[ds(it)];
}

const locate = (it) => send({ cmd: 'highlight', fieldId: it.fieldId }).catch(() => {});

function compactRow(it) {
  const k = `ap-${it.fieldId}`;
  return h('div', { class: `zy-frow${it.approved ? ' is-approved' : ''}` },
    h('span', { class: 'zy-frow__icon zy-c-ready' }, icon('check', 14, '', it.approved ? 'Carimbado por você' : 'Pronto')),
    h('button', { type: 'button', class: 'zy-frow__label', title: `${it.label} · mostrar no site`, onclick: () => locate(it) }, it.label),
    h('span', { class: 'zy-frow__value', title: [displayValue(it), sourceText(it)].filter(Boolean).join(' — ') }, displayValue(it)),
    it.approved
      ? h('button', { type: 'button', class: 'zy-frow__btn is-on', 'data-k': k, 'aria-label': `Desfazer aprovação de ${it.label}`, 'aria-pressed': 'true', onclick: () => { it.approved = false; render(); } }, icon('check', 14))
      : h('button', { type: 'button', class: 'zy-frow__btn', 'data-k': k, 'aria-label': `Aprovar ${it.label}`, 'aria-pressed': 'false', onclick: () => { it.approved = true; it.approvedAt = hhmm(); render(); } }, icon('check', 14)));
}

function approve(it) { it.approved = true; it.approvedAt = hhmm(); it.skipped = false; it.editing = false; render(); }
function undo(it) { it.approved = false; it.skipped = false; if (it.state === 'divergent') it.useProfile = false; render(); }

function doneRow(it, label = 'será preenchido') {
  return h('div', { class: 'zy-field__done' }, Stamp('Carimbado por você', it.approvedAt), h('span', { class: 'zy-field__will' }, label),
    h('button', { type: 'button', class: 'zy-link', 'data-k': `ap-${it.fieldId}`, onclick: () => undo(it) }, 'Desfazer'));
}

function fieldCard(it) {
  const st = ds(it);
  const card = h('article', { class: `zy-field zy-field--${st}${it.approved || it.skipped ? ' is-approved' : ''}` });
  card.append(h('header', { class: 'zy-field__head' },
    h('div', {}, h('div', { class: 'zy-field__label' }, it.label), h('code', { class: 'zy-field__id' }, fieldIdText(it))),
    StatusBadge(st, badgeLabel(it))));
  const src = sourceText(it);

  if (it.skipped) {
    card.append(h('div', { class: 'zy-field__done' }, h('span', { class: 'zy-field__will' }, 'Pulado por você. Fica como está no site.'),
      h('button', { type: 'button', class: 'zy-link', 'data-k': `ap-${it.fieldId}`, onclick: () => undo(it) }, 'Desfazer')));
    return card;
  }

  if (it.state === 'blocked') {
    card.append(h('p', { class: 'zy-field__note' }, blockedReason(it)));
    card.append(h('div', { class: 'zy-field__actions' }, Button({ variant: 'quiet', size: 'sm', onclick: () => locate(it) }, 'Mostrar no site')));
    return card;
  }

  // Pronto e sensível: trancado por padrão, destranca para conferir, aprova um a um.
  if (it.state === 'ready') {
    if (it.sensitive && !it.approved) {
      card.append(h('div', { class: 'zy-field__sens' }, icon('shield', 12), it.unlocked ? 'Confira e aprove. Ele não entra em nenhuma aprovação em lote.' : 'Dado sensível, guardado trancado. Destranque para conferir antes de aprovar.'));
    }
    const shown = displayValue(it);
    const val = it.sensitive
      ? h('span', { class: `zy-sens${it.unlocked ? '' : ' is-locked'}` },
        icon(it.unlocked ? 'unlock' : 'lock', 14),
        h('span', { class: 'zy-sens__val' }, it.unlocked ? shown : mask(shown)),
        h('button', { type: 'button', class: 'zy-sens__toggle', 'data-k': `lk-${it.fieldId}`, onclick: () => { it.unlocked = !it.unlocked; render(); } }, it.unlocked ? 'Trancar de novo' : 'Destrancar para conferir'))
      : shown;
    card.append(h('div', { class: 'zy-field__value' }, val));
    if (it.note) card.append(h('p', { class: 'zy-field__note' }, it.note));
    if (src) card.append(h('p', { class: 'zy-field__source' }, src));
    if (it.approved) card.append(doneRow(it));
    else {
      const locked = it.sensitive && !it.unlocked;
      card.append(h('div', { class: 'zy-field__actions' },
        Button({ variant: 'primary', size: 'sm', iconName: 'check', k: `ap-${it.fieldId}`, disabled: locked, title: locked ? 'Destranque e confira antes de aprovar' : null, onclick: () => approve(it) }, 'Aprovar'),
        Button({ variant: 'ghost', size: 'sm', onclick: () => { it.skipped = true; render(); } }, 'Pular')));
    }
    return card;
  }

  // Já preenchido no site: manter (padrão) ou usar o do perfil.
  if (it.state === 'divergent') {
    const name = nextId('div');
    const site = it.currentValue || '—';
    const prof = displayValue(it) || it.profileValue || '—';
    card.append(h('div', { class: 'radios', role: 'radiogroup', 'aria-label': it.label },
      h('label', { class: 'radio-row' }, h('input', { type: 'radio', name, 'data-k': `dv0-${it.fieldId}`, checked: !it.useProfile, onchange: () => { it.useProfile = false; it.approved = false; render(); } }), h('span', {}, `Manter o do site: ${site}`)),
      h('label', { class: 'radio-row' }, h('input', { type: 'radio', name, 'data-k': `dv1-${it.fieldId}`, checked: it.useProfile, onchange: () => { it.useProfile = true; it.approved = true; it.approvedAt = hhmm(); render(); } }), h('span', {}, `Usar o do perfil: ${prof}`))));
    if (src) card.append(h('p', { class: 'zy-field__source' }, src));
    if (it.approved) card.append(h('div', { class: 'zy-field__done' }, Stamp('Carimbado por você', it.approvedAt, true), h('span', { class: 'zy-field__will' }, 'o valor do perfil será preenchido')));
    return card;
  }

  // Escolha: o perfil tem um valor, mas nenhuma opção do site bate com segurança.
  if (it.state === 'ambiguous') {
    card.append(h('p', { class: 'zy-field__note' }, it.profileValue ? `O site tem opções que não batem exatamente com o seu perfil ("${it.profileValue}"). Escolha uma:` : 'Escolha a opção que vale para você:'));
    if (it.approved) {
      card.append(h('div', { class: 'zy-field__value' }, displayValue(it)));
      if (src) card.append(h('p', { class: 'zy-field__source' }, src));
      card.append(doneRow(it));
      return card;
    }
    card.append(control(it));
    if (src) card.append(h('p', { class: 'zy-field__source' }, src));
    card.append(h('div', { class: 'zy-field__actions' },
      Button({ variant: 'primary', size: 'sm', iconName: 'check', k: `ap-${it.fieldId}`, disabled: it.value === '', onclick: () => approve(it) }, 'Aprovar'),
      Button({ variant: 'ghost', size: 'sm', onclick: () => { it.skipped = true; render(); } }, 'Pular')));
    return card;
  }

  // Falta no perfil / pergunta aberta: a pessoa responde ali; pode guardar no Banco de respostas.
  card.append(h('p', { class: 'zy-field__note' }, it.openQuestion
    ? 'Pergunta aberta. Escreva com as suas palavras ou peça um rascunho à IA, que usa só fatos aprovados do seu perfil.'
    : 'Isso ainda não está no seu perfil. Se quiser, responda aqui: o Zyft guarda no Banco de respostas e você não digita de novo.'));
  if (it.approved) {
    card.append(h('div', { class: 'zy-field__value' }, displayValue(it)), doneRow(it));
    return card;
  }
  // Pedido de rascunho que voltou sem texto: o aviso fica no próprio cartão, com a caixa já aberta.
  if (it.aiNote) card.append(h('p', { class: 'zy-field__note', role: 'status' }, icon('alert', 12), ' ', it.aiNote));
  if (it.editing) {
    card.append(control(it));
    if (it.type !== 'checkbox') {
      card.append(h('label', { class: 'check-row' }, h('input', { type: 'checkbox', checked: it.saveToBank, onchange: (e) => { it.saveToBank = e.target.checked; } }), 'Guardar esta resposta no Banco de respostas'));
    }
    card.append(h('div', { class: 'zy-field__actions' },
      Button({ variant: 'primary', size: 'sm', iconName: 'check', k: `ap-${it.fieldId}`, disabled: it.value === '', onclick: () => answerApprove(it) }, 'Aprovar'),
      Button({ variant: 'ghost', size: 'sm', onclick: () => { it.editing = false; render(); } }, 'Cancelar')));
  } else {
    card.append(h('div', { class: 'zy-field__actions' },
      Button({ variant: 'primary', size: 'sm', iconName: 'edit', k: `ed-${it.fieldId}`, onclick: () => { it.editing = true; render(); $(`[data-k="in-${CSS.escape(it.fieldId)}"]`)?.focus(); } }, it.openQuestion ? 'Escrever' : 'Responder'),
      it.openQuestion ? Button({ variant: 'quiet', size: 'sm', iconName: 'draft', k: `ai-${it.fieldId}`, disabled: it.aiBusy, onclick: () => suggestText(it) }, it.aiBusy ? 'Pedindo rascunho…' : 'Pedir um rascunho à IA') : null,
      Button({ variant: 'ghost', size: 'sm', onclick: () => { it.skipped = true; render(); } }, 'Pular')));
  }
  if (S.limit?.where === 'ai' && S.limit.fieldId === it.fieldId) card.append(limitCard());
  const why = it.note && !it.openQuestion ? it.note : null;
  card.append(WhyThis('Por que o Zyft não preencheu?', why || 'O Zyft não procura essa informação fora do seu perfil e não inventa um valor por você.'));
  return card;
}

function blockedReason(it) {
  if (it.type === 'file') return 'O Zyft não anexa arquivos. Use o botão do próprio site.';
  if (/captcha|c[oó]digo de seguran/i.test(`${it.label} ${it.name}`)) return 'Código de segurança: digite você mesmo(a) no site. O Zyft não contorna essa verificação.';
  if (it.kind === 'sensitive') return 'Dado sensível: fica com você. Responda direto no site, se quiser. Para o Zyft ajudar, cadastre em Dados sensíveis e autorize o uso na plataforma.';
  if (it.type === 'checkbox' || it.kind === 'declaration') return 'Termos e declarações são sempre decididos por você, no site.';
  return it.note || 'Esta é uma decisão sua. Responda direto no site.';
}

// Controle de resposta (input/select/radio/checkbox/textarea), sem rerender ao digitar.
function control(it) {
  const k = `in-${it.fieldId}`;
  const setVal = (v) => {
    it.value = v;
    const b = document.querySelector(`[data-k="ap-${CSS.escape(it.fieldId)}"]`);
    if (b && b.tagName === 'BUTTON' && !b.classList.contains('zy-link')) b.disabled = v === '' || v == null;
  };
  // Opção de valor vazio ("Selecione") é placeholder do site, não uma resposta.
  const opts = (it.options || []).filter((o) => o.value !== '' && o.value != null);
  const label = `Resposta para: ${it.label}`;
  if (opts.length && (it.type === 'radio' || opts.length <= 6) && it.type !== 'select') {
    const name = nextId('opt');
    return h('div', { class: 'radios', role: 'radiogroup', 'aria-label': it.label }, opts.map((o, i) => h('label', { class: 'radio-row' },
      h('input', { type: 'radio', name, value: o.value, 'data-k': i === 0 ? k : `${k}-${i}`, checked: String(o.value) === String(it.value), onchange: () => setVal(o.value) }),
      h('span', {}, o.text || String(o.value)))));
  }
  if (opts.length) {
    const sel = h('select', { class: 'zy-tf__input', 'data-k': k, 'aria-label': label, onchange: (e) => setVal(e.target.value) },
      h('option', { value: '' }, 'Escolha uma opção'),
      opts.map((o) => h('option', { value: o.value, selected: String(o.value) === String(it.value) }, o.text || String(o.value))));
    return sel;
  }
  if (it.type === 'checkbox') {
    return h('label', { class: 'check-row' }, h('input', { type: 'checkbox', 'data-k': k, checked: it.value === 'true', onchange: (e) => setVal(e.target.checked ? 'true' : 'false') }), 'Marcar este campo');
  }
  if (it.type === 'textarea' || it.openQuestion) {
    const ta = h('textarea', { class: 'zy-tf__input', rows: 5, 'data-k': k, 'aria-label': label, maxlength: it.maxLength || null, oninput: (e) => setVal(e.target.value) });
    ta.value = String(it.value ?? '');
    return ta;
  }
  const inp = h('input', { class: 'zy-tf__input', 'data-k': k, 'aria-label': label, type: ['email', 'tel', 'url', 'date', 'number', 'month'].includes(it.type) ? it.type : 'text', maxlength: it.maxLength || null, oninput: (e) => setVal(e.target.value) });
  inp.value = String(it.value ?? '');
  return inp;
}

async function answerApprove(it) {
  const text = String(displayValue(it) ?? '').trim();
  approve(it);
  if (!it.saveToBank || it.type === 'checkbox' || !text) return;
  const r = await apiRaw('POST', '/answers', { question: it.label, text });
  if (r.ok) status('Resposta guardada no Banco de respostas. Na próxima vez, o Zyft já sugere.', 'ok');
  else if (r.status === 402 || r.problem?.code === 'plan.limit_reached') status('O Banco de respostas do plano gratuito está cheio. A resposta vale para este formulário; para guardar mais, veja os planos na plataforma.', 'info');
  else if (r.status !== 0) status(`A resposta vale para este formulário, mas não foi guardada: ${problemText(r.problem) || 'erro da API'}.`, 'info');
}

// ---------- Sugestão de texto da IA (RF-15): rascunho que a pessoa termina ----------
const NO_DRAFT = 'A IA não conseguiu escrever um rascunho com os fatos do seu perfil; escreva do seu jeito.';
/** Texto da resposta de POST /forms/open-answers (value; aceita text/answer/draft de versões antigas). */
function draftText(r) {
  for (const k of ['value', 'text', 'answer', 'draft']) {
    const v = r?.[k];
    if (typeof v === 'string' && v.trim()) return v.trim();
  }
  return '';
}
async function suggestText(it) {
  it.aiBusy = true; render();
  status('Pedindo um rascunho com base só nos fatos aprovados do seu perfil…', 'busy');
  try {
    const r = await api('POST', '/forms/open-answers', {
      question: it.label, maxLength: it.maxLength > 0 ? it.maxLength : null, pageUrl: S.page?.url,
      jobTitle: S.lastJob?.title || null, company: S.lastJob?.company || null,
    });
    // Qualquer 200 vira algo visível NO CARTÃO do campo: rascunho, resposta aprovada reaproveitada ou um aviso
    // com a caixa de texto já aberta. Aceita nomes alternativos do campo de texto e ignora texto só com espaços.
    const text = draftText(r);
    if (S.limit?.where === 'ai') S.limit = null;
    loadBilling();
    if (!text) {
      const unavailable = String(r?.kind || '').toLowerCase() === 'unknown';
      it.aiNote = unavailable && r?.note ? r.note : (r?.note && /escreva do seu jeito/i.test(r.note) ? r.note : NO_DRAFT);
      it.editing = true;
      status(it.aiNote, 'info');
      return;
    }
    const reused = String(r?.kind || '').toLowerCase() === 'approved' || r?.approvedAnswerId != null;
    it.aiNote = '';
    it.draft = { text, original: text, reused, note: reused ? (r?.note || 'Resposta que você aprovou antes para uma pergunta parecida.') : '', facts: (r?.usedFacts || []).map((f) => (typeof f === 'string' ? f : f.text || f.label || '')).filter(Boolean), checked: false };
    it.approved = false;
    status(reused ? 'Achamos uma resposta que você já aprovou. Confira se serve para esta pergunta.' : 'Rascunho pronto. Leia, ajuste e confirme que é verdade sobre você.', 'ok');
  } catch (err) {
    if (isLimit(err)) { S.limit = { where: 'ai', problem: err.problem, fieldId: it.fieldId }; status(''); }
    else showError(err);
  } finally { it.aiBusy = false; render(); }
}

function draftCard(it) {
  const d = it.draft;
  const card = h('section', { class: `zy-draft${it.approved ? ' is-approved' : ''}` });
  card.append(h('header', { class: 'zy-draft__head' }, h('div', { class: 'zy-draft__q' }, it.label), StatusBadge('ai', badgeLabel(it))));
  if (it.approved) {
    card.append(h('div', { class: 'zy-field__value', style: 'background:var(--surface);' }, it.value));
    card.append(doneRow(it));
    card.append(h('div', {}, h('button', { type: 'button', class: 'zy-link', onclick: (e) => saveDraftToBank(it, e.currentTarget) }, 'Guardar no Banco de respostas')));
    return card;
  }
  card.append(h('p', { class: 'zy-draft__lead' }, d.reused ? `${d.note} Confira se serve para esta pergunta.` : 'Um ponto de partida. Termine com as suas palavras.'));
  const ta = h('textarea', { class: 'zy-draft__text', rows: 6, 'data-k': `dt-${it.fieldId}`, maxlength: it.maxLength || null, 'aria-label': `Resposta para: ${it.label}` });
  ta.value = d.text;
  const approveBtn = Button({ variant: 'primary', size: 'sm', iconName: 'check', k: `ap-${it.fieldId}`, disabled: !d.checked || !d.text.trim() }, 'Aprovar texto');
  const cnt = it.maxLength ? h('div', { class: 'count' }, `${d.text.length} de ${it.maxLength}`) : null;
  ta.addEventListener('input', () => {
    const wasEdited = d.text !== d.original;
    d.text = ta.value;
    if (cnt) cnt.textContent = `${d.text.length} de ${it.maxLength}`;
    approveBtn.disabled = !d.checked || !d.text.trim();
    if (wasEdited !== (d.text !== d.original)) { const b = card.querySelector('.zy-badge span'); if (b) b.textContent = badgeLabel(it); }
  });
  card.append(ta, cnt);
  if (d.facts.length) {
    card.append(h('div', { class: 'zy-draft__facts' },
      h('div', { class: 'zy-draft__factsLabel' }, 'De onde veio: a IA usou só isto do seu perfil'),
      h('ul', {}, d.facts.map((f) => h('li', {}, f)))));
  }
  card.append(h('label', { class: 'check-row zy-draft__check' },
    h('input', { type: 'checkbox', 'data-k': `ck-${it.fieldId}`, checked: d.checked, onchange: (e) => { d.checked = e.target.checked; approveBtn.disabled = !d.checked || !d.text.trim(); } }),
    'Li e tudo o que está escrito é verdade sobre mim.'));
  approveBtn.addEventListener('click', () => { it.value = d.text; approve(it); });
  card.append(h('div', { class: 'zy-draft__actions' },
    approveBtn,
    Button({ variant: 'quiet', size: 'sm', iconName: 'refresh', disabled: it.aiBusy, onclick: () => suggestText(it) }, 'Outro começo'),
    Button({ variant: 'ghost', size: 'sm', onclick: () => { it.draft = null; it.value = ''; it.editing = true; render(); } }, 'Escrever do zero')));
  if (S.limit?.where === 'ai' && S.limit.fieldId === it.fieldId) card.append(limitCard());
  return card;
}
async function saveDraftToBank(it, btn) {
  btn.disabled = true;
  const r = await apiRaw('POST', '/answers', { question: it.label, text: String(it.value) });
  if (r.ok) { status('Resposta guardada no Banco de respostas.', 'ok'); btn.replaceWith(h('span', { class: 'small muted' }, 'Guardada no Banco de respostas.')); }
  else { btn.disabled = false; status(r.status === 402 ? 'O Banco de respostas do plano gratuito está cheio. Veja os planos na plataforma.' : `Não foi possível guardar: ${problemText(r.problem) || 'erro da API'}.`, 'info'); }
}

// ---------- Preencher só o aprovado (RF-14) ----------
async function fillApproved() {
  const chosen = S.items.filter(willFill);
  if (!chosen.length) return status('Aprove pelo menos um campo para preencher.', 'info');
  status(`Preenchendo ${chosen.length} ${plural(chosen.length, 'campo', 'campos')}…`, 'busy');
  try {
    const { report } = await send({ cmd: 'fill', items: chosen.map((i) => ({ fieldId: i.fieldId, value: fillValue(i) })) });
    const ok = report.filter((r) => r.ok).length;
    const failed = report.filter((r) => !r.ok).map((r) => ({ label: S.items.find((i) => i.fieldId === r.fieldId)?.label || r.fieldId, error: r.error }));
    const manual = S.items.filter((i) => i.state === 'blocked').length + failed.length;
    const skipped = S.items.filter((i) => i.state !== 'blocked' && !chosen.includes(i)).length;
    S.lastFill = { ok, failed, skipped, manual };
    status(`${ok} ${plural(ok, 'campo preenchido', 'campos preenchidos')}${failed.length ? `, ${failed.length} com falha` : ''}. Confira no site e envie você mesmo(a).`, failed.length ? 'info' : 'ok');
    render();
    window.scrollTo(0, 0);
  } catch (err) { showError(err, fillApproved); }
}

function fillSummary() {
  const { ok, failed, skipped, manual } = S.lastFill;
  return h('section', { class: 'box', 'aria-labelledby': 'sum-title' },
    h('div', { class: 'overline', id: 'sum-title' }, 'Resumo do preenchimento'),
    h('div', { class: 'nums' },
      h('div', {}, h('b', {}, String(ok)), h('span', {}, 'preenchidos com a sua aprovação')),
      h('div', {}, h('b', {}, String(skipped)), h('span', {}, 'pulados por você')),
      h('div', {}, h('b', {}, String(manual)), h('span', {}, 'para fazer no site'))),
    failed.length ? h('div', {}, h('p', { class: 'small' }, `${failed.length} não ${plural(failed.length, 'pôde', 'puderam')} ser preenchido${plural(failed.length, '', 's')}:`),
      h('ul', { class: 'fail-list' }, failed.map((f) => h('li', {}, `${f.label}: ${f.error}`)))) : null,
    h('div', {}, Button({ variant: 'quiet', size: 'sm', iconName: 'refresh', onclick: readForm }, 'Ler novamente (nova etapa)')));
}

// ---------- Conclusão (RF-19, ADR-0012) ----------
function renderConclude(v) {
  const jobTitle = S.job?.info?.title || S.lastJob?.title || '';
  const company = S.job?.info?.company || S.lastJob?.company || '';
  setTop([company, S.page?.level === 'full' ? 'Portal demo' : S.page?.host].filter(Boolean).join(' · ') || 'Concluir candidatura');
  const body = h('div', { class: 'pad grow', style: 'gap:16px' });
  v.append(body);
  if (S.lastFill) body.append(fillSummary());

  if (!S.registered) {
    const title = TextField({ label: 'Vaga', value: jobTitle || S.tab?.title || '', required: true, maxlength: '200' });
    const comp = TextField({ label: 'Empresa', value: company, maxlength: '200', placeholder: 'Opcional' });
    const submit = Button({ variant: 'primary', iconName: 'check', type: 'submit' }, 'Confirmo que enviei: registrar');
    const form = h('form', { class: 'hero', novalidate: true, 'aria-labelledby': 'cmp-title' },
      h('h1', { class: 'h2', id: 'cmp-title' }, 'Você enviou o formulário?'),
      h('p', { class: 'muted' }, 'O Zyft não envia nada. Se você já clicou em enviar no site, registre aqui para acompanhar em Candidaturas. O registro não guarda os valores dos campos.'),
      title.wrap, comp.wrap, submit,
      h('button', { type: 'button', class: 'zy-link', style: 'align-self:flex-start', onclick: () => { S.view = S.formRead ? 'form' : 'vaga'; render(); } }, 'Voltar à revisão'));
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      const jt = title.input.value.trim();
      title.setError(jt ? '' : 'Informe a vaga.');
      if (!jt) return title.input.focus();
      submit.disabled = true;
      try {
        await api('POST', '/forms/completions', { portal: S.page?.host || '', jobTitle: jt, company: comp.input.value.trim() || null, jobUrl: S.lastJob?.url || S.page?.url || null, fieldsFilled: S.lastFill?.ok ?? 0 });
        S.registered = { jobTitle: jt, company: comp.input.value.trim(), at: hhmm() };
        status('Candidatura registrada.', 'ok');
        render();
        $('#done-title')?.focus();
      } catch (err) { submit.disabled = false; showError(err); }
    });
    body.append(form);
    if (S.fromSubmit) setTimeout(() => title.input.focus(), 0);
    return;
  }

  const r = S.registered;
  body.append(h('section', { class: 'done', role: 'status' },
    h('div', { class: 'row between' }, h('span', { class: 'done__title', id: 'done-title', tabindex: '-1' }, 'Candidatura registrada'), Stamp('Registrado por você', r.at, true)),
    h('p', { style: 'font-size:13px;line-height:18px' }, `${[r.jobTitle, r.company].filter(Boolean).join(' · ').replace(/\.+$/, '')}. Acompanhe o status e anote o que acontecer em Candidaturas.`),
    h('div', {}, h('button', { type: 'button', class: 'zy-link', onclick: () => openUrl(platformUrl('candidaturas')) }, 'Abrir Candidaturas'))));
  // Anúncio "próximo passo": só depois que a pessoa concluiu.
  const slot = h('div', {});
  body.append(slot);
  mountAd(slot, 'ext.after-complete', null, 'next'); // sem tópico: anúncios só usam área, cargos desejados e requisito da vaga
  body.append(Button({ variant: 'ghost', size: 'sm', style: 'align-self:flex-start', onclick: () => { resetForm(); S.registered = null; S.view = 'vaga'; render(); } }, 'Voltar ao painel'));
}

chrome.runtime.onMessage.addListener((msg, sender) => {
  if (sender.id !== chrome.runtime.id) return;
  // Evento do envio feito PELA PESSOA no site (o Zyft só observa).
  if (msg?.type === 'submitted' && sender.tab?.id === S.tab?.id && !S.registered) {
    S.view = 'conclude'; S.fromSubmit = true; render();
  }
});

// ---------- Inicialização ----------
chrome.tabs.onActivated.addListener(() => { if (!$('#view-main').hidden && !FORCED_TAB) refreshTab(); });
chrome.tabs.onUpdated.addListener((id, info) => { if (id === S.tab?.id && info.status === 'complete' && !$('#view-main').hidden) refreshTab(); });

(async function init() {
  const st = await bg({ type: 'status' });
  if (st?.data?.apiBase) S.apiBase = st.data.apiBase;
  if (st?.data?.paired) showMain(); else showPair();
})();
