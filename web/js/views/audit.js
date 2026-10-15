// Registro de ações (RF-20): linha do tempo legível, nunca com valores de campos.
import { api, asList } from '../api.js';
import { h, clear, fmtDateTime, nextId } from '../dom.js';

// Ações conhecidas → texto em linguagem simples. Ações novas aparecem com o código original.
const ACTIONS = {
  'conta.criada': 'Conta criada',
  'dados.exportados': 'Dados exportados',
  'consentimento.alterado': 'Consentimento alterado',
  'perfil.atualizado': 'Perfil Mestre atualizado',
  'curriculo.importado': 'Currículo enviado para leitura',
  'curriculo.aplicado': 'Proposta do currículo aplicada ao perfil',
  'curriculo.descartado': 'Proposta do currículo descartada',
  'extensao.pareada': 'Extensão pareada',
  'extensao.revogada': 'Extensão desconectada',
  'formulario.mapeado': 'Formulário lido pela extensão',
  'texto.sugerido': 'Texto sugerido pela IA',
  'vaga.analisada': 'Vaga analisada',
  'candidatura.confirmada': 'Candidatura concluída por você',
  'sensivel.alterado': 'Dado sensível alterado',
  'sensivel.apagado': 'Dado sensível apagado',
};
const label = (a) => ACTIONS[a] || ACTIONS[String(a).toLowerCase()] || humanize(a);
const humanize = (a) => String(a || 'Ação').replace(/[._]/g, ' ').replace(/([a-z])([A-Z])/g, '$1 $2').replace(/^\w/, (c) => c.toUpperCase());

export async function render(main) {
  const events = asList(await api('/audit-events'));
  const kinds = [...new Set(events.map((e) => e.action))].sort();
  const selectId = nextId('filter');
  const select = h('select', { id: selectId }, h('option', { value: '' }, 'Todas as ações'), kinds.map((k) => h('option', { value: k }, label(k))));
  const list = h('ol', { class: 'timeline' });
  const count = h('p', { class: 'small muted', role: 'status' });

  const paint = () => {
    const f = select.value;
    const shown = events.filter((e) => !f || e.action === f);
    count.textContent = `${shown.length} ${shown.length === 1 ? 'registro' : 'registros'}`;
    clear(list, shown.length ? shown.map((e) => h('li', {},
      h('time', { datetime: e.occurredAt }, fmtDateTime(e.occurredAt)),
      h('span', {},
        h('strong', {}, label(e.action)),
        e.portal ? ` em ${e.portal}` : '',
        e.fieldCount != null ? ` · ${e.fieldCount} ${e.fieldCount === 1 ? 'campo' : 'campos'}` : ''))) : h('li', { class: 'empty' }, 'Nenhum registro.'));
  };
  select.addEventListener('change', paint);
  paint();

  main.append(
    h('div', { class: 'page-head' }, h('div', {},
      h('h1', {}, 'Registro de ações'),
      h('p', { class: 'lead' }, 'Tudo o que o Zyft fez em seu nome, em ordem. O registro guarda a ação, o portal e quantos campos, nunca o conteúdo que você preencheu.'))),
    h('section', { class: 'card', 'aria-label': 'Linha do tempo' },
      h('div', { class: 'row' }, h('label', { for: selectId, class: 'label' }, 'Filtrar por tipo'), h('div', {}, select)),
      count, list));
}
