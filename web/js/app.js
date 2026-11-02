// Shell da SPA: roteamento por hash, guarda de autenticação, menu lateral (AppNav) e foco acessível.
import { api, asList, isLoggedIn, logout, onAuthChange, rememberRoute } from './api.js';
import { $, h, clear, announce, errorBox, loading, busy, replaceRoute, closeModals, confirmDiscard, hasUnsavedModal, hasUnsavedPage, hasBusyModal, whenModalsIdle, forgetPageGuards, routeNav, toast } from './dom.js';
import { AppNav, Logo, Button, Icon } from './ui.js';
import { getOnboarding, getBilling } from './store.js';
import './billing.js'; // registra o diálogo do 402 para todas as telas
import './conflict.js'; // registra o diálogo do 409 profile.conflict
import * as auth from './views/auth.js';
import * as onboarding from './views/onboarding.js';
import * as profile from './views/profile.js';
import * as importer from './views/import.js';
import * as answers from './views/answers.js';
import * as extension from './views/extension.js';
import * as applications from './views/applications.js';
import * as audit from './views/audit.js';
import * as account from './views/account.js';
import * as plans from './views/plans.js';
import * as notFound from './views/not-found.js';
import * as verify from './views/verify.js';
import * as password from './views/password.js';

// layout: 'public' (entrar), 'full' (onboarding, tela cheia) ou 'app' (com menu lateral).
// open: tela pública que também vale com login (links de e-mail), sem redirecionar para o perfil.
const ROUTES = [
  { path: /^\/entrar$/, view: auth, title: 'Entrar', layout: 'public', params: () => ({ mode: 'login' }) },
  { path: /^\/cadastrar$/, view: auth, title: 'Criar conta', layout: 'public', params: () => ({ mode: 'register' }) },
  { path: /^\/verificar$/, view: verify, title: 'Confirmar e-mail', layout: 'public', open: true },
  { path: /^\/esqueci-senha$/, view: password, title: 'Esqueci minha senha', layout: 'public', params: () => ({ mode: 'forgot' }) },
  { path: /^\/redefinir-senha$/, view: password, title: 'Redefinir senha', layout: 'public', open: true, params: () => ({ mode: 'reset' }) },
  { path: /^\/boas-vindas(?:\/([\w-]+))?$/, view: onboarding, title: 'Monte seu passaporte', layout: 'full', params: (m) => ({ step: m[1] }) },
  { path: /^\/perfil$/, view: profile, title: 'Perfil Mestre', nav: 'perfil' },
  { path: /^\/importar$/, view: importer, title: 'Importar currículo', nav: 'importar' },
  { path: /^\/respostas$/, view: answers, title: 'Banco de respostas', nav: 'respostas' },
  { path: /^\/extensao$/, view: extension, title: 'Extensão', nav: 'extensao' },
  { path: /^\/candidaturas$/, view: applications, title: 'Candidaturas', nav: 'candidaturas' },
  { path: /^\/candidaturas\/([\w-]+)$/, view: applications, title: 'Detalhe da candidatura', nav: 'candidaturas', params: (m) => ({ id: m[1] }) },
  { path: /^\/registro$/, view: audit, title: 'Registro de ações', nav: 'registro' },
  { path: /^\/conta$/, view: account, title: 'Conta e privacidade', nav: 'conta' },
  { path: /^\/planos$/, view: plans, title: 'Planos', nav: 'planos' },
];

const NAV = [
  { group: 'Seu perfil' },
  { id: 'perfil', label: 'Perfil Mestre', icon: 'user', href: '#/perfil' },
  { id: 'importar', label: 'Importar currículo', icon: 'upload', href: '#/importar' },
  { id: 'respostas', label: 'Banco de respostas', icon: 'book', href: '#/respostas' },
  { group: 'Candidaturas' },
  { id: 'extensao', label: 'Extensão', icon: 'plug', href: '#/extensao' },
  { id: 'candidaturas', label: 'Candidaturas', icon: 'briefcase', href: '#/candidaturas' },
  { group: 'Conta' },
  { id: 'planos', label: 'Planos', icon: 'target', href: '#/planos' },
  { id: 'registro', label: 'Registro de ações', icon: 'list', href: '#/registro' },
  { id: 'conta', label: 'Conta e privacidade', icon: 'shield', href: '#/conta' },
];

