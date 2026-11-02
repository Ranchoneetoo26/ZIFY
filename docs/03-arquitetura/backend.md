# Arquitetura do backend (API .NET 8)

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Arquitetura do backend |
| Versão | 3.4 |
| Status | Em revisão. Módulos da Entrega 3 e da versão 2 implementados e conferidos no código (02/11/2026). Depende do [ADR-0017](adr/0017-backend-em-csharp-dotnet-clean-architecture.md) (proposto). |
| Dono | Arquitetura de software, com o time de backend |
| Documentos relacionados | [DAT](dat.md), [API](api.md), [IA](ia.md), [Modelo de dados](modelo-de-dados.md), [Segurança](seguranca.md), [Entrega 2 §1](../entregas/entrega-2-projeto-e-arquitetura.md#1-arquitetura-da-solução), [Relatório do backend](../entregas/entrega-3-relatorio-backend.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.x | até 05/10/2026 | Backend do marketplace. Arquivado. |
| 2.0 | 12/10/2026 | Alvo em Node.js/TypeScript (Fastify, Zod, pg-boss), substituído junto com o [ADR-0003](adr/0003-monorepo-typescript-node.md). |
| 3.0 | 21/10/2026 | Reescrito para a API real em C#/.NET 8 com Clean Architecture e CQRS. Acrescenta os módulos da versão 2: catálogo por área, onboarding, campos personalizados, planos e uso, anúncios. |
| 3.1 | 21/10/2026 | Sincronizado com a implementação: controllers novos (`BillingController`, `AdsController`, `CatalogController`, `OnboardingController` e rotas `custom-fields` no `ProfileController`), pontos de chamada do `IUsageMeter` confirmados, catálogo de anúncios estático, migration `20261021233314_V2PlanosAnunciosOnboarding`, 223 testes (136 + 75 + 12). |
| 3.2 | 21/10/2026 | `DELETE /extension/devices/current` (`RevokeCurrentDeviceCommand`, 77 operações); `POST /billing/packs` só com token web; 402 documentado no Swagger nas rotas com cota; regras de LinkedIn e GitHub corrigidas (§7); 230 testes (140 + 78 + 12). |
| 3.3 | 21/10/2026 | Correções da gravação do tour: `trialAvailable`/`trialDays` no `BillingDto`; `CourseInput.Type` nulo vale `Course`; erro de model binding aponta o campo, não o parâmetro; `ProposalCompleteness` e `missingFields` na importação, mensagens do `apply` nomeiam o item; `TextTools.NormalizeExtracted` (NFKC) e `ResumeParser.CourseKind`; `open-answers` com texto vazio vira `missing` com `OpenAnswerNotes.NoDraft`. 252 testes (156 + 84 + 12). |
| 3.4 | 02/11/2026 | Conta e segurança: verificação de e-mail (`UserToken`, `IEmailSender` com `LoggingEmailSender`, `App:WebBaseUrl`), aceite dos termos com versão (`Terms.CurrentVersion`), troca e recuperação de senha que encerram as sessões (RF-01, RF-30). Concorrência otimista do perfil (`Profile.Version`, `IProfileConcurrency`, `ProfileVersionFilter`, 409 `profile.conflict`, RNF-18). Troca de período do Plus com rateio (`Subscription.ChangePeriod`, `SubscriptionPeriodChange`, RF-31) e pacote no Plus (`packTotals`). `[AllowAnonymous]` por action no `AuthController`. `MapFallback` para endereços sem hash. SQLite: `SqliteSchemaUpgrader` (atualização aditiva; não é mais preciso apagar o banco quando o esquema muda). Quatro migrations novas. 84 operações. |

## 1. Visão geral

A API é um **monólito modular em ASP.NET Core 8**, organizado em Clean Architecture com quatro projetos. Cada caso de uso é um command ou query do MediatR. Não há worker separado: as chamadas de IA são síncronas, com tempo limite e degradação para as regras quando a IA não responde (RNF-12).

A mesma API serve a plataforma web (`web/`, em `/`) e o portal de demonstração (`/demo/`). O protótipo em Node.js (`server/src/*.js`) é a referência de **comportamento** do de-para, da análise de vaga, da redação e da importação; o código foi portado para C#.

## 2. Projetos e dependências

```
src/Zyft.Domain/          entidades, enums, Guard, DomainException; sem nenhum pacote
src/Zyft.Application/     casos de uso (MediatR), DTOs, validators (FluentValidation), portas,
                          FieldMapper, JobAnalyzer, AnswerWriter, ProfileFacts, catálogos estáticos
src/Zyft.Infrastructure/  EF Core (ZyftDbContext, Configurations, Migrations, repositórios, UnitOfWork),
                          SqliteSchemaUpgrader, JWT, BCrypt, AES-256-GCM, OllamaLlmClient, PdfPig/OpenXml,
                          LoggingEmailSender (Email/)
src/Zyft.Api/             controllers finos, ExceptionHandlingMiddleware, Problems, policies,
                          Swagger, Serilog, rate limiting, health checks, CORS, arquivos estáticos
```

Controllers (17, 84 operações): `Auth`, `Me`, `Consents`, `Profile` (com `custom-fields` na v2), `ResumeImports`, `Answers`, `SensitiveData`, `Extension`, `AuthorizedSites`, `Forms`, `Jobs`, `Applications`, `AuditEvents` e, na v2, `Catalog`, `Onboarding`, `Billing` e `Ads`. Todos herdam de `ApiControllerBase` e só chamam `Sender.Send`. No `AuthController` o `[AllowAnonymous]` fica em cada action pública, não na classe, porque na classe ele anularia o `[Authorize(Policy = WebUser)]` de `verify/resend` e `password`.

| Regra | Como é garantida |
|---|---|
| `Domain` não referencia nada | Projeto sem `PackageReference` nem `ProjectReference` |
| `Application` referencia só `Domain` | Usa portas: `IRepository<T>`, `IProfileRepository`, `IUnitOfWork`, `IPasswordHasher`, `ITokenService`, `ICurrentUser`, `IClock`, `IResumeTextExtractor`, `ISensitiveDataProtector`, `ILlmClient`, `IEmailSender` e `IProfileConcurrency` (`Common/Interfaces/Ports.cs`) |
| `Infrastructure` implementa as portas | Registro em `DependencyInjection.cs` |
| Controllers não acessam o banco | Todo controller só chama `IMediator.Send` |

## 3. Fluxo de uma requisição

1. Middleware: Serilog (log da requisição), `ExceptionHandlingMiddleware`, autenticação JWT, autorização por policy (`WebUser` ou `ExtensionOrWeb`), rate limiting.
2. Controller monta o command ou query.
3. Pipeline do MediatR: `LoggingBehavior` (nome do caso de uso e tempo, sem conteúdo) → `ValidationBehavior` (FluentValidation; falha vira 400 com `errors`) → handler.
4. Handler aplica regras: consentimento (`ConsentGuard`), **cota de uso** (v2, seção 5), domínio, persistência pelo `IUnitOfWork`, evento de auditoria.
5. Exceções de domínio viram ProblemDetails com `code` estável (422, 402, 409…).

## 4. Módulos (pastas da Application)

| Pasta | Responsabilidade | Requisitos | Entidades |
|---|---|---|---|
| `Auth` | Cadastro (com aceite dos termos na versão vigente), login, refresh com rotação e detecção de reuso, logout; verificação de e-mail (`EmailVerification.cs`); troca e recuperação de senha (`PasswordFeatures.cs`, `Sessions.RevokeAllAsync` revoga refresh tokens e extensões) | RF-01, RF-30 | `User`, `RefreshToken`, `UserToken` |
| `Account` | `GET /me`, exportação JSON, exclusão em cascata | RF-08 | todas |
| `Consents` | Finalidades `AiText`, `JobAnalysis`, `SensitiveData`; desligar sensíveis apaga os dados | RF-07 | `Consent` |
| `Profiles` | Dados básicos, seções, `missing`, `ProfileFacts`; v2: `WorkArea`, `ProfessionalRegistration`, `SalaryType` e **campos personalizados** | RF-02, RF-05, RF-24, RF-25 | `Profile`, `Experience`, `Education`, `Course`, `Skill`, `Language`, `CustomField` |
| `Catalog` (v2) | Catálogo estático das 8 áreas (`AreaCatalog`) | RF-24 | — |
| `Onboarding` (v2) | Progresso do onboarding em passos (ids respondidos e pulados) | RF-23 | `OnboardingProgress` |
| `Imports` | Extração de PDF/DOCX em memória, `ResumeParser` com IA ou heurística, proposta, aplicar ou descartar | RF-03, RF-04 | `ResumeImport` |
| `Answers` | Banco de respostas (CRUD); v2: até 15 no Gratuito | RF-06, RF-26 | `ApprovedAnswer` |
| `Sensitive` | Dados sensíveis cifrados | RN-04 | `SensitiveData` |
| `Extension` | Código de pareamento, pareamento, dispositivos, revogação (pela plataforma ou pela própria extensão, `RevokeCurrentDeviceCommand`); v2: 1 navegador no Gratuito, 3 no Plus | RF-09, RF-26 | `PairingCode`, `ExtensionDevice` |
| `Sites` | Sites autorizados (só a origem) | RF-10 | `AuthorizedSite` |
| `Forms` | `FieldMapper` (de-para), `FieldRules`, sugestões de texto, conclusão | RF-11 a RF-16, RF-18, RF-19 | — |
| `Writing` | `AnswerWriter`: texto livre só com fatos | RF-15 | — |
| `Jobs` | `JobAnalyzer`: requisitos com evidência por linha | RF-17 | — |
| `Applications` | Lista, detalhe, status e anotação, exclusão | RF-19 | `JobApplication` |
| `Audit` | Registro de ações (lista fechada `AuditActions`) | RF-20 | `AuditEvent` |
| `Billing` (v2) | `GET /billing`, assinatura e cancelamento simulados, troca de período com rateio, pacotes (também no Plus, `packTotals`), recompensado; `UsageMeter` | RF-26 a RF-28, RF-31 | `Subscription`, `SubscriptionPeriodChange`, `MonthlyUsage`, `UsagePack`, `UsageMark` |
| `Ads` (v2) | Catálogo de demonstração (`AdCatalog`), seleção por área, cargos e tema, ocultar, clique agregado | RF-29 | `AdHide`, `AdStat` |

### 4.1 Conta: e-mail, termos e senha (RF-01, RF-30)

- **Tokens de e-mail** (`UserToken`, tabela `UserTokens`): uso único, só o hash SHA-256 no banco, `Purpose` `EmailVerification` (24 h) ou `PasswordReset` (30 min). Emitir um novo invalida o anterior da mesma finalidade. Um token de verificação não serve para redefinir senha.
- **Envio** por `IEmailSender`. A única implementação é `LoggingEmailSender` (Infrastructure/Email), registrada em qualquer ambiente: não envia nada. Com `Email:LogLinks=true` (`EmailOptions`, ligado só em `appsettings.Development.json`) registra o link (`{App:WebBaseUrl}/#/verificar?token=…` ou `/#/redefinir-senha?token=…`); sem isso, só "E-mail de … gerado (envio não configurado).", sem token. Nunca o endereço nem o nome (RNF-09). Um provedor real de e-mail ainda não existe; ver [segurança §3.3](seguranca.md).
- **E-mail não verificado não bloqueia nenhuma rota** (teste `Email_nao_verificado_nao_bloqueia_rotas`). `GET /me` e o `user` do login trazem `emailVerified`.
- **Termos:** `RegisterValidator` exige `acceptTerms: true` e `termsVersion == Terms.CurrentVersion` (hoje `2026-10-22`, em `Domain/Users/User.cs`). A conta guarda `TermsVersion` e `TermsAcceptedAt`, e o registro ganha `termos.aceitos`. Mudar a constante obriga quem se cadastrar a aceitar a versão nova; contas existentes não são afetadas.
- **Troca de senha** (`POST /auth/password`): confere a senha atual, recusa senha nova igual, revoga todos os refresh tokens e todas as extensões pareadas (`Sessions.RevokeAllAsync`) e devolve um par novo para a sessão atual. **Recuperação** (`forgot` sempre 202, sem enumeração; `reset` com o token): troca a senha, revoga as sessões, invalida links em aberto e marca o e-mail como verificado. Eventos `senha.alterada` e `senha.redefinida`.
- Os usuários de demonstração já saem com e-mail verificado e termos aceitos; contas demo de bancos antigos são atualizadas na subida (`DemoSeed.UpgradeExistingAsync`).

### 4.2 Concorrência otimista do Perfil Mestre (RNF-18)

`Profile.Version` sobe a cada alteração dos dados básicos ou de qualquer item e é token de concorrência do EF (`IsConcurrencyToken`). O `ProfileVersionFilter` (filtro do `ProfileController`) lê `If-Match` e coloca a versão esperada em `IProfileConcurrency`; o `ZyftDbContext.SaveChangesAsync` usa essa versão como valor original, então o `UPDATE … WHERE Version = esperada` faz a verificação sem janela entre ler e gravar. `DbUpdateConcurrencyException` vira `ProfileVersionConflictException` → 409 `profile.conflict` com `currentVersion`. A resposta sai com `ETag`. Sem `If-Match` (ou com `*`) nada muda para o cliente, mas duas gravações simultâneas também não se sobrescrevem em silêncio.

## 5. Planos e cota de uso (v2)

O `IUsageMeter` (`Application/Billing/UsageMeter.cs`) concentra a regra:

- `ConsumeAsync(userId, resource, dedupKey)`: consome 1 crédito na ordem **limite do mês → bônus recompensado (só candidaturas) → pacote que vence primeiro**. Sem crédito, lança `PlanLimitReachedException` → **402** `plan.limit_reached` com `resource`, `used` e `limit`.
- `dedupKey` (origem + caminho da página) faz a candidatura assistida contar **uma vez por mês por página**. A chave é guardada só como hash em `UsageMarks`.
- `EnsureAnswerCapacityAsync` e `EnsureDeviceCapacityAsync`: 402 com `resource` `answers` ou `devices`.
- Os limites ficam em `PlanRules` (`Domain/Billing/Plans.cs`): 10 assistidas, 5 sugestões de IA, 10 análises, 2 importações, 15 respostas, 1 navegador; Plus sem limite e com 3 navegadores; pacote +15/+10 por 60 dias; recompensado +3 até 3 vezes.

**Onde é chamado (conferido no código):**

| Handler | Chamada | `resource` no 402 |
|---|---|---|
| `MapFieldsHandler` (`POST /forms/mappings`) | `ConsumeAsync(AssistedApplications, PageKey(pageUrl))`; a chave é `esquema://host/caminho`, sem query | `assistedApplications` |
| `OpenAnswerHandler` (`POST /forms/open-answers`) | `ConsumeAsync(AiSuggestions)` | `aiSuggestions` |
| `AnalyzeJobHandler` (`POST /jobs/analyze`) | `ConsumeAsync(JobAnalyses)` | `jobAnalyses` |
| `CreateResumeImportHandler` (`POST /resume-imports`) | `ConsumeAsync(ResumeImports)` | `resumeImports` |
| `AnswerHandlers` (`POST /answers`) | `EnsureAnswerCapacityAsync`, só quando a pergunta é nova (reaprovar substitui e não conta) | `answers` |
| `PairDeviceHandler` (`POST /extension/pair`) | `EnsureDeviceCapacityAsync` (conta extensões ativas, não revogadas; a extensão que se desconecta por `DELETE /extension/devices/current` deixa de contar) | `devices` |
| `GetAdHandler` (`GET /ads`) | `GetPlanAsync`: Plus recebe 204 sem consultar o perfil | — |

Todas as rotas com `resource` na tabela declaram `[ProducesResponseType(typeof(ProblemDetails), Status402PaymentRequired)]`, então o 402 aparece no Swagger.

As chamadas ficam **depois** da validação e do consentimento e **antes** da chamada à IA. Um `IPipelineBehavior` genérico foi considerado e descartado: a cota depende de dados que só o handler conhece (a origem da página, se a pessoa já tinha usado aquela página) e precisaria de marcação por atributo, o que esconde a regra.

O consumo é registrado nas entidades rastreadas e só vira linha no banco no `SaveChanges` do caso de uso. Se a operação falhar antes (por exemplo, IA fora do ar na importação), nada é cobrado da cota.

Assinar, trocar o período, cancelar e comprar pacote são **simulados**: não existe gateway, cartão nem webhook. As quatro rotas exigem `Policies.WebUser` (token da plataforma); a extensão recebe 403 e só lê `GET /billing`. O `BillingDto` sempre devolve `demo: true` (RN-13). Cancelar volta ao Gratuito na hora. O pacote cobre só candidaturas assistidas e sugestões de IA (`UsagePack.RemainingOf` devolve 0 para os outros recursos). Ausência de linha em `Subscriptions` significa Gratuito; a linha é criada na primeira assinatura.

**Pacote no Plus.** `POST /billing/packs` aceita quem assina o Plus. No Plus o `UsageMeter` não gasta nada (sem limite), então os créditos do pacote ficam guardados e só entram em uso se a pessoa voltar ao Gratuito antes de o pacote vencer (60 dias depois da compra). `BillingDto.PackTotals` soma os pacotes ativos (teste `No_Plus_os_pacotes_nao_sao_gastos_e_ficam_para_depois`).

**Troca de período (RF-31).** `Subscription.ChangePeriod(to, now)` vale na hora. No teste grátis só troca o período (valor 0, renovação no fim do teste). Pago: crédito = preço do período atual × (tempo que falta do ciclo ÷ duração do ciclo), valor = preço novo − crédito (negativo = crédito a favor da pessoa), e o ciclo novo começa na troca. Cada troca vira uma linha em `SubscriptionPeriodChanges` (entra na exportação) e o evento `plan.period_changed`. Sem Plus: 422 `billing.not_subscribed`; mesmo período: 422 `billing.same_period`.

## 6. Anúncios (v2)

- O catálogo é **estático**: `Application/Ads/AdCatalog.cs`, 16 anúncios com ids de texto (`demo-tec-sql`, `demo-tec-cloud`, `demo-sau-sbv`…), área, assunto e palavras-chave. Não existe tabela de anúncios; o banco guarda só `AdHides` e `AdStats`.
- `GET /ads` recebe `placement` (obrigatório: sem ele, 400) e `topic`. Os placements válidos e o formato de cada um ficam em `AdPlacements.Allowed`. O handler lê `WorkArea` e `DesiredRoles` do perfil **no servidor** e monta um `AdTargeting` que, por construção, só tem esses dois campos e o `topic`. O `AdSelector` filtra os ocultos, pontua (requisito 10, área 5, cargos 3, geral 1) e, entre os empatados, escolhe o de menos exibições. Devolve 204 para Plus, placement desconhecido e quando nada combina.
- No `ext.job.contextual`, um anúncio só entra se o `topic` casar com alguma palavra-chave dele. Sem relação, 204.
- `reason` é montado a partir da regra que casou (requisito da vaga, cargos ou área) e termina com a frase fixa "Usamos só a sua área, os cargos que você busca e o texto da vaga", sem repetir dado pessoal.
- `POST /ads/{id}/click` e cada exibição incrementam `AdStat` (sem usuário). `POST /ads/{id}/hide` grava `AdHide`. `POST /ads/rewarded/claim` chama `MonthlyUsage.ClaimRewarded()` (409 `ads.rewarded_exhausted` depois de 3 no mês; 409 `ads.rewarded_unavailable` para Plus).
- Anúncios **não** usam IA e nada deles entra em prompt (ver [IA](ia.md)).

## 7. De-para com campos personalizados (v2)

O `FieldMapper` mantém a ordem: bloqueios → regras determinísticas → respostas aprovadas → IA (só aponta chave) → validação. Na versão 2 (implementado e coberto por testes de unidade e integração):

- cada `CustomField` vira uma chave `custom:<id>` no perfil achatado e uma linha em `ProfileFacts` ("Disponível para plantão noturno: Sim, 12x36");
- as **regras** comparam o rótulo do campo da página com o `Label` do campo personalizado (mesma normalização sem acento usada nas outras regras);
- a normalização (`TextTools.Norm`) também separa camelCase: "LinkedIn" vira "linked in" e "GitHub" vira "git hub". Por isso as regras de `FieldRules` usam `\blinked ?in\b` e `\bgit ?hub\b`; antes da correção esses campos não casavam por regra (teste `LinkedIn_e_GitHub_casam_por_regra_mesmo_com_camelCase`);
- a **IA** recebe as chaves `custom:<id>` com o rótulo, nunca o valor, e pode apontá-las; a validação confere se a chave existe e lê o valor do perfil (RN-02);
- campos personalizados não podem guardar sensíveis (rótulo recusado no domínio por `SensitiveLabels`), então não abrem uma porta lateral para dados que o RN-04 segrega.

## 8. Erros

Formato e códigos na [API §5](api.md#5-códigos-de-erro). O `ExceptionHandlingMiddleware` traduz:

| Exceção | HTTP | `code` |
|---|---|---|
| `ValidationException` (FluentValidation) | 400 | `validation_failed` |
| JSON inválido | 400 | `bad_request` |
| Falha de autenticação | 401 | `unauthorized` |
| `ForbiddenException` / policy | 403 | `consent_required` / `forbidden` |
| `NotFoundException` | 404 | `not_found` |
| `PlanLimitReachedException` (v2) | 402 | `plan.limit_reached` + `resource`, `used`, `limit` no topo e repetidos em `extensions` |
| `RewardedExhaustedException` (v2) | 409 | `ads.rewarded_exhausted` |
| `ConflictException` com código (v2) | 409 | `ads.rewarded_unavailable` (oferta pedida no Plus), `email.already_verified` |
| `ProfileVersionConflictException` | 409 | `profile.conflict` + `currentVersion` |
| Conflito / `DbUpdateException` | 409 | `conflict` |
| `DomainException` | 422 | o `code` da regra |
| Qualquer outra | 500 | `internal_error`, sem stack trace |

## 9. Limites, logs e configuração

- **Rate limiting:** por IP em autenticação, verificação de e-mail, troca e recuperação de senha e pareamento (política `auth`, 10/min, um limite compartilhado entre essas rotas); por pessoa nas rotas de IA e importação (60/min). A cota mensal do plano é outra coisa: rate limiting protege a infraestrutura, a cota define o produto.
- **Logs (Serilog):** método, caminho sem query, status, duração e nome do caso de uso. Nunca corpo, e-mail, token, prompt, resposta da IA, rótulo de campo personalizado, URL de página ou anúncio clicado por pessoa (RNF-09, RNF-14).
- **Configuração:** `Database:Provider`, `ConnectionStrings:Default`, `Jwt:*`, `Encryption:Key`, `Llm:*`, `Cors:*`, `RateLimiting:*`, `Seed:DemoUser`, `App:WebBaseUrl` (endereço da plataforma nos links de e-mail, padrão `http://localhost:5080`). Detalhes no [runbook local](../06-operacao/runbook-local.md).
- **Banco na subida** (`DatabaseInitializer`): PostgreSQL aplica as migrations (`Database.Migrate`; a da v2 é `20261021233314_V2PlanosAnunciosOnboarding`); as migrations de 02/11/2026 são `20261102183823_VerificacaoDeEmail`, `20261102184148_AceiteDosTermos`, `20261102184704_VersaoDoPerfil` e `20261102185344_TrocaDePeriodoDoPlus`. SQLite usa `EnsureCreated`, porque as migrations são específicas do PostgreSQL, seguido do `SqliteSchemaUpgrader`: cria as tabelas e os índices que faltam e adiciona as colunas novas (com valor padrão quando obrigatórias), sem nunca remover nem alterar o que existe (teste `Banco_antigo_ganha_tabelas_e_colunas_que_faltam_sem_perder_dados`). Por isso **não é mais preciso apagar `zyft-dev.db` quando o esquema muda**; apagar só serve para zerar os dados. Mudança não aditiva (renomear ou trocar tipo de coluna) continua exigindo apagar o banco de desenvolvimento.
- **Endereço sem hash:** `MapFallback` em `Program.cs` redireciona GET/HEAD fora de `/api` e sem extensão de arquivo (ex.: `/conta`) para `/#/conta`, mantendo a query (`/redefinir-senha?token=abc` → `/#/redefinir-senha?token=abc`). O destino sempre começa com `/#`, então não há redirecionamento para fora do site (`//evil.com` é 404 por parecer arquivo; `//evil.com/x` vira `/#//evil.com/x`). Arquivo inexistente continua 404.
- **Seed de demonstração** (só Development, `DemoSeed`): Rafael Moreira (`demo@zyft.local`, Tecnologia, perfil completo, onboarding concluído, 2 campos personalizados, 7 de 10 candidaturas assistidas usadas no mês) e Beatriz Lima (`beatriz@demo.zyft`, Saúde, onboarding parado em `competencias` com passos pulados, 2 campos personalizados), ambos com a senha `Zyft@2026` e no plano Gratuito.

## 10. Testes

| Nível | O que cobre | Ferramenta |
|---|---|---|
| Unidade (140) | Domínio (regras de perfil, tokens, códigos), `FieldMapper`, `JobAnalyzer`, `AnswerWriter`, handlers com Moq, segurança. v2 (`V2/V2DomainTests.cs`, 46 casos): `PlanRules`, `MonthlyUsage.Consume` (ordem limite → bônus → pacote), pacote vencido e pacote que não cobre análise de vaga, recompensado até 3 vezes, teste grátis só na primeira vez, medidor que guarda só hash, `SensitiveLabels`, `OnboardingProgress.Update`, de-para com `custom:<id>`, `AdSelector` sem dados pessoais; regras de LinkedIn e GitHub com camelCase (`FieldMapperTests`, 4 casos) | xUnit, FluentAssertions, Moq |
| Integração (78) | API real em memória com SQLite: autenticação, policies, erros, regras de confiança, importação, rate limit, CORS, logs sem PII. v2 (`V2Tests.cs`, 23 casos): 402 com `extensions`, deduplicação por página, pacote, Plus e cancelamento, limite de respostas e de navegadores, 204 de anúncio para Plus e placement proibido, resposta de `/ads` sem PII, contextual e ocultar assunto, 422 de rótulo sensível, extensão lê billing e não assina; limite atingido nunca bloqueia exportar nem excluir (TN-14); extensão que se desconecta libera a vaga de navegador e não compra pacote (403); logs das rotas v2 sem dados pessoais | `WebApplicationFactory` |
| Aceitação (12) | Regras inegociáveis em Gherkin (as mesmas da Entrega 3; a v2 ainda não tem cenário Reqnroll) | Reqnroll |

Execução registrada em 21/10/2026: **252 testes, todos passando** (156 unidade, 84 integração, 12 aceitação). Depois disso entraram os testes dos QAs de 01/11/2026 e, em 02/11/2026, 19 casos de unidade (`Auth/AccountSecurityTests`, `Billing/PeriodChangeTests`, `DomainTests`) e 22 de integração (`AccountSecurityTests`, `ProfileConcurrencyTests`, `BillingPeriodTests`, `SqliteUpgradeTests`); a contagem total precisa ser conferida de novo com `dotnet test`. Na Entrega 3 eram 157 (90 + 55 + 12); na primeira contagem da v2, 223 (136 + 75 + 12); depois das correções pós-v2, 230 (140 + 78 + 12); as correções da gravação do tour acrescentaram 22 (`ResumeImportFixesTests` e `TourFixesTests`). Os testes da versão 2 estão descritos na [estratégia de testes](../04-qualidade/estrategia-de-testes.md) e nos [critérios de aceite](../04-qualidade/criterios-de-aceite-mvp1.md) CA-23 a CA-29.
