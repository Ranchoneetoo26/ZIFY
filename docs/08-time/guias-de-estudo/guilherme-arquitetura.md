# Guia de estudo: arquitetura de software, IA e privacidade

| Campo | Valor |
|---|---|
| Integrante | Guilherme Brogio Macedo da Silva |
| Papel | Arquiteto de software e dev full stack; acumula Arquiteto de IA e Privacidade e Segurança |
| Versão | 1.1 (produto v2, sincronizado com a implementação) |
| Data | 21/10/2026 |
| Relacionados | [Papéis e RACI](../papeis-e-raci.md), [DAT](../../03-arquitetura/dat.md), [ADRs](../../03-arquitetura/adr/README.md), [API](../../03-arquitetura/api.md), [Segurança](../../03-arquitetura/seguranca.md), [IA](../../03-arquitetura/ia.md) |

Este é um guia de estudo. Ele diz o que você precisa dominar para responder à banca; não registra autoria de código nem de documento.

## 1. O projeto inteiro em uma página

**Problema.** Redigitar os mesmos dados em cada site de vagas: média 4,40 de 5, 80% de notas 4–5 na pesquisa v2. Preencher experiências e formação item a item é a etapa que mais toma tempo (67%).

**Pesquisa.** 15 respostas válidas (uma linha copiada da v1 foi excluída). Viés forte: 60% tecnologia. Confiança num assistente que preenche: 3,4 de 5. Receios: CPF e RG 67%, endereço 40%, respostas abertas por IA 33%. Barreiras: preço 47%, privacidade 40%, poucos sites 40%. Perfil único e importar currículo: 87% acham importante. Ver [resultados-v2.md](../../05-pesquisa/resultados-v2.md).

**MVP 1 (v2).** Perfil Mestre (onboarding em passos ou importação de currículo), extensão MV3 que lê o formulário e preenche só o aprovado, IA presa a fatos. Novidades da v2: onboarding adiável (RF-23, ADR-0018), área dinâmica e campos personalizados (RF-24, RF-25, ADR-0019), planos, limites e anúncios (RF-26 a RF-29, ADR-0020).

**Regras inegociáveis.** RN-01 nunca enviar; RN-02/RN-03 IA só com fatos; RNF-01/RNF-13 sem senha e sem requisição ao portal; RN-04 sensíveis segregados; RNF-09 sem PII em log; RN-06 sem promessa; RN-07 escopo. Na v2, RN-08 a RN-11 (anúncios fora do formulário, sem PII, rotulados, um por tela), RN-12 (privacidade nunca paga), RN-13 (pagamento simulado), RN-15 (campo personalizado sem dado sensível).

**Personas.** Rafael (primária), Bia (secundária), Beatriz de saúde (hipótese), antipersona da candidatura em massa. Ver [personas](../../01-produto/personas.md).

**Arquitetura em 5 linhas.**
1. `Zyft.Domain` (entidades e regras puras) ← `Zyft.Application` (CQRS com MediatR, FluentValidation, portas) ← `Zyft.Infrastructure` (EF Core, JWT, BCrypt, AES-GCM, Ollama, PdfPig/OpenXml) ← `Zyft.Api` (controllers finos, ProblemDetails, Serilog, rate limiting).
2. PostgreSQL 16 em Docker; SQLite em desenvolvimento e nos testes de integração.
3. Web em HTML/CSS/JS sem build (`web/`), servida pela API em `/`.
4. Extensão MV3 (`extension/`, versão 0.4.0): só o service worker fala com a API e guarda tokens, e ele só aceita mensagens das páginas da própria extensão.
5. IA atrás da porta `ILlmClient`: aponta chave, nunca escreve valor; sem IA, as regras seguem.

**Monetização.** Gratuito com limites e anúncios, Plus R$ 9,90/mês ou R$ 99/ano, pacote R$ 4,90, recompensado +3 (até +9/mês). Pagamento simulado. Ver [monetização](../../01-produto/monetizacao.md).

## 2. A sua área a fundo

### 2.1 Decisões de arquitetura que você precisa defender

