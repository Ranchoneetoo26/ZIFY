# Arquitetura do frontend web (plataforma)

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Arquitetura do frontend web |
| Versão | 1.0 |
| Status | Em revisão. A stack depende do [ADR-0003](adr/0003-monorepo-typescript-node.md) (proposto). |
| Dono | Arquitetura de software, com o time de frontend e UX |
| Documentos relacionados | [DAT](dat.md), [API](api.md), [Fluxos](../01-produto/fluxos.md), [DRP §6](../01-produto/drp.md#6-requisitos-de-ux) |

## 1. Escopo

A plataforma web é onde a pessoa **prepara** as candidaturas. O **uso** acontece no painel lateral da extensão, que compartilha componentes e tokens com a web pelo pacote `packages/ui`.

| Área | Requisitos | Protótipo (`server/public/`) |
|---|---|---|
| Conta: cadastro, login, verificação de e-mail, Google | RF-01 | Não existe (um usuário local) |
| Perfil Mestre: editor por seções | RF-02 | Aba "Perfil Mestre" |
| O que falta: perguntas sobre dados ausentes | RF-05 | Painel de perguntas após importar |
| Importar currículo: envio e revisão lado a lado | RF-03, RF-04 | Aba "Importar currículo" (revisão simples) |
| Respostas aprovadas: biblioteca | RF-06 | Aba "Respostas aprovadas" |
| Consentimentos por finalidade | RF-07 | Configurações |
| Extensões pareadas: autorizar (PKCE) ou confirmar código, listar, revogar | RF-09 | Token colado à mão |
| Registro de ações | RF-20 | "Registro de automação" |
| Exportar e excluir dados | RF-08 | Configurações |
| Candidaturas (10 status) | MVP 2 | Aba "Candidaturas"; **sai do MVP 1**, salvo decisão do [ADR-0012](adr/0012-registro-minimo-de-conclusao-no-mvp1.md) |

## 2. Stack (proposta)

| Necessidade | Escolha | Motivo |
|---|---|---|
| UI | React 18 + TypeScript | Mesmo ecossistema do painel lateral, que reutiliza componentes |
| Build | Vite | Rápido e simples; já usado no build da extensão |
| Rotas | React Router | Poucas telas, sem necessidade de SSR |
| Dados do servidor | TanStack Query | Cache, polling da importação, estados de carregamento e erro |
| Formulários | React Hook Form + resolvers Zod (`profile-schema`) | A validação é a mesma da API |
| Componentes acessíveis | Radix UI (primitivos) + componentes próprios em `packages/ui` | Foco, teclado e ARIA corretos por padrão |
| Estilo | CSS Modules + design tokens em CSS custom properties | Sem dependência de framework de CSS; tema claro e escuro |
| Testes | Vitest + Testing Library; Playwright; axe-core | Ver §7 |

SSR não é necessário: a plataforma é privada, fica atrás de login e não depende de SEO. As páginas públicas (landing, termos, privacidade) podem ser estáticas.

## 3. Estrutura

```
apps/web/src/
  app/            rotas, layout, guarda de autenticação
  features/
    account/      cadastro, login, verificação
    profile/      editor por seção, "o que falta"
    import/       envio, acompanhamento, revisão lado a lado
    answers/      biblioteca de respostas aprovadas
    consents/     finalidades e histórico
    devices/      autorizar extensão (PKCE), confirmar código (alternativa), listar, revogar
    activity/     registro de ações
    privacy/      exportar, excluir conta
  lib/api/        cliente gerado de packages/api-contract
```

## 4. Telas-chave

### 4.1 Revisão da importação (RF-04)

Três colunas por campo ou item: **No currículo**, **No seu perfil hoje** e **Decisão** (aceitar, editar ou descartar).

- Itens de lista (experiências, formações) são revisados como cartões. A pessoa pode aceitar o cartão inteiro ou campo a campo.
- Nada vem aceito por padrão quando há conflito com o perfil atual. Quando o perfil está vazio, "aceitar tudo" é permitido, mas exige um clique explícito.
- Ao aplicar, a tela mostra o resumo ("12 campos adicionados, 2 alterados, 3 descartados") e leva para "O que falta" (RF-05).

### 4.2 Perfil Mestre (RF-02)

- Seções recolhíveis na ordem do DRP. Cada campo tem rótulo, ajuda curta e indicação de opcional.
- Os dados sensíveis ficam numa seção separada, **fechada** por padrão. Ela explica o uso e fica desabilitada enquanto o consentimento (c) estiver desligado.
- Salvamento explícito por seção, com aviso de alterações não salvas. Cada gravação cria uma versão (histórico visível: "salvo em 12/10 às 14:03").

### 4.3 Extensões pareadas (RF-09)

- Tela de autorização do fluxo principal (Authorization Code + PKCE, [ADR-0007](adr/0007-autenticacao-e-pareamento-da-extensao.md)): aberta pela extensão via `launchWebAuthFlow`, mostra "Extensão Zyft neste navegador", o que ela poderá fazer, o nome sugerido do dispositivo ("Chrome no Windows") e os botões "Autorizar" e "Cancelar".
- Fluxo alternativo: campo para o código mostrado pela extensão (formato `XXX-XXXX`), nome sugerido do dispositivo e botão "Conectar".
- Lista com nome, navegador, último uso e "Desconectar".

### 4.4 Registro de ações (RF-20)

Linha do tempo legível: "12/10 14:05 — Preenchimento em *Portal X*: 18 campos preenchidos, 3 deixados para você". Nunca mostra valores. Filtro por tipo de ação.

### 4.5 Privacidade (RF-08)

- "Baixar meus dados" (JSON) e "Excluir minha conta". A exclusão pede que a pessoa digite "EXCLUIR" e explica o que acontece com as extensões pareadas.

## 5. Design tokens

Os tokens ficam em `packages/ui/tokens.css` e valem para a web e para o painel lateral. A identidade visual do marketplace (amarelo `#FFC629` e preto `#121212`) **não** é reutilizada sem revisão: ela foi pensada para destaque de prioridade, um conceito que não existe no MVP 1. A paleta abaixo é a de partida, a ser validada pelo designer.

| Token | Uso | Regra |
|---|---|---|
| `--color-bg`, `--color-surface`, `--color-text`, `--color-text-muted` | Base | Contraste do texto ≥ 4,5:1 nos dois temas |
| `--color-accent` | Ações primárias | Contraste ≥ 4,5:1 com o texto do botão |
| `--color-state-ready` | Campo pronto | Sempre acompanhado do texto "Pronto" e de um ícone |
| `--color-state-ambiguous` | Ambíguo ou ausente | Texto "Precisa de você" |
| `--color-state-blocked` | Sensível, declaração, anexo, CAPTCHA | Texto "Ação sua" |
| `--color-state-ai` | Sugestão de IA | Texto "Sugestão — revise" |
| `--space-*`, `--radius-*`, `--font-*` | Ritmo e tipografia | Escala de 4 px; corpo de 16 px na web e 14 px no painel |

Os estados nunca são comunicados só por cor (RNF-06, DRP §6).

## 6. Acessibilidade (RNF-06)

- WCAG 2.1 AA como critério de aceite de toda tela.
- Navegação completa por teclado, foco visível, ordem de foco lógica e "pular para o conteúdo".
- Formulários com `label` associado, erros ligados por `aria-describedby` e mensagem de erro em texto.
- Atualizações assíncronas (importação em andamento, salvamento) anunciadas por `aria-live="polite"`.
- Movimento reduzido respeitado (`prefers-reduced-motion`).
- Linguagem simples (RNF-11): frases curtas e sem jargão de RH ou de TI. Revisão de UX writing nas histórias com texto novo.

## 7. Estados de interface

Cada tela trata explicitamente:

| Estado | Comportamento |
|---|---|
| Carregando | Esqueleto com `aria-busy`; nada de spinner sem texto |
| Vazio | Explica o próximo passo ("Importe seu currículo ou preencha à mão") |
| Erro | Mensagem do `detail` (Problem Details) e ação de recuperação |
| IA indisponível | A importação mostra "A leitura automática está indisponível; preencha à mão ou tente mais tarde"; o resto da plataforma continua funcionando |
| Consentimento desligado | Recurso desabilitado com explicação e link para Configurações |
| Sessão expirada | Volta ao login preservando o rascunho do formulário (em memória) |

## 8. Segurança no frontend

- Sessão por cookie `HttpOnly; Secure; SameSite=Lax`; nenhum token em `localStorage`.
- Proteção CSRF por header customizado (`X-Zyft-CSRF`) com valor vindo de um cookie legível (double submit).
- CSP: `default-src 'self'; script-src 'self'; connect-src 'self' https://api.zyft.app; frame-ancestors 'none'`.
- Nenhum HTML vindo da API é renderizado como HTML (sem `dangerouslySetInnerHTML`). O protótipo montava HTML por string com `esc()`; isso deixa de existir com o React.

## 9. Testes

| Nível | Cobertura |
|---|---|
| Componentes (Vitest + Testing Library) | Estados de campo, revisão de importação, formulários do perfil |
| E2E (Playwright) | Cadastro → importação → revisão → perfil → pareamento → registro → exportar e excluir |
| Acessibilidade | axe em todas as rotas no E2E; zero violações "critical" e "serious" |
| Visual | Capturas dos componentes do pacote `ui` nos temas claro e escuro |
