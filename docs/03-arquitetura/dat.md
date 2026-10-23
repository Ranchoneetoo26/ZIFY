# DAT — Documento de Arquitetura Técnica

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Arquitetura Técnica (DAT) |
| Versão | 2.3 (substitui o DAT do marketplace, arquivado em `docs/arquivo/marketplace-v1/dat.md`) |
| Status | Em revisão. A arquitetura-alvo depende dos ADRs marcados como "Proposta". |
| Dono | Arquitetura de software |
| Documentos relacionados | [DRP](../01-produto/drp.md), [Backend](backend.md), [Extensão](extensao.md), [Frontend web](frontend-web.md), [IA](ia.md), [Modelo de dados](modelo-de-dados.md), [API](api.md), [Segurança](seguranca.md), [ADRs](adr/) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.x | até 05/10/2026 | Arquitetura do marketplace (C#/.NET 8, PostgreSQL). Arquivada. |
| 2.0 | 12/10/2026 | Reescrita para a Candidatura Assistida: estado do protótipo, arquitetura-alvo do MVP 1, fluxos, qualidades e riscos. |
| 2.1 | 21/10/2026 | Nova seção "Versão 2 (21/10/2026)": o estado real é a API .NET 8 ([ADR-0017](adr/0017-backend-em-csharp-dotnet-clean-architecture.md)) e entram os componentes da versão 2 do produto. Tabela de evolução atualizada (planos do assistente no MVP 1; assinaturas do marketplace continuam MVP 2). |
| 2.2 | 21/10/2026 | Seções 3 a 6 reescritas para o sistema real, consistentes com o [backend](backend.md) e o ADR-0017: contexto sem e-mail nem login social; contêineres .NET 8 (Clean Architecture, MediatR/CQRS, FluentValidation, EF Core com PostgreSQL ou SQLite), Ollama atrás de `ILlmClient`, SPA em JavaScript puro servida pela API e extensão MV3 sem build; componentes da API e da extensão; estrutura real do repositório; fluxos com as rotas `/api/v1`, a cota de uso e o pareamento por código, incluindo a desconexão (`DELETE /extension/devices/current`). As seções 2 (protótipo) e 7 a 12 não mudaram. |
| 2.3 | 21/10/2026 | Contagem de testes atualizada: 252 (156 + 84 + 12). |

## Versão 2 (21/10/2026)

> Leia esta seção antes do resto. Desde a versão 2.2, as **seções 3 a 6 descrevem o sistema real**. A seção 2 continua descrevendo o protótipo Node de 05–06/10/2026 (ponto de partida histórico), e as seções 7 a 11 ainda trazem nomes da arquitetura-alvo em TypeScript de 12/10/2026 (módulos, tabelas e ferramentas); em caso de divergência, valem as seções 3 a 6 e os documentos específicos. Em resumo: a API foi construída em C#/.NET 8 com Clean Architecture ([ADR-0017](adr/0017-backend-em-csharp-dotnet-clean-architecture.md), que substitui o ADR-0003), a plataforma web é HTML/CSS/JavaScript sem build servida pela própria API, e a extensão MV3 pareia por código de 6 dígitos. Os detalhes atualizados estão em [backend](backend.md), [API](api.md), [modelo de dados](modelo-de-dados.md), [segurança](seguranca.md) e na [Entrega 3](../entregas/entrega-3-desenvolvimento-e-validacao.md). Os princípios (regras antes da IA, IA só aponta chave, nunca enviar, sensíveis segregados, logs sem dados pessoais) não mudaram.

A versão 2 do produto acrescenta, sem mudar a forma dos contêineres:

| Componente | Onde | Requisitos | Decisão |
|---|---|---|---|
| Onboarding em passos ("passaporte"), com perguntas adiáveis | Web (`#/boas-vindas`) + `GET/PUT /onboarding` | RF-23 | [ADR-0018](adr/0018-onboarding-em-passos-com-perguntas-adiaveis.md) |
| Catálogo por área e área profissional no perfil | `Application/Catalog` (estático) + `GET /catalog/areas` | RF-24 | [ADR-0019](adr/0019-area-profissional-dinamica-e-campos-personalizados.md) |
| Campos personalizados (`custom:<id>` no de-para e nos fatos da IA) | `Profiles` + `/profile/custom-fields` | RF-25 | ADR-0019 |
| Planos, cota de uso, pacote avulso, recompensado (pagamento simulado) | `Application/Billing` (`UsageMeter`, `PlanRules`) + `/billing` | RF-26 a RF-28 | [ADR-0020](adr/0020-monetizacao-com-anuncios-e-planos.md) |
| Anúncios rotulados, sem dados pessoais | `Application/Ads` (catálogo de demonstração) + `/ads`; web e painel da extensão | RF-29, RN-08 a RN-11 | ADR-0020 |
| Design system Zyft (tokens, componentes, estados de campo, voz) | Web e extensão | RNF-06, RNF-11 | [design-system.md](design-system.md) |

Requisitos e regras: [DRP](../01-produto/drp.md). Modelo de negócio: [monetização](../01-produto/monetizacao.md).

## 1. Objetivo e escopo

Este documento descreve como o Zyft é construído para atender ao MVP 1 definido no [DVP-E](../01-produto/dvp-e.md) e no [DRP](../01-produto/drp.md). Ele cobre:

- o protótipo funcional que já existe (seção 2), que é o ponto de partida técnico;
- a arquitetura implementada do MVP 1 em visão C4 (seções 3 a 5);
- os fluxos críticos em diagramas de sequência, com as rotas reais (seção 6);
- rastreabilidade, atributos de qualidade, ambientes, riscos e evolução para o MVP 2 (seções 7 a 12).

Detalhes por contêiner ficam nos documentos específicos: [backend](backend.md), [extensão](extensao.md), [frontend web](frontend-web.md), [IA](ia.md).

## 2. Estado atual (protótipo)

O protótipo foi construído em 05–06/10/2026, **antes** do reposicionamento, a partir do documento *Automação Assistida de Candidaturas* (`docs/00-contexto/plano-automacao-candidaturas.pdf`). O código está fora deste repositório (repositório local de um membro do time) e será migrado para o monorepo conforme o [ADR-0003](adr/0003-monorepo-typescript-node.md).

### 2.1 Visão geral

```
extension/ (Chrome/Edge, Manifest V3)        server/ (Node.js 22, Express, ESM)
  manifest.json   permissões e hosts           src/index.js     API REST, só 127.0.0.1, token Bearer
  background.js   abre o painel lateral        src/profile.js   modelo do Perfil Mestre, "achatamento"
  content.js      crawler: lê e preenche       src/mapper.js    de-para campo → chave do perfil
  sidepanel.*     revisão e aprovação          src/jobs.js      análise de vaga com evidência
                                               src/writer.js    respostas abertas, melhoria de texto
                                               src/importer.js  PDF/DOCX/TXT → proposta de perfil
                                               src/llm.js       cliente Ollama (JSON com schema)
                                               src/store.js     persistência em data/db.json
                                               src/text.js      normalização, tokens, Jaccard
                                               public/          plataforma web (HTML/JS puro)
                                               public/demo/     portal de vagas de teste
```

### 2.2 Componentes e comportamento

| Componente | Implementação | Observações |
|---|---|---|
| Servidor | Express 4, escuta só em `127.0.0.1:4317`. Middleware recusa `Host` diferente de `localhost`/`127.0.0.1` (proteção contra DNS rebinding). CORS só para origens `chrome-extension://`, `moz-extension://` e `edge-extension://`. | Um único usuário. |
| Autenticação | Token aleatório de 128 bits gerado no primeiro uso e gravado em `db.json`. A plataforma web (mesma origem) obtém o token por `GET /api/session`; a extensão recebe o token colado pela pessoa ou por `config.local.json`. | Token fixo, sem expiração nem revogação. |
| Persistência | `store.js` grava `db.json` de forma atômica (arquivo temporário e `rename`). Contém token, consentimentos, perfil, candidaturas e log (últimos 500 eventos). | Sem criptografia em repouso. |
| Perfil Mestre | `profile.js`: seções `personal`, `objective`, `experiences`, `education`, `courses`, `certifications`, `skills`, `languages`, `preferences`, `approvedAnswers` e `sensitive` (separado). `flattenProfile` gera chaves como `experiences[0].company` com rótulo legível, **sem** os dados sensíveis. `profileFacts` gera linhas de fatos e calcula no código o tempo total de experiência. | É o contrato central entre mapeamento, IA e interface. |
| IA | `llm.js` chama `POST /api/chat` do Ollama com `format` = JSON Schema, temperatura 0,1 (0,3 para redação), `num_ctx` 8192 e um prompt de sistema com regras inegociáveis (só fatos do perfil, não inferir sensíveis, pt-BR). Modelo padrão `qwen2.5:7b`, configurável por `OLLAMA_MODEL`. | Timeout de 180 s (300 s na importação). |
| De-para | `mapper.js`: (1) bloqueios: `password`/`hidden`/`submit` ignorados, CAPTCHA e `file` viram "manual", campos sensíveis viram "sensível" (valor só com consentimento e sem pré-seleção), checkbox de declaração vira "declaração"; (2) regras em PT/EN (`TOP_RULES`, 30 chaves) e grupos repetíveis (`GROUPS`: experiências, formação, cursos, certificações, idiomas), com alinhamento de blocos por organização/instituição (`alignSlots`); (3) o que sobra vai à IA, que devolve `fill` com `profileKey`, `generate` ou `ask`; a chave é validada contra as chaves existentes e o **valor sempre vem do perfil**; (4) `pickOption` só escolhe opções que existem na página (exata, contida, sim/não ou Jaccard ≥ 0,5); sem opção segura, o campo fica "ambíguo". | Rodar só as regras leva milissegundos; a etapa da IA leva segundos. |
| Texto livre | `freeText` reaproveita uma resposta aprovada se a pergunta for parecida (Jaccard ≥ 0,6). Senão, `writer.generateAnswer` pede `answer`, `usedFacts` e `missingInfo`, e respeita `maxLength`. A sugestão nunca vem pré-selecionada. | Usa a vaga analisada como contexto, marcada como "não são qualificações". |
| Análise de vaga | `jobs.js`: a IA extrai cargo, empresa, local, contrato, modalidade, jornada, salário, resumo e requisitos; depois avalia cada requisito (até 20) contra fatos **numerados** e devolve o número da linha. "Atende" ou "parcial" sem linha real vira "não encontrado". `CONFIRM_RULES` detecta por regex pontos a confirmar (CNH, turnos, viagens, salário, modalidade, mudança, contrato). | Bloqueada na API se o consentimento `jobAnalysis` estiver desligado. Sempre com aviso de que não é garantia. |
| Importação | `importer.js`: PDF (`pdf-parse`), DOCX (`mammoth`) e TXT até 10 MB (`multer`). A IA converte o texto (até 12.000 caracteres) em JSON com schema, com instrução de não extrair sensíveis; limpezas determinísticas depois. Devolve uma **proposta**; nada é gravado sem a pessoa salvar. `missingQuestions` lista até 11 perguntas sobre dados ausentes. | PDF escaneado não é suportado. |
| Extensão | MV3 com `sidePanel`, `storage`, `scripting` e `tabs`. Hosts fixos: localhost, `*.maringa.com`, `*.gupy.io`, `*.linkedin.com` e `*.workana.com`; outros sites por `optional_host_permissions`, pedidos no primeiro uso. `content.js` é injetado sob demanda (`chrome.scripting.executeScript`). | "Conector" é só um rótulo por domínio; não há regras específicas por portal. |
| Crawler | `content.js` lê `input`, `select` e `textarea` visíveis; com modal aberto (`[role=dialog]`, `[aria-modal]`, `dialog[open]`), lê só o modal. Para cada campo: rótulo (aria-labelledby, label[for], label envolvente, aria-label, texto anterior, texto do pai), seção (legend ou último título), tipo, opções, `maxLength`, `required` e valor atual. Agrupa radios. Marca cada campo com `data-ca-id`. | Não lê iframes nem shadow DOM. |
| Preenchimento | Usa o setter nativo de `value` e dispara `input`, `change` e `blur` (compatível com React/Vue); radio e checkbox por `click()`; recusa `file` e `password`; destaca o resultado com contorno verde ou vermelho. **Não existe código que clique em enviar.** | Escuta o evento `submit` feito pela pessoa para sugerir o registro. |
| Painel lateral | Fluxo: "Analisar vaga" → "Ler formulário" (de-para) → revisão por campo (estados: perfil, resposta aprovada, sugestão IA, sensível, declaração, manual, ambíguo, falta no perfil, sem correspondência) → "Preencher selecionados". Divergência entre portal e perfil: o padrão é manter o portal, com as opções "usar o perfil" e "importar do portal" (com `confirm`). | Depois do envio, sugere registrar a candidatura no histórico. |
| Plataforma web | HTML e JS puros com abas: Perfil Mestre, Importar currículo, Candidaturas (10 status), Respostas aprovadas e Configurações (consentimentos, token, status da IA, exportar e excluir, registro de automação). | O histórico de candidaturas é escopo de MVP 2 (ver [ADR-0012](adr/0012-registro-minimo-de-conclusao-no-mvp1.md)). |
| Portal demo | `/demo/`: currículo vazio, currículo já preenchido (para importar e comparar), vaga e candidatura com triagem, pergunta aberta, dado sensível, termos, anexo e CAPTCHA. | Usado nos testes E2E com Playwright e nos vídeos. |

### 2.3 O que o protótipo provou

- O pipeline regras → IA aponta chave → validação preenche formulários longos de currículo **sem a IA escrever valores objetivos**.
- A evidência por linha numerada torna visível quando o modelo erra a classificação de um requisito.
- Um modelo de 7B local (`qwen2.5:7b`, cerca de 4,7 GB, numa GPU de 12 GB) é suficiente para de-para, redação curta e análise de vaga, **com ressalvas**: a classificação varia entre execuções (por exemplo, "parcial" em vez de "atende"), e a análise de vaga leva de 20 a 30 s.

### 2.4 Lacunas para o MVP 1

| Lacuna | Requisito afetado | Tratamento |
|---|---|---|
| Um usuário, servidor local, token fixo | RF-01, RF-09, RNF-03 | Contas, API na nuvem, pareamento com tokens curtos ([ADR-0007](adr/0007-autenticacao-e-pareamento-da-extensao.md)) |
| `db.json` sem criptografia | RNF-07, RN-04 | PostgreSQL, sensíveis criptografados por campo ([ADR-0008](adr/0008-postgresql-com-perfil-em-jsonb.md), [ADR-0014](adr/0014-dados-sensiveis-segregados-e-criptografados.md)) |
| `map-fields` devolve também o `profileValue` dos campos ambíguos e o perfil é enviado à IA com valores | RNF-02 | Contrato de mínimo retorno; prompt da IA com valores truncados e sem identificadores diretos ([api.md](api.md), [ia.md](ia.md)) |
| Hosts de quatro portais fixos no manifesto | RF-10, DVP-E §7.2 | Um portal suportado ([ADR-0011](adr/0011-primeiro-portal-e-conectores-por-portal.md)) |
| Conector genérico, sem regras por portal | RNF-10 | Pacote de conectores com fixtures gravadas |
| Histórico com 10 status | Fora do MVP 1 | [ADR-0012](adr/0012-registro-minimo-de-conclusao-no-mvp1.md) |
| JavaScript sem tipos, sem testes unitários | RNF-10 | TypeScript, testes por camada ([estratégia de testes](../04-qualidade/estrategia-de-testes.md)) |
| Logs sem política de dados pessoais (o `fill-log` grava a URL completa) | RNF-09 | [ADR-0015](adr/0015-observabilidade-sem-dados-pessoais.md) |
| **Senha lida pelo crawler.** A `scanFields()` de `extension/content.js` não exclui `type=password` da leitura: o valor de um campo de senha visível vai como `currentValue` em `POST /api/map-fields`. O servidor descarta o campo, mas o valor já trafegou (viola RNF-01). | RNF-01 | O crawler descarta, antes de ler o valor, campos `type=password` e campos com `autocomplete` `current-password`, `new-password` ou `one-time-code`; teste unitário e E2E obrigatórios([ADR-0010](adr/0010-sem-credenciais-sem-contorno-de-seguranca-campos-bloqueados.md)). |

## 3. Visão de contexto (C4 nível 1)

```mermaid
flowchart LR
  P([Pessoa candidata])
  subgraph Zyft
    Z[Sistema Zyft<br/>API .NET 8 + plataforma web,<br/>extensão MV3, IA local]
  end
  Portal[(Portal de vagas<br/>site de terceiros)]
  Demo[(Portal de demonstração<br/>/demo/, servido pela API)]

  P -- "mantém o Perfil Mestre,<br/>consentimentos e dados" --> Z
  P -- "faz login e envia a candidatura<br/>por conta própria" --> Portal
  Z -- "lê e preenche o DOM da página<br/>aberta pela pessoa (extensão)" --> Portal
  Z -- "mesmo fluxo, para testes e vídeos" --> Demo
```

O Zyft **não** se integra a portais por API, **não** faz requisições ao portal (RNF-13) e **não** conhece a senha do portal (RNF-01). Toda interação com o portal acontece no DOM da aba que a pessoa abriu.

Não há sistema externo de e-mail nem de login social no MVP 1 implementado: a conta é por e-mail e senha (BCrypt), sem verificação de e-mail. Verificação de e-mail e login com Google estão previstos para a Entrega 4. O serviço de IA (Ollama) roda ao lado da API e é opcional: sem ele, o de-para segue só com regras (RNF-12).

## 4. Visão de contêineres (C4 nível 2)

```mermaid
flowchart TB
  P([Pessoa candidata])
  subgraph Navegador da pessoa
    EXT[Extensão MV3 0.4.0<br/>JavaScript sem build<br/>painel lateral + service worker]
    CS[Content script<br/>injetado sob demanda]
    WEB[Plataforma web<br/>HTML, CSS e JavaScript puro<br/>SPA com rotas por hash]
    PG[(Página do portal)]
  end
  subgraph Servidor Zyft
    API[Zyft.Api<br/>ASP.NET Core 8<br/>serve /api/v1, a web em / e o demo em /demo/]
    DB[(Banco<br/>PostgreSQL 16 ou SQLite<br/>EF Core 8)]
    LLM[Ollama<br/>qwen2.5:7b<br/>opcional]
  end

  P --> WEB
  P --> EXT
  EXT <-- "chrome.tabs.sendMessage" --> CS
  CS <-- "DOM" --> PG
  WEB -- "HTTPS, Bearer scope=web<br/>(mesma origem)" --> API
  EXT -- "HTTPS, Bearer scope=extension<br/>(só o service worker)" --> API
  API -- "EF Core" --> DB
  API -- "porta ILlmClient<br/>HTTP /api/chat com JSON Schema" --> LLM
```

| Contêiner | Tecnologia (implementada) | Responsabilidade | Documento |
|---|---|---|---|
| Plataforma web | HTML, CSS e JavaScript puro (módulos ES, sem build), rotas por hash (`#/perfil`, `#/planos`…), servida pela própria API a partir de `web/` | Conta, onboarding em passos, Perfil Mestre, importação com revisão, banco de respostas, consentimentos, dados sensíveis, pareamento e navegadores conectados, sites autorizados, candidaturas, registro de ações, planos e anúncios, exportar e excluir | [frontend-web.md](frontend-web.md) |
| Extensão | Manifest V3, JavaScript sem build: `background.js` (service worker, único que guarda tokens e fala com a API), `content.js` (lê e preenche a página), `sidepanel.{html,js,css}` (painel lateral) | Reconhecer o site, ler campos e a vaga, mostrar o de-para para revisão, preencher só o aprovado, registrar a conclusão; medidor de uso e anúncios do painel | [extensao.md](extensao.md) |
| API | C#/.NET 8, ASP.NET Core, Clean Architecture em quatro projetos (`Zyft.Domain`, `Zyft.Application`, `Zyft.Infrastructure`, `Zyft.Api`), MediatR (CQRS), FluentValidation, Serilog, Swagger, rate limiting, health checks | Regras de negócio, autenticação (JWT + refresh rotativo), pareamento, de-para, orquestração da IA, cota de uso, anúncios, auditoria; 77 operações em `/api/v1` | [backend.md](backend.md), [api.md](api.md) |
| Banco | EF Core 8. PostgreSQL 16 (Npgsql, migrations) em Docker; SQLite (`EnsureCreated`) no desenvolvimento local e nos testes | Conta, Perfil Mestre **relacional** (uma tabela por seção), consentimentos, sensíveis cifrados (AES-256-GCM), dispositivos, candidaturas, auditoria, planos e uso; 25 tabelas | [modelo-de-dados.md](modelo-de-dados.md) |
| Serviço de IA | Ollama com modelo open-weight (`qwen2.5:7b`), atrás da porta `ILlmClient` (`OllamaLlmClient`; `NullLlmClient` quando desligado) | De-para residual (só aponta chave), texto livre com fatos, extração de currículo, extração e avaliação de requisitos da vaga | [ia.md](ia.md) |

Não há worker nem fila: as chamadas de IA são síncronas, com tempo limite e queda para as regras quando a IA não responde (RNF-12). A escolha está no [ADR-0017](adr/0017-backend-em-csharp-dotnet-clean-architecture.md) (Proposta), que substitui o [ADR-0003](adr/0003-monorepo-typescript-node.md); a porta de IA segue o [ADR-0005](adr/0005-modelo-open-weight-self-hosted-atras-de-porta-llm.md). O ADR-0017 ajusta o [ADR-0008](adr/0008-postgresql-com-perfil-em-jsonb.md): o perfil é relacional, não JSONB.

## 5. Visão de componentes (C4 nível 3)

### 5.1 API

```mermaid
flowchart LR
  subgraph Zyft.Api
    MW[Middleware<br/>Serilog, ExceptionHandling → ProblemDetails,<br/>JWT, policies WebUser / ExtensionOrWeb,<br/>rate limiting, CORS]
    CTRL[Controllers finos<br/>17, só Sender.Send]
  end
  subgraph Zyft.Application
    PIPE[Pipeline MediatR<br/>LoggingBehavior → ValidationBehavior<br/>FluentValidation]
    H[Handlers por módulo<br/>Auth, Profiles, Imports, Answers, Sensitive,<br/>Extension, Sites, Forms, Writing, Jobs,<br/>Applications, Audit, Catalog, Onboarding,<br/>Billing, Ads]
    SVC[Serviços<br/>FieldMapper + FieldRules, AnswerWriter,<br/>JobAnalyzer, ProfileFacts, UsageMeter,<br/>AdSelector, ConsentGuard]
    PORTS[Portas<br/>IRepository, IUnitOfWork, ILlmClient,<br/>ITokenService, IPasswordHasher,<br/>ISensitiveDataProtector, IResumeTextExtractor,<br/>ICurrentUser, IClock]
  end
  subgraph Zyft.Domain
    DOM[Entidades e regras puras<br/>Profile, CustomField, ExtensionDevice,<br/>MonthlyUsage, PlanRules, AuditEvent…]
  end
  subgraph Zyft.Infrastructure
    INF[EF Core: ZyftDbContext, migrations,<br/>repositórios, UnitOfWork<br/>JWT, BCrypt, AES-256-GCM<br/>OllamaLlmClient, PdfPig/OpenXml]
  end
  MW --> CTRL --> PIPE --> H
  H --> SVC & DOM & PORTS
  SVC --> DOM & PORTS
  INF -. implementa .-> PORTS
```

Regras de dependência: `Domain` não referencia nada; `Application` só referencia `Domain`; `Infrastructure` implementa as portas; `Api` compõe tudo por injeção de dependência e nenhum controller acessa o `DbContext`. Detalhes por módulo, cota de uso, anúncios e erros estão no [backend](backend.md).

### 5.2 Extensão

```mermaid
flowchart LR
  subgraph Extensão MV3
    SW[background.js<br/>service worker: tokens,<br/>chamadas à API, pareamento,<br/>desconexão]
    SP[sidepanel.js<br/>painel: abas "A vaga e você"<br/>e "Formulário", revisão,<br/>sensíveis trancados, anúncios]
    CS[content.js<br/>lê campos e a vaga,<br/>preenche o aprovado,<br/>avisa o envio feito pela pessoa]
  end
  API[(API /api/v1)]
  SP <-- "chrome.runtime.sendMessage<br/>(só de páginas da extensão)" --> SW
  SP <-- "chrome.tabs.sendMessage<br/>scan, job, fill, highlight" --> CS
  SW -- "fetch com Bearer" --> API
```

Não existe conector por portal nem regra específica de site: o content script é genérico e as regras de campo ficam na API (`FieldRules`). O conector por portal continua como evolução ([ADR-0011](adr/0011-primeiro-portal-e-conectores-por-portal.md)).

### 5.3 Estrutura do repositório

As regras determinísticas e o esquema do perfil ficam **só na API** (C#); a web e a extensão não duplicam regras de negócio, só a apresentação.

```
ZIFY/
  Zyft.sln
  src/
    Zyft.Domain/          entidades, enums, regras puras (sem pacotes)
    Zyft.Application/     commands/queries (MediatR), validators, DTOs, portas, FieldMapper,
                          FieldRules, AnswerWriter, JobAnalyzer, catálogos estáticos (áreas, anúncios)
    Zyft.Infrastructure/  EF Core (PostgreSQL/SQLite), migrations, JWT, BCrypt, AES-GCM,
                          OllamaLlmClient, extração de PDF/DOCX
    Zyft.Api/             controllers, middleware, policies, Swagger, Serilog, Program.cs
  tests/
    Zyft.UnitTests/         xUnit + FluentAssertions + Moq
    Zyft.IntegrationTests/  WebApplicationFactory + SQLite em memória
    Zyft.AcceptanceTests/   Reqnroll (Gherkin em português)
  web/                    plataforma (index.html, css/, js/, js/views/) e portal demo (demo/)
  extension/              manifest.json, background.js, content.js, sidepanel.*, icons/, fonts/
  docs/                   esta documentação
  ferramentas/            scripts de apoio (pesquisa, geradores das entregas)
  docker-compose.yml      API + PostgreSQL 16 (+ Ollama no perfil "ai")
```

Ferramentas: .NET 8 SDK (`global.json`), `dotnet test` (252 testes: 156 de unidade, 84 de integração, 12 de aceitação), Docker Compose e CI no GitHub Actions ([estratégia de testes](../04-qualidade/estrategia-de-testes.md)).

## 6. Fluxos principais

Rotas relativas a `/api/v1`. Toda resposta de erro é ProblemDetails em português; limite do plano estourado responde 402 `plan.limit_reached` ([API §4.5](api.md)).

### 6.1 Importação de currículo (RF-03, RF-04, RF-05)

```mermaid
sequenceDiagram
  actor P as Pessoa
  participant W as Plataforma web
  participant A as API
  participant L as Ollama
  participant D as Banco

  P->>W: envia PDF ou DOCX (até 10 MB)
  W->>A: POST /resume-imports (multipart, scope=web)
  A->>A: valida tipo e tamanho (400); cota resumeImports (402 se esgotada)
  A->>A: extrai o texto em memória (PdfPig/OpenXml); o arquivo não é guardado<br/>PDF sem texto → 422 import.no_text
  alt IA disponível
    A->>L: texto + JSON Schema do perfil, sem pedir sensíveis
    L-->>A: proposta em JSON
  else IA fora do ar
    A->>A: ResumeParser cai na heurística
  end
  A->>D: ResumeImport (status Pending), evento e consumo da cota no mesmo SaveChanges<br/>(se algo falhar antes, nada é cobrado)
  A-->>W: 201 com a proposta
  P->>W: aceita, edita ou descarta cada item
  W->>A: POST /resume-imports/{id}/apply (ou /discard)
  A->>D: grava no Perfil Mestre; evento de auditoria sem valores
  A-->>W: perfil atualizado + perguntas sobre o que falta (RF-05)
```

### 6.2 Ler formulário, de-para, revisão e preenchimento (RF-10 a RF-14, RF-16, RF-18)

```mermaid
sequenceDiagram
  actor P as Pessoa
  participant SP as Painel lateral
  participant CS as Content script
  participant SW as Service worker
  participant A as API
  participant L as Ollama

  P->>SP: "Ler o formulário desta página"
  SP->>SP: site autorizado? (senão pede permissão do host)
  SP->>CS: scan
  CS->>CS: lê campos visíveis (ou só o modal)<br/>descarta senha antes de ler; ignora hidden e submit
  CS-->>SP: {fields, url, title, host}
  SP->>SW: api POST /forms/mappings
  SW->>A: POST /forms/mappings (Bearer scope=extension)
  A->>A: cota assistedApplications por origem + caminho (402 se esgotada)
  A->>A: bloqueios (senha, CAPTCHA, anexo, termos, sensível sem consentimento)
  A->>A: FieldRules sobre rótulos normalizados + blocos repetidos + custom:<id>
  A->>A: respostas aprovadas parecidas
  alt sobram campos e IA disponível
    A->>L: rótulos restantes + chaves do perfil (sem valores sensíveis)
    L-->>A: {fieldId, profileKey}
    A->>A: valida chave; valor sai do perfil; opção precisa existir
  end
  A-->>SW: propostas (Ready, Ambiguous, Missing, Divergent, Blocked)
  SW-->>SP: propostas
  SP-->>P: Prontos compactos, sensíveis trancados um a um, "Ação sua"
  P->>SP: aprova (destranca sensíveis um a um), edita, pula
  P->>SP: "Preencher N campos aprovados"
  SP->>CS: fill(itens aprovados)
  CS->>CS: setter nativo + input/change/blur
  CS-->>SP: relatório por campo
  P->>P: envia o formulário no portal, por conta própria
  CS-->>SP: aviso "submitted" (envio feito pela pessoa)
  SP-->>P: tela de conclusão
  P->>SP: "Concluí a candidatura"
  SP->>SW: api POST /forms/completions
  SW->>A: registra a candidatura e o evento de auditoria (sem valores)
```

A extensão nunca chama `submit()`, `requestSubmit()` nem clica em botões ([ADR-0009](adr/0009-nunca-enviar-confirmacao-explicita.md)). As regras determinísticas são o primeiro passo depois dos bloqueios ([ADR-0006](adr/0006-regras-deterministicas-antes-da-ia.md)); a normalização dos rótulos está descrita em [IA §2](ia.md).

### 6.3 Texto livre (RF-15, RF-06)

```mermaid
sequenceDiagram
  participant SP as Painel lateral
  participant A as API
  participant L as Ollama

  SP->>A: POST /forms/open-answers {question, maxLength, contexto da vaga}
  A->>A: consentimento "IA para textos"? senão 403
  A->>A: cota aiSuggestions (402 se esgotada)
  alt resposta aprovada parecida no banco (Jaccard ≥ 0,6)
    A-->>SP: resposta aprovada, para revisar
  else
    A->>A: ProfileFacts (sem sensíveis; campos personalizados como fatos)
    A->>L: fatos + pergunta + limite
    L-->>A: {answer, usedFacts}
    A->>A: corta no limite; confere usedFacts contra os fatos
    A-->>SP: rascunho (AiText), nunca pré-selecionado
  end
  Note over SP: aprovar exige marcar "Li e tudo o que está escrito é verdade sobre mim."
  SP->>A: (opcional) POST /answers ao guardar no banco de respostas
```

### 6.4 Análise de vaga (RF-17)

```mermaid
sequenceDiagram
  actor P as Pessoa
  participant SP as Painel lateral
  participant CS as Content script
  participant A as API
  participant L as Ollama

  P->>SP: "Analisar esta vaga"
  SP->>CS: job
  CS-->>SP: {url, title, text ≤ 15.000 caracteres}
  SP->>A: POST /jobs/analyze
  A->>A: consentimento "análise de vaga"? senão 403 (sem chamar a IA)
  A->>A: cota jobAnalyses (402 se esgotada)
  A->>L: extrair dados visíveis e requisitos (schema)
  loop cada requisito (até 20)
    A->>L: fatos numerados + requisito
    L-->>A: {status, line}
    A->>A: "atende"/"parcial" sem linha real → "não encontrado"
  end
  A->>A: pontos a confirmar por regra (CNH, salário, modalidade...)
  A-->>SP: requisitos + evidência (linha real) + aviso
  SP-->>P: sem nota nem porcentagem; anúncio contextual só depois do conteúdo
```

### 6.5 Pareamento e desconexão da extensão (RF-09)

Implementado com o fluxo de **código curto** do [ADR-0007](adr/0007-autenticacao-e-pareamento-da-extensao.md); o fluxo PKCE com `launchWebAuthFlow` ficou para depois do MVP 1 acadêmico ([ADR-0017](adr/0017-backend-em-csharp-dotnet-clean-architecture.md)).

```mermaid
sequenceDiagram
  actor P as Pessoa
  participant W as Plataforma web
  participant SP as Painel lateral
  participant SW as Service worker
  participant A as API

  P->>W: Extensão → "Gerar código de pareamento"
  W->>A: POST /extension/pairing-codes (scope=web)
  A-->>W: código de 6 dígitos, 10 min, uso único
  P->>SP: digita o código e o nome do navegador
  SP->>SW: pair {code, deviceName}
  SW->>A: POST /extension/pair
  A->>A: limite de navegadores do plano (402 devices se cheio)
  A-->>SW: {device, tokens: {accessToken 15 min, refreshToken 7 dias}}
  SW->>SW: access em chrome.storage.session;<br/>refresh em chrome.storage.local
  Note over W,A: a plataforma lista os navegadores (GET /extension/devices)<br/>e revoga qualquer um (DELETE /extension/devices/{id})
  P->>SP: Ajustes → "Desconectar extensão"
  SP->>SW: unpair
  SW->>A: DELETE /extension/devices/current (revoga o próprio dispositivo, libera a vaga)
  SW->>A: POST /auth/logout {refreshToken}
  SW->>SW: apaga os tokens locais
```

A revogação vale na hora: a validação do JWT confere a cada requisição se o `device_id` ainda está ativo. Um token web chamando `DELETE /extension/devices/current` recebe 404, porque não tem `device_id`.

## 7. Mapeamento requisito → componente

| Requisito | Web | Extensão | API (módulo) | IA | Banco |
|---|---|---|---|---|---|
| RF-01 Conta | Telas de conta | — | auth | — | users, auth_identities |
| RF-02 Perfil | Editor do perfil | — | profile | — | profiles |
| RF-03/04 Importação | Revisão lado a lado | — | import + worker | Extração | resume_imports |
| RF-05 Perguntas do que falta | Lista de pendências | — | profile | — | — |
| RF-06 Respostas aprovadas | Biblioteca | Reuso | writing | — | approved_answers |
| RF-07 Consentimentos | Configurações | Respeita estado | consent | — | consents |
| RF-08 Exportar/excluir | Configurações | Revogação | privacy | — | todas |
| RF-09 Pareamento | Autorização, confirmação de código, lista | `launchWebAuthFlow` + PKCE (alternativa: código) | auth | — | extension_devices, device_pairings |
| RF-10 Página autorizada | — | Conector, permissões | — | — | — |
| RF-11 Leitura | — | Crawler | — | — | — |
| RF-12 De-para | — | Painel | mapping | Apontar chave | — |
| RF-13 Revisão | — | Painel | — | — | — |
| RF-14 Preenchimento | — | Filler | audit | — | audit_events |
| RF-15 Texto livre | — | Painel | writing | Redação | — |
| RF-16 Portal × perfil | — | Painel | profile (patch) | — | profiles |
| RF-17 Vaga | — | Painel, crawler | job-analysis | Extração e evidência | — (efêmero) |
| RF-18 Bloqueios | — | Crawler, painel | mapping | — | — |
| RF-19 Conclusão | — | Painel | (ADR-0012) | — | completion_records (opcional) |
| RF-20 Registro | Registro de ações | Envia eventos | audit | — | audit_events |

## 8. Decisões de arquitetura (ADRs)

| ADR | Título | Status |
|---|---|---|
| [0001](adr/0001-reposicionamento-mvp1-candidatura-assistida.md) | Reposicionamento: MVP 1 é a Candidatura Assistida | Aceita |
| [0002](adr/0002-extensao-de-navegador-mv3-com-painel-lateral.md) | Extensão de navegador MV3 com painel lateral | Aceita |
| [0003](adr/0003-monorepo-typescript-node.md) | Monorepo TypeScript/Node.js (substitui C#/.NET do marketplace) | Proposta |
| [0004](adr/0004-ia-aponta-chave-nunca-escreve-valor.md) | A IA aponta a chave e nunca escreve o valor | Aceita |
| [0005](adr/0005-modelo-open-weight-self-hosted-atras-de-porta-llm.md) | Modelo open-weight self-hosted atrás de uma porta de IA | Aceita (porta); Proposta (hospedagem de produção) |
| [0006](adr/0006-regras-deterministicas-antes-da-ia.md) | Regras determinísticas antes da IA | Aceita |
| [0007](adr/0007-autenticacao-e-pareamento-da-extensao.md) | Autenticação e pareamento da extensão | Proposta |
| [0008](adr/0008-postgresql-com-perfil-em-jsonb.md) | PostgreSQL com perfil em JSONB | Proposta |
| [0009](adr/0009-nunca-enviar-confirmacao-explicita.md) | Nunca enviar; confirmação explícita | Aceita |
| [0010](adr/0010-sem-credenciais-sem-contorno-de-seguranca-campos-bloqueados.md) | Sem credenciais, sem contorno de segurança, campos bloqueados | Aceita |
| [0011](adr/0011-primeiro-portal-e-conectores-por-portal.md) | Primeiro portal e conectores por portal | Proposta (portal a definir) |
| [0012](adr/0012-registro-minimo-de-conclusao-no-mvp1.md) | Registro mínimo de conclusão no MVP 1 | Proposta |
| [0013](adr/0013-evidencia-por-linha-numerada-na-analise-de-vaga.md) | Evidência por linha numerada na análise de vaga | Aceita |
| [0014](adr/0014-dados-sensiveis-segregados-e-criptografados.md) | Dados sensíveis segregados e criptografados | Aceita |
| [0015](adr/0015-observabilidade-sem-dados-pessoais.md) | Observabilidade sem dados pessoais | Proposta |
| [0016](adr/0016-time-de-agentes-de-ia-no-desenvolvimento.md) | Time de agentes de IA no desenvolvimento | Aceita |

## 9. Atributos de qualidade

| Atributo | Requisito | Tática arquitetural |
|---|---|---|
| Desempenho | RNF-04 | Regras rodam antes e sem IA (milissegundos); uma única chamada de IA por leitura de formulário, só com os campos restantes; análise de vaga com progresso e, se preciso, no worker; cache por hash do texto da vaga durante a sessão. |
| Confiabilidade da IA | RNF-05, RN-02, RN-03 | IA aponta a chave e nunca escreve o valor; JSON Schema obrigatório com `enum` de `fieldId`; validação de chave e opção no servidor; evidência por linha; fatos derivados calculados em código; suíte de avaliação no CI. |
| Segurança | RNF-01, RNF-03 | Tokens curtos e revogáveis; TLS; CORS restrito; nenhum campo `password` lido; CSP estrita na web e na extensão; ver [seguranca.md](seguranca.md). |
| Privacidade | RNF-02, RNF-07, RN-04 | Mínimo retorno; sensíveis em cofre criptografado e fora dos prompts; arquivo de currículo descartado após a extração; exportação e exclusão. |
| Disponibilidade | RNF-12 | Meta de 99,5%; sem IA, a extensão continua com as regras (estados "sem correspondência" em vez de erro); health checks; banco gerenciado com backup diário. |
| Manutenibilidade | RNF-10 | Conectores versionados com fixtures gravadas; pacotes compartilhados; TypeScript `strict`; ADRs. |
| Acessibilidade | RNF-06 | Componentes acessíveis no pacote `ui`; testes axe no CI. |
| Observabilidade | RNF-09 | Logs estruturados com lista de campos permitidos; métricas por etapa do pipeline; eventos de auditoria separados da telemetria ([ADR-0015](adr/0015-observabilidade-sem-dados-pessoais.md)). |

## 10. Ambientes

| Ambiente | Uso | IA | Dados |
|---|---|---|---|
| Local | Desenvolvimento; `docker compose` com PostgreSQL e Ollama | Ollama local (`qwen2.5:7b`) | Perfis sintéticos (`packages/test-fixtures`) |
| CI | Lint, testes unitários, contrato, E2E com o portal demo, avaliação de IA | Ollama em runner com GPU, ou adaptador gravado (respostas fixadas) para PRs | Sintéticos |
| Homologação | Testes de usabilidade com pessoas convidadas | Mesmo serviço de produção, isolado | Reais, de participantes com consentimento; apagados ao fim do ciclo |
| Produção | MVP 1 | A decidir ([ADR-0005](adr/0005-modelo-open-weight-self-hosted-atras-de-porta-llm.md)) | Reais |

Hospedagem da API, do banco e da GPU: **a decidir**. Os candidatos e critérios (custo da GPU, região no Brasil por LGPD, PostgreSQL gerenciado) estão em [`docs/06-operacao/`](../06-operacao/).

## 11. Riscos técnicos

| # | Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|---|
| RT1 | O portal escolhido muda o layout e quebra o reconhecimento de campos | Alta | Alto | Conector versionado; fixtures gravadas; métrica de taxa de campos reconhecidos com alerta; fallback para regras genéricas |
| RT2 | Termos de uso do portal proíbem extensões que preenchem formulários | Média | Alto | Análise jurídica no spike ([ADR-0011](adr/0011-primeiro-portal-e-conectores-por-portal.md)); só ação iniciada pela pessoa; nenhuma requisição automatizada |
| RT3 | Campos dentro de iframes ou shadow DOM (comum em ATS) | Média | Alto | Critério de escolha do portal; suporte a shadow DOM aberto e `all_frames` só no domínio autorizado, como evolução |
| RT4 | Variação do modelo de 7B gera classificações inconsistentes | Alta | Médio | Evidência obrigatória; temperatura baixa; avaliação com casos fixos; troca de modelo pela porta de IA |
| RT5 | Custo ou latência da GPU em produção | Média | Alto | Regras primeiro; uma chamada por leitura; fila; modelo de 7–8B quantizado; medir antes de decidir a hospedagem |
| RT6 | Prompt injection no texto da vaga ou nos rótulos da página | Média | Médio | IA só devolve `enum` e chaves validadas; texto da página marcado como dado; nenhuma ação derivada de texto livre da IA ([seguranca.md](seguranca.md)) |
| RT7 | Vazamento de dados de perfil | Baixa | Muito alto | Mínimo retorno, criptografia, tokens curtos, revisão de segurança, logs sem dados pessoais |
| RT8 | Revisão da Chrome Web Store atrasa a distribuição | Média | Médio | Permissões mínimas; política de privacidade publicada; testes com distribuição não listada |

## 12. Evolução para o MVP 2

| Capacidade do MVP 2 | Base criada no MVP 1 | Mudança esperada |
|---|---|---|
| Painel central de candidaturas | Tabela `Applications` com status e anotações (implementada além do que o ADR-0012 propunha), eventos de auditoria | Lembretes, próximas ações, filtros e busca (RF-M2-02) |
| Outros portais | Pacote `connectors` com interface comum | Um conector por portal, cada um com fixtures e testes |
| Marketplace PJ/CLT, matching, reputação | Perfil Mestre estruturado e versionado | Novos contextos (empresas, oportunidades, avaliações) em módulos ou serviços separados; os documentos arquivados em `docs/arquivo/marketplace-v1/` são o ponto de partida |
| Assinaturas do marketplace (RF-M2-09) | Planos do assistente da versão 2 (Gratuito, Plus, pacote), com pagamento **simulado** e cota de uso ([ADR-0020](adr/0020-monetizacao-com-anuncios-e-planos.md)) | Integração com provedor de pagamento real (gateway, nota fiscal, webhooks, estorno) e planos do marketplace. A cobrança real não faz parte do MVP 1 |

A API é um monólito modular. A separação em serviços só deve acontecer quando um módulo tiver carga ou ciclo de vida próprios (por exemplo, o serviço de IA, que já é separado).
