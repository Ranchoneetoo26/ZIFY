// Candidaturas (RF-19, ADR-0012): registro do que a pessoa concluiu no site, com status e anotações.
// Lista com faixa web.applications.feed; detalhe com banner web.banner. Sem métricas de sucesso (RN-06).
import { api, asList } from '../api.js';
import { h, clear, field, readForm, showErrors, toast, announce, confirmDialog, fmtDay, fmtDateTime, go, safeUrl } from '../dom.js';
import { Button, PageHead, Card } from '../ui.js';
import { adSlot, PLACEMENTS } from '../ads.js';
import { labelOf, enumName } from './profile-schema.js';

// Zyft.Domain.Applications.ApplicationStatus: [nome, rótulo pt-BR, valor].
export const STATUSES = [
  ['Confirmed', 'Enviada por mim', 1], ['InReview', 'Em análise pela empresa', 2], ['Interview', 'Entrevista', 3], ['Test', 'Teste', 4],
  ['Offer', 'Proposta recebida', 5], ['Hired', 'Contratado(a)', 6], ['Rejected', 'Não seguiu', 7], ['Withdrawn', 'Desisti', 8],
  ['NoResponse', 'Sem resposta', 9], ['Archived', 'Arquivada', 10],
];
const statusLabel = (s) => labelOf(STATUSES, s) || '—';
const SECONDS_PER_FIELD = 30; // estimativa de digitação poupada por campo

export async function render(main, { id, refreshShell }) {
  if (id) return detail(main, id, refreshShell);
  const apps = asList(await api('/applications'));
  const now = new Date();
  const thisMonth = apps.filter((a) => { const d = new Date(a.confirmedAt || a.createdAt); return d.getMonth() === now.getMonth() && d.getFullYear() === now.getFullYear(); });
  const fields = apps.reduce((n, a) => n + (a.fieldsFilled || 0), 0);
  const mins = Math.round((fields * SECONDS_PER_FIELD) / 60);
  const saved = mins >= 60 ? `≈ ${Math.round(mins / 6) / 10} h`.replace('.', ',') : `≈ ${mins} min`;
  const monthName = now.toLocaleDateString('pt-BR', { month: 'long' });

  const statusSel = h('select', { id: 'f-status' }, h('option', { value: '' }, 'Todos'), STATUSES.map(([v, t]) => h('option', { value: v }, t)));
  const search = h('input', { id: 'f-search', type: 'search', placeholder: 'Ex.: Nuvem Fiscal', autocomplete: 'off' });
  const tableSlot = h('div');
  const count = h('p', { class: 'sr-only', role: 'status' });

  const paint = () => {
    const q = search.value.trim().toLowerCase();
    const s = statusSel.value;
    const shown = apps.filter((a) => (!s || enumName(STATUSES, a.status) === s) && (!q || `${a.jobTitle} ${a.company} ${a.portal}`.toLowerCase().includes(q)));
    count.textContent = `${shown.length} ${shown.length === 1 ? 'candidatura' : 'candidaturas'} na lista.`;
    clear(tableSlot, shown.length ? h('div', { class: 'table-wrap' }, h('table', { class: 'table', style: { minWidth: '760px' } },
      h('caption', { class: 'sr-only' }, 'Candidaturas registradas'),
      h('thead', {}, h('tr', {}, ['Data', 'Vaga', 'Empresa', 'Portal', 'Campos preenchidos', 'Status'].map((t) => h('th', { scope: 'col' }, t)))),
      h('tbody', {}, shown.map((a) => h('tr', {},
        h('td', { class: 'muted', style: { whiteSpace: 'nowrap' } }, fmtDay(a.confirmedAt || a.createdAt)),
        h('td', {}, h('a', { href: `#/candidaturas/${encodeURIComponent(a.id)}` }, a.jobTitle || 'Vaga sem título')),
        h('td', {}, a.company || '—'),
        h('td', {}, a.portal || '—'),
        h('td', {}, a.fieldsFilled != null ? String(a.fieldsFilled) : '—'),
        h('td', {}, h('span', { class: 'status-pill' }, statusLabel(a.status)))))))) : h('p', { class: 'empty' }, apps.length ? 'Nenhuma candidatura com esses filtros.' : 'Nenhuma candidatura registrada. Quando terminar uma candidatura no site da empresa, toque em "Concluí a candidatura" no painel da extensão.'));
  };
  statusSel.addEventListener('change', paint);
  search.addEventListener('input', paint);
  paint();

  main.append(h('div', { class: 'content content--md' },
    PageHead({ title: 'Candidaturas', lead: 'Tudo o que você concluiu com o Zyft, para lembrar onde se candidatou e acompanhar cada uma.' }),
    h('section', { class: 'stats', 'aria-label': 'Resumo' },
      h('div', { class: 'stat' }, h('span', { class: 'stat__n' }, String(thisMonth.length)), h('span', { class: 'stat__label' }, `Candidaturas registradas em ${monthName}`)),
      h('div', { class: 'stat' }, h('span', { class: 'stat__n' }, String(fields)), h('span', { class: 'stat__label' }, 'Campos preenchidos com a sua aprovação')),
      h('div', { class: 'stat' }, h('span', { class: 'stat__n' }, saved), h('span', { class: 'stat__label' }, 'De digitação poupada (estimativa)'))),
    adSlot(PLACEMENTS.applicationsFeed, { variant: 'feed' }),
    h('div', { class: 'filters', role: 'search', 'aria-label': 'Filtrar candidaturas' },
      h('label', { class: 'mini-label', for: 'f-status' }, 'Status', statusSel),
      h('label', { class: 'mini-label', for: 'f-search', style: { flex: '1 1 240px' } }, 'Buscar vaga ou empresa', search)),
    count,
    h('section', { class: 'card card--flush', 'aria-label': 'Lista de candidaturas' }, tableSlot)));
  void refreshShell;
}

