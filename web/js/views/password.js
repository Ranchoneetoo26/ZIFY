// Senha: campos compartilhados (troca na Conta e redefinição), "Esqueci minha senha" (#/esqueci-senha) e
// "Redefinir senha" (#/redefinir-senha?token=…). As duas telas públicas funcionam sem login.
import { api, forgotPassword, resetPassword, isLoggedIn } from '../api.js';
import { h, clear, field, readForm, showErrors, clearErrors, announce, busy, replaceRoute } from '../dom.js';
import { Icon, Logo, Button, LinkButton } from '../ui.js';
import * as v from '../validate.js';
import { flashLogin } from './auth.js';

// Mesma regra de StrongPassword na API (Validation.cs): 8 a 128 caracteres, uma letra e um número.
export const PASSWORD_RULE = 'De 8 a 128 caracteres, com pelo menos uma letra e um número.';

/**
 * Campos de senha nova (e, se pedido, a atual) com um botão "Mostrar senhas" que vale para todos.
 * Sem maxlength: o navegador cortaria a senha colada em silêncio; a validação avisa acima de 128.
 */
export function passwordFields({ current = false } = {}) {
  const nodes = [];
  if (current) nodes.push(field({ name: 'currentPassword', label: 'Senha atual', type: 'password', required: true, attrs: { autocomplete: 'current-password' } }));
  nodes.push(
    field({ name: 'newPassword', label: 'Nova senha', type: 'password', required: true, hint: PASSWORD_RULE, attrs: { autocomplete: 'new-password' } }),
    field({ name: 'confirmPassword', label: 'Confirme a nova senha', type: 'password', required: true, attrs: { autocomplete: 'new-password' } }));
  const inputs = nodes.map((n) => n.querySelector('input'));
  const toggle = Button({ variant: 'quiet', size: 'sm', icon: 'eye', 'aria-pressed': 'false', className: 'pw-toggle' }, 'Mostrar senhas');
  toggle.addEventListener('click', () => {
    const show = toggle.getAttribute('aria-pressed') !== 'true';
    toggle.setAttribute('aria-pressed', String(show));
    toggle.lastChild.textContent = show ? 'Ocultar senhas' : 'Mostrar senhas';
    for (const i of inputs) i.type = show ? 'text' : 'password';
    announce(show ? 'Senhas visíveis.' : 'Senhas ocultas.');
  });
  return { nodes: [...nodes, toggle], inputs, hide: () => { if (toggle.getAttribute('aria-pressed') === 'true') toggle.click(); } };
}

/** Validação local dos campos de senha nova: mesmas mensagens que a pessoa veria da API. */
export function checkNewPassword(form) {
  const pw = form.elements.newPassword.value;
  const confirm = form.elements.confirmPassword.value;
  return v.collect({
    currentPassword: form.elements.currentPassword ? v.required(form.elements.currentPassword.value, 'Informe a senha atual.') : null,
    newPassword: v.password(pw),
    confirmPassword: !confirm ? 'Repita a nova senha.' : confirm !== pw ? 'As senhas não são iguais. Digite a mesma senha nos dois campos.' : null,
  });
}

const shell = (...children) => h('div', { class: 'auth' },
  h('section', { class: 'auth__panel' }, h('div', { class: 'auth__box' }, Logo({ height: 28 }), ...children)));

export async function render(main, { mode, query, me }) {
  if (mode !== 'reset') { main.append(forgotScreen()); return; }
  // Com login nesta aba, o link pode ser de outra conta (ex.: aberto no computador de outra pessoa): a tela diz com
  // qual conta a aba está e não encerra esta sessão. A API não diz de quem é o link.
  let email = null;
  if (isLoggedIn()) email = me?.email || (await api('/me').catch(() => null))?.email || null;
  main.append(resetScreen((query?.get('token') || '').trim(), { loggedAs: isLoggedIn() ? email || '' : null }));
}

