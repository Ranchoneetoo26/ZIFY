// Conta e privacidade: dados da conta, consentimentos (RF-07), exportar e excluir (RF-08). Sem anúncios.
import { api, download, setTokens, changePassword } from '../api.js';
import { h, clear, toast, confirmDialog, fmtDate, go, nextId, busy, showErrors, clearErrors, guardPage, announce } from '../dom.js';
import { Button, PageHead, ConsentRow, StatusBadge } from '../ui.js';
import { consentRows } from './consents.js';
import { resendLink } from './verify.js';
import { passwordFields, checkNewPassword } from './password.js';
import * as v from '../validate.js';

export async function render(main, { refreshMe } = {}) {
  const [me, rows] = await Promise.all([api('/me'), consentRows()]);

  const exportBtn = Button({ variant: 'secondary', icon: 'copy' }, 'Baixar meus dados (JSON)');
  exportBtn.addEventListener('click', () => busy(exportBtn, async () => {
    try {
      await download('/me/export', `zyft-meus-dados-${new Date().toISOString().slice(0, 10)}.json`);
      toast('Arquivo gerado', 'ok');
    } catch (err) { if (!err.handled) toast(err.message, 'err'); }
  }));

  const confirmId = nextId('del');
  const confirmInput = h('input', { id: confirmId, class: 'zy-tf__input', placeholder: 'EXCLUIR', autocomplete: 'off', 'aria-describedby': `${confirmId}-hint` });
  const delBtn = Button({ variant: 'danger', disabled: true }, 'Excluir conta');
  confirmInput.addEventListener('input', () => { delBtn.disabled = confirmInput.value.trim() !== 'EXCLUIR'; });
  confirmInput.addEventListener('keydown', (e) => { if (e.key === 'Enter' && !delBtn.disabled) { e.preventDefault(); delBtn.click(); } }); // sem preventDefault, o Enter ativaria o primeiro botão do diálogo
  delBtn.addEventListener('click', () => busy(delBtn, async () => {
    const ok = await confirmDialog({ title: 'Excluir sua conta agora?', message: 'Esta é a última confirmação. Seus dados serão apagados e não dá para desfazer.', confirmText: 'Excluir definitivamente', danger: true });
    if (!ok) return;
    try {
      await api('/me', { method: 'DELETE' });
      setTokens({});
      go('#/entrar');
      toast('Conta excluída. Seus dados foram apagados.');
    } catch (err) { if (!err.handled) toast(err.message, 'err'); }
  }));

  // Confirmação do e-mail: só informativa, nunca bloqueia nada.
  const verifyStatus = h('p', { class: 'muted', role: 'status', 'aria-live': 'polite', style: { fontSize: '14px', flexBasis: '100%' } });
  const verifyCell = h('div', { class: 'verify-status' });
  const paintVerify = (verified, at) => {
    if (verified) {
      clear(verifyCell, StatusBadge({ status: 'ready', label: 'Confirmado', size: 'sm' }), at ? h('span', { class: 'muted', style: { fontSize: '14px' } }, `em ${fmtDate(at)}`) : null, verifyStatus);
      return;
    }
    const resend = Button({ variant: 'secondary', size: 'sm' }, 'Reenviar link de confirmação');
    resend.addEventListener('click', () => busy(resend, async () => {
      verifyStatus.textContent = '';
      const r = await resendLink();
      if (r.kind === 'verified') {
        const fresh = await (refreshMe ? refreshMe() : api('/me')).catch(() => null);
        paintVerify(true, fresh?.emailVerifiedAt);
      }
      verifyStatus.textContent = r.message;
    }));
    clear(verifyCell, StatusBadge({ status: 'needs', label: 'Não confirmado', size: 'sm' }), resend, verifyStatus);
  };
  paintVerify(me.emailVerified !== false, me.emailVerifiedAt);

  // Alterar senha: a API devolve tokens novos (guardados por changePassword) e desconecta o resto.
  const pw = passwordFields({ current: true });
  const pwSubmit = Button({ variant: 'primary', type: 'submit' }, 'Alterar senha');
  const pwForm = h('form', { novalidate: true, class: 'stack pw-form', 'aria-labelledby': 'pw-title' },
    h('h2', { class: 'card__title', id: 'pw-title' }, 'Alterar senha'),
    h('p', { class: 'card__sub' }, 'Ao alterar, as outras sessões abertas e a extensão são desconectadas. Esta aba continua conectada.'),
    ...pw.nodes, h('div', {}, pwSubmit));
  guardPage(pwForm, () => pw.inputs.some((i) => i.value));
  pwForm.addEventListener('submit', (e) => {
    e.preventDefault();
    return busy(pwSubmit, async () => {
      clearErrors(pwForm);
      const errors = checkNewPassword(pwForm);
      if (v.hasErrors(errors)) return showErrors(pwForm, errors);
      try {
        await changePassword(pwForm.elements.currentPassword.value, pwForm.elements.newPassword.value);
        for (const i of pw.inputs) i.value = '';
        pw.hide();
        toast('Senha alterada. Outras sessões e a extensão foram desconectadas.', 'ok');
        announce('Senha alterada. Outras sessões e a extensão foram desconectadas.');
      } catch (err) {
        if (err.status === 429) showErrors(pwForm, null, 'Muitas tentativas em pouco tempo. Aguarde alguns minutos e tente de novo.');
        else if (!err.handled) showErrors(pwForm, err);
      }
    });
  });

  const info = (k, v) => h('div', { class: 'kv' }, h('span', { class: 'kv__k' }, k), h('span', { class: 'kv__v', style: { fontSize: '15px' } }, v || '—'));
  main.append(h('div', { class: 'content', style: { maxWidth: '820px' } },
    PageHead({ title: 'Conta e privacidade', lead: 'Seus dados são seus. Escolha o que o Zyft pode fazer, baixe tudo ou apague a conta.' }),
    h('section', { class: 'card', 'aria-label': 'Sua conta' }, h('div', { class: 'kv-grid' },
      info('Nome', me.name), info('E-mail de acesso', me.email), info('Conta criada em', me.createdAt ? fmtDate(me.createdAt) : null),
      h('div', { class: 'kv', style: { gridColumn: '1 / -1' } }, h('span', { class: 'kv__k' }, 'Confirmação do e-mail'), verifyCell))),
    h('section', { class: 'card', 'aria-labelledby': 'pw-title' }, pwForm),
    h('section', { class: 'card consents-card', 'aria-labelledby': 'cons-title' },
      h('h2', { class: 'card__title', id: 'cons-title' }, 'Consentimentos'),
      ConsentRow({ title: 'Conta e Perfil Mestre', purpose: 'Guardar seu perfil e preencher formulários só com o que você aprovou.', basis: 'Base legal: execução de contrato (art. 7º, V)', required: true, checked: true }),
      rows),
    h('section', { class: 'card', 'aria-labelledby': 'data-title', style: { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: '16px' } },
      h('div', { class: 'stack', style: { gap: '4px', flex: '1 1 360px' } },
        h('h2', { class: 'card__title', id: 'data-title' }, 'Baixar meus dados'),
        h('p', { class: 'card__sub' }, 'Um arquivo JSON com perfil, respostas, candidaturas, consentimentos e registro de ações.')),
      exportBtn),
    h('section', { class: 'card danger-card', 'aria-labelledby': 'del-title' },
      h('h2', { class: 'card__title', id: 'del-title' }, 'Excluir minha conta'),
      h('p', { style: { fontSize: '14px', lineHeight: '20px' } }, 'Apaga o Perfil Mestre, as respostas, as candidaturas e os dados sensíveis. As extensões conectadas perdem o acesso na hora. Não dá para desfazer.'),
      h('div', { class: 'row-end' },
        h('div', { class: 'zy-tf grow', style: { flexBasis: '280px' } },
          h('label', { class: 'zy-tf__label', for: confirmId }, 'Para confirmar, digite EXCLUIR'), confirmInput,
          h('p', { class: 'zy-tf__hint', id: `${confirmId}-hint` }, 'Isto evita exclusões por engano.')),
        delBtn))));
}
