// Entrar e criar conta (RF-01). Sem anúncios (contrato v2).
import { login, register, getTermsVersion, getSessionNotice, takeReturnRoute } from '../api.js';
import { h, clear, field, readForm, showErrors, clearErrors, go, replaceRoute, announce, busy } from '../dom.js';
import { Icon, Logo, Button } from '../ui.js';
import * as v from '../validate.js';

// Mais estrito que o login, que precisa aceitar contas já criadas: recusa ponto solto, "<b>",
// emoji e caracteres invisíveis.
const ATOM = "[\\p{L}\\p{N}!#$%&'*+/=?^_`{|}~-]+";
const LABEL = '[\\p{L}\\p{N}](?:[\\p{L}\\p{N}-]*[\\p{L}\\p{N}])?';
const SIGNUP_EMAIL_RE = new RegExp(`^${ATOM}(?:\\.${ATOM})*@${LABEL}(?:\\.${LABEL})*\\.\\p{L}{2,}$`, 'u');
const signupEmail = (value) => v.email(value) || (SIGNUP_EMAIL_RE.test(value) ? null : 'Informe um e-mail válido, como nome@exemplo.com.');

/** Precisa de ao menos uma letra ou número: só espaços ou caracteres invisíveis não formam um nome. */
const signupName = (value) => (/[\p{L}\p{N}]/u.test(value || '') ? null : 'Informe seu nome.');

// O que já foi digitado sobrevive à troca entre Entrar e Criar conta. A senha nunca fica guardada.
let draft = { name: '', email: '' };
const keepDraft = (form) => {
  form.addEventListener('input', () => {
    draft = { name: form.elements.name?.value ?? draft.name, email: form.elements.email.value };
  });
  return form;
};

export function render(main, { mode }) {
  const panel = h('div', {});
  const tabLogin = h('a', { href: '#/entrar', role: 'tab', class: 'tab', id: 'tab-login', 'aria-controls': 'auth-panel' }, 'Entrar');
  const tabSignup = h('a', { href: '#/cadastrar', role: 'tab', class: 'tab', id: 'tab-signup', 'aria-controls': 'auth-panel' }, 'Criar conta');
  const isRegister = mode === 'register';
  tabLogin.setAttribute('aria-selected', String(!isRegister));
  tabSignup.setAttribute('aria-selected', String(isRegister));
  const tabs = h('div', { role: 'tablist', 'aria-label': 'Acesso', class: 'tabs' }, tabLogin, tabSignup);
  tabs.addEventListener('keydown', (e) => {
    if (e.key === 'ArrowRight' || e.key === 'ArrowLeft') { e.preventDefault(); (isRegister ? tabLogin : tabSignup).click(); }
  });

  clear(panel, keepDraft(isRegister ? signupForm() : loginForm()));
  panel.id = 'auth-panel';
  panel.setAttribute('role', 'tabpanel');
  panel.setAttribute('aria-labelledby', isRegister ? 'tab-signup' : 'tab-login');

  main.append(h('div', { class: 'auth' },
    h('section', { class: 'auth__brand', 'aria-label': 'Sobre o Zyft' },
      Logo({ onDark: true, height: 32 }),
      h('div', { class: 'auth__hero' },
        h('p', { class: 'auth__title' }, 'Você no controle.'),
        h('p', { class: 'auth__lead' }, 'Monte seu Perfil Mestre uma vez. A extensão propõe o preenchimento dos formulários de vaga, e você confere e aprova cada campo.'),
        h('ul', { class: 'auth__points' },
          h('li', {}, Icon('check'), h('span', {}, 'Só preenche o que você aprovou')),
          h('li', {}, Icon('shield'), h('span', {}, 'Nunca pede a senha dos portais')),
          h('li', {}, Icon('hand'), h('span', {}, 'Nunca envia uma candidatura por você')))),
      h('div', { class: 'auth__foot' }, 'Zyft · Candidatura Assistida')),
    h('section', { class: 'auth__panel' },
      h('div', { class: 'auth__box' },
        tabs, panel,
        h('p', { class: 'auth__note' }, Icon('shield', { size: 14 }), h('span', {}, 'O Zyft nunca pede a senha dos portais e nunca envia uma candidatura por você.'))))));
}

// Aviso de uma tela pública para o próximo Entrar (ex.: "Senha redefinida"), mostrado uma vez.
let flash = '';
export const flashLogin = (msg) => { flash = msg || ''; };

