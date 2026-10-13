# ADR-0008 — PostgreSQL com Perfil Mestre em JSONB

| Campo | Valor |
|---|---|
| Status | Proposta |
| Data | 12/10/2026 |
| Decisores | Arquiteto de software |
| Consultados | Dev backend, Responsável por privacidade |
| Requisitos | RF-02, RF-04, RF-06, RF-07, RF-08, RF-20; RNF-07 |
| Relacionados | ADR-0003, ADR-0007, ADR-0012, ADR-0014 |

## Contexto

O protótipo guarda tudo num arquivo `server/data/db.json` (`store.js`):

- `apiToken`, `consents`, `profile`, `applications` e `logs` (limitado aos 500 últimos);
- a escrita é atômica por arquivo temporário e `rename`.

Funciona para um usuário só, mas não tem concorrência, consulta, backup, criptografia nem multiusuário.

O Perfil Mestre é um documento hierárquico com listas de tamanho variável (experiências, formação, cursos, certificações, idiomas, respostas aprovadas). `normalizeProfile()` garante a forma.

O MVP 1 precisa de várias contas, de consentimentos, de auditoria por usuário, de exclusão completa (RF-08) e de dados sensíveis segregados ([ADR-0014](0014-dados-sensiveis-segregados-e-criptografados.md)).

## Fatores de decisão

- O perfil muda de forma com frequência no começo do produto (campos novos descobertos no portal).
- Integridade e transações para conta, consentimento e auditoria.
- Exclusão completa e exportação simples (LGPD).
- Familiaridade do time: a documentação do marketplace já previa PostgreSQL.
- Custo e operação baixos.

## Opções consideradas

### Opção A — Manter arquivos JSON (um por usuário)

- **Prós:** zero migração.
- **Contras:** sem transação, concorrência ou backup consistente; não escala; criptografia e auditoria manuais.

### Opção B — PostgreSQL totalmente relacional

Uma tabela por seção do perfil (experiência, formação, curso…).

- **Prós:** integridade forte; consultas por qualquer campo.
- **Contras:** cada campo novo exige migração; o código de mapeamento já trabalha com o documento achatado (`flattenProfile`), então a modelagem relacional só adiciona tradução; o MVP 1 não consulta dentro do perfil.

### Opção C — PostgreSQL com o perfil em JSONB e o restante relacional

- **Prós:** flexibilidade do documento para o perfil, validado por schema Zod na aplicação; tabelas relacionais para usuários, consentimentos, concessões da extensão, respostas aprovadas, auditoria e conclusões; transações; um banco só.
- **Contras:** a integridade interna do perfil depende da validação da aplicação, não do banco; consultas analíticas dentro do perfil ficam menos eficientes (aceitável no MVP 1).

### Opção D — Banco de documentos (MongoDB)

- **Prós:** modelo natural para o perfil.
- **Contras:** um segundo paradigma para conta e auditoria, que são relacionais; menos familiaridade do time; sem ganho real sobre JSONB.

## Decisão (proposta)

Adotar a **Opção C**: PostgreSQL 16 gerenciado.

| Tabela | Conteúdo |
|---|---|
| `usuario` | Conta, e-mail, estado, datas |
| `perfil_mestre` | `usuario_id`, `dados JSONB` (sem sensíveis), `versao_schema`, `atualizado_em` |
| `dado_sensivel` | Cifrado em nível de campo ([ADR-0014](0014-dados-sensiveis-segregados-e-criptografados.md)) |
| `resposta_aprovada` | Pergunta, texto, data |
| `consentimento` | Finalidade, estado, data, versão do termo |
| `concessao_extensao` | Pareamentos e refresh tokens com hash ([ADR-0007](0007-autenticacao-e-pareamento-da-extensao.md)) |
| `registro_acao` | Auditoria visível à pessoa, sem valores |
| `conclusao_candidatura` | Só se o [ADR-0012](0012-registro-minimo-de-conclusao-no-mvp1.md) for aceito com registro mínimo |

- Todas as tabelas de dados pessoais têm `ON DELETE CASCADE` a partir de `usuario`, de modo que excluir a conta apaga tudo numa transação.
- `perfil_mestre.versao_schema` permite migrar documentos antigos na leitura (como `normalizeProfile` faz hoje).
- Acesso a dados com Kysely e migrações versionadas pelo Kysely migrator, no repositório; fila de jobs com pg-boss no mesmo PostgreSQL (sem Redis).

## Consequências

**Positivas**
- O perfil pode evoluir sem migração de banco para cada campo novo.
- A exclusão (RF-08) e a exportação são triviais de implementar e testar.
- A auditoria e os consentimentos têm integridade relacional.

**Negativas**
- Validação do perfil obrigatoriamente na aplicação (`packages/profile-schema`).
- Busca dentro do perfil exige índices GIN se um dia for necessária (MVP 2, para matching).

**Riscos e mitigação**
- **Documento de perfil inválido gravado.** Mitigação: validação Zod na escrita e teste de contrato.
- **Crescimento da auditoria.** Mitigação: retenção definida na política de privacidade (proposta: 12 meses) e particionamento por mês, se necessário.

## Conformidade

- Teste de integração: excluir o usuário apaga linhas em todas as tabelas com `usuario_id`.
- Nenhuma coluna de `perfil_mestre.dados` contém as chaves de `sensitive` (teste de schema).
- Backups diários com restauração testada antes do beta.

## Links

- [Modelo de dados](../modelo-de-dados.md)
- Protótipo: `server/src/store.js`, `server/src/profile.js` (`emptyProfile`, `normalizeProfile`)
