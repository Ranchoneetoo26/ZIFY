# ADR-0003 — Monorepo TypeScript/Node.js para API, web e extensão

| Campo | Valor |
|---|---|
| Status | Proposta |
| Data | 12/10/2026 |
| Decisores | Arquiteto de software |
| Consultados | Dev backend, Dev frontend, Dev extensão, QA |
| Requisitos | RF-02, RF-12, RNF-04, RNF-10 |
| Relacionados | ADR-0002, ADR-0006, ADR-0008; substitui a stack do marketplace (`docs/arquivo/marketplace-v1/arquitetura-backend.md`) |

## Contexto

A documentação do marketplace definia backend em **C# (.NET 8/9) com Minimal APIs** e PostgreSQL. Nada foi implementado nessa stack.

O protótipo da Candidatura Assistida usa **Node.js 22 (ESM) com Express**: `mammoth` (DOCX), `pdf-parse`, `multer` e chamadas HTTP ao Ollama. A extensão é JavaScript puro e a plataforma web é HTML com JavaScript sem framework.

O núcleo do produto é o **de-para de campos** (`server/src/mapper.js`). Ele trabalha sobre o mesmo modelo de dados que a extensão produz (`content.js`: id, rótulo, seção, tipo, opções, valor atual) e sobre o mesmo Perfil Mestre que a web edita (`profile.js`). Hoje esses formatos são implícitos e duplicados entre três bases de código.

## Fatores de decisão

- Um único modelo de tipos para campo, perfil e mapeamento, compartilhado pelos três componentes.
- Reaproveitar o protótipo funcional, sem reescrever do zero.
- Time pequeno, sem especialistas dedicados em várias linguagens.
- A extensão obrigatoriamente é JavaScript/TypeScript.
- Ecossistema maduro para PDF/DOCX, LLM e testes E2E de extensão (Playwright).

## Opções consideradas

### Opção A — Manter C#/.NET 8 no backend (decisão do marketplace)

- **Prós:** tipagem forte e desempenho; quem escreveu a documentação antiga conhece a stack; bom suporte a PostgreSQL.
- **Contras:** reescreve todo o protótipo; os tipos de campo e perfil ficam duplicados entre C# e TypeScript da extensão; as regras determinísticas não podem rodar na extensão; duas cadeias de build e de teste.

### Opção B — Python (FastAPI) no backend

- **Prós:** ecossistema forte de IA e de extração de documentos.
- **Contras:** os mesmos problemas de duplicação da Opção A; o protótipo não usa nada que exija Python; o LLM é acessado por HTTP (Ollama/vLLM), então a vantagem do ecossistema é pequena.

### Opção C — Monorepo TypeScript em Node.js (pnpm workspaces)

- **Prós:**
  - tipos compartilhados (`packages/profile-schema` e `packages/api-contract`) entre API, web e extensão;
  - as regras determinísticas do de-para (`packages/field-rules`) podem rodar na API e, se necessário, na extensão, com o mesmo teste;
  - migração incremental do protótipo (JS para TS arquivo a arquivo);
  - uma única cadeia de lint, teste e build.
- **Contras:**
  - tipagem só em tempo de compilação, exigindo validação em runtime (Zod) nas fronteiras;
  - Node é single-thread, então extração de PDF pesada precisa de worker;
  - o time precisa padronizar o tooling (pnpm, tsconfig, ESLint).

## Decisão (proposta)

Adotar a **Opção C**: monorepo TypeScript (Node 22) com pnpm workspaces e Turborepo; Vitest e Playwright para testes.

```
apps/
  api/            Fastify + Zod, módulos de domínio, worker (entrypoint separado)
  web/            plataforma do Perfil Mestre (React 18 + Vite)
  extension/      Manifest V3 (service worker, painel lateral, content script)
packages/
  profile-schema/ esquema Zod do Perfil Mestre, achatamento, rótulos, fatos
  field-rules/    regras determinísticas do de-para (TOP_RULES, GROUPS, alinhamento de blocos, bloqueios)
  connectors/     conectores por portal: detecção de telas, regras e fixtures (ADR-0011)
  llm/            porta LLMProvider, adaptadores, prompts versionados, schemas de saída (ADR-0005)
  api-contract/   tipos de request/response da API v1, gerados dos schemas Zod
  ui/             design tokens e componentes compartilhados entre web e painel lateral
  test-fixtures/  perfis sintéticos, páginas de portal gravadas, casos de avaliação de IA
ferramentas/      scripts (pesquisa, migrações)
```

A árvore detalhada está no [DAT §5.3](../dat.md#53-pacotes-compartilhados).

Framework da API: **Fastify** (validação por schema nativa e desempenho), em vez de manter o Express do protótipo; a migração é pequena (cerca de 150 linhas de rotas). Persistência conforme o [ADR-0008](0008-postgresql-com-perfil-em-jsonb.md) (PostgreSQL 16, Kysely, fila pg-boss).

## Consequências

**Positivas**
- Uma mudança no formato do campo quebra a compilação dos três componentes ao mesmo tempo, não em produção.
- A suíte de regras do de-para é única e roda no CI sobre fixtures do portal.
- O protótipo vira ponto de partida real.

**Negativas**
- A documentação de backend do marketplace (C#) fica sem uso no MVP 1. Se o MVP 2 justificar outra linguagem para algum serviço, isso será um novo ADR.
- Custo inicial de configurar o monorepo (estimado em 2–3 dias).

**Riscos e mitigação**
- **Dependências inseguras do npm.** Mitigação: lockfile, `pnpm audit` no CI, Renovate/Dependabot.
- **Extração de PDF bloqueando o event loop.** Mitigação: worker thread ou fila para importação.

## Conformidade

- CI roda `tsc --noEmit`, lint e testes em todos os pacotes.
- Fronteiras (HTTP, mensagens da extensão, saída do LLM) validadas com os schemas de `packages/profile-schema` e `packages/api-contract`.
- Nenhum tipo de domínio é declarado fora de `packages/profile-schema` e `packages/api-contract`.

## Links

- [DAT](../dat.md), [Backend](../backend.md)
- Protótipo: `server/package.json`, `server/src/*.js`
