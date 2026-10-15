# Entrega 3 — Relatório do backend (.NET 8)

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida (MVP 1) |
| Documento | Relatório de implementação do backend (Entrega 3) |
| Data | 15/10/2026 |
| Decisão relacionada | ADR-0017 "Backend em C#/.NET 8 com Clean Architecture" (Proposta) |
| Resultado dos testes | **154 testes, 154 passando** (90 unidade, 52 integração, 12 aceitação/BDD) |

## 1. O que foi entregue

- Solução `Zyft.sln` em Clean Architecture: `Zyft.Domain` (sem dependências) ← `Zyft.Application` (CQRS com MediatR 12, FluentValidation, portas) ← `Zyft.Infrastructure` (EF Core 8, PostgreSQL/SQLite, JWT, BCrypt, AES-GCM, Ollama, PdfPig/OpenXml) ← `Zyft.Api` (controllers finos, middleware de exceções, JWT + policies, rate limiting, Swagger, Serilog, health checks, CORS, frontend estático).
- Porte da lógica do protótipo Node (`server/src`):
  - `mapper.js` → `Application/Forms/FieldMapper.cs` + `FieldRules.cs`: regras PT/EN, grupos repetidos (experiências, formação, cursos, certificações, idiomas) com **alinhamento de blocos pela organização/instituição**, índices explícitos (`exp[1][empresa]`, base 0 ou 1), adaptação de datas/número/maxLength, escolha de opção de select, divergência com o valor já no portal (RF-16), respostas aprovadas, e IA que **só aponta a chave** (o `value` que a IA devolver é ignorado; chave inexistente é descartada).
  - `jobs.js` → `Application/Jobs/JobAnalyzer.cs`: extração dos requisitos e avaliação com **linhas numeradas do perfil**; "atende/parcial" sem linha real vira "nao_encontrado" (ADR-0013). Sem IA, há heurística determinística (seções "Requisitos/Diferenciais", cobertura de termos). Sem nota nem porcentagem (RN-06).
  - `writer.js` → `Application/Writing/AnswerWriter.cs`: texto livre só com fatos; o servidor descarta "fatos usados" que não existem no perfil e corta no `maxLength`.
  - `importer.js` → `Application/Imports/ResumeParser.cs` + `Infrastructure/Files/ResumeTextExtractor.cs`: PDF (PdfPig) e DOCX (OpenXml) em memória → proposta (IA com JSON Schema; sem IA, heurística de contatos, links, competências e idiomas). Dados sensíveis não são extraídos.
  - `profile.js` → `Application/Profiles/ProfileFacts.cs`: perfil "achatado" em chaves estáveis (`personal.email`, `experiences[0].company`…) e linhas legíveis com tempo de experiência calculado.
  - `llm.js` → `Infrastructure/Ai/OllamaLlmClient.cs` (porta `ILlmClient`; `/api/chat` com `format` = JSON Schema, timeout por chamada) e `NullLlmClient` (sem IA, as regras continuam — RNF-12).
- Migration inicial PostgreSQL (`src/Zyft.Infrastructure/Persistence/Migrations/…_InitialCreate`) gerada com `dotnet-ef` 8.0.11 (ferramenta local em `.config/dotnet-tools.json`).
- `docker-compose.yml` (PostgreSQL 16 + API; Ollama opcional no perfil `ai`), `src/Zyft.Api/Dockerfile`, `.github/workflows/ci.yml`, `.gitignore`, `.editorconfig`, `.dockerignore`, `global.json`, `Directory.Build.props`, `Directory.Packages.props` (versões centralizadas).

## 2. Como executar

Pré-requisito: .NET 8 SDK (`export PATH=$HOME/.dotnet:$PATH DOTNET_ROOT=$HOME/.dotnet`). O `global.json` fixa a linha 8.0.4xx.

### 2.1 Desenvolvimento com SQLite (sem Docker)

```bash
cd ZIFY
dotnet build
dotnet run --project src/Zyft.Api          # http://localhost:5080
```

- Ambiente `Development` (`launchSettings.json`): SQLite em `src/Zyft.Api/zyft-dev.db`, criado com `EnsureCreated`.
- Usuário de demonstração criado automaticamente (só em Development): **demo@zyft.local / Zyft@2026**, com o perfil de exemplo do protótipo (`server/examples/perfil-exemplo.json`, dados fictícios) e uma resposta aprovada.
- Web em `http://localhost:5080/`, Swagger em `/swagger` (botão **Authorize**: cole só o `accessToken`), saúde em `/health`.
- IA: se o Ollama estiver em `http://localhost:11434` com `qwen2.5:7b`, é usado; senão o `/health` fica `Degraded` e tudo funciona só com regras. Para desligar: `Llm__Provider=None`.

**Por que `EnsureCreated` no SQLite?** As migrations geradas são do PostgreSQL (tipos `timestamp with time zone`, `uuid`, `numeric`) e não rodam no SQLite. Manter dois conjuntos de migrations dobraria a manutenção para um banco que só serve a desenvolvimento rápido e testes. Por isso: PostgreSQL → `Database.Migrate()` na subida; SQLite → `EnsureCreated()` (`Infrastructure/Persistence/DatabaseInitializer.cs`). O CI confere que a migration está em dia com o modelo (`dotnet ef migrations has-pending-model-changes`).

### 2.2 Docker (PostgreSQL 16)

```bash
cd ZIFY
docker compose up -d --build             # API em http://localhost:5080 (aplica a migration na subida)
docker compose --profile ai up -d --build   # + Ollama no container (depois: docker compose exec ollama ollama pull qwen2.5:7b)
docker compose down -v                    # para e apaga o volume
```

Variáveis (arquivo `.env`, fora do Git): `ZYFT_JWT_SECRET` (≥ 32 caracteres), `ZYFT_ENCRYPTION_KEY` (32 bytes em Base64), `ZYFT_DB_PASSWORD`, `ZYFT_LLM_PROVIDER` (`Ollama`|`None`), `ZYFT_LLM_URL` (padrão: Ollama do host via `host.docker.internal`). O container roda como usuário não-root, em ambiente `Staging` (em `Production` a API exige `Cors__ExtensionIds`, ver D-07).

Verificado em 15/10/2026: `docker compose up --build` subiu, a migration `20261015200622_InitialCreate` foi aplicada (18 tabelas), `/health` = `Healthy` e o fluxo curl da seção 2.4 passou contra o PostgreSQL.

### 2.3 Testes

```bash
dotnet test                      # 154 testes
dotnet test tests/Zyft.UnitTests # só unidade
```

Os testes de integração e aceitação sobem a API real (`WebApplicationFactory<Program>`) com SQLite em memória (uma base por fábrica) e IA desligada ou de mentira; não precisam de Docker nem de Ollama.

### 2.4 Fluxo verificado via curl

`register → login → PUT /profile → POST /profile/experiences → POST /forms/mappings → POST /forms/completions → GET /applications → GET /audit-events`, executado contra `dotnet run` (SQLite) e contra `docker compose` (PostgreSQL). Trecho real da resposta de `/forms/mappings` (8 campos):

