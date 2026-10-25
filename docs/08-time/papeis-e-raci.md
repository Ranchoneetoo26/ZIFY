# Papéis e matriz RACI

| Campo | Valor |
|---|---|
| Documento | Papéis e RACI |
| Versão | 1.0 |
| Data | 12/10/2026 |
| Relacionados | [Processo](processo.md), [Agentes de IA](agentes-de-ia.md), [ADR-0016](../03-arquitetura/adr/0016-time-de-agentes-de-ia-no-desenvolvimento.md) |

## 1. Papéis

Cada papel tem **uma pessoa responsável** (humana) e, quando útil, **um agente de IA de apoio** em `.claude/agents/`. Num time pequeno, uma pessoa acumula papéis. O agente prepara rascunhos, revisões e testes, mas a decisão e a aprovação são sempre humanas.

| Papel | Responsabilidade principal | Agente de apoio |
|---|---|---|
| CEO / idealizadores | Estratégia, escopo entre MVPs, go/no-go, viabilidade econômica | `ceo` |
| Product Owner (PO) | DVP-E, DRP, backlog, critérios de aceite, aceite | `product-owner` |
| Product Manager (PM) | Descoberta, pesquisa, roadmap, métricas, beta | `product-manager` |
| Designer UX | Fluxos, telas, textos de interface, acessibilidade, usabilidade | `designer-ux` |
| Arquiteto de Software | DAT, ADRs, contratos, revisão arquitetural, spike do portal | `arquiteto-de-software` |
| Arquiteto de IA | Modelo, prompts, schemas, guardrails, evals | `arquiteto-de-ia` |
| Dev Backend | API, persistência, integração com IA, auditoria | `dev-backend` |
| Dev Frontend | Plataforma web | `dev-frontend` |
| Dev Extensão | Extensão MV3, crawler, preenchimento, conector do portal | `dev-extensao` |
| QA | Estratégia de testes, E2E, evals, testes negativos de confiança | `qa` |
| Encarregado de dados / AppSec | LGPD, RIPD, modelo de ameaças, revisão de segurança | `privacidade-e-seguranca` |

### 1.1 Quem ocupa cada papel (2026/2)

| Integrante | Papel | Responsabilidades |
|---|---|---|
| Antonio Randolfo Barroso Neto | Product Owner, Product Manager e gestão | Escopo do MVP 1, backlog, critérios de aceite, pesquisa com usuários e prazos das entregas |
| Guilherme Brogio Macedo da Silva | Arquiteto de software e dev full stack | Arquitetura, ADRs, modelagem do banco, contratos da API, integração e IA |
| Lucas Ulisses Clementino De Arruda E Silva | Dev frontend | Telas, navegação, formulários e validações da web e do painel da extensão |
| Matheus Guilherme Moreira De Oliveira | Dev backend | Controllers, handlers CQRS, repositórios, EF Core, autenticação e testes da API |

Papéis não listados (UX, QA, Arquiteto de IA, Privacidade e Segurança) são acumulados: UX com o dev frontend, QA com os devs, Arquiteto de IA e Privacidade com o arquiteto.

## 2. Matriz RACI

**R** = executa · **A** = responde pela decisão (um por linha) · **C** = consultado · **I** = informado

| Atividade | CEO | PO | PM | UX | Arq. SW | Arq. IA | Devs | QA | Priv./Seg. |
|---|---|---|---|---|---|---|---|---|---|
| Visão e escopo (DVP-E, MVP 1 × MVP 2) | A | R | C | I | C | C | I | I | C |
| Mudança de escopo (RN-07) | A | R | C | I | C | C | I | I | C |
| Backlog e priorização | C | A/R | C | C | C | C | I | C | I |
| Histórias e critérios de aceite | I | A/R | C | C | C | C | C | C | C |
| Pesquisa com usuários | I | C | A/R | C | I | I | I | I | C |
| Roadmap e métricas | C | C | A/R | I | I | I | I | I | I |
| Fluxos, telas e textos | I | A | C | R | C | I | C | C | C |
| ADR técnico | I | C | I | I | A/R | C | C | C | C |
| ADR de IA (modelo, prompts, guardrails) | I | C | I | I | C | A/R | C | C | C |
| Contratos de API | I | I | I | I | A | C | R | C | C |
| Implementação | I | I | I | C | C | C | A/R | C | C |
| Testes e evals | I | C | I | I | C | C | R | A/R | I |
| Revisão de privacidade e segurança | I | I | I | I | C | C | C | C | A/R |
| Termos, política e RIPD | C | C | I | I | I | I | I | I | A/R |
| Aceite da história | I | A/R | I | C | I | I | I | C | C |
| Release e beta | A | R | R | I | C | C | R | R | C |
| Go/no-go de fase | A/R | C | R | I | C | C | I | C | C |

## 3. Regras

- Agentes de IA **nunca** são A. Podem executar (R) rascunhos e verificações, sempre revisados por uma pessoa.
- Todo PR tem ao menos **um revisor humano** diferente do autor.
- Mudanças em dados pessoais, auth, permissões da extensão ou envio de dados à IA exigem o parecer de privacidade e segurança antes do merge.
