// Registro de ações (RF-20): linha do tempo por dia, nunca com valores de campos. Banner web.banner.
import { api, asList } from '../api.js';
import { h, clear, fmtTime, fmtDay, toast, announce, busy } from '../dom.js';
import { Button, Icon, PageHead } from '../ui.js';
import { adSlot, PLACEMENTS } from '../ads.js';

// ação → [título, ícone, categoria]
const ACTIONS = {
  'conta.criada': ['Conta criada', 'user', 'conta'],
  'dados.exportados': ['Dados exportados', 'shield', 'conta'],
  'consentimento.alterado': ['Consentimento alterado', 'shield', 'conta'],
  'perfil.atualizado': ['Perfil Mestre atualizado', 'user', 'perfil'],
  'custom_field.changed': ['Informação da sua área alterada', 'user', 'perfil'],
  'onboarding.completed': ['Passaporte concluído', 'check', 'perfil'],
  'sensivel.alterado': ['Dado sensível alterado', 'lock', 'perfil'],
  'sensivel.apagado': ['Dado sensível apagado', 'lock', 'perfil'],
  'curriculo.importado': ['Currículo enviado para leitura', 'upload', 'curriculo'],
  'curriculo.aplicado': ['Proposta do currículo aplicada', 'upload', 'curriculo'],
  'curriculo.descartado': ['Proposta do currículo descartada', 'upload', 'curriculo'],
  'extensao.pareada': ['Extensão pareada', 'plug', 'extensao'],
  'extensao.revogada': ['Extensão desconectada', 'plug', 'extensao'],
  'formulario.mapeado': ['Formulário lido pela extensão', 'list', 'extensao'],
  'texto.sugerido': ['Texto sugerido pela IA', 'draft', 'extensao'],
  'vaga.analisada': ['Vaga analisada', 'target', 'extensao'],
  'candidatura.confirmada': ['Candidatura concluída por você', 'check', 'candidaturas'],
  'plan.changed': ['Plano alterado', 'target', 'planos'],
  'pack.purchased': ['Pacote avulso ativado', 'target', 'planos'],
  'rewarded.claimed': ['Candidaturas extras por oferta patrocinada', 'target', 'planos'],
  'ad.hidden': ['Anúncio ocultado', 'eyeOff', 'planos'],
};
// A API não pagina: devolve os últimos `limit` eventos, de 200 em 200 até o teto de 1000.
const PAGE = 200;
const MAX_LIMIT = 1000;
const FILTERS = [['', 'Tudo'], ['conta', 'Conta'], ['perfil', 'Perfil'], ['curriculo', 'Currículo'], ['extensao', 'Extensão'], ['candidaturas', 'Candidaturas'], ['planos', 'Planos e anúncios']];
const humanize = (a) => String(a || 'Ação').replace(/[._]/g, ' ').replace(/^\w/, (c) => c.toUpperCase());
const info = (a) => ACTIONS[a] || ACTIONS[String(a).toLowerCase()] || [humanize(a), 'list', 'outros'];

function dayLabel(d) {
  const x = new Date(d);
  const today = new Date();
  const y = new Date(); y.setDate(today.getDate() - 1);
  const same = (a, b) => a.toDateString() === b.toDateString();
  if (same(x, today)) return `Hoje · ${fmtDay(x)}`;
  if (same(x, y)) return `Ontem · ${fmtDay(x)}`;
  const year = x.getFullYear() === today.getFullYear() ? {} : { year: 'numeric' };
  return x.toLocaleDateString('pt-BR', { weekday: 'long', day: '2-digit', month: '2-digit', ...year });
}

export async function render(main) {
  let limit = PAGE;
  const fetchEvents = async () => asList(await api(`/audit-events?limit=${limit}`)).sort((a, b) => new Date(b.occurredAt) - new Date(a.occurredAt));
  let events = await fetchEvents();
  const mayHaveMore = () => events.length >= limit;
  let filter = '';
  const listSlot = h('section', { class: 'stack-lg', 'aria-label': 'Linha do tempo' });
  const status = h('p', { class: 'sr-only', role: 'status' });
  const chips = h('div', { class: 'filter-chips', role: 'group', 'aria-label': 'Filtrar por tipo' });
  const coverage = h('p', { class: 'small muted' });
  const more = Button({ variant: 'quiet' }, 'Carregar mais');
  more.addEventListener('click', () => busy(more, async () => {
    const before = events.length;
    limit = Math.min(limit + PAGE, MAX_LIMIT);
    try {
      events = await fetchEvents();
      paint();
      announce(`${events.length - before} registros carregados.`);
    } catch (err) {
      limit -= PAGE;
      if (!err.handled) toast(err.message, 'err');
    }
  }));

  const paint = () => {
    clear(chips, FILTERS.map(([k, label]) => h('button', {
      type: 'button', class: 'filter-chip', 'aria-pressed': String(filter === k),
      onclick: () => { filter = k; paint(); chips.querySelector('[aria-pressed="true"]')?.focus(); },
    }, label)));
    const shown = events.filter((e) => !filter || info(e.action)[2] === filter);
    status.textContent = `${shown.length} ${shown.length === 1 ? 'registro' : 'registros'}.`;
    const partial = mayHaveMore();
    const loaded = partial ? `Mostrando os ${events.length} registros mais recentes.` : `Mostrando todos os ${events.length} ${events.length === 1 ? 'registro' : 'registros'}.`;
    const filtered = filter ? ` ${shown.length} ${shown.length === 1 ? 'combina' : 'combinam'} com o filtro${partial ? ', que vale só para o que já foi carregado' : ''}.` : '';
    const ceiling = partial && limit >= MAX_LIMIT ? ' Este é o máximo desta tela. O histórico completo está em Baixar meus dados, na Conta e privacidade.' : '';
    coverage.textContent = `${loaded}${filtered}${ceiling}`;
    more.hidden = !partial || limit >= MAX_LIMIT;
    const groups = new Map();
    for (const e of shown) {
      const k = new Date(e.occurredAt).toDateString();
      if (!groups.has(k)) groups.set(k, []);
      groups.get(k).push(e);
    }
    clear(listSlot, shown.length ? [...groups.values()].map((evs) => h('div', { class: 'day' },
      h('h2', { class: 'overline' }, dayLabel(evs[0].occurredAt)),
      h('ol', { class: 'events' }, evs.map((e) => {
        const [title, icon] = info(e.action);
        const detail = [e.portal, e.fieldCount != null ? `${e.fieldCount} ${e.fieldCount === 1 ? 'campo' : 'campos'}` : null].filter(Boolean).join(' · ');
        return h('li', { class: 'event' },
          h('time', { class: 'event__time', datetime: e.occurredAt }, fmtTime(e.occurredAt)),
          h('span', { class: 'event__icon', 'aria-hidden': 'true' }, Icon(icon)),
          h('div', { class: 'event__text' }, h('span', { class: 'event__title' }, title), detail ? h('span', { class: 'event__detail' }, detail) : null));
      })))) : h('p', { class: 'card empty' }, filter ? 'Nenhum registro com esse filtro.' : 'Nenhuma ação registrada ainda.'));
  };
  paint();

  main.append(h('div', { class: 'content' },
    PageHead({ title: 'Registro de ações', lead: 'Tudo o que o Zyft fez em seu nome, na ordem em que aconteceu. Guardamos a ação, o site e a quantidade de campos, nunca o conteúdo preenchido.' }),
    adSlot(PLACEMENTS.banner),
    chips, status, coverage, listSlot, h('div', {}, more)));
}