| Campo da página | Resultado |
|---|---|
| Nome completo | `Ready`, `Rule`, `personal.fullName`, valor do perfil, marcado |
| E-mail | `Ready`, `Rule`, `personal.email` |
| Senha (`type=password`) | `Blocked`, sem valor: "Campo de senha: o Zyft nunca lê nem preenche senhas." |
| Modalidade (select) | `Ready`, opção `h` = "Híbrido" (opção existente na página) |
| Gênero (select) | `Blocked`, sem valor (sem consentimento de sensíveis) |
| Empresa (`exp[0][empresa]`, seção "Experiência profissional") | `Ready`, `experiences[0].company` |
| Currículo (`type=file`) | `Blocked`: "Anexe o arquivo manualmente." |
| "Declaro que as informações são verdadeiras" | `Blocked`, `declaration`, desmarcado |

Auditoria resultante: `conta.criada`, `perfil.atualizado`, `formulario.mapeado` (portal `vagas.exemplo.com`, 8 campos), `candidatura.confirmada` (3 campos) — sem valores e sem caminho/query da URL.

### 2.5 Verificação com a IA real (Ollama `qwen2.5:7b`, via Docker)

| Chamada | Tempo | Observação |
|---|---|---|
| `POST /forms/mappings` com "Nome da última organização em que atuou" | 43 s (1ª chamada, modelo carregando) | A regra não reconheceu; a IA apontou `experiences[0].company` e o valor veio do perfil (`source: Ai`, `confidence: medium`). "Município onde reside" foi resolvido por regra. |
| `POST /jobs/analyze` (4 requisitos) | 9,9 s | "Experiência com sistema ERP" → `atende` com a linha real do perfil; "Inglês fluente" → `nao_encontrado`; "CNH B" → `atende` (linha "CNH (categoria): B"); `toConfirm` com `hasValue`. Sem porcentagem. |
| `POST /forms/open-answers` (maxLength 300) | 2,9 s | Texto em 1ª pessoa com 2 fatos usados (ambos existentes no perfil), `selected: false`. |
| `POST /resume-imports` (DOCX) | 8,5 s | Proposta com nome, contatos, 2 experiências (datas `AAAA-MM`, atual sem término), formação `InProgress`, curso 40 h, idioma `Basic`. A linha "Estado civil: solteira" do currículo **não** foi extraída. |

### 2.6 Teste de fumaça da web (Playwright + Chromium headless)

Contra a API em Docker: cadastro em `#/cadastrar` → tela de Boas-vindas (consentimentos começam desligados; o script liga IA e Análise de vaga) → `#/perfil` "Salvar dados básicos" → registro de conclusão (chamada que a extensão faria) → `#/candidaturas` lista a candidatura "Assistente Administrativo / Empresa Z / vagas.exemplo.com / 7 campos". Nenhum erro de console e nenhuma resposta 4xx/5xx da API durante o fluxo. Conferido por API: perfil salvo, `AiText` e `JobAnalysis` ligados, `SensitiveData` desligado.

## 3. Estrutura de pastas

```
ZIFY/
  Zyft.sln  global.json  Directory.Build.props  Directory.Packages.props  docker-compose.yml
  .config/dotnet-tools.json          dotnet-ef 8.0.11 (ferramenta local)
  .github/workflows/ci.yml           build + testes + migrations em dia + imagem Docker e /health
  src/
    Zyft.Domain/                     entidades e regras puras (sem pacotes)
      Common/ (Entity, DomainException)  Users/ (User, RefreshToken)  Consents/  Profiles/ (Profile = raiz de agregado,
      Experience, Education, Course, Skill, Language, enums)  Answers/  Sensitive/  Imports/  Extension/ (ExtensionDevice,
      PairingCode, AuthorizedSite)  Applications/ (JobApplication)  Audit/ (AuditEvent, AuditActions)
    Zyft.Application/                casos de uso (CQRS) e serviços de aplicação
      Common/ (Behaviors: Validation, Logging; Exceptions; Interfaces = portas; TextTools; DisplayNames; Dates; ConsentGuard; AuthTokens; LlmPrompts)
      Auth/ Account/ Consents/ Profiles/ (ProfileFacts, DTOs, CRUD) Imports/ (ResumeParser, proposta) Answers/ Sensitive/
      Extension/ Sites/ Forms/ (FieldMapper, FieldRules, mappings/open-answers/completions) Jobs/ (JobAnalyzer) Writing/ (AnswerWriter)
      Applications/ Audit/  DependencyInjection.cs
    Zyft.Infrastructure/
      Persistence/ (ZyftDbContext = IUnitOfWork, Configurations Fluent API, Repositories, DatabaseInitializer + seed, DesignTimeDbContextFactory, Migrations/)
      Security/ (JwtTokenService, BCryptPasswordHasher, AesGcmSensitiveDataProtector, SystemClock)
      Ai/ (OllamaLlmClient, NullLlmClient)  Files/ (ResumeTextExtractor)  DependencyInjection.cs
    Zyft.Api/
      Controllers/ (13 controllers finos)  Infrastructure/ (ExceptionHandlingMiddleware, Problems, HttpCurrentUser, LlmHealthCheck, LenientDateOnlyConverter)
      Program.cs  appsettings*.json  Properties/launchSettings.json (porta 5080)  Dockerfile
  tests/
    Zyft.UnitTests/          xUnit + FluentAssertions + Moq (domínio, FieldMapper, JobAnalyzer, AnswerWriter, handlers, validators, segurança)
    Zyft.IntegrationTests/   WebApplicationFactory + SQLite in-memory (HTTP, auth, autorização, erros, regras de confiança, logs, CORS)
    Zyft.AcceptanceTests/    Reqnroll (Gherkin pt) — Features/RegrasInegociaveis.feature
  web/  extension/           (frontend e extensão — outro agente; o backend serve web/ em "/")
```

Dependências apontam para dentro: `Api → Application/Infrastructure`, `Infrastructure → Application → Domain`. O domínio não referencia nenhum pacote; a aplicação não referencia EF Core (usa `IRepository<T>`/`IProfileRepository`/`IUnitOfWork`).

## 4. Decisões técnicas relevantes

