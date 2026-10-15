// Onboarding: explica o copiloto e pede os consentimentos por finalidade (RF-07).
import { h, go } from '../dom.js';
import { consentsPanel } from './consents.js';

export async function render(main) {
  const panel = await consentsPanel();
  main.append(
    h('div', { class: 'page-head' }, h('div', {},
      h('h1', {}, 'Boas-vindas ao Zyft'),
      h('p', { class: 'lead' }, 'Antes de começar, escolha o que o Zyft pode fazer por você. Você pode mudar isso a qualquer momento em Conta.'))),
    h('div', { class: 'card callout info' },
      h('h2', {}, 'Como o copiloto funciona'),
      h('ul', {},
        h('li', {}, 'Você monta o seu Perfil Mestre, como um passaporte: cada item aprovado ganha um carimbo.'),
        h('li', {}, 'No portal de vagas, a extensão lê o formulário e mostra o que pode preencher.'),
        h('li', {}, 'Você revisa, escolhe e manda preencher. Só entra o que você aprovou.'),
        h('li', {}, h('strong', {}, 'Quem envia a candidatura é sempre você.'), ' O Zyft nunca clica em enviar, nunca lê senhas e nunca resolve CAPTCHA.'))),
    h('section', { class: 'card', 'aria-labelledby': 'consents-title' },
      h('h2', { id: 'consents-title' }, 'Consentimentos'),
      h('p', { class: 'small muted' }, 'Ligue só o que fizer sentido para você. Dados sensíveis começam desligados e só mudam por decisão sua.'),
      panel),
    h('div', { class: 'form-actions' },
      h('button', { class: 'primary', type: 'button', onclick: () => go('#/importar') }, 'Importar meu currículo'),
      h('button', { type: 'button', onclick: () => go('#/perfil') }, 'Preencher o perfil à mão')));
}
