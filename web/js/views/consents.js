// Consentimentos por finalidade (RF-07) no padrão ConsentRow. Usado em Conta e privacidade.
import { api, asList } from '../api.js';
import { h, toast, announce, fmtDate } from '../dom.js';
import { ConsentRow } from '../ui.js';

export const PURPOSES = [
  { key: 'AiText', title: 'Sugestões de texto com IA', purpose: 'Rascunhar respostas para perguntas abertas usando só os dados aprovados do seu perfil.', basis: 'Base legal: consentimento (art. 7º, I)' },
  { key: 'JobAnalysis', title: 'Análise de vagas', purpose: 'Ler o texto da vaga aberta e mostrar onde cada requisito aparece no seu perfil.', basis: 'Base legal: consentimento (art. 7º, I)' },
  { key: 'SensitiveData', title: 'Dados sensíveis', purpose: 'Guardar dados sensíveis trancados para você destrancar e conferir um a um. Eles nunca vão para a IA.', basis: 'Base legal: consentimento específico (art. 11, I)' },
];

const norm = (s) => String(s || '').replace(/[_\s-]/g, '').toLowerCase();

export async function loadConsents() {
  const list = asList(await api('/consents'));
  const out = {};
  for (const p of PURPOSES) {
    const found = list.find((c) => norm(c.purpose) === norm(p.key));
    out[p.key] = { granted: Boolean(found?.granted), updatedAt: found?.updatedAt, apiKey: found?.purpose ?? p.key };
  }
  return out;
}

/** Linhas de consentimento; cada mudança é gravada na hora. */
export async function consentRows() {
  const state = await loadConsents();
  return PURPOSES.map((p) => ConsentRow({
    title: p.title, purpose: p.purpose, basis: p.basis,
    updatedAt: state[p.key].updatedAt ? `Alterado em ${fmtDate(state[p.key].updatedAt)}` : null,
    checked: state[p.key].granted,
    onChange: async (next, sw, meta) => {
      sw.disabled = true;
      try {
        const r = await api(`/consents/${encodeURIComponent(state[p.key].apiKey)}`, { method: 'PUT', body: { granted: next } });
        state[p.key].granted = next;
        sw.setChecked(next);
        meta.textContent = [p.basis, `Alterado em ${fmtDate(r?.updatedAt || new Date())}`].join(' · ');
        announce(`${p.title}: ${next ? 'ligado' : 'desligado'}.`);
      } catch (err) { if (!err.handled) toast(err.message, 'err'); } finally { sw.disabled = false; }
    },
  }));
}
void h;
