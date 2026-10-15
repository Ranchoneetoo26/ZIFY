// Shell da SPA: roteamento por hash, guarda de autenticação e navegação acessível.
import { api, isLoggedIn, logout, onAuthChange } from './api.js';
import { $, $$, clear, announce, errorBox, loading } from './dom.js';
import * as auth from './views/auth.js';
import * as onboarding from './views/onboarding.js';
import * as profile from './views/profile.js';
import * as importer from './views/import.js';
import * as answers from './views/answers.js';
import * as extension from './views/extension.js';
import * as applications from './views/applications.js';
import * as audit from './views/audit.js';
import * as account from './views/account.js';

const ROUTES = [
  { path: /^\/entrar$/, view: auth, title: 'Entrar', public: true, params: () => ({ mode: 'login' }) },
  { path: /^\/cadastrar$/, view: auth, title: 'Criar conta', public: true, params: () => ({ mode: 'register' }) },
  { path: /^\/boas-vindas$/, view: onboarding, title: 'Boas-vindas' },
  { path: /^\/perfil$/, view: profile, title: 'Perfil Mestre', nav: '#/perfil' },
  { path: /^\/importar$/, view: importer, title: 'Importar currículo', nav: '#/importar' },
  { path: /^\/respostas$/, view: answers, title: 'Respostas aprovadas', nav: '#/respostas' },
  { path: /^\/extensao$/, view: extension, title: 'Extensão', nav: '#/extensao' },
  { path: /^\/candidaturas$/, view: applications, title: 'Candidaturas', nav: '#/candidaturas' },
  { path: /^\/candidaturas\/([\w-]+)$/, view: applications, title: 'Candidatura', nav: '#/candidaturas', params: (m) => ({ id: m[1] }) },
  { path: /^\/registro$/, view: audit, title: 'Registro de ações', nav: '#/registro' },
  { path: /^\/conta$/, view: account, title: 'Conta e privacidade', nav: '#/conta' },
];

let me = null;
async function loadMe(force = false) {
  if (me && !force) return me;
  me = await api('/me');
  $('#who-name').textContent = me?.name || me?.email || '';
  return me;
}

function go(hash) {
  if (location.hash === hash) route();
  else location.hash = hash;
}

let renderSeq = 0;
async function route() {
  const path = location.hash.replace(/^#/, '') || '/perfil';
  const match = ROUTES.map((r) => ({ r, m: path.match(r.path) })).find((x) => x.m);
  if (!match) return go('#/perfil');
  const { r, m } = match;

  if (!r.public && !isLoggedIn()) return go('#/entrar');
  if (r.public && isLoggedIn()) return go('#/perfil');

  const seq = ++renderSeq;
  const main = $('#main');
  $('#topbar').hidden = Boolean(r.public);
  for (const a of $$('#nav a')) {
    if (a.getAttribute('href') === r.nav) a.setAttribute('aria-current', 'page');
    else a.removeAttribute('aria-current');
  }
  document.title = `${r.title} — Zyft`;
  clear(main, loading());

  try {
    if (!r.public) await loadMe();
    if (seq !== renderSeq) return;
    clear(main);
    await r.view.render(main, r.params ? r.params(m) : {});
  } catch (err) {
    if (seq !== renderSeq) return;
    if (err.status === 401) return go('#/entrar');
    clear(main, errorBox(err, route));
  }
  if (seq !== renderSeq) return;
  // Leva o foco para o título da tela nova (navegação por teclado e leitores de tela).
  const h1 = main.querySelector('h1');
  if (h1) { h1.setAttribute('tabindex', '-1'); h1.focus({ preventScroll: true }); }
  window.scrollTo(0, 0);
  announce(`${r.title}. Tela carregada.`);
}

$('#logout').addEventListener('click', async () => {
  await logout();
  me = null;
  go('#/entrar');
});

onAuthChange((logged) => {
  if (!logged) { me = null; if (!location.hash.match(/entrar|cadastrar/)) go('#/entrar'); }
});

window.addEventListener('hashchange', route);
route();
