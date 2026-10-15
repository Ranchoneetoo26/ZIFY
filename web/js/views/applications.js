// Candidaturas: registro mínimo de conclusão (RF-19, ADR-0012).
// A extensão cria o registro quando a pessoa clica em "Concluí a candidatura". Aqui a pessoa
// consulta, ajusta o status, anota e exclui. Sem métricas de sucesso nem promessas (RN-06).
import { api, asList } from '../api.js';
import { labelOf, enumName } from './profile-schema.js';
import { h, clear, field, readForm, showErrors, toast, announce, confirmDialog, fmtDateTime, badge, go } from '../dom.js';

// Zyft.Domain.Applications.ApplicationStatus: [nome, rótulo pt-BR, valor numérico].
export const STATUSES = [
  ['Confirmed', 'Enviada por mim', 1],
  ['InReview', 'Em análise pela empresa', 2],
  ['Interview', 'Entrevista', 3],
  ['Test', 'Teste', 4],
  ['Offer', 'Proposta recebida', 5],
  ['Hired', 'Contratado(a)', 6],
  ['Rejected', 'Não seguiu', 7],
  ['Withdrawn', 'Desisti', 8],
  ['NoResponse', 'Sem resposta', 9],
  ['Archived', 'Arquivada', 10],
];
const statusLabel = (s) => labelOf(STATUSES, s) || '—';

export async function render(main, { id }) {
  if (id) return detail(main, id);
  const apps = asList(await api('/applications'));
  main.append(
    h('div', { class: 'page-head' }, h('div', {},
      h('h1', {}, 'Candidaturas'),
      h('p', { class: 'lead' }, 'Cada registro nasce quando você confirma, na extensão, que enviou a candidatura no portal. O Zyft não envia nada por você.'))),
    h('section', { class: 'card table-wrap', 'aria-label': 'Lista de candidaturas' }, apps.length
      ? h('table', {},
        h('caption', {}, `${apps.length} ${apps.length === 1 ? 'candidatura registrada' : 'candidaturas registradas'}`),
        h('thead', {}, h('tr', {}, ['Data', 'Vaga', 'Empresa', 'Portal', 'Campos preenchidos', 'Status'].map((t) => h('th', { scope: 'col' }, t)))),
        h('tbody', {}, apps.map((a) => h('tr', {},
          h('td', {}, fmtDateTime(a.confirmedAt || a.createdAt)),
          h('td', {}, h('a', { href: `#/candidaturas/${encodeURIComponent(a.id)}` }, a.jobTitle || 'Vaga sem título')),
          h('td', {}, a.company || '—'),
          h('td', {}, a.portal || '—'),
          h('td', {}, a.fieldsFilled ?? '—'),
          h('td', {}, badge(statusLabel(a.status), 'neutral'))))))
      : h('p', { class: 'empty' }, 'Nenhuma candidatura registrada. Quando terminar uma candidatura no portal, clique em “Concluí a candidatura” no painel da extensão.')));
}

async function detail(main, id) {
  const a = await api(`/applications/${encodeURIComponent(id)}`);
  const save = h('button', { class: 'primary', type: 'submit' }, 'Salvar');
  const form = h('form', { novalidate: true, 'aria-labelledby': 'app-edit' },
    h('div', { class: 'grid' },
      field({ name: 'status', label: 'Status', type: 'select', options: STATUSES, value: enumName(STATUSES, a.status) }),
      field({ name: 'notes', label: 'Anotações', type: 'textarea', optional: true, value: a.notes, wide: true, hint: 'Só para você. Ex.: data da entrevista.', attrs: { maxlength: 2000 } })),
    h('div', { class: 'form-actions' }, save));
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    save.disabled = true;
    try {
      const data = readForm(form);
      const r = await api(`/applications/${encodeURIComponent(id)}`, { method: 'PATCH', body: { status: data.status, notes: data.notes } });
      Object.assign(a, r && typeof r === 'object' ? r : data);
      toast('Candidatura atualizada', 'ok');
      announce(`Status: ${statusLabel(a.status)}.`);
    } catch (err) { showErrors(form, err); } finally { save.disabled = false; }
  });

  const remove = h('button', { type: 'button', class: 'danger' }, 'Excluir registro');
  remove.addEventListener('click', async () => {
    if (!(await confirmDialog({ title: 'Excluir este registro?', message: 'Só o registro no Zyft é apagado. A candidatura no portal continua lá.', confirmText: 'Excluir', danger: true }))) return remove.focus();
    try {
      await api(`/applications/${encodeURIComponent(id)}`, { method: 'DELETE' });
      toast('Registro excluído');
      go('#/candidaturas');
    } catch (err) { toast(err.message, 'err'); }
  });

  const dl = (k, val) => [h('dt', { class: 'small muted' }, k), h('dd', {}, val ?? '—')];
  main.append(
    h('p', {}, h('a', { href: '#/candidaturas' }, '← Voltar para candidaturas')),
    h('h1', {}, a.jobTitle || 'Candidatura'),
    h('section', { class: 'card' },
      h('dl', { class: 'grid' },
        h('div', {}, dl('Empresa', a.company)),
        h('div', {}, dl('Portal', a.portal)),
        h('div', {}, dl('Concluída em', fmtDateTime(a.confirmedAt || a.createdAt))),
        h('div', {}, dl('Campos preenchidos pelo Zyft', a.fieldsFilled)),
        a.jobUrl ? h('div', { class: 'wide' }, dl('Endereço da vaga', h('a', { href: safeUrl(a.jobUrl), target: '_blank', rel: 'noopener noreferrer' }, a.jobUrl))) : null)),
    h('section', { class: 'card' }, h('h2', { id: 'app-edit' }, 'Acompanhamento'), form),
    h('div', { class: 'form-actions' }, remove));
}

const safeUrl = (u) => (/^https?:\/\//i.test(u) ? u : '#');
