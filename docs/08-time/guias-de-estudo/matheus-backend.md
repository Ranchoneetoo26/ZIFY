# Guia de estudo: backend .NET

| Campo | Valor |
|---|---|
| Integrante | Matheus Guilherme Moreira De Oliveira |
| Papel | Dev backend (API .NET, CQRS, EF Core, autenticação, testes da API) |
| Versão | 1.1 (produto v2, sincronizado com a implementação) |
| Data | 21/10/2026 |
| Relacionados | [Papéis e RACI](../papeis-e-raci.md), [Backend](../../03-arquitetura/backend.md), [API](../../03-arquitetura/api.md), [Modelo de dados](../../03-arquitetura/modelo-de-dados.md), [ADR-0017](../../03-arquitetura/adr/0017-backend-em-csharp-dotnet-clean-architecture.md), [Estratégia de testes](../../04-qualidade/estrategia-de-testes.md) |

Este é um guia de estudo. Ele diz o que você precisa dominar para responder à banca; não registra autoria de código nem de documento.

## 1. O projeto inteiro em uma página

**Problema.** Redigitar os mesmos dados em cada site de vagas: 4,40 de 5, 80% de notas 4–5. Experiências e formação item a item é a etapa mais demorada (67%).

**Pesquisa.** 15 respostas válidas (uma cópia da v1 excluída), 60% de tecnologia. Confiança 3,4; interesse 3,73. Perfil único e importar currículo: 87%. Barreiras: preço 47%, privacidade 40%, poucos sites 40%. Gratuito com limite: 60%. Ver [resultados-v2.md](../../05-pesquisa/resultados-v2.md).

**MVP 1 (v2).** Perfil Mestre, importação, extensão que preenche só o aprovado, IA presa a fatos. Na v2: onboarding com progresso (RF-23), catálogo por área e campos personalizados (RF-24, RF-25), planos e limites com 402 (RF-26), pacote (RF-27), recompensado (RF-28), anúncios (RF-29).

**Regras inegociáveis no backend.** O valor objetivo sai do perfil, nunca da IA (RN-02). Sensível fora da IA e só com consentimento (RN-04). Consentimento bloqueado na API, não só na tela. Nada de PII em log (RNF-09). Recurso de outra pessoa responde 404. Na v2: anúncio sem PII (RN-09, RNF-14), contagem idempotente (RNF-16), limite nunca bloqueia exportar, excluir e editar perfil (RN-12), rótulo sensível em campo personalizado é 422 (RN-15).

**Personas.** Rafael (primária, tecnologia), Bia (secundária, perguntas abertas), Beatriz (saúde, hipótese). São também os usuários demo.

**Arquitetura em 5 linhas.** `Zyft.Domain` (regras puras, sem pacotes) ← `Zyft.Application` (commands e queries MediatR, validators FluentValidation, portas) ← `Zyft.Infrastructure` (EF Core, repositórios, JWT, BCrypt, AES-GCM, Ollama, leitura de PDF/DOCX) ← `Zyft.Api` (controllers finos, middleware de exceções, policies, Serilog). PostgreSQL 16 com migrations; SQLite com `EnsureCreated` em desenvolvimento e testes.

**Monetização.** Gratuito com limites e anúncios; Plus R$ 9,90/mês ou R$ 99/ano; pacote R$ 4,90; recompensado +3, até 3 vezes no mês. Pagamento simulado.

## 2. A sua área a fundo

### 2.1 Como uma requisição atravessa a API

1. O controller (ex.: `src/Zyft.Api/Controllers/FormsController.cs`) recebe o DTO e chama `IMediator.Send`. Nenhum controller usa o `DbContext`.
2. `ValidationBehavior` (`src/Zyft.Application/Common/Behaviors/`) roda o validator do FluentValidation. Erro: 400 com `errors` por campo.
3. O handler aplica a regra usando portas (`src/Zyft.Application/Common/Interfaces/Ports.cs`: `IRepository<T>`, `IProfileRepository`, `IUnitOfWork`, `ILlmClient`, `ICurrentUser`, `IClock`...).
4. Regra de domínio violada lança `DomainException` e vira 422 com `code` estável. Exceções de aplicação (`src/Zyft.Application/Common/Exceptions/AppExceptions.cs`) viram 401, 403, 404, 409 etc. no `ExceptionHandlingMiddleware`.
5. Resposta em JSON camelCase, enums como nome, nulos omitidos.

