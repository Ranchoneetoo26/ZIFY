# Documentação do Zyft

Mapa de todos os documentos. O produto atual é a **Candidatura Assistida (MVP 1)**, na **versão 2** (21/10/2026): onboarding em passos, área profissional dinâmica, campos personalizados, planos e anúncios sem dados pessoais, com pagamento simulado. A documentação do marketplace está em [`arquivo/marketplace-v1/`](arquivo/marketplace-v1/README.md) e **não** descreve o produto atual.

> Este índice passou a ser mantido à mão na v2, porque o gerador `ferramentas/gerar_indice_docs.py` não cobre as subpastas `entregas/` e `08-time/guias-de-estudo/`. Se o gerador for rodado, reinclua essas seções.

## Por onde começar

| Se você quer… | Leia |
|---|---|
| Entender o produto e o porquê de cada decisão | [Por que o Zyft](00-contexto/por-que-o-zyft.md) → [Resultados da pesquisa v2](05-pesquisa/resultados-v2.md) → [Personas](01-produto/personas.md) |
| Saber o que o sistema faz (requisitos) | [DVP-E](01-produto/dvp-e.md) → [DRP](01-produto/drp.md) → [Critérios de aceite](04-qualidade/criterios-de-aceite-mvp1.md) |
| Entender planos, preços e anúncios | [Monetização](01-produto/monetizacao.md) → [ADR-0020](03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md) |
| Entender como foi construído | [DAT](03-arquitetura/dat.md) → [Backend](03-arquitetura/backend.md) → [API](03-arquitetura/api.md) → [Modelo de dados](03-arquitetura/modelo-de-dados.md) → [ADRs](03-arquitetura/adr/README.md) |
| Usar o sistema ou demonstrar à banca | [Guia do usuário](06-operacao/guia-do-usuario.md) → [Runbook local](06-operacao/runbook-local.md) |
| Estudar para a apresentação | [Guias de estudo](08-time/guias-de-estudo/README.md) |
| Ver o que mudou na v2 | [Resumo da v2](entregas/entrega-3-v2-resumo.md) |

## Contexto

| Documento | Status |
|---|---|
| [Glossário](00-contexto/glossario.md) | Vigente |
| [Histórico e reposicionamento do Zyft](00-contexto/historico-e-reposicionamento.md) | Vigente |
| [Por que o Zyft existe e por que ele é assim](00-contexto/por-que-o-zyft.md) | Vigente |
| [plano-automacao-candidaturas.pdf](00-contexto/plano-automacao-candidaturas.pdf) (documento de origem) | Referência |
| [ZYFT_visao_produto_reposicionamento_MVP1_MVP2.docx](00-contexto/ZYFT_visao_produto_reposicionamento_MVP1_MVP2.docx) (documento de origem) | Referência |

## Produto

| Documento | Status |
|---|---|
| [DRP — Documento de Requisitos do Produto](01-produto/drp.md) | Aprovado para refinamento. RF-23 a RF-29 dependem dos ADRs 0018, 0019 e 0020 (propostos). |
| [DVP-E — Documento de Visão do Produto e Escopo](01-produto/dvp-e.md) | Aprovado para planejamento do MVP 1. O escopo v2 (E10 a E13) depende dos ADRs 0018, 0019 e 0020 (propostos). |
| [Fluxos do MVP 1](01-produto/fluxos.md) | Aprovado para refinamento |
| [Métricas](01-produto/metricas.md) | Metas fixadas a partir da pesquisa v2 (n = 15). Revisar depois do teste de usabilidade e do beta. |
| [Monetização: planos, limites e anúncios](01-produto/monetizacao.md) | Proposta. Depende do ADR-0020. Pagamento simulado. |
| [Personas do MVP 1](01-produto/personas.md) | Validado em parte pela pesquisa v2 (n = 15). Beatriz Lima é hipótese a validar. |
| [Roadmap](01-produto/roadmap.md) | Vigente (revisado a cada marco de decisão) |

## Viabilidade

| Documento | Status |
|---|---|
| [DVS — Documento de Viabilidade de Software](02-viabilidade/dvs.md) | Aprovado para a Fase 0 |

## Arquitetura

