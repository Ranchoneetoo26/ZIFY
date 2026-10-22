# ADR-0005 — Modelo open-weight atrás de uma porta `LLMProvider`

| Campo | Valor |
|---|---|
| Status | **Aceita** (porta `LLMProvider` e uso de Ollama em desenvolvimento) / **Proposta** (hospedagem de produção) |
| Data | 12/10/2026 |
| Decisores | Arquiteto de IA, Arquiteto de software |
| Consultados | Responsável por privacidade (LGPD), CEO (custo), Dev backend |
| Requisitos | RF-03, RF-12, RF-15, RF-17; RNF-04, RNF-05, RNF-07, RNF-12 |
| Relacionados | ADR-0004, ADR-0013, ADR-0015 |

## Contexto

O protótipo usa **IA local via Ollama** (`server/src/llm.js`):

- **Modelo e configuração:** `qwen2.5:7b` (~4,7 GB), `temperature` 0,1, `num_ctx` 8192.
- **Formato:** saída sempre em JSON com schema (`format: schema`).
- **Variáveis:** `OLLAMA_URL` e `OLLAMA_MODEL`.
- **Desempenho medido:** roda numa GPU de 12 GB. Cada análise de vaga leva **20–30 s**, porque são várias chamadas, uma por requisito.
- **Determinismo:** baixo. O resultado da mesma vaga variou entre "3 de 4" e "5 de 5" requisitos atendidos.

Os dados enviados ao modelo são **currículos e perfis profissionais**: dados pessoais com nome, contato, histórico de trabalho e formação. Em produção, o modelo precisa atender várias pessoas ao mesmo tempo, o que uma GPU no computador do desenvolvedor não faz.

## Fatores de decisão

- **Privacidade:** dados de currículo não devem ser usados para treinar modelos de terceiros nem sair do país sem base legal (RNF-07).
- **Custo** por pessoa ativa, num produto ainda sem receita.
- **Latência** compatível com RNF-04 (de-para com IA p95 ≤ 10 s; texto livre ≤ 15 s; vaga ≤ 30 s).
- **Qualidade:** seguir schema JSON e não inventar (RNF-05).
- **Trocabilidade:** não acoplar o código a um fornecedor.

## Opções consideradas

### Opção A — Chamar o Ollama direto no código (protótipo)

- **Prós:** simples; já funciona.
- **Contras:** acopla prompts e chamadas a um único servidor; não escala para muitos usuários; sem métricas nem limites.

### Opção B — Porta `LLMProvider` com adaptadores

Interface única, por exemplo `gerarJSON({ tarefa, prompt, schema, temperatura }) → objeto validado`, com adaptadores para Ollama, vLLM (API compatível com OpenAI) e um provedor gerenciado.

- **Prós:** trocar o modelo ou o provedor vira configuração; permite comparar modelos na suíte de avaliação; centraliza timeouts, retries, métricas e validação de schema.
- **Contras:** uma camada a mais; recursos específicos de um provedor (ex.: cache de prompt) precisam passar pela abstração.

### Hospedagem de produção — B1: vLLM com modelo open-weight em GPU própria ou alugada

- **Prós:** os dados não saem da infraestrutura do Zyft; custo fixo previsível; mesmo modelo do desenvolvimento (Qwen 2.5 7B/14B, Llama 3.1 8B); suporta *structured outputs*.
- **Contras:** custo fixo mesmo sem uso (uma GPU de 24 GB custa a partir de algumas centenas de dólares por mês em nuvem); o time precisa operar GPU; a qualidade de modelos 7–14B é menor que a dos grandes modelos gerenciados.

### Hospedagem de produção — B2: API gerenciada de LLM com acordo de tratamento de dados (DPA)

- **Prós:** qualidade e velocidade superiores (menos erros de classificação, como o "parcial" em vez de "atende" visto no protótipo); custo por uso, perto de zero sem tráfego; nada para operar.
- **Contras:** os dados de currículo saem para um terceiro, o que exige DPA, retenção zero, região e transferência internacional tratadas na política de privacidade; dependência de fornecedor; custo variável com o uso.

## Decisão

**Aceito:**

1. Todo uso de LLM passa pela porta `LLMProvider` (`packages/llm`, [ADR-0003](0003-monorepo-typescript-node.md)). Nenhum outro módulo chama HTTP de modelo diretamente.
2. A porta valida a saída contra o schema da tarefa e trata JSON inválido como erro recuperável (uma nova tentativa, depois falha controlada), não como texto livre.
3. Desenvolvimento e testes usam o **adaptador Ollama** com `qwen2.5:7b` como modelo de referência.
4. Os prompts são versionados por tarefa (`mapear-campos@v1`, `texto-livre@v1`, `extrair-curriculo@v1`, `extrair-vaga@v1`, `avaliar-requisito@v1`). Cada versão é avaliada na suíte de evals antes de entrar em produção.
5. Dados sensíveis nunca são enviados ao LLM ([ADR-0014](0014-dados-sensiveis-segregados-e-criptografados.md)).

**Proposto (a decidir até o fim do spike de hospedagem):** em produção, usar **B1 (vLLM self-hosted com modelo open-weight)** como padrão, por privacidade e coerência com o desenvolvimento. **B2** fica como alternativa se o custo fixo da GPU for inviável ou se a qualidade medida nos evals ficar abaixo das metas de RNF-05. Adotar B2 exige DPA com retenção zero e atualização da política de privacidade **antes** do uso.

Critérios do spike:

| Critério | Meta |
|---|---|
| Taxa de JSON válido | ≥ 99% |
| Valores objetivos fora do perfil | 0 |
| p95 do de-para com IA | ≤ 10 s |
| Concordância com o gabarito na análise de vaga | Não inferior à linha de base do `qwen2.5:7b` no Ollama (limiares em [estratégia de testes §5.2](../../04-qualidade/estrategia-de-testes.md#62-métricas-e-limiares)) |
| Custo mensal com 200 pessoas ativas | Estimado e aprovado pelo CEO |

## Consequências

**Positivas**
- Comparar modelos e provedores vira um experimento, não uma reescrita.
- Os dados ficam, por padrão, dentro da infraestrutura do Zyft.
- Métricas de latência e taxa de JSON inválido ficam num lugar só.

**Negativas**
- Mesmo com a porta, trocar de modelo muda o comportamento: toda troca passa pelos evals.
- O self-hosting traz custo operacional de GPU e de plantão.

**Riscos e mitigação**
- **Latência da análise de vaga (20–30 s hoje).** Mitigação: avaliar todos os requisitos numa chamada só ou em paralelo, com streaming de progresso no painel; RF-17 é *Should*, então pode ficar fora se não atingir a meta.
- **Não determinismo.** Mitigação: `temperature` baixa, evidência por linha ([ADR-0013](0013-evidencia-por-linha-numerada-na-analise-de-vaga.md)), revisão humana.
- **IA fora do ar.** Mitigação: o de-para degrada para só regras (RNF-12), com aviso no painel.

## Conformidade

- Lint ou teste de arquitetura: nenhum import de cliente HTTP de LLM fora de `packages/llm`.
- Os evals rodam no CI para todo PR que altera prompt, schema ou modelo.
- Métricas por tarefa: latência, tokens, taxa de JSON inválido e erros, sem conteúdo ([ADR-0015](0015-observabilidade-sem-dados-pessoais.md)).

## Links

- [Arquitetura de IA](../ia.md)
- Protótipo: `server/src/llm.js`
