# Contrato da API v1

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Contrato REST da API v1 |
| Versão | 1.0 |
| Status | Rascunho para revisão. A autenticação da extensão depende do [ADR-0007](adr/0007-autenticacao-e-pareamento-da-extensao.md) (proposto). |
| Dono | Arquitetura de software, com o time de backend |
| Documentos relacionados | [Backend](backend.md), [Extensão](extensao.md), [Modelo de dados](modelo-de-dados.md), [Segurança](seguranca.md) |

## 1. Convenções

- Base: `https://api.zyft.app/v1`. Só JSON (`application/json; charset=utf-8`), exceto o envio de currículo (`multipart/form-data`).
- Nomes de campos em camelCase, em inglês, como no protótipo. Mensagens para pessoas em português.
- Erros no formato Problem Details ([backend §5](backend.md#5-erros)).
- Toda resposta tem o header `X-Trace-Id`.
- A fonte da verdade são os schemas Zod em `packages/api-contract`. Este documento é a versão legível, e um teste de contrato garante que os dois batem.

### 1.1 Autenticação

| Cliente | Mecanismo | Rotas permitidas |
|---|---|---|
| Plataforma web | Cookie de sessão `zyft_session` (`HttpOnly; Secure; SameSite=Lax`) mais o header `X-Zyft-CSRF` em métodos que alteram dados | Todas, exceto as marcadas como "só extensão" |
| Extensão | `Authorization: Bearer <access token>` (validade de 15 min) mais o header `X-Zyft-Extension-Version` | Só as marcadas com **E** na tabela da seção 2 |

CORS: a origem da plataforma (`WEB_ORIGIN`) e as origens `chrome-extension://<id>` dos IDs publicados (`EXTENSION_IDS`). Qualquer outra origem é recusada. O protótipo aceitava qualquer origem de extensão, e isso deixa de valer.

## 2. Mapa de rotas

Legenda: **W** = plataforma web, **E** = extensão.

| Método e rota | Cliente | Requisito | Origem no protótipo |
|---|---|---|---|
| `POST /v1/auth/signup`, `/login`, `/logout`, `/verify-email`, `/google/callback` | W | RF-01 | — |
| `GET /v1/me` | W, E | RF-01 | `GET /api/session` (token) |
| `GET /oauth/authorize` (web) e `POST /v1/oauth/token` | E (sem auth; PKCE) | RF-09 | — |
| `POST /v1/devices/pairing` | E (sem auth; fluxo alternativo) | RF-09 | — |
| `POST /v1/devices/pairing/confirm` | W | RF-09 | — |
| `POST /v1/devices/token` | E (sem auth; `deviceCode`, fluxo alternativo) | RF-09 | — |
| `GET /v1/devices`, `DELETE /v1/devices/{id}` | W | RF-09 | — |
| `GET /v1/profile`, `PUT /v1/profile` | W | RF-02 | `GET/PUT /api/profile` |
| `GET /v1/profile/versions`, `POST /v1/profile/versions/{n}/restore` | W | RF-02 | — |
| `GET /v1/profile/missing` | W | RF-05 | `GET /api/profile/missing` |
| `POST /v1/profile/patches` | W, E | RF-16 | `POST /api/profile/patch` |
| `GET/PUT /v1/profile/sensitive` | W | RN-04 | parte do perfil |
| `POST /v1/resume-imports` | W | RF-03 | `POST /api/profile/import` |
| `GET /v1/resume-imports/{id}` | W | RF-03 | — |
| `POST /v1/resume-imports/{id}/apply`, `/discard` | W | RF-04 | `PUT /api/profile` com a proposta |
| `GET/POST /v1/approved-answers`, `PUT/DELETE /v1/approved-answers/{id}` | W (E só `POST`) | RF-06 | `/api/profile/answers` |
| `GET/PUT /v1/consents` | W (E só `GET`) | RF-07 | `/api/consents` |
| `POST /v1/mappings` | E | RF-12, RF-18 | `POST /api/map-fields` |
| `POST /v1/writing/answers` | E | RF-15 | `POST /api/generate` |
| `POST /v1/writing/improve` | W, E | RF-21 | `POST /api/improve` |
| `POST /v1/job-analyses` | E | RF-17 | `POST /api/analyze-job` |
| `POST /v1/audit-events` | E | RF-20 | `POST /api/fill-log` |
| `GET /v1/audit-events` | W | RF-20 | `GET /api/logs` |
| `POST /v1/completions` | E | RF-19 | `POST /api/applications` (**só se o ADR-0012 for aceito**) |
| `POST /v1/privacy/export` | W | RF-08 | `GET /api/export` |
| `DELETE /v1/account` | W | RF-08 | `DELETE /api/data` |
| `GET /v1/status` | público | RNF-12 | `GET /api/status` |

Saem do MVP 1: `GET/POST/PATCH/DELETE /api/applications` (histórico com status, MVP 2) e `GET /api/profile/flat` (expunha o perfil achatado inteiro).

## 3. Rotas principais

### 3.1 `POST /v1/mappings` (extensão)

Mínimo retorno (RNF-02): a resposta tem **um item por campo enviado** e nada além disso. Não há como pedir "o perfil todo".

Requisição:

```json
{
  "url": "https://vagas.exemplo.com/candidatura/123",
  "connector": { "id": "portal-x", "version": "1.2.0", "screen": "resume-form" },
  "jobAnalysisId": null,
  "fields": [
    { "id": "f1", "tag": "input", "type": "text", "name": "nome", "label": "Nome completo", "section": "Dados pessoais", "required": true, "maxLength": 120, "currentValue": "" },
    { "id": "f7", "tag": "select", "type": "select", "label": "Escolaridade", "section": "Formação",
      "options": [{ "value": "", "text": "Selecione" }, { "value": "3", "text": "Superior completo" }], "currentValue": "" },
    { "id": "f9", "tag": "textarea", "type": "textarea", "label": "Por que você quer trabalhar conosco?", "maxLength": 600 },
    { "id": "f12", "tag": "input", "type": "checkbox", "label": "Declaro que as informações são verdadeiras" }
  ]
}
```

Resposta `200`:

```json
{
  "mappings": [
    { "fieldId": "f1", "label": "Nome completo", "kind": "profile", "source": "regra", "profileKey": "personal.fullName",
      "profileLabel": "Nome completo", "value": "Ana Paula Souza", "confidence": "alta", "selected": true },
    { "fieldId": "f7", "label": "Escolaridade", "kind": "profile", "source": "ia", "profileKey": "education[0].degree",
      "value": "3", "optionText": "Superior completo", "confidence": "média", "selected": true },
    { "fieldId": "f9", "label": "Por que você quer trabalhar conosco?", "kind": "generated", "source": "ia",
      "value": "Tenho 3 anos de experiência como assistente administrativa...", "usedFacts": ["Experiência 1: ..."],
      "selected": false, "note": "Texto gerado a partir do seu perfil — revise antes de usar." },
    { "fieldId": "f12", "label": "Declaro que as informações são verdadeiras", "kind": "declaration", "selected": false,
      "note": "Declarações e termos são sempre decididos por você." }
  ],
  "stats": { "fields": 4, "byRules": 1, "byAi": 2, "blocked": 1 },
  "ms": 2140
}
```

Regras do contrato:
- `value` só aparece quando `kind ∈ {profile, approved, generated}`, ou em `sensitive` com consentimento (c) ativo, sempre com `selected=false`.
- `profileValue` só aparece em `ambiguous` ou quando `currentValue` difere do valor do perfil (RF-16).
- `kind=generated` e `kind=sensitive` têm sempre `selected=false`.
- Limite de 300 campos por requisição e de 2.000 caracteres por rótulo ou opção (`413` acima disso).

### 3.2 `POST /v1/writing/answers` (extensão)

```json
{ "question": "Por que você quer trabalhar conosco?", "maxLength": 600, "jobAnalysisId": "01J..." }
```

Resposta: o mesmo formato de um item de `mappings`, com `kind ∈ {approved, generated, missing, unknown}`. Requer o consentimento `aiFreeText`; sem ele, `403 CONSENT_REQUIRED`.

### 3.3 `POST /v1/job-analyses` (extensão)

```json
{ "url": "https://vagas.exemplo.com/vaga/987", "title": "Assistente Administrativo", "text": "..." }
```

Resposta `200` (ou `202` com `{id}` se estiver em fila; nesse caso, `GET /v1/job-analyses/{id}`):

```json
{
  "id": "01J...",
  "job": { "title": "Assistente Administrativo", "company": "Empresa X", "location": "Ibiporã/PR", "contractType": "CLT",
           "workModel": "presencial", "schedule": "", "salary": "", "summary": "..." },
  "requirements": [
    { "text": "Experiência com rotinas administrativas", "kind": "obrigatório", "status": "atende",
      "evidence": "Experiência 1: Assistente administrativa na Empresa Y (03/2021 a atual, 3 anos e 7 meses)" },
    { "text": "Pacote Office avançado", "kind": "desejável", "status": "nao_encontrado", "evidence": "" }
  ],
  "toConfirm": [{ "label": "CNH", "profileKey": "preferences.driverLicense", "hasValue": false }],
  "stats": { "atende": 1, "nao_encontrado": 1, "total": 2, "requiredTotal": 1, "requiredMet": 1 },
  "disclaimer": "Análise baseada apenas no que está visível na página e no seu Perfil Mestre. Não é garantia de aprovação."
}
```

Mudanças em relação ao protótipo: `toConfirm` devolve `hasValue` em vez do valor do perfil (mínimo retorno), e a resposta tem `id` para o contexto do texto livre. Requer o consentimento `jobAnalysis`.

O painel mostra `stats` como contagem ("3 de 4 requisitos obrigatórios aparecem no seu perfil"), **nunca** como porcentagem ou nota (RN-06).

### 3.4 Importação de currículo

`POST /v1/resume-imports` (`multipart/form-data`, campo `file`, PDF ou DOCX, até 10 MB):

```json
{ "id": "01J...", "status": "processing" }
```

`GET /v1/resume-imports/{id}`:

```json
{
  "id": "01J...", "status": "ready", "chars": 5321,
  "proposal": { "personal": { "fullName": "Ana Paula Souza", "...": "..." }, "experiences": [ { "...": "..." } ] },
  "diff": [ { "path": "personal.phone", "current": "(44) 99999-1111", "proposed": "(44) 99999-2222" } ]
}
```

`POST /v1/resume-imports/{id}/apply`:

```json
{ "decisions": [
  { "path": "personal.phone", "action": "keep-current" },
  { "path": "experiences[1]", "action": "accept" },
  { "path": "skills", "action": "edit", "value": ["Excel", "Atendimento ao cliente"] }
] }
```

Resposta: `{ "profileVersion": 7, "missing": [{ "key": "preferences.availability", "question": "Quando pode começar?" }] }`.

Caminhos sem decisão são tratados como **descartados**. Nada entra no perfil por omissão.

### 3.5 `POST /v1/profile/patches` (RF-16, "levar para o perfil")

```json
{ "source": "portal", "portalHost": "vagas.exemplo.com",
  "changes": [{ "key": "personal.phone", "value": "(44) 99999-2222" }] }
```

Só aceita chaves do `profile-schema` (não aceita `sensitive.*`). Cria uma nova versão do perfil com `source=portal` e um evento de auditoria com as chaves alteradas, sem os valores.

### 3.6 Pareamento (ADR-0007, proposto)

**Fluxo principal — Authorization Code + PKCE** (`chrome.identity.launchWebAuthFlow`):

```
GET  /oauth/authorize   ?response_type=code&client_id=extensao&redirect_uri=https://<id>.chromiumapp.org/
                        &code_challenge=<S256>&code_challenge_method=S256&state=<aleatório>   (web; login + consentimento)
POST /v1/oauth/token    ← { grant_type: "authorization_code", code, code_verifier, redirect_uri }
                        → 200 { accessToken, refreshToken, expiresIn: 900 }
POST /v1/oauth/token    ← { grant_type: "refresh_token", refreshToken }               → novo par (rotação; reuso revoga a concessão)
```

**Fluxo alternativo — código de pareamento** (quando `launchWebAuthFlow` falha):

```
POST /v1/devices/pairing          → { deviceCode, userCode: "K7Q-4MZP", verificationUri, interval: 5, expiresIn: 600 }
POST /v1/devices/pairing/confirm  ← { userCode, deviceName }                          (web, logada)
POST /v1/devices/token            ← { grantType: "device_code", deviceCode }
                                  → 428 { code: "AUTHORIZATION_PENDING" } | 200 { accessToken, refreshToken, expiresIn: 900 }
(a renovação usa POST /v1/oauth/token com grant_type=refresh_token nos dois fluxos)
```

### 3.7 Auditoria

`POST /v1/audit-events` (extensão):

```json
{ "action": "campos.preenchidos", "portalHost": "vagas.exemplo.com",
  "counts": { "lidos": 24, "propostos": 21, "preenchidos": 18, "falhas": 0, "acaoSua": 3 } }
```

O schema só aceita `action` de uma lista fechada e `counts` com valores inteiros. Qualquer outro campo é rejeitado com `400`. Assim a extensão não consegue registrar dados pessoais nem por engano.

## 4. Versionamento e compatibilidade

- Mudanças incompatíveis exigem `/v2`. Campos novos e opcionais podem entrar na `v1`.
- A extensão informa a versão em `X-Zyft-Extension-Version`. A API aceita a versão atual e a anterior; para versões mais antigas, responde `426` com `code: "EXTENSION_OUTDATED"`.
- Toda mudança de contrato atualiza `packages/api-contract`, este documento e os testes de contrato no mesmo PR.
