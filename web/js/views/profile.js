// Perfil Mestre (RF-02) + "O que falta" (RF-05).
// Metáfora do Passaporte: o perfil é um documento único; cada item aprovado é um "carimbo".
import { api, asList } from '../api.js';
import { h, clear, field, readForm, showErrors, clearErrors, toast, announce, badge, confirmDialog, errorBox } from '../dom.js';
import * as v from '../validate.js';
import { SECTIONS, BASIC_FIELDS, toInput, toApi } from './profile-schema.js';

export async function render(main) {
  const [profile, missing] = await Promise.all([
    api('/profile').catch((e) => (e.status === 404 ? {} : Promise.reject(e))),
    api('/profile/missing').then(asList).catch(() => []),
  ]);
  const p = profile || {};
  const passport = h('div');
  const counts = {};

  const paintPassport = () => clear(passport, h('section', { class: 'passport', 'aria-label': 'Resumo do seu passaporte' },
    h('div', {},
      h('div', { class: 'small muted' }, 'Passaporte profissional'),
      h('div', { class: 'item-title' }, p.fullName || 'Seu nome aparece aqui'),
      h('div', { class: 'small muted' }, p.headline || 'Adicione um título profissional nos dados básicos')),
    h('ul', { class: 'stamps', 'aria-label': 'Carimbos: itens aprovados por seção' },
      SECTIONS.map((s) => h('li', { class: `stamp ${counts[s.key] ? '' : 'empty'}` },
        h('strong', {}, String(counts[s.key] ?? 0)), s.title)))));

  main.append(
    h('div', { class: 'page-head' }, h('div', {},
      h('h1', {}, 'Perfil Mestre'),
      h('p', { class: 'lead' }, 'Preencha uma vez. A extensão sempre usa a versão aprovada daqui, e só os campos que cada formulário pede.'))),
    passport,
    missingBox(missing) || '',
    basicsCard(p, paintPassport));

  for (const s of SECTIONS) {
    const holder = h('div');
    main.append(holder);
    await listSection(holder, s, p, (n) => { counts[s.key] = n; paintPassport(); });
  }
  paintPassport();
}

// ---------- O que falta (RF-05) ----------
function missingBox(missing) {
  if (!missing.length) return null;
  return h('section', { class: 'card callout', 'aria-labelledby': 'missing-title' },
    h('h2', { id: 'missing-title' }, `Faltam ${missing.length} ${missing.length === 1 ? 'informação' : 'informações'} que os portais costumam pedir`),
    h('ul', {}, missing.map((m) => h('li', {}, m.question || m.label || m.key || String(m)))),
    h('p', { class: 'small muted' }, 'Preencha nos dados básicos ou nas seções abaixo. Nada é obrigatório: deixe em branco o que não quiser informar.'));
}

// ---------- Dados básicos ----------
function basicsCard(p, onSaved) {
  const status = h('span', { class: 'small muted', role: 'status' });
  const save = h('button', { class: 'primary', type: 'submit' }, 'Salvar dados básicos');
  const form = h('form', { novalidate: true, 'aria-labelledby': 'basics-title' },
    h('div', { class: 'grid' }, BASIC_FIELDS.map((f) => field({ ...f, value: toInput(f, p[f.name]) }))),
    h('div', { class: 'form-actions' }, save, status));

  form.addEventListener('input', () => { status.textContent = 'Alterações não salvas'; });
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    clearErrors(form);
    const data = toApi(BASIC_FIELDS, readForm(form));
    const errors = v.collect({
      fullName: v.required(data.fullName, 'Informe seu nome completo.'),
      email: data.email ? v.email(data.email) : null,
      linkedinUrl: v.url(data.linkedinUrl),
      portfolioUrl: v.url(data.portfolioUrl),
            summary: v.maxLen(data.summary, 2000),
      githubUrl: v.url(data.githubUrl),
      salaryExpectation: data.salaryExpectation !== null && !(data.salaryExpectation >= 0) ? 'Informe um valor numérico, sem pontos de milhar.' : null,
      birthDate: v.notFuture(data.birthDate, 'A data de nascimento não pode estar no futuro.'),
    });
    if (v.hasErrors(errors)) return showErrors(form, errors);
    save.disabled = true;
    try {
      const r = await api('/profile', { method: 'PUT', body: data });
      Object.assign(p, r && typeof r === 'object' ? r : data);
      status.textContent = `Salvo às ${new Date().toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}`;
      toast('Dados básicos salvos', 'ok');
      onSaved();
    } catch (err) {
      showErrors(form, err);
    } finally {
      save.disabled = false;
    }
  });

  return h('section', { class: 'card', 'aria-labelledby': 'basics-title' },
    h('h2', { id: 'basics-title' }, 'Dados básicos'),
    h('p', { class: 'small muted' }, 'Campos marcados como opcionais podem ficar em branco.'),
    form);
}


