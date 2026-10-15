// Utilitários de DOM. Tudo é criado com createElement/textContent: nenhum dado da API vira HTML.

export const $ = (s, el = document) => el.querySelector(s);
export const $$ = (s, el = document) => [...el.querySelectorAll(s)];

/** h('button', { class: 'primary', onclick }, 'Salvar') */
export function h(tag, attrs = {}, ...children) {
  const el = document.createElement(tag);
  for (const [k, v] of Object.entries(attrs || {})) {
    if (v === undefined || v === null || v === false) continue;
    if (k === 'class') el.className = v;
    else if (k === 'dataset') Object.assign(el.dataset, v);
    else if (k.startsWith('on') && typeof v === 'function') el.addEventListener(k.slice(2), v);
    else if (k === 'value' && 'value' in el) el.value = v;
    else if (k === 'checked' || k === 'selected' || k === 'disabled' || k === 'hidden' || k === 'required' || k === 'multiple') el[k] = Boolean(v);
    else el.setAttribute(k, v === true ? '' : String(v));
  }
  append(el, children);
  return el;
}

function append(el, children) {
  for (const c of children.flat(Infinity)) {
    if (c === null || c === undefined || c === false) continue;
    el.append(c instanceof Node ? c : document.createTextNode(String(c)));
  }
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
  t.textContent = msg;
  t.className = `show ${kind}`;
  clearTimeout(t._h);
  t._h = setTimeout(() => { t.className = ''; }, 4000);
}