function forgotScreen() {
  const submit = Button({ variant: 'primary', type: 'submit' }, 'Enviar link');
  const done = h('div', { role: 'status', 'aria-live': 'polite' });
  const form = h('form', { novalidate: true, 'aria-labelledby': 'pw-title' },
    h('h1', { id: 'pw-title', class: 'auth__h2' }, 'Esqueci minha senha'),
    h('p', { class: 'muted' }, 'Informe o e-mail da sua conta. Se ele estiver cadastrado, enviamos um link para criar uma senha nova.'),
    field({ name: 'email', label: 'E-mail', type: 'email', required: true, attrs: { autocomplete: 'email', inputmode: 'email', maxlength: 254 } }),
    submit, done,
    h('a', { href: '#/entrar', class: 'auth__link' }, 'Voltar para Entrar'));
  form.addEventListener('submit', (e) => {
    e.preventDefault();
    return busy(submit, async () => {
      clearErrors(form);
      done.replaceChildren();
      const data = readForm(form);
      const errors = v.collect({ email: v.email(data.email) });
      if (v.hasErrors(errors)) return showErrors(form, errors);
      try {
        await forgotPassword(data.email);
        // Mesma mensagem exista ou não a conta: ninguém descobre quem tem cadastro.
        done.replaceChildren(h('div', { class: 'notice notice--warm' }, Icon('check', { size: 16 }),
          h('p', {}, 'Se existir uma conta com esse e-mail, enviamos um link para redefinir a senha. Ele vale por 30 minutos. Confira também a caixa de spam.')));
        announce('Pedido enviado. Confira seu e-mail.');
      } catch (err) {
        if (err.status === 429) showErrors(form, null, 'Muitos pedidos em pouco tempo. Aguarde alguns minutos e tente de novo.');
        else showErrors(form, err);
      }
    });
  });
  return shell(form);
}

function resetScreen(token, { loggedAs = null } = {}) {
  if (!token) {
    return shell(h('h1', { class: 'auth__h2' }, 'Link incompleto'),
      h('div', { class: 'notice notice--error', role: 'alert' }, Icon('alert', { size: 16 }),
        h('p', {}, 'Este endereço não traz o código de redefinição. Abra o link do e-mail de novo ou peça um novo link.')),
      h('div', { class: 'auth__actions' }, LinkButton('#/esqueci-senha', { variant: 'primary' }, 'Pedir um novo link'), h('a', { href: '#/entrar', class: 'auth__link' }, 'Voltar para Entrar')));
  }
  const submit = Button({ variant: 'primary', type: 'submit' }, 'Salvar nova senha');
  const pw = passwordFields();
  const logged = loggedAs !== null;
  const form = h('form', { novalidate: true, 'aria-labelledby': 'pw-title' },
    h('h1', { id: 'pw-title', class: 'auth__h2' }, 'Crie uma senha nova'),
    logged ? h('div', { class: 'notice notice--care', role: 'note' }, Icon('alert', { size: 16 }),
      h('p', {}, loggedAs ? ['Você está conectado como ', h('b', {}, loggedAs), '. '] : 'Você está conectado nesta aba. ',
        'Redefinir a senha por este link vale para a conta dona do link.')) : null,
    h('p', { class: 'muted' }, 'Depois de salvar, todas as sessões abertas e a extensão da conta dona do link são desconectadas. Ela entra de novo com a senha nova.'),
    ...pw.nodes, submit,
    logged ? h('a', { href: '#/perfil', class: 'auth__link' }, 'Voltar para o Perfil Mestre') : h('a', { href: '#/entrar', class: 'auth__link' }, 'Voltar para Entrar'));
  form.addEventListener('submit', (e) => {
    e.preventDefault();
    return busy(submit, async () => {
      clearErrors(form);
      const errors = checkNewPassword(form);
      if (v.hasErrors(errors)) return showErrors(form, errors);
      try {
        await resetPassword(token, form.elements.newPassword.value);
        pw.hide();
        if (!isLoggedIn()) {
          flashLogin('Senha redefinida. Entre com a senha nova.');
          replaceRoute('#/entrar');
          return;
        }
        // Com login nesta aba, a sessão continua: se o link era desta mesma conta, a API já revogou o refresh token e a
        // próxima renovação leva para Entrar com o aviso de sessão encerrada.
        const done = h('div', { class: 'stack' },
          h('h1', { class: 'auth__h2', tabindex: '-1' }, 'Senha redefinida'),
          h('div', { class: 'notice notice--warm', role: 'status' }, Icon('check', { size: 16 }),
            h('p', {}, 'A conta dona do link já pode entrar com a senha nova. As sessões dela foram encerradas.')),
          h('div', { class: 'auth__actions' }, LinkButton('#/perfil', { variant: 'primary' }, 'Ir para o Perfil Mestre')));
        clear(form.parentElement, Logo({ height: 28 }), done);
        done.querySelector('h1').focus();
        announce('Senha redefinida.');
      } catch (err) {
        if (err.errors?.token) {
          // Link vencido ou já usado: a mensagem fica no topo com o caminho para pedir outro.
          const { token: tokenMsgs, ...rest } = err.errors;
          showErrors(form, rest, null);
          const box = h('div', { class: 'notice notice--error form-error', role: 'alert', tabindex: '-1' },
            h('p', {}, tokenMsgs.join(' ')), LinkButton('#/esqueci-senha', { variant: 'secondary', size: 'sm' }, 'Pedir um novo link'));
          form.querySelector('.form-error')?.remove();
          form.prepend(box);
          if (!Object.keys(rest).length) box.focus();
          announce(tokenMsgs.join(' '));
        } else showErrors(form, err);
      }
    });
  });
  return shell(form);
}
