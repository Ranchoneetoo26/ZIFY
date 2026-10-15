// Importar currículo (RF-03) e revisar a proposta campo a campo (RF-04).
// Nada entra no Perfil Mestre por omissão: tudo começa como "Descartar" até a pessoa decidir.
import { api, asList } from '../api.js';
import { h, clear, field, toast, announce, loading, errorBox, confirmDialog, go, nextId, showErrors, clearErrors } from '../dom.js';
import * as v from '../validate.js';
import { BASIC_FIELDS, SECTIONS, toInput, toApi, labelOf } from './profile-schema.js';

export async function render(main) {
  const area = h('div');
  main.append(
    h('div', { class: 'page-head' }, h('div', {},
      h('h1', {}, 'Importar currículo'),
      h('p', { class: 'lead' }, 'Envie seu currículo em PDF ou DOCX. O Zyft organiza o conteúdo numa proposta; você confere cada item e decide o que entra no Perfil Mestre. Nada é salvo sem a sua aprovação.'))),
    area);
  uploadStep(area);
}

// ---------- 1. Envio ----------
function uploadStep(area) {
  const inputId = nextId('resume');
  const errId = `${inputId}-err`;
  const input = h('input', { type: 'file', id: inputId, name: 'file', accept: '.pdf,.docx,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document', 'aria-describedby': `${inputId}-hint ${errId}` });
  const err = h('span', { class: 'field-error', id: errId, hidden: true });
  const chosen = h('p', { class: 'small', role: 'status' });
  const send = h('button', { class: 'primary', type: 'submit' }, 'Enviar e ler o currículo');
  const drop = h('div', { class: 'drop' },
    h('label', { for: inputId, class: 'label' }, 'Escolha o arquivo do currículo'),
    h('span', { class: 'hint', id: `${inputId}-hint` }, 'PDF ou DOCX, até 10 MB. PDF escaneado (imagem) não tem texto para ler.'),
    input, err, chosen);
  const form = h('form', { class: 'card', novalidate: true }, drop, h('div', { class: 'form-actions' }, send));

  const showErr = (msg) => {
    err.textContent = msg || '';
    err.hidden = !msg;
    if (msg) { input.setAttribute('aria-invalid', 'true'); input.focus(); announce(msg); } else input.removeAttribute('aria-invalid');
  };
  const pick = (file) => {
    const msg = v.resumeFile(file);
    showErr(msg);
    chosen.textContent = file && !msg ? `Arquivo escolhido: ${file.name} (${(file.size / 1024 / 1024).toFixed(1)} MB)` : '';
    return !msg;
  };
  input.addEventListener('change', () => pick(input.files[0]));
  drop.addEventListener('dragover', (e) => { e.preventDefault(); drop.classList.add('over'); });
  drop.addEventListener('dragleave', () => drop.classList.remove('over'));
  drop.addEventListener('drop', (e) => {
    e.preventDefault();
    drop.classList.remove('over');
    if (e.dataTransfer.files[0]) { input.files = e.dataTransfer.files; pick(input.files[0]); }
  });

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const file = input.files[0];
    if (!pick(file)) return;
    const fd = new FormData();
    fd.append('file', file);
    clear(area, h('div', { class: 'card' }, loading('Lendo o currículo e montando a proposta. Isso pode levar até um minuto.')));
    announce('Lendo o currículo. Aguarde.');
    try {
      let imp = await api('/resume-imports', { method: 'POST', body: fd });
      imp = await waitReady(imp);
      await reviewStep(area, imp);
    } catch (error) {
      clear(area, errorBox(friendly(error), () => uploadStep(clear(area))));
    }
  });
  area.append(form);
}

function friendly(err) {
  if (err.status === 422 || err.status === 415) err.message = err.detail || 'Não conseguimos ler o texto deste arquivo. Envie um PDF gerado por editor de texto ou um DOCX, ou preencha o perfil à mão.';
  if (err.status === 413) err.message = 'O arquivo passa de 10 MB. Envie uma versão menor.';
  if (err.status === 503) err.message = 'A leitura automática está indisponível agora. Preencha o perfil à mão ou tente mais tarde.';
  return err;
}