// ---------- Seções em lista (experiências, formação, cursos, competências, idiomas) ----------
async function listSection(holder, s, profile, onCount) {
  let items;
  try {
    items = asList(await api(s.endpoint));
  } catch (err) {
    if (err.status === 404 || err.status === 405) items = profile[s.key] || [];
    else { holder.append(errorBox(err)); return; }
  }
  const titleId = `sec-${s.key}`;
  const list = h('ul', { class: 'items' });
  const editorSlot = h('div');
  const addBtn = h('button', { type: 'button', 'aria-expanded': 'false' }, `Adicionar ${s.singular}`);

  const paint = () => {
    onCount(items.length);
    clear(list, items.length ? items.map((it) => itemRow(it)) : h('li', { class: 'empty' }, s.empty));
  };

  const itemRow = (it) => {
    const row = h('li', { class: 'item' });
    const edit = h('button', { type: 'button', class: 'link', 'aria-expanded': 'false' }, 'Editar', h('span', { class: 'sr-only' }, ` ${s.summary(it).title}`));
    const remove = h('button', { type: 'button', class: 'link danger' }, 'Excluir', h('span', { class: 'sr-only' }, ` ${s.summary(it).title}`));
    const { title, meta } = s.summary(it);
    row.append(h('div', { class: 'item-head' },
      h('div', {},
        h('div', { class: 'item-title' }, title),
        meta ? h('div', { class: 'small muted' }, meta) : null),
      h('div', { class: 'item-actions' }, badge('Carimbado', 'ready', '✓'), edit, remove)));
    edit.addEventListener('click', () => {
      edit.setAttribute('aria-expanded', 'true');
      openEditor(row, it, () => { edit.setAttribute('aria-expanded', 'false'); edit.focus(); });
    });
    remove.addEventListener('click', async () => {
      const ok = await confirmDialog({ title: `Excluir ${s.singular}?`, message: `“${title}” sai do seu Perfil Mestre. Esta ação não pode ser desfeita.`, confirmText: 'Excluir', danger: true });
      if (!ok) return remove.focus();
      try {
        await api(`${s.endpoint}/${encodeURIComponent(it.id)}`, { method: 'DELETE' });
        items = items.filter((x) => x !== it);
        paint();
        announce('Item excluído do perfil.');
        addBtn.focus();
      } catch (err) { toast(err.message, 'err'); }
    });
    return row;
  };

  const openEditor = (container, item, onClose) => {
    container.querySelector('.editor')?.remove();
    const isNew = !item;
    const save = h('button', { class: 'primary', type: 'submit' }, isNew ? `Adicionar ${s.singular}` : 'Salvar alterações');
    const cancel = h('button', { type: 'button' }, 'Cancelar');
    const form = h('form', { class: 'editor', novalidate: true, 'aria-label': isNew ? `Adicionar ${s.singular}` : `Editar ${s.singular}` },
      h('div', { class: 'grid' }, s.fields.map((f) => field({ ...f, value: toInput(f, item?.[f.name]) }))),
      h('div', { class: 'form-actions' }, save, cancel));

    // "Emprego atual" desabilita a data de fim.
    const cur = form.elements.isCurrent;
    const end = form.elements.endDate;
    if (cur && end) {
      const sync = () => { end.disabled = cur.checked; if (cur.checked) end.value = ''; };
      cur.addEventListener('change', sync);
      sync();
    }
    const close = () => { form.remove(); onClose?.(); };
    cancel.addEventListener('click', close);
    form.addEventListener('keydown', (e) => { if (e.key === 'Escape') close(); });
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      clearErrors(form);
      const data = toApi(s.fields, readForm(form));
      if (cur && cur.checked) data.endDate = null;
      const errors = v.collect(s.validate(data));
      if (v.hasErrors(errors)) return showErrors(form, errors);
      save.disabled = true;
      try {
        const saved = isNew
          ? await api(s.endpoint, { method: 'POST', body: data })
          : await api(`${s.endpoint}/${encodeURIComponent(item.id)}`, { method: 'PUT', body: data });
        if (isNew) items.push(saved && typeof saved === 'object' ? saved : data);
        else Object.assign(item, saved && typeof saved === 'object' ? saved : data);
        paint();
        toast(isNew ? 'Item adicionado ao perfil. Carimbado!' : 'Alterações salvas', 'ok');
        form.remove();
        onClose?.();
      } catch (err) {
        showErrors(form, err);
      } finally {
        save.disabled = false;
      }
    });
    container.append(form);
    form.querySelector('input, select, textarea')?.focus();
  };

  addBtn.addEventListener('click', () => {
    addBtn.setAttribute('aria-expanded', 'true');
    openEditor(editorSlot, null, () => { addBtn.setAttribute('aria-expanded', 'false'); addBtn.focus(); });
  });

  paint();
  holder.append(h('section', { class: 'card', 'aria-labelledby': titleId },
    h('div', { class: 'card-head' }, h('h2', { id: titleId }, s.title), addBtn),
    s.help ? h('p', { class: 'small muted' }, s.help) : null,
    list, editorSlot));
}
