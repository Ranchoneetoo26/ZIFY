// Shell da SPA: roteamento por hash, guarda de autenticação, menu lateral (AppNav) e foco acessível.
import { api, asList, isLoggedIn, logout, onAuthChange, rememberRoute } from './api.js';
import { $, h, clear, announce, errorBox, loading, toast, busy, replaceRoute, closeModals, confirmDiscard, hasUnsavedModal } from './dom.js';
import { AppNav, Logo, Button } from './ui.js';
import { getOnboarding, getBilling } from './store.js';
import './billing.js'; // registra o diálogo do 402 para todas as telas
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

// layout: 'public' (entrar), 'full' (onboarding, tela cheia) ou 'app' (com menu lateral).
const ROUTES = [
  { path: /^\/entrar$/, view: auth, title: 'Entrar', layout: 'public', params: () => ({ mode: 'login' }) },
  { path: /^\/cadastrar$/, view: auth, title: 'Criar conta', layout: 'public', params: () => ({ mode: 'register' }) },
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

/** Contador de candidaturas e plano para o menu. Falha deixa null: tenta de novo na próxima tela. */
async function loadShellData() {
  const epoch = sessionEpoch;
  const [count, current] = await Promise.all([
    appCount === null ? api('/applications').then((r) => asList(r).length).catch(() => null) : appCount,
    plan === null ? getBilling().then((b) => b?.plan || null).catch(() => null) : plan,
  ]);
  if (epoch === sessionEpoch) { appCount = count; plan = current; }
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
  const path = raw.replace(/\/+$/, '').toLowerCase() || '/perfil';
  const match = ROUTES.map((r) => ({ r, m: path.match(r.path) })).find((x) => x.m);
  if (!match) {
    toast('Não encontramos essa página. Levamos você para o início.');
    return replaceRoute(isLoggedIn() ? '#/perfil' : '#/entrar');
  }
  const { r, m } = match;
  const isPublic = r.layout === 'public';

  if (!isPublic && !isLoggedIn()) {
    rememberRoute(location.hash);
    return replaceRoute('#/entrar');
  }
  if (isPublic && isLoggedIn()) return replaceRoute('#/perfil');

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
  if (withNav) renderSidebar(r.nav); // o menu e o Sair existem mesmo se /me falhar ou demorar

  let failed = false;
  try {
    if (!isPublic) {
      await loadMe();
      if (r.layout !== 'full' && (await needsOnboarding())) { if (seq === renderSeq) go('#/boas-vindas'); return; }
      if (seq !== renderSeq) return;
      if (withNav) {
        renderSidebar(r.nav);
        loadShellData().then(() => { if (seq === renderSeq) renderSidebar(r.nav); });
      }
    }
    if (seq !== renderSeq) return;
    await r.view.render(stage, { ...(r.params ? r.params(m) : {}), me, refreshShell });
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

/** Telas chamam depois de mudar plano ou candidaturas, para o menu refletir. */
function refreshShell({ applications: a, billing } = {}) {
  if (a) appCount = null;
  if (billing) plan = null;
  loadShellData().then(() => {
    const active = $('#sidebar .zy-nav__item.is-active')?.dataset.nav;
    if (!$('#sidebar').hidden) renderSidebar(active);
  });
}

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
  me = null; appCount = null; plan = null;
  try {
    sessionStorage.removeItem(OB_CHECK);
    // Sair não deixa dado pessoal na aba. Em sessão expirada, o rascunho fica para a mesma pessoa retomar.
    if (reason !== 'expired') {
      for (const key of Object.keys(sessionStorage)) {
        if (/^zyft\.(importReview|onboarding\.draft)\./.test(key)) sessionStorage.removeItem(key);
      }
    }
  } catch { /* ok */ }
  closeModals();
  if (reason === 'expired' && !isAuthRoute()) rememberRoute(location.hash);
  if (!isAuthRoute()) replaceRoute('#/entrar');
});

let currentHash = location.hash;
window.addEventListener('hashchange', async () => {
  const target = location.hash;
  if (target === currentHash) return route(); // go() para a própria rota: recarrega
  if (hasUnsavedModal()) {
    // A URL volta para a tela do formulário enquanto a pessoa decide (vale para o botão Voltar).
    history.replaceState(null, '', currentHash || '#/perfil');
    if (!(await confirmDiscard())) return;
    closeModals();
    currentHash = target;
    location.hash = target;
    return;
  }
  closeModals();
  currentHash = target;
  route();
});
route();