| ADR | Decisão | Status | Por quê, em uma frase |
|---|---|---|---|
| [0004](../../03-arquitetura/adr/0004-ia-aponta-chave-nunca-escreve-valor.md) | IA aponta a chave do perfil, nunca escreve o valor | Aceita | Um modelo de 7B erra e inventa; se ele só escolhe entre chaves existentes, o pior caso é um campo errado visível, nunca um fato falso |
| [0006](../../03-arquitetura/adr/0006-regras-deterministicas-antes-da-ia.md) | Regras antes da IA | Aceita | Campos objetivos comuns resolvem em milissegundos e sem risco; a IA só vê o que sobrou |
| [0009](../../03-arquitetura/adr/0009-nunca-enviar-confirmacao-explicita.md) | Nunca enviar | Aceita | Termos dos portais, qualidade da candidatura e a antipersona |
| [0010](../../03-arquitetura/adr/0010-sem-credenciais-sem-contorno-de-seguranca-campos-bloqueados.md) | Sem credenciais, campos bloqueados | Aceita | Senha, CAPTCHA, anexo e declaração exigem a pessoa |
| [0013](../../03-arquitetura/adr/0013-evidencia-por-linha-numerada-na-analise-de-vaga.md) | Evidência por linha numerada | Aceita | A interface mostra a linha real do perfil, não um texto da IA |
| [0014](../../03-arquitetura/adr/0014-dados-sensiveis-segregados-e-criptografados.md) | Sensíveis segregados e cifrados | Aceita | LGPD art. 11 e o receio medido na pesquisa |
| [0012](../../03-arquitetura/adr/0012-registro-minimo-de-conclusao-no-mvp1.md) | Registro mínimo de conclusão | Proposta | A implementação foi além (status e anotações); ver a nota de divergência no próprio ADR e a pergunta 14 |
| [0017](../../03-arquitetura/adr/0017-backend-em-csharp-dotnet-clean-architecture.md) | .NET 8 com Clean Architecture | Proposta | Exigência da disciplina; substitui o 0003 (Node) |
| [0018](../../03-arquitetura/adr/0018-onboarding-em-passos-com-perguntas-adiaveis.md) | Onboarding em passos, perguntas adiáveis | Proposta | O progresso fica em `/onboarding`; as respostas vão para o perfil pelos endpoints que já existiam |
| [0019](../../03-arquitetura/adr/0019-area-profissional-dinamica-e-campos-personalizados.md) | Área dinâmica e campos personalizados | Proposta | Catálogo estático por área, sem tabela nova; campos próprios com chave `custom:<id>` |
| [0020](../../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md) | Anúncios e planos no MVP 1 | Proposta | Decisão de escopo (RN-07) com alternativas registradas |

### 2.2 O pipeline do de-para (o coração do produto)

Arquivo central: `src/Zyft.Application/Forms/FieldMapper.cs` (com `FieldRules.cs`, `FormModels.cs`, `FormFeatures.cs`).

1. **Bloqueios primeiro.** Senha, CAPTCHA, anexo e declaração são classificados como bloqueados antes de qualquer IA, e nem entram no prompt. Sensível: sem consentimento, bloqueado sem valor; com consentimento, valor do cofre, sempre desmarcado.
2. **Regras determinísticas** em português e inglês, inclusive blocos repetidos alinhados pela organização.
3. **Campos personalizados (v2):** o rótulo do campo da página é comparado com o rótulo do campo personalizado; se bater, a chave é `custom:<id>`. A IA também pode apontar `custom:<id>`, porque esses campos entram nos fatos (`src/Zyft.Application/Profiles/ProfileFacts.cs`).
4. **IA** (`ILlmClient`, implementado em `src/Zyft.Infrastructure/Ai/OllamaLlmClient.cs`): recebe rótulos e a lista de chaves, devolve só a chave. Chave inexistente é descartada.
5. **Validação:** o valor sai do perfil; select só aceita opção que existe na página, senão fica ambíguo.

### 2.3 Privacidade e segurança na v2

