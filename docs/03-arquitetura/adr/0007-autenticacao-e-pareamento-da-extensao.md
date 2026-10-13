# ADR-0007 — Autenticação da plataforma e pareamento da extensão

| Campo | Valor |
|---|---|
| Status | Proposta |
| Data | 12/10/2026 |
| Decisores | Arquiteto de software |
| Consultados | Dev backend, Dev extensão, Dev frontend, Responsável por privacidade, QA |
| Requisitos | RF-01, RF-08, RF-09; RNF-03 |
| Relacionados | ADR-0002, ADR-0008, ADR-0015 |

## Contexto

O protótipo é **local e de um usuário só** (`server/src/index.js`):

- o servidor só aceita `Host: localhost` ou `127.0.0.1`, contra DNS rebinding;
- o CORS é liberado apenas para origens `chrome-extension://`, `moz-extension://` e `edge-extension://`;
- toda rota `/api` exige `Authorization: Bearer <token>`, um token estático de 32 caracteres hexadecimais gerado na primeira execução e guardado em `db.json`;
- a pessoa copia o token da tela de Configurações e cola na extensão, ou ele é lido de `config.local.json`.

Isso não serve para o MVP 1, que tem contas, roda na nuvem e precisa revogar extensões (RF-09). O token estático não expira, não identifica dispositivo e já apareceu num vídeo de demonstração.

## Fatores de decisão

- Segurança: tokens de vida curta, revogáveis por dispositivo (RNF-03).
- Experiência de pareamento simples para pessoas com pouca familiaridade digital.
- Padrões abertos, sem protocolo próprio.
- Compatibilidade com Chrome e Edge (o Edge implementa `chrome.identity.launchWebAuthFlow` com diferenças).
- Exclusão de conta encerra todos os pareamentos (RF-08).

## Opções consideradas

### Opção A — Token estático por usuário (protótipo)

- **Prós:** simples.
- **Contras:** não expira; um token vazado dá acesso a todo o perfil; sem revogação por dispositivo; copiar e colar é ruim para o público-alvo.

### Opção B — Cookie de sessão da plataforma compartilhado com a extensão

- **Prós:** login único.
- **Contras:** cookies de terceiros e `SameSite` tornam isso frágil; mistura a sessão da web com a da extensão; CSRF.

### Opção C — OAuth 2.1 Authorization Code + PKCE via `chrome.identity.launchWebAuthFlow`

A extensão abre o fluxo de login do Zyft, recebe o código no redirect `https://<id>.chromiumapp.org/` e troca por tokens.

- **Prós:** padrão de mercado; sem segredo na extensão (PKCE); tokens de curta duração com refresh; um dispositivo por concessão, revogável.
- **Contras:** exige servidor de autorização (biblioteca pronta ou provedor gerenciado); diferenças de comportamento no Edge precisam ser testadas.

### Opção D — Código de pareamento (estilo *device code*, RFC 8628)

A extensão mostra um código de 8 caracteres; a pessoa, logada na plataforma, digita o código em "Conectar extensão".

- **Prós:** funciona em qualquer navegador Chromium; fácil de explicar; não depende de `chrome.identity`.
- **Contras:** um passo manual a mais; precisa de limite de tentativas e expiração curta contra força bruta.

## Decisão (proposta)

- **Plataforma web:** login por e-mail com link mágico ou senha com verificação de e-mail, e login Google (OpenID Connect). Sessão por cookie `HttpOnly`, `Secure`, `SameSite=Lax`. A senha da plataforma é guardada com Argon2id. Nenhuma senha de portal é pedida em nenhum momento.
- **Extensão:** **Opção C** como fluxo principal e **Opção D** como alternativa quando `launchWebAuthFlow` falhar.
  - **Access token:** JWT assinado, validade **≤ 15 min**, escopo `extensao`, com `sub` (usuário) e `did` (id da concessão/dispositivo).
  - **Refresh token:** opaco, **rotativo** (cada uso emite um novo e invalida o anterior; reuso detectado revoga a concessão), validade de 30 dias deslizantes.
  - **Armazenamento na extensão:** o access token fica em `chrome.storage.session` (memória, some ao fechar o navegador) e o refresh token em `chrome.storage.local`. O content script nunca recebe tokens.
  - **Revogação:** a plataforma lista as extensões conectadas (navegador, data, último uso) com o botão "Desconectar". Excluir a conta revoga tudo.
- **API:** CORS restrito à origem da web e ao ID publicado da extensão; *rate limit* por usuário e por IP em autenticação e pareamento.

## Consequências

**Positivas**
- Um token vazado vale no máximo 15 minutos; a concessão pode ser revogada a qualquer momento.
- Auditoria por dispositivo (RF-20 registra "extensão X conectada").

**Negativas**
- Mais componentes: servidor de autorização, tabela de concessões e rotação de refresh.
- O ambiente local de desenvolvimento precisa simular o fluxo (perfil "dev" com usuário de teste).

**Riscos e mitigação**
- **Comportamento de `launchWebAuthFlow` no Edge.** Mitigação: teste E2E nos dois navegadores, com fallback para código de pareamento.
- **Força bruta no código de pareamento.** Mitigação: código de 8 caracteres sem caracteres ambíguos, validade de 10 min e 5 tentativas.

## Conformidade

- Testes de integração: token expirado → 401; refresh reutilizado → concessão revogada; conta excluída → todos os refresh tokens inválidos.
- Teste da extensão: nenhum token em mensagens trocadas com o content script.
- Revisão de segurança antes do primeiro beta.

## Links

- [Segurança](../seguranca.md), [API](../api.md)
- Protótipo: `server/src/index.js` (middlewares de host, CORS e Bearer), `server/src/store.js` (`apiToken`)
