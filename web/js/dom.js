// Utilitários de DOM. Tudo é criado com createElement/textContent: nenhum dado da API vira HTML.
// Estilos inline vão por CSSOM (style: {…}), compatível com a CSP sem 'unsafe-inline'.
import { ApiError, fieldLabel } from './api.js';

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

import { CNH_CATEGORIES, ufFromName } from './validate.js';

const onlyDigits = (value) => String(value).replace(/\D/g, '');
// DDD nunca começa com zero: o 0 de tronco ("044 …", "055 …") sai do número.
const phoneDigits = (value) => onlyDigits(value).replace(/^0+/, '');

const MASKS = {
  phone: (value) => {
    const digits = phoneDigits(value).slice(0, 11);
    if (!digits) return '';
    if (digits.length <= 2) return `(${digits}`;
    const split = digits.length > 10 ? 7 : 6;
    return digits.length <= split
      ? `(${digits.slice(0, 2)}) ${digits.slice(2)}`
      : `(${digits.slice(0, 2)}) ${digits.slice(2, split)}-${digits.slice(split)}`;
  },
  cep: (value) => {
    const digits = onlyDigits(value).slice(0, 8);
    return digits.length > 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits;
  },
  uf: (value) => String(value).replace(/[^a-z]/gi, '').slice(0, 2).toUpperCase(),
  // Cada letra só entra se o resultado for uma categoria: BB, EE e CC ficam em B, E e C.
  cnh: (value) => {
    let out = '';
    for (const ch of String(value).replace(/[^a-e]/gi, '').toUpperCase()) if (CNH_CATEGORIES.includes(out + ch)) out += ch;
    return out;
  },
};

// Texto que chega inteiro (colado ou preenchido pelo navegador). `free` = não cabe na máscara sem perder
// dado: o campo fica sem formatar, com o limite da API, e quem salva valida.
const PASTED = {
  phone: (raw) => (phoneDigits(raw).length > 11
    ? { value: raw.trim().replace(/^0+/, ''), free: true }
    : { value: MASKS.phone(raw), free: false }),
  cep: (raw) => (onlyDigits(raw).length > 8 || /[a-z]/i.test(raw) ? { value: raw, free: true } : { value: MASKS.cep(raw), free: false }), // letras: código postal de fora
  uf: (raw) => {
    const text = raw.trim();
    if (/^[a-z]{0,2}$/i.test(text)) return { value: text.toUpperCase(), free: false };
    const code = ufFromName(text);
    return code ? { value: code, free: false } : { value: raw, free: true };
  },
  cnh: (raw) => {
    const words = raw.toUpperCase().split(/[^A-Z]+/).filter(Boolean);
    if (!words.length) return { value: '', free: false };
    const hit = words.find((w) => CNH_CATEGORIES.includes(w));
    return hit ? { value: hit, free: false } : { value: raw, free: true };
  },
};

// Tamanho do texto no formato da máscara.
const MASK_LIMITS = { phone: 15, cep: 9, uf: 2, cnh: 3 };
// Limites de UpdateProfileValidator: valem para o que a máscara não cobre (dado antigo, telefone internacional).
const API_LIMITS = { phone: 40, cep: 20, uf: 60, cnh: 5 };
// Limite solto durante a inserção, para o texto colado chegar inteiro à máscara.
const PASTE_LIMITS = { phone: 40, cep: 40, uf: 60, cnh: 40 };
const SIGNIFICANT = /[0-9a-z]/i;

const maskFor = (name, explicit) => {
  if (explicit) return explicit;
  if (name === 'phone' || /phone|telefone|cel/i.test(name)) return 'phone';
  if (name === 'zipCode' || /cep|postal/i.test(name)) return 'cep';
  if (name === 'state' || /uf/i.test(name)) return 'uf';
  if (name === 'driverLicenseCategory' || /cnh/i.test(name)) return 'cnh';
  return null;
};

/**
 * Liga a máscara ao campo. Nunca altera o valor recebido: um valor salvo fora do formato da máscara
 * é válido na API, então o campo fica sem máscara e com o limite da API, para ser editado livremente.
 */
