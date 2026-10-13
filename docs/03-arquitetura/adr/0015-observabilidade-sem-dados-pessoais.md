# ADR-0015 — Observabilidade sem dados pessoais

| Campo | Valor |
|---|---|
| Status | Proposta |
| Data | 12/10/2026 |
| Decisores | Arquiteto de software, Responsável por privacidade |
| Consultados | Dev backend, Dev extensão, Arquiteto de IA, QA |
| Requisitos | RF-20; RNF-04, RNF-07, RNF-09, RNF-10, RNF-12 |
| Relacionados | ADR-0005, ADR-0011, ADR-0014 |

## Contexto

O Zyft processa currículos, perfis e o conteúdo de formulários de candidatura. Logs técnicos costumam vazar dados pessoais por acidente: corpo da requisição em erro, prompt completo do LLM em modo de depuração, exceções com o valor do campo.

O protótipo mistura duas coisas no mesmo `store.log()`:

- a **auditoria para a pessoa** (RF-20), com eventos como `perfil.atualizado`, `campos.mapeados`, `vaga.analisada` e `candidatura.registrada`. Os detalhes incluem URL, cargo da vaga e títulos de perguntas aprovadas;
- informações técnicas, como a duração do mapeamento (`ms`).

Erros vão para `console.error(e)`, que pode incluir trechos da resposta do LLM (ex.: "A IA local retornou um JSON inválido: <200 caracteres do conteúdo>").

O MVP 1 precisa de métricas de operação: latência (RNF-04), disponibilidade da IA (RNF-12) e taxa de reconhecimento do conector (RNF-10).

## Fatores de decisão

- LGPD: minimização e finalidade (RNF-07).
- Diagnóstico de problemas em produção.
- Medir qualidade da IA e do conector sem ver o conteúdo.
- Separar o que é para a pessoa (auditoria) do que é para o time (telemetria).

## Opções consideradas

### Opção A — Logar tudo e restringir o acesso

- **Prós:** diagnóstico fácil.
- **Contras:** dados pessoais espalhados em ferramentas de terceiros; retenção difícil de controlar; viola a minimização.

### Opção B — Não ter observabilidade além do básico

- **Prós:** risco mínimo.
- **Contras:** sem como cumprir e verificar RNF-04, RNF-10 e RNF-12; incidentes viram adivinhação.

### Opção C — Dois canais separados, com lista de campos permitidos na telemetria

- **Prós:** auditoria completa para a pessoa, guardada no banco dela e exportável; telemetria útil para o time sem conteúdo pessoal; regra verificável por teste.
- **Contras:** depurar um caso específico exige reprodução com dados fictícios ou consentimento da pessoa; disciplina em todo o código.

## Decisão (proposta)

Adotar a **Opção C**:

1. **Auditoria da pessoa (RF-20)** — tabela `registro_acao`:
   - **Campos:** data, ação (vocabulário fechado), portal (domínio), quantidade de campos por estado e origem.
   - **Sem valores:** nenhum valor preenchido, nenhum texto gerado e nenhum dado sensível.
   - **Acesso:** visível na plataforma, incluída na exportação e apagada com a conta.
2. **Telemetria técnica** (logs estruturados, métricas, traces via OpenTelemetry):
   - **Lista de campos permitidos** (*allowlist*): `request_id`, rota, status, latência, `usuario_id` pseudonimizado (HMAC), domínio do portal, versão do conector, versão da extensão, tarefa e versão do prompt, modelo, tokens de entrada e saída, taxa de JSON válido, contagens por estado.
   - **Proibido:** corpo de requisição ou resposta, rótulos e valores de campos, conteúdo de prompts e respostas do LLM, nome, e-mail, telefone, CPF, URL com query string, texto de currículo ou de vaga.
   - **Mensagens de erro:** o logger remove o conteúdo; o erro registra tipo e código, não o texto do modelo.
3. **Depuração com conteúdo:** só em ambiente de desenvolvimento, com dados fictícios. Em produção, só com consentimento explícito e pontual da pessoa (ex.: "Enviar este formulário para análise do time"), com retenção de 30 dias.
4. **Retenção:** telemetria por 30 dias; auditoria conforme a política de privacidade (proposta: 12 meses ou até a exclusão da conta).
5. **Extensão:** sem SDK de analytics de terceiros. Os eventos da extensão vão para a API do Zyft seguindo a mesma lista permitida.

## Consequências

**Positivas**
- RNF-09 vira verificável.
- Métricas de qualidade da IA e do conector sem expor conteúdo.
- Exportação e exclusão (RF-08) cobrem toda a informação da pessoa, que fica num lugar só.

**Negativas**
- Investigar um bug específico de um portal fica mais trabalhoso: depende de fixtures e reprodução.
- O logger customizado precisa ser usado em todo lugar (sem `console.log` no código de produção).

**Riscos e mitigação**
- **Alguém loga o objeto de requisição inteiro.** Mitigação: regra de lint proibindo `console.*` fora do logger; o logger serializa só a lista permitida; teste de "canário".

## Conformidade

- **Teste de canário:** a suíte E2E usa um perfil com valores marcadores únicos (ex.: `ZYFT_CANARIO_7f3a`) e falha se algum marcador aparecer nos logs ou na telemetria capturados.
- Lint: proíbe `console.*` em `apps/api` e `apps/extension` (exceto no logger).
- Revisão de privacidade antes de adicionar qualquer campo à lista permitida.

## Links

- [Segurança](../seguranca.md), [Operação](../../06-operacao/)
- Protótipo: `server/src/store.js` (`log`, `listLogs`), `server/src/index.js` (`store.log`, `console.error`), `server/src/llm.js`
