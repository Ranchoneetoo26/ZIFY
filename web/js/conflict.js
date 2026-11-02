// 409 profile.conflict: o Perfil Mestre mudou em outra sessão (outra aba, outro aparelho ou a extensão) depois que
// esta tela leu os dados. Nada é gravado em silêncio e nada do que a pessoa digitou some sem ela confirmar:
// - "Recarregar dados mais recentes": pergunta antes de descartar o que foi mudado aqui e redesenha a tela;
// - "Sobrescrever mesmo assim": confirmação explícita e reenvio com a versão atual (api.js). No PUT /profile, que
//   substitui tudo, o corpo é remontado sobre o perfil mais recente com só os campos que esta tela mudou (store.js);
// - "Cancelar": fecha o aviso; o formulário continua como estava, sem salvar.
import { onProfileConflict } from './api.js';
import { h, modal, confirmDialog, closeModals, go, announce } from './dom.js';

let prompt = null;

function ask() {
  return new Promise((resolve) => {
    let choice = 'cancel';
    const reload = h('button', { type: 'button', class: 'zy-btn zy-btn--md zy-btn--primary' }, 'Recarregar dados mais recentes');
    const overwrite = h('button', { type: 'button', class: 'zy-btn zy-btn--md zy-btn--secondary' }, 'Sobrescrever mesmo assim');
    const cancel = h('button', { type: 'button', class: 'zy-btn zy-btn--md zy-btn--quiet' }, 'Cancelar');
    const m = modal({
      title: 'Seu perfil foi alterado em outra sessão',
      body: h('div', { class: 'stack' },
        h('p', {}, 'Depois que esta tela foi aberta, o seu Perfil Mestre foi salvo em outra aba, em outro aparelho ou pela extensão. Para não apagar nada sem você saber, a alteração desta tela ainda não foi salva.'),
        h('p', {}, 'Recarregue para ver a versão mais recente, ou sobrescreva para gravar o que está nesta tela por cima dela.')),
      actions: [reload, overwrite, cancel],
      onClose: () => resolve(choice),
    });
    reload.addEventListener('click', () => { choice = 'reload'; m.close(choice); });
    overwrite.addEventListener('click', () => { choice = 'overwrite'; m.close(choice); });
    cancel.addEventListener('click', () => m.close('cancel'));
  });
}

async function decide() {
  for (;;) {
    const choice = await ask();
    if (choice === 'reload') {
      const ok = await confirmDialog({
        title: 'Descartar o que você mudou?',
        message: 'O que você mudou nesta tela e ainda não foi salvo será descartado, e a tela mostrará os dados mais recentes do perfil.',
        confirmText: 'Descartar e recarregar', cancelText: 'Voltar', danger: true,
      });
      if (!ok) continue;
      // Fora da fila de gravação: a tela é redesenhada depois que o pedido em andamento termina com erro.
      setTimeout(() => { closeModals(); go(location.hash); announce('Mostrando os dados mais recentes do seu perfil.'); });
      return 'reload';
    }
    if (choice === 'overwrite') {
      const ok = await confirmDialog({
        title: 'Sobrescrever a versão mais recente?',
        message: 'O que você mudou nesta tela será gravado por cima do perfil salvo na outra sessão. As mudanças feitas lá nos mesmos dados serão substituídas; o resto continua como foi salvo lá.',
        confirmText: 'Sobrescrever', cancelText: 'Voltar', danger: true,
      });
      if (!ok) continue;
      return 'overwrite';
    }
    announce('Nada foi salvo. Suas alterações continuam na tela.');
    return 'cancel';
  }
}

// Várias gravações em conflito ao mesmo tempo recebem a mesma resposta (um único diálogo).
onProfileConflict(() => {
  prompt ||= decide().finally(() => { prompt = null; });
  return prompt;
});
