// Planos (contrato v2): GET /billing; assinar Plus (simulado), cancelar, comprar pacote avulso,
// mensal/anual, medidor de uso e tabela comparativa só com os limites do contrato. Sem anúncios.
import { api } from '../api.js';
import { h, clear, toast, announce, confirmDialog, fmtDate } from '../dom.js';
import { Button, Icon, PageHead, Card, UsageMeter } from '../ui.js';
import { getBilling, invalidateBilling } from '../store.js';
import { DEMO_NOTE, money, resetNote, isPlus, buyPack, RESOURCES, trialAvailable, trialDays } from '../billing.js';

const DEFAULT_PRICES = { plusMonthly: 9.9, plusYearly: 99, pack: 4.9 };

const FREE_LIST = [
  'Perfil Mestre completo e onboarding em passos',
  '10 candidaturas assistidas por mês',
  '+3 por oferta patrocinada que você escolher ver (até +9)',
  '5 sugestões de texto com IA por mês',
  'Histórico de candidaturas e registro de ações',
];
const PLUS_LIST = [
  'Sem anúncios',
  'Candidaturas assistidas sem limite',
  'Sugestões de IA, análise de vagas e importações sem limite',
  'Banco de respostas sem limite',
  'Até 3 navegadores conectados',
];
const sec = (k) => ({ k, section: true });
const row = (k, free, plus) => ({ k, free, plus });
const TABLE = [
  sec('Perfil'),
  row('Perfil Mestre, competências e campos da sua área', 'Completo', 'Completo'),
  row('Importar currículo (PDF ou DOCX)', '2 por mês', 'Sem limite'),
  row('Banco de respostas', 'Até 15 respostas', 'Sem limite'),
  sec('Na vaga (extensão)'),
  row('Candidaturas assistidas (preencher formulário)', '10 por mês', 'Sem limite'),
  row('Candidaturas extras com oferta patrocinada', '+3 por oferta, até +9 por mês', 'Não precisa'),
  row('Análise da vaga (a vaga e você)', '10 por mês', 'Sem limite'),
  row('Sugestão de texto com IA', '5 por mês', 'Sem limite'),
  row('Navegadores conectados', '1', 'Até 3'),
  sec('Depois de se candidatar'),
  row('Histórico de candidaturas e status', 'Completo', 'Completo'),
  row('Anúncios', 'Até 1 por tela', 'Nenhum'),
];

