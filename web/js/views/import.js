// Importar currículo (RF-03) e revisar item a item (RF-04), no padrão ReviewItem.
// Nada entra no Perfil Mestre sem a aprovação da pessoa. Sem anúncios nesta tela.
import { api, asList } from '../api.js';
import { h, clear, field, toast, announce, loading, errorBox, confirmDialog, go, nextId, showErrors, clearErrors, modal, fmtTime } from '../dom.js';
import { Button, Icon, PageHead, ReviewItem, StatusBadge, LinkButton } from '../ui.js';
import { getBilling } from '../store.js';
import { usageMeterFor, isPlus } from '../billing.js';
import * as v from '../validate.js';
import { BASIC_FIELDS, SECTIONS, toInput, toApi, labelOf, enumName } from './profile-schema.js';

export async function render(main) {
  const wrap = h('div', { class: 'content' });
  main.append(wrap);
  await uploadStep(wrap);
}

const str = (x) => (Array.isArray(x) ? x.join(', ') : x === true ? 'Sim' : x === false ? 'Não' : x ?? '');
const show = (f, x) => (f.type === 'select' && !f.bool ? labelOf(f.options, x) : f.type === 'month' ? toInput(f, x).split('-').reverse().join('/') : str(x));
const parseProposal = (p) => (typeof p === 'string' ? JSON.parse(p) : p || {});

/**
 * Campos obrigatórios que faltam num item da proposta (ex.: formação sem "situação"). Junta a validação da tela,
 * opções de select que não batem com nenhuma opção válida e o que a própria API marcou em `missingFields`.
 * O Zyft não inventa esses valores: a pessoa escolhe antes de aceitar.
 */
function missingOf(d) {
  if (d.kind !== 'item') return [];
  const val = d.value || {};
  const errs = v.collect(d.section.validate(val));
  for (const f of d.section.fields) {
    if (f.type !== 'select' || !f.required) continue;
    const k = enumName(f.options, val[f.name]);
    if (!f.options.some(([o]) => o && o === k)) errs[f.name] = errs[f.name] || ['Escolha uma opção.'];
  }
  for (const name of [].concat(val.missingFields || [])) {
    const key = String(name).replace(/^\w/, (c) => c.toLowerCase());
    if (d.section.fields.some((f) => f.name === key) && (val[key] === null || val[key] === undefined || val[key] === '')) errs[key] = errs[key] || ['Campo obrigatório.'];
  }
  return d.section.fields.filter((f) => errs[f.name]);
}

/** Texto acolhedor para o que falta: "a situação", "o nível e a situação"… */
function missingText(d, fields) {
  const names = fields.map((f) => f.label.toLowerCase());
  const list = names.length > 1 ? `${names.slice(0, -1).join(', ')} e ${names[names.length - 1]}` : names[0];
  if (d.section.key === 'educations' && fields.length === 1 && fields[0].name === 'status') {
    return 'O currículo não diz se você ainda está cursando, já concluiu ou trancou. Escolha a situação abaixo para poder aprovar; o Zyft não adivinha por você.';
  }
  return `Falta ${list} neste item. Preencha abaixo para poder aprovar; o Zyft não completa por você o que o currículo não diz.`;
}

const kb = (n) => (n >= 1024 * 1024 ? `${(n / 1024 / 1024).toFixed(1).replace('.', ',')} MB` : `${Math.max(1, Math.round(n / 1024))} KB`);

