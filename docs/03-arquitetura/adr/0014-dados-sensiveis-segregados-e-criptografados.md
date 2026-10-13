# ADR-0014 — Dados sensíveis segregados e criptografados

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 12/10/2026 |
| Decisores | Arquiteto de software, Responsável por privacidade |
| Consultados | Product Owner, Arquiteto de IA, Dev backend |
| Requisitos | RF-03, RF-07, RF-18; RNF-07, RNF-09; RN-04 |
| Relacionados | ADR-0004, ADR-0008, ADR-0010, ADR-0015 |

## Contexto

Formulários de vagas frequentemente pedem **dados pessoais sensíveis** (LGPD, art. 5º, II), como gênero, raça/etnia, deficiência (PcD), religião e saúde, além de dados que, mesmo não sendo sensíveis pela lei, exigem cuidado: estado civil, filhos, gestação, nome social.

No protótipo:

- **Perfil:** os dados ficam em `profile.sensitive` (`gender`, `ethnicity`, `disability`, `notes`), fora das chaves usadas pelo de-para (`flattenProfile` não inclui `sensitive`).
- **Consentimento:** `consents.sensitiveData` começa como `false`.
- **De-para (`mapper.js`):** detecta campos sensíveis pela expressão `SENSITIVE`. Só sugere o valor com consentimento, sempre com `selected: false`, e a pessoa precisa marcar.
- **Importação (`importer.js`):** instrui o LLM a não extrair dados sensíveis do currículo.
- **IA (`llm.js`):** o prompt de sistema proíbe inferir dados sensíveis.
- **Armazenamento:** em texto puro no `db.json`.

## Fatores de decisão

- Conformidade com a LGPD (art. 11: consentimento específico e destacado para dados sensíveis).
- Risco de discriminação se os dados vazarem ou forem usados indevidamente.
- Minimização: o Zyft não precisa de dados sensíveis para nada além de repassá-los, quando a pessoa quiser, a um formulário que os pede.
- O LLM não pode recebê-los (nem para "ajudar").

## Opções consideradas

### Opção A — Não armazenar dados sensíveis

A pessoa sempre preenche manualmente.

- **Prós:** risco mínimo.
- **Contras:** retira uma conveniência que algumas pessoas querem (ex.: pessoas PcD que respondem a mesma pergunta em todos os formulários, muitas vezes para concorrer a vagas afirmativas).

### Opção B — Armazenar junto com o perfil, com a mesma proteção

- **Prós:** simples.
- **Contras:** os dados entram no fluxo do de-para e da IA por acidente; o vazamento do perfil expõe os sensíveis.

### Opção C — Segregar, criptografar em nível de campo, fora da IA, com consentimento específico

- **Prós:** atende a LGPD; o vazamento do banco sem a chave não expõe os sensíveis; separação física impede o uso acidental pela IA.
- **Contras:** gestão de chaves; mais código.

## Decisão

Escolhemos a **Opção C**:

1. Os dados sensíveis ficam numa tabela própria (`dado_sensivel`), **criptografados em nível de campo** (AES-256-GCM, com chave de dados por usuário cifrada por uma chave mestra num KMS ou cofre de segredos). Nunca ficam no JSONB do perfil ([ADR-0008](0008-postgresql-com-perfil-em-jsonb.md)).
2. O armazenamento é **opcional**. A plataforma explica a finalidade e pede o consentimento (c), específico e destacado, desligado por padrão (RF-07).
3. Dados sensíveis **nunca** são enviados ao LLM: não entram em `profileFacts`, no achatamento do perfil, nos prompts, nem na análise de vaga ou no texto livre.
4. Na importação de currículo, o texto não é usado para extrair sensíveis. Se o modelo devolver algo nessas chaves, o valor é descartado antes de chegar à proposta.
5. No de-para, o campo sensível sempre aparece como "ação sua". Com consentimento, o painel **sugere** o valor guardado, mas desmarcado; a pessoa marca o campo para preencher (RN-04).
6. A API devolve à extensão o valor sensível apenas do campo específico, apenas quando a pessoa marcou o campo, e apenas naquela requisição (RNF-02).
7. Os dados sensíveis nunca aparecem em logs, métricas ou registro de ações ([ADR-0015](0015-observabilidade-sem-dados-pessoais.md)). O registro de ações diz "1 dado sensível preenchido por você", sem o valor.
8. Desligar o consentimento (c) apaga os dados sensíveis armazenados.

## Consequências

**Positivas**
- Atende o art. 11 da LGPD com finalidade clara e consentimento destacado.
- Reduz o impacto de um vazamento do banco.

**Negativas**
- Custo de gestão de chaves e rotação.
- Mais um caminho de código para testar.

**Riscos e mitigação**
- **Uma pergunta sensível não detectada pela expressão regular** (ex.: "Você se autodeclara…"). Mitigação: a lista `SENSITIVE` é revisada a cada conector; os conectores marcam explicitamente os campos sensíveis do portal; os evals incluem casos.
- **Perda da chave mestra.** Mitigação: o KMS gerenciado com backup; a perda afeta só dados opcionais, que a pessoa pode digitar de novo.

## Conformidade

- Teste: `profileFacts` e `flattenProfile` nunca retornam chaves de `sensitive`.
- Teste de prompt: nenhuma chamada ao `LLMProvider` contém os valores sensíveis da fixture (verificação por *spy*).
- Teste do de-para: um campo sensível vem `selected: false` em qualquer configuração.
- Teste de banco: a coluna de `dado_sensivel` não contém texto puro.
- RIPD (relatório de impacto) do MVP 1 cobre esse tratamento.

## Links

- [Segurança](../seguranca.md), [Política de privacidade](../../07-legal/politica-de-privacidade.md)
- Protótipo: `server/src/profile.js` (`sensitive`), `server/src/mapper.js` (`SENSITIVE`, `sensitiveValue`), `server/src/importer.js`, `server/src/store.js` (`consents`)
