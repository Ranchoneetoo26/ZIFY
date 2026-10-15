// Painel lateral do Zyft (Copiloto): mostra e sugere; a pessoa decide.
// Todas as chamadas à API passam pelo service worker (background.js), que guarda os tokens.
// O painel nunca envia, salva ou avança formulários no portal (RN-01, ADR-0009).

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

// ---------- Comunicação com o service worker ----------
class ApiError extends Error {
  constructor(r) {
    super(problemText(r.problem) || `Erro ${r.status}`);
    this.status = r.status;
    this.problem = r.problem || {};
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
async function api(method, path, body) {
  const r = await bg({ type: 'api', method, path: `/api/v1${path}`, body });
  if (!r) throw new ApiError({ status: 0, problem: { title: 'Erro interno', detail: 'O serviço da extensão não respondeu.' } });
  if (r.paired === false) { showPair(); }
  if (!r.ok) throw new ApiError(r);
  return r.data;
}

// ---------- Estado ----------
let tab = null;            // aba ativa
let pageInfo = null;       // { url, origin, host, supported }
let items = [];            // campos lidos + sugestões da API
let lastFill = null;       // { ok, failed: [], pending: [] }
let authorizedCache = null;

// ?tabId=N abre o painel numa aba comum apontando para outra aba (testes manuais).
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
    h('p', { class: 'err-title' }, h('span', { 'aria-hidden': 'true' }, '⚠ '), 'Erro: ', err.message),
    consent ? h('p', { class: 'small' }, 'Este recurso depende de um consentimento. Ative em "Consentimentos" na plataforma web do Zyft e tente de novo.') : null,
    retry ? h('button', { type: 'button', onclick: () => { status(''); retry(); } }, 'Tentar de novo') : null,
  );
}
function busy(on, text) {
  for (const b of document.querySelectorAll('#btn-job, #btn-map')) b.disabled = on || !pageInfo?.supported;
  if (on) status(text, 'busy');
}

// ---------- Pareamento ----------
function defaultDeviceName() {
  const brands = navigator.userAgentData?.brands?.map((b) => b.brand) || [];
  const ua = navigator.userAgent;
  const browser = brands.find((b) => /Edge/i.test(b)) ? 'Edge' : /Edg\//.test(ua) ? 'Edge' : 'Chrome';
  const plat = navigator.userAgentData?.platform || navigator.platform || '';
  const os = /win/i.test(plat) ? 'Windows' : /mac/i.test(plat) ? 'macOS' : /cros/i.test(plat) ? 'ChromeOS' : /linux/i.test(plat) ? 'Linux' : plat || 'este computador';
  return `${browser} no ${os}`;
}

function showPair() {
  $('#view-pair').hidden = false;
  $('#view-main').hidden = true;
  if (!$('#pair-name').value) $('#pair-name').value = defaultDeviceName();
}
function showMain() {
  $('#view-pair').hidden = true;
  $('#view-main').hidden = false;
  refreshTab();
}

function setFieldError(input, errEl, msg) {
  errEl.textContent = msg || '';
  if (msg) input.setAttribute('aria-invalid', 'true');
  else input.removeAttribute('aria-invalid');
}

$('#pair-form').addEventListener('submit', async (e) => {
  e.preventDefault();
  const codeEl = $('#pair-code'), nameEl = $('#pair-name');
  const code = codeEl.value.replace(/\D/g, '');
  const deviceName = nameEl.value.trim();
  setFieldError(codeEl, $('#pair-code-err'), /^\d{6}$/.test(code) ? '' : 'Digite os 6 números do código.');
  setFieldError(nameEl, $('#pair-name-err'), deviceName ? '' : 'Dê um nome a este navegador (ex.: Chrome no notebook).');
  if (!/^\d{6}$/.test(code)) return codeEl.focus();
  if (!deviceName) return nameEl.focus();
  status('Conectando…', 'busy');
  const r = await bg({ type: 'pair', code, deviceName });
  if (!r?.ok) {
    const msg = r?.status === 0 ? problemText(r.problem) : (r?.problem?.detail || r?.problem?.title || 'Código inválido ou expirado. Gere um novo na plataforma.');
    setFieldError(codeEl, $('#pair-code-err'), msg);
    status('');
    return codeEl.focus();
  }
  codeEl.value = '';
  status('Extensão conectada à sua conta.', 'ok');
  showMain();
});

