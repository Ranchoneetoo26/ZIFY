# ADR-0016 — Time de agentes de IA no desenvolvimento

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 12/10/2026 |
| Decisores | CEO, Product Owner, Arquiteto de software |
| Consultados | Product Manager, QA, Encarregado de dados |
| Relacionados | [CLAUDE.md](../../../CLAUDE.md), [Agentes de IA](../../08-time/agentes-de-ia.md), [Papéis e RACI](../../08-time/papeis-e-raci.md) |

## Contexto

O Zyft é feito por um time pequeno, em calendário acadêmico, e o reposicionamento para a Candidatura Assistida exigiu reescrever visão, requisitos e arquitetura de uma vez. Já usamos o Claude Code no projeto: o protótipo e a documentação do marketplace, que citava um "Guia do Agente Supervisor", foram feitos com ele. Faltava padronizar **como** a IA participa do trabalho, para que a ajuda não viole as regras do produto nem dilua a responsabilidade pelas decisões.

## Fatores de decisão

- Capacidade limitada de pessoas para cobrir produto, UX, arquitetura, três frentes de código, QA e LGPD.
- Regras de confiança do produto que não podem ser relaxadas (nunca enviar, IA só com fatos, sem senhas, sensíveis bloqueados).
- Rastreabilidade: decisões e artefatos precisam ficar no repositório.
- Responsabilidade clara: alguém humano responde por cada decisão.
- Portabilidade: o processo não pode depender de uma ferramenta.

## Opções consideradas

1. **Sem padronização.** Cada pessoa usa IA como quiser.
2. **Um único assistente genérico** com o `CLAUDE.md` do projeto.
3. **Time de agentes por papel** (`.claude/agents/`), com entradas, saídas, regras e handoffs definidos, e aprovação humana obrigatória.

## Decisão

Adotamos a **opção 3**. São onze agentes: ceo, product-owner, product-manager, designer-ux, arquiteto-de-software, arquiteto-de-ia, dev-backend, dev-frontend, dev-extensao, qa e privacidade-e-seguranca. O `CLAUDE.md` na raiz define o contexto e as regras comuns.

- Cada agente tem ferramentas mínimas para o papel (por exemplo, CEO sem edição de código).
- Agentes **nunca** são "A" na [RACI](../../08-time/papeis-e-raci.md): não aprovam ADR, não fazem merge e não fazem push.
- O fluxo de handoff segue o [processo](../../08-time/processo.md#5-da-história-ao-código).

**Por que não a opção 1:** gera saídas inconsistentes e não protege as regras do produto. **Por que não a opção 2:** um assistente genérico mistura papéis (escreve código e aprova o próprio teste) e não carrega o contexto específico de cada função.

## Consequências

**Positivas**
- Rascunhos de histórias, ADRs, testes e pareceres saem mais rápido, com formato padrão.
- Regras inegociáveis repetidas em cada papel e checadas por testes negativos do QA.
- Separação entre quem faz e quem verifica, mesmo num time pequeno.

**Negativas**
- Mais artefatos para revisar; mitigado com limite de WIP e PRs pequenos.
- Risco de confiar demais no agente; mitigado com IDs rastreáveis e revisão humana obrigatória.
- Manutenção dos arquivos de agente quando os processos mudarem.

## Conformidade

- O template de PR pergunta quais agentes foram usados e quem revisou.
- A DoD exige revisão humana e testes negativos de confiança.
- O arquiteto de software revisa os arquivos em `.claude/agents/` a cada mudança de processo.

## Links

- [Agentes de IA](../../08-time/agentes-de-ia.md)
- [Papéis e RACI](../../08-time/papeis-e-raci.md)
- [Processo](../../08-time/processo.md)
- [`CLAUDE.md`](../../../CLAUDE.md)
- [ADR-0001](0001-reposicionamento-mvp1-candidatura-assistida.md)
