# ADR-0017 — Backend em C#/.NET 8 com Clean Architecture

| Campo | Valor |
|---|---|
| Status | Proposta |
| Data | 15/10/2026 |
| Decisores | Arquiteto de software, Product Owner |
| Consultados | Dev backend, Dev frontend, Dev extensão, QA, Responsável por privacidade |
| Requisitos | RF-01 a RF-20; RNF-02, RNF-03, RNF-04, RNF-09, RNF-12 |
| Relacionados | Substitui o [ADR-0003](0003-monorepo-typescript-node.md). Ajusta a persistência do [ADR-0008](0008-postgresql-com-perfil-em-jsonb.md) e o pareamento do [ADR-0007](0007-autenticacao-e-pareamento-da-extensao.md). Mantém [ADR-0004](0004-ia-aponta-chave-nunca-escreve-valor.md), [ADR-0005](0005-modelo-open-weight-self-hosted-atras-de-porta-llm.md), [ADR-0006](0006-regras-deterministicas-antes-da-ia.md), [ADR-0009](0009-nunca-enviar-confirmacao-explicita.md), [ADR-0010](0010-sem-credenciais-sem-contorno-de-seguranca-campos-bloqueados.md), [ADR-0014](0014-dados-sensiveis-segregados-e-criptografados.md) e [ADR-0015](0015-observabilidade-sem-dados-pessoais.md). |

## Contexto

O [ADR-0003](0003-monorepo-typescript-node.md) propôs um monorepo TypeScript/Node.js para API, web e extensão, aproveitando o protótipo em Express (`server/src/*.js`). Ele nunca passou de **Proposta** e nenhum código foi escrito nessa stack.

O Zyft é desenvolvido como projeto da disciplina **Tópicos Especiais em Engenharia de Software** (Unicive, 6º semestre). A ementa exige que a API seja feita em **C#/.NET** e que use: Clean Architecture, DDD, SOLID, CQRS com MediatR, FluentValidation, Entity Framework Core com Migrations, injeção de dependência, middleware global de exceções, JWT com refresh token e policies, xUnit, FluentAssertions, Moq, Reqnroll (BDD), Serilog, Health Checks, Docker e GitHub Actions. As entregas 2, 3 e 4 (18/10, 25/10 e 06/11/2026) são avaliadas sobre essa stack.

O protótipo continua sendo a referência de **comportamento** (de-para em `mapper.js`, análise de vaga em `jobs.js`, redação em `writer.js`, importação em `importer.js`), não de tecnologia.

## Fatores de decisão

- Exigência da disciplina: API em C#/.NET com as práticas listadas acima.
- Manter as regras inegociáveis do produto (nunca enviar, IA só aponta chave, sensíveis fora da IA, sem dados pessoais em log).
- Time pequeno, prazo curto (três semanas até a E4).
- A extensão continua obrigatoriamente em JavaScript (Manifest V3).
- Testabilidade das regras de negócio sem banco nem IA.

## Opções consideradas

### Opção A — Manter o ADR-0003 (TypeScript/Node.js)

- **Prós:** reaproveita o protótipo quase sem tradução; tipos compartilhados com a extensão.
- **Contras:** não atende à exigência da disciplina, o que inviabiliza as entregas avaliadas.

### Opção B — .NET 8 com Minimal APIs em um único projeto

- **Prós:** menos arquivos; rápido para começar.
- **Contras:** mistura HTTP, regras e acesso a dados; não demonstra Clean Architecture, CQRS nem a separação de camadas pedidas na ementa; regras ficam difíceis de testar isoladamente.

### Opção C — .NET 8 com Clean Architecture em quatro projetos, CQRS com MediatR e EF Core

- **Prós:** atende à ementa; dependências apontam para o domínio; regras de negócio (de-para, consentimento, rotação de tokens) testáveis com Moq, sem banco e sem IA; contratos explícitos por DTOs; EF Core com Migrations versiona o banco.
- **Contras:** mais projetos e mais código de cerimônia (commands, handlers, validators); o comportamento do protótipo precisa ser traduzido de JavaScript para C#; os tipos de campo e perfil ficam duplicados entre C# e a extensão (mitigado por testes de contrato).

## Decisão (proposta)

Adotar a **Opção C**.