export async function render(main, { refreshShell } = {}) {
  let b = await getBilling(true).catch(() => null);
  let period = b?.period === 'yearly' ? 'yearly' : 'monthly';
  const wrap = h('div', { class: 'content content--md' });
  main.append(wrap);

  const reload = async (msg) => {
    invalidateBilling();
    b = await getBilling(true).catch(() => b);
    refreshShell?.({ billing: true });
    paint();
    if (msg) announce(msg);
  };

  const check = (t) => h('li', {}, Icon('check'), h('span', {}, t));

  function paint() {
    const prices = { ...DEFAULT_PRICES, ...(b?.prices || {}) };
    const plus = isPlus(b);
    // Dado real do GET /billing: quem já usou o teste (e cancelou) assina sem os dias grátis.
    const trial = trialAvailable(b);
    const days = trialDays(b);
    const yearly = period === 'yearly';
    const perMonth = yearly ? prices.plusYearly / 12 : prices.plusMonthly;

    const toggle = h('div', { class: 'period', role: 'radiogroup', 'aria-label': 'Período' },
      ['monthly', 'yearly'].map((p) => h('button', {
        type: 'button', role: 'radio', 'aria-checked': String(period === p), tabindex: period === p ? '0' : '-1',
        onclick: () => { period = p; paint(); wrap.querySelector('.period [aria-checked="true"]')?.focus(); },
        onkeydown: (e) => { if (['ArrowLeft', 'ArrowRight'].includes(e.key)) { e.preventDefault(); period = period === 'monthly' ? 'yearly' : 'monthly'; paint(); wrap.querySelector('.period [aria-checked="true"]')?.focus(); } },
      }, p === 'monthly' ? 'Mensal' : 'Anual · 2 meses grátis')));

    // Gratuito
    const assisted = b?.usage?.assistedApplications;
    const freeCard = h('article', { class: `plan ${plus ? '' : 'plan--current'}`, 'aria-labelledby': 'plan-free' },
      h('div', { class: 'plan__head' }, h('h2', { class: 'plan__name', id: 'plan-free' }, 'Gratuito'), plus ? null : h('span', { class: 'plan__badge' }, 'Seu plano')),
      h('div', {}, h('div', { class: 'plan__price' }, 'R$ 0'), h('div', { class: 'plan__note' }, 'Para sempre, com anúncios úteis')),
      h('ul', { class: 'plan__list' }, FREE_LIST.map(check)),
      !plus && assisted ? h('div', { class: 'plan__foot' }, UsageMeter({ used: assisted.used ?? 0, limit: assisted.limit ?? 10, bonus: assisted.bonus || 0, label: `Candidaturas assistidas em ${new Date().toLocaleDateString('pt-BR', { month: 'long' })}`, note: resetNote() })) : null);

    // Plus
    const plusFoot = h('div', { class: 'plan__foot' });
    if (plus) {
      const inTrial = b?.trialEndsAt && new Date(b.trialEndsAt) > new Date();
      const cancel = Button({ variant: 'quiet' }, 'Cancelar assinatura');
      cancel.addEventListener('click', async () => {
        const ok = await confirmDialog({ title: 'Cancelar o Plus?', message: 'Você volta para o plano gratuito. Nada do seu perfil, das respostas ou das candidaturas se perde.', confirmText: 'Cancelar assinatura' });
        if (!ok) return;
        cancel.disabled = true;
        try { await api('/billing/cancel', { method: 'POST', body: {} }); toast('Assinatura cancelada. Você está no plano gratuito.'); await reload('Plano gratuito.'); } catch (e) { cancel.disabled = false; if (!e.handled) toast(e.message, 'err'); }
      });
      plusFoot.append(
        h('span', { class: 'plan__fine' }, [inTrial ? `Teste grátis até ${fmtDate(b.trialEndsAt)}` : null, b?.renewsAt ? `Renova em ${fmtDate(b.renewsAt)}` : null, b?.period === 'yearly' ? 'Plano anual' : b?.period === 'monthly' ? 'Plano mensal' : null].filter(Boolean).join(' · ')),
        cancel);
    } else {
      const sub = Button({ variant: 'primary' }, trial ? `Testar ${days} dias grátis` : 'Assinar o Plus');
      sub.addEventListener('click', async () => {
        sub.disabled = true;
        try {
          const r = await api('/billing/subscribe', { method: 'POST', body: { period } });
          toast(r?.trialEndsAt ? `Plus ativado. Teste grátis até ${fmtDate(r.trialEndsAt)}.` : 'Plus ativado.', 'ok');
          await reload('Você está no Zyft Plus.');
        } catch (e) { sub.disabled = false; if (!e.handled) toast(e.message, 'err'); }
      });
      plusFoot.append(sub, h('span', { class: 'plan__fine' }, [
        trial ? null : `O teste grátis de ${days} dias já foi usado nesta conta; a assinatura começa hoje.`,
        'Cancele quando quiser, sem multa. Se cancelar, volta para o gratuito e nada do seu perfil se perde.',
      ].filter(Boolean).join(' ')));
    }
    const plusCard = h('article', { class: 'plan plan--plus', 'aria-labelledby': 'plan-plus' },
      h('div', { class: 'plan__head' }, h('h2', { class: 'plan__name', id: 'plan-plus' }, 'Zyft Plus'), plus || trial ? h('span', { class: 'plan__badge plan__badge--zyft' }, plus ? 'Seu plano' : `${days} dias grátis`) : null),
      h('div', {},
        h('div', { class: 'row', style: { alignItems: 'baseline', gap: '6px' } }, h('span', { class: 'plan__price' }, money(perMonth)), h('span', { class: 'plan__per' }, '/mês')),
        h('div', { class: 'plan__note' }, yearly ? `${money(prices.plusYearly)} por ano, cobrado uma vez` : `Ou ${money(prices.plusYearly)} por ano (2 meses grátis)`)),
      h('ul', { class: 'plan__list' }, PLUS_LIST.map(check)),
      plusFoot);

    // Pacote avulso
    const packs = (b?.packs || []).filter((p) => !p.expiresAt || new Date(p.expiresAt) > new Date());
    const buy = Button({ variant: 'quiet' }, 'Comprar pacote');
    buy.addEventListener('click', async () => {
      const ok = await confirmDialog({ title: `Comprar pacote por ${money(prices.pack)}?`, message: h('div', { class: 'stack' }, h('p', {}, '+15 candidaturas assistidas e +10 sugestões de texto com IA, válidos por 60 dias. Pagamento único, sem assinatura.'), h('p', { class: 'demo-note' }, Icon('shield', { size: 14 }), DEMO_NOTE)), confirmText: 'Comprar pacote' });
      if (!ok) return;
      buy.disabled = true;
      try { await buyPack(); await reload('Pacote ativado.'); } catch (e) { buy.disabled = false; if (!e.handled) toast(e.message, 'err'); }
    });
    const packCard = h('article', { class: 'plan', 'aria-labelledby': 'plan-pack' },
      h('h2', { class: 'plan__name', id: 'plan-pack' }, 'Pacote avulso'),
      h('div', {}, h('div', { class: 'plan__price' }, money(prices.pack)), h('div', { class: 'plan__note' }, 'Pagamento único, sem assinatura')),
      h('ul', { class: 'plan__list' }, ['+15 candidaturas assistidas', '+10 sugestões de texto com IA', 'Vale por 60 dias'].map(check)),
      packs.length ? h('div', { class: 'stack', style: { gap: '4px' } }, h('span', { class: 'caption' }, 'Seus pacotes ativos'),
        packs.map((p) => h('span', { class: 'small' }, `${p.assistedRemaining ?? 0} candidaturas e ${p.aiRemaining ?? 0} sugestões · até ${fmtDate(p.expiresAt)}`))) : null,
      h('div', { class: 'plan__foot' }, buy));

    // Uso do mês
    const usage = b?.usage;
    const meters = usage ? Object.keys(RESOURCES).filter((k) => usage[k]).map((k) => {
      const u = usage[k];
      return UsageMeter({ used: u.used ?? 0, limit: u.limit ?? null, bonus: u.bonus || 0, label: RESOURCES[k].label });
    }) : [];
    const rw = b?.rewarded;
    const usageCard = Card({ title: 'Seu uso neste mês', titleId: 'usage-title' },
      b ? [
        h('div', { class: 'usage-grid' }, meters,
          b.answersLimit != null ? h('p', { class: 'small muted' }, `Banco de respostas: até ${b.answersLimit} respostas.`) : null,
          b.devicesLimit != null ? h('p', { class: 'small muted' }, `Navegadores conectados: até ${b.devicesLimit}.`) : null),
        !plus && rw ? h('p', { class: 'small muted' }, `Ofertas patrocinadas vistas neste mês: ${rw.claimedThisMonth ?? 0} de ${rw.maxPerMonth ?? 3} (+${rw.creditsPerClaim ?? 3} candidaturas cada). Ganhe pela extensão, junto do contador.`) : null,
        h('p', { class: 'small muted' }, plus ? 'No Plus não há limite mensal.' : resetNote()),
      ] : h('p', { class: 'notice notice--care', role: 'status' }, Icon('alert', { size: 14 }), 'Não foi possível carregar o seu uso agora. Os planos abaixo continuam valendo; tente de novo em instantes.'));

    // Tabela
    const table = Card({ title: 'Compare', titleId: 'compare-title', className: 'card--flush' },
      h('div', { class: 'table-wrap' }, h('table', { class: 'table compare' },
        h('caption', { class: 'sr-only' }, 'Comparação entre o plano Gratuito e o Plus'),
        h('thead', {}, h('tr', {}, h('th', { scope: 'col' }, 'Recurso'), h('th', { scope: 'col' }, 'Gratuito'), h('th', { scope: 'col' }, 'Plus'))),
        h('tbody', {}, TABLE.map((r) => (r.section
          ? h('tr', { class: 'is-section' }, h('th', { scope: 'rowgroup', colspan: '3', style: { background: 'var(--surface-sunken)', color: 'var(--ink)', fontSize: '14px', padding: '12px 24px' } }, r.k))
          : h('tr', {}, h('td', {}, r.k), h('td', {}, r.free), h('td', {}, r.plus))))))));

    clear(wrap,
      PageHead({ title: 'Planos', lead: 'O essencial é de graça para sempre. O Plus é para quem está numa busca intensa e quer ir sem limite e sem anúncio.', actions: toggle, wide: true }),
      h('p', { class: 'demo-note' }, Icon('shield', { size: 14 }), DEMO_NOTE),
      h('section', { class: 'plans', 'aria-label': 'Planos disponíveis' }, freeCard, plusCard, packCard),
      usageCard,
      table,
      h('section', { class: 'free-forever', 'aria-labelledby': 'ff-title' }, Icon('shield'),
        h('div', { class: 'stack', style: { gap: '4px' } },
          h('h2', { id: 'ff-title', style: { fontSize: '15px', lineHeight: '22px', fontWeight: '700' } }, 'Sempre de graça, em qualquer plano'),
          h('p', { class: 'small muted', style: { fontSize: '14px', lineHeight: '21px' } }, 'Revisar cada campo antes de preencher, dados sensíveis trancados, nunca pedir senha dos portais, consentimentos, baixar e apagar seus dados. Privacidade não é recurso pago.'))));
  }
  paint();
}
