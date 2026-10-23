# Contrato da API v1

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Contrato REST da API v1 |
| Versão | 2.3 |
| Status | Em revisão. Todas as rotas descritas aqui estão implementadas e cobertas por testes de integração: **77 operações** em `/api/v1`, mais `GET /health`. As rotas da versão 2 seguem os [ADR-0018](adr/0018-onboarding-em-passos-com-perguntas-adiaveis.md), [ADR-0019](adr/0019-area-profissional-dinamica-e-campos-personalizados.md) e [ADR-0020](adr/0020-monetizacao-com-anuncios-e-planos.md), todos ainda propostos. |
| Dono | Arquitetura de software, com o time de backend |
| Documentos relacionados | [Backend](backend.md), [Modelo de dados](modelo-de-dados.md), [Segurança](seguranca.md), [Extensão](extensao.md), [DRP](../01-produto/drp.md), [Monetização](../01-produto/monetizacao.md), [Entrega 2 §3](../entregas/entrega-2-projeto-e-arquitetura.md#3-definição-da-api) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | Contrato-alvo em TypeScript (rotas `/v1/...`, schemas Zod, PKCE). Substituído junto com o [ADR-0003](adr/0003-monorepo-typescript-node.md). |
| 2.0 | 21/10/2026 | Reescrito para a API real em .NET 8 ([ADR-0017](adr/0017-backend-em-csharp-dotnet-clean-architecture.md)): base `/api/v1`, JWT com `scope`, pareamento por código. Acrescenta as rotas da versão 2: catálogo por área, onboarding, campos personalizados, planos e uso, anúncios. |
| 2.1 | 21/10/2026 | Conferido com o código implementado: 76 operações (eram 61 na Entrega 3; +15 da v2); políticas reais de cada rota nova; `POST /billing/packs` responde 201; 402 com `resource`, `used` e `limit` no topo e em `extensions`; 409 `ads.rewarded_unavailable` no Plus; `GET /ads` sem `placement` responde 400; catálogo estático de 16 anúncios com `demo: true`; passo de onboarding e tipo de pretensão inválidos respondem 400 (validação), não 422. |
| 2.2 | 21/10/2026 | 77 operações: nova `DELETE /extension/devices/current` (a extensão se desconecta e libera a vaga de navegador). `POST /billing/packs` passa a exigir `WebUser` (extensão recebe 403). 402 documentado no Swagger (`ProducesResponseType`) em `forms/mappings`, `forms/open-answers`, `jobs/analyze`, `resume-imports`, `POST /answers` e `extension/pair`. |
| 2.3 | 21/10/2026 | `GET /billing` (e as respostas de assinar, cancelar, pacote e recompensado) ganham `trialAvailable` e `trialDays`. Erro de formato (model binding) aponta o campo e não a chave do parâmetro (`cmd`). `type` de curso ausente ou nulo vira `Course`. Proposta de importação marca itens incompletos em `missingFields` (sem inventar valor) e o `apply` responde 400 com mensagem que nomeia o item. `open-answers` nunca devolve `value` vazio: texto vazio da IA vira `kind: "missing"` com nota padrão. |
| 2.4 | 22/10/2026 | `GET /ads` pode devolver `format: "network"` (Google AdSense **simulado**, [ADR-0021](adr/0021-rede-de-anuncios-google-adsense.md)) com o objeto `network`; id `adsense-<slot>` aceito em `hide` (30 dias) e `click` (não contado). Sem rota nova: continuam 77 operações. |

## 1. Convenções

- **Base:** `/api/v1` (em desenvolvimento, `http://localhost:5080/api/v1`). A única rota fora dela é `GET /health`.
- **Formato:** JSON em camelCase, `application/json; charset=utf-8`. O envio de currículo usa `multipart/form-data`. Enums saem como **nome** (`"Ready"`, `"AiText"`); nulos são omitidos, salvo onde o contrato pede `null` explícito (`limit`, `linkLabel`, `registration`). Datas saem como `AAAA-MM-DD` ou ISO 8601 UTC; a entrada aceita `AAAA-MM`, `MM/AAAA` e `DD/MM/AAAA`.
- **Erros:** sempre ProblemDetails (RFC 9457) em português, com `type` (`https://httpstatuses.io/<status>`), `title`, `status`, `detail`, `instance`, `traceId` e um `code` estável (seção 5). Erros de validação trazem `errors` por campo (seção 5.1).
- **Fonte da verdade:** os controllers em `src/Zyft.Api/Controllers` e os DTOs em `src/Zyft.Application`. O Swagger (`/swagger`) é gerado deles. Este documento é a versão legível; o detalhamento endpoint a endpoint das rotas da Entrega 3 está na [Entrega 2 §3.3](../entregas/entrega-2-projeto-e-arquitetura.md#33-detalhamento-por-endpoint).

### 1.1 Autenticação e autorização

| Cliente | Mecanismo | Como obtém |
|---|---|---|
| Plataforma web | `Authorization: Bearer <access token>` com `scope=web` | `POST /auth/register` ou `/auth/login` |
| Extensão | `Authorization: Bearer <access token>` com `scope=extension` e `device_id`, mais o header `X-Zyft-Extension-Version` | `POST /extension/pair` com o código de 6 dígitos gerado na web |

- Access token JWT HS256 de 15 minutos (`sub`, `email`, `scope`, `device_id`). Refresh token opaco de 7 dias, guardado só como SHA-256, com **rotação** a cada uso; reapresentar um refresh já trocado revoga a cadeia inteira.
- Policies: **`WebUser`** (só `scope=web`) e **`ExtensionOrWeb`**. Token de extensão revogada é recusado na hora.
- O fluxo Authorization Code + PKCE do [ADR-0007](adr/0007-autenticacao-e-pareamento-da-extensao.md) ficou para depois do MVP 1 acadêmico; o pareamento é o código curto.
- CORS: origens de `Cors:AllowedOrigins` e `chrome-extension://<id>` (restrito a `Cors:ExtensionIds` em Production).
- Rate limiting: por IP em `register`, `login` e `extension/pair` (10/min); por pessoa em `/forms/*`, `/jobs/analyze` e `POST /resume-imports` (60/min). Excesso: 429 com `Retry-After`.

## 2. Mapa de rotas

Legenda: **W** = só plataforma web (`WebUser`); **E/W** = extensão ou web (`ExtensionOrWeb`); **público** = sem token. "v2" marca rota nova ou alterada na versão 2 do produto.

### 2.1 Conta, autenticação e consentimentos

| Método e rota | Cliente | Requisito |
|---|---|---|
| `POST /auth/register` | público | RF-01 |
| `POST /auth/login` | público | RF-01 |
| `POST /auth/refresh` | público (refresh token) | RF-01 |
| `POST /auth/logout` | W, E | RF-01 |
| `GET /me` | E/W | RF-01 |
| `DELETE /me` | W | RF-08 |
| `GET /me/export` | W | RF-08 |
| `GET /consents` | W | RF-07 |
| `PUT /consents/{purpose}` | W | RF-07 |

### 2.2 Perfil Mestre

| Método e rota | Cliente | Requisito |
|---|---|---|
| `GET /profile` (v2: inclui `workArea`, `professionalRegistration`, `salaryType` e `customFields`) | W | RF-02, RF-24, RF-25 |
| `PUT /profile` (v2: aceita `workArea`, `professionalRegistration`, `salaryType`) | W | RF-02, RF-24 |
| `GET /profile/missing` | W | RF-05 |
| `GET`, `POST /profile/experiences`; `PUT`, `DELETE /profile/experiences/{id}` | W | RF-02 |
| `GET`, `POST /profile/educations`; `PUT`, `DELETE /profile/educations/{id}` | W | RF-02 |
| `GET`, `POST /profile/courses`; `PUT`, `DELETE /profile/courses/{id}` | W | RF-02 |
| `GET`, `POST /profile/skills`; `PUT`, `DELETE /profile/skills/{id}` | W | RF-02 |
| `GET`, `POST /profile/languages`; `PUT`, `DELETE /profile/languages/{id}` | W | RF-02 |
| `GET`, `POST /profile/custom-fields`; `PUT`, `DELETE /profile/custom-fields/{id}` (v2) | W | RF-25 |
| `GET /catalog/areas` (v2) | E/W | RF-24 |
| `GET /onboarding`, `PUT /onboarding` (v2) | W | RF-23 |

### 2.3 Importação, respostas e dados sensíveis

| Método e rota | Cliente | Requisito |
|---|---|---|
| `POST /resume-imports` (v2: conta no limite `resumeImports`) | W | RF-03, RF-26 |
| `GET /resume-imports/{id}` | W | RF-03 |
| `POST /resume-imports/{id}/apply` | W | RF-04 |
| `POST /resume-imports/{id}/discard` | W | RF-04 |
| `GET /answers`, `GET /answers/{id}` | W | RF-06 |
| `POST /answers` (v2: até 15 no Gratuito) | E/W | RF-06, RF-26 |
| `PUT`, `DELETE /answers/{id}` | W | RF-06 |
| `GET`, `POST /sensitive-data`; `PUT`, `DELETE /sensitive-data/{id}` | W | RN-04 |

### 2.4 Extensão, sites e formulários

| Método e rota | Cliente | Requisito |
|---|---|---|
| `POST /extension/pairing-codes` | W | RF-09 |
| `POST /extension/pair` (v2: 1 navegador no Gratuito, 3 no Plus) | público (código) | RF-09, RF-26 |
| `GET /extension/devices` | W | RF-09 |
| `DELETE /extension/devices/{id}` | W | RF-09 |
| `DELETE /extension/devices/current` (v2: a extensão revoga o próprio dispositivo) | E/W | RF-09, RF-26 |
| `GET`, `POST /authorized-sites` | E/W | RF-10 |
| `DELETE /authorized-sites/{id}` | W | RF-10 |
| `POST /forms/mappings` (v2: conta `assistedApplications`; aceita chave `custom:<id>`) | E/W | RF-11 a RF-14, RF-18, RF-25, RF-26 |
| `POST /forms/open-answers` (v2: conta `aiSuggestions`) | E/W | RF-15, RF-26 |
| `POST /forms/completions` | E/W | RF-19 |
| `POST /jobs/analyze` (v2: conta `jobAnalyses`) | E/W | RF-17, RF-26 |

### 2.5 Candidaturas, registro e saúde

| Método e rota | Cliente | Requisito |
|---|---|---|
| `GET /applications` | E/W | RF-19 |
| `GET`, `PATCH`, `DELETE /applications/{id}` | W | RF-19 |
| `GET /audit-events` | W | RF-20 |
| `GET /health` | público | RNF-12 |

### 2.6 Planos, uso e anúncios (v2)

| Método e rota | Cliente | Requisito |
|---|---|---|
| `GET /billing` | E/W | RF-26 |
| `POST /billing/subscribe` | W | RF-26 |
| `POST /billing/cancel` | W | RF-26 |
| `POST /billing/packs` (responde 201) | W | RF-27 |
| `GET /ads?placement=<p>[&topic=<texto>]` | E/W | RF-29 |
| `POST /ads/{id}/hide` | E/W | RF-29 |
| `POST /ads/{id}/click` | E/W | RF-29 |
| `POST /ads/rewarded/claim` | E/W | RF-28 |

A extensão lê `GET /billing` para o medidor de uso e chama `POST /ads/rewarded/claim` no botão "Ganhar +3". Assinar, cancelar e comprar pacote exigem `scope=web` (`Policies.WebUser`): token de extensão recebe 403, coberto pelos testes `Extensao_pode_ler_billing_e_anuncios_mas_nao_assinar` e `Extensao_que_se_desconecta_libera_a_vaga_de_navegador_e_nao_compra_pacote`. Para pacote e Plus a extensão abre `#/planos` na web.

`DELETE /extension/devices/current` é a forma de a extensão se desconectar: a API lê o `device_id` do token, revoga o dispositivo e os refresh tokens dele, grava o evento de revogação na auditoria e responde **204**. A vaga de navegador do plano fica livre na hora (o próximo `POST /extension/pair` não recebe 402 `devices`). A policy é `ExtensionOrWeb`, mas um token web não tem `device_id` e recebe **404** ("Este acesso não é de uma extensão pareada."). Para revogar outro navegador, a plataforma usa `DELETE /extension/devices/{id}`. `GET /onboarding` e `PUT /onboarding` também são só web.

## 3. Rotas da Entrega 3 (resumo)

O comportamento destas rotas não mudou na versão 2, exceto pela contagem de uso (seção 4.5). Exemplos completos estão na Entrega 2 §3.3.

- **`POST /forms/mappings`**: recebe `{ pageUrl, fields[] }` (1 a 300 campos) e devolve **um item por campo** (`fieldId`, `kind`, `state` ∈ `Ready | Ambiguous | Missing | Blocked | Divergent`, `source` ∈ `Rule | Ai | Answer | None`, `profileKey`, `value`, `selected`, `note`) mais `stats`, `aiUsed` e `elapsedMs`. Senha, CAPTCHA, anexo, declarações e sensíveis ficam `Blocked`; só `Ready` vem marcado; divergência mantém o valor do portal por padrão (RF-16). Nada além dos campos pedidos volta (RNF-02).
- **`POST /forms/open-answers`**: `{ question, maxLength, ... }` → sugestão com `usedFacts` e `selected=false`. Exige o consentimento `AiText` (403 `consent_required`). `kind` ∈ `approved` (resposta aprovada reaproveitada, com `approvedAnswerId`) · `generated` (rascunho da IA) · `missing` · `unknown` (IA fora). **Garantia:** `value` nunca vem vazio nem só com espaços; quando não há texto, `value` é omitido e `note` explica. Se a IA devolver texto vazio (ou em outro campo), a resposta é `kind: "missing"` com `note` começando por "A IA não conseguiu escrever um rascunho com os fatos do seu perfil; escreva do seu jeito." (seguido de "O que faltou: …" quando a IA disser). Teste: `Rascunho_de_IA_vazio_volta_200_com_aviso_e_sem_texto_em_branco`.
- **`POST /resume-imports`**: a proposta (`proposal`) usa os nomes do perfil. Itens de `experiences`, `educations` e `languages` podem trazer `missingFields` (ex.: `["status"]`) quando o currículo não trouxe um campo obrigatório ou trouxe um rótulo que não corresponde a nenhuma opção; nesse caso o campo fica **ausente**, nunca adivinhado. Cursos com Certified/Certification/Certificação no nome vêm com `type: "Certification"`, e nomes próprios ficam no idioma original.
- **`POST /resume-imports/{id}/apply`**: item ainda incompleto responde **400** `validation_failed` com a chave do item e uma mensagem que o nomeia, por exemplo `errors["educations[0].status"] = ["Formação “Administração – UEM”: escolha a situação (Cursando, Concluído, Trancado ou Incompleto). O currículo não dizia."]`. A importação continua pendente e pode ser aplicada de novo depois da escolha.
- **`POST /profile/courses`** e **`PUT /profile/courses/{id}`**: `type` ∈ `Course | Certification`; ausente ou `null` vale `Course`.
- **`POST /jobs/analyze`**: requisitos com status (`atende`, `parcial`, `nao_encontrado`, `confirmar`) e evidência por linha real do perfil; nunca nota ou porcentagem (RN-06). Exige `JobAnalysis`.
- **`POST /forms/completions`**: registra a conclusão feita pela pessoa no portal; cria a candidatura (`status: Confirmed`) e o evento de auditoria, sem valores.

## 4. Rotas da versão 2

### 4.1 Perfil: campos novos

`PUT /profile` aceita, além dos dados básicos da Entrega 3:

| Campo | Tipo | Regra |
|---|---|---|
| `workArea` | string | Nome da área escolhida no onboarding (ex.: `"Saúde"`). Texto livre: "Outra área" aceita o nome que a pessoa escrever. |
| `professionalRegistration` | string | Registro profissional quando a área pede (ex.: `"COREN-MG 123456"`, `"OAB/MG 123.456"`). Não é dado sensível; é usado no de-para como qualquer campo do perfil. |
| `salaryType` | string | Um de `"CLT (bruto)"`, `"PJ"`, `"Por hora"`, `"Por plantão"` (sem diferenciar maiúsculas). Outro valor: 400 `validation_failed` com o erro em `errors.salaryType`. O domínio repete a regra (422 `profile.invalid_salary_type`) como proteção, mas o validador responde antes. |

`GET /profile` passa a incluir:

```json
{
  "fullName": "Beatriz Lima",
  "workArea": "Saúde",
  "professionalRegistration": "COREN-MG 123456",
  "salaryType": "Por plantão",
  "customFields": [
    { "id": "6b1f…", "label": "Disponível para plantão noturno", "value": "Sim, 12x36", "updatedAt": "2026-10-21T12:03:00Z" }
  ]
}
```

### 4.2 Campos personalizados (RF-25, ADR-0019)

| Rota | Corpo | Resposta |
|---|---|---|
| `GET /profile/custom-fields` | — | `200` com `[{ id, label, value, updatedAt }]` |
| `POST /profile/custom-fields` | `{ "label": "Disponível para plantão noturno", "value": "Sim, 12x36" }` | `201` com o campo |
| `PUT /profile/custom-fields/{id}` | `{ label, value }` | `200` com o campo |
| `DELETE /profile/custom-fields/{id}` | — | `204` |

Regras:

- `label` obrigatório, até 80 caracteres; `value` obrigatório, até 500. Fora disso: 400 `validation_failed`.
- Rótulo repetido no mesmo perfil (sem diferenciar maiúsculas): 422 `profile.custom_field_duplicated`. Até 50 campos por perfil: acima disso, 422 `profile.custom_field_limit`.
- **Rótulo que pareça sensível é recusado** com `422` e `code: "profile.custom_field_sensitive"`: raça, religião, saúde, deficiência, gênero, orientação, CPF, RG, senha e variações (comparação sem acento e por palavra). A mensagem orienta a usar **Dados sensíveis**, que ficam trancados e fora da IA (RN-04, RN-15).
- Cada campo vira um fato da IA (`ProfileFacts`) e uma chave do de-para: **`custom:<id>`**. O mapeador por regras compara o rótulo do campo do formulário com o `label`; a IA pode apontar `custom:<id>` como qualquer outra chave, e o valor continua vindo do perfil (RN-02).
- Toda criação, edição e exclusão gera o evento `custom_field.changed`, sem o rótulo nem o valor.

### 4.3 Catálogo por área (RF-24, ADR-0019)

`GET /catalog/areas` (web ou extensão, autenticado) devolve o catálogo estático da camada Application, com o mesmo conteúdo do onboarding desenhado:

```json
[
  {
    "id": "saude", "name": "Saúde",
    "roles": ["Técnico(a) de enfermagem", "Enfermeiro(a)", "Recepção hospitalar", "Farmácia", "Fisioterapia", "Análises clínicas"],
    "sectors": ["Hospital", "Clínica", "Laboratório", "Home care", "Saúde pública"],
    "tools": ["Prontuário eletrônico", "Tasy", "MV Soul", "Pacote Office"],
    "skills": ["Atendimento humanizado", "Administração de medicamentos", "Primeiros socorros", "Biossegurança", "Triagem", "Trabalho em equipe"],
    "linkLabel": "Currículo Lattes",
    "registration": { "label": "Registro no conselho", "placeholder": "Ex.: COREN-MG 123456" }
  }
]
```

| `id` | `name` | `linkLabel` | `registration.label` |
|---|---|---|---|
| `tecnologia` | Tecnologia | GitHub | `null` |
| `saude` | Saúde | Currículo Lattes | Registro no conselho |
| `vendas-e-comercial` | Vendas e comercial | `null` | `null` |
| `administrativo-e-financeiro` | Administrativo e financeiro | `null` | Registro profissional (se tiver) |
| `educacao` | Educação | Currículo Lattes | `null` |
| `logistica-e-operacoes` | Logística e operações | `null` | Cursos obrigatórios (NR, MOPP…) |
| `juridico` | Jurídico | `null` | Inscrição na OAB |
| `outra-area` | Outra área | `null` | `null` (listas vazias) |

As listas são **sugestões**: a pessoa pode cadastrar valores próprios em qualquer uma. O catálogo não depende do usuário e pode ser cacheado pelo cliente.

### 4.4 Onboarding (RF-23, ADR-0018)

```
GET /onboarding  → 200 { "answered": ["nome","contato"], "skipped": ["salario"], "lastStep": "contato", "completedAt": null }
PUT /onboarding  ← { "answered": [...], "skipped": [...], "lastStep": "area", "completed": false }
                 → 200 com o mesmo formato do GET
```

- Ids de passo aceitos, em ordem (26): `nome`, `nascimento`, `contato`, `endereco`, `area`, `cargos`, `setores`, `registro`, `links`, `nivel`, `modelo`, `local`, `contrato`, `disponibilidade`, `salario`, `mobilidade`, `experiencia`, `diaadia`, `ferramentas`, `formacao`, `cursos`, `competencias`, `idiomas`, `titulo`, `resumo`, `combinados`. Passo desconhecido em `answered`, `skipped` ou `lastStep`: 400 `validation_failed` (o validador lista os ids aceitos). O domínio repete a regra com 422 `onboarding.invalid_step` como proteção.
- O `PUT` **substitui** o progresso inteiro (não é incremental): o cliente manda as listas completas. Os ids são gravados em minúsculas, sem repetição e na ordem do onboarding.
- Rota só da web (`scope=web`); token de extensão recebe 403.
- A rota guarda **só o progresso** (ids). As respostas vão para os endpoints de perfil já existentes: `PUT /profile`, `POST /profile/experiences`, `educations`, `courses`, `skills`, `languages` e `PUT /consents/{purpose}` (passo `combinados`).
- Um passo respondido sai da lista de pulados. `completed: true` grava `completedAt` e o evento `onboarding.completed` uma única vez.
- Os passos em `skipped` alimentam o alerta "perguntas que ficaram para depois" no topo do Perfil Mestre.

### 4.5 Planos e uso (RF-26, RF-27, ADR-0020)

`GET /billing`:

```json
{
  "plan": "free", "period": null, "trialEndsAt": null, "renewsAt": null, "demo": true,
  "prices": { "plusMonthly": 9.90, "plusYearly": 99.00, "pack": 4.90 },
  "usage": {
    "month": "2026-10",
    "assistedApplications": { "used": 7, "limit": 10, "bonus": 3 },
    "aiSuggestions": { "used": 2, "limit": 5, "bonus": 0 },
    "jobAnalyses": { "used": 4, "limit": 10, "bonus": 0 },
    "resumeImports": { "used": 1, "limit": 2, "bonus": 0 }
  },
  "answersLimit": 15, "devicesLimit": 1,
  "rewarded": { "claimedThisMonth": 1, "maxPerMonth": 3, "creditsPerClaim": 3 },
  "packs": [{ "assistedRemaining": 15, "aiRemaining": 10, "expiresAt": "2026-12-20T14:00:00Z" }],
  "adsEnabled": true,
  "trialAvailable": true, "trialDays": 7
}
```

`trialAvailable` diz se a **próxima** assinatura ganha `trialDays` dias grátis: `true` só enquanto a conta nunca assinou (`Subscription.TrialUsed` falso). Depois da primeira assinatura fica `false`, inclusive após cancelar; assinar de novo começa sem `trialEndsAt`. A web só oferece "7 dias grátis" quando `trialAvailable` é `true` (teste `Teste_gratis_fica_indisponivel_depois_de_usado_mesmo_cancelando`).

| Rota | Corpo | Efeito |
|---|---|---|
| `POST /billing/subscribe` | `{ "period": "monthly" }` ou `"yearly"` | `200`. **Checkout simulado**: plano `plus`; na primeira assinatura da conta, 7 dias grátis (`trialEndsAt`); nas seguintes, sem teste (`trialEndsAt: null`, `trialAvailable: false`). Outro `period`: 400. Assinar de novo o mesmo período: 422 `billing.already_subscribed` (trocar de mensal para anual é aceito). Evento `plan.changed`. |
| `POST /billing/cancel` | — | `200`. Volta ao `free` **na hora** (é a regra da demonstração; em produção seria no fim do período pago). Já no gratuito: 422 `billing.not_subscribed`. Evento `plan.changed`. |
| `POST /billing/packs` | — | **`201`**. Cria um pacote: +15 candidaturas assistidas, +10 sugestões de IA, válido por 60 dias. Evento `pack.purchased`. |

Todas respondem com o `BillingDto` atualizado. O pacote cobre **só** candidaturas assistidas e sugestões de IA: análises de vaga, importações, respostas e navegadores dependem apenas do plano. `demo: true` significa que **nenhuma cobrança real** acontece e nenhum dado de cartão é pedido (RN-13).

**Limites** (por mês civil `AAAA-MM`, UTC):

| Recurso | Gratuito | Plus | O que conta |
|---|---|---|---|
| `assistedApplications` | 10 | `null` (sem limite) | `POST /forms/mappings`, **uma vez por (pessoa, origem + caminho da página) no mês** |
| `aiSuggestions` | 5 | `null` | `POST /forms/open-answers` |
| `jobAnalyses` | 10 | `null` | `POST /jobs/analyze` |
| `resumeImports` | 2 | `null` | `POST /resume-imports` |
| Banco de respostas (`answersLimit`) | 15 | `null` | Respostas guardadas (`POST /answers`) |
| Navegadores (`devicesLimit`) | 1 | 3 | Extensões pareadas ativas (`POST /extension/pair`) |

**Ordem de consumo:** limite do mês → bônus do anúncio recompensado (só candidaturas assistidas) → pacote que vence primeiro (só candidaturas assistidas e sugestões de IA). A deduplicação guarda só o **hash** de origem + caminho, nunca a URL (RNF-16). Ler de novo o mesmo formulário, ou outra etapa no mesmo endereço, não gasta outra candidatura.

**Limite atingido** — `402 Payment Required`:

```json
{ "type": "https://httpstatuses.io/402", "title": "Limite do plano atingido", "status": 402,
  "detail": "Você usou as 10 candidaturas assistidas do plano Gratuito este mês. Ganhe +3 vendo uma oferta, compre um pacote avulso ou conheça o Plus.",
  "instance": "/api/v1/forms/mappings", "traceId": "…", "code": "plan.limit_reached",
  "resource": "assistedApplications", "used": 10, "limit": 10,
  "extensions": { "resource": "assistedApplications", "used": 10, "limit": 10 } }
```

`resource`, `used` e `limit` aparecem **duas vezes**: no topo e dentro de `extensions`. Os clientes podem ler qualquer um dos dois. `resource` pode ser `assistedApplications`, `aiSuggestions`, `jobAnalyses`, `resumeImports`, `answers` (16ª resposta aprovada no Gratuito; reaprovar uma pergunta que já existe substitui o texto e não conta) ou `devices` (segundo navegador pareado no Gratuito, quarto no Plus). O `detail` muda conforme o recurso. Os clientes mostram as opções: oferta recompensada (se ainda houver no mês), pacote e Plus. O limite **nunca** bloqueia editar o perfil, exportar ou excluir a conta (RN-12; TN-14 coberto pelo teste `Limite_atingido_nunca_bloqueia_exportar_nem_excluir_os_dados`); o consumo só é gravado se a operação der certo.

No Swagger, o 402 aparece como resposta documentada (`ProducesResponseType(typeof(ProblemDetails), 402)`) em `POST /forms/mappings`, `POST /forms/open-answers`, `POST /jobs/analyze`, `POST /resume-imports`, `POST /answers` e `POST /extension/pair`.

### 4.6 Anúncios (RF-28, RF-29, ADR-0020)

`GET /ads?placement=<p>[&topic=<texto do requisito>]`:

```json
{ "id": "demo-sau-sbv", "placement": "ext.job.contextual", "format": "contextual",
  "title": "Suporte Básico de Vida com certificado", "advertiser": "Anunciante de demonstração",
  "body": "Curso presencial de 16 h com simulação prática. Turmas aos sábados.", "cta": "Ver turmas",
  "url": "https://example.com/zyft-demo/sbv", "imageUrl": null,
  "reason": "Porque a vaga pede algo que não aparece no seu perfil, e este anúncio é sobre cursos da área da saúde. Usamos só a sua área, os cargos que você busca e o texto da vaga — nunca nome, contato ou dados sensíveis.",
  "topic": "Certificado de Suporte Básico de Vida (SBV)", "demo": true }
```

- `topic` na resposta é o **texto do requisito** recebido, ecoado (até 80 caracteres) só no placement contextual, para o card dizer "Sobre: …". Nos outros placements é `null`. Não é o assunto interno do catálogo.
- `demo: true` marca que o anunciante é fictício. `imageUrl` é sempre `null` no MVP 1.

Respostas sem anúncio:

- **`400`** `validation_failed` quando falta `placement` (ou ele passa de 60 caracteres, ou `topic` de 500).
- **`204 No Content`** quando: a pessoa é Plus; o placement é desconhecido ou proibido; não há anúncio que combine; ou tudo o que combinaria foi ocultado.
- Em `ext.job.contextual`, só há anúncio se o `topic` **casar com as palavras-chave** de algum anúncio. Sem `topic`, ou com um requisito sem relação, a resposta é 204. Por isso os clientes tentam alguns requisitos ausentes e, se nenhum casar, pedem o `ext.panel.banner`.

| Placement | Onde | Formato |
|---|---|---|
| `web.profile.rail` | Coluna lateral do Perfil Mestre | `native` (card) |
| `web.applications.feed` | Entre o resumo e a lista de Candidaturas | `native` (card) |
| `web.banner` | Abaixo do título em Banco de respostas, Extensão, Detalhe da candidatura e Registro de ações | `banner` |
| `ext.panel.banner` | Rodapé do painel ao conectar e na aba "A vaga e você" | `banner` |
| `ext.job.contextual` | Ligado a um requisito da vaga que não aparece no perfil; substitui o `ext.panel.banner` | `contextual` |
| `ext.after-complete` | Depois de "Candidatura registrada" | `native` |

Nunca há anúncio no onboarding, em entrar/criar conta, em importar currículo, em dados sensíveis, em conta e privacidade nem no painel "Ler formulário" (RN-08). Por isso não existe placement para essas telas, e um placement desconhecido devolve 204.

**Seleção:** catálogo **estático no código** (`AdCatalog`, 16 anúncios, ids de texto como `demo-tec-cloud` e `demo-sau-sbv`, todos de "Anunciante de demonstração"). Não há tabela de anúncios: o banco guarda só as ocultações de cada pessoa e os contadores agregados. A seleção usa `workArea` e `desiredRoles` (lidos no servidor) e o `topic`. Pontuação: requisito da vaga (10) > área (5) > cargos (3) > anúncio geral (1). Anúncio de outra área só entra se o requisito pedir o assunto. Entre os empatados, vence o que teve menos exibições. `reason` explica em linguagem simples por que o anúncio apareceu. Nenhum dado pessoal identificável ou sensível entra na seleção nem na resposta (RN-09, RNF-14).

| Rota | Corpo | Resposta |
|---|---|---|
| `POST /ads/{id}/hide` | `{ "scope": "ad" }` (por 30 dias; é o padrão se o corpo vier vazio) ou `{ "scope": "topic" }` (o assunto, para sempre) | `204`; evento `ad.hidden`. Outro `scope`: 400. Id fora do catálogo: 404 |
| `POST /ads/{id}/click` | — | `204`; incrementa um contador **agregado** por anúncio, sem usuário. Id fora do catálogo: 404 |
| `POST /ads/rewarded/claim` | — | `200` com o `BillingDto`: +3 candidaturas assistidas de bônus no mês; evento `rewarded.claimed`. Na 4ª vez no mês: `409` `ads.rewarded_exhausted`. No Plus: `409` `ads.rewarded_unavailable` (sem limite, a oferta não se aplica) |

Cada `GET /ads` com anúncio também soma uma exibição no mesmo contador agregado (`AdStats`). A API não verifica se a oferta recompensada foi de fato vista: na demonstração, o clique em "Ganhar +3" é a confirmação.

#### Rede de anúncios (Google AdSense simulado, ADR-0021)

Quando o espaço é preenchido pela rede, a resposta muda de forma:

```json
{ "id": "adsense-1000000001", "placement": "web.banner", "format": "network",
  "title": "Anúncio do Google", "advertiser": "Google AdSense", "body": "", "cta": "", "url": "", "imageUrl": null,
  "reason": "Espaço da rede de anúncios do Google (AdSense). Nesta versão é uma simulação: nada é enviado ao Google. Em produção, só anúncios não personalizados, sem nenhum dado do seu perfil.",
  "topic": null, "demo": true,
  "network": { "provider": "Google AdSense", "client": "ca-pub-0000000000000000", "slot": "1000000001", "adFormat": "horizontal", "mock": true } }
```

- `network` só existe com `format: "network"`. O cliente monta `<ins class="adsbygoogle" data-ad-client={client} data-ad-slot={slot} data-ad-format={adFormat}>`. `mock: true` = não carregar o script do Google (única opção nesta versão).
- Ordem: em `web.banner` e `ext.panel.banner` a rede vem **antes** do anúncio direto; em `web.profile.rail`, `web.applications.feed` e `ext.after-complete` a rede só entra quando não há anúncio direto; `ext.job.contextual` nunca é da rede. Plus e espaços proibidos: 204, como sempre.
- `POST /ads/adsense-<slot>/hide` oculta o bloco por 30 dias (o `scope` é ignorado; a rede não tem tema); depois o espaço cai para o anúncio direto ou 204. `POST /ads/adsense-<slot>/click` responde 204 e **não** conta nada (o clique de rede é da rede). Exibições de rede também não entram em `AdStats`.
- Nada do perfil entra na resposta nem na configuração do bloco (RN-09). Configuração em `Ads:Network` (`Enabled`, `Mock`, `ClientId`, `Slots`, `Preferred`); código em `IAdNetwork` / `AdSenseNetwork`.

### 4.7 Auditoria: tipos novos

`GET /audit-events` passa a listar também `plan.changed`, `pack.purchased`, `ad.hidden`, `rewarded.claimed`, `onboarding.completed` e `custom_field.changed`. Como os anteriores, registram **o que** aconteceu, nunca conteúdo pessoal (sem rótulo, valor, anúncio clicado com dados da pessoa ou URL).

## 5. Códigos de erro

| HTTP | `code` | Quando |
|---|---|---|
| 400 | `validation_failed` (com `errors` por campo) ou `bad_request` | Formato ou validação (FluentValidation, JSON inválido) |
| 401 | `unauthorized` | Sem token, token inválido, expirado ou de extensão revogada |
| 402 | `plan.limit_reached` | Limite do plano atingido (v2), com `resource`, `used` e `limit` no topo e em `extensions` |
| 403 | `forbidden` / `consent_required` | Policy errada (ex.: token de extensão em rota só web) ou finalidade desligada |
| 404 | `not_found` | Inexistente **ou de outra pessoa** (não revela existência) |
| 409 | `conflict` / `ads.rewarded_exhausted` / `ads.rewarded_unavailable` | Conflito (e-mail já cadastrado, duplicata no banco); ofertas recompensadas do mês esgotadas; oferta recompensada pedida no Plus (v2) |
| 413 | `payload_too_large` | Currículo acima de 10 MB |
| 422 | regra de domínio (`experience.invalid_period`, `profile.custom_field_sensitive`, `profile.custom_field_duplicated`, `profile.custom_field_limit`, `billing.already_subscribed`, `billing.not_subscribed`…) | Regra de negócio violada. `profile.invalid_salary_type`, `onboarding.invalid_step` e `billing.invalid_period` existem no domínio, mas na prática o validador responde antes com 400 |
| 429 | `rate_limited` | Acima do limite por IP ou por pessoa, com `Retry-After` |
| 500 | `internal_error` | Erro inesperado, sem stack trace |

### 5.1 Erros de validação (400)

Corpo `ValidationProblemDetails`: `detail` "Um ou mais campos são inválidos." e `errors` com a chave em camelCase do campo, incluindo o índice do item quando é lista (`educations[0].status`).

- **FluentValidation**: a mensagem é específica do campo ("Informe a situação.", "Escolha o tipo: Curso ou Certificação.").
- **Formato/model binding** (JSON inválido, enum desconhecido, número em texto): a chave é o **campo** que falhou (`level`, `startDate`), nunca o nome do parâmetro da action (`cmd`); a mensagem é "Valor ausente ou em formato inválido." ou, para datas, "Data inválida. Use AAAA-MM-DD ou AAAA-MM.". Só quando nenhum campo explica o erro aparece a chave `body` ("O corpo da requisição está ausente ou não é um JSON válido."). Teste: `Curso_sem_tipo_vira_Curso_e_erro_de_formato_aponta_o_campo_e_nao_o_parametro`.
- **Clientes**: a web troca a chave pelo nome que a pessoa vê na tela (`educations[0].status` → "Formação 1 · Situação", `type` → "Tipo") e mostra o erro junto ao campo; se o campo não estiver no formulário aberto, o resumo do topo diz "Rótulo: mensagem" (ver [frontend web](frontend-web.md)).

Os códigos genéricos vêm de `src/Zyft.Api/Infrastructure/ExceptionHandlingMiddleware.cs` e `Program.cs` (403 `forbidden`, 429 `rate_limited`); os de domínio, nas exceções do `Zyft.Domain` (`PlanLimitReachedException` e `RewardedExhaustedException` em `Domain/Billing/Plans.cs`). `ads.rewarded_unavailable` é uma `ConflictException` da Application.

## 6. Versionamento e compatibilidade

- Campos novos e opcionais entram na `v1` sem quebrar clientes (é o caso de toda a versão 2). Mudança incompatível exige `/api/v2`.
- O status `402` é novo em relação à Entrega 3. A web (`web/js/billing.js`) e a extensão (v0.4.0) já o tratam e mostram as opções. Um cliente antigo mostraria o `detail` como erro comum.
- A extensão envia `X-Zyft-Extension-Version`. Toda mudança de contrato atualiza, no mesmo PR, o controller, os DTOs, os testes de integração, a web, a extensão e este documento.
