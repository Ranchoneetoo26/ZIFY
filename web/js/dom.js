// Utilitários de DOM. Tudo é criado com createElement/textContent: nenhum dado da API vira HTML.
// Estilos inline vão por CSSOM (style: {…}), compatível com a CSP sem 'unsafe-inline'.
import { fieldLabel } from './api.js';

export const $ = (s, el = document) => el.querySelector(s);
export const $$ = (s, el = document) => [...el.querySelectorAll(s)];

const SVG_NS = 'http://www.w3.org/2000/svg';
const SVG_TAGS = new Set(['svg', 'path', 'circle', 'rect', 'g', 'line', 'polyline']);

/** h('button', { class: 'zy-btn', onclick }, 'Salvar') */
export function h(tag, attrs = {}, ...children) {
  const isSvg = SVG_TAGS.has(tag);
  const el = isSvg ? document.createElementNS(SVG_NS, tag) : document.createElement(tag);
  for (const [k, v] of Object.entries(attrs || {})) {
    if (v === undefined || v === null || v === false) continue;
    if (k === 'class') { if (isSvg) el.setAttribute('class', v); else el.className = v; }
    else if (k === 'style' && typeof v === 'object') Object.assign(el.style, v);
    else if (k === 'dataset') Object.assign(el.dataset, v);
    else if (k.startsWith('on') && typeof v === 'function') el.addEventListener(k.slice(2).toLowerCase(), v);
    else if (k === 'value' && 'value' in el && !isSvg) el.value = v;
    else if (!isSvg && (k === 'checked' || k === 'selected' || k === 'disabled' || k === 'hidden' || k === 'required' || k === 'multiple')) el[k] = Boolean(v);
    else el.setAttribute(k, v === true ? (k.startsWith('aria-') ? 'true' : '') : String(v));
  }
  append(el, children);
  return el;
}

export function append(el, children) {
  for (const c of [children].flat(Infinity)) {
    if (c === null || c === undefined || c === false || c === '') continue;
    el.append(c instanceof Node ? c : document.createTextNode(String(c)));
  }
  return el;
}

export function clear(el, ...children) {
  el.replaceChildren();
  append(el, children);
  return el;
}

let uid = 0;
export const nextId = (p = 'f') => `${p}-${++uid}`;

/** Mensagem para leitores de tela (aria-live polite). */
export function announce(msg) {
  const live = $('#live');
  if (!live) return;
  live.textContent = '';
  setTimeout(() => { live.textContent = msg; }, 50);
}

export function toast(msg, kind = '') {
  const t = $('#toast');
  if (!t) return;
  t.textContent = msg;
  t.className = `show ${kind}`;
  clearTimeout(t._h);
  t._h = setTimeout(() => { t.className = ''; }, 4500);
}

const pad = (n) => String(n).padStart(2, '0');
export const fmtDate = (d) => (d ? new Date(d).toLocaleDateString('pt-BR') : '');
export const fmtDay = (d) => { if (!d) return ''; const x = new Date(d); return `${pad(x.getDate())}/${pad(x.getMonth() + 1)}`; };
export const fmtTime = (d) => (d ? new Date(d).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }) : '');
export const fmtDateTime = (d) => (d ? new Date(d).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' }) : '');
const MONTHS = ['jan', 'fev', 'mar', 'abr', 'mai', 'jun', 'jul', 'ago', 'set', 'out', 'nov', 'dez'];
/** "2021-02-01" → "fev/2021" */
export const fmtMonth = (d) => {
  if (!d) return '';
  const m = String(d).match(/^(\d{4})-(\d{2})/);
  return m ? `${MONTHS[Number(m[2]) - 1]}/${m[1]}` : String(d);
};
export const fmtMoney = (n) => (n === null || n === undefined || n === '' ? '' : Number(n).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }));
export const plural = (n, one, many) => `${n} ${n === 1 ? one : many}`;

export const loading = (text = 'Carregando…') =>
  h('div', { class: 'loading', role: 'status', 'aria-busy': 'true' }, h('span', { class: 'spinner', 'aria-hidden': 'true' }), text);

/**
 * Campo de formulário no padrão TextField do design system (zy-tf): label associado,
 * dica e espaço para erro ligados por aria-describedby.
 * opts: { name, label, type, value, required, optional, hint, options:[[value,text]], rows, attrs, wide, placeholder }
 */