- **Anúncios sem PII (RN-09, RNF-14).** `GET /ads` recebe só `placement` e `topic` (texto do requisito). A seleção lê área e cargos desejados no servidor. A resposta não tem dado da pessoa; o clique vira contador agregado sem `user_id` (tabela `AdStats`). O catálogo é estático no código (`AdCatalog`, 16 anúncios fictícios, ids de texto como `demo-tec-cloud`, `demo: true`); o banco guarda só ocultações e contadores. Em `ext.job.contextual`, só há anúncio se o `topic` casar com algum assunto do catálogo; o painel tenta até 3 requisitos ausentes e depois cai no banner. Código em `src/Zyft.Application/Ads/` (`AdSelector.cs`, `AdCatalog.cs`, `AdFeatures.cs`).
- **Placements proibidos (RN-08).** Onboarding, entrar, importar currículo, sensíveis, conta e privacidade e o painel "Ler formulário" não têm placement. Placement desconhecido e Plus recebem 204; sem `placement`, 400. No painel, a aba Formulário e a tela desconectada nunca mostram anúncio.
- **Campo personalizado sensível (RN-15).** Rótulos que pareçam raça, religião, saúde, deficiência, gênero, orientação, CPF, RG ou senha recebem 422 `profile.custom_field_sensitive`.
- **Contagem de uso (RNF-16).** Uma candidatura assistida conta uma vez por pessoa, origem e caminho da página no mês. Guardar um hash da origem mais o caminho evita persistir URL com identificadores (ADR-0015). Código em `src/Zyft.Application/Billing/UsageMeter.cs` e `src/Zyft.Domain/Billing/`.
- **Pagamento simulado (RN-13).** Nenhum dado de cartão entra no sistema. Sem gateway, não há PCI a cumprir; quando houver gateway real, o cartão nunca deve passar pela API do Zyft (tokenização no provedor).
- **Chrome Web Store.** A política de dados de usuário da loja restringe uso de dados para publicidade personalizada. Anúncio contextual na extensão precisa de revisão de conformidade antes de publicar na loja. Está registrado como risco no [ADR-0020](../../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md) e em [segurança](../../03-arquitetura/seguranca.md).

### 2.4 Contrato e dados da v2

Endpoints novos (base `/api/v1`, 15 operações em `BillingController`, `AdsController`, `CatalogController`, `OnboardingController` e nas rotas de campos personalizados do `ProfileController`; com a `DELETE /extension/devices/current` que veio depois, o total é de 77 operações): `/profile/custom-fields`, `/catalog/areas`, `/onboarding`, `/billing`, `/billing/subscribe`, `/billing/cancel`, `/billing/packs` (201, só com token web), `/ads`, `/ads/{id}/hide`, `/ads/{id}/click`, `/ads/rewarded/claim`. Limite excedido: **402** `plan.limit_reached` com `resource`, `used` e `limit` no topo e em `extensions`; vale também para a 16ª resposta aprovada e o 2º navegador no gratuito. Recompensado: 409 `ads.rewarded_exhausted` (acabou no mês) ou `ads.rewarded_unavailable` (Plus).

Banco: migration `20261021233314_V2PlanosAnunciosOnboarding` (PostgreSQL), 8 tabelas novas e 25 no total (`CustomFields`, `OnboardingProgress`, `Subscriptions`, `MonthlyUsages`, `UsagePacks`, `UsageMarks`, `AdHides`, `AdStats`) e 3 colunas em `Profiles`. O SQLite usa `EnsureCreated`. Detalhes em [modelo de dados](../../03-arquitetura/modelo-de-dados.md).

## 3. Perguntas difíceis prováveis