```
Zyft.sln
src/Zyft.Domain/          entidades, value objects, enums e regras puras (sem dependências externas)
src/Zyft.Application/     commands/queries/handlers (MediatR), DTOs, validators (FluentValidation),
                          portas (IRepositórios, IUnitOfWork, ILlmClient, IPasswordHasher, ITokenService,
                          ICurrentUser, IResumeTextExtractor, IClock) e serviços FieldMapper, JobAnalyzer, AnswerWriter
src/Zyft.Infrastructure/  EF Core (ZyftDbContext, configurações Fluent API, Migrations, repositórios, UnitOfWork),
                          OllamaLlmClient, BCrypt, JWT, extração PDF/DOCX, criptografia AES dos sensíveis
src/Zyft.Api/             controllers, middleware de exceções (ProblemDetails), autenticação JWT, policies,
                          Swagger, Serilog, Health Checks, CORS, rate limiting, wwwroot (frontend web)
tests/Zyft.UnitTests/         xUnit + FluentAssertions + Moq
tests/Zyft.IntegrationTests/  WebApplicationFactory + SQLite em memória
tests/Zyft.AcceptanceTests/   Reqnroll (Gherkin em português)
web/        frontend web (HTML, CSS e JavaScript sem build, servido pela API)
extension/  extensão Manifest V3
```

Regras obrigatórias:

- **Dependências:** `Domain` não referencia nenhum outro projeto; `Application` referencia só `Domain`; `Infrastructure` implementa as portas da `Application`; `Api` compõe tudo por injeção de dependência. Controllers não acessam o `DbContext`.
- **CQRS:** cada caso de uso é um command ou query do MediatR; validação por `ValidationBehavior` (FluentValidation) antes do handler.
- **Persistência:** PostgreSQL 16 (Npgsql) com EF Core e Migrations. O Perfil Mestre passa a ser **relacional** (uma tabela por seção), o que ajusta a Opção C do [ADR-0008](0008-postgresql-com-perfil-em-jsonb.md): o objetivo acadêmico de evidenciar entidades, chaves e relacionamentos pesa mais que a flexibilidade do JSONB. SQLite é aceito para desenvolvimento sem Docker e para testes de integração (`Database:Provider`).
- **Autenticação:** JWT HS256 de 15 minutos e refresh token opaco com rotação. O pareamento da extensão usa o código curto do fluxo alternativo do [ADR-0007](0007-autenticacao-e-pareamento-da-extensao.md), gerado na plataforma e digitado na extensão; o fluxo PKCE fica para depois do MVP 1 acadêmico.
- **IA:** a porta `ILlmClient` substitui a `LLMProvider` do [ADR-0005](0005-modelo-open-weight-self-hosted-atras-de-porta-llm.md), com o mesmo contrato de comportamento: a IA devolve só chaves do perfil e JSON validado por schema; se estiver fora, as regras seguem funcionando.
- **Erros:** respostas de erro sempre em ProblemDetails (RFC 9457) com mensagens em português.
- Os pacotes e a estrutura `apps/`/`packages/` do ADR-0003 deixam de valer.

## Consequências

**Positivas**
- Atende à disciplina sem mudar o produto: as regras de confiança e os requisitos continuam os mesmos.
- O domínio e os casos de uso ficam testáveis isoladamente (xUnit + Moq), e as regras inegociáveis ganham testes de aceitação legíveis (Reqnroll).
- O esquema do banco é versionado por Migrations e documentado por entidade.

**Negativas**
- O protótipo em Node.js precisa ser portado para C#. O comportamento é mantido; o código não é reaproveitado.
- A extensão (JavaScript) e a API (C#) não compartilham tipos. Mudanças no contrato exigem atualizar os dois lados.
- O perfil relacional exige migração para cada campo novo.

**Riscos e mitigação**
- **Divergência de contrato entre API e extensão** → testes de integração sobre os DTOs de `POST /api/v1/forms/mappings` e revisão obrigatória dos dois lados no mesmo PR.
- **Prazo curto para portar o de-para** → portar primeiro as regras determinísticas e os bloqueios (cobertos por testes unitários), depois a etapa de IA.
- **Duplicação de regras entre frontend e backend** → o backend é a fonte da verdade; o frontend só repete validações simples para evitar idas e voltas.

## Conformidade

- CI (GitHub Actions) roda `dotnet build` e `dotnet test` nas três suítes a cada PR.
- Teste de arquitetura (ou revisão de PR) garante que `Zyft.Domain` não referencia outros projetos e que `Zyft.Application` não referencia `Infrastructure` nem `Api`.
- Nenhum controller injeta `ZyftDbContext`; todo caso de uso passa por `IMediator`.
- Testes de aceitação Reqnroll para: nunca enviar, IA não escreve valor objetivo, sensível bloqueado sem consentimento.

## Links

- [Entrega 2 — Projeto e Arquitetura](../../entregas/entrega-2-projeto-e-arquitetura.md)
- [ADR-0003](0003-monorepo-typescript-node.md) (substituído)
- Protótipo de referência: `server/src/*.js` e `extension/*` (fora do repositório)