export function enhanceInput(control, { name, mask } = {}) {
  const resolved = maskFor(name, mask);
  if (!resolved || !MASKS[resolved]) return control;
  const applyMask = MASKS[resolved];
  const initial = control.value;
  if (initial && applyMask(initial) !== initial) {
    control.maxLength = API_LIMITS[resolved];
    control.inputMode = resolved === 'phone' ? 'tel' : 'text';
    return control;
  }
  control.maxLength = MASK_LIMITS[resolved];
  control.inputMode = resolved === 'phone' ? 'tel' : resolved === 'cep' ? 'numeric' : 'text';
  const isInternational = (value) => resolved === 'phone' && value.trimStart().startsWith('+');
  let free = false; // true: o texto atual não cabe na máscara e fica como a pessoa deixou
  let pasted = false;

  // Apaga o caractere real do outro lado do separador: sem isto a máscara o recolocava e a tecla travava.
  const deleteAcrossSeparator = (e) => {
    const back = e.inputType === 'deleteContentBackward';
    const text = control.value;
    const start = control.selectionStart;
    if (start !== control.selectionEnd) return;
    let i = back ? start - 1 : start;
    if (i < 0 || i >= text.length || SIGNIFICANT.test(text[i])) return;
    e.preventDefault();
    while (i >= 0 && i < text.length && !SIGNIFICANT.test(text[i])) i += back ? -1 : 1;
    if (i < 0 || i >= text.length) return;
    control.value = text.slice(0, i) + text.slice(i + 1);
    control.setSelectionRange(i, i);
    control.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: e.inputType }));
  };

  // O navegador corta pelo maxlength antes do evento input. Solta o limite durante a inserção para um
  // texto colado chegar inteiro à máscara, que tem o próprio corte (e "+55 …" não perder os dígitos finais).
  control.addEventListener('beforeinput', (e) => {
    if (e.inputType?.startsWith('insert')) {
      control.maxLength = PASTE_LIMITS[resolved];
      pasted = !['insertText', 'insertCompositionText'].includes(e.inputType) || (e.data?.length || 0) > 1;
    } else if (!free && (e.inputType === 'deleteContentBackward' || e.inputType === 'deleteContentForward')) {
      deleteAcrossSeparator(e);
    }
  });

  // Mantém o cursor depois dos mesmos caracteres reais, seja qual for a formatação nova.
  const caretFor = (raw, position, value) => {
    let n = [...raw.slice(0, position)].filter((c) => SIGNIFICANT.test(c)).length;
    if (resolved === 'phone') n -= onlyDigits(raw).length - phoneDigits(raw).length;
    let i = 0;
    for (; i < value.length && n > 0; i++) if (SIGNIFICANT.test(value[i])) n--;
    return i;
  };

  const format = (e) => {
    const raw = control.value;
    const whole = !e.inputType?.startsWith('delete') && (pasted || !e.inputType); // sem inputType: preenchimento automático
    pasted = false;
    if (isInternational(raw)) { free = true; control.maxLength = API_LIMITS.phone; return; }
    let next;
    if (free || whole) next = PASTED[resolved](raw);
    else if (resolved === 'phone' && phoneDigits(raw).length > 11) next = { value: phoneDigits(raw), free: true }; // digitado além de 11: nada se perde
    else next = { value: applyMask(raw), free: false };
    free = next.free;
    control.maxLength = free ? API_LIMITS[resolved] : MASK_LIMITS[resolved];
    if (next.value === raw) return;
    const position = control.selectionStart;
    control.value = next.value;
    if (document.activeElement === control && position !== null) {
      const caret = caretFor(raw, position, next.value);
      control.setSelectionRange(caret, caret);
    }
  };
  control.addEventListener('input', format);
  return control;
}

