// Confirmação de e-mail (#/verificar?token=…). Funciona com ou sem login. E-mail não confirmado
// nunca bloqueia nada: é só um aviso (decisão de produto).
import { verifyEmail, resendVerification, isLoggedIn, ApiError } from '../api.js';
import { h } from '../dom.js';
import { Icon, Logo, LinkButton } from '../ui.js';

// Um token vale uma vez: a mesma tela desenhada de novo (ex.: Voltar) reaproveita o resultado em vez de
// mandar o token já usado e mostrar "link inválido" para quem acabou de confirmar.
const attempts = new Map();
const attempt = (token) => {
  if (!attempts.has(token)) attempts.set(token, verifyEmail(token).then(() => null, (err) => err));
  return attempts.get(token);
};

export async function render(main, { query, refreshMe }) {
  const token = (query?.get('token') || '').trim();
  const logged = isLoggedIn();
  const next = logged
    ? LinkButton('#/perfil', { variant: 'primary' }, 'Ir para o Perfil Mestre')
    : LinkButton('#/entrar', { variant: 'primary' }, 'Entrar');

  let title;
  let text;
  let ok = false;
  let actions = [next];
  if (!token) {
    title = 'Link incompleto';
    text = 'Este endereço não traz o código de confirmação. Abra o link do e-mail de novo ou peça um novo link.';
  } else {
    const err = await attempt(token);
    ok = !err;
    if (ok) {
      title = 'E-mail confirmado';
      text = 'Pronto: seu e-mail está confirmado. Você não precisa fazer mais nada.';
      if (logged) await refreshMe?.().catch(() => {});
    } else {
      title = 'Não foi possível confirmar';
      text = err.errors?.token?.join(' ') || err.message;
    }
  }
  if (!ok) {
    actions = logged
      ? [LinkButton('#/conta', { variant: 'primary' }, 'Pedir um novo link em Conta'), LinkButton('#/perfil', { variant: 'quiet' }, 'Ir para o Perfil Mestre')]
      : [next, h('p', { class: 'muted', style: { fontSize: '14px' } }, 'Depois de entrar, peça um novo link em Conta e privacidade.')];
  }

  main.append(h('div', { class: 'auth' },
    h('section', { class: 'auth__panel' },
      h('div', { class: 'auth__box' },
        Logo({ height: 28 }),
        h('h1', { class: 'auth__h2' }, title),
        h('div', { class: `notice ${ok ? 'notice--warm' : 'notice--error'}`, role: ok ? 'status' : 'alert' },
          Icon(ok ? 'check' : 'alert', { size: 16 }), h('p', {}, text)),
        h('div', { class: 'auth__actions' }, actions)))));
}

/**
 * Pede um novo link de confirmação. Devolve { kind: 'ok'|'verified'|'err', message } para a tela mostrar
 * no próprio aviso (sem toast solto).
 */
export async function resendLink() {
  try {
    await resendVerification();
    return { kind: 'ok', message: 'Enviamos um novo link para o seu e-mail. Ele vale por 24 horas; o link anterior deixou de valer.' };
  } catch (err) {
    if (err instanceof ApiError && err.status === 409) return { kind: 'verified', message: 'Seu e-mail já está confirmado.' };
    if (err instanceof ApiError && err.status === 429) return { kind: 'err', message: 'Você pediu muitos links em pouco tempo. Aguarde alguns minutos e tente de novo.' };
    return { kind: 'err', message: err.message || 'Não foi possível enviar o link agora. Tente de novo.' };
  }
}