**Por que CQRS com MediatR?** Cada caso de uso fica isolado e testável com Moq; validação e log entram como comportamentos do pipeline, sem repetir código. É também exigência da disciplina (ADR-0017).

### 2.2 Autenticação e autorização

- BCrypt (fator 11); login com tempo constante para e-mail inexistente; 10 tentativas por minuto por IP.
- JWT HS256 de 15 minutos com `sub`, `email`, `scope` (`web` ou `extension`) e `device_id`. Refresh opaco de 7 dias, só o SHA-256 no banco, rotação a cada uso; reuso de um refresh já trocado revoga a cadeia.
- Policies `WebUser` e `ExtensionOrWeb`. A extensão não acessa exportação nem o perfil inteiro.
- Pareamento: `POST /extension/pairing-codes` (web) e `POST /extension/pair` (extensão), código de 6 dígitos, 10 minutos, uso único.

### 2.3 O que a v2 acrescenta no backend

| RF | Endpoints (base `/api/v1`) | Onde estudar |
|---|---|---|
| RF-23 | `GET /onboarding`, `PUT /onboarding` (só progresso: `answered`, `skipped`, `lastStep`, `completed`) | `src/Zyft.Application/Onboarding/`, `src/Zyft.Domain/Onboarding/OnboardingProgress.cs` |
| RF-24 | `GET /catalog/areas`; `PUT /profile` com `workArea`, `professionalRegistration`, `salaryType` | `src/Zyft.Application/Catalog/AreaCatalog.cs`, `src/Zyft.Domain/Profiles/Profile.cs` |
| RF-25 | `GET/POST /profile/custom-fields`, `PUT/DELETE /profile/custom-fields/{id}`; label ≤ 80, value ≤ 500; 422 `profile.custom_field_sensitive` | `src/Zyft.Application/Profiles/CustomFieldFeatures.cs`, `src/Zyft.Domain/Profiles/CustomField.cs` |
| RF-26 | `GET /billing`, `POST /billing/subscribe {period}`, `POST /billing/cancel`; 402 `plan.limit_reached` com `resource`, `used` e `limit` no topo do ProblemDetails e também em `extensions` | `BillingController`, `src/Zyft.Application/Billing/` (`BillingFeatures.cs`, `UsageMeter.cs`), `src/Zyft.Domain/Billing/` |
| RF-27 | `POST /billing/packs` (responde 201; +15 assistidas, +10 IA, 60 dias; exige `WebUser`, extensão recebe 403 como em assinar e cancelar). O pacote cobre só candidaturas assistidas e IA | idem |
| RF-28 | `POST /ads/rewarded/claim` (+3 assistidas, máx 3 no mês; 409 `ads.rewarded_exhausted`; para Plus, 409 `ads.rewarded_unavailable`) | `AdsController`, `src/Zyft.Application/Ads/` |
| RF-29 | `GET /ads?placement=&topic=` (200 ou 204; sem `placement` = 400; placement desconhecido = 204; `ext.job.contextual` só responde se o `topic` casar com algum anúncio), `POST /ads/{id}/hide {scope}`, `POST /ads/{id}/click` | `src/Zyft.Application/Ads/AdSelector.cs`, `AdCatalog.cs` |

Limites do Gratuito por mês: candidaturas assistidas 10 (`POST /forms/mappings`, contada uma vez por origem e caminho da página), sugestões de IA 5 (`POST /forms/open-answers`), análises de vaga 10 (`POST /jobs/analyze`), importações 2 (`POST /resume-imports`). Fora do mês: banco de respostas até 15 (`POST /answers` devolve 402 na 16ª) e 1 navegador (`POST /extension/pair` devolve 402 no segundo; a extensão libera a vaga ao se desconectar com `DELETE /extension/devices/current`, que usa o `device_id` do token e devolve 404 para token web). Plus: sem limite (`null`) e 3 navegadores.

