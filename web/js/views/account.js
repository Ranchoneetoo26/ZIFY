// Conta e privacidade: dados da conta, consentimentos (RF-07), exportar e excluir (RF-08). Sem anúncios.
import { api, download, setTokens } from '../api.js';
import { h, toast, confirmDialog, fmtDate, go, nextId } from '../dom.js';
import { Button, PageHead, ConsentRow } from '../ui.js';
import { consentRows } from './consents.js';

export async function render(main) {
  const [me, rows] = await Promise.all([api('/me'), consentRows()]);

  const exportBtn = Button({ variant: 'secondary', icon: 'copy' }, 'Baixar meus dados (JSON)');
  exportBtn.addEventListener('click', async () => {
    exportBtn.disabled = true;
    try {
      await download('/me/export', `zyft-meus-dados-${new Date().toISOString().slice(0, 10)}.json`);
      toast('Arquivo gerado', 'ok');
    } catch (err) { if (!err.handled) toast(err.message, 'err'); } finally { exportBtn.disabled = false; }
  });

  const confirmId = nextId('del');
  const confirmInput = h('input', { id: confirmId, class: 'zy-tf__input', placeholder: 'EXCLUIR', autocomplete: 'off', 'aria-describedby': `${confirmId}-hint` });
  const delBtn = Button({ variant: 'danger', disabled: true }, 'Excluir conta');
  confirmInput.addEventListener('input', () => { delBtn.disabled = confirmInput.value.trim() !== 'EXCLUIR'; });
  delBtn.addEventListener('click', async () => {
    const ok = await confirmDialog({ title: 'Excluir sua conta agora?', message: 'Esta é a última confirmação. Seus dados serão apagados e não dá para desfazer.', confirmText: 'Excluir definitivamente', danger: true });
    if (!ok) return;
    try {
      await api('/me', { method: 'DELETE' });
      setTokens({});
      go('#/entrar');
      toast('Conta excluída. Seus dados foram apagados.');
    } catch (err) { if (!err.handled) toast(err.message, 'err'); }
  });

  const info = (k, v) => h('div', { class: 'kv' }, h('span', { class: 'kv__k' }, k), h('span', { class: 'kv__v', style: { fontSize: '15px' } }, v || '—'));
  main.append(h('div', { class: 'content', style: { maxWidth: '820px' } },
    PageHead({ title: 'Conta e privacidade', lead: 'Seus dados são seus. Escolha o que o Zyft pode fazer, baixe tudo ou apague a conta.' }),
    h('section', { class: 'card', 'aria-label': 'Sua conta' }, h('div', { class: 'kv-grid' },
      info('Nome', me.name), info('E-mail de acesso', me.email), info('Conta criada em', me.createdAt ? fmtDate(me.createdAt) : null))),
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