const OB_CHECK = 'zyft.obChecked';
let me = null;
let appCount = null;
let plan = null;
let sessionEpoch = 0; // resposta que chega depois de sair não pode preencher o menu da próxima sessão

async function loadMe(force = false) {
  if (me && !force) return me;
  const epoch = sessionEpoch;
  const fresh = await api('/me');
  if (epoch === sessionEpoch) me = fresh;
  return fresh;
}

// Candidaturas nascem na extensão (outra aba): o contador do menu é relido quando fica velho, quando a aba volta
// a ficar visível e logo depois de a web mudar alguma candidatura.
const COUNT_TTL = 30_000;
let appCountAt = 0;
let countSeq = 0;

/** Contador de candidaturas e plano para o menu. Falha mantém o último valor: tenta de novo na próxima tela. */
async function loadShellData() {
  const epoch = sessionEpoch;
  const seq = ++countSeq;
  const stale = appCount === null || Date.now() - appCountAt > COUNT_TTL;
  const [count, current] = await Promise.all([
    stale ? api('/applications').then((r) => asList(r).length).catch(() => appCount) : appCount,
    plan === null ? getBilling().then((b) => b?.plan || null).catch(() => null) : plan,
  ]);
  if (epoch !== sessionEpoch) return;
  plan = current;
  if (seq === countSeq) setAppCount(count); // resposta atrasada não sobrescreve uma mais nova
}

function setAppCount(n) {
  appCount = n;
  appCountAt = n === null ? 0 : Date.now();
}

const visibleName = (name) => (/[\p{L}\p{N}]/u.test(name || '') ? name : '');

function closeMenu() {
  const sb = $('#sidebar');
  if (!sb.classList.contains('is-open')) return false;
  sb.classList.remove('is-open');
  $('.sidebar__menu', sb)?.setAttribute('aria-expanded', 'false');
  return true;
}

function renderSidebar(active) {
  const sb = $('#sidebar');
  const focusedHref = sb.contains(document.activeElement) ? document.activeElement.getAttribute('href') : null;
  const items = NAV.map((it) => (it.id === 'candidaturas' && appCount ? { ...it, count: appCount } : it));
  const menuBtn = Button({ variant: 'quiet', size: 'sm', icon: 'menu', className: 'sidebar__menu', 'aria-expanded': String(sb.classList.contains('is-open')), 'aria-controls': 'sidebar-nav' }, 'Menu');
  menuBtn.addEventListener('click', () => {
    const open = !sb.classList.contains('is-open');
    sb.classList.toggle('is-open', open);
    menuBtn.setAttribute('aria-expanded', String(open));
  });
  const out = Button({ variant: 'ghost', size: 'sm', icon: 'logout' }, 'Sair');
  out.addEventListener('click', () => busy(out, logout)); // onAuthChange limpa o estado e leva para #/entrar
  clear(sb,
    h('div', { class: 'sidebar__top' }, h('a', { href: '#/perfil', 'aria-label': 'Zyft, ir para o Perfil Mestre' }, Logo()), menuBtn),
    h('div', { class: 'sidebar__nav', id: 'sidebar-nav' },
      AppNav({ items, active }),
      h('div', { class: 'sidebar__user' },
        me ? h('span', { class: 'muted' }, 'Você entrou como') : null,
        me ? h('b', {}, visibleName(me.name) || me.email || '') : null,
        plan ? h('a', { class: 'sidebar__plan', href: '#/planos' }, plan === 'plus' ? 'Zyft Plus' : 'Plano gratuito') : null,
        out)));
  if (focusedHref) sb.querySelector(`a[href="${focusedHref}"]`)?.focus();
}

// ---------- Aviso de e-mail não confirmado ----------
// Só informa: nunca bloqueia rota nem ação. "Agora não" esconde até fechar a aba.
const VERIFY_HIDDEN = 'zyft.verifyBannerHidden';
const bannerHidden = () => { try { return sessionStorage.getItem(VERIFY_HIDDEN) === '1'; } catch { return false; } };

