# ADR-0002 — Extensão de navegador Manifest V3 com painel lateral

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 12/10/2026 |
| Decisores | Arquiteto de software, Product Owner |
| Consultados | Dev extensão, Dev frontend, QA |
| Requisitos | RF-09 a RF-18; RNF-01, RNF-08, RNF-13; RN-01 |
| Relacionados | ADR-0007, ADR-0009, ADR-0010, ADR-0011 |

## Contexto

O preenchimento assistido precisa atuar **dentro do formulário do portal de vagas**, numa sessão em que a pessoa já está autenticada. O Zyft não pode guardar a senha do portal (RNF-01) nem gerar tráfego automatizado contra ele (RNF-13).

O protótipo usa uma extensão Chrome/Edge Manifest V3:

- **`content.js`:** lê os campos visíveis e preenche os valores aprovados.
- **`sidepanel.html`/`sidepanel.js`:** mostra a revisão e as ações.
- **`background.js`:** abre o painel lateral.
- **`host_permissions`:** fixas para localhost e quatro portais. Em outros sites, a extensão pede `optional_host_permissions` no primeiro uso.

Esse desenho funcionou no portal de demonstração e numa vaga pública real.

## Fatores de decisão

- Atuar na sessão do próprio usuário, sem credenciais.
- Revisão visível ao lado do formulário (a pessoa vê o campo e a proposta ao mesmo tempo).
- Não gerar requisições ao portal fora das que a própria pessoa faz.
- Respeitar termos de uso dos portais.
- Distribuição simples para usuários não técnicos.

## Opções consideradas

### Opção A — RPA/navegador headless no servidor

O servidor do Zyft abre o portal num Chromium sem interface, faz login e preenche.

- **Prós:** independente do navegador da pessoa; automação completa possível.
- **Contras:** exige a senha do portal (viola RNF-01); dispara CAPTCHA e bloqueios antifraude (viola ADR-0010); o tráfego vem dos IPs do Zyft (viola RNF-13 e provavelmente os termos dos portais); a pessoa não vê o que está sendo preenchido; empurra o produto para "robô de candidatura em massa".

### Opção B — Bookmarklet ou script colado no console

- **Prós:** sem instalação de loja; funciona em qualquer navegador.
- **Contras:** bloqueado por Content Security Policy em muitos portais; não tem painel persistente; não guarda tokens com segurança; experiência ruim para pessoas com pouca familiaridade digital.

### Opção C — Aplicativo desktop que controla o navegador

- **Prós:** controle total da interface; possível suporte a vários navegadores.
- **Contras:** instalação pesada; precisa de automação de navegador (CDP) com permissões amplas; não roda em Chromebooks nem em computadores de escolas ou lan houses; mais superfície de ataque.

### Opção D — Integração por API com os portais

- **Prós:** a forma mais robusta e "oficial"; não depende do HTML.
- **Contras:** os portais-alvo não oferecem API pública de candidatura para terceiros; as parcerias levam meses e dependem de negociação comercial; não serve para o MVP 1. Fica como possibilidade para o MVP 2 (RF-M2-04).

### Opção E — Extensão Manifest V3 com painel lateral (protótipo)

- **Prós:**
  - roda na sessão da pessoa, sem credenciais;
  - só lê e escreve o DOM da página aberta, sem tráfego extra;
  - o painel lateral (`chrome.sidePanel`) fica ao lado do formulário durante toda a revisão;
  - instalação pela Chrome Web Store e Edge Add-ons;
  - permissões por site.
- **Contras:**
  - só navegadores Chromium no MVP 1 (sem Firefox e Safari);
  - depende da estrutura HTML de cada portal;
  - campos em iframes de outra origem e shadow DOM fechado não são lidos (limitação conhecida do protótipo);
  - a revisão da loja pode atrasar publicações.

## Decisão

Escolhemos a **Opção E**: uma extensão Manifest V3 para Chrome e Edge, com painel lateral para revisão e content script para leitura e escrita de campos.

Regras obrigatórias:

1. A extensão só atua em páginas do portal suportado ([ADR-0011](0011-primeiro-portal-e-conectores-por-portal.md)) e em sites que a pessoa autorizar explicitamente pelo pedido de permissão do navegador. No MVP 1, `host_permissions` fixas contêm **apenas** o portal suportado e o domínio da API do Zyft. Os quatro portais do protótipo saem da lista.
2. O content script não faz requisições de rede ao portal nem ao Zyft. Toda comunicação com a API passa pelo painel ou pelo service worker, autenticada ([ADR-0007](0007-autenticacao-e-pareamento-da-extensao.md)).
3. O content script nunca dispara `submit`, `click` em botões de envio, avanço ou aceite ([ADR-0009](0009-nunca-enviar-confirmacao-explicita.md)).
4. O content script injeta o mínimo: leitura de campos, preenchimento de valores aprovados e marcação visual. Lógica de de-para fica na API e, quando for determinística e compartilhável, num pacote comum ([ADR-0003](0003-monorepo-typescript-node.md)).

## Consequências

**Positivas**
- Atende RNF-01 e RNF-13 por construção.
- A pessoa vê o formulário e a proposta lado a lado, o que é o centro da proposta de valor.
- Distribuição pelas lojas oficiais, com atualização automática.

**Negativas**
- Firefox e Safari ficam fora do MVP 1.
- Precisamos manter compatibilidade com mudanças das APIs do Chrome (MV3, `sidePanel`).
- Dependemos da política das lojas: a extensão precisa de justificativa clara de permissões e política de privacidade publicada.

**Riscos e mitigação**
- **O portal muda o HTML.** Mitigação: conector versionado com fixtures HTML e alerta de queda na taxa de reconhecimento (RNF-10).
- **A loja rejeita a extensão por permissões amplas.** Mitigação: `optional_host_permissions` só sob pedido explícito; documentar a finalidade de cada permissão.
- **Iframes e shadow DOM.** Mitigação: tratar no conector do portal escolhido, se ele usar; caso contrário, os campos aparecem como "ação sua".

## Conformidade

- Teste E2E (Playwright com a extensão carregada) em Chrome e Edge sobre fixtures do portal (RNF-08).
- Teste que falha se o content script chamar `form.submit()`, `requestSubmit()` ou `click()` em `button[type=submit]` ou `input[type=submit]`.
- Revisão do `manifest.json` em todo PR que o altera: qualquer nova permissão exige atualização deste ADR ou de um novo.

## Links

- [DAT — extensão](../extensao.md)
- Protótipo: `extension/manifest.json`, `extension/content.js`, `extension/sidepanel.js`