Ordem de consumo (RNF-16): primeiro o limite do mês, depois o bônus recompensado, depois os pacotes (o que vence antes primeiro).

Novos tipos de auditoria, sem conteúdo pessoal: `plan.changed`, `pack.purchased`, `ad.hidden`, `rewarded.claimed`, `onboarding.completed`, `custom_field.changed`.

Controllers novos da v2: `BillingController`, `AdsController`, `CatalogController` e `OnboardingController`; as rotas de campos personalizados ficam no `ProfileController`. Com eles, a API passou de 61 para 76 operações em `/api/v1`; a rota `DELETE /extension/devices/current` (no `ExtensionController`) levou a 77. O 402 está declarado com `ProducesResponseType` no Swagger em `forms/mappings`, `forms/open-answers`, `jobs/analyze`, `resume-imports`, `POST /answers` e `extension/pair`.

O catálogo de anúncios **não é tabela**: é uma lista estática no código (`AdCatalog`, 16 anúncios fictícios com ids de texto como `demo-tec-cloud` e `demo: true` na resposta). No banco ficam só as ocultações (`AdHides`) e os contadores agregados (`AdStats`).

### 2.4 Banco de dados

17 tabelas na Entrega 3 (`Users`, `RefreshTokens`, `Consents`, `Profiles`, `Experiences`, `Educations`, `Courses`, `Skills`, `Languages`, `ApprovedAnswers`, `SensitiveData`, `ResumeImports`, `ExtensionDevices`, `PairingCodes`, `AuthorizedSites`, `Applications`, `AuditEvents`). A migration `20261021233314_V2PlanosAnunciosOnboarding` acrescenta 8 tabelas, total de 25: `CustomFields`, `OnboardingProgress`, `Subscriptions`, `MonthlyUsages` (uso do mês e bônus recompensado), `UsagePacks`, `UsageMarks` (hash de origem+caminho para não contar duas vezes), `AdHides` e `AdStats` (cliques agregados, sem `UserId`). `Profiles` ganha `WorkArea`, `ProfessionalRegistration` e `SalaryType`. Não há tabela de anúncios nem de resgates: os resgates do mês ficam em `MonthlyUsages`. Detalhes em [modelo de dados](../../03-arquitetura/modelo-de-dados.md).

Pontos que caem em prova: FKs com `ON DELETE CASCADE` a partir de `Users` (exclusão de conta, RF-08); índices únicos (`Users.Email`, `Consents(UserId, Purpose)`, `Skills(ProfileId, Name)`...); enums como texto; `Guid` gerado na aplicação; `CreatedAt`/`UpdatedAt` em UTC no `SaveChangesAsync`. A tabela de cliques de anúncio **não** tem `UserId` (RN-09).

### 2.5 Testes

| Suíte | Pasta | O que cobre |
|---|---|---|
| Unidade | `tests/Zyft.UnitTests/` (`Domain/`, `Forms/`, `Handlers/`, `Infrastructure/`, `Jobs/`) | Regras, de-para, análise de vaga, handlers, JWT, BCrypt, AES |
| Integração | `tests/Zyft.IntegrationTests/` | HTTP real com `WebApplicationFactory` e SQLite em memória |
| Aceitação | `tests/Zyft.AcceptanceTests/Features/RegrasInegociaveis.feature` | Regras inegociáveis em Gherkin (Reqnroll) |

Na Entrega 3: 157 testes (90, 55 e 12). Hoje: **263 testes, todos passando** (160 de unidade, 91 de integração, 12 de aceitação; os 7 mais novos são `AdNetworkTests`, da rede de anúncios simulada, ADR-0021). Na v2 eram 252 (156, 84 e 12; a primeira contagem da v2 foi 223, depois entraram 4 casos de regras de LinkedIn e GitHub e 3 testes de integração, e mais 22 nas correções da gravação do tour, em `tests/Zyft.UnitTests/Imports/ResumeImportFixesTests.cs` e `tests/Zyft.IntegrationTests/TourFixesTests.cs`); os novos estão em `tests/Zyft.UnitTests/V2/` e `tests/Zyft.IntegrationTests/V2Tests.cs`. Testes negativos novos da v2: TN-11 a TN-15 (anúncio em placement proibido ou para Plus, PII em anúncio, rótulo sensível, limite que não bloqueia exportar e excluir, anúncio no painel "Ler formulário"). TN-14 agora tem teste (`Limite_atingido_nunca_bloqueia_exportar_nem_excluir_os_dados`) e o teste de logs sem PII cobre `/ads`, `/billing`, `/onboarding` e campos personalizados (`Logs_das_rotas_v2_nao_contem_dados_pessoais`). Lacunas que a banca pode achar: não há cenário de aceitação novo da v2 (os 12 são da Entrega 3), os evals de IA ainda não existem no código e o recompensado não tem confirmação de exibição pelo servidor nem rate limit dedicado. Detalhes em [estratégia de testes](../../04-qualidade/estrategia-de-testes.md).