// ---------- Ajustes ----------
$('#btn-settings').addEventListener('click', async () => {
  const s = $('#settings');
  s.hidden = !s.hidden;
  $('#btn-settings').setAttribute('aria-expanded', String(!s.hidden));
  if (!s.hidden) {
    const st = await bg({ type: 'status' });
    $('#set-url').value = st?.data?.apiBase || 'http://localhost:5080';
    $('#set-url').focus();
  }
});
$('#settings-form').addEventListener('submit', (e) => {
  e.preventDefault();
  const input = $('#set-url');
  let url;
  try {
    url = new URL(input.value.trim() || 'http://localhost:5080');
    if (!/^https?:$/.test(url.protocol)) throw new Error();
  } catch {
    return setFieldError(input, $('#set-url-err'), 'Informe um endereço completo, como http://localhost:5080.');
  }
  setFieldError(input, $('#set-url-err'), '');
  const base = url.origin;
  const local = /^(localhost|127\.0\.0\.1)$/.test(url.hostname);
  // permissions.request precisa ser chamado direto do gesto do usuário (antes de qualquer await).
  const perm = local ? Promise.resolve(true) : chrome.permissions.request({ origins: [`${base}/*`] });
  perm.then(async (granted) => {
    if (!granted) return setFieldError(input, $('#set-url-err'), 'Sem permissão do navegador para esse endereço.');
    await bg({ type: 'setApiBase', url: base });
    status(`Endereço da API salvo: ${base}`, 'ok');
    authorizedCache = null;
  });
});
$('#btn-unpair').addEventListener('click', async () => {
  if (!confirm('Desconectar esta extensão da sua conta Zyft? Você poderá conectar de novo com um novo código.')) return;
  await bg({ type: 'unpair' });
  items = []; clear($('#out')); clear($('#complete'));
  status('Extensão desconectada.', 'ok');
  showPair();
});

// ---------- Página atual e sites autorizados (RF-10) ----------
async function authorizedOrigins() {
  if (authorizedCache) return authorizedCache;
  const data = await api('GET', '/authorized-sites');
  const list = Array.isArray(data) ? data : data?.items || data?.sites || [];
  authorizedCache = list.map((s) => (typeof s === 'string' ? s : s.origin)).filter(Boolean).map((o) => o.replace(/\/+$/, ''));
  return authorizedCache;
}

