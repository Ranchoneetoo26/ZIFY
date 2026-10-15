// Conta e privacidade: consentimentos (RF-07), exportar e excluir dados (RF-08).
import { api, download, setTokens } from '../api.js';
import { h, toast, confirmDialog, fmtDate, go } from '../dom.js';
import { consentsPanel } from './consents.js';

export async function render(main) {
  const [me, panel] = await Promise.all([api('/me'), consentsPanel()]);

  const exportBtn = h('button', { type: 'button' }, 'Baixar meus dados (JSON)');
  exportBtn.addEventListener('click', async () => {
    exportBtn.disabled = true;
    try {
      await download('/me/export', `zyft-meus-dados-${new Date().toISOString().slice(0, 10)}.json`);
      toast('Arquivo gerado', 'ok');
    } catch (err) { toast(err.message, 'err'); } finally { exportBtn.disabled = false; }
  });

  const deleteBtn = h('button', { type: 'button', class: 'danger' }, 'Excluir minha conta');
  deleteBtn.addEventListener('click', async () => {
    const ok = await confirmDialog({
      title: 'Excluir sua conta?',
      message: h('div', {},
        h('p', {}, 'Isto apaga de vez o Perfil Mestre, as respostas aprovadas, os dados sensíveis, as importações, os registros de candidaturas e o registro de ações.'),
        h('p', {}, 'As extensões pareadas perdem o acesso na hora. Se quiser uma cópia, baixe seus dados antes.')),
      confirmText: 'Excluir definitivamente',
      danger: true,
      typeToConfirm: 'EXCLUIR',
    });
    if (!ok) return deleteBtn.focus();
    try {
      await api('/me', { method: 'DELETE' });
      setTokens({});
      go('#/entrar');
      toast('Conta excluída. Seus dados foram apagados.');
    } catch (err) { toast(err.message, 'err'); }
  });

  main.append(
    h('div', { class: 'page-head' }, h('div', {},
      h('h1', {}, 'Conta e privacidade'),
      h('p', { class: 'lead' }, 'Seus dados são seus: decida o que o Zyft pode fazer, baixe uma cópia ou apague tudo.'))),
    h('section', { class: 'card', 'aria-labelledby': 'acc-title' },
      h('h2', { id: 'acc-title' }, 'Sua conta'),
      h('p', {}, h('strong', {}, me.name || ''), me.name ? ' · ' : '', me.email || ''),
      me.createdAt ? h('p', { class: 'small muted' }, `Conta criada em ${fmtDate(me.createdAt)}`) : null),
    h('section', { class: 'card', 'aria-labelledby': 'cons-title' },
      h('h2', { id: 'cons-title' }, 'Consentimentos'),
      panel),
    h('section', { class: 'card', 'aria-labelledby': 'data-title' },
      h('h2', { id: 'data-title' }, 'Seus dados'),
      h('p', { class: 'small muted' }, 'A exportação traz tudo o que o Zyft guarda sobre você, em JSON.'),
      exportBtn),
    h('section', { class: 'card callout danger', 'aria-labelledby': 'del-title' },
      h('h2', { id: 'del-title' }, 'Excluir conta'),
      h('p', {}, 'A exclusão é definitiva e não pode ser desfeita.'),
      deleteBtn));
}