/** Relê /me (ex.: depois de confirmar o e-mail) e atualiza o menu e o aviso. */
async function refreshMe() {
  const fresh = await loadMe(true);
  if (!$('#sidebar').hidden) { repaintSidebar(); paintVerifyBanner(); }
  return fresh;
}

function paintVerifyBanner() {
  const main = $('#main');
  const current = $('#verify-banner', main);
  const show = me && me.emailVerified === false && !bannerHidden() && !$('#sidebar').hidden;
  if (!show) { current?.remove(); return; }
  if (current) { main.prepend(current); return; } // mantém o que a pessoa já viu (ex.: "link enviado")
  const status = h('p', { class: 'verify-banner__status', role: 'status', 'aria-live': 'polite' });
  const resend = Button({ variant: 'secondary', size: 'sm' }, 'Reenviar link');
  const dismiss = Button({ variant: 'quiet', size: 'sm', 'aria-label': 'Agora não: esconder o aviso de confirmação de e-mail até fechar a aba' }, 'Agora não');
  const banner = h('section', { id: 'verify-banner', class: 'notice notice--warm verify-banner', 'aria-label': 'Confirmação de e-mail' },
    Icon('alert', { size: 16 }),
    h('p', {}, h('strong', {}, 'Confirme seu e-mail. '),
      'Enviamos um link para ', h('b', {}, me.email || 'o seu e-mail'), '. Você pode usar o Zyft normalmente enquanto isso; a confirmação ajuda a recuperar a conta se você esquecer a senha.'),
    h('div', { class: 'verify-banner__actions' }, resend, dismiss),
    status);
  resend.addEventListener('click', () => busy(resend, async () => {
    status.textContent = '';
    const r = await verify.resendLink();
    status.textContent = r.message;
    if (r.kind === 'verified') {
      await refreshMe().catch(() => {});
      announce(r.message);
    }
  }));
  dismiss.addEventListener('click', () => {
    try { sessionStorage.setItem(VERIFY_HIDDEN, '1'); } catch { /* ok */ }
    banner.remove();
    const h1 = $('#main h1');
    if (h1) { h1.setAttribute('tabindex', '-1'); h1.focus(); } else $('#main').focus();
    announce('Aviso escondido. Você pode confirmar o e-mail depois em Conta e privacidade.');
  });
  main.prepend(banner);
}

export function go(hash) {
  if (location.hash === hash) route();
  else location.hash = hash;
}

/** Primeiro acesso da sessão: se o onboarding não foi concluído, leva para #/boas-vindas. */
async function needsOnboarding() {
  let checked = false;
  try { checked = sessionStorage.getItem(OB_CHECK) === '1'; } catch { /* ok */ }
  if (checked) return false;
  try { sessionStorage.setItem(OB_CHECK, '1'); } catch { /* ok */ }
  try {
    const ob = await getOnboarding();
    return !ob.completedAt;
  } catch { return false; }
}