async function waitReady(imp) {
  const deadline = Date.now() + 120_000;
  while (!imp.proposal && /pending|processing/i.test(String(imp.status || '')) && Date.now() < deadline) {
    await new Promise((r) => setTimeout(r, 2000));
    imp = await api(`/resume-imports/${encodeURIComponent(imp.id)}`);
  }
  if (!imp.proposal) throw Object.assign(new Error('A proposta não ficou pronta. Tente de novo em instantes.'), { title: 'Importação' });
  return imp;
}

// ---------- 2. Revisão ----------
const str = (x) => (Array.isArray(x) ? x.join(', ') : x === true ? 'Sim' : x === false ? 'Não' : x ?? '');
const show = (f, x) => (f.type === 'select' && !f.bool ? labelOf(f.options, x) : f.type === 'month' ? toInput(f, x).split('-').reverse().join('/') : str(x));
const parseProposal = (p) => (typeof p === 'string' ? JSON.parse(p) : p || {});

async function reviewStep(area, imp) {
  const proposal = parseProposal(imp.proposal);
  const basicsProp = proposal.profile || proposal.basics || proposal;
  const current = await api('/profile').catch(() => ({}));
  // Itens que já existem no perfil (mesmo título) não são aceitos por "Aceitar tudo".
  const existing = {};
  await Promise.all(SECTIONS.map(async (s) => {
    const list = await api(s.endpoint).then(asList).catch(() => asList(current?.[s.key]));
    existing[s.key] = new Set(list.map((x) => s.summary(x).title.toLowerCase()));
  }));
  const decisions = []; // { kind: 'basic'|'item', key, section?, getValue(), getAction() }

  // Campos simples
  const basicRows = BASIC_FIELDS.filter((f) => f.type !== 'checkbox' || basicsProp[f.name] !== undefined)
    .filter((f) => basicsProp[f.name] !== undefined && basicsProp[f.name] !== null && str(basicsProp[f.name]) !== '')
    .map((f) => basicRow(f, basicsProp[f.name], current?.[f.name], decisions));

  // Itens de lista
  const itemBlocks = SECTIONS.map((s) => {
    const items = asList(proposal[s.key]).map((x) => (typeof x === 'string' ? { name: x } : x));
    if (!items.length) return null;
    return h('section', { 'aria-labelledby': `rv-${s.key}` },
      h('h3', { id: `rv-${s.key}` }, `${s.title} (${items.length})`),
      items.map((it) => itemCard(s, it, decisions, existing[s.key]?.has(s.summary(it).title.toLowerCase()))));
  }).filter(Boolean);

  const counter = h('p', { class: 'small', role: 'status', 'aria-live': 'polite' });
  const recount = () => {
    const acts = decisions.map((d) => d.getAction());
    const n = (a) => acts.filter((x) => x === a).length;
    counter.textContent = `${n('accept')} para aceitar, ${n('edit')} editados, ${n('discard')} descartados.`;
  };

  const acceptAll = h('button', { type: 'button' }, 'Aceitar tudo que não conflita com o perfil');
  acceptAll.addEventListener('click', () => {
    let k = 0;
    for (const d of decisions) if (!d.conflict && d.getAction() === 'discard') { d.setAction('accept'); k++; }
    recount();
    announce(`${k} itens marcados para aceitar. Itens com conflito continuam com você.`);
  });

  const applyBtn = h('button', { class: 'primary', type: 'button' }, 'Aplicar ao Perfil Mestre');
  const discardBtn = h('button', { type: 'button', class: 'danger' }, 'Descartar importação');
  const form = h('form', { novalidate: true, 'aria-labelledby': 'review-title' });
  form.addEventListener('change', recount);

  applyBtn.addEventListener('click', async () => {
    clearErrors(form);
    const body = { profile: {}, experiences: [], educations: [], courses: [], skills: [], languages: [] };
    const errors = {};
    for (const d of decisions) {
      const action = d.getAction();
      if (action === 'discard') continue;
      const value = d.getValue(action);
      if (d.kind === 'basic') body.profile[d.key] = value;
      else {
        const errs = v.collect(d.section.validate(value));
        if (v.hasErrors(errs)) { d.setAction('edit'); Object.assign(errors, prefix(errs, d.prefix)); }
        body[d.section.key].push(value);
      }
    }
    if (v.hasErrors(errors)) return showErrors(form, errors);
    const total = decisions.filter((d) => d.getAction() !== 'discard').length;
    if (!total) {
      const ok = await confirmDialog({ title: 'Nada para aplicar', message: 'Você não aceitou nenhum item. Quer descartar esta importação?', confirmText: 'Descartar importação' });
      if (ok) discard();
      return;
    }
    applyBtn.disabled = true;
    try {
      const r = await api(`/resume-imports/${encodeURIComponent(imp.id)}/apply`, { method: 'POST', body });
      done(area, total, decisions.length - total, r);
    } catch (err) {
      showErrors(form, err);
    } finally {
      applyBtn.disabled = false;
    }
  });

  const discard = async () => {
    try {
      await api(`/resume-imports/${encodeURIComponent(imp.id)}/discard`, { method: 'POST', body: {} });
      toast('Importação descartada. Nada mudou no seu perfil.');
      uploadStep(clear(area));
    } catch (err) { toast(err.message, 'err'); }
  };
  discardBtn.addEventListener('click', async () => {
    if (await confirmDialog({ title: 'Descartar importação?', message: 'A proposta será apagada e nada entra no seu Perfil Mestre.', confirmText: 'Descartar', danger: true })) discard();
  });

  form.append(...[
    basicRows.length ? h('section', { class: 'card', 'aria-labelledby': 'rv-basic' },
      h('h2', { id: 'rv-basic' }, 'Dados básicos'),
      h('div', { class: 'review-row head', 'aria-hidden': 'true' }, h('span', {}, 'Campo'), h('span', {}, 'No currículo'), h('span', {}, 'No seu perfil hoje'), h('span', {}, 'Decisão')),
      basicRows) : null,
    itemBlocks.length ? h('div', { class: 'card' }, h('h2', {}, 'Itens encontrados'), itemBlocks) : null,
    !basicRows.length && !itemBlocks.length ? h('div', { class: 'card callout' }, 'Não encontramos dados aproveitáveis neste arquivo. Preencha o perfil à mão ou tente outro arquivo.') : null].filter(Boolean));

  clear(area,
    h('div', { class: 'card callout info' },
      h('h2', { id: 'review-title', tabindex: '-1' }, `Revise a proposta${imp.fileName ? ` de “${imp.fileName}”` : ''}`),
      h('p', {}, 'Para cada item, escolha Aceitar, Editar ou Descartar. Tudo começa como Descartar: só entra no perfil o que você escolher. Itens aceitos viram carimbos no seu passaporte.'),
      h('div', { class: 'row' }, acceptAll)),
    form,
    h('div', { class: 'card' }, counter, h('div', { class: 'form-actions' }, applyBtn, discardBtn)));
  recount();
  area.querySelector('#review-title')?.focus();
}

