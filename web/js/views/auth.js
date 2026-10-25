// Entrar e criar conta (RF-01). Sem anúncios (contrato v2).
import { login, register } from '../api.js';
import { h, clear, field, readForm, showErrors, clearErrors, go, announce } from '../dom.js';
import { Icon, Logo, Button } from '../ui.js';
import * as v from '../validate.js';

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

  clear(panel, isRegister ? signupForm() : loginForm());
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

function loginForm() {
  const submit = Button({ variant: 'primary', type: 'submit' }, 'Entrar');
  const form = h('form', { novalidate: true, 'aria-labelledby': 'auth-title' },
    h('h1', { id: 'auth-title', class: 'auth__h2' }, 'Que bom ver você de novo'),
    field({ name: 'email', label: 'E-mail', type: 'email', required: true, attrs: { autocomplete: 'email', inputmode: 'email' } }),
    field({ name: 'password', label: 'Senha', type: 'password', required: true, attrs: { autocomplete: 'current-password' } }),
    submit);
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    clearErrors(form);
    const data = readForm(form);
    const errors = v.collect({ email: v.email(data.email), password: v.required(data.password, 'Informe sua senha.') });
    if (v.hasErrors(errors)) return showErrors(form, errors);
    submit.disabled = true;
    announce('Entrando…');
    try {
      await login(data.email, form.elements.password.value);
      go('#/perfil');
    } catch (err) {
      if (err.status === 401 || err.status === 400) showErrors(form, { password: ['E-mail ou senha incorretos. Confira e tente de novo.'] });
      else showErrors(form, err);
    } finally { submit.disabled = false; }
  });
  return form;
}

function signupForm() {
  const submit = Button({ variant: 'primary', type: 'submit' }, 'Criar conta');
  const terms = h('input', { type: 'checkbox', id: 'terms', name: 'terms', 'aria-describedby': 'terms-err' });
  const termsErr = h('p', { class: 'zy-tf__error', id: 'terms-err', hidden: true });
  const form = h('form', { novalidate: true, 'aria-labelledby': 'auth-title' },
    h('h1', { id: 'auth-title', class: 'auth__h2' }, 'Crie sua conta'),
    field({ name: 'name', label: 'Nome', required: true, attrs: { autocomplete: 'name', maxlength: 120 } }),
    field({ name: 'email', label: 'E-mail', type: 'email', required: true, placeholder: 'voce@email.com', attrs: { autocomplete: 'email', inputmode: 'email' } }),
    field({ name: 'password', label: 'Senha', type: 'password', required: true, hint: 'Pelo menos 8 caracteres, com letra e número.', attrs: { autocomplete: 'new-password' } }),
    h('div', {},
      h('label', { class: 'check', for: 'terms' }, terms, h('span', {}, 'Li e aceito os termos de uso e a política de privacidade do Zyft.')),
      termsErr),
    submit);
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    clearErrors(form);
    termsErr.hidden = true;
    const data = readForm(form);
    const errors = v.collect({ name: v.required(data.name, 'Informe seu nome.'), email: v.email(data.email), password: v.password(form.elements.password.value) });
    if (v.hasErrors(errors)) return showErrors(form, errors);
    if (!terms.checked) {
      termsErr.textContent = 'Para criar a conta, marque que você leu e aceita os termos.';
      termsErr.hidden = false;
      terms.setAttribute('aria-invalid', 'true');
      terms.focus();
      return;
    }
    submit.disabled = true;
    announce('Criando sua conta…');
    try {
      await register(data.name, data.email, form.elements.password.value);
      try { sessionStorage.setItem('zyft.obChecked', '1'); } catch { /* ok */ }
      go('#/boas-vindas');
    } catch (err) {
      showErrors(form, err);
    } finally { submit.disabled = false; }
  });
  return form;
}
