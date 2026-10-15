# Registros de Decisão de Arquitetura (ADRs)

Um **ADR** (Architecture Decision Record) registra uma decisão que afeta a estrutura do produto ou do sistema: o contexto em que foi tomada, as opções consideradas, o que foi decidido e as consequências. O objetivo é que qualquer pessoa do time (ou agente de IA) entenda **por que** o sistema é como é, sem depender da memória de quem estava na conversa.

Escrevemos ADRs para decisões que:

- são caras de reverter (stack, armazenamento, protocolo de autenticação, contrato da API com a extensão);
- mudam uma regra de confiança do produto (o que a IA pode fazer, o que a extensão nunca faz);
- mudam o escopo entre MVP 1 e MVP 2 (regra RN-07 do [DRP](../../01-produto/drp.md#5-regras-de-negócio));
- foram debatidas e podem ser rediscutidas no futuro.

Decisões pequenas e locais (nome de função, biblioteca de utilitário) ficam no PR, não em ADR.

## Ciclo de vida

```
Proposta ──► Aceita ──► Substituída (por ADR-XXXX)
    │            └────► Depreciada (deixou de se aplicar, sem substituta)
    └──► Rejeitada
```

| Status | Significado |
|---|---|
| **Proposta** | Escrita e aberta para revisão. Pode orientar spikes e protótipos, mas não autoriza implementação definitiva no MVP 1. |
| **Aceita** | Aprovada pelos decisores. É a regra vigente; código que a contradiz é bug. |
| **Substituída** | Uma decisão nova tomou o lugar desta. O ADR antigo continua no repositório com o link para o novo. |
| **Depreciada** | Deixou de se aplicar (ex.: o componente foi removido), sem substituta. |
| **Rejeitada** | Foi avaliada e não adotada. Fica registrada para não ser rediscutida sem fato novo. |

Um ADR **Aceita** nunca é reescrito para mudar a decisão: cria-se um ADR novo que o substitui. Correções de texto, links e esclarecimentos são permitidos.

## Quem aprova

Papéis descritos em [`docs/08-time/`](../../08-time/).

| Tipo de decisão | Decide | Precisa ser consultado |
|---|---|---|
| Técnica (stack, dados, integração, segurança) | Arquiteto de software | Devs envolvidos, QA, arquiteto de IA quando envolver modelos |
| Técnica com impacto em produto ou escopo (o que o usuário vê, MVP 1 × MVP 2) | Arquiteto de software **e** Product Owner | Product Manager, CEO se mudar a estratégia |
| Regras de IA (o que o modelo pode fazer, avaliação, provedor) | Arquiteto de IA **e** arquiteto de software | Privacidade/LGPD, QA |
| Dados pessoais e privacidade | Arquiteto de software **e** responsável por privacidade | Product Owner |
| Estratégia e posicionamento | CEO | Product Owner, Product Manager |

Agentes de IA do time podem **redigir** ADRs e marcá-los como Proposta; a passagem para Aceita é sempre feita por uma pessoa nos papéis acima ([ADR-0016](0016-time-de-agentes-de-ia-no-desenvolvimento.md)).

**Lote inicial (12/10/2026).** Os ADRs 0001 a 0016 foram redigidos com apoio de IA, a pedido de um integrante do time, durante a reescrita da documentação. Os marcados como **Aceita** registram decisões que já constavam nos documentos de visão aprovados pelo time ([PDF de origem](../../00-contexto/plano-automacao-candidaturas.pdf) e [documento de reposicionamento](../../00-contexto/ZYFT_visao_produto_reposicionamento_MVP1_MVP2.docx)) ou que já estão implementadas no protótipo. Decisões novas ficaram como **Proposta**. O lote inteiro deve ser ratificado pelos decisores na primeira revisão de arquitetura; qualquer ADR não ratificado volta para Proposta.

## Índice

| ADR | Título | Status |
|---|---|---|
| [0000](0000-template.md) | Modelo de ADR | — |
| [0001](0001-reposicionamento-mvp1-candidatura-assistida.md) | Reposicionamento: MVP 1 é a Candidatura Assistida | Aceita |
| [0002](0002-extensao-de-navegador-mv3-com-painel-lateral.md) | Extensão de navegador Manifest V3 com painel lateral | Aceita |
| [0003](0003-monorepo-typescript-node.md) | Monorepo TypeScript/Node.js para API, web e extensão | Substituída por ADR-0017 |
| [0004](0004-ia-aponta-chave-nunca-escreve-valor.md) | A IA aponta a chave do perfil, nunca escreve o valor | Aceita |
| [0005](0005-modelo-open-weight-self-hosted-atras-de-porta-llm.md) | Modelo open-weight atrás de uma porta `LLMProvider` | Aceita (porta) / Proposta (hospedagem de produção) |
| [0006](0006-regras-deterministicas-antes-da-ia.md) | Regras determinísticas antes da IA no de-para | Aceita |
| [0007](0007-autenticacao-e-pareamento-da-extensao.md) | Autenticação da plataforma e pareamento da extensão | Proposta |
| [0008](0008-postgresql-com-perfil-em-jsonb.md) | PostgreSQL com Perfil Mestre em JSONB | Proposta |
| [0009](0009-nunca-enviar-confirmacao-explicita.md) | O Zyft nunca envia; confirmação explícita da pessoa | Aceita |
| [0010](0010-sem-credenciais-sem-contorno-de-seguranca-campos-bloqueados.md) | Sem credenciais, sem contorno de segurança, campos bloqueados | Aceita |
| [0011](0011-primeiro-portal-e-conectores-por-portal.md) | Primeiro portal e conectores por portal | Proposta |
| [0012](0012-registro-minimo-de-conclusao-no-mvp1.md) | Registro mínimo de conclusão no MVP 1 | Proposta |
| [0013](0013-evidencia-por-linha-numerada-na-analise-de-vaga.md) | Evidência por linha numerada na análise de vaga | Aceita |
| [0014](0014-dados-sensiveis-segregados-e-criptografados.md) | Dados sensíveis segregados e criptografados | Aceita |
| [0015](0015-observabilidade-sem-dados-pessoais.md) | Observabilidade sem dados pessoais | Proposta |
| [0016](0016-time-de-agentes-de-ia-no-desenvolvimento.md) | Time de agentes de IA no desenvolvimento | Aceita |
| [0017](0017-backend-em-csharp-dotnet-clean-architecture.md) | Backend em C#/.NET 8 com Clean Architecture | Proposta |

## Como criar um ADR

1. Copie [`0000-template.md`](0000-template.md) para `NNNN-titulo-em-kebab-case.md`, usando o próximo número livre. Números nunca são reaproveitados.
2. Preencha com status **Proposta**. Liste pelo menos duas opções reais, com prós e contras. "Não fazer nada" conta como opção quando for plausível.
3. Referencie os requisitos afetados (`RF-xx`, `RNF-xx`, `RN-xx` do [DRP](../../01-produto/drp.md)) e os ADRs relacionados.
4. Abra um PR só com o ADR (template em `.github/ISSUE_TEMPLATE/adr.md`) e marque os decisores da tabela acima.
5. Ao aprovar, mude o status para **Aceita**, atualize este índice e, se for o caso, marque o ADR substituído como **Substituída por ADR-NNNN**.
6. A seção "Conformidade" diz como verificar que o código segue a decisão (teste, lint, checklist de revisão). Sem ela, o ADR não é aceito.
