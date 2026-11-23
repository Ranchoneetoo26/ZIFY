// Rota desconhecida (#/qualquer-coisa): explica e oferece o caminho de volta, sem trocar a tela por conta própria.
import { h } from '../dom.js';
import { isLoggedIn } from '../api.js';
import { PageHead } from '../ui.js';

export async function render(main) {
  const logged = isLoggedIn();
  const home = logged ? { href: '#/perfil', label: 'Ir para o Perfil Mestre' } : { href: '#/entrar', label: 'Ir para Entrar' };
  // Sem login não há menu e o <main> público não tem respiro: o espaçamento vem daqui.
  main.append(h('div', { class: 'content content--md', style: logged ? null : { padding: '48px 16px', margin: '0 auto' } },
    PageHead({ overline: 'Erro 404', title: 'Não encontramos essa página', lead: 'O endereço pode estar errado ou a página mudou de lugar. Nada do que você salvou foi alterado.' }),
    h('div', { class: 'row-end' },
      h('a', { class: 'zy-btn zy-btn--primary zy-btn--md', href: home.href }, home.label),
      logged ? h('a', { class: 'zy-btn zy-btn--quiet zy-btn--md', href: '#/candidaturas' }, 'Ver candidaturas') : null)));
}
