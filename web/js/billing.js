// Planos e limites (contrato v2). O diálogo do 402 plan.limit_reached vale para qualquer chamada.
// Pagamento é simulado: a interface diz isso de forma discreta.
import { api, onLimitReached } from './api.js';
import { h, modal, toast, announce, clear, fmtDate } from './dom.js';
import { Button, LinkButton, Icon, UsageMeter } from './ui.js';
import { getBilling, invalidateBilling } from './store.js';
import { fetchAd } from './ads.js';

export const DEMO_NOTE = 'Ambiente de demonstração: nenhuma cobrança real.';

export const RESOURCES = {
  assistedApplications: { label: 'Candidaturas assistidas', unit: 'candidaturas assistidas' },
  aiSuggestions: { label: 'Sugestões de texto com IA', unit: 'sugestões de texto com IA' },
  jobAnalyses: { label: 'Análises de vaga', unit: 'análises de vaga' },
  resumeImports: { label: 'Importações de currículo', unit: 'importações de currículo' },
  answers: { label: 'Respostas no banco', unit: 'respostas no banco' },
  devices: { label: 'Navegadores conectados', unit: 'navegadores conectados' },
};

export const money = (n) => Number(n || 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

/** "Recomeça em 1º de novembro." */
export function resetNote() {
  const d = new Date();
  const next = new Date(d.getFullYear(), d.getMonth() + 1, 1);
  return `Recomeça em 1º de ${next.toLocaleDateString('pt-BR', { month: 'long' })}.`;
}

export const isPlus = (b) => b?.plan === 'plus';

/** Os dias grátis valem uma vez por conta: a interface só oferece quando a API diz que estão disponíveis. */
export const trialAvailable = (b) => b?.trialAvailable === true;
export const trialDays = (b) => b?.trialDays || 7;

/** Medidor do recurso com base em GET /billing. */
export function usageMeterFor(b, key, label) {
  const u = b?.usage?.[key];
  if (!u) return null;
  return UsageMeter({ used: u.used ?? 0, limit: u.limit ?? null, bonus: u.bonus || 0, label: label || RESOURCES[key]?.label, note: u.limit == null ? null : resetNote() });
}

export async function buyPack() {
  const r = await api('/billing/packs', { method: 'POST', body: {} });
  invalidateBilling();
  toast('Pacote ativado: +15 candidaturas assistidas e +10 sugestões de IA por 60 dias.', 'ok');
  return r;
}

export async function claimRewarded() {
  const r = await api('/ads/rewarded/claim', { method: 'POST', body: {} });
  invalidateBilling();
  toast('Pronto: +3 candidaturas assistidas neste mês.', 'ok');
  return r;
}

/** Diálogo amigável para 402: oferta recompensada (se houver), pacote avulso e Plus. */
export async function showLimitDialog(err) {
  if (document.querySelector('dialog.modal--limit')) return;
  const p = err?.problem || {};
  const ext = p.extensions || {};
  const resource = p.resource ?? ext.resource ?? 'assistedApplications';
  const used = p.used ?? ext.used;
  const limit = p.limit ?? ext.limit;
  const info = RESOURCES[resource] || { label: 'Este recurso', unit: 'usos' };
  const b = await getBilling(true).catch(() => null);
  const rw = b?.rewarded;
  const canReward = resource === 'assistedApplications' && (!rw || (rw.claimedThisMonth ?? 0) < (rw.maxPerMonth ?? 3));
  const packHelps = ['assistedApplications', 'aiSuggestions'].includes(resource);

  const offerSlot = h('div', { class: 'stack' });
  const options = h('div', { class: 'limit-options' });
  const body = h('div', { class: 'stack' },
    h('p', {}, used != null && limit != null
      ? `Você usou ${used} de ${limit} ${info.unit} do plano gratuito neste mês. ${resetNote()}`
      : `Você chegou ao limite de ${info.unit} do seu plano neste mês. ${resetNote()}`),
    h('p', { class: 'muted' }, 'Nada do seu perfil se perde. Se quiser continuar agora, escolha uma opção:'),
    options, offerSlot,
    h('p', { class: 'demo-note' }, Icon('shield', { size: 14 }), DEMO_NOTE));

  const later = Button({ variant: 'ghost' }, 'Agora não');
  const m = modal({ title: 'Você chegou ao limite deste mês', body, actions: [later] });
  m.dlg.classList.add('modal--limit');
  later.addEventListener('click', () => m.close());

  const option = (title, text, action) => h('div', { class: 'limit-option' },
    h('div', {}, h('b', {}, title), h('p', { class: 'muted small' }, text)), action);

  if (canReward) {
    const btn = Button({ variant: 'secondary', size: 'sm' }, 'Ver oferta');
    btn.addEventListener('click', async () => {
      btn.disabled = true;
      const ad = await fetchAd('web.banner');
      const claim = Button({ variant: 'primary', size: 'sm', icon: 'check' }, 'Ganhar +3 candidaturas');
      clear(offerSlot,
        ad ? h('div', { class: 'ad ad--reward' },
          h('span', { class: 'ad__tag' }, 'Patrocinado'),
          h('b', {}, ad.title),
          h('span', { class: 'muted small' }, [ad.advertiser, ad.body].filter(Boolean).join(' · '))) : null,
        claim);
      claim.addEventListener('click', async () => {
        claim.disabled = true;
        try { await claimRewarded(); m.close(); } catch (e) {
          claim.disabled = false;
          toast(e.code === 'ads.rewarded_exhausted' ? 'Você já usou as 3 ofertas deste mês.' : e.message, 'err');
        }
      });
      claim.focus();
    });
    options.append(option('+3 candidaturas grátis', `Veja uma oferta patrocinada e ganhe +3 neste mês (até +9). ${rw ? `${rw.claimedThisMonth ?? 0} de ${rw.maxPerMonth ?? 3} usadas.` : ''}`, btn));
  }
  if (packHelps) {
    const btn = Button({ variant: 'quiet', size: 'sm' }, `Comprar por ${money(b?.prices?.pack ?? 4.9)}`);
    btn.addEventListener('click', async () => {
      btn.disabled = true;
      try { await buyPack(); m.close(); } catch (e) { btn.disabled = false; if (!e.handled) toast(e.message, 'err'); }
    });
    options.append(option('Pacote avulso', '+15 candidaturas assistidas e +10 sugestões de IA, válido por 60 dias. Pagamento único.', btn));
  }
  options.append(option('Zyft Plus', `Sem limites e sem anúncios. ${money(b?.prices?.plusMonthly ?? 9.9)}/mês${trialAvailable(b) ? `, ${trialDays(b)} dias grátis` : ''}.`,
    LinkButton('#/planos', { variant: 'primary', size: 'sm', onclick: () => m.close() }, 'Conhecer o Plus')));
  announce('Você chegou ao limite deste mês. Veja as opções.');
}

onLimitReached((err) => { showLimitDialog(err); });

export const planLabel = (b) => (isPlus(b) ? 'Zyft Plus' : 'Gratuito');
export const trialText = (b) => (b?.trialEndsAt && new Date(b.trialEndsAt) > new Date() ? `Teste grátis até ${fmtDate(b.trialEndsAt)}` : '');