export function field(opts) {
  const id = opts.id || nextId(String(opts.name).replace(/\W/g, '-'));
  const hintId = `${id}-hint`;
  const errId = `${id}-err`;
  const describedBy = [opts.hint ? hintId : null, errId].filter(Boolean).join(' ');
  const common = { id, name: opts.name, required: opts.required, 'aria-describedby': describedBy, placeholder: opts.placeholder, ...(opts.attrs || {}) };
  let control;

  if (opts.type === 'checkbox') {
    control = h('input', { ...common, type: 'checkbox', checked: Boolean(opts.value) });
    return h('div', { class: `zy-tf zy-tf--check ${opts.wide ? 'wide' : ''}` },
      h('label', { class: 'check', for: id }, control, h('span', {}, opts.label)),
      opts.hint ? h('p', { class: 'zy-tf__hint', id: hintId }, opts.hint) : null,
      h('p', { class: 'zy-tf__error', id: errId, hidden: true }));
  }
  if (opts.type === 'textarea') {
    control = h('textarea', { ...common, class: 'zy-tf__input', rows: opts.rows || 4 });
    control.value = opts.value ?? '';
  } else if (opts.type === 'select') {
    control = h('select', { ...common, class: 'zy-tf__input' }, (opts.options || []).map(([v, t]) => h('option', { value: v }, t)));
    const v = opts.value ?? '';
    if (v && ![...control.options].some((o) => o.value === String(v))) control.append(h('option', { value: v }, v));
    control.value = v;
    // Select sem opção vazia nunca abre em branco: vale a primeira opção válida (ex.: Tipo = Curso).
    if (control.selectedIndex < 0 && control.options.length) control.selectedIndex = 0;
  } else {
    control = h('input', { ...common, class: 'zy-tf__input', type: opts.type || 'text', value: opts.value ?? '' });
  }

  return h('div', { class: `zy-tf ${opts.wide ? 'wide' : ''}` },
    h('label', { class: 'zy-tf__label', for: id }, opts.label,
      opts.optional ? h('span', { class: 'zy-tf__opt' }, ' (opcional)') : null),
    control,
    opts.hint ? h('p', { class: 'zy-tf__hint', id: hintId }, opts.hint) : null,
    h('p', { class: 'zy-tf__error', id: errId, hidden: true }));
}

/** Lê os controles nomeados de um form. Checkbox → boolean; vazio → null. */
export function readForm(form) {
  const out = {};
  for (const el of form.elements) {
    if (!el.name || el.disabled) continue;
    if (el.type === 'checkbox') out[el.name] = el.checked;
    else if (el.type === 'radio') { if (el.checked) out[el.name] = el.value; }
    else if (el.type === 'file') out[el.name] = el.files;
    else out[el.name] = el.value.trim() === '' ? null : el.value.trim();
  }
  return out;
}

export function clearErrors(form) {
  for (const el of $$('[aria-invalid="true"]', form)) el.removeAttribute('aria-invalid');
  for (const e of $$('.zy-tf__error', form)) { e.textContent = ''; e.hidden = true; }
  for (const e of $$('.zy-tf.has-error', form)) e.classList.remove('has-error');
  $('.form-error', form)?.remove();
}

/**
 * Mostra erros por campo junto ao input e, para o que não casar com nenhum campo,
 * um resumo no topo do formulário. `errors` = { campo: [mensagens] } ou um ApiError.
 */
export function showErrors(form, errOrMap, fallback) {
  clearErrors(form);
  const map = errOrMap?.errors ?? errOrMap ?? {};
  const general = [];
  let first = null;
  for (const [key, msgs] of Object.entries(map)) {
    const el = findControl(form, key);
    // Sem controle na tela: o resumo diz QUAL campo, com o nome legível (nunca só "valor inválido").
    if (!el) { general.push(`${fieldLabel(key)}: ${[].concat(msgs).join(' ')}`); continue; }
    el.setAttribute('aria-invalid', 'true');
    el.closest('.zy-tf')?.classList.add('has-error');
    const errId = (el.getAttribute('aria-describedby') || '').split(' ').find((x) => x.endsWith('-err'));
    const slot = errId && document.getElementById(errId);
    if (slot) { slot.replaceChildren(document.createTextNode(msgs.join(' '))); slot.hidden = false; }
    first ||= el;
  }
  const message = errOrMap instanceof Error ? errOrMap.message : fallback;
  if (general.length || (!first && message)) {
    const box = h('div', { class: 'notice notice--error form-error', role: 'alert', tabindex: '-1' },
      general.length ? general.join(' ') : message);
    form.prepend(box);
    if (!first) box.focus();
  }
  if (first) first.focus();
  announce(first ? 'Há campos para conferir. Corrija e tente de novo.' : (message || 'Não foi possível concluir.'));
}