## 3. Perguntas difíceis prováveis

1. **"Por que quatro projetos? Não é cerimônia demais?"** É. O ganho é que a regra de negócio não depende de banco, HTTP nem IA, e é testada com Moq. A disciplina pede Clean Architecture (ADR-0017).
2. **"Onde fica a regra de que a IA não escreve valor?"** No `FieldMapper` (`src/Zyft.Application/Forms/FieldMapper.cs`): a IA devolve chave; a chave é validada; o valor é lido do perfil. Há teste com IA que tenta escrever valor (`AiGuardTests`, `TrustRulesTests`).
3. **"Como o consentimento é garantido?"** `ConsentGuard` (`src/Zyft.Application/Common/ConsentGuard.cs`) no handler: sem consentimento, 403 `consent_required`, e a IA nem é chamada. Contas novas nascem com tudo desligado.
4. **"Por que 404 para recurso de outra pessoa e não 403?"** Para não revelar que o recurso existe.
5. **"Por que 402 no limite?"** O código diz o que aconteceu: falta plano ou crédito. 403 já significa consentimento ou permissão. O corpo traz `resource`, `used` e `limit` (no topo e em `extensions`) para o cliente oferecer as opções.
6. **"Como uma candidatura não conta duas vezes se a pessoa clica 'Ler formulário' de novo?"** A contagem é por pessoa, origem e caminho da página no mês (RNF-16). Guardar só o hash da origem mais o caminho evita persistir URL com identificadores.
7. **"E se um formulário de várias etapas muda o caminho a cada etapa?"** Pode contar mais de uma vez. É um risco conhecido e está registrado; o conector do portal pode normalizar o caminho.
8. **"O pagamento é real?"** Não. `POST /billing/subscribe` simula o checkout e ativa o Plus com 7 dias de trial. Nenhum dado de cartão entra (RN-13). `GET /billing` devolve `demo: true`.
9. **"Atingir o limite bloqueia a exportação?"** Não. Só as rotas contadas (`/forms/mappings`, `/forms/open-answers`, `/jobs/analyze`, `/resume-imports`), a criação de resposta acima de 15 (`POST /answers`) e o pareamento de navegador extra (`POST /extension/pair`) podem devolver 402. Perfil, exportação e exclusão nunca (RN-12, TN-14).
10. **"Como o anúncio é escolhido sem dado pessoal?"** O cliente manda `placement` e, na extensão, `topic` (texto do requisito ausente). O servidor lê a área e os cargos desejados do perfil, filtra o catálogo estático (`AdCatalog`), exclui os ocultos e faz rotação simples. Em `ext.job.contextual`, se o `topic` não casar com nenhum anúncio, a resposta é 204 (o painel tenta outro requisito e depois cai no banner). A resposta tem só o anúncio e um `reason` em linguagem simples.
11. **"O anunciante recebe alguma coisa?"** Nada. Os anunciantes são de demonstração e o clique é um contador agregado sem `UserId`. E o recompensado não é verificado no servidor: o próprio pedido de resgate concede +3 (até 3 vezes no mês) e a rota não tem rate limit próprio. É aceitável com pagamento simulado; antes de anúncio real, o servidor precisa de prova de exibição.
12. **"Como o sistema impede campo personalizado com dado sensível?"** O handler compara o rótulo com uma lista de termos (raça, religião, saúde, deficiência, gênero, orientação, CPF, RG, senha) e devolve 422 `profile.custom_field_sensitive`. É uma lista, não IA: pode ter falso negativo, e por isso a interface também orienta.
13. **"O ADR-0012 dizia registro sem status e a API tem `PATCH /applications/{id}` com status."** Divergência conhecida, ADR em Proposta. Precisa de decisão humana registrada.
14. **"Por que SQLite usa `EnsureCreated` e não migration?"** As migrations são do PostgreSQL (`uuid`, `timestamptz`). Manter um segundo conjunto só para desenvolvimento dobraria a manutenção. O CI confere se o modelo e a migration estão em dia.
15. **"`Users.DeletedAt` existe e não é usado. Por quê?"** Pendência D-05: a exclusão é física em cascata. Falta decidir entre remover a coluna ou documentar o uso futuro.
16. **"Como provam que não há PII nos logs?"** Teste de integração procura e-mail, senha, nome, telefone e token nos logs (`PlatformTests`). Serilog registra só método, caminho sem query, status e duração.
17. **"O refresh token pode ser roubado?"** Só o hash fica no banco; ele roda a cada uso; reuso de um antigo revoga a cadeia inteira; revogar o dispositivo derruba access e refresh na hora.
18. **"Por que o catálogo de áreas é estático e não uma tabela?"** É conteúdo de produto, igual para todos, e muda com deploy. Uma tabela exigiria tela de administração que o MVP 1 não tem (ADR-0019).
19. **"E a verificação de e-mail e o login com Google?"** Pendência D-08, prevista para a Entrega 4.
20. **"O que acontece com os dados de plano e anúncio quando a conta é excluída?"** Tudo que tem `UserId` cai em cascata. Os contadores agregados de clique não têm `UserId` e por isso não identificam ninguém.

