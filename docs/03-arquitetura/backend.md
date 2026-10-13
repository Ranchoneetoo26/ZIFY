# Arquitetura do backend (API e worker)

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Arquitetura do backend |
| Versão | 2.0 (substitui `docs/arquivo/marketplace-v1/arquitetura-backend.md`) |
| Status | Em revisão. Depende dos ADRs [0003](adr/0003-monorepo-typescript-node.md), [0007](adr/0007-autenticacao-e-pareamento-da-extensao.md) e [0008](adr/0008-postgresql-com-perfil-em-jsonb.md) (propostos). |
| Dono | Arquitetura de software, com o time de backend |
| Documentos relacionados | [DAT](dat.md), [API](api.md), [IA](ia.md), [Modelo de dados](modelo-de-dados.md), [Segurança](seguranca.md) |

## 1. Visão geral

A API é um **monólito modular** em Node.js 22 LTS e TypeScript (`apps/api`), com Fastify e validação Zod. Um segundo processo, o **worker**, usa o mesmo código e consome tarefas da fila (pg-boss, sobre o próprio PostgreSQL) para operações longas de IA.

Ponto de partida: o servidor Express do protótipo (`server/src/*.js`). A lógica de domínio (`profile.js`, `mapper.js`, `jobs.js`, `writer.js`, `importer.js`, `text.js`) é migrada quase sem mudança de comportamento para os pacotes `profile-schema`, `field-rules` e `llm` e para os módulos abaixo, agora com tipos e testes.

## 2. Camadas

```
apps/api/src/
  http/            rotas Fastify v1, schemas Zod de entrada/saída, autenticação, limites
  modules/
    <módulo>/
      routes.ts    adaptador HTTP do módulo
      service.ts   regras de negócio (sem Fastify, sem SQL)
      repo.ts      acesso a dados (Kysely sobre PostgreSQL)
      events.ts    eventos de auditoria emitidos
  platform/
    db.ts          pool, transações, migrações (Kysely migrator)
    queue.ts       pg-boss
    crypto.ts      envelope encryption para o cofre de sensíveis
    llm.ts         instancia o LLMProvider configurado (pacote packages/llm)
    logger.ts      pino com lista de campos permitidos (ADR-0015)
    config.ts      variáveis de ambiente validadas por Zod
  worker.ts        entrypoint do worker
  server.ts        entrypoint da API
```

Regras de dependência:
- `http` → `modules/*/service` → `repo` e `platform`. Serviços não importam Fastify.
- Um módulo chama outro **só pelo seu `service`**, nunca pelo `repo`.
- A lógica determinística (regras de campo, achatamento, fatos) fica nos pacotes, sem I/O, para ser testada isoladamente e reutilizada pela extensão.

## 3. Módulos

| Módulo | Responsabilidade | Requisitos | Dados |
|---|---|---|---|
| `auth` | Cadastro e login (e-mail com verificação por link mágico ou senha com Argon2id; Google OIDC), sessão web por cookie `HttpOnly; Secure; SameSite=Lax`, pareamento da extensão (Authorization Code + PKCE via `launchWebAuthFlow`; código de pareamento como alternativa), emissão e rotação de tokens, revogação | RF-01, RF-09, RNF-03 | `users`, `auth_identities`, `web_sessions`, `extension_devices`, `device_pairings` |
| `profile` | CRUD do Perfil Mestre, validação pelo esquema do `profile-schema`, versões (cada gravação cria uma versão), achatamento, `missing` (RF-05), `patch` vindo do portal (RF-16) | RF-02, RF-05, RF-16 | `profiles`, `profile_versions` |
| `sensitive-vault` | Guarda e lê dados sensíveis criptografados por campo; só libera um valor para o `mapping` quando o consentimento (c) está ativo; nunca entrega ao `llm` | RN-04, RF-18 | `profile_sensitive` |
| `import` | Recebe o arquivo, extrai o texto em memória (`pdf-parse`, `mammoth`), enfileira a extração, guarda a proposta e aplica as decisões da pessoa | RF-03, RF-04 | `resume_imports` |
| `mapping` | Pipeline do de-para: bloqueios → regras (`field-rules`) → IA (`llm`) → validação; contrato de mínimo retorno | RF-12, RF-18, RNF-02, RNF-05 | Sem estado (só auditoria) |
| `writing` | Respostas abertas (reuso de aprovadas ou geração), melhoria de texto (RF-21), CRUD de respostas aprovadas | RF-06, RF-15, RF-21 | `approved_answers` |
| `job-analysis` | Extração da vaga visível, avaliação com evidência por linha, pontos a confirmar | RF-17 | Efêmero (cache em memória por hash, TTL de 30 min) |
| `consent` | Estado atual e histórico por finalidade; o guard `requireConsent(finalidade)` é usado pelos outros módulos | RF-07, RNF-07 | `consents`, `consent_events` |
| `audit` | Grava eventos legíveis pela pessoa, **sem valores de campos** | RF-20 | `audit_events` |
| `privacy` | Exportação (JSON) e exclusão total, com revogação dos dispositivos | RF-08 | Todas |
| `completion` | Só se o [ADR-0012](adr/0012-registro-minimo-de-conclusao-no-mvp1.md) for aceito: registro mínimo de conclusão | RF-19 | `completion_records` |

