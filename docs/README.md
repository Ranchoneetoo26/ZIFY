# Documentação do Zyft

Índice de todos os documentos, gerado por `ferramentas/gerar_indice_docs.py`. O produto atual é a **Candidatura Assistida (MVP 1)**. A documentação do marketplace está em [`arquivo/marketplace-v1/`](arquivo/marketplace-v1/README.md) e **não** descreve o produto atual.

Para quem está chegando, a ordem de leitura sugerida é: [histórico](00-contexto/historico-e-reposicionamento.md) → [DVP-E](01-produto/dvp-e.md) → [DRP](01-produto/drp.md) → [DAT](03-arquitetura/dat.md) → [ADRs](03-arquitetura/adr/README.md) → [processo](08-time/processo.md).

## Contexto

| Documento | Status |
|---|---|
| [Glossário](00-contexto/glossario.md) | Vigente |
| [Histórico e reposicionamento do Zyft](00-contexto/historico-e-reposicionamento.md) | Vigente |
| [plano-automacao-candidaturas.pdf](00-contexto/plano-automacao-candidaturas.pdf) (documento de origem) | Referência |
| [ZYFT_visao_produto_reposicionamento_MVP1_MVP2.docx](00-contexto/ZYFT_visao_produto_reposicionamento_MVP1_MVP2.docx) (documento de origem) | Referência |

## Produto

| Documento | Status |
|---|---|
| [DRP — Documento de Requisitos do Produto](01-produto/drp.md) | Aprovado para refinamento |
| [DVP-E — Documento de Visão do Produto e Escopo](01-produto/dvp-e.md) | Aprovado para planejamento do MVP 1 |
| [Fluxos do MVP 1](01-produto/fluxos.md) | Aprovado para refinamento |
| [Métricas](01-produto/metricas.md) | Candidatas — metas numéricas só depois da pesquisa v2 |
| [Personas do MVP 1](01-produto/personas.md) | Hipótese — a validar com a pesquisa v2 |
| [Roadmap](01-produto/roadmap.md) | Vigente (revisado a cada marco de decisão) |

## Viabilidade

| Documento | Status |
|---|---|
| [DVS — Documento de Viabilidade de Software](02-viabilidade/dvs.md) | Aprovado para a Fase 0 |

## Arquitetura

| Documento | Status |
|---|---|
| [Contrato da API v1](03-arquitetura/api.md) | Rascunho para revisão. A autenticação da extensão depende do ADR-0007 (proposto). |
| [Arquitetura do backend (API e worker)](03-arquitetura/backend.md) | Em revisão. Depende dos ADRs 0003, 0007 e 0008 (propostos). |
| [DAT — Documento de Arquitetura Técnica](03-arquitetura/dat.md) | Em revisão. A arquitetura-alvo depende dos ADRs marcados como "Proposta". |
| [Arquitetura da extensão de navegador](03-arquitetura/extensao.md) | Em revisão. O portal suportado depende do ADR-0011 e o pareamento do ADR-0007 (ambos propostos). |
| [Arquitetura do frontend web (plataforma)](03-arquitetura/frontend-web.md) | Em revisão. A stack depende do ADR-0003 (proposto). |
| [Arquitetura de IA](03-arquitetura/ia.md) | Em revisão. A porta de IA e os guardrails estão aceitos; a hospedagem de produção é proposta (ADR-0005). |
| [Modelo de dados](03-arquitetura/modelo-de-dados.md) | Em revisão. Depende do ADR-0008 (proposto). Os campos do perfil necessários no primeiro fluxo dependem do ponto V2 do DVP-E. |
| [Segurança e privacidade técnica](03-arquitetura/seguranca.md) | Em revisão. Revisar a cada ADR que toque autenticação, dados pessoais, permissões da extensão ou IA. |

### Decisões de arquitetura (ADRs)