let renderSeq = 0;
async function route() {
  const raw = location.hash.replace(/^#/, '');
  // "?token=…" fica fora da comparação de rota e nunca é convertido para minúsculas.
  const qAt = raw.indexOf('?');
  const query = new URLSearchParams(qAt >= 0 ? raw.slice(qAt + 1) : '');
  const path = (qAt >= 0 ? raw.slice(0, qAt) : raw).replace(/\/+$/, '').toLowerCase() || '/perfil';
  // Rota desconhecida mostra o 404 no próprio endereço (com menu se houver login), sem redirecionar.
  const match = ROUTES.map((r) => ({ r, m: path.match(r.path) })).find((x) => x.m)
    || { r: { view: notFound, title: 'Página não encontrada', layout: isLoggedIn() ? 'app' : 'public', notFound: true }, m: [] };
  const { r, m } = match;
  const isPublic = r.layout === 'public';

  if (!isPublic && !isLoggedIn() && !r.notFound) {
    rememberRoute(location.hash);
    return replaceRoute('#/entrar');
  }
  if (isPublic && isLoggedIn() && !r.notFound && !r.open) return replaceRoute('#/perfil');

  const seq = ++renderSeq;
  const main = $('#main');
  const app = $('#app');
  const sb = $('#sidebar');
  const withNav = r.layout !== 'public' && r.layout !== 'full';
  app.classList.toggle('app--public', !withNav);
  sb.hidden = !withNav;
  closeMenu();
  document.title = `${r.title} · Zyft`;
  // Cada tela desenha no próprio palco. A view pode dar append depois de um await: se a rota mudou,
  // o palco dela já saiu do <main> e o que chegar atrasado cai num nó solto, sem misturar telas.
  const stage = h('div', { class: 'route-stage' });
  const loader = withNav ? h('div', { class: 'content' }, loading()) : loading();
  clear(main, stage, loader);
  if (withNav) { renderSidebar(r.nav); paintVerifyBanner(); } // o menu e o Sair existem mesmo se /me falhar ou demorar

  let failed = false;
  try {
    if (!isPublic) {
      await loadMe();
      if (r.layout !== 'full' && (await needsOnboarding())) { if (seq === renderSeq) go('#/boas-vindas'); return; }
      if (seq !== renderSeq) return;
      if (withNav) {
        renderSidebar(r.nav);
        paintVerifyBanner();
        loadShellData().then(() => { if (seq === renderSeq) renderSidebar(r.nav); });
      }
    }
    if (seq !== renderSeq) return;
    await r.view.render(stage, { ...(r.params ? r.params(m) : {}), query, me, refreshShell, refreshMe });
    if (seq === renderSeq) loader.remove();
  } catch (err) {
    if (seq !== renderSeq) return;
    if (err.status === 401) return go('#/entrar');
    failed = true;
    const home = NAV.find((it) => it.id === r.nav) || NAV.find((it) => it.id === 'perfil');
    clear(main, h('div', { class: 'content' }, errorBox(err, route, { back: { href: home.href, label: `Ir para ${home.label}` } })));
  }
  if (seq !== renderSeq) return;
  // Leva o foco para o título da tela nova (teclado e leitores de tela).
  const h1 = main.querySelector('h1');
  if (h1) { h1.setAttribute('tabindex', '-1'); h1.focus({ preventScroll: true }); } else main.focus({ preventScroll: true });
  window.scrollTo(0, 0);
  announce(failed ? `${r.title}. Não foi possível carregar a tela.` : `${r.title}. Tela carregada.`);
}

const repaintSidebar = () => {
  const active = $('#sidebar .zy-nav__item.is-active')?.dataset.nav;
  if (!$('#sidebar').hidden) renderSidebar(active);
};

/**
 * Telas chamam depois de mudar plano ou candidaturas, para o menu refletir.
 * `applications: true` relê a contagem; um número (ex.: a lista que a tela acabou de carregar) é usado direto.
 */
function refreshShell({ applications: a, billing } = {}) {
  if (typeof a === 'number') { countSeq += 1; setAppCount(a); } // invalida leitura em andamento, mais velha
  else if (a) setAppCount(null);
  if (billing) plan = null;
  if (typeof a === 'number' && !billing) { repaintSidebar(); return; }
  loadShellData().then(repaintSidebar);
}

// Voltou para a aba (ex.: concluiu uma candidatura pela extensão no site da vaga): relê a contagem.
document.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'visible' && isLoggedIn() && !$('#sidebar').hidden) refreshShell({ applications: true });
});

// Link "Pular para o conteúdo": foca o <main> sem mexer na rota.
document.querySelector('.skip')?.addEventListener('click', (e) => { e.preventDefault(); $('#main').focus(); });

// Menu do celular: Esc, clique fora e clique em qualquer item fecham (o item da rota atual não navega).
document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape' && closeMenu()) $('.sidebar__menu')?.focus();
});
document.addEventListener('click', (e) => {
  if (!e.target.closest('#sidebar') || e.target.closest('.zy-nav__item')) closeMenu();
});

const isAuthRoute = () => /^#\/(entrar|cadastrar)\/?$/i.test(location.hash);