function prefix(errs, p) {
  const out = {};
  for (const [k, val] of Object.entries(errs)) out[`${p}${k}`] = val;
  return out;
}

/** Linha de campo simples: No currículo | No seu perfil hoje | Decisão. */
function basicRow(f, proposed, currentVal, decisions) {
  const group = nextId('dec');
  const conflict = str(currentVal) !== '' && str(currentVal) !== str(proposed);
  const same = str(currentVal) === str(proposed);
  const editWrap = h('div', { hidden: true });
  const editInput = field({ ...f, name: `${group}-edit`, label: `Novo valor para ${f.label}`, value: toInput(f, proposed), required: false, optional: false, hint: null });
  editWrap.append(editInput);
  const radios = ['accept', 'edit', 'discard'].map((a) => h('label', { class: 'check' },
    h('input', { type: 'radio', name: group, value: a, checked: a === 'discard' }),
    h('span', {}, { accept: 'Aceitar', edit: 'Editar', discard: 'Descartar' }[a])));
  const fs = h('fieldset', { class: 'decision', 'data-col': 'Decisão' }, h('legend', { class: 'sr-only' }, `Decisão para ${f.label}`), radios, editWrap);
  const getAction = () => fs.querySelector(`input[name="${group}"]:checked`)?.value || 'discard';
  fs.addEventListener('change', () => { editWrap.hidden = getAction() !== 'edit'; });

  decisions.push({
    kind: 'basic', key: f.name, conflict,
    getAction,
    setAction: (a) => { fs.querySelector(`input[value="${a}"]`).checked = true; editWrap.hidden = a !== 'edit'; },
    getValue: (action) => {
      if (action !== 'edit') return proposed;
      const el = editWrap.querySelector('input, select, textarea');
      const val = el.type === 'checkbox' ? el.checked : el.value.trim();
      return toApi([f], { [f.name]: val === '' ? null : val })[f.name];
    },
  });

  return h('div', { class: 'review-row' },
    h('strong', {}, f.label),
    h('span', { 'data-col': 'No currículo' }, show(f, proposed)),
    h('span', { 'data-col': 'No seu perfil hoje' }, show(f, currentVal) || h('em', { class: 'muted' }, 'vazio'),
      same ? h('div', { class: 'small muted' }, '(igual ao currículo)') : null,
      conflict ? h('div', { class: 'small' }, h('span', { class: 'badge ambiguous' }, '! Diferente do perfil')) : null),
    fs);
}

