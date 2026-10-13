# Zyft — Candidatura Assistida

O **Zyft** é um assistente digital para pessoas que se candidatam a vagas de emprego pela internet. Ele reduz o esforço repetitivo da candidatura, ajuda a pessoa a apresentar informações profissionais **verdadeiras** e facilita o preenchimento de formulários de vagas, **mantendo a pessoa no controle de cada decisão**.

> "Um assistente que ajuda você a preparar e preencher candidaturas com base nas suas informações, revisar cada resposta e confirmar cada ação antes do envio."

O produto não é exclusivo de nenhuma área, nível de experiência, modalidade de trabalho ou tipo de candidato.

## Estado do projeto

| Item | Situação |
|---|---|
| Fase | **Fase 0**: pesquisa v2 em campo e spike do primeiro portal ([roadmap](docs/01-produto/roadmap.md)) |
| MVP 1 | Candidatura Assistida, em planejamento ([DVP-E](docs/01-produto/dvp-e.md), [DRP](docs/01-produto/drp.md)) |
| MVP 2 | Marketplace de TI (PJ/CLT), painel central de candidaturas, outros portais ([histórico](docs/00-contexto/historico-e-reposicionamento.md)) |
| Protótipo | Funcional (Node.js, extensão MV3, IA local com Ollama), ainda fora deste repositório; migração prevista no [ADR-0003](docs/03-arquitetura/adr/0003-monorepo-typescript-node.md) ([como rodar](docs/06-operacao/runbook-local.md)) |

## Como funciona (MVP 1)

```mermaid
flowchart LR
  P[Pessoa candidata] -->|cria ou importa| PM[Perfil Profissional Mestre<br/>plataforma web]
  P -->|abre e faz login| Portal[Site de vagas]
  Ext[Extensão Chrome/Edge<br/>painel lateral] -->|lê os campos da página| Portal
  Ext -->|campos sem valores| API[API Zyft]
  API -->|regras, depois IA que só aponta a chave| PM
  API -->|valores propostos| Ext
  P -->|revisa, edita e aprova| Ext
  Ext -->|preenche só o aprovado| Portal
  P -->|salva ou envia, por conta própria| Portal
```

1. A pessoa monta o **Perfil Mestre**, à mão ou importando um currículo em PDF ou Word, e revisa e aprova cada informação.
2. No site de vagas, com login feito por ela mesma, a **extensão** lê o formulário e propõe o preenchimento.
3. Os campos objetivos vêm do perfil. A IA só indica **qual** dado do perfil usar, nunca escreve o valor. Os textos abertos são sugeridos apenas com fatos aprovados.
4. A pessoa confere cada campo, edita o que quiser e decide se salva ou envia. **O Zyft nunca envia nada.**

## Regras inegociáveis

- Nada é enviado sem confirmação explícita; a extensão nunca clica em enviar.
- A IA não inventa fatos e não escreve valores objetivos.
- O Zyft não pede nem guarda a senha dos sites de vagas e não contorna CAPTCHA ou autenticação.
- Dados sensíveis ficam separados, fora da IA, e só são usados com ação expressa.
- O Zyft não promete compatibilidade nem contratação.

## Documentação

O índice completo está em [`docs/README.md`](docs/README.md).

| Área | Documentos |
|---|---|
| Contexto | [Histórico e reposicionamento](docs/00-contexto/historico-e-reposicionamento.md) · [Glossário](docs/00-contexto/glossario.md) |
| Produto | [DVP-E](docs/01-produto/dvp-e.md) · [DRP](docs/01-produto/drp.md) · [Personas](docs/01-produto/personas.md) · [Fluxos](docs/01-produto/fluxos.md) · [Roadmap](docs/01-produto/roadmap.md) · [Métricas](docs/01-produto/metricas.md) |
| Viabilidade | [DVS](docs/02-viabilidade/dvs.md) |
| Arquitetura | [DAT](docs/03-arquitetura/dat.md) · [Backend](docs/03-arquitetura/backend.md) · [Extensão](docs/03-arquitetura/extensao.md) · [Web](docs/03-arquitetura/frontend-web.md) · [IA](docs/03-arquitetura/ia.md) · [API](docs/03-arquitetura/api.md) · [Dados](docs/03-arquitetura/modelo-de-dados.md) · [Segurança](docs/03-arquitetura/seguranca.md) · [ADRs](docs/03-arquitetura/adr/README.md) |
| Qualidade | [Estratégia de testes e evals](docs/04-qualidade/estrategia-de-testes.md) · [Critérios de aceite](docs/04-qualidade/criterios-de-aceite-mvp1.md) |
| Pesquisa | [Plano](docs/05-pesquisa/plano-de-pesquisa.md) · [Questionário v2](docs/05-pesquisa/questionario-v2.md) · [Ferramentas](ferramentas/pesquisa/README.md) |
| Operação | [Ambientes](docs/06-operacao/ambientes.md) · [Rodar o protótipo](docs/06-operacao/runbook-local.md) |
| Legal | [Termos de uso](docs/07-legal/termos-de-uso.md) · [Privacidade](docs/07-legal/politica-de-privacidade.md) (rascunhos) |
| Time | [Papéis e RACI](docs/08-time/papeis-e-raci.md) · [Processo](docs/08-time/processo.md) · [Agentes de IA](docs/08-time/agentes-de-ia.md) · [Contribuir](CONTRIBUTING.md) |

## Time de agentes de IA

O time trabalha com agentes do Claude Code definidos em [`.claude/agents/`](.claude/agents/): CEO, Product Owner, Product Manager, arquiteto de software, arquiteto de IA, dev backend, dev frontend, dev da extensão, QA, designer UX e privacidade e segurança. As regras do projeto para os agentes estão em [`CLAUDE.md`](CLAUDE.md) e a decisão no [ADR-0016](docs/03-arquitetura/adr/0016-time-de-agentes-de-ia-no-desenvolvimento.md). Os agentes não fazem push, não fazem merge e não aprovam o próprio trabalho.

## Estrutura do repositório

```
.
├── CLAUDE.md                  regras do projeto para o Claude Code
├── CONTRIBUTING.md            como contribuir (branches, commits, PRs)
├── CHANGELOG.md
├── .claude/agents/            agentes de IA do time
├── .github/                   templates de PR e issues
├── docs/
│   ├── 00-contexto/           histórico, glossário, documentos de origem
│   ├── 01-produto/            DVP-E, DRP, personas, fluxos, roadmap, métricas
│   ├── 02-viabilidade/        DVS
│   ├── 03-arquitetura/        DAT, componentes, API, dados, segurança, adr/
│   ├── 04-qualidade/          testes, evals, critérios de aceite
│   ├── 05-pesquisa/           plano e questionário
│   ├── 06-operacao/           ambientes e runbooks
│   ├── 07-legal/              termos e privacidade
│   ├── 08-time/               papéis, processo, agentes
│   └── arquivo/marketplace-v1 documentação do marketplace (MVP 2)
└── ferramentas/pesquisa/      scripts do Google Forms e do relatório
```

A estrutura de código prevista (`apps/api`, `apps/web`, `apps/extension`, `packages/*`) está no [DAT](docs/03-arquitetura/dat.md) e no [ADR-0003](docs/03-arquitetura/adr/0003-monorepo-typescript-node.md).