### 3.1 Pipeline do `mapping`

Comportamento herdado de `server/src/mapper.js` e formalizado:

1. **Normalização**: `norm()` sem acento, minúsculo, sem pontuação; separa camelCase.
2. **Bloqueios** (`field-rules/blockers`), avaliados antes de tudo:
   - `password`, `hidden`, `submit`, `button`, `image`, `reset`: descartados;
   - CAPTCHA e códigos de verificação: `kind=manual`;
   - `file`: `kind=manual` ("anexe o arquivo manualmente");
   - sensíveis (gênero, raça/etnia, deficiência, religião, saúde, orientação sexual, estado civil, filhos...): `kind=sensitive`, valor só do cofre e só com consentimento (c), sempre `selected=false`;
   - checkbox de declaração (declaro, aceito, termos, LGPD, autorizo): `kind=declaration`, `selected=false`.
3. **Regras determinísticas**: `TOP_RULES` (campos de nível superior, ordem de prioridade, com regras genéricas avaliadas por último) e `GROUPS` (experiências, formação, cursos, certificações, idiomas), com índice explícito (`exp[1][empresa]`, "Experiência 2") ou ordinal e alinhamento por identidade (`alignSlots`).
4. **Regras do conector** (novo): o conector do portal pode fixar `fieldSelector → profileKey` para telas conhecidas, com precedência sobre as regras genéricas.
5. **IA** (só para o que sobrou e com IA disponível): uma chamada com rótulo, tipo, seção e até 15 opções de cada campo, e a lista de chaves do perfil. Resposta em JSON Schema com `fieldId` em `enum` e `action ∈ {fill, generate, ask}`.
6. **Validação**: `profileKey` precisa existir no perfil achatado; o valor é lido do perfil; em select ou radio, `pickOption` precisa encontrar uma opção existente, senão o campo vira `ambiguous`; datas são adaptadas ao tipo do input; o texto é cortado em `maxLength`.
7. **Texto livre**: `generate` vai para o `writing` (requer consentimento (a)).

Saída: um item por campo recebido, na ordem da página, com `kind ∈ {profile, approved, generated, sensitive, declaration, manual, ambiguous, missing, unknown}` e `source ∈ {regra, conector, ia, resposta aprovada}`.

### 3.2 Mínimo retorno (RNF-02)

- A resposta do `mapping` contém valores **só** para os campos enviados naquela leitura.
- `profileValue` (o valor do perfil mostrado quando há divergência ou ambiguidade) só é devolvido quando o campo tem `currentValue` diferente ou é `ambiguous`, porque a pessoa precisa dele para decidir.
- Nenhuma rota usada pela extensão devolve o perfil inteiro; `GET /v1/profile` só aceita sessão web.

## 4. Contratos internos

```ts
// packages/field-rules
export type ScannedField = {
  id: string; tag: 'input' | 'select' | 'textarea'; type: string;
  name?: string; htmlId?: string; label?: string; placeholder?: string; ariaLabel?: string;
  section?: string; required?: boolean; maxLength?: number; currentValue?: string;
  options?: { value: string; text: string }[];
};
export type FieldProposal = {
  fieldId: string; label: string;
  kind: 'profile' | 'approved' | 'generated' | 'sensitive' | 'declaration' | 'manual' | 'ambiguous' | 'missing' | 'unknown';
  source?: 'regra' | 'conector' | 'ia' | 'resposta aprovada';
  profileKey?: string; profileLabel?: string; profileValue?: string;
  value?: string | boolean; optionText?: string;
  confidence?: 'alta' | 'média'; selected: boolean; note?: string;
  usedFacts?: string[];
};

// packages/llm
export interface LLMProvider {
  json<T>(req: { task: LlmTask; system?: string; prompt: string; schema: JsonSchema; temperature?: number; timeoutMs?: number }): Promise<T>;
  status(): Promise<{ online: boolean; model: string; available: boolean }>;
}
export type LlmTask = 'map-fields' | 'free-text' | 'improve-text' | 'resume-parse' | 'job-extract' | 'job-evaluate';
```

O `task` é usado para métricas, para escolher o prompt versionado e para os limites de tempo.

## 5. Erros

Formato único ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457), Problem Details):