async function detail(main, id, refreshShell) {
  const a = await api(`/applications/${encodeURIComponent(id)}`);
  const save = Button({ variant: 'primary', type: 'submit' }, 'Salvar acompanhamento');
  const form = h('form', { novalidate: true, class: 'stack' },
    field({ name: 'status', label: 'Status', type: 'select', options: STATUSES, value: enumName(STATUSES, a.status) }),
    field({ name: 'notes', label: 'Anotações', type: 'textarea', optional: true, rows: 5, value: a.notes, hint: 'Só para você. Ex.: data da entrevista.', attrs: { maxlength: 2000 } }),
    save);
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    save.disabled = true;
    try {
      const data = readForm(form);
      const r = await api(`/applications/${encodeURIComponent(id)}`, { method: 'PATCH', body: { status: data.status, notes: data.notes } });
      Object.assign(a, r && typeof r === 'object' ? r : data);
      toast('Acompanhamento salvo', 'ok');
      announce(`Status: ${statusLabel(a.status)}.`);
    } catch (err) { if (!err.handled) showErrors(form, err); } finally { save.disabled = false; }
  });

  const remove = Button({ variant: 'ghost', size: 'sm' }, 'Excluir registro');
  remove.addEventListener('click', async () => {
    if (!(await confirmDialog({ title: 'Excluir este registro?', message: 'Apaga só o registro no Zyft. A candidatura continua no site da empresa.', confirmText: 'Excluir', danger: true }))) return;
    try {
      await api(`/applications/${encodeURIComponent(id)}`, { method: 'DELETE' });
      toast('Registro excluído');
      refreshShell?.({ applications: true });
      go('#/candidaturas');
    } catch (err) { if (!err.handled) toast(err.message, 'err'); }
  });

  const kv = (k, val) => h('div', { class: 'kv' }, h('span', { class: 'kv__k' }, k), h('span', { class: 'kv__v' }, val ?? '—'));
  main.append(h('div', { class: 'content content--md' },
    PageHead({
      crumb: h('a', { href: '#/candidaturas', class: 'crumb' }, 'Candidaturas'),
      title: a.jobTitle || 'Candidatura',
      lead: [a.company, a.portal ? `via ${a.portal}` : null].filter(Boolean).join(' · '),
      wide: true,
    }),
    adSlot(PLACEMENTS.banner),
    h('div', { class: 'detail-cols' },
      h('div', { class: 'stack-lg' },
        h('section', { class: 'card', 'aria-label': 'Dados da candidatura' },
          h('div', { class: 'kv-grid' },
            a.jobUrl ? h('div', { class: 'kv' }, h('span', { class: 'kv__k' }, 'Endereço da vaga'),
              h('a', { href: safeUrl(a.jobUrl), target: '_blank', rel: 'noopener noreferrer', class: 'kv__v', style: { wordBreak: 'break-all' } }, a.jobUrl.replace(/^https?:\/\//, ''))) : null,
            kv('Você concluiu em', fmtDateTime(a.confirmedAt || a.createdAt)),
            kv('Campos preenchidos pelo Zyft', a.fieldsFilled != null ? String(a.fieldsFilled) : '—'))),
        Card({ title: 'O que o Zyft preencheu', titleId: 'filled-title' },
          h('p', { class: 'card__sub' }, `O Zyft preencheu ${a.fieldsFilled ?? 0} ${a.fieldsFilled === 1 ? 'campo' : 'campos'} que você aprovou nesta página. Por privacidade, o registro guarda só a quantidade, nunca o conteúdo preenchido.`),
          h('p', { class: 'small muted' }, 'O envio foi feito por você, no site da empresa.'))),
      h('aside', { class: 'card', 'aria-labelledby': 'follow-title' },
        h('h2', { class: 'card__title', id: 'follow-title' }, 'Acompanhamento'),
        form,
        h('div', { class: 'stack', style: { borderTop: '1px solid var(--line)', paddingTop: '16px', gap: '8px' } },
          h('div', {}, remove),
          h('p', { class: 'caption' }, 'Apaga só o registro no Zyft. A candidatura continua no site da empresa.'))))));
}
