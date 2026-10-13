# ADR-0001 — Reposicionamento: o MVP 1 é a Candidatura Assistida

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 12/10/2026 |
| Decisores | CEO, Product Owner |
| Consultados | Product Manager, Arquiteto de software |
| Requisitos | Todos os RF do MVP 1; RN-07 |
| Relacionados | [Histórico e reposicionamento](../../00-contexto/historico-e-reposicionamento.md), [DVP-E](../../01-produto/dvp-e.md) |

## Contexto

Até 05/10/2026, o Zyft estava documentado como um **marketplace de TI**: profissionais e empresas, contratação PJ e CLT, assinatura fixa sem comissão, matching por senioridade e reputação bilateral imutável (DVP-E v12, arquivado em `docs/arquivo/marketplace-v1/`). Nada desse escopo tinha sido implementado.

Em paralelo, o documento *Automação Assistida de Candidaturas* propôs um assistente de candidatura, e o time construiu em dois dias um protótipo funcional:

- Perfil Mestre;
- importação de currículo;
- extensão MV3 que lê formulários;
- de-para com IA local;
- análise de vaga com evidência;
- revisão campo a campo.

O protótipo foi testado num portal de demonstração e numa vaga pública real.

O documento de reposicionamento de 12/10/2026 formaliza a mudança: a primeira entrega é a Candidatura Assistida, e o marketplace vai para o MVP 2.

## Fatores de decisão

- **Tempo até o valor:** um marketplace só gera valor com os dois lados ativos (liquidez); um assistente gera valor para uma pessoa, no primeiro uso.
- **Risco técnico já reduzido:** o núcleo da Candidatura Assistida existe em protótipo; o do marketplace não.
- **Tamanho do público:** qualquer pessoa que se candidata a vagas pela internet, não só profissionais de TI.
- **Validação barata:** o MVP 1 pode ser testado com poucas pessoas num único portal, sem cobrança, moderação ou reputação.
- **Continuidade:** o Perfil Mestre é a base de que o marketplace também precisa.

## Opções consideradas

### Opção A — Manter o marketplace como MVP 1

- **Prós:** aproveita toda a documentação já escrita (DVP-E v12, DRP, DAT, ER); diferencial claro (sem comissão).
- **Contras:** problema de "ovo e galinha" (empresas e profissionais); exige cobrança, reputação, moderação e LGPD de dois lados antes do primeiro valor; nenhuma linha de código pronta; público restrito a TI.

### Opção B — Candidatura Assistida como MVP 1, marketplace no MVP 2

- **Prós:** valor de um lado só; protótipo funcional; público amplo; o MVP 1 gera o Perfil Mestre que o MVP 2 reaproveita; risco regulatório menor (sem intermediar contratação).
- **Contras:** a documentação antiga deixa de valer para o MVP 1; o produto passa a depender do comportamento de portais de terceiros (termos de uso, mudanças de layout); monetização ainda indefinida.

### Opção C — Fazer os dois em paralelo

- **Prós:** não abandona nenhuma linha.
- **Contras:** time pequeno dividido; nenhum dos dois chega a ser validado; escopo difícil de controlar.

## Decisão

Escolhemos a **Opção B**. O MVP 1 é a Candidatura Assistida, como descrita no [DVP-E v2.0](../../01-produto/dvp-e.md). O marketplace e as demais capacidades fora do fluxo de candidatura assistida ficam no MVP 2.

Passa a valer a regra **RN-07**: nenhuma funcionalidade do MVP 2 entra no MVP 1 sem decisão explícita de escopo (ADR ou revisão aprovada do DVP-E).

Os documentos do marketplace são arquivados sem alteração em `docs/arquivo/marketplace-v1/`, como insumo do MVP 2.

## Consequências

**Positivas**
- O time foca em um fluxo só: perfil → portal → revisão → preenchimento.
- O protótipo vira referência viva para requisitos e arquitetura.
- A pesquisa de validação passa a perguntar sobre candidatura, não sobre contratação PJ/CLT.

**Negativas**
- O protótipo tem funcionalidades que agora são MVP 2 (histórico com 10 status, vários portais na `host_permissions`). Elas precisam ser desligadas ou isoladas no MVP 1 ([ADR-0011](0011-primeiro-portal-e-conectores-por-portal.md), [ADR-0012](0012-registro-minimo-de-conclusao-no-mvp1.md)).
- A stack proposta para o marketplace (C#/.NET 8) precisa ser reavaliada ([ADR-0003](0003-monorepo-typescript-node.md)).
- Mockups e personas antigas deixam de valer e precisam ser refeitos.

**Riscos e mitigação**
- **Portais bloqueiam ou mudam o layout.** Mitigação: um portal por vez, conector testado com páginas gravadas, sem tráfego automatizado (RNF-10, RNF-13).
- **Ser confundido com robô de candidatura em massa.** Mitigação: confirmação explícita ([ADR-0009](0009-nunca-enviar-confirmacao-explicita.md)) e comunicação de produto alinhada.
- **Monetização indefinida.** Mitigação: pergunta específica na pesquisa v2; sem cobrança no MVP 1.

## Conformidade

- Revisão de PR: toda história nova cita um RF do MVP 1. Histórias que citam `RF-M2-xx` são bloqueadas sem ADR.
- O DVP-E, o DRP e o roadmap mostram MVP 1 e MVP 2 separados.

## Links

- `docs/00-contexto/ZYFT_visao_produto_reposicionamento_MVP1_MVP2.docx`
- `docs/00-contexto/plano-automacao-candidaturas.pdf`
- [Roadmap](../../01-produto/roadmap.md)
