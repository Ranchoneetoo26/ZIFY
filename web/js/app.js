// Shell da SPA: roteamento por hash, guarda de autenticação, menu lateral (AppNav) e foco acessível.
import { api, asList, isLoggedIn, logout, onAuthChange } from './api.js';
import { $, h, clear, announce, errorBox, loading } from './dom.js';
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

async function loadMe(force = false) {
  if (me && !force) return me;
  me = await api('/me');
  return me;
}

/** Contador de candidaturas e plano para o menu (falhas não impedem a tela). */
async function loadShellData() {
  if (appCount === null) appCount = await api('/applications').then((r) => asList(r).length).catch(() => undefined);
  if (plan === null) plan = await getBilling().then((b) => b?.plan || undefined).catch(() => undefined);
}

function renderSidebar(active) {
  const sb = $('#sidebar');
  const items = NAV.map((it) => (it.id === 'candidaturas' && appCount ? { ...it, count: appCount } : it));
  const menuBtn = Button({ variant: 'quiet', size: 'sm', icon: 'menu', className: 'sidebar__menu', 'aria-expanded': String(sb.classList.contains('is-open')), 'aria-controls': 'sidebar-nav' }, 'Menu');
  menuBtn.addEventListener('click', () => {
    const open = !sb.classList.contains('is-open');
    sb.classList.toggle('is-open', open);
    menuBtn.setAttribute('aria-expanded', String(open));
  });
  const out = Button({ variant: 'ghost', size: 'sm', icon: 'logout' }, 'Sair');
  out.addEventListener('click', async () => {
    await logout();
    me = null; appCount = null; plan = null;
    try { sessionStorage.removeItem(OB_CHECK); } catch { /* ok */ }
    go('#/entrar');
  });
  clear(sb,
    h('div', { class: 'sidebar__top' }, h('a', { href: '#/perfil', 'aria-label': 'Zyft, ir para o Perfil Mestre' }, Logo()), menuBtn),
    h('div', { class: 'sidebar__nav', id: 'sidebar-nav' },
      AppNav({ items, active }),
      h('div', { class: 'sidebar__user' },
        h('span', { class: 'muted' }, 'Você entrou como'),
        h('b', {}, me?.name || me?.email || ''),
        plan ? h('a', { class: 'sidebar__plan', href: '#/planos' }, plan === 'plus' ? 'Zyft Plus' : 'Plano gratuito') : null,
        out)));
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
  const path = location.hash.replace(/^#/, '') || '/perfil';
  const match = ROUTES.map((r) => ({ r, m: path.match(r.path) })).find((x) => x.m);
  if (!match) return go(isLoggedIn() ? '#/perfil' : '#/entrar');
  const { r, m } = match;
  const isPublic = r.layout === 'public';

  if (!isPublic && !isLoggedIn()) return go('#/entrar');
  if (isPublic && isLoggedIn()) return go('#/perfil');

  const seq = ++renderSeq;
  const main = $('#main');
  const app = $('#app');
  const sb = $('#sidebar');
  const withNav = r.layout !== 'public' && r.layout !== 'full';
  app.classList.toggle('app--public', !withNav);
  sb.hidden = !withNav;
  sb.classList.remove('is-open');
  document.title = `${r.title} · Zyft`;
  clear(main, withNav ? h('div', { class: 'content' }, loading()) : loading());

  try {
    if (!isPublic) {
      await loadMe();
      if (r.layout !== 'full' && (await needsOnboarding())) { if (seq === renderSeq) go('#/boas-vindas'); return; }
      if (withNav) { await loadShellData(); if (seq === renderSeq) renderSidebar(r.nav); }
    }
    if (seq !== renderSeq) return;
    clear(main);
    await r.view.render(main, { ...(r.params ? r.params(m) : {}), me, refreshShell });
  } catch (err) {
    if (seq !== renderSeq) return;
    if (err.status === 401) return go('#/entrar');
    clear(main, h('div', { class: 'content' }, errorBox(err, route)));
  }
  if (seq !== renderSeq) return;
  // Leva o foco para o título da tela nova (teclado e leitores de tela).
  const h1 = main.querySelector('h1');
  if (h1) { h1.setAttribute('tabindex', '-1'); h1.focus({ preventScroll: true }); }
  window.scrollTo(0, 0);
  announce(`${r.title}. Tela carregada.`);
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

onAuthChange((logged) => {
  if (!logged) { me = null; appCount = null; plan = null; if (!location.hash.match(/entrar|cadastrar/)) go('#/entrar'); }
});

window.addEventListener('hashchange', route);
route();