- **Erros** (`ExceptionHandlingMiddleware` + `Problems`): `application/problem+json` em pt-BR com `type`, `title`, `status`, `detail`, `instance`, `traceId` e `code`. 400 = formato/validação (FluentValidation ou JSON inválido, com `errors` por campo em camelCase, ex.: `experiences[0].startDate`); 401 = token/credenciais (inclusive o *challenge* do JwtBearer); 403 = policy ou consentimento (`code: consent_required`); 404 = inexistente **ou de outra pessoa** (não revela existência); 409 = conflito (e `DbUpdateException`); 413; 422 = regra de negócio do domínio (`code` estável, ex.: `experience.invalid_period`); 429 com `Retry-After`; 500 sem stack trace. Rotas `/api` inexistentes também respondem ProblemDetails.
- **Autenticação**: BCrypt (fator 11); JWT HS256 de 15 min com `sub`, `email`, `scope` (`web`|`extension`) e `device_id`; refresh token opaco (48 bytes aleatórios) de 7 dias, só o SHA-256 no banco, **rotação** a cada uso e, se um token já trocado for reapresentado, **revogação de toda a cadeia** (`ReplacedByHash`). Login gasta o mesmo tempo com e-mail inexistente (hash descartável). Token de extensão revogada é recusado na hora (`OnTokenValidated` confere o dispositivo).
- **Policies**: `WebUser` (scope `web`) e `ExtensionOrWeb`. Só web: conta (`DELETE /me`, `/me/export`), consentimentos, perfil completo, importação, dados sensíveis, dispositivos, edição/exclusão de candidaturas e respostas, auditoria. Web ou extensão: `GET /me`, `/forms/*`, `/jobs/analyze`, `GET /applications`, `GET`/`POST /authorized-sites`, `POST /answers`.
- **Rate limiting**: por IP em `register`, `login` e `extension/pair` (`RateLimiting:AuthPermitLimit`, padrão 10/min); **por pessoa** (`sub`) em `/forms/*`, `/jobs/analyze` e `POST /resume-imports` (`RateLimiting:AiPermitPerMinute`, padrão 60/min).
- **Consentimentos**: as três finalidades nascem **desligadas** (opt-in); a tela de Boas-vindas liga (a) e (b). Desligar "dados sensíveis" apaga os dados sensíveis guardados. O bloqueio é na API (403), não só na interface.
- **Dados sensíveis**: tabela separada, valor cifrado com AES-256-GCM (nonce aleatório por valor, chave por configuração), nunca enviados à IA (o `FieldMapper` classifica campos sensíveis antes da IA, e o perfil achatado não os contém). Com consentimento, o de-para devolve o valor **sempre com `selected=false`** e estado `Blocked` (marcação manual).
- **Logs (Serilog)**: só método, caminho (sem query string), status e duração; nome do caso de uso e tempo; nunca corpo, e-mail, token, prompt ou resposta da IA. Há teste que procura e-mail, senha, nome, telefone e token nos logs capturados.
- **Banco**: modelo relacional (17 tabelas), PK `Guid` gerada no cliente (`ValueGeneratedNever`), `CreatedAt/UpdatedAt` UTC preenchidos no `SaveChangesAsync`, enums gravados como texto, FKs com `ON DELETE CASCADE` a partir de `Users` (RF-08), índices únicos (`Users.Email`, `RefreshTokens.TokenHash`, `Consents(UserId,Purpose)`, `Profiles.UserId`, `Skills(ProfileId,Name)`, `Languages(ProfileId,Name)`, `SensitiveData(UserId,Kind)`, `AuthorizedSites(UserId,Origin)`), *check constraints* de período em `Experiences` e `Educations`.
- **JSON**: camelCase, enums como **nome** (`"AiText"`, `"Ready"`, `"Undergraduate"`), nulos omitidos, datas `AAAA-MM-DD` na saída e aceitação de `AAAA-MM`, `MM/AAAA` e `DD/MM/AAAA` na entrada (inputs `type=month` da web).
- **Frontend**: `web/` é servido em `/` a partir de `../../web` (dev) ou `wwwroot` (container, copiado no Dockerfile). CORS: origens de `Cors:AllowedOrigins` e `chrome-extension://*` (restrito a `Cors:ExtensionIds` quando configurado; obrigatório em Production).


## 5. Endpoints implementados

Lista gerada do Swagger (`/swagger/v1/swagger.json`) em 15/10/2026: **61 operações**. Todas sob `/api/v1`, exceto `/health`. A política de acesso de cada grupo está na seção 4.