```json
{ "type": "https://zyft.app/erros/consentimento-necessario", "title": "Análise de vaga desativada",
  "status": 403, "code": "CONSENT_REQUIRED", "detail": "Ative a análise de vagas em Configurações.", "traceId": "01J..." }
```

| `code` | HTTP | Quando |
|---|---|---|
| `VALIDATION_FAILED` | 400 | Payload fora do schema Zod |
| `UNAUTHENTICATED` | 401 | Sem sessão ou token expirado (a extensão renova pelo refresh) |
| `DEVICE_REVOKED` | 401 | Dispositivo revogado; a extensão volta para o pareamento |
| `CONSENT_REQUIRED` | 403 | Finalidade desligada (IA para textos livres, análise de vaga, sensíveis) |
| `NOT_FOUND` | 404 | Recurso inexistente ou de outro usuário (nunca 403, para não revelar existência) |
| `UNSUPPORTED_FILE` | 415 | Currículo fora de PDF com texto ou DOCX |
| `FILE_TOO_LARGE` | 413 | Acima de 10 MB |
| `RATE_LIMITED` | 429 | Acima do limite por usuário ou dispositivo |
| `LLM_UNAVAILABLE` | 503 | IA fora do ar. **No `mapping` isso não vira erro:** os campos restantes voltam como `unknown`, com nota. |
| `LLM_INVALID_OUTPUT` | 502 | JSON inválido após uma nova tentativa |

Mensagens de `detail` são em português simples e dizem o que fazer (requisitos de UX do DRP).

## 6. Tarefas assíncronas

| Tarefa | Por que é assíncrona | Fila | Tentativas | Timeout |
|---|---|---|---|---|
| `resume.parse` | Extração com IA leva de 30 s a 3 min com 7B | `resume` | 2 | 300 s |
| `job.analyze` | Até 21 chamadas de IA (extração mais requisitos) | `job` | 1 | 120 s |
| `privacy.delete` | Exclusão em cascata e revogação | `privacy` | 5 | 60 s |
| `retention.sweep` | Apaga propostas não aplicadas, pareamentos expirados e eventos fora do prazo | `maintenance` (cron diário) | 3 | 600 s |

O `mapping` e o `writing` continuam **síncronos** (a pessoa está esperando no painel), com timeout de 20 s. Se a IA não responder nesse prazo, o `mapping` devolve o resultado só das regras.

A análise de vaga começa síncrona, como no protótipo. Ela passa para a fila se o p95 superar 30 s (RNF-04); nesse caso, o painel acompanha por polling.

## 7. Configuração

| Variável | Exemplo | Uso |
|---|---|---|
| `DATABASE_URL` | `postgres://...` | PostgreSQL |
| `LLM_PROVIDER` | `ollama` \| `openai-compatible` | Adaptador da porta de IA |
| `LLM_BASE_URL` | `http://ollama:11434` | Endpoint do modelo |
| `LLM_MODEL` | `qwen2.5:7b` | Modelo |
| `VAULT_KEK_ID` | id da chave no KMS | Criptografia de sensíveis |
| `WEB_ORIGIN` | `https://app.zyft.app` | CORS e cookies |
| `EXTENSION_IDS` | `abcd...,efgh...` | Origens `chrome-extension://` aceitas |
| `RATE_LIMIT_*` | | Limites por rota |

## 8. Limites de uso

| Rota | Limite por usuário |
|---|---|
| `POST /v1/mappings` | 30/min |
| `POST /v1/writing/answers` | 20/min |
| `POST /v1/job-analyses` | 10/min |
| `POST /v1/resume-imports` | 10/h |
| Autenticação | 5 tentativas/min por IP e por conta |

## 9. Testes

| Nível | O que cobre | Ferramenta |
|---|---|---|
| Unitário (pacotes) | Regras de campo, `pickOption`, `alignSlots`, `adaptValue`, achatamento, `profileFacts`, bloqueios | Vitest, casos tabulares |
| Serviço | `mapping`, `writing`, `job-analysis` com `LLMProvider` falso (respostas fixadas, inclusive maliciosas e inválidas) | Vitest |
| Integração | Repositórios e migrações num PostgreSQL real | Vitest + Testcontainers |
| Contrato | Schemas de request/response da API v1 contra os tipos de `api-contract`; teste que falha se `POST /v1/mappings` devolver valor de campo não enviado | Vitest |
| Segurança | Autenticação, isolamento entre usuários (IDOR), CORS, limites | Vitest + suíte dedicada |
| Avaliação de IA | Casos fixos de de-para, texto livre, extração e evidência | Ver [estratégia de testes](../04-qualidade/estrategia-de-testes.md) |

Critérios de aceite por requisito: [criterios-de-aceite-mvp1.md](../04-qualidade/criterios-de-aceite-mvp1.md) (CA-NN corresponde a RF-NN).