function findControl(form, key) {
  const k = key.toLowerCase();
  const last = k.split('.').pop().replace(/\[\d+\]/g, '');
  const els = [...form.elements].filter((e) => e.name);
  return els.find((e) => e.name.toLowerCase() === k) || els.find((e) => e.name.toLowerCase() === last) || null;
}

/**
 * Diálogo modal acessível (<dialog>). `body` pode ser Node ou texto; `actions` é uma lista de
 * botões já montados. Devolve { dlg, close }. Esc e "Fechar" chamam onClose.
 */
export function modal({ title, body, actions = [], onClose, wide = false, label }) {
  const titleId = nextId('dlg');
  const closeBtn = h('button', { type: 'button', class: 'zy-btn zy-btn--ghost zy-btn--sm modal__x', 'aria-label': 'Fechar' }, '✕');
  const dlg = h('dialog', { class: `modal ${wide ? 'modal--wide' : ''}`, 'aria-labelledby': titleId },
    h('div', { class: 'modal__head' }, h('h2', { id: titleId, class: 'modal__title' }, title), closeBtn),
    h('div', { class: 'modal__body' }, body instanceof Node ? body : h('p', {}, body)),
    actions.length ? h('div', { class: 'modal__actions' }, actions) : null);
  if (label) dlg.setAttribute('aria-label', label);
  const opener = document.activeElement;
  let closed = false;
  const close = (v) => {
    if (closed) return;
    closed = true;
    dlg.close();
    dlg.remove();
    if (opener && opener.isConnected) opener.focus();
    onClose?.(v);
  };
  closeBtn.addEventListener('click', () => close(false));
  dlg.addEventListener('cancel', (e) => { e.preventDefault(); close(false); });
  document.body.append(dlg);
  dlg.showModal();
  setTimeout(() => (dlg.querySelector('.modal__body input, .modal__body select, .modal__body textarea') || dlg.querySelector('.modal__actions button') || closeBtn).focus());
  return { dlg, close };
}

/** Confirmação simples. Resolve true/false. */
export function confirmDialog({ title, message, confirmText = 'Confirmar', danger = false, typeToConfirm = null }) {
  return new Promise((resolve) => {
    let input = null;
    const body = h('div', { class: 'stack' }, message instanceof Node ? message : h('p', {}, message));
    if (typeToConfirm) {
      const f = field({ name: 'confirm', label: `Para confirmar, digite ${typeToConfirm}`, attrs: { autocomplete: 'off' } });
      input = f.querySelector('input');
      body.append(f);
    }
    const ok = h('button', { type: 'button', class: `zy-btn zy-btn--md ${danger ? 'zy-btn--danger' : 'zy-btn--primary'}`, disabled: Boolean(typeToConfirm) }, confirmText);
    const cancel = h('button', { type: 'button', class: 'zy-btn zy-btn--md zy-btn--quiet' }, 'Cancelar');
    let result = false;
    const m = modal({ title, body, actions: [ok, cancel], onClose: () => resolve(result) });
    if (input) {
      input.addEventListener('input', () => { ok.disabled = input.value.trim() !== typeToConfirm; });
      input.addEventListener('keydown', (e) => { if (e.key === 'Enter' && !ok.disabled) { result = true; m.close(true); } });
    }
    ok.addEventListener('click', () => { result = true; m.close(true); });
    cancel.addEventListener('click', () => m.close(false));
  });
}

/** Mensagem de erro de carregamento com ação de recuperação. */
export function errorBox(err, retry) {
  return h('div', { class: 'notice notice--error', role: 'alert' },
    h('p', {}, h('strong', {}, err.title || 'Algo não carregou.'), ' ', err.message || ''),
    retry ? h('button', { type: 'button', class: 'zy-btn zy-btn--quiet zy-btn--sm', onclick: retry }, 'Tentar de novo') : null);
}

/** Navega para uma rota (hash). Se já estiver nela, força o recarregamento. */
export function go(hash) {
  if (location.hash === hash) window.dispatchEvent(new HashChangeEvent('hashchange'));
  else location.hash = hash;
}

/** Garante http(s) e devolve '#'' para qualquer outro esquema. */
export const safeUrl = (u) => (/^https?:\/\//i.test(String(u || '')) ? String(u) : '#');
