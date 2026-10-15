// Consentimentos por finalidade (RF-07). Usado no onboarding e na tela Conta.
import { api, asList } from '../api.js';
import { h, badge, toast, announce, fmtDateTime, nextId } from '../dom.js';

export const PURPOSES = [
  {
    key: 'AiText',
    title: 'Sugestões de texto com IA',
    text: 'A IA sugere respostas para perguntas abertas usando só fatos que você aprovou no Perfil Mestre. Toda sugestão chega desmarcada para você revisar.',
  },
  {
    key: 'JobAnalysis',
    title: 'Análise de vagas',
    text: 'A extensão lê o texto da vaga aberta no seu navegador e mostra quais requisitos aparecem no seu perfil, com a linha do perfil que serve de evidência. Não há nota nem porcentagem.',
  },
  {
    key: 'SensitiveData',
    title: 'Dados sensíveis',
    text: 'Gênero, raça/etnia, deficiência e outros dados sensíveis ficam guardados à parte, criptografados, e nunca vão para a IA. Mesmo ligado, o Zyft só preenche um campo sensível se você marcar aquele campo manualmente.',
    warn: true,
  },
];

const norm = (s) => String(s || '').replace(/[_\s-]/g, '').toLowerCase();

/** Lê o estado dos consentimentos como { AiText: {granted, updatedAt}, ... }. */
export async function loadConsents() {
  const r = await api('/consents');
  const out = {};
  const list = Array.isArray(r) || r?.items ? asList(r) : Object.entries(r || {}).map(([purpose, granted]) => ({ purpose, granted }));
  for (const p of PURPOSES) {
    const found = list.find((c) => norm(c.purpose) === norm(p.key));
    out[p.key] = { granted: Boolean(found?.granted), updatedAt: found?.updatedAt, apiKey: found?.purpose ?? p.key };
  }
  return out;
}

/** Painel com um interruptor por finalidade. Cada mudança é gravada na hora. */
export async function consentsPanel() {
  const state = await loadConsents();
  const wrap = h('div', { class: 'consents' });
  for (const p of PURPOSES) {
    const id = nextId('consent');
    const status = h('span', {});
    const when = h('span', { class: 'small muted' });
    const paint = () => {
      status.replaceChildren(state[p.key].granted ? badge('Ligado', 'on', '●') : badge('Desligado', 'off', '○'));
      when.textContent = state[p.key].updatedAt ? `Última alteração: ${fmtDateTime(state[p.key].updatedAt)}` : '';
    };
    const input = h('input', { type: 'checkbox', id, role: 'switch', checked: state[p.key].granted, 'aria-describedby': `${id}-d` });
    input.addEventListener('change', async () => {
      input.disabled = true;
      try {
        const r = await api(`/consents/${encodeURIComponent(state[p.key].apiKey)}`, { method: 'PUT', body: { granted: input.checked } });
        state[p.key].granted = input.checked;
        state[p.key].updatedAt = r?.updatedAt || new Date().toISOString();
        paint();
        announce(`${p.title}: ${input.checked ? 'ligado' : 'desligado'}.`);
      } catch (err) {
        input.checked = !input.checked;
        toast(err.message, 'err');
      } finally {
        input.disabled = false;
      }
    });
    paint();
    wrap.append(h('div', { class: 'consent' },
      h('div', {},
        h('label', { for: id, class: 'label' }, p.title),
        h('p', { id: `${id}-d`, class: 'small muted' }, p.text),
        when),
      h('div', { class: 'switch' }, input, status)));
  }
  return wrap;
}
