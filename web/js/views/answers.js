// Respostas aprovadas (RF-06): biblioteca de respostas para perguntas recorrentes.
import { api, asList } from '../api.js';
import { h, clear, field, readForm, showErrors, clearErrors, toast, announce, confirmDialog, fmtDate, badge } from '../dom.js';
import * as v from '../validate.js';

const MAX_TEXT = 4000;

export async function render(main) {
  let items = asList(await api('/answers'));
  const list = h('ul', { class: 'items' });
  const formSlot = h('div');

  const paint = () => clear(list, items.length
    ? items.map((a) => row(a))
    : h('li', { class: 'empty' }, 'Nenhuma resposta aprovada ainda. Quando a extensão sugerir um texto e você aprovar, ele também aparece aqui.'));

  const row = (a) => {
    const li = h('li', { class: 'item' });
    const edit = h('button', { type: 'button', class: 'link', 'aria-expanded': 'false' }, 'Editar', h('span', { class: 'sr-only' }, `: ${a.question}`));
    const remove = h('button', { type: 'button', class: 'link danger' }, 'Excluir', h('span', { class: 'sr-only' }, `: ${a.question}`));
    li.append(
      h('div', { class: 'item-head' },
        h('div', { class: 'item-title' }, a.question),
        h('div', { class: 'item-actions' }, badge('Aprovada', 'ready', '✓'), edit, remove)),
      h('p', { class: 'answer-text' }, a.text),
      h('p', { class: 'small muted' },
        a.approvedAt ? `Aprovada em ${fmtDate(a.approvedAt)}` : '',
        a.lastUsedAt ? ` · Último uso em ${fmtDate(a.lastUsedAt)}` : ' · Ainda não usada'));
    edit.addEventListener('click', () => {
      edit.setAttribute('aria-expanded', 'true');
      li.append(editor(a, () => { edit.setAttribute('aria-expanded', 'false'); edit.focus(); }));
    });
    remove.addEventListener('click', async () => {
      if (!(await confirmDialog({ title: 'Excluir resposta?', message: `A resposta para “${a.question}” deixa de ser sugerida pela extensão.`, confirmText: 'Excluir', danger: true }))) return remove.focus();
      try {
        await api(`/answers/${encodeURIComponent(a.id)}`, { method: 'DELETE' });
        items = items.filter((x) => x !== a);
        paint();
        announce('Resposta excluída.');
        newBtn.focus();
      } catch (err) { toast(err.message, 'err'); }
    });
    return li;
  };

  const editor = (a, onClose) => {
    const isNew = !a;
    const save = h('button', { class: 'primary', type: 'submit' }, isNew ? 'Aprovar e salvar' : 'Salvar alterações');
    const cancel = h('button', { type: 'button' }, 'Cancelar');
    const form = h('form', { class: 'editor', novalidate: true, 'aria-label': isNew ? 'Nova resposta aprovada' : 'Editar resposta' },
      field({ name: 'question', label: 'Pergunta', required: true, value: a?.question, hint: 'Ex.: Por que você quer trabalhar conosco?', attrs: { maxlength: 500 } }),
      field({ name: 'text', label: 'Resposta', type: 'textarea', rows: 6, required: true, value: a?.text, hint: `Escreva só fatos verdadeiros. Até ${MAX_TEXT} caracteres.`, attrs: { maxlength: MAX_TEXT } }),
      h('div', { class: 'form-actions' }, save, cancel));
    const close = () => { form.remove(); onClose?.(); };
    cancel.addEventListener('click', close);
    form.addEventListener('keydown', (e) => { if (e.key === 'Escape') close(); });
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      clearErrors(form);
      const data = readForm(form);
      const errors = v.collect({
        question: v.required(data.question, 'Escreva a pergunta.') || v.maxLen(data.question, 500),
        text: v.required(data.text, 'Escreva a resposta.') || v.maxLen(data.text, MAX_TEXT),
      });
      if (v.hasErrors(errors)) return showErrors(form, errors);
      save.disabled = true;
      try {
        const r = isNew
          ? await api('/answers', { method: 'POST', body: data })
          : await api(`/answers/${encodeURIComponent(a.id)}`, { method: 'PUT', body: data });
        if (isNew) items.unshift(r && typeof r === 'object' ? r : { ...data, approvedAt: new Date().toISOString() });
        else Object.assign(a, r && typeof r === 'object' ? r : data);
        paint();
        toast(isNew ? 'Resposta aprovada e salva' : 'Resposta atualizada', 'ok');
        close();
      } catch (err) {
        showErrors(form, err);
      } finally {
        save.disabled = false;
      }
    });
    setTimeout(() => form.querySelector('input')?.focus());
    return form;
  };

  const newBtn = h('button', { type: 'button', class: 'primary', 'aria-expanded': 'false' }, 'Nova resposta');
  newBtn.addEventListener('click', () => {
    if (formSlot.firstChild) return formSlot.querySelector('input')?.focus();
    newBtn.setAttribute('aria-expanded', 'true');
    formSlot.append(editor(null, () => { newBtn.setAttribute('aria-expanded', 'false'); newBtn.focus(); }));
  });

  paint();
  main.append(
    h('div', { class: 'page-head' },
      h('div', {},
        h('h1', {}, 'Respostas aprovadas'),
        h('p', { class: 'lead' }, 'Respostas para perguntas que se repetem nos formulários. Quando a extensão encontra uma pergunta parecida, ela sugere a resposta aprovada, e você ainda revisa antes de preencher.')),
      newBtn),
    formSlot,
    h('section', { class: 'card', 'aria-label': 'Lista de respostas aprovadas' }, list));
}