onAuthChange((logged, reason) => {
  if (logged) return;
  sessionEpoch += 1;
  me = null; setAppCount(null); plan = null;
  try {
    sessionStorage.removeItem(OB_CHECK);
    sessionStorage.removeItem(VERIFY_HIDDEN);
    // Sair não deixa dado pessoal no navegador (aba e localStorage, onde fica o rascunho do onboarding).
    // Em sessão expirada, o rascunho fica para a mesma pessoa retomar.
    if (reason !== 'expired') {
      for (const store of [sessionStorage, localStorage]) {
        for (const key of Object.keys(store)) {
          if (/^zyft\.(importReview|onboarding\.draft)\./.test(key)) store.removeItem(key);
        }
      }
    }
  } catch { /* ok */ }
  closeModals();
  forgetPageGuards();
  if (reason === 'expired' && !isAuthRoute()) rememberRoute(location.hash);
  if (!isAuthRoute()) replaceRoute('#/entrar');
});

// Cada entrada do histórico ganha uma posição (history.state.zyIdx). Com ela, o Voltar/Avançar sobre um formulário
// alterado pode ser desfeito com history.go(-delta) sem apagar nem duplicar entradas do histórico.
let currentHash = location.hash;
let curIdx = Number.isInteger(history.state?.zyIdx) ? history.state.zyIdx : 0;
try { history.replaceState({ ...(history.state || {}), zyIdx: curIdx }, ''); } catch { /* ok */ }
let pass = null; // { hash, render }: próxima troca já decidida (volta depois de Cancelar, ida depois de Descartar)
let waitTicket = 0; // troca adiada enquanto um modal salva: só a última pedida vale

function entryIndex() {
  const st = history.state;
  if (Number.isInteger(st?.zyIdx)) return st.zyIdx;
  const idx = routeNav.replacing ? curIdx : curIdx + 1; // entrada nova (link, hash digitado) ou substituída
  try { history.replaceState({ ...(st || {}), zyIdx: idx }, ''); } catch { /* ok */ }
  return idx;
}

window.addEventListener('hashchange', async () => {
  const target = location.hash;
  const idx = entryIndex();
  routeNav.replacing = false;
  if (pass && target === pass.hash) {
    const { render } = pass;
    pass = null;
    curIdx = idx;
    currentHash = target;
    if (render) { closeModals(); route(); }
    return;
  }
  if (target === currentHash) { curIdx = idx; return route(); } // go() para a própria rota: recarrega
  if (hasBusyModal()) {
    // Salvando: perguntar "Descartar?" seria falso (o que foi digitado está indo para o servidor). A URL volta, a troca
    // espera o pedido terminar e só acontece se deu certo; se deu erro, a pessoa continua no formulário, com o erro.
    const delta = idx - curIdx;
    if (delta) { pass = { hash: currentHash, render: false }; history.go(-delta); }
    else history.replaceState(history.state, '', currentHash || '#/perfil');
    const ticket = ++waitTicket;
    toast('Aguarde: estamos salvando. Assim que terminar, seguimos para a outra tela.');
    await whenModalsIdle();
    if (ticket !== waitTicket || location.hash !== currentHash) return; // outra navegação já foi decidida
    if (hasUnsavedModal() || hasUnsavedPage()) return; // não salvou: fica no formulário, sem perguntar
    if (delta) { pass = { hash: target, render: true }; history.go(delta); return; }
    closeModals();
    currentHash = target;
    location.hash = target;
    return;
  }
  if (hasUnsavedModal() || hasUnsavedPage()) {
    // A URL volta para a tela do formulário enquanto a pessoa decide (vale para o botão Voltar e o hash digitado).
    // A tela não é redesenhada: o que foi digitado continua lá.
    const delta = idx - curIdx;
    if (delta) { pass = { hash: currentHash, render: false }; history.go(-delta); }
    else history.replaceState(history.state, '', currentHash || '#/perfil');
    if (!(await confirmDiscard())) return;
    if (delta) { pass = { hash: target, render: true }; history.go(delta); return; }
    closeModals();
    currentHash = target;
    location.hash = target;
    return;
  }
  closeModals();
  curIdx = idx;
  currentHash = target;
  route();
});
route();