| Documento | Status |
|---|---|
| [Contrato da API v1](03-arquitetura/api.md) | Em revisão. Rotas da Entrega 3 implementadas; rotas da versão 2 do produto em implementação (ADR-0018, ADR-0019, ADR-0020, todos propostos). |
| [Arquitetura do backend (API .NET 8)](03-arquitetura/backend.md) | Em revisão. Estrutura e módulos da Entrega 3 implementados; módulos da versão 2 em implementação (o que ainda não foi conferido no código está marcado como… |
| [DAT — Documento de Arquitetura Técnica](03-arquitetura/dat.md) | Em revisão. A arquitetura-alvo depende dos ADRs marcados como "Proposta". |
| [Design system do Zyft](03-arquitetura/design-system.md) | Vigente para a v2 do MVP 1 |
| [Arquitetura da extensão de navegador](03-arquitetura/extensao.md) | Vigente para a v2 do MVP 1. O primeiro portal real depende do ADR-0011; anúncios e limites dependem do ADR-0020 (ambos propostos). |
| [Arquitetura do frontend web (plataforma)](03-arquitetura/frontend-web.md) | Vigente para a v2 do MVP 1. As telas de planos e anúncios dependem do ADR-0020 (proposto). |
| [Arquitetura de IA](03-arquitetura/ia.md) | Em revisão. A porta de IA e os guardrails estão aceitos; a hospedagem de produção é proposta (ADR-0005). |
| [Modelo de dados](03-arquitetura/modelo-de-dados.md) | Em revisão. As 17 tabelas da Entrega 3 estão implementadas; as tabelas da versão 2 estão em implementação (nomes das entidades em `src/Zyft.Domain`; a… |
| [Segurança e privacidade técnica](03-arquitetura/seguranca.md) | Em revisão. Revisar a cada ADR que toque autenticação, dados pessoais, permissões da extensão, IA, anúncios ou pagamento. |

### Decisões de arquitetura (ADRs)

| ADR | Status |
|---|---|
| [ADR-0001 — Reposicionamento: o MVP 1 é a Candidatura Assistida](03-arquitetura/adr/0001-reposicionamento-mvp1-candidatura-assistida.md) | Aceita |
| [ADR-0002 — Extensão de navegador Manifest V3 com painel lateral](03-arquitetura/adr/0002-extensao-de-navegador-mv3-com-painel-lateral.md) | Aceita |
| [ADR-0003 — Monorepo TypeScript/Node.js para API, web e extensão](03-arquitetura/adr/0003-monorepo-typescript-node.md) | Substituída por ADR-0017 |
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
| [ADR-0017 — Backend em C#/.NET 8 com Clean Architecture](03-arquitetura/adr/0017-backend-em-csharp-dotnet-clean-architecture.md) | Proposta |
| [ADR-0018 — Onboarding em passos com perguntas adiáveis](03-arquitetura/adr/0018-onboarding-em-passos-com-perguntas-adiaveis.md) | Proposta |
| [ADR-0019 — Área profissional dinâmica e campos personalizados](03-arquitetura/adr/0019-area-profissional-dinamica-e-campos-personalizados.md) | Proposta |
| [ADR-0020 — Monetização com anúncios e planos no MVP 1](03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md) | Proposta |

## Qualidade

| Documento | Status |
|---|---|
| [Critérios de aceite do MVP 1](04-qualidade/criterios-de-aceite-mvp1.md) | Aprovado para refinamento |
| [Estratégia de testes](04-qualidade/estrategia-de-testes.md) | Vigente |

## Pesquisa

| Documento | Status |
|---|---|
| [Plano de pesquisa — validação da Candidatura Assistida](05-pesquisa/plano-de-pesquisa.md) | Rodada 1 concluída (15 respostas válidas). Resultados em resultados-v2.md |
| [Questionário v2 — Candidatura Assistida](05-pesquisa/questionario-v2.md) | Em campo |
| [Resultados da pesquisa v2 — Candidatura Assistida](05-pesquisa/resultados-v2.md) | Vigente. Rodada 1 concluída com 15 respostas válidas |

## Operação

| Documento | Status |
|---|---|
| [Ambientes e implantação](06-operacao/ambientes.md) | Proposta (depende dos ADRs 0003, 0005, 0007 e 0008) |
| [Guia do usuário](06-operacao/guia-do-usuario.md) | Vigente para a v2 do MVP 1, conferido contra a implementação (21/10/2026). |
| [Runbook — rodar o Zyft localmente](06-operacao/runbook-local.md) | Vigente |
| [Vídeos de apresentação](06-operacao/videos.md) | Em produção: executivo, técnico e onboarding no sistema |

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

### Guias de estudo (preparação para a banca)

| Guia |
|---|
| [Guias de estudo do time](08-time/guias-de-estudo/README.md) |
| [Guia de estudo: Product Owner, Product Manager e gestão](08-time/guias-de-estudo/antonio-po-pm.md) |
| [Guia de estudo: arquitetura de software, IA e privacidade](08-time/guias-de-estudo/guilherme-arquitetura.md) |
| [Guia de estudo: frontend web, painel da extensão e UX](08-time/guias-de-estudo/lucas-frontend.md) |
| [Guia de estudo: backend .NET](08-time/guias-de-estudo/matheus-backend.md) |

## Entregas da disciplina

Documentos já enviados ficam como foram entregues. O que mudou depois está no resumo da v2.

| Documento |
|---|
| [Zyft — Entrega 2: Projeto e Arquitetura](entregas/entrega-2-projeto-e-arquitetura.md) |
| [Zyft — Entrega 3: Desenvolvimento e Validação](entregas/entrega-3-desenvolvimento-e-validacao.md) |
| [Entrega 3: frontend web e extensão](entregas/entrega-3-frontend.md) |
| [Entrega 3 — Relatório do backend (.NET 8)](entregas/entrega-3-relatorio-backend.md) |
| [Entrega 3: resumo da versão 2 do Zyft](entregas/entrega-3-v2-resumo.md) |
