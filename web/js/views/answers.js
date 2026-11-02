// Banco de respostas (RF-06): "Responda uma vez e o Zyft guarda". Banner web.banner.
import { api, asList } from '../api.js';
import { h, clear, field, readForm, showErrors, clearErrors, toast, announce, confirmDialog, confirmDiscard, fmtDay, fmtDate, guardPage } from '../dom.js';
import { Button, Stamp, PageHead, Card } from '../ui.js';
import { adSlot, PLACEMENTS } from '../ads.js';
import { getBilling } from '../store.js';
import * as v from '../validate.js';

const MAX_TEXT = 5000;

export async function render(main) {
  const [list, billing] = await Promise.all([api('/answers').then(asList), getBilling().catch(() => null)]);
  let items = list;
  const cards = new Map(); // id → card: repintar a lista não pode fechar uma edição em andamento
  const listEl = h('div', { class: 'stack' });
  const usage = h('p', { class: 'small muted', role: 'status' });

  const paintUsage = () => {
    const lim = billing?.answersLimit;
    usage.textContent = lim != null ? `${items.length} de ${lim} respostas no plano gratuito.` : `${items.length} ${items.length === 1 ? 'resposta guardada' : 'respostas guardadas'}.`;
  };

  const validate = (form, data) => {
    const errors = v.collect({
      question: v.required(data.question, 'Escreva a pergunta.') || v.maxLen(data.question, 500),
      text: v.required(data.text, 'Escreva a resposta.') || v.maxLen(data.text, MAX_TEXT),
    });
    if (v.hasErrors(errors)) { showErrors(form, errors); return false; }
    return true;
  };

  const article = (a) => {
    const el = h('article', { class: 'answer', 'aria-labelledby': `ans-${a.id}` });
    const view = () => {
      const edit = Button({ variant: 'ghost', size: 'sm', icon: 'edit', 'aria-label': `Editar a resposta para: ${a.question}` }, 'Editar');
      const del = Button({ variant: 'ghost', size: 'sm', 'aria-label': `Excluir a resposta para: ${a.question}` }, 'Excluir');
      edit.addEventListener('click', editMode);
      del.addEventListener('click', async () => {
        if (!(await confirmDialog({ title: 'Excluir resposta?', message: `A resposta para “${a.question}” deixa de ser sugerida pela extensão.`, confirmText: 'Excluir', danger: true }))) return;
        try {
          await api(`/answers/${encodeURIComponent(a.id)}`, { method: 'DELETE' });
          items = items.filter((x) => x.id !== a.id);
          cards.delete(a.id);
          paint();
          announce('Resposta excluída.');
          document.querySelector('#new-answer')?.focus();
        } catch (err) { if (!err.handled) toast(err.message, 'err'); }
      });
      const meta = [`Aprovada em ${fmtDate(a.approvedAt)}`, a.lastUsedAt ? `Usada pela última vez em ${fmtDate(a.lastUsedAt)}` : 'Ainda não usada'].join(' · ');
      clear(el,
        h('div', { class: 'answer__top' }, h('h3', { class: 'answer__q', id: `ans-${a.id}` }, a.question), Stamp({ size: 'sm', label: 'Aprovada', at: fmtDay(a.approvedAt) })),
        h('p', { class: 'answer__text' }, a.text),
        h('div', { class: 'answer__foot' }, h('span', { class: 'caption' }, meta), h('div', { class: 'row', style: { gap: '4px' } }, edit, del)));
    };
    const editMode = () => {
      const save = Button({ variant: 'primary', size: 'sm', icon: 'check', type: 'submit' }, 'Aprovar e guardar');
      const cancel = Button({ variant: 'ghost', size: 'sm' }, 'Cancelar');
      const form = h('form', { novalidate: true, class: 'stack' },
        field({ name: 'question', label: 'Pergunta', value: a.question, attrs: { maxlength: 500 } }),
        field({ name: 'text', label: 'Sua resposta', type: 'textarea', rows: 5, value: a.text, attrs: { maxlength: MAX_TEXT } }),
        h('div', { class: 'row' }, save, cancel));
      const isDirty = () => { const data = readForm(form); return (data.question ?? '') !== (a.question ?? '') || (data.text ?? '') !== (a.text ?? ''); };
      guardPage(form, isDirty); // Voltar, menu e recarregar perguntam antes de jogar fora a edição
      cancel.addEventListener('click', async () => {
        if (isDirty() && !(await confirmDiscard())) return;
        view();
        el.querySelector('button')?.focus();
      });
      form.addEventListener('submit', async (e) => {
        e.preventDefault();
        clearErrors(form);
        const data = readForm(form);
        if (!validate(form, data)) return;
        save.disabled = true;
        try {
          const r = await api(`/answers/${encodeURIComponent(a.id)}`, { method: 'PUT', body: data });
          Object.assign(a, r && typeof r === 'object' ? r : data);
          view();
          toast('Resposta aprovada e guardada', 'ok');
          el.querySelector('h3')?.setAttribute('tabindex', '-1');
          el.querySelector('h3')?.focus();
        } catch (err) { if (!err.handled) showErrors(form, err); } finally { save.disabled = false; }
      });
      clear(el, form);
      form.querySelector('input')?.focus();
    };
    view();
    return el;
  };

  const paint = () => {
    paintUsage();
    const cardOf = (a) => { if (!cards.has(a.id)) cards.set(a.id, article(a)); return cards.get(a.id); };
    clear(listEl, items.length ? items.map(cardOf)
      : h('p', { class: 'card empty' }, 'Nenhuma resposta guardada ainda. Quando a extensão sugerir um texto e você aprovar, ele também aparece aqui.'));
  };

  // Nova resposta
  const save = Button({ variant: 'primary', icon: 'check', type: 'submit' }, 'Aprovar e guardar');
  const cancel = Button({ variant: 'ghost' }, 'Cancelar');
  const form = h('form', { novalidate: true, class: 'stack', 'aria-labelledby': 'new-answer' },
    field({ name: 'question', label: 'Pergunta', placeholder: 'Ex.: Por que você quer trabalhar aqui?', attrs: { maxlength: 500 } }),
    field({ name: 'text', label: 'Sua resposta', type: 'textarea', rows: 5, hint: 'Escreva com as suas palavras. Ela só é usada depois que você aprova.', attrs: { maxlength: MAX_TEXT } }),
    h('div', { class: 'row' }, save, cancel));
  const resetForm = () => {
    form.reset();
    clearErrors(form);
    for (const el of form.elements) if (el.name) el.dispatchEvent(new Event('input')); // reset não dispara input: o contador ficaria no valor antigo
  };
  const isDirty = () => { const data = readForm(form); return Boolean(data.question || data.text); };
  guardPage(form, isDirty);
  cancel.addEventListener('click', async () => {
    if (isDirty() && !(await confirmDiscard())) return;
    resetForm();
  });
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    clearErrors(form);
    const data = readForm(form);
    if (!validate(form, data)) return;
    save.disabled = true;
    try {
      const r = await api('/answers', { method: 'POST', body: data });
      // A API guarda uma resposta por pergunta: repetir a pergunta devolve o mesmo id, com o texto novo.
      const replaced = items.some((x) => x.id === r.id);
      if (replaced) {
        items = items.filter((x) => x.id !== r.id);
        cards.delete(r.id);
      }
      items.unshift(r);
      resetForm();
      paint();
      const message = replaced ? 'Você já tinha uma resposta para esta pergunta. Ela foi substituída pelo texto novo.' : 'Resposta aprovada e guardada';
      toast(message, 'ok');
      announce(replaced ? message : 'Resposta guardada no banco.');
    } catch (err) { if (!err.handled) showErrors(form, err); } finally { save.disabled = false; }
  });

  paint();
  main.append(h('div', { class: 'content' },
    PageHead({ title: 'Banco de respostas', lead: 'Responda uma vez e o Zyft guarda. Quando um formulário fizer uma pergunta parecida, a extensão propõe a sua resposta aprovada.' }),
    adSlot(PLACEMENTS.banner),
    Card({ title: 'Nova resposta', titleId: 'new-answer' }, form),
    usage,
    listEl));
}