1. **"Por que .NET se o protótipo era Node?"** A disciplina exige C#/.NET com Clean Architecture, CQRS, EF Core e testes com xUnit e Reqnroll (ADR-0017). O protótipo ficou como referência de comportamento.
2. **"O ADR-0008 dizia JSONB e vocês fizeram relacional. Qual vale?"** O ADR-0017 ajusta o 0008: perfil relacional, uma tabela por seção, porque a disciplina avalia entidades e relacionamentos. O custo é migration a cada campo novo. Campos personalizados (ADR-0019) reduzem esse custo para o que é específico de cada área.
3. **"Os ADRs estão em Proposta. Então nada foi decidido?"** Estão implementados como proposta e só humanos aceitam. É honesto: a ratificação está pendente. Os de regra de confiança (0004, 0009, 0010, 0013, 0014) estão Aceitos.
4. **"O ADR-0007 falava em PKCE. Por que código de 6 dígitos?"** O ADR-0017 adotou o fluxo alternativo do 0007: código gerado na web, uso único, 10 minutos. PKCE com `launchWebAuthFlow` fica para depois do MVP 1 acadêmico. O risco do código curto é mitigado por expiração, uso único e limite de tentativas por IP.
5. **"Como garantem que a IA não inventa?"** Estruturalmente: ela só devolve chave, validada contra as chaves existentes; o valor sai do perfil. Há testes com IA "maliciosa" que tenta escrever valor ou apontar senha (`tests/Zyft.IntegrationTests/TrustRulesTests.cs`, cenários em `tests/Zyft.AcceptanceTests/Features/RegrasInegociaveis.feature`).
6. **"E prompt injection na vaga?"** O texto da página é dado. A saída da IA é JSON com schema e não vira ação. Evidência inventada vira "não encontrado".
7. **"Campo personalizado entra na IA. Isso não vaza dado?"** Entra como fato aprovado pela própria pessoa, como qualquer item do perfil. Rótulo sensível é recusado (RN-15), então dado sensível não chega à IA por esse caminho.
8. **"Anúncio com base no requisito da vaga não é rastreamento?"** O tema é o texto do requisito ausente, enviado pela própria extensão na hora; não guardamos histórico de navegação, não montamos perfil de interesse e nada vai para o anunciante. Mesmo assim, a loja pode classificar como uso de dados para publicidade; por isso a revisão de conformidade está registrada antes de publicar.
9. **"Por que 402 e não 403?"** 403 já significa consentimento ou permissão. 402 (Payment Required) diz exatamente o caso: há um limite de plano. Os clientes leem `resource` (no topo ou em `extensions`) para oferecer recompensado, pacote ou Plus.
10. **"Atingiu o limite no meio do formulário. E aí?"** O Zyft para de propor, nada é apagado e a pessoa continua no portal normalmente. Editar perfil, exportar e excluir nunca são bloqueados (RN-12).
11. **"Se o pagamento é simulado, o que exatamente vocês testam?"** A regra de negócio: limites, ordem de consumo (mês, depois bônus, depois pacote), trial, cancelamento e auditoria (`plan.changed`, `pack.purchased`). O gateway é uma porta que pode ser plugada depois.
12. **"Onde estão os dados sensíveis e como são protegidos?"** Tabela `SensitiveData`, AES-256-GCM com nonce aleatório, fora do perfil achatado e fora da IA. Desligar o consentimento apaga os dados.
13. **"Logs sem PII: como provam?"** Teste de integração procura e-mail, senha, nome, telefone e token nos logs capturados (`PlatformTests`). Serilog registra só método, caminho sem query, status e duração.
14. **"O ADR-0012 recomendava registro sem status, e vocês têm status e anotações."** Divergência conhecida, registrada numa nota no próprio ADR (que segue em Proposta). A implementação usa o enum `ApplicationStatus` (10 valores), anotações e `PATCH /applications/{id}`, e grava ao toque em "Concluí a candidatura", sem consentimento próprio. A nota propõe dois caminhos: aceitar o status com justificativa ("lembrar onde se candidatou" teve 3,00 e 47% de notas 4–5) ou esconder status e anotações até o MVP 2. Está pendente e não vamos fingir que não está.
15. **"Por que a extensão não lê iframe nem shadow DOM?"** Limitação conhecida, documentada em [extensão](../../03-arquitetura/extensao.md). Ler iframes de terceiros amplia a superfície de ataque; o critério de escolha de portal leva isso em conta.
16. **"Monetização no MVP 1 não fere a RN-07?"** A RN-07 exige decisão explícita registrada, e é isso que o ADR-0020 faz, com alternativas (sem monetização, só assinatura, só anúncios, freemium completo, comissão ou venda de dados rejeitada).
17. **"Quantos testes vocês têm?"** 263, todos passando: 160 de unidade, 91 de integração e 12 cenários de aceitação (os 7 mais novos, `AdNetworkTests`, cobrem a rede de anúncios simulada). Na Entrega 3 eram 157 (90, 55 e 12); a v2 acrescentou 73 (66 na primeira contagem, que é a dos vídeos, mais 7 depois: regras de LinkedIn e GitHub, TN-14, desconexão da extensão e logs das rotas v2; e mais 22 nas correções da gravação do tour: importação, erros legíveis, teste grátis e rascunho vazio).
18. **"Como a área muda as sugestões sem banco?"** O catálogo é estático na camada Application (`src/Zyft.Application/Catalog/AreaCatalog.cs`). Mudar uma lista de sugestões não muda dado de ninguém; o que a pessoa escolhe vai para o perfil dela.
19. **"E se a IA estiver fora do ar?"** `/health` fica Degraded, o de-para usa só regras, sugestão de texto devolve `unknown` sem valor. Nada é preenchido por suposição (RNF-12).
20. **"Qual é o maior risco técnico hoje?"** Dependência de layout dos portais (o conector quebra quando o site muda) e, na v2, a conformidade de anúncios com a loja de extensões.
21. **"Vocês colocaram Google AdSense dentro de uma extensão? E a LGPD?"** Não está ativo: é **simulado** (ADR-0021, Proposta). A API devolve `format: "network"` e o cliente desenha só um `<ins class="adsbygoogle">` com "Anúncio do Google (simulado)"; nenhum script, cookie ou dado vai para o Google, e a extensão nunca carrega código remoto (o MV3 proíbe). Para produção: (a) as políticas do AdSense não permitem anúncios dentro de software ou extensões de navegador (confirmar na política vigente), então **a extensão fica só com anúncios diretos** e a rede só na web; (b) a LGPD exige **consentimento prévio** para cookies de publicidade, com um CMP e "Recusar" tão fácil quanto "Aceitar", e sem consentimento o script não carrega; (c) só anúncios não personalizados e nada do perfil vai para a rede (RN-09); (d) a CSP teria de liberar domínios do Google e o script pesa na tela (RNF-15). Por que propor então? Sem equipe comercial, o inventário fica vazio; a rede preenche, e o anúncio direto continua com prioridade fora dos banners.