| ADR | Status |
|---|---|
| [ADR-0001 — Reposicionamento: o MVP 1 é a Candidatura Assistida](03-arquitetura/adr/0001-reposicionamento-mvp1-candidatura-assistida.md) | Aceita |
| [ADR-0002 — Extensão de navegador Manifest V3 com painel lateral](03-arquitetura/adr/0002-extensao-de-navegador-mv3-com-painel-lateral.md) | Aceita |
| [ADR-0003 — Monorepo TypeScript/Node.js para API, web e extensão](03-arquitetura/adr/0003-monorepo-typescript-node.md) | Proposta |
| [ADR-0004 — A IA aponta a chave do perfil, nunca escreve o valor](03-arquitetura/adr/0004-ia-aponta-chave-nunca-escreve-valor.md) | Aceita |
| [ADR-0005 — Modelo open-weight atrás de uma porta `LLMProvider`](03-arquitetura/adr/0005-modelo-open-weight-self-hosted-atras-de-porta-llm.md) | Aceita (porta `LLMProvider` e uso de Ollama em desenvolvimento) / Proposta (hospedagem de produção) |
| [ADR-0006 — Regras determinísticas antes da IA no de-para](03-arquitetura/adr/0006-regras-deterministicas-antes-da-ia.md) | Aceita |
| [ADR-0007 — Autenticação da plataforma e pareamento da extensão](03-arquitetura/adr/0007-autenticacao-e-pareamento-da-extensao.md) | Proposta |
| [ADR-0008 — PostgreSQL com Perfil Mestre em JSONB](03-arquitetura/adr/0008-postgresql-com-perfil-em-jsonb.md) | Proposta |
| [ADR-0009 — O Zyft nunca envia; confirmação explícita da pessoa](03-arquitetura/adr/0009-nunca-enviar-confirmacao-explicita.md) | Aceita |
| [ADR-0010 — Sem credenciais, sem contorno de segurança, campos bloqueados](03-arquitetura/adr/0010-sem-credenciais-sem-contorno-de-seguranca-campos-bloqueados.md) | Aceita |
| [ADR-0011 — Primeiro portal e conectores por portal](03-arquitetura/adr/0011-primeiro-portal-e-conectores-por-portal.md) | Proposta |
| [ADR-0012 — Registro mínimo de conclusão no MVP 1](03-arquitetura/adr/0012-registro-minimo-de-conclusao-no-mvp1.md) | Proposta |
| [ADR-0013 — Evidência por linha numerada na análise de vaga](03-arquitetura/adr/0013-evidencia-por-linha-numerada-na-analise-de-vaga.md) | Aceita |
| [ADR-0014 — Dados sensíveis segregados e criptografados](03-arquitetura/adr/0014-dados-sensiveis-segregados-e-criptografados.md) | Aceita |
| [ADR-0015 — Observabilidade sem dados pessoais](03-arquitetura/adr/0015-observabilidade-sem-dados-pessoais.md) | Proposta |
| [ADR-0016 — Time de agentes de IA no desenvolvimento](03-arquitetura/adr/0016-time-de-agentes-de-ia-no-desenvolvimento.md) | Aceita |

## Qualidade

| Documento | Status |
|---|---|
| [Critérios de aceite do MVP 1](04-qualidade/criterios-de-aceite-mvp1.md) | Aprovado para refinamento |
| [Estratégia de testes](04-qualidade/estrategia-de-testes.md) | Vigente |

## Pesquisa

| Documento | Status |
|---|---|
| [Plano de pesquisa — validação da Candidatura Assistida](05-pesquisa/plano-de-pesquisa.md) | Em campo |
| [Questionário v2 — Candidatura Assistida](05-pesquisa/questionario-v2.md) | Em campo |

## Operação

| Documento | Status |
|---|---|
| [Ambientes e implantação](06-operacao/ambientes.md) | Proposta (depende dos ADRs 0003, 0005, 0007 e 0008) |
| [Runbook — rodar o protótipo localmente](06-operacao/runbook-local.md) | Vigente até a migração do protótipo para o monorepo (ADR-0003) |

## Legal

| Documento | Status |
|---|---|
| [Política de Privacidade — Zyft](07-legal/politica-de-privacidade.md) | Rascunho para revisão jurídica |
| [Termos de Uso — Zyft](07-legal/termos-de-uso.md) | Rascunho para revisão jurídica |

## Time

| Documento | Status |
|---|---|
| [Agentes de IA no time](08-time/agentes-de-ia.md) |  |
| [Papéis e matriz RACI](08-time/papeis-e-raci.md) |  |
| [Processo de trabalho](08-time/processo.md) |  |
