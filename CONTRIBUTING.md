# Como contribuir com o Zyft

Obrigado por ajudar. Este guia resume o que está detalhado em [`docs/08-time/processo.md`](docs/08-time/processo.md).

## Antes de começar

1. Leia o [`CLAUDE.md`](CLAUDE.md), principalmente as **regras inegociáveis**.
2. Leia o [DVP-E](docs/01-produto/dvp-e.md) e o [DRP](docs/01-produto/drp.md). O produto atual é a **Candidatura Assistida (MVP 1)**. Os documentos em `docs/arquivo/` descrevem o marketplace antigo e **não** valem para o MVP 1.
3. Confira os [ADRs](docs/03-arquitetura/adr/) da área em que vai mexer.

## Fluxo

1. **Issue primeiro.** Use o template certo:
   - [história](.github/ISSUE_TEMPLATE/historia.md);
   - [bug](.github/ISSUE_TEMPLATE/bug.md);
   - [ADR](.github/ISSUE_TEMPLATE/adr.md).

   A história só entra em desenvolvimento com a Definition of Ready cumprida.
2. **Branch curta** a partir da `main`, no formato `tipo/descricao-curta` (`feat/`, `fix/`, `docs/`, `test/`, `refactor/`, `chore/`).
3. **Commits** em Conventional Commits, em português e citando os IDs. Exemplo:
   ```
   feat(extensao): bloqueia caixas de declaração (RF-18)
   ```
   Não inclua trailer `Co-Authored-By` nem atribuição a ferramentas.
4. **Pull request pequeno**, com o [template](.github/pull_request_template.md) preenchido, os testes passando e a documentação atualizada.
5. **Revisão:**
   - pelo menos uma pessoa diferente de quem escreveu;
   - parecer de privacidade e segurança se a mudança tocar dados pessoais, autenticação, permissões da extensão ou a IA.
6. **Aceite do PO e merge** por squash.

## Decisões técnicas

Toda decisão estrutural nova precisa de um ADR em `docs/03-arquitetura/adr/NNNN-titulo.md`:

- formato MADR em português;
- status inicial **Proposta**;
- pessoas do time aprovam a mudança para **Aceita**;
- para mudar uma decisão aceita, escreva um ADR novo que **substitui** o anterior.

## Mudança de escopo

Nada do MVP 2 (`RF-M2-xx`) entra no MVP 1 sem uma decisão explícita (RN-07). Abra uma issue de ADR marcando "mudança de escopo" e envolva o CEO e o PO.

## Agentes de IA

Pode usar os agentes de `.claude/agents/` (veja [agentes de IA](docs/08-time/agentes-de-ia.md)), com estes limites:

- quem abre o PR responde pelo conteúdo;
- agentes não fazem push, merge nem aprovação;
- não use dados pessoais reais em prompts, fixtures ou evals.

## Testes em portais

Siga estas regras ao testar:

- **não** crie contas falsas em portais reais;
- **não** use dados de outras pessoas;
- use as fixtures, o portal de demonstração ou a sua própria conta, fazendo você mesmo o login;
- nunca automatize o envio de candidaturas.