async function refreshTab() {
  if (FORCED_TAB) tab = await chrome.tabs.get(FORCED_TAB).catch(() => null);
  else [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  items = []; lastFill = null;
  clear($('#out')); clear($('#unsupported')); $('#unsupported').hidden = true;
  status('');
  const page = clear($('#page'));
  if (!tab?.url?.startsWith('http')) {
    pageInfo = { supported: false };
    page.append(h('p', { class: 'muted' }, 'Abra uma página de vaga ou um formulário de candidatura para começar.'));
    busy(false);
    renderJobCtx();
    return;
  }
  const u = new URL(tab.url);
  pageInfo = { url: tab.url, origin: u.origin, host: u.hostname, supported: false };
  page.append(
    h('p', { class: 'small muted' }, 'Página atual'),
    h('p', { class: 'page-title ellipsis', title: tab.url }, tab.title || u.hostname),
    h('p', { class: 'small muted ellipsis' }, u.host),
  );
  if (/^(localhost|127\.0\.0\.1)$/.test(u.hostname)) pageInfo.supported = true;
  else {
    try {
      const origins = await authorizedOrigins();
      pageInfo.supported = origins.includes(u.origin) && (await chrome.permissions.contains({ origins: [`${u.origin}/*`] }));
    } catch (err) {
      if (err.status !== 401) showError(err, refreshTab);
    }
  }
  if (!pageInfo.supported) renderUnsupported();
  busy(false);
  renderJobCtx();
  renderComplete(false);
}

function renderUnsupported() {
  const box = clear($('#unsupported'));
  box.hidden = false;
  const origin = pageInfo.origin;
  box.append(h('div', { class: 'card notice' },
    h('h2', {}, 'Site ainda não autorizado'),
    h('p', { class: 'small' }, `O Zyft só lê páginas que você autorizou. ${pageInfo.host} não é um portal suportado oficialmente no MVP 1, e a leitura genérica pode errar.`),
    h('button', {
      type: 'button',
      onclick: () => {
        // Pedido de permissão direto do clique (gesto do usuário).
        chrome.permissions.request({ origins: [`${origin}/*`] }).then(async (granted) => {
          if (!granted) return status('Permissão negada. O Zyft não vai ler esta página.', 'err');
          try { await api('POST', '/authorized-sites', { origin }); }
          catch (err) { if (err.status !== 409) return showError(err); }
          authorizedCache = null;
          status(`${pageInfo.host} autorizado.`, 'ok');
          refreshTab();
        });
      },
    }, 'Usar neste site mesmo assim'),
  ));
}

async function send(msg) {
  try { return await chrome.tabs.sendMessage(tab.id, msg); }
  catch {
    await chrome.scripting.executeScript({ target: { tabId: tab.id }, files: ['content.js'] });
    return chrome.tabs.sendMessage(tab.id, msg);
  }
}

// ---------- Análise de vaga (RF-17) ----------
const REQ = {
  atende: { icon: '✔', cls: 'ok', text: 'Aparece no seu perfil' },
  parcial: { icon: '◐', cls: 'warn', text: 'Aparece em parte' },
  naoencontrado: { icon: '✖', cls: 'muted', text: 'Não encontrado no perfil' },
  confirmar: { icon: '?', cls: 'warn', text: 'Confirme você' },
};
const reqKey = (s) => norm(s).replace(/[_\s-]/g, '');

$('#btn-job').addEventListener('click', analyzeJob);
async function analyzeJob() {
  busy(true, 'Lendo a vaga e comparando com o seu Perfil Mestre…');
  try {
    const page = await send({ cmd: 'job' });
    if (!page?.text?.trim()) { status('Não encontramos texto de vaga nesta página.'); return; }
    const r = await api('POST', '/jobs/analyze', { text: page.text });
    const job = {
      title: r?.job?.title || r?.title || page.title || '',
      company: r?.job?.company || r?.company || '',
      url: page.url,
    };
    await chrome.storage.session.set({ lastJob: job });
    renderJob(r, job);
    renderJobCtx();
    status('Análise pronta. Ela não é garantia de aprovação.', 'ok');
  } catch (err) { showError(err, analyzeJob); }
  finally { busy(false); }
}

function evidenceText(ev) {
  if (!ev) return '';
  if (typeof ev === 'string') return ev;
  const line = ev.line ?? ev.lineNumber;
  const t = ev.text ?? ev.content ?? '';
  return line != null ? `Linha ${line}: ${t}` : t;
}

function renderJob(r, job) {
  const reqs = r?.requirements || [];
  const met = reqs.filter((q) => reqKey(q.status) === 'atende').length;
  const info = [['Empresa', job.company], ['Local', r?.job?.location], ['Contrato', r?.job?.contractType], ['Modalidade', r?.job?.workModel]].filter(([, v]) => v);
  const out = clear($('#out'));
  out.append(
    h('section', { class: 'card', 'aria-labelledby': 'job-title' },
      h('h2', { id: 'job-title' }, job.title || 'Vaga'),
      info.map(([k, v]) => h('p', { class: 'kv' }, h('span', {}, k), String(v))),
      h('p', { class: 'small' }, `${met} de ${reqs.length} requisitos aparecem no seu perfil.`),
    ),
    h('section', { class: 'card', 'aria-labelledby': 'req-title' },
      h('h3', { id: 'req-title' }, 'Requisitos'),
      reqs.length ? h('ul', { class: 'reqs' }, reqs.map((q) => {
        const st = REQ[reqKey(q.status)] || REQ.naoencontrado;
        const ev = evidenceText(q.evidence) || (q.evidenceLine != null ? `Linha ${q.evidenceLine}` : '');
        return h('li', { class: 'req' },
          h('span', { class: `badge ${st.cls}`, 'aria-hidden': 'true' }, st.icon),
          h('div', {},
            h('p', {}, q.text || q.requirement || '', q.kind ? h('span', { class: 'tag' }, ` ${q.kind}`) : null),
            h('p', { class: `small state-${st.cls}` }, st.text, ev ? ` — evidência no perfil: “${ev}”` : ''),
          ));
      })) : h('p', { class: 'small muted' }, 'Nenhum requisito identificado.'),
    ),
    h('p', { class: 'small muted' }, r?.disclaimer || 'Análise baseada só no texto visível da vaga e no seu Perfil Mestre. Não é nota nem garantia de aprovação.'),
  );
}

async function renderJobCtx() {
  const box = clear($('#job-ctx'));
  const { lastJob } = await chrome.storage.session.get('lastJob');
  if (!lastJob) return;
  box.append(h('p', { class: 'ctx small' },
    'Vaga em contexto: ', h('b', {}, lastJob.title || '—'), lastJob.company ? ` · ${lastJob.company}` : '', ' ',
    h('button', { type: 'button', class: 'link', onclick: async () => { await chrome.storage.session.remove('lastJob'); renderJobCtx(); } }, 'limpar'),
  ));
}

// ---------- Ler formulário e revisão (RF-11 a RF-18) ----------
const STATE = {
  ready: { label: 'Pronto', icon: '✔', cls: 'ready' },
  ambiguous: { label: 'Escolha', icon: '⇄', cls: 'ambiguous' },
  missing: { label: 'Falta no perfil', icon: '○', cls: 'missing' },
  divergent: { label: 'Já preenchido no portal', icon: '≠', cls: 'divergent' },
  blocked: { label: 'Ação sua', icon: '🔒', cls: 'blocked' },
};
const AI_TEXT = { label: 'Sugestão — revise', icon: '✎', cls: 'ai' };
const SOURCE = { rule: 'regra do perfil', ai: 'IA apontou a chave do perfil', answer: 'resposta aprovada', none: '' };

$('#btn-map').addEventListener('click', readForm);
async function readForm() {
  busy(true, 'Lendo os campos da página…');
  clear($('#out'));
  try {
    const page = await send({ cmd: 'scan' });
    const fields = page?.fields || [];
    if (!fields.length) { status('Nenhum campo de formulário visível nesta página.'); return; }
    status(`Lendo ${fields.length} campos… comparando com o seu Perfil Mestre.`, 'busy');
    const payload = fields.map((f) => ({
      fieldId: f.fieldId, id: f.fieldId, tag: f.tag, type: f.type, name: f.name, htmlId: f.htmlId,
      label: f.label, placeholder: f.placeholder, section: f.section, required: !!f.required,
      maxLength: f.maxLength || 0, options: f.options || [], currentValue: f.currentValue || '',
    }));
    const r = await api('POST', '/forms/mappings', { pageUrl: page.url, fields: payload });
    const list = Array.isArray(r) ? r : r?.mappings || r?.suggestions || r?.items || [];
    const byId = new Map(list.map((m) => [String(m.fieldId ?? m.id), m]));
    items = fields.map((f) => toItem(f, byId.get(String(f.fieldId))));
    lastFill = null;
    renderReview();
    status('Revise antes de preencher. Nada é enviado.', 'ok');
  } catch (err) { showError(err, readForm); }
  finally { busy(false); }
}

function toItem(f, m) {
  m = m || {};
  // A API serializa enums como texto ("Ready"/"ready"); por segurança aceitamos também o número (ordem do enum).
  const STATES = ['ready', 'ambiguous', 'missing', 'blocked', 'divergent'];
  const SOURCES = ['rule', 'ai', 'answer', 'none'];
  let state = typeof m.state === 'number' ? STATES[m.state] : norm(m.state || 'missing').replace(/[_\s-]/g, '');
  if (!STATE[state]) state = 'missing';
  // Defesa extra: senha, anexo e captcha nunca são preenchíveis, mesmo se a API errar.
  if (f.type === 'file' || /captcha|c[oó]digo de seguran/i.test(`${f.label} ${f.name}`)) state = 'blocked';
  const source = typeof m.source === 'number' ? SOURCES[m.source] || 'none' : norm(m.source || 'none');
  const kind = String(m.kind || '');
  const item = {
    ...f,
    label: f.label || m.label || f.name || '(campo sem rótulo)',
    state, source,
    kind,
    profileKey: m.profileKey || '',
    profileLabel: m.profileLabel || '',
    value: m.value ?? '',
    profileValue: m.profileValue ?? m.value ?? '',
    note: m.note || m.reason || '',
    alternatives: (m.alternatives || []).map((a) => (typeof a === 'object' ? { value: a.value ?? '', text: a.label || a.text || a.profileKey || String(a.value ?? '') } : { value: a, text: String(a) })),
    usedFacts: m.usedFacts || [],
    aiText: false,
    // Só "pronto" pode vir marcado; a API pode desmarcar (ex.: sensível com consentimento sempre vem desmarcado).
    selected: state === 'ready' && m.selected !== false && kind !== 'sensitive',
    keepPortal: state === 'divergent',
  };
  // Para select/radio, alinha o valor à opção existente (por value ou por texto).
  if (item.options?.length && item.value !== '') item.value = matchOption(item.options, item.value) ?? item.value;
  if (state === 'blocked') item.selected = false;
  return item;
}
function matchOption(options, v) {
  const o = options.find((x) => String(x.value) === String(v)) || options.find((x) => norm(x.text) === norm(v));
  return o ? o.value : null;
}
const isText = (it) => it.type === 'textarea' || it.kind === 'openQuestion';
const fillable = (it) => it.state !== 'blocked';
const needsYou = (it) => !it.selected || it.state !== 'ready';

function renderReview() {
  const out = clear($('#out'));
  const n = items.filter((i) => i.selected && fillable(i)).length;
  const m = items.filter((i) => fillable(i) ? !i.selected : true).length;
  out.append(
    h('p', { class: 'summary', id: 'review-summary' }, `Vamos preencher ${n} campo${n === 1 ? '' : 's'}. ${m} precisa${m === 1 ? '' : 'm'} de você.`),
    h('ul', { class: 'fields', 'aria-label': 'Campos do formulário' }, items.map((it, i) => fieldCard(it, i))),
    h('div', { class: 'sticky' },
      h('button', { type: 'button', class: 'primary wide', id: 'btn-fill', onclick: fillSelected }, `Preencher ${n} campo${n === 1 ? '' : 's'} selecionado${n === 1 ? '' : 's'}`),
      h('p', { class: 'small muted' }, 'O Zyft nunca envia por você. Depois de preencher, confira no portal e envie você mesmo(a).'),
    ),
  );
}
function updateCounts() {
  const n = items.filter((i) => i.selected && fillable(i)).length;
  const m = items.filter((i) => fillable(i) ? !i.selected : true).length;
  const s = $('#review-summary');
  if (s) s.textContent = `Vamos preencher ${n} campo${n === 1 ? '' : 's'}. ${m} precisa${m === 1 ? '' : 'm'} de você.`;
  const b = $('#btn-fill');
  if (b) b.textContent = `Preencher ${n} campo${n === 1 ? '' : 's'} selecionado${n === 1 ? '' : 's'}`;
}

function stateBadge(it) {
  const st = it.aiText ? AI_TEXT : STATE[it.state];
  return h('span', { class: `tag st-${st.cls}` }, h('span', { 'aria-hidden': 'true' }, `${st.icon} `), st.label);
}

function blockedReason(it) {
  if (it.note) return it.note;
  if (it.type === 'file') return 'Anexo: escolha o arquivo direto no portal.';
  if (/captcha|c[oó]digo de seguran/i.test(`${it.label} ${it.name}`)) return 'Código de segurança: digite você mesmo(a) no portal.';
  if (it.type === 'checkbox') return 'Termos e declarações são sempre decididos por você.';
  return 'Este é um dado sensível ou uma decisão sua. Responda direto no formulário, se quiser.';
}

function fieldCard(it, i) {
  const st = it.aiText ? AI_TEXT : STATE[it.state];
  const ctlId = `fld-${i}`;
  const li = h('li', { class: `field st-${st.cls}` });
  const head = h('div', { class: 'fh' });
  if (fillable(it)) {
    head.append(h('input', {
      type: 'checkbox', id: `sel-${i}`, checked: it.selected,
      'aria-label': `Preencher o campo ${it.label}`,
      onchange: (e) => { it.selected = e.target.checked; if (it.state === 'divergent') { it.keepPortal = !it.selected; renderReview(); } else updateCounts(); },
    }));
  }
  head.append(
    h('button', { type: 'button', class: 'link lbl', title: 'Mostrar o campo na página', onclick: () => send({ cmd: 'highlight', fieldId: it.fieldId }).catch(() => {}) }, it.label),
    stateBadge(it),
  );
  li.append(head);
  if (SOURCE[it.source] && it.state !== 'blocked') li.append(h('p', { class: 'small muted' }, `Origem: ${SOURCE[it.source]}${it.profileLabel ? ` (${it.profileLabel})` : ''}`));

  if (it.state === 'blocked') {
    li.append(h('p', { class: 'small note' }, blockedReason(it)));
    return li;
  }

  if (it.state === 'divergent') {
    const name = nextId('div');
    li.append(h('fieldset', { class: 'diff' },
      h('legend', { class: 'small' }, 'O portal já tem um valor diferente do seu perfil'),
      h('p', { class: 'small' }, 'No portal: ', h('b', {}, it.currentValue || '—')),
      h('p', { class: 'small' }, 'No perfil: ', h('b', {}, optionText(it, it.profileValue) || '—')),
      h('label', { class: 'inline' }, h('input', { type: 'radio', name, checked: it.keepPortal, onchange: () => { it.keepPortal = true; it.selected = false; renderReview(); } }), 'Manter o do portal'),
      h('label', { class: 'inline' }, h('input', { type: 'radio', name, checked: !it.keepPortal, onchange: () => { it.keepPortal = false; it.selected = true; renderReview(); } }), 'Usar o do perfil'),
    ));
    if (it.keepPortal) return li;
  }

  li.append(h('label', { for: ctlId, class: 'small muted' }, it.state === 'missing' ? 'Valor (digite se quiser preencher)' : 'Valor a preencher'));
  li.append(control(it, ctlId));

  if (isText(it)) {
    li.append(h('div', { class: 'row' },
      h('button', { type: 'button', class: 'link', onclick: (e) => suggestText(it, e.target) }, h('span', { 'aria-hidden': 'true' }, '✎ '), 'Sugerir texto'),
      it.value ? h('button', { type: 'button', class: 'link', onclick: (e) => saveAnswer(it, e.target) }, h('span', { 'aria-hidden': 'true' }, '✓ '), 'Salvar como resposta aprovada') : null,
    ));
  }
  if (it.note) li.append(h('p', { class: 'small note' }, it.note));
  if (it.usedFacts?.length) {
    li.append(h('details', { class: 'small' }, h('summary', {}, 'Fatos aprovados usados'), h('ul', {}, it.usedFacts.map((f) => h('li', {}, typeof f === 'string' ? f : f.text || JSON.stringify(f))))));
  }
  return li;
}

function optionText(it, v) {
  const o = it.options?.find((x) => String(x.value) === String(v));
  return o ? o.text : v;
}

function onEdit(it, v) {
  it.value = v;
  // Editar um campo não-IA marca para preencher; texto sugerido pela IA exige marcação explícita.
  if (!it.aiText) {
    it.selected = v !== '' && v !== false;
    const cb = document.getElementById(`sel-${items.indexOf(it)}`);
    if (cb) cb.checked = it.selected;
  }
  updateCounts();
}

function control(it, id) {
  let opts = it.options?.length ? it.options : null;
  if (!opts && it.state === 'ambiguous' && it.alternatives.length) opts = it.alternatives;
  if (opts) {
    return h('select', { id, onchange: (e) => onEdit(it, e.target.value) },
      h('option', { value: '' }, '— não preencher —'),
      opts.filter((o) => o.value !== '' || o.text).map((o) => h('option', { value: o.value, selected: String(o.value) === String(it.value) }, o.text || String(o.value))));
  }
  if (it.type === 'checkbox') {
    return h('label', { class: 'inline' }, h('input', { id, type: 'checkbox', checked: it.value === true || it.value === 'true', onchange: (e) => onEdit(it, e.target.checked) }), 'Marcar');
  }
  if (isText(it) || String(it.value || '').length > 80) {
    return h('textarea', { id, rows: it.aiText ? 6 : 3, maxlength: it.maxLength || null, oninput: (e) => onEdit(it, e.target.value) }, String(it.value ?? ''));
  }
  return h('input', { id, value: String(it.value ?? ''), type: ['email', 'tel', 'url', 'date', 'number'].includes(it.type) ? it.type : 'text', oninput: (e) => onEdit(it, e.target.value) });
}

async function suggestText(it, btn) {
  btn.disabled = true;
  status('Pedindo uma sugestão com base só nos fatos aprovados do seu perfil…', 'busy');
  try {
    const r = await api('POST', '/forms/open-answers', { question: it.label, maxLength: it.maxLength > 0 ? it.maxLength : null });
    const text = r?.text ?? r?.value ?? '';
    if (!text) { status(r?.note || 'Não há fatos aprovados suficientes para sugerir um texto. Escreva você mesmo(a).'); return; }
    Object.assign(it, { value: text, aiText: true, selected: false, note: r?.note || 'Sugestão a partir dos seus fatos aprovados. Revise, edite e marque para preencher.', usedFacts: r?.usedFacts || [] });
    renderReview();
    status('Sugestão pronta. Revise e marque o campo para preencher.', 'ok');
  } catch (err) { showError(err); }
  finally { btn.disabled = false; }
}

async function saveAnswer(it, btn) {
  if (!String(it.value || '').trim()) return;
  btn.disabled = true;
  try {
    await api('POST', '/answers', { question: it.label, text: String(it.value) });
    status('Resposta salva nas suas respostas aprovadas.', 'ok');
  } catch (err) { showError(err); btn.disabled = false; }
}

// ---------- Preencher só o aprovado (RF-14) ----------
async function fillSelected() {
  const chosen = items.filter((i) => i.selected && fillable(i) && i.value !== '' && i.value != null);
  if (!chosen.length) return status('Nenhum campo selecionado para preencher.');
  status(`Preenchendo ${chosen.length} campos…`, 'busy');
  try {
    const { report } = await send({ cmd: 'fill', items: chosen.map((i) => ({ fieldId: i.fieldId, value: i.value })) });
    const ok = report.filter((r) => r.ok);
    const failed = report.filter((r) => !r.ok).map((r) => ({ ...r, label: items.find((i) => i.fieldId === r.fieldId)?.label || r.fieldId }));
    const pending = items.filter((i) => !chosen.includes(i));
    lastFill = { ok: ok.length, failed, pending };
    renderSummary();
    renderComplete(false);
    status(`${ok.length} campo(s) preenchido(s)${failed.length ? `, ${failed.length} com falha` : ''}.`, failed.length ? 'err' : 'ok');
  } catch (err) { showError(err, fillSelected); }
}

function renderSummary() {
  const out = clear($('#out'));
  const { ok, failed, pending } = lastFill;
  out.append(h('section', { class: 'card', 'aria-labelledby': 'sum-title' },
    h('h2', { id: 'sum-title' }, 'Resumo do preenchimento'),
    h('p', {}, h('span', { class: 'tag st-ready' }, '✔ ', `${ok} preenchido${ok === 1 ? '' : 's'}`)),
    failed.length ? h('div', {},
      h('p', { class: 'state-err' }, `${failed.length} não puderam ser preenchidos:`),
      h('ul', { class: 'small' }, failed.map((f) => h('li', {}, `${f.label}: ${f.error}`)))) : null,
    pending.length ? h('div', {},
      h('p', {}, `${pending.length} ficaram com você:`),
      h('ul', { class: 'small' }, pending.map((p) => h('li', {}, `${p.label} (${(p.aiText ? AI_TEXT : STATE[p.state]).label})`)))) : null,
    h('p', { class: 'reminder' }, 'Confira no portal e envie quando estiver pronto(a). O Zyft nunca envia por você.'),
    h('div', { class: 'row' },
      h('button', { type: 'button', onclick: renderReview }, 'Voltar à revisão'),
      h('button', { type: 'button', onclick: readForm }, 'Ler novamente (nova etapa)')),
  ));
}

// ---------- Conclusão (RF-19, ADR-0012) ----------
async function renderComplete(fromSubmit) {
  const box = clear($('#complete'));
  if (!pageInfo?.supported) return;
  const { lastJob } = await chrome.storage.session.get('lastJob');
  const card = h('section', { class: `card ${fromSubmit ? 'pulse' : ''}`, 'aria-labelledby': 'cmp-title' },
    h('h2', { id: 'cmp-title' }, fromSubmit ? 'Você enviou o formulário?' : 'Depois de enviar no portal'),
    h('p', { class: 'small muted' }, 'Registre a conclusão só depois de enviar você mesmo(a) no portal. O registro não guarda os valores dos campos.'),
  );
  const openBtn = h('button', { type: 'button', class: fromSubmit ? 'primary wide' : 'wide', 'aria-expanded': 'false' }, 'Concluí a candidatura');
  const form = h('form', { novalidate: true, hidden: true });
  const fId = nextId('c');
  const fields = {
    portal: h('input', { id: `${fId}-portal`, value: pageInfo.host || '' }),
    jobTitle: h('input', { id: `${fId}-title`, value: lastJob?.title || tab?.title || '', required: true, 'aria-describedby': `${fId}-title-err` }),
    company: h('input', { id: `${fId}-company`, value: lastJob?.company || '' }),
    fieldsFilled: h('input', { id: `${fId}-n`, type: 'number', min: '0', value: String(lastFill?.ok ?? 0) }),
  };
  const titleErr = h('p', { id: `${fId}-title-err`, class: 'field-err', role: 'alert' });
  form.append(
    h('label', { for: fields.portal.id }, 'Portal'), fields.portal,
    h('label', { for: fields.jobTitle.id }, 'Cargo da vaga'), fields.jobTitle, titleErr,
    h('label', { for: fields.company.id }, 'Empresa (opcional)'), fields.company,
    h('label', { for: fields.fieldsFilled.id }, 'Campos preenchidos pelo Zyft'), fields.fieldsFilled,
    h('button', { type: 'submit', class: 'primary wide' }, 'Confirmo que enviei — registrar'),
  );
  openBtn.addEventListener('click', () => {
    form.hidden = !form.hidden;
    openBtn.setAttribute('aria-expanded', String(!form.hidden));
    if (!form.hidden) fields.jobTitle.focus();
  });
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const jobTitle = fields.jobTitle.value.trim();
    setFieldError(fields.jobTitle, titleErr, jobTitle ? '' : 'Informe o cargo da vaga.');
    if (!jobTitle) return fields.jobTitle.focus();
    const n = Math.max(0, parseInt(fields.fieldsFilled.value, 10) || 0);
    try {
      await api('POST', '/forms/completions', { portal: fields.portal.value.trim(), jobTitle, company: fields.company.value.trim(), fieldsFilled: n });
      clear(box).append(h('section', { class: 'card ok-card', role: 'status' },
        h('h2', {}, h('span', { 'aria-hidden': 'true' }, '✔ '), 'Candidatura concluída'),
        h('p', { class: 'small' }, `“${jobTitle}” foi registrada na sua conta. Você pode acompanhar em Candidaturas, na plataforma web.`)));
      status('Conclusão registrada.', 'ok');
    } catch (err) { showError(err); }
  });
  card.append(openBtn, form);
  box.append(card);
  if (fromSubmit) openBtn.focus();
}

chrome.runtime.onMessage.addListener((msg, sender) => {
  if (sender.id !== chrome.runtime.id) return;
  if (msg?.type === 'submitted' && sender.tab?.id === tab?.id) renderComplete(true);
});

// ---------- Inicialização ----------
chrome.tabs.onActivated.addListener(() => { if (!$('#view-main').hidden) refreshTab(); });
chrome.tabs.onUpdated.addListener((id, info) => { if (id === tab?.id && info.status === 'complete' && !$('#view-main').hidden) refreshTab(); });

(async function init() {
  const st = await bg({ type: 'status' });
  if (st?.data?.paired) showMain(); else showPair();
})();