/** Contador "usados / limite". Lê o maxlength do campo: chame depois de enhanceInput, que pode trocá-lo. */
export function characterCounter(control) {
  if (!(control.maxLength > 0)) return null;
  const counter = h('span', { class: 'zy-tf__counter', 'aria-live': 'polite' });
  const update = () => { counter.textContent = `${control.value.length} / ${control.maxLength}`; };
  control.addEventListener('input', update);
  update();
  return counter;
}

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
  // O <dialog> fica na top layer e deixa o resto da página inerte: o aviso precisa morar dentro dele.
  const host = openModals.at(-1)?.dlg || document.body;
  if (t.parentElement !== host) host.append(t);
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
  if (control.matches('input, textarea')) enhanceInput(control, { name: opts.name, mask: opts.mask });

  return h('div', { class: `zy-tf ${opts.wide ? 'wide' : ''}` },
    h('label', { class: 'zy-tf__label', for: id }, opts.label,
      opts.optional ? h('span', { class: 'zy-tf__opt' }, ' (opcional)') : null),
    control,
    opts.hint ? h('p', { class: 'zy-tf__hint', id: hintId }, opts.hint) : null,
    characterCounter(control),
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
 * Marca o botão como ocupado enquanto `fn` roda e ignora novos cliques. Usa aria-disabled em vez de
 * disabled para o botão manter o foco do teclado.
 */
export async function busy(btn, fn) {
  if (btn.getAttribute('aria-busy') === 'true') return undefined;
  btn.setAttribute('aria-busy', 'true');
  btn.setAttribute('aria-disabled', 'true');
  try {
    return await fn();
  } finally {
    btn.removeAttribute('aria-busy');
    btn.removeAttribute('aria-disabled');
  }
}

const openModals = [];
const formSnapshot = (form) => JSON.stringify([...form.elements]
  .filter((el) => el.matches('input, select, textarea') && el.type !== 'file')
  .map((el) => [el.name, el.type === 'checkbox' || el.type === 'radio' ? el.checked : el.value]));

let discardPrompt = null;
/** Pergunta se pode jogar fora o que foi editado. Resolve true para descartar. */
export function confirmDiscard() {
  discardPrompt ||= confirmDialog({ title: 'Descartar o que você mudou?', message: 'As alterações deste formulário ainda não foram salvas.', confirmText: 'Descartar', cancelText: 'Continuar editando', danger: true })
    .finally(() => { discardPrompt = null; });
  return discardPrompt;
}

/** Há modal aberto com formulário alterado? */
export const hasUnsavedModal = () => openModals.some((m) => m.isDirty());

// Formulários da própria tela (fora de modal) que pedem confirmação ao trocar de rota, inclusive pelo Voltar.
const pageGuards = new Set();
/** Registra `isDirty` enquanto `node` estiver na página; sai sozinho quando a tela troca. */
export function guardPage(node, isDirty) {
  pageGuards.add({ node, isDirty });
}
/** Fim de sessão: a troca para #/entrar não pergunta nada. */
export const forgetPageGuards = () => pageGuards.clear();
/** Há formulário da tela atual com alteração não salva? */
export function hasUnsavedPage() {
  for (const g of pageGuards) {
    if (!g.node.isConnected) pageGuards.delete(g);
    else if (g.isDirty()) return true;
  }
  return false;
}

/** Fecha todos os modais sem perguntar (troca de rota já confirmada, fim de sessão). */
export function closeModals() {
  [...openModals].reverse().forEach((m) => m.close(false));
}

/**
 * Diálogo modal acessível (<dialog>). `body` pode ser Node ou texto; `actions` é uma lista de
 * botões já montados. Devolve { dlg, close }. Esc e "Fechar" chamam onClose.
 * Com <form> no corpo, Esc, ✕ e o botão "Cancelar" perguntam antes de descartar o que mudou;
 * `confirmOnDiscard: false` desliga. `close()` chamado pela tela (ex.: depois de salvar) nunca pergunta.
 * Enquanto um botão do modal está com aria-busy (pedido em andamento, ver `busy()`), Esc e ✕ não fecham:
 * o resultado (sucesso ou erro no campo) precisa de onde aparecer.
 */
export function modal({ title, body, actions = [], onClose, wide = false, label, confirmOnDiscard = true }) {
  const titleId = nextId('dlg');
  const closeBtn = h('button', { type: 'button', class: 'zy-btn zy-btn--ghost zy-btn--sm modal__x', 'aria-label': 'Fechar' }, '✕');
  const dlg = h('dialog', { class: `modal ${wide ? 'modal--wide' : ''}`, 'aria-labelledby': titleId },
    h('div', { class: 'modal__head' }, h('h2', { id: titleId, class: 'modal__title' }, title), closeBtn),
    h('div', { class: 'modal__body' }, body instanceof Node ? body : h('p', {}, body)),
    actions.length ? h('div', { class: 'modal__actions' }, actions) : null);
  if (label) dlg.setAttribute('aria-label', label);
  const opener = document.activeElement;
  const form = confirmOnDiscard ? dlg.querySelector('.modal__body form') : null;
  let initial = null; // lido depois de a tela terminar de montar o formulário
  const isDirty = () => Boolean(form && initial !== null && !closed && formSnapshot(form) !== initial);
  let closed = false;
  const close = (v) => {
    if (closed) return;
    closed = true;
    openModals.splice(openModals.indexOf(entry), 1);
    const ownToast = $('#toast', dlg);
    if (ownToast) (openModals.at(-1)?.dlg || document.body).append(ownToast);
    dlg.close();
    dlg.remove();
    if (opener && opener.isConnected) opener.focus();
    onClose?.(v);
  };
  const entry = { dlg, close, isDirty };
  const isBusy = () => Boolean(dlg.querySelector('[aria-busy="true"]'));
  const tryClose = async () => {
    if (isBusy()) { toast('Aguarde: estamos salvando. Assim que terminar, você vê o resultado aqui.'); return; }
    if (isDirty() && !(await confirmDiscard())) return;
    if (isBusy()) return; // a pessoa decidiu enquanto um pedido começou: espera ele terminar
    close(false);
  };
  closeBtn.addEventListener('click', tryClose);
  dlg.addEventListener('cancel', (e) => { e.preventDefault(); tryClose(); });
  // Sem gesto recente, o navegador fecha no segundo Esc sem disparar `cancel` cancelável.
  dlg.addEventListener('close', () => {
    if (closed) return;
    if (isDirty() || isBusy()) { dlg.showModal(); tryClose(); } else close(false);
  });
  actions.find((b) => b.textContent.trim() === 'Cancelar')?.addEventListener('click', (e) => {
    if (!isDirty()) return;
    e.stopImmediatePropagation();
    tryClose();
  });
  document.body.append(dlg);
  dlg.showModal();
  openModals.push(entry);
  const shownToast = $('#toast.show');
  if (shownToast) dlg.append(shownToast);
  setTimeout(() => {
    if (form) initial = formSnapshot(form);
    if (!closed) (dlg.querySelector('.modal__body input, .modal__body select, .modal__body textarea') || dlg.querySelector('.modal__actions button') || closeBtn).focus();
  });
  return { dlg, close };
}

/** Confirmação simples. Resolve true/false. */
export function confirmDialog({ title, message, confirmText = 'Confirmar', cancelText = 'Cancelar', danger = false, typeToConfirm = null }) {
  return new Promise((resolve) => {
    let input = null;
    const body = h('div', { class: 'stack' }, message instanceof Node ? message : h('p', {}, message));
    if (typeToConfirm) {
      const f = field({ name: 'confirm', label: `Para confirmar, digite ${typeToConfirm}`, attrs: { autocomplete: 'off' } });
      input = f.querySelector('input');
      body.append(f);
    }
    const ok = h('button', { type: 'button', class: `zy-btn zy-btn--md ${danger ? 'zy-btn--danger' : 'zy-btn--primary'}`, disabled: Boolean(typeToConfirm) }, confirmText);
    const cancel = h('button', { type: 'button', class: 'zy-btn zy-btn--md zy-btn--quiet' }, cancelText);
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
export function errorBox(err, retry, { back } = {}) {
  const notFound = err.status === 404;
  // O título do ProblemDetails é técnico ("Regra de negócio violada"): só a mensagem vem da API.
  const fromApi = err instanceof ApiError;
  const title = notFound ? 'Não encontramos o que você procurou.'
    : fromApi && err.status >= 400 && err.status < 500 ? 'Não foi possível concluir.'
      : (!fromApi && err.title) || 'Algo não carregou.';
  const message = notFound ? 'O endereço pode estar errado, ou o item foi excluído.' : err.message || '';
  return h('div', { class: 'notice notice--error', role: 'alert' },
    h('p', {}, h('strong', {}, title), ' ', message === title ? '' : message),
    retry && !notFound ? h('button', { type: 'button', class: 'zy-btn zy-btn--quiet zy-btn--sm', onclick: retry }, 'Tentar de novo') : null,
    back ? h('a', { class: 'zy-btn zy-btn--quiet zy-btn--sm', href: back.href }, back.label) : null);
}

/** Vai para a rota sem criar entrada no histórico (redirecionamentos não prendem o Voltar). */
export function replaceRoute(hash) {
  if (location.hash === hash) window.dispatchEvent(new HashChangeEvent('hashchange'));
  else { routeNav.replacing = true; location.replace(hash); }
}
/** Lido pelo roteador: a próxima troca de hash substitui a entrada atual do histórico (não cria uma nova). */
export const routeNav = { replacing: false };

/** Navega para uma rota (hash). Se já estiver nela, força o recarregamento. */
export function go(hash) {
  if (location.hash === hash) window.dispatchEvent(new HashChangeEvent('hashchange'));
  else location.hash = hash;
}

/** Garante http(s) e devolve '#'' para qualquer outro esquema. */
export const safeUrl = (u) => (/^https?:\/\//i.test(String(u || '')) ? String(u) : '#');