/** Cartão de item (experiência, formação…) com decisão e editor completo. */
function itemCard(s, it, decisions, duplicate = false) {
  const group = nextId('item');
  const { title, meta } = s.summary(it);
  const editWrap = h('div', { class: 'editor', hidden: true },
    h('div', { class: 'grid' }, s.fields.map((f) => field({
      ...f, name: `${group}.${f.name}`,
      value: toInput(f, it[f.name]),
    }))));
  const radios = ['accept', 'edit', 'discard'].map((a) => h('label', { class: 'check' },
    h('input', { type: 'radio', name: group, value: a, checked: a === 'discard' }),
    h('span', {}, { accept: 'Aceitar', edit: 'Editar antes de aceitar', discard: 'Descartar' }[a])));
  const details = s.fields.filter((f) => it[f.name] !== undefined && it[f.name] !== null && it[f.name] !== '' && f.type !== 'checkbox')
    .map((f) => h('div', { class: 'small' }, h('strong', {}, `${f.label}: `), show(f, it[f.name])));
  const fs = h('fieldset', { class: 'review-item' },
    h('legend', {}, title),
    duplicate ? h('p', {}, h('span', { class: 'badge ambiguous' }, '! Já existe no seu perfil')) : null,
    meta ? h('p', { class: 'small muted' }, meta) : null,
    details,
    h('div', { class: 'row', role: 'radiogroup', 'aria-label': `Decisão para ${title}` }, radios),
    editWrap);
  const getAction = () => fs.querySelector(`input[name="${group}"]:checked`)?.value || 'discard';
  fs.addEventListener('change', (e) => { if (e.target.name === group) editWrap.hidden = getAction() !== 'edit'; });

  decisions.push({
    kind: 'item', section: s, prefix: `${group}.`, conflict: duplicate,
    getAction,
    setAction: (a) => { fs.querySelector(`input[name="${group}"][value="${a}"]`).checked = true; editWrap.hidden = a !== 'edit'; },
    getValue: (action) => {
      const base = { ...it };
      delete base.id;
      if (action !== 'edit') return base;
      const raw = {};
      for (const el of editWrap.querySelectorAll('[name]')) raw[el.name.slice(group.length + 1)] = el.type === 'checkbox' ? el.checked : el.value.trim() || null;
      toApi(s.fields, raw);
      if (raw.isCurrent) raw.endDate = null;
      return { ...base, ...raw };
    },
  });
  return fs;
}

// ---------- 3. Resultado ----------
function done(area, applied, discarded, r) {
  clear(area, h('div', { class: 'card callout info', role: 'status' },
    h('h2', {}, 'Perfil Mestre atualizado'),
    h('p', {}, `${applied} ${applied === 1 ? 'item aplicado' : 'itens aplicados'} e ${discarded} ${discarded === 1 ? 'descartado' : 'descartados'}. Os itens aceitos agora são carimbos no seu passaporte.`),
    r?.missing?.length ? h('p', {}, `Ainda faltam ${r.missing.length} informações que os portais costumam pedir. Veja em “O que falta”, no Perfil Mestre.`) : null,
    h('div', { class: 'form-actions' },
      h('button', { class: 'primary', type: 'button', onclick: () => go('#/perfil') }, 'Ver meu Perfil Mestre'),
      h('button', { type: 'button', onclick: () => uploadStep(clear(area)) }, 'Importar outro arquivo'))));
  announce('Perfil Mestre atualizado.');
}