export const fmtDate = (d) => (d ? new Date(d).toLocaleDateString('pt-BR') : '');
export const fmtDateTime = (d) => (d ? new Date(d).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' }) : '');
/** "2021-02-01" → "02/2021" */
export const fmtMonth = (d) => {
  if (!d) return '';
  const m = String(d).match(/^(\d{4})-(\d{2})/);
  return m ? `${m[2]}/${m[1]}` : String(d);
};

export const loading = (text = 'Carregando…') =>
  h('div', { class: 'loading', role: 'status', 'aria-busy': 'true' }, h('span', { class: 'spinner', 'aria-hidden': 'true' }), text);

export function badge(text, kind = 'neutral', symbol = '') {
  return h('span', { class: `badge ${kind}` }, symbol ? h('span', { 'aria-hidden': 'true' }, symbol) : null, text);
}

/**
 * Campo de formulário acessível: label associado, dica e espaço para erro ligados por aria-describedby.
 * opts: { name, label, type, value, required, optional, hint, options:[[value,text]], rows, attrs, wide }
 */
export function field(opts) {
  const id = nextId(opts.name.replace(/\W/g, '-'));
  const hintId = `${id}-hint`;
  const errId = `${id}-err`;
  const describedBy = [opts.hint ? hintId : null, errId].filter(Boolean).join(' ');
  let control;
  const common = { id, name: opts.name, required: opts.required, 'aria-describedby': describedBy, ...(opts.attrs || {}) };

  if (opts.type === 'textarea') {
    control = h('textarea', { ...common, rows: opts.rows || 3 });
    control.value = opts.value ?? '';
  } else if (opts.type === 'select') {
    control = h('select', common, (opts.options || []).map(([v, t]) => h('option', { value: v }, t)));
    const v = opts.value ?? '';
    if (v && ![...control.options].some((o) => o.value === String(v))) control.append(h('option', { value: v }, v));
    control.value = v;
  } else if (opts.type === 'checkbox') {
    control = h('input', { ...common, type: 'checkbox', checked: Boolean(opts.value) });
    return h('div', { class: `field ${opts.wide ? 'wide' : ''}` },
      h('label', { class: 'check', for: id }, control, h('span', { class: 'label' }, opts.label)),
      opts.hint ? h('span', { class: 'hint', id: hintId }, opts.hint) : null,
      h('span', { class: 'field-error', id: errId, hidden: true }));
  } else {
    control = h('input', { ...common, type: opts.type || 'text', value: opts.value ?? '' });
  }

  return h('div', { class: `field ${opts.wide ? 'wide' : ''}` },
    h('label', { for: id }, opts.label, opts.required ? h('span', { class: 'sr-only' }, ' (obrigatório)') : null,
      opts.optional ? h('span', { class: 'optional' }, ' (opcional)') : null),
    opts.hint ? h('span', { class: 'hint', id: hintId }, opts.hint) : null,
    control,
    h('span', { class: 'field-error', id: errId, hidden: true }));
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
  for (const e of $$('.field-error', form)) { e.textContent = ''; e.hidden = true; }
  const box = $('.form-error', form);
  if (box) box.remove();
}

/**
 * Mostra erros por campo junto ao input e, para o que não casar com nenhum campo,
 * um resumo no topo do formulário. `errors` = { campo: [mensagens] }.
 * Também aceita um ApiError (usa err.errors e err.message).
 */
export function showErrors(form, errOrMap, fallback) {
  clearErrors(form);
  const map = errOrMap?.errors ?? errOrMap ?? {};
  const general = [];
  let first = null;
  for (const [key, msgs] of Object.entries(map)) {
    const el = findControl(form, key);
    if (!el) { general.push(...msgs); continue; }
    el.setAttribute('aria-invalid', 'true');
    const errId = (el.getAttribute('aria-describedby') || '').split(' ').find((x) => x.endsWith('-err'));
    const slot = errId && document.getElementById(errId);
    if (slot) { slot.textContent = msgs.join(' '); slot.hidden = false; }
    first ||= el;
  }
  const message = errOrMap instanceof Error ? errOrMap.message : fallback;
  if (general.length || (!first && message)) {
    const box = h('div', { class: 'card callout danger form-error', role: 'alert', tabindex: '-1' },
      h('strong', {}, errOrMap?.title || 'Não foi possível salvar.'), ' ',
      general.length ? general.join(' ') : message);
    form.prepend(box);
    if (!first) box.focus();
  }
  if (first) first.focus();
  announce(first ? 'Há campos com erro. Corrija e tente de novo.' : (message || 'Erro.'));
}

function findControl(form, key) {
  const k = key.toLowerCase();
  const last = k.split('.').pop().replace(/\[\d+\]/g, '');
  const els = [...form.elements].filter((e) => e.name);
  return els.find((e) => e.name.toLowerCase() === k) || els.find((e) => e.name.toLowerCase() === last) || null;
}

/** Diálogo modal acessível (elemento <dialog>). Resolve true/false. */
export function confirmDialog({ title, message, confirmText = 'Confirmar', danger = false, typeToConfirm = null }) {
  return new Promise((resolve) => {
    const inputId = nextId('confirm');
    const input = typeToConfirm ? h('input', { id: inputId, autocomplete: 'off', 'aria-describedby': `${inputId}-hint` }) : null;
    const ok = h('button', { class: danger ? 'danger solid' : 'primary', type: 'submit', disabled: Boolean(typeToConfirm) }, confirmText);
    const cancel = h('button', { type: 'button' }, 'Cancelar');
    const titleId = nextId('dlg');
    const dlg = h('dialog', { 'aria-labelledby': titleId, class: 'card' },
      h('form', { method: 'dialog' },
        h('h2', { id: titleId }, title),
        message instanceof Node ? message : h('p', {}, message),
        typeToConfirm ? h('div', { class: 'field' },
          h('label', { for: inputId }, `Digite ${typeToConfirm} para confirmar`),
          h('span', { class: 'hint', id: `${inputId}-hint` }, 'Isto evita exclusões por engano.'),
          input) : null,
        h('div', { class: 'form-actions' }, ok, cancel)));
    if (input) input.addEventListener('input', () => { ok.disabled = input.value.trim() !== typeToConfirm; });
    const done = (v) => { dlg.close(); dlg.remove(); resolve(v); };
    cancel.addEventListener('click', () => done(false));
    dlg.addEventListener('cancel', (e) => { e.preventDefault(); done(false); });
    dlg.querySelector('form').addEventListener('submit', (e) => { e.preventDefault(); if (!ok.disabled) done(true); });
    document.body.append(dlg);
    dlg.showModal();
    (input || cancel).focus();
  });
}

/** Mensagem de erro de carregamento com ação de recuperação. */
export function errorBox(err, retry) {
  return h('div', { class: 'card callout danger', role: 'alert' },
    h('p', {}, h('strong', {}, err.title || 'Algo deu errado.'), ' ', err.message),
    retry ? h('button', { type: 'button', onclick: retry }, 'Tentar de novo') : null);
}

/** Navega para uma rota (hash). Se já estiver nela, força o recarregamento. */
export function go(hash) {
  if (location.hash === hash) window.dispatchEvent(new HashChangeEvent('hashchange'));
  else location.hash = hash;
}