| Método | Rota | Resumo (XML comment) |
|---|---|---|
| `GET` | `/api/v1/answers` | Lista as respostas aprovadas. |
| `POST` | `/api/v1/answers` | Aprova uma resposta (a extensão também pode aprovar o texto revisado no painel). Mesma pergunta → substitui. |
| `GET` | `/api/v1/answers/{id}` | Detalhe de uma resposta. |
| `PUT` | `/api/v1/answers/{id}` | Edita pergunta e texto. |
| `DELETE` | `/api/v1/answers/{id}` | Exclui a resposta. |
| `GET` | `/api/v1/applications` | Lista as candidaturas (mais recentes primeiro). |
| `GET` | `/api/v1/applications/{id}` | Detalhe. |
| `PATCH` | `/api/v1/applications/{id}` | Atualiza status e/ou notas. |
| `DELETE` | `/api/v1/applications/{id}` | Exclui o registro. |
| `GET` | `/api/v1/audit-events` | Últimos eventos (padrão 200, máx. 1000). |
| `POST` | `/api/v1/auth/register` | Cria a conta (senha: mín. 8 caracteres com letra e número) e já devolve os tokens. |
| `POST` | `/api/v1/auth/login` | Login com e-mail e senha. Limitado por IP (429 ao exceder). |
| `POST` | `/api/v1/auth/refresh` | Troca o refresh token por um novo par (rotação). Reusar um token já trocado revoga a cadeia inteira. |
| `POST` | `/api/v1/auth/logout` | Revoga o refresh token informado (idempotente). |
| `GET` | `/api/v1/authorized-sites` | Lista as origens autorizadas (a extensão também consulta). |
| `POST` | `/api/v1/authorized-sites` | Autoriza uma origem (aceita URL completa; guarda só esquema://host[:porta]). A extensão também pode chamar |
| `DELETE` | `/api/v1/authorized-sites/{id}` | Remove a autorização. |
| `GET` | `/api/v1/consents` | Lista as finalidades e se estão concedidas. |
| `PUT` | `/api/v1/consents/{purpose}` | Liga/desliga uma finalidade. Desligar SensitiveData apaga os dados sensíveis guardados. |
| `POST` | `/api/v1/extension/pairing-codes` | Gera um código de 6 dígitos (válido por 10 min, uso único). Só a plataforma web logada. |
| `POST` | `/api/v1/extension/pair` | A extensão troca o código por tokens com `scope=extension`. Anônimo e limitado por IP. |
| `GET` | `/api/v1/extension/devices` | Lista extensões pareadas. |
| `DELETE` | `/api/v1/extension/devices/{id}` | Revoga uma extensão (tokens dela deixam de valer imediatamente). |
| `POST` | `/api/v1/forms/mappings` | De-para campo → Perfil Mestre: regras, IA que só aponta chave, validação (RF-12). Devolve um item por campo enviado. |
| `POST` | `/api/v1/forms/open-answers` | Sugestão de texto para pergunta aberta, só com fatos do perfil (RF-15). Exige consentimento AiText (403). |
| `POST` | `/api/v1/forms/completions` | Registra que a pessoa concluiu a candidatura no portal POR CONTA PRÓPRIA (RF-19). Cria a candidatura e o evento de auditoria. |
| `POST` | `/api/v1/jobs/analyze` | Requisitos com status atende/parcial/nao_encontrado/confirmar e evidência = linha real numerada do perfil |
| `GET` | `/api/v1/me` | Dados da conta autenticada (web ou extensão). |
| `DELETE` | `/api/v1/me` | Exclui a conta e tudo o que depende dela; encerra o pareamento das extensões. |
| `GET` | `/api/v1/me/export` | Exporta todos os dados em JSON legível. Só pela plataforma web. |
| `GET` | `/api/v1/profile` | Perfil completo com experiências, formação, cursos, competências e idiomas. |
| `PUT` | `/api/v1/profile` | Atualiza os dados básicos (substitui todos os campos básicos). |
| `GET` | `/api/v1/profile/missing` | Perguntas curtas só sobre o que falta (RF-05). |
| `GET` | `/api/v1/profile/experiences` | Lista as experiências. |
| `POST` | `/api/v1/profile/experiences` | Adiciona experiência. Datas aceitam AAAA-MM ou AAAA-MM-DD. Término < início → 422. |
| `PUT` | `/api/v1/profile/experiences/{id}` | Edita experiência. |
| `DELETE` | `/api/v1/profile/experiences/{id}` | Remove experiência. |
| `GET` | `/api/v1/profile/educations` | Lista a formação. |
| `POST` | `/api/v1/profile/educations` | Adiciona formação (level/status como nome do enum, ex.: Undergraduate / InProgress). |
| `PUT` | `/api/v1/profile/educations/{id}` | Edita formação. |
| `DELETE` | `/api/v1/profile/educations/{id}` | Remove formação. |
| `GET` | `/api/v1/profile/courses` | Lista cursos e certificações. |
| `POST` | `/api/v1/profile/courses` | Adiciona curso (type = Course \| Certification). |
| `PUT` | `/api/v1/profile/courses/{id}` | Edita curso. |
| `DELETE` | `/api/v1/profile/courses/{id}` | Remove curso. |
| `GET` | `/api/v1/profile/skills` | Lista competências. |
| `POST` | `/api/v1/profile/skills` | Adiciona competência (nome único no perfil; duplicada → 422). |
| `PUT` | `/api/v1/profile/skills/{id}` | Edita competência. |
| `DELETE` | `/api/v1/profile/skills/{id}` | Remove competência. |
| `GET` | `/api/v1/profile/languages` | Lista idiomas. |
| `POST` | `/api/v1/profile/languages` | Adiciona idioma (proficiency = Basic…Native). |
| `PUT` | `/api/v1/profile/languages/{id}` | Edita idioma. |
| `DELETE` | `/api/v1/profile/languages/{id}` | Remove idioma. |
| `POST` | `/api/v1/resume-imports` | Envia um currículo PDF (com texto) ou DOCX, até 10 MB, e recebe a proposta para revisão. O arquivo não é guardado. |
| `GET` | `/api/v1/resume-imports/{id}` | Consulta a importação e a proposta (enquanto pendente). |
| `POST` | `/api/v1/resume-imports/{id}/apply` | Aplica só os itens aceitos/editados: corpo no mesmo formato da proposta (`profile`, `experiences`, |
| `POST` | `/api/v1/resume-imports/{id}/discard` | Descarta a proposta; nada muda no perfil. |
| `GET` | `/api/v1/sensitive-data` | Lista os dados sensíveis (decifrados só para a própria pessoa). |
| `POST` | `/api/v1/sensitive-data` | Cadastra um dado (kind: Gender, Ethnicity, Disability, Religion, SexualOrientation, Health, SocialName, Other). |
| `PUT` | `/api/v1/sensitive-data/{id}` | Altera o valor. |
| `DELETE` | `/api/v1/sensitive-data/{id}` | Exclui o dado. |
| `GET` | `/health` | Saúde: banco (`database`) e IA (`llm`, `Degraded` se o Ollama estiver fora). Fora do Swagger. |

## 6. Casos de teste executados

Execução de 15/10/2026 (`dotnet test`, Debug; também compilado em Release sem avisos): **154 testes, 154 passaram, 0 falharam, 0 ignorados**. Teorias (`[Theory]`) e esquemas de cenário aparecem uma linha por exemplo.

| Projeto | Testes |
|---|---|
| UnitTests | 90 |
| IntegrationTests | 52 |
| AcceptanceTests (Reqnroll, Gherkin pt) | 12 |

Regras inegociáveis cobertas (onde): senha/CAPTCHA/arquivo/declaração nunca recebem valor (unidade, integração e BDD); IA só aponta chave e valor da IA é ignorado (unidade, integração com IA maliciosa, BDD); chave inventada pela IA é descartada (unidade); sensível bloqueado sem consentimento e desmarcado com consentimento (todas); prompt da IA sem dado sensível (unidade); análise de vaga sem consentimento → 403 (unidade com Moq, integração, BDD); evidência inventada vira `nao_encontrado` (unidade, integração, BDD); sem porcentagem/nota (unidade, integração, BDD); refresh reutilizado revoga a cadeia (unidade, integração); token de extensão não acessa `/me/export` (integração, BDD); logs sem dados pessoais (integração); mapeamento devolve só os valores dos campos pedidos (integração).

| Projeto | Classe/Feature | Caso de teste | Resultado |
|---|---|---|---|
| AcceptanceTests | RegrasInegociaveisDoZyftFeature | A IA só aponta a chave; o valor escrito por ela é ignorado (ADR-0004) | Passou |
| AcceptanceTests | RegrasInegociaveisDoZyftFeature | Análise de vaga não aceita evidência inventada (ADR-0013, RN-06) | Passou |
| AcceptanceTests | RegrasInegociaveisDoZyftFeature | Análise de vaga sem consentimento é recusada pela API (RF-07) | Passou |
| AcceptanceTests | RegrasInegociaveisDoZyftFeature | Campos que exigem ação humana nunca recebem valor (RF-18)(tipo: "checkbox", rotulo: "Declaro que as informações são verdadeiras", exampleTags: []) | Passou |
| AcceptanceTests | RegrasInegociaveisDoZyftFeature | Campos que exigem ação humana nunca recebem valor (RF-18)(tipo: "file", rotulo: "Anexe seu currículo", exampleTags: []) | Passou |
| AcceptanceTests | RegrasInegociaveisDoZyftFeature | Campos que exigem ação humana nunca recebem valor (RF-18)(tipo: "password", rotulo: "Senha", exampleTags: []) | Passou |
| AcceptanceTests | RegrasInegociaveisDoZyftFeature | Campos que exigem ação humana nunca recebem valor (RF-18)(tipo: "text", rotulo: "Confirme sua senha", exampleTags: []) | Passou |
| AcceptanceTests | RegrasInegociaveisDoZyftFeature | Campos que exigem ação humana nunca recebem valor (RF-18)(tipo: "text", rotulo: "Digite o código do captcha", exampleTags: []) | Passou |
| AcceptanceTests | RegrasInegociaveisDoZyftFeature | Dado sensível bloqueado sem consentimento (RN-04) | Passou |
| AcceptanceTests | RegrasInegociaveisDoZyftFeature | Dado sensível com consentimento vem desmarcado (RN-04) | Passou |
| AcceptanceTests | RegrasInegociaveisDoZyftFeature | Token da extensão não exporta os dados da conta (RF-08) | Passou |
| AcceptanceTests | RegrasInegociaveisDoZyftFeature | Valor objetivo vem do perfil por regra determinística (RN-02) | Passou |
| IntegrationTests | AiGuardTests | IA_que_inventa_linha_de_evidencia_tem_o_atende_rebaixado | Passou |
| IntegrationTests | AiGuardTests | IA_que_tenta_escrever_valor_ou_apontar_senha_nao_consegue | Passou |
| IntegrationTests | AuthTests | Cadastro_devolve_201_com_tokens_e_scope_web | Passou |
| IntegrationTests | AuthTests | Cadastro_duplicado_e_409_ProblemDetails_pt_BR | Passou |
| IntegrationTests | AuthTests | Cadastro_invalido_e_400_com_errors_por_campo | Passou |
| IntegrationTests | AuthTests | JSON_malformado_e_400_pt_BR | Passou |
| IntegrationTests | AuthTests | Login_errado_e_401_e_rota_protegida_sem_token_e_401 | Passou |
| IntegrationTests | AuthTests | Logout_revoga_o_refresh_token | Passou |
| IntegrationTests | AuthTests | Refresh_rotaciona_e_reuso_revoga_a_cadeia | Passou |
| IntegrationTests | AuthTests | Token_adulterado_e_401 | Passou |
| IntegrationTests | ExtensionAndAccountTests | Codigo_de_pareamento_e_de_uso_unico | Passou |
| IntegrationTests | ExtensionAndAccountTests | Conclusao_cria_candidatura_e_auditoria_sem_valores | Passou |
| IntegrationTests | ExtensionAndAccountTests | Exportacao_traz_os_dados_e_exclusao_apaga_tudo | Passou |
| IntegrationTests | ExtensionAndAccountTests | Extensao_pode_autorizar_site_mas_nao_remover | Passou |
| IntegrationTests | ExtensionAndAccountTests | Refresh_de_extensao_mantem_scope_extension | Passou |
| IntegrationTests | ExtensionAndAccountTests | Revogar_extensao_derruba_access_e_refresh_na_hora | Passou |
| IntegrationTests | ExtensionAndAccountTests | Token_de_extensao_nao_acessa_exportacao_nem_perfil_inteiro | Passou |
| IntegrationTests | PlatformTests | CORS_aceita_extensao_e_origem_configurada(origin: "chrome-extension://abcdefghijklmnop", allowed: True) | Passou |
| IntegrationTests | PlatformTests | CORS_aceita_extensao_e_origem_configurada(origin: "http://localhost:5080", allowed: True) | Passou |
| IntegrationTests | PlatformTests | CORS_aceita_extensao_e_origem_configurada(origin: "https://site-malicioso.com", allowed: False) | Passou |
| IntegrationTests | PlatformTests | Erro_500_nao_expoe_stack_trace | Passou |
| IntegrationTests | PlatformTests | Frontend_web_e_servido_na_raiz | Passou |
| IntegrationTests | PlatformTests | Health_responde_com_banco_e_IA | Passou |
| IntegrationTests | PlatformTests | Logs_nao_contem_dados_pessoais | Passou |
| IntegrationTests | PlatformTests | Swagger_publica_rotas_e_esquema_Bearer | Passou |
| IntegrationTests | ProfileAndErrorsTests | CRUD_de_experiencia_aceita_mes_AAAA_MM | Passou |
| IntegrationTests | ProfileAndErrorsTests | Campo_obrigatorio_ausente_e_400_com_mensagem_pt_BR | Passou |
| IntegrationTests | ProfileAndErrorsTests | Data_em_formato_invalido_e_400 | Passou |
| IntegrationTests | ProfileAndErrorsTests | Missing_lista_so_o_que_falta | Passou |
| IntegrationTests | ProfileAndErrorsTests | Perfil_e_criado_no_cadastro_e_pode_ser_editado | Passou |
| IntegrationTests | ProfileAndErrorsTests | Recurso_de_outra_pessoa_e_404 | Passou |
| IntegrationTests | ProfileAndErrorsTests | Regra_de_negocio_violada_e_422_com_code | Passou |
| IntegrationTests | ProfileAndErrorsTests | Respostas_aprovadas_CRUD | Passou |
| IntegrationTests | ProfileAndErrorsTests | Rota_inexistente_da_API_e_404_ProblemDetails | Passou |
| IntegrationTests | ProfileAndErrorsTests | Sites_autorizados_normalizam_origem_e_recusam_duplicata | Passou |
| IntegrationTests | RateLimitTests | Login_acima_do_limite_e_429_com_Retry_After | Passou |
| IntegrationTests | RateLimitTests | Rotas_de_IA_tem_limite_por_pessoa_e_nao_por_IP | Passou |
| IntegrationTests | ResumeImportTests | Arquivo_acima_de_10MB_e_recusado | Passou |
| IntegrationTests | ResumeImportTests | Arquivo_com_extensao_falsa_ou_formato_errado_e_400 | Passou |
| IntegrationTests | ResumeImportTests | DOCX_vira_proposta_e_apply_aplica_so_o_aceito | Passou |
| IntegrationTests | ResumeImportTests | Descartar_nao_muda_o_perfil | Passou |
| IntegrationTests | ResumeImportTests | PDF_com_texto_vira_proposta | Passou |
| IntegrationTests | TrustRulesTests | Analise_de_vaga_com_consentimento_tem_evidencia_real_e_nenhuma_porcentagem | Passou |
| IntegrationTests | TrustRulesTests | Analise_de_vaga_sem_consentimento_e_403 | Passou |
| IntegrationTests | TrustRulesTests | Consentimento_desconhecido_e_404 | Passou |
| IntegrationTests | TrustRulesTests | Conta_nova_nasce_com_todos_os_consentimentos_desligados | Passou |
| IntegrationTests | TrustRulesTests | Mappings_devolvem_so_valores_dos_campos_pedidos | Passou |
| IntegrationTests | TrustRulesTests | Mappings_nunca_devolvem_valor_para_senha_captcha_arquivo_ou_declaracao | Passou |
| IntegrationTests | TrustRulesTests | MaxLength_negativo_ou_fora_da_faixa_e_400 | Passou |
| IntegrationTests | TrustRulesTests | Sensivel_bloqueado_sem_consentimento_e_desmarcado_com_consentimento | Passou |
| IntegrationTests | TrustRulesTests | Texto_livre_com_IA_fora_devolve_unknown_sem_valor | Passou |
| IntegrationTests | TrustRulesTests | Texto_livre_sem_consentimento_e_403 | Passou |
| UnitTests | AnswerWriterTests | Fatos_que_nao_existem_no_perfil_sao_descartados_e_texto_respeita_limite | Passou |
| UnitTests | AnswerWriterTests | Prompt_contem_so_fatos_do_perfil | Passou |
| UnitTests | AnswerWriterTests | Sem_IA_lanca_indisponivel | Passou |
| UnitTests | FieldMapperTests | Blocos_repetidos_sao_alinhados_pela_organizacao | Passou |
| UnitTests | FieldMapperTests | Campo_numerico_recebe_so_numero | Passou |
| UnitTests | FieldMapperTests | Campos_bloqueados_sao_classificados_antes_da_IA_e_nem_chegam_ao_prompt | Passou |
| UnitTests | FieldMapperTests | Chave_inventada_pela_IA_e_descartada | Passou |
| UnitTests | FieldMapperTests | Datas_sao_adaptadas_ao_tipo_do_input(type: "date", label: "Data de nascimento", expected: "1998-03-14") | Passou |
| UnitTests | FieldMapperTests | Datas_sao_adaptadas_ao_tipo_do_input(type: "month", label: "Data de início", expected: "2023-07") | Passou |
| UnitTests | FieldMapperTests | Declaracao_e_termos_nunca_marcados | Passou |
| UnitTests | FieldMapperTests | Devolve_exatamente_um_item_por_campo_na_ordem_da_pagina | Passou |
| UnitTests | FieldMapperTests | IA_fora_do_ar_nao_derruba_o_mapeamento | Passou |
| UnitTests | FieldMapperTests | IA_so_aponta_chave_e_o_valor_vem_do_perfil | Passou |
| UnitTests | FieldMapperTests | Indices_explicitos_no_name_base_1_sao_normalizados | Passou |
| UnitTests | FieldMapperTests | Pergunta_aberta_reaproveita_resposta_aprovada_sem_marcar | Passou |
| UnitTests | FieldMapperTests | PickOption_casa_por_texto_ou_sim_nao(option: "Não possuo", value: "Não", shouldMatch: True) | Passou |
| UnitTests | FieldMapperTests | PickOption_casa_por_texto_ou_sim_nao(option: "Sim", value: "Sim", shouldMatch: True) | Passou |
| UnitTests | FieldMapperTests | PickOption_casa_por_texto_ou_sim_nao(option: "Superior completo", value: "Graduação", shouldMatch: False) | Passou |
| UnitTests | FieldMapperTests | Pretensao_salarial_em_texto_vem_formatada_em_reais | Passou |
| UnitTests | FieldMapperTests | Primeiro_nome_e_sobrenome_separados_quando_a_pagina_pede | Passou |
| UnitTests | FieldMapperTests | Prompt_da_IA_nao_contem_dados_sensiveis | Passou |
| UnitTests | FieldMapperTests | Regras_PT_EN_mapeiam_campos_objetivos(label: "CEP", key: "personal.zipCode", value: "87000-000") | Passou |
| UnitTests | FieldMapperTests | Regras_PT_EN_mapeiam_campos_objetivos(label: "Cidade", key: "personal.city", value: "Maringá") | Passou |
| UnitTests | FieldMapperTests | Regras_PT_EN_mapeiam_campos_objetivos(label: "Disponibilidade para viagens", key: "preferences.travel", value: "Sim") | Passou |
| UnitTests | FieldMapperTests | Regras_PT_EN_mapeiam_campos_objetivos(label: "E-mail", key: "personal.email", value: "ana.souza@email.com") | Passou |
| UnitTests | FieldMapperTests | Regras_PT_EN_mapeiam_campos_objetivos(label: "Email address", key: "personal.email", value: "ana.souza@email.com") | Passou |
| UnitTests | FieldMapperTests | Regras_PT_EN_mapeiam_campos_objetivos(label: "Full name", key: "personal.fullName", value: "Ana Paula Souza") | Passou |
| UnitTests | FieldMapperTests | Regras_PT_EN_mapeiam_campos_objetivos(label: "Nome completo", key: "personal.fullName", value: "Ana Paula Souza") | Passou |
| UnitTests | FieldMapperTests | Regras_PT_EN_mapeiam_campos_objetivos(label: "Phone", key: "personal.phone", value: "(44) 99999-2222") | Passou |
| UnitTests | FieldMapperTests | Regras_PT_EN_mapeiam_campos_objetivos(label: "Telefone celular", key: "personal.phone", value: "(44) 99999-2222") | Passou |
| UnitTests | FieldMapperTests | Respeita_maxLength_do_campo | Passou |
| UnitTests | FieldMapperTests | Select_escolhe_opcao_existente_na_pagina | Passou |
| UnitTests | FieldMapperTests | Select_sem_opcao_correspondente_fica_ambiguo_e_desmarcado | Passou |
| UnitTests | FieldMapperTests | Senha_captcha_e_arquivo_nunca_recebem_valor(type: "file", label: "Anexe seu currículo") | Passou |
| UnitTests | FieldMapperTests | Senha_captcha_e_arquivo_nunca_recebem_valor(type: "hidden", label: "token") | Passou |
| UnitTests | FieldMapperTests | Senha_captcha_e_arquivo_nunca_recebem_valor(type: "password", label: "Senha") | Passou |
| UnitTests | FieldMapperTests | Senha_captcha_e_arquivo_nunca_recebem_valor(type: "text", label: "Confirme sua senha") | Passou |
| UnitTests | FieldMapperTests | Senha_captcha_e_arquivo_nunca_recebem_valor(type: "text", label: "Digite o código de verificação (captcha)") | Passou |
| UnitTests | FieldMapperTests | Sensivel_com_consentimento_traz_valor_mas_nunca_marcado | Passou |
| UnitTests | FieldMapperTests | Sensivel_sem_consentimento_fica_bloqueado_sem_valor | Passou |
| UnitTests | FieldMapperTests | Valor_diferente_ja_no_portal_fica_divergente_e_mantem_o_do_portal | Passou |
| UnitTests | HandlerTests | Analise_de_vaga_sem_consentimento_e_bloqueada_na_API_e_nao_chama_a_IA | Passou |
| UnitTests | HandlerTests | Cadastro_cria_perfil_e_consentimentos_com_sensiveis_desligado | Passou |
| UnitTests | HandlerTests | Desligar_consentimento_de_sensiveis_apaga_os_dados_sensiveis | Passou |
| UnitTests | HandlerTests | Login_com_senha_errada_e_401_sem_revelar_se_o_email_existe | Passou |
| UnitTests | HandlerTests | Mapeamento_recusa_mais_de_300_campos | Passou |
| UnitTests | HandlerTests | Refresh_token_reutilizado_revoga_a_cadeia_inteira | Passou |
| UnitTests | HandlerTests | Senha_exige_8_caracteres_com_letra_e_numero(password: "12345678", valid: False) | Passou |
| UnitTests | HandlerTests | Senha_exige_8_caracteres_com_letra_e_numero(password: "Senha123", valid: True) | Passou |
| UnitTests | HandlerTests | Senha_exige_8_caracteres_com_letra_e_numero(password: "curta1", valid: False) | Passou |
| UnitTests | HandlerTests | Senha_exige_8_caracteres_com_letra_e_numero(password: "semnumeros", valid: False) | Passou |
| UnitTests | InfrastructureTests | AES_GCM_cifra_com_nonce_aleatorio_e_decifra | Passou |
| UnitTests | InfrastructureTests | BCrypt_verifica_e_nao_quebra_com_hash_invalido | Passou |
| UnitTests | InfrastructureTests | Chave_AES_com_tamanho_errado_falha_cedo | Passou |
| UnitTests | InfrastructureTests | Datas_de_curriculo_sao_interpretadas(text: "07/2023", year: 2023, month: 7) | Passou |
| UnitTests | InfrastructureTests | Datas_de_curriculo_sao_interpretadas(text: "15/07/2023", year: 2023, month: 7) | Passou |
| UnitTests | InfrastructureTests | Datas_de_curriculo_sao_interpretadas(text: "2023", year: 2023, month: 1) | Passou |
| UnitTests | InfrastructureTests | Datas_de_curriculo_sao_interpretadas(text: "2023-07", year: 2023, month: 7) | Passou |
| UnitTests | InfrastructureTests | Datas_de_curriculo_sao_interpretadas(text: "2023-07-15", year: 2023, month: 7) | Passou |
| UnitTests | InfrastructureTests | Importacao_sem_IA_extrai_contatos_competencias_e_idiomas_sem_sensiveis | Passou |
| UnitTests | InfrastructureTests | JWT_tem_sub_scope_device_e_expira_em_15_minutos | Passou |
| UnitTests | InfrastructureTests | Rotulos_pt_BR_viram_enum(text: "Concluído", expected: Completed) | Passou |
| UnitTests | InfrastructureTests | Rotulos_pt_BR_viram_enum(text: "InProgress", expected: InProgress) | Passou |
| UnitTests | InfrastructureTests | Rotulos_pt_BR_viram_enum(text: "Superior incompleto", expected: Incomplete) | Passou |
| UnitTests | InfrastructureTests | Rotulos_pt_BR_viram_enum(text: "cursando", expected: InProgress) | Passou |
| UnitTests | JobAnalyzerTests | Atende_sem_linha_real_vira_nao_encontrado(line: -3) | Passou |
| UnitTests | JobAnalyzerTests | Atende_sem_linha_real_vira_nao_encontrado(line: 0) | Passou |
| UnitTests | JobAnalyzerTests | Atende_sem_linha_real_vira_nao_encontrado(line: 999) | Passou |
| UnitTests | JobAnalyzerTests | Evidencia_e_a_linha_real_do_perfil_apontada_pela_IA | Passou |
| UnitTests | JobAnalyzerTests | Linhas_do_perfil_incluem_tempo_total_calculado | Passou |
| UnitTests | JobAnalyzerTests | Resultado_nao_tem_nota_nem_porcentagem | Passou |
| UnitTests | JobAnalyzerTests | Sem_IA_usa_heuristica_e_nunca_inventa_evidencia | Passou |
| UnitTests | ProfileTests | Categoria_de_CNH_sem_possuir_CNH_viola_regra | Passou |
| UnitTests | ProfileTests | Competencia_duplicada_ignorando_maiusculas_viola_regra | Passou |
| UnitTests | ProfileTests | Curso_com_carga_horaria_zero_viola_regra | Passou |
| UnitTests | ProfileTests | Experiencia_atual_nao_pode_ter_termino | Passou |
| UnitTests | ProfileTests | Experiencia_com_termino_antes_do_inicio_viola_regra | Passou |
| UnitTests | ProfileTests | Formacao_com_conclusao_antes_do_inicio_viola_regra | Passou |
| UnitTests | ProfileTests | Pretensao_salarial_negativa_viola_regra | Passou |
| UnitTests | ProfileTests | Remover_item_inexistente_devolve_falso | Passou |
| UnitTests | ProfileTests | Termino_igual_ao_inicio_e_valido | Passou |
| UnitTests | TokenAndCodeTests | Codigo_de_pareamento_expira_em_10_minutos_e_e_de_uso_unico | Passou |
| UnitTests | TokenAndCodeTests | Email_do_usuario_e_normalizado | Passou |
| UnitTests | TokenAndCodeTests | Importacao_so_pode_ser_aplicada_uma_vez | Passou |
| UnitTests | TokenAndCodeTests | Refresh_token_expirado_ou_revogado_nao_esta_ativo | Passou |
| UnitTests | TokenAndCodeTests | Site_autorizado_guarda_so_a_origem(input: "http://localhost:5080/demo/", expected: "http://localhost:5080") | Passou |
| UnitTests | TokenAndCodeTests | Site_autorizado_guarda_so_a_origem(input: "https://vagas.exemplo.com/candidatura/1?x=1", expected: "https://vagas.exemplo.com") | Passou |
| UnitTests | TokenAndCodeTests | Site_autorizado_recusa_origem_invalida(input: "ftp://x.com") | Passou |
| UnitTests | TokenAndCodeTests | Site_autorizado_recusa_origem_invalida(input: "javascript:alert(1)") | Passou |
| UnitTests | TokenAndCodeTests | Site_autorizado_recusa_origem_invalida(input: "nao e url") | Passou |

## 7. Bugs reais encontrados durante o desenvolvimento

Registrados na ordem em que apareceram. "Como apareceu" diz honestamente se foi teste automatizado, execução manual ou revisão.

| # | Bug | Como apareceu | Correção |
|---|---|---|---|
| 1 | `DisplayNames.TryParse` usava `Contains` com sinônimos num dicionário: "Superior **incompleto**" casava com "completo" e virava `Completed`; o sinônimo "pos" (pós-graduação) casava com "**pos**suo". | Revisão do código logo depois de escrever, antes do primeiro teste. | Lista ordenada ("incomplet" antes de "complet"), "pos" removido; teste `Rotulos_pt_BR_viram_enum` com "Superior incompleto". |
| 2 | Chave AES de desenvolvimento (`appsettings.Development.json`) tinha **31 bytes** (Base64 de um texto de 31 caracteres): a primeira operação com dado sensível derrubaria a API. | Conferência do tamanho decodificado ao configurar. | Chave de 32 bytes; o construtor do `AesGcmSensitiveDataProtector` valida o tamanho e falha na subida (teste `Chave_AES_com_tamanho_errado_falha_cedo`). |
| 3 | Uma execução anterior (`timeout 25 dotnet run`) deixou a porta 5080 ocupada: a segunda subida falhou com "address already in use". | Execução manual. | Operacional: encerrar o processo antigo. Registrado aqui porque a porta 5080 é fixa (contrato com a extensão). |
| 4 | Script de fluxo curl passava o header `Authorization` em variável sem aspas: "Bearer" e o token viravam argumentos separados e todas as rotas protegidas davam 401. | Execução manual do fluxo. | Header em *array* do bash. A API estava correta (o 401 em ProblemDetails confirmou o *challenge*). |
| 5 | ProblemDetails e `/health` saíam com acentos escapados (`Não autenticado`). | Leitura da saída do curl. | `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` (UTF-8 legível) nos dois pontos. |
| 6 | **Ordem dos middlewares**: o log de requisição do Serilog ficava *dentro* do middleware de exceções. Um login errado — 401 correto para o cliente — era registrado como "respondeu **500**" com a `UnauthorizedException` e o stack trace no log. | Teste automatizado `Logs_nao_contem_dados_pessoais` (sink em memória). | `UseSerilogRequestLogging` antes de `ExceptionHandlingMiddleware`. |
| 7 | O sink de teste renderizava propriedades com aspas (`HTTP "POST" "/api/..."`) e a asserção não encontrava a linha. | Mesmo teste. | Formatter `{Message:lj}` no sink de teste. |
| 8 | A classe auxiliar de teste `Api` colidia com o namespace `Zyft.Api` (CS0234 em 29 lugares). | Compilação. | Renomeada para `TestApi`. |
| 9 | **Serilog com logger estático**: sem `preserveStaticLogger`, o provider escreve no `Log.Logger` global, que cada host sobrescreve. Com várias `WebApplicationFactory` em paralelo, os logs de um host iam para o sink de outro: o teste de logs passava isolado e falhava na suíte completa (3 de 3 execuções). | Teste automatizado intermitente por paralelismo. | `UseSerilog(..., preserveStaticLogger: true)` e `RequestLoggingOptions.Logger` vindo do DI. Suíte estável em execuções repetidas. |
| 10 | Contrato com o frontend: a proposta de importação saía como `personal` e `skills: string[]`, mas a web espera `profile` e itens no formato das entidades (`skills: [{name, level}]`, enums como nome). O `apply` também esperava `personal`. | Recado do agente de frontend (suposições de contrato). | `ResumeProposal.Profile`, `ProposalSkill`, enums convertidos para nome (`Undergraduate`, `InProgress`, `Basic`), `Type` do curso como texto; teste de integração do DOCX cobre o novo formato. |
| 11 | D-02: o painel da extensão chama `POST /authorized-sites`, mas a rota exigia token web → 403 para a extensão. | Revisão cruzada da Entrega 2 (D-02). | Rota aberta para `ExtensionOrWeb` (remoção continua só web); teste `Extensao_pode_autorizar_site_mas_nao_remover`. |
| 12 | D-03: o painel enviava `maxLength: 0` para "sem limite" e o validador (20–10.000) devolvia 400. | Revisão cruzada da Entrega 2 (D-03). | API aceita `0`/`null` como sem limite e recusa negativos; extensão passou a enviar `null` (`extension/sidepanel.js`, linha da chamada `/forms/open-answers`); testes para `0`, `5` e `-1`. |
| 13 | Teste de fumaça (Playwright): `wait_for_url` não dispara em navegação por *hash* e `wait_for_function` foi bloqueado pela CSP da web (`script-src 'self'`, sem `unsafe-eval`) — o que confirma que a CSP está ativa; `get_by_label("Cidade")` casava dois campos. | Execução do teste de fumaça. | Esperas por seletor e seletores por `name`. Problemas só do script; a aplicação não precisou mudar. |

Cuidado tomado de antemão (não virou bug): a formatação pt-BR de moeda usa espaço não separável (`R$ 2.500,00`), por isso o teste compara com `\s`; e a imagem Docker usa Debian (com ICU), não Alpine, para a cultura `pt-BR` funcionar.

## 8. Desvios do brief e pendências

| Item | O que mudou | Motivo |
|---|---|---|
| Entidade `Application` | Classe `JobApplication` (tabela `Applications`). | `Application` colide com o namespace `Zyft.Application`. |
| `Course`/`Certification` | Uma entidade `Course` com `Type` (`Course`/`Certification`), rota única `/profile/courses`. | Mesmos campos; o de-para separa `courses[i]` e `certifications[i]`. |
| Consentimentos | Todos nascem **desligados** (o brief só dizia que SensitiveData começa false). | D-01 / promessa da Entrega 1. A Boas-vindas liga (a) e (b). |
| `POST /authorized-sites` | `ExtensionOrWeb` (o restante de sites/dispositivos segue só web). | D-02. |
| Valores de `state`/`source` | Serializados como nome do enum (`Ready`, `Rule`), não minúsculo. | Pedido do frontend (enums como nome em toda a API). |
| `POST /forms/open-answers` | Responde 200 com `kind: unknown` (sem `value`) quando a IA está fora, em vez de 503. | RNF-12: o painel continua funcionando e mostra "responda manualmente". |
| `POST /forms/mappings` | Perguntas abertas não geram texto dentro do de-para: vêm como `openQuestion`/`Missing` e o painel chama `/forms/open-answers`. | Mantém o de-para rápido (RNF-04) e exige o consentimento (a) só onde há texto gerado. |
| Exclusão de dados sensíveis | Desligar o consentimento (c) apaga os dados sensíveis guardados. | Minimização (LGPD); documentado na rota. |
| `Users.DeletedAt` | Mantido, sem uso (exclusão física em cascata). | Segue o brief; D-05 continua como decisão de documentação. |
| Migrations | Só PostgreSQL; SQLite usa `EnsureCreated`. | Ver 2.1. |

### Situação dos itens D-01 a D-09 (seção 7.1 da Entrega 2)

| # | Situação |
|---|---|
| D-01 | **Resolvido**: consentimentos nascem desligados; teste `Conta_nova_nasce_com_todos_os_consentimentos_desligados`; a Boas-vindas (`web/js/views/onboarding.js` → `consentsPanel`) faz `PUT /consents/{purpose}` — confirmado no teste de fumaça. |
| D-02 | **Resolvido** (rota aberta para a extensão, com teste). |
| D-03 | **Resolvido** nos dois lados (API aceita 0/null; extensão envia null). |
| D-04 | **Resolvido**: limite por pessoa nas rotas de IA/importação; teste `Rotas_de_IA_tem_limite_por_pessoa_e_nao_por_IP`. |
| D-05 | **Aberto (documentação)**: `DeletedAt` continua na tabela sem uso; decidir entre remover na próxima migration ou documentar o uso futuro. |
| D-06 | **Resolvido**: índice único `Languages(ProfileId, Name)` já na migration inicial. |
| D-07 | **Resolvido**: em `Production` a API não sobe sem `Cors:ExtensionIds`. Em Development/Staging qualquer extensão é aceita (o ID da extensão descompactada muda por máquina). |
| D-08 | **Aberto** (Entrega 4): verificação de e-mail e login Google. |
| D-09 | **Resolvido**: `docker-compose.yml`, `ci.yml` e as três suítes no repositório (CI ainda não executado no GitHub — nada foi enviado). |

Outras pendências conhecidas: o primeiro de-para com IA leva ~40 s enquanto o Ollama carrega o modelo (as chamadas seguintes ficam em segundos); a proposta de importação sem IA só traz contatos, links, competências e idiomas; `DataProtection` do ASP.NET guarda chaves no container (aviso no log; não é usado para dados da aplicação, que usam a chave AES própria).