## 4. Onde está cada coisa

| Tema | Onde |
|---|---|
| Camadas e composição | `src/Zyft.Api/Program.cs`, `src/Zyft.Application/DependencyInjection.cs`, `src/Zyft.Infrastructure/DependencyInjection.cs` |
| Portas | `src/Zyft.Application/Common/Interfaces/Ports.cs` |
| De-para | `src/Zyft.Application/Forms/` |
| Análise de vaga | `src/Zyft.Application/Jobs/JobAnalyzer.cs` |
| Texto livre | `src/Zyft.Application/Writing/AnswerWriter.cs` |
| Fatos do perfil para a IA | `src/Zyft.Application/Profiles/ProfileFacts.cs` |
| Consentimento | `src/Zyft.Application/Common/ConsentGuard.cs` |
| Catálogo por área (v2) | `src/Zyft.Application/Catalog/AreaCatalog.cs` |
| Planos e uso (v2) | `src/Zyft.Application/Billing/`, `src/Zyft.Domain/Billing/` |
| Anúncios (v2) | `src/Zyft.Application/Ads/`, `src/Zyft.Domain/Ads/` |
| Onboarding (v2) | `src/Zyft.Application/Onboarding/`, `src/Zyft.Domain/Onboarding/` |
| Campos personalizados (v2) | `src/Zyft.Application/Profiles/CustomFieldFeatures.cs`, `src/Zyft.Domain/Profiles/CustomField.cs` |
| IA, criptografia, JWT | `src/Zyft.Infrastructure/Ai/`, `src/Zyft.Infrastructure/Security/` |
| Banco e seed | `src/Zyft.Infrastructure/Persistence/` (`ZyftDbContext.cs`, `DatabaseInitializer.cs`, `Migrations/`) |
| Erros | `src/Zyft.Api/Infrastructure/ExceptionHandlingMiddleware.cs`, `Problems.cs` |
| Testes de confiança | `tests/Zyft.IntegrationTests/TrustRulesTests.cs`, `tests/Zyft.AcceptanceTests/Features/RegrasInegociaveis.feature` |
| CI | `.github/workflows/ci.yml` |
| Docs | [DAT](../../03-arquitetura/dat.md), [API](../../03-arquitetura/api.md), [backend](../../03-arquitetura/backend.md), [modelo de dados](../../03-arquitetura/modelo-de-dados.md), [segurança](../../03-arquitetura/seguranca.md), [IA](../../03-arquitetura/ia.md), [design system](../../03-arquitetura/design-system.md) |


## 5. Exercício de 30 minutos

1. (5 min) `dotnet test` e anote quantos testes passam.
2. (10 min) Rode a API, entre no Swagger como Rafael, chame `POST /forms/mappings` com um campo "Senha" (`type=password`), um campo "Registro no conselho" e um campo personalizado cujo rótulo bata com o seu. Confirme: senha bloqueada, campo personalizado com chave `custom:<id>`.
3. (5 min) Tente criar um campo personalizado com rótulo "Religião" e confirme o 422 `profile.custom_field_sensitive`.
4. (5 min) Chame `GET /ads?placement=web.profile.rail` e leia a resposta procurando qualquer dado pessoal. Depois assine o Plus (`POST /billing/subscribe`) e confirme o 204.
5. (5 min) Leia o ADR-0020 inteiro e diga em voz alta as alternativas consideradas e por que cada uma perdeu.
