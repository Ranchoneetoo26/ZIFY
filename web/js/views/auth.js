// Entrar e criar conta (RF-01).
import { login, register } from '../api.js';
import { h, field, readForm, showErrors, clearErrors, go, announce } from '../dom.js';
import * as v from '../validate.js';

export function render(main, { mode }) {
  const isRegister = mode === 'register';
  const submit = h('button', { class: 'primary', type: 'submit' }, isRegister ? 'Criar conta' : 'Entrar');

  const form = h('form', { novalidate: true, 'aria-labelledby': 'auth-title' },
    isRegister ? field({ name: 'name', label: 'Seu nome', required: true, attrs: { autocomplete: 'name', maxlength: 120 } }) : null,
    field({ name: 'email', label: 'E-mail', type: 'email', required: true, attrs: { autocomplete: 'email', inputmode: 'email' } }),
    field({
      name: 'password', label: 'Senha', type: 'password', required: true,
      hint: isRegister ? 'Pelo menos 8 caracteres, com letra e número.' : null,
      attrs: { autocomplete: isRegister ? 'new-password' : 'current-password' },
    }),
    isRegister ? h('p', { class: 'small muted' },
      'Ao criar a conta, você concorda com os termos de uso e a política de privacidade. ',
      'O Zyft nunca pede a senha dos portais de vagas e nunca envia uma candidatura por você.') : null,
    h('div', { class: 'form-actions' }, submit));

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    clearErrors(form);
    const data = readForm(form);
    const errors = v.collect({
      name: isRegister ? v.required(data.name, 'Informe seu nome.') : null,
      email: v.email(data.email),
      password: isRegister ? v.password(data.password) : v.required(data.password, 'Informe sua senha.'),
    });
    if (v.hasErrors(errors)) return showErrors(form, errors);

    submit.disabled = true;
    announce(isRegister ? 'Criando sua conta…' : 'Entrando…');
    try {
      if (isRegister) {
        await register(data.name, data.email, form.elements.password.value);
        go('#/boas-vindas');
      } else {
        await login(data.email, form.elements.password.value);
        go('#/perfil');
      }
    } catch (err) {
      showErrors(form, err, err.status === 401 ? 'E-mail ou senha incorretos.' : err.message);
    } finally {
      submit.disabled = false;
    }
  });

  main.append(h('section', { class: 'auth' },
    h('div', { class: 'brand' }, h('span', { class: 'logo', 'aria-hidden': 'true' }, 'Z'), 'Zyft'),
    h('div', { class: 'card' },
      h('h1', { id: 'auth-title' }, isRegister ? 'Crie sua conta' : 'Entre na sua conta'),
      h('p', { class: 'lead' }, 'Seu copiloto para candidaturas: você prepara o Perfil Mestre uma vez, e a extensão sugere o preenchimento. Quem decide e quem envia é sempre você.'),
      h('nav', { class: 'tabs-auth', 'aria-label': 'Entrar ou criar conta' },
        h('a', { href: '#/entrar', 'aria-current': isRegister ? null : 'page' }, 'Entrar'),
        h('a', { href: '#/cadastrar', 'aria-current': isRegister ? 'page' : null }, 'Criar conta')),
      form)));
}