## 4. Onde está cada coisa

| Tema | Onde |
|---|---|
| Composição e middleware | `src/Zyft.Api/Program.cs`, `src/Zyft.Api/Infrastructure/` |
| Controllers | `src/Zyft.Api/Controllers/` |
| Casos de uso | `src/Zyft.Application/<Módulo>/` |
| Pipeline (validação e log) | `src/Zyft.Application/Common/Behaviors/` |
| Entidades | `src/Zyft.Domain/` |
| DbContext, configurações, migrations, seed | `src/Zyft.Infrastructure/Persistence/` |
| JWT, BCrypt, AES | `src/Zyft.Infrastructure/Security/` |
| IA | `src/Zyft.Infrastructure/Ai/OllamaLlmClient.cs` |
| Configuração | `src/Zyft.Api/appsettings.json`, `appsettings.Development.json` |
| Testes | `tests/` |
| Docker e CI | `docker-compose.yml`, `src/Zyft.Api/Dockerfile`, `.github/workflows/ci.yml` |
| Docs | [backend](../../03-arquitetura/backend.md), [API](../../03-arquitetura/api.md), [modelo de dados](../../03-arquitetura/modelo-de-dados.md), [critérios de aceite](../../04-qualidade/criterios-de-aceite-mvp1.md), [runbook](../../06-operacao/runbook-local.md), [relatório do backend da E3](../../entregas/entrega-3-relatorio-backend.md) |

## 5. Exercício de 30 minutos

1. (5 min) `dotnet build` e `dotnet test`. Anote o total.
2. (5 min) `dotnet run --project src/Zyft.Api`, Swagger em `/swagger`, login como Rafael (`demo@zyft.local` / `Zyft@2026`), botão Authorize com o `accessToken`.
3. (5 min) `GET /billing`: confira `plan`, `usage`, `prices` e `demo: true`.
4. (10 min) Chame `POST /jobs/analyze` (com o consentimento de análise de vaga ligado) até passar de 10 no mês (o Rafael já começa com 4 usadas) e veja o 402 `plan.limit_reached`. Depois `POST /ads/rewarded/claim` e confirme que o bônus de candidaturas assistidas aparece no `GET /billing` (o recompensado não libera análises). Em seguida, confirme que `GET /me/export` continua funcionando.
5. (5 min) Encontre o teste que garante "IA não escreve valor" e o que garante "logs sem dados pessoais". Leia cada um e explique em voz alta o que ele afirma.