function loginForm() {
  const submit = Button({ variant: 'primary', type: 'submit' }, 'Entrar');
  const notice = getSessionNotice();
  const done = flash;
  flash = '';
  const form = h('form', { novalidate: true, 'aria-labelledby': 'auth-title' },
    h('h1', { id: 'auth-title', class: 'auth__h2' }, 'Que bom ver você de novo'),
    done ? h('p', { class: 'notice notice--warm', role: 'status' }, Icon('check', { size: 14 }), h('span', {}, done)) : null,
    notice && !done ? h('p', { class: 'notice notice--care', role: 'status' }, Icon('alert', { size: 14 }), h('span', {}, notice)) : null,
    field({ name: 'email', label: 'E-mail', type: 'email', required: true, value: draft.email, attrs: { autocomplete: 'email', inputmode: 'email', maxlength: 254 } }),
    field({ name: 'password', label: 'Senha', type: 'password', required: true, attrs: { autocomplete: 'current-password' } }),
    h('a', { href: '#/esqueci-senha', class: 'auth__link' }, 'Esqueci minha senha'),
    submit);
  form.addEventListener('submit', (e) => {
    e.preventDefault();
    return busy(submit, async () => {
    clearErrors(form);
    const data = readForm(form);
    const errors = v.collect({ email: v.email(data.email), password: v.required(data.password, 'Informe sua senha.') });
    if (v.hasErrors(errors)) return showErrors(form, errors);
    announce('Entrando…');
    try {
      await login(data.email, form.elements.password.value);
      draft = { name: '', email: '' };
      // Substitui #/entrar no histórico: Voltar não cai de novo no login.
      replaceRoute(takeReturnRoute() || '#/perfil');
    } catch (err) {
      // O 401 não diz qual dos dois campos está errado: o aviso vale para o formulário inteiro.
      if (err.status === 401) showErrors(form, null, 'E-mail ou senha incorretos. Confira e tente de novo.');
      else showErrors(form, err);
    }
    });
  });
  return form;
}

// Versão dos termos que a pessoa está aceitando: lida ao abrir o cadastro e de novo se a API disser que mudou.
let termsVersion = null;
const loadTerms = () => {
  termsVersion = getTermsVersion().catch((err) => { termsVersion = null; throw err; });
  return termsVersion;
};

function signupForm() {
  const submit = Button({ variant: 'primary', type: 'submit' }, 'Criar conta');
  // name = acceptTerms: o erro da API (errors.acceptTerms) cai embaixo da caixa, pelo aria-describedby.
  const terms = h('input', { type: 'checkbox', id: 'terms', name: 'acceptTerms', 'aria-describedby': 'terms-err' });
  const termsErr = h('p', { class: 'zy-tf__error', id: 'terms-err', hidden: true });
  const legalLink = (href, text) => h('a', { href, target: '_blank', rel: 'noopener' }, text, h('span', { class: 'sr-only' }, ' (abre em nova aba)'));
  terms.addEventListener('change', () => {
    termsErr.hidden = true;
    terms.removeAttribute('aria-invalid');
  });
  if (!termsVersion) loadTerms().catch(() => {}); // falha aqui é tentada de novo ao enviar
  const form = h('form', { novalidate: true, 'aria-labelledby': 'auth-title' },
    h('h1', { id: 'auth-title', class: 'auth__h2' }, 'Crie sua conta'),
    field({ name: 'name', label: 'Nome', required: true, value: draft.name, attrs: { autocomplete: 'name', maxlength: 120 } }),
    field({ name: 'email', label: 'E-mail', type: 'email', required: true, value: draft.email, placeholder: 'voce@email.com', attrs: { autocomplete: 'email', inputmode: 'email', maxlength: 254 } }),
    // Sem maxlength: o navegador cortaria a senha colada em silêncio. A validação avisa acima de 128.
    field({ name: 'password', label: 'Senha', type: 'password', required: true, hint: 'Pelo menos 8 caracteres, com letra e número.', attrs: { autocomplete: 'new-password' } }),
    h('div', {},
      h('label', { class: 'check', for: 'terms' }, terms, h('span', {}, 'Li e aceito os ', legalLink('legal/termos.html', 'Termos de uso'), ' e a ', legalLink('legal/privacidade.html', 'Política de privacidade'), '.')),
      termsErr),
    submit);
  const termsError = (msg) => showErrors(form, { acceptTerms: [msg] });
  form.addEventListener('submit', (e) => {
    e.preventDefault();
    return busy(submit, async () => {
    clearErrors(form);
    const data = readForm(form);
    const errors = v.collect({
      name: signupName(data.name), email: signupEmail(data.email), password: v.password(form.elements.password.value),
      acceptTerms: terms.checked ? null : 'Para criar a conta, marque que você leu e aceita os Termos de uso e a Política de privacidade.',
    });
    if (v.hasErrors(errors)) return showErrors(form, errors);
    let version;
    try { version = await (termsVersion || loadTerms()); } catch {
      return termsError('Não foi possível carregar a versão atual dos termos. Verifique sua conexão e tente de novo.');
    }
    announce('Criando sua conta…');
    try {
      await register(data.name, data.email, form.elements.password.value, { acceptTerms: true, termsVersion: version });
      draft = { name: '', email: '' };
      takeReturnRoute();
      try { sessionStorage.setItem('zyft.obChecked', '1'); } catch { /* ok */ }
      go('#/boas-vindas');
    } catch (err) {
      if (err.status === 409) return showErrors(form, { email: [err.message] });
      const termsMsg = err.errors?.termsVersion;
      if (termsMsg) {
        // "Informe a versão" ou "termos atualizados": nos dois casos lemos a versão de novo e a pessoa aceita outra vez.
        // Os termos mudaram enquanto a página estava aberta: a pessoa precisa ler e aceitar a versão nova.
        terms.checked = false;
        loadTerms().catch(() => {});
        const { termsVersion: _, ...rest } = err.errors;
        return showErrors(form, { ...rest, acceptTerms: ['Os termos foram atualizados enquanto esta página estava aberta. Abra os links acima, leia a versão atual e marque a caixa de novo.'] });
      }
      showErrors(form, err);
    }
    });
  });
  return form;
}