// ---------- 1. Envio ----------
async function uploadStep(wrap) {
  const billing = await getBilling().catch(() => null);
  const inputId = nextId('resume');
  const errId = `${inputId}-err`;
  const input = h('input', { type: 'file', id: inputId, name: 'file', accept: '.pdf,.docx,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document', 'aria-describedby': `${inputId}-hint ${errId}` });
  const err = h('p', { class: 'zy-tf__error', id: errId, hidden: true });
  const chosen = h('span', { class: 'small muted', id: `${inputId}-hint` }, 'PDF ou DOCX, até 10 MB.');
  const send = Button({ variant: 'primary', type: 'submit', icon: 'upload' }, 'Enviar e ler o currículo');
  const zone = h('div', { class: 'upload' },
    h('div', { class: 'upload__info' }, Icon('upload', { size: 24 }),
      h('div', { class: 'stack', style: { gap: '4px' } }, h('label', { for: inputId, style: { fontWeight: '700' } }, 'Escolha o arquivo do seu currículo'), chosen, input, err)),
    send);
  const form = h('form', { novalidate: true, class: 'stack' }, zone);

  const showErr = (msg) => {
    err.textContent = msg || '';
    err.hidden = !msg;
    if (msg) { input.setAttribute('aria-invalid', 'true'); input.focus(); announce(msg); } else input.removeAttribute('aria-invalid');
  };
  const pick = (file) => {
    const msg = v.resumeFile(file);
    showErr(msg);
    chosen.textContent = file && !msg ? `${file.name} · ${kb(file.size)}` : 'PDF ou DOCX, até 10 MB.';
    return !msg;
  };
  input.addEventListener('change', () => pick(input.files[0]));
  zone.addEventListener('dragover', (e) => { e.preventDefault(); zone.classList.add('is-over'); });
  zone.addEventListener('dragleave', () => zone.classList.remove('is-over'));
  zone.addEventListener('drop', (e) => {
    e.preventDefault();
    zone.classList.remove('is-over');
    if (e.dataTransfer.files[0]) { input.files = e.dataTransfer.files; pick(input.files[0]); }
  });

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const file = input.files[0];
    if (!pick(file)) return;
    const fd = new FormData();
    fd.append('file', file);
    const started = Date.now();
    clear(wrap, PageHead({ title: 'Lendo o seu currículo' }), loading('Lendo o arquivo e montando a proposta. Pode levar até um minuto.'));
    announce('Lendo o currículo. Aguarde.');
    try {
      let imp = await api('/resume-imports', { method: 'POST', body: fd });
      imp = await waitReady(imp);
      await reviewStep(wrap, imp, { size: file.size, seconds: Math.round((Date.now() - started) / 1000) });
    } catch (error) {
      if (error.handled) { uploadStep(clear(wrap)); return; }
      clear(wrap, PageHead({ title: 'Importar currículo' }), errorBox(friendly(error), () => uploadStep(clear(wrap))));
    }
  });

  const meter = billing && !isPlus(billing) ? usageMeterFor(billing, 'resumeImports', 'Importações de currículo neste mês') : null;
  clear(wrap,
    PageHead({ title: 'Importar currículo', lead: 'Envie seu currículo em PDF ou DOCX. O Zyft lê o arquivo e mostra cada item para você conferir. Nada entra no seu Perfil Mestre sem a sua aprovação.' }),
    form,
    meter ? h('div', { style: { maxWidth: '420px' } }, meter) : null,
    h('p', { class: 'small muted' }, 'Se o PDF for escaneado (imagem), o Zyft não consegue ler o texto: envie a versão exportada do editor ou preencha à mão.'),
    h('p', {}, h('a', { href: '#/perfil' }, 'Prefiro preencher o Perfil Mestre à mão')));
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
async function reviewStep(wrap, imp, meta = {}) {
  const proposal = parseProposal(imp.proposal);
  const basicsProp = proposal.profile || proposal.basics || {};
  const current = await api('/profile').catch(() => ({}));
  const fileName = imp.fileName || 'curriculo';
  const source = `Importado de ${fileName}`;

  // decisões: { id, kind, field|section, value, state: pending|approved|edited|discarded, conflict, current }
  const decisions = [];
  for (const f of BASIC_FIELDS) {
    const pv = basicsProp[f.name];
    if (pv === undefined || pv === null || str(pv) === '') continue;
    const cv = current?.[f.name];
    if (str(cv) === str(pv)) continue; // igual ao perfil: nada a revisar
    decisions.push({ id: nextId('d'), kind: 'basic', field: f, value: pv, current: cv, conflict: str(cv) !== '', state: 'pending' });
  }
  const existing = {};
  for (const s of SECTIONS) existing[s.key] = new Set(asList(current?.[s.key]).map((x) => s.summary(x).title.toLowerCase()));
  for (const s of SECTIONS) {
    for (const raw of asList(proposal[s.key])) {
      const it = typeof raw === 'string' ? { name: raw } : { ...raw };
      delete it.id;
      const dup = existing[s.key].has(s.summary(it).title.toLowerCase());
      decisions.push({ id: nextId('d'), kind: 'item', section: s, value: it, conflict: dup, state: 'pending' });
    }
  }

  const ctaSlot = h('div');
  const countEl = h('span', { class: 'small muted', role: 'status' });
  const listSlot = h('div', { class: 'stack-lg' });
  const now = () => fmtTime(new Date());

  const remaining = () => decisions.filter((d) => d.state === 'pending').length;
  const paintHead = () => {
    const left = remaining();
    const done = decisions.length - left;
    countEl.textContent = `${done} de ${decisions.length} itens revisados`;
    const cta = Button({ variant: 'primary', disabled: left > 0 || !decisions.length }, left > 0 ? `Faltam ${left} para revisar` : 'Salvar no Perfil Mestre');
    cta.addEventListener('click', apply);
    clear(ctaSlot, cta);
  };

  const setState = (d, state, focusId) => {
    d.state = state;
    if (state !== 'pending') d.at = now();
    paint(focusId || d.id);
    announce(state === 'discarded' ? 'Item descartado.' : state === 'pending' ? 'Decisão desfeita.' : 'Item carimbado.');
  };

  const editModal = (d) => {
    const fields = d.kind === 'basic' ? [d.field] : d.section.fields;
    const val = d.kind === 'basic' ? { [d.field.name]: d.value } : d.value;
    const form = h('form', { novalidate: true, class: 'form-grid', id: nextId('edit') },
      fields.map((f) => field({ ...f, wide: f.wide || f.type === 'checkbox' || d.kind === 'basic', value: toInput(f, val[f.name]) })));
    const cur = form.elements.isCurrent;
    const end = form.elements.endDate;
    if (cur && end) { const sync = () => { end.disabled = cur.checked; if (cur.checked) end.value = ''; }; cur.addEventListener('change', sync); sync(); }
    const save = Button({ variant: 'primary', type: 'submit', form: form.id, icon: 'check' }, 'Corrigir e carimbar');
    const cancel = Button({ variant: 'quiet' }, 'Cancelar');
    const m = modal({ title: 'Editar antes de aceitar', body: form, actions: [save, cancel], wide: d.kind === 'item' });
    cancel.addEventListener('click', () => m.close());
    form.addEventListener('submit', (e) => {
      e.preventDefault();
      clearErrors(form);
      const raw = {};
      for (const el of form.elements) if (el.name) raw[el.name] = el.type === 'checkbox' ? el.checked : (el.value.trim() || null);
      toApi(fields, raw);
      if (raw.isCurrent) raw.endDate = null;
      if (d.kind === 'item') {
        const errs = v.collect(d.section.validate(raw));
        if (v.hasErrors(errs)) return showErrors(form, errs);
        d.value = { ...d.value, ...raw };
      } else {
        const f = d.field;
        const e1 = f.type === 'url' ? v.url(raw[f.name]) : f.type === 'email' && raw[f.name] ? v.email(raw[f.name]) : null;
        if (e1) return showErrors(form, { [f.name]: [e1] });
        d.value = raw[f.name];
      }
      m.close();
      setState(d, 'edited');
    });
  };

  const reviewFor = (d) => {
    let label; let value; let src = source;
    if (d.kind === 'basic') { label = d.field.label; value = show(d.field, d.value); } else {
      const { title, meta: mt } = d.section.summary(d.value);
      label = d.section.singular.replace(/^\w/, (c) => c.toUpperCase());
      value = [title, mt].filter(Boolean).join(' · ');
      if (d.section.key === 'experiences' && d.value.results) value += `\n${d.value.results}`;
      if (d.conflict) src = `${source} · já existe um item parecido no seu perfil`;
    }
    const missing = d.state === 'pending' ? missingOf(d) : [];
    let extra = null;
    if (missing.length) {
      // Item incompleto: os campos que faltam aparecem destacados aqui mesmo; "Aprovar" só libera depois.
      const box = h('div', { class: 'notice notice--care review-missing', role: 'group', 'aria-label': `O que falta em ${label}` },
        h('p', {}, Icon('alert', { size: 14 }), ' ', missingText(d, missing)),
        h('div', { class: 'form-grid' }, missing.map((f) => {
          const el = field({ ...f, value: toInput(f, d.value[f.name]) });
          el.classList.add('has-error');
          const ctl = el.querySelector('input, select, textarea');
          ctl.setAttribute('aria-invalid', 'true');
          const sync = () => {
            const raw = { [f.name]: ctl.value.trim() || null };
            toApi([f], raw);
            d.value = { ...d.value, [f.name]: raw[f.name] };
            const left = missingOf(d);
            const btn = document.querySelector(`#${CSS.escape(d.id)} .zy-review__actions .zy-btn--primary`);
            if (btn) btn.disabled = left.length > 0;
            const ok = !left.some((x) => x.name === f.name);
            el.classList.toggle('has-error', !ok);
            if (ok) ctl.removeAttribute('aria-invalid'); else ctl.setAttribute('aria-invalid', 'true');
          };
          ctl.addEventListener('change', sync);
          ctl.addEventListener('input', sync);
          return el;
        })));
      extra = box;
    }
    const el = ReviewItem({
      label, value, source: src, state: d.state, approvedAt: d.at, extra, approveDisabled: missing.length > 0,
      onApprove: () => (missingOf(d).length ? editModal(d) : setState(d, 'approved')), onEdit: () => editModal(d), onDiscard: () => setState(d, 'discarded'), onUndo: () => setState(d, 'pending'),
    });
    el.id = d.id;
    el.tabIndex = -1;
    el.querySelector('.zy-review__value').style.whiteSpace = 'pre-line';
    return el;
  };

  const conflictCard = (d) => {
    const f = d.field;
    const decided = d.state !== 'pending';
    const useCv = d.state === 'approved' || d.state === 'edited';
    const card = h('section', { class: 'conflict', id: d.id, tabindex: '-1', 'aria-labelledby': `${d.id}-t` },
      h('div', { class: 'conflict__head' },
        h('h3', { id: `${d.id}-t`, class: 'card__title' }, f.label),
        decided ? StatusBadge({ status: 'ready', label: d.state === 'discarded' ? 'Mantido o do perfil' : d.state === 'edited' ? 'Corrigido por você' : 'Usando o do currículo' }) : StatusBadge({ status: 'needs', label: 'Diferente do seu perfil' })),
      h('div', { class: 'conflict__cols' },
        h('div', { class: `conflict__col ${decided && useCv ? 'is-chosen' : ''}` }, h('span', { class: 'kv__k' }, d.state === 'edited' ? 'Sua correção' : 'No currículo'), h('b', {}, show(f, d.value)), h('span', { class: 'caption' }, source)),
        h('div', { class: `conflict__col ${d.state === 'discarded' ? 'is-chosen' : ''}` }, h('span', { class: 'kv__k' }, 'No seu perfil hoje'), h('b', {}, show(f, d.current)), h('span', { class: 'caption' }, 'Carimbado por você'))),
      h('div', { class: 'conflict__actions' },
        decided ? Button({ variant: 'ghost', size: 'sm', onClick: () => setState(d, 'pending') }, 'Desfazer escolha') : [
          Button({ variant: 'quiet', size: 'sm', onClick: () => setState(d, 'approved') }, 'Usar o do currículo'),
          Button({ variant: 'quiet', size: 'sm', onClick: () => setState(d, 'discarded') }, 'Manter o do perfil'),
          Button({ variant: 'ghost', size: 'sm', icon: 'edit', onClick: () => editModal(d) }, 'Editar antes de aceitar'),
        ]));
    return card;
  };

  function paint(focusId) {
    paintHead();
    const conflicts = decisions.filter((d) => d.kind === 'basic' && d.conflict);
    const basics = decisions.filter((d) => d.kind === 'basic' && !d.conflict);
    const groups = [
      { title: 'Dados básicos', items: basics },
      ...SECTIONS.map((s) => ({ title: s.title, items: decisions.filter((d) => d.section === s) })),
    ].filter((g) => g.items.length);
    clear(listSlot,
      conflicts.map(conflictCard),
      groups.map((g) => {
        const n = g.items.filter((d) => d.state === 'approved' || d.state === 'edited').length;
        return h('section', { class: 'group', 'aria-label': g.title },
          h('div', { class: 'group__head' }, h('h2', { class: 'card__title' }, g.title), h('span', { class: 'small muted' }, `${n} de ${g.items.length} aprovados`)),
          g.items.map(reviewFor));
      }),
      !decisions.length ? h('p', { class: 'card empty' }, 'Não encontramos nada novo neste arquivo em relação ao seu perfil. Tente outro arquivo ou preencha à mão.') : null);
    if (focusId) {
      const el = document.getElementById(focusId);
      const btn = el?.querySelector('button');
      (btn || el)?.focus();
    }
  }

  const acceptSafe = Button({ variant: 'quiet', size: 'sm', icon: 'check' }, 'Aceitar tudo que não conflita com o perfil');
  acceptSafe.addEventListener('click', () => {
    let k = 0;
    let incomplete = 0;
    for (const d of decisions) {
      if (d.conflict || d.state !== 'pending') continue;
      if (missingOf(d).length) { incomplete++; continue; } // falta algo: a pessoa completa antes
      d.state = 'approved'; d.at = now(); k++;
    }
    paint();
    announce(`${k} itens carimbados.${incomplete ? ` ${incomplete} ${incomplete === 1 ? 'item precisa' : 'itens precisam'} de um dado seu antes de aprovar.` : ''} Os que conflitam continuam com você.`);
  });

  const discardImport = async (thenUpload) => {
    try {
      await api(`/resume-imports/${encodeURIComponent(imp.id)}/discard`, { method: 'POST', body: {} });
      toast('Importação descartada. Nada mudou no seu perfil.');
    } catch (err) { if (!err.handled) toast(err.message, 'err'); }
    if (thenUpload) uploadStep(clear(wrap));
  };
  const swap = Button({ variant: 'quiet', size: 'sm', icon: 'refresh' }, 'Trocar arquivo');
  swap.addEventListener('click', () => discardImport(true));
  const discard = Button({ variant: 'ghost', size: 'sm' }, 'Descartar importação');
  discard.addEventListener('click', async () => {
    if (await confirmDialog({ title: 'Descartar importação?', message: 'A proposta será apagada e nada entra no seu Perfil Mestre.', confirmText: 'Descartar', danger: true })) discardImport(true);
  });

  async function apply() {
    const body = { profile: {}, experiences: [], educations: [], courses: [], skills: [], languages: [] };
    let applied = 0;
    for (const d of decisions) {
      if (d.state !== 'approved' && d.state !== 'edited') continue;
      applied++;
      if (d.kind === 'basic') body.profile[d.field.name] = d.value;
      else { const { missingFields, ...item } = d.value; void missingFields; body[d.section.key].push(item); }
    }
    if (!applied) {
      if (await confirmDialog({ title: 'Nada para salvar', message: 'Você descartou todos os itens. Quer descartar esta importação?', confirmText: 'Descartar importação' })) discardImport(true);
      return;
    }
    const btn = ctaSlot.querySelector('button');
    btn.disabled = true;
    try {
      const r = await api(`/resume-imports/${encodeURIComponent(imp.id)}/apply`, { method: 'POST', body });
      doneStep(wrap, applied, decisions.length - applied, r);
    } catch (err) {
      btn.disabled = false;
      if (!err.handled) toast(err.message, 'err');
    }
  }

  const warnings = asList(proposal.warnings);
  clear(wrap,
    PageHead({
      crumb: h('p', { class: 'crumb' }, h('a', { href: '#/perfil' }, 'Perfil Mestre'), ' / Importar currículo'),
      title: 'Revise o que lemos', lead: 'Nada entra no seu Perfil Mestre sem a sua aprovação. Confira cada item e corrija o que estiver errado.', actions: ctaSlot,
    }),
    h('section', { class: 'upload', 'aria-label': 'Arquivo importado' },
      h('div', { class: 'upload__info' }, Icon('upload'),
        h('div', {}, h('b', {}, [fileName, meta.size ? kb(meta.size) : null].filter(Boolean).join(' · ')),
          h('span', { class: 'small muted' }, `${meta.seconds ? `Lido em ${meta.seconds} segundos · ` : ''}PDF ou DOCX, até 10 MB${imp.aiUsed ? ' · organizado com ajuda da IA, confira com atenção' : ''}`))),
      h('div', { class: 'row' }, swap, discard)),
    warnings.length ? h('div', { class: 'notice notice--care' }, Icon('alert', { size: 14 }), h('p', {}, warnings.join(' '))) : null,
    h('div', { class: 'row', style: { justifyContent: 'space-between' } }, countEl, acceptSafe),
    listSlot,
    h('p', { class: 'small muted' }, 'Se o PDF for escaneado (imagem), o Zyft não consegue ler o texto: envie a versão exportada do editor ou preencha à mão.'));
  paint();
}

// ---------- 3. Resultado ----------
function doneStep(wrap, applied, discarded, r) {
  clear(wrap,
    PageHead({ title: 'Perfil Mestre atualizado', lead: `${applied} ${applied === 1 ? 'item carimbado' : 'itens carimbados'} e ${discarded} ${discarded === 1 ? 'descartado' : 'descartados'}.` }),
    r?.missing?.length ? h('p', { class: 'notice notice--warm' }, Icon('alert', { size: 14 }), `Ainda faltam ${r.missing.length} informações que os formulários costumam pedir. Elas aparecem no topo do Perfil Mestre.`) : null,
    h('div', { class: 'row' },
      LinkButton('#/perfil', { variant: 'primary' }, 'Ver meu Perfil Mestre'),
      Button({ variant: 'quiet', onClick: () => uploadStep(clear(wrap)) }, 'Importar outro arquivo')));
  announce('Perfil Mestre atualizado.');
  void go;
}
