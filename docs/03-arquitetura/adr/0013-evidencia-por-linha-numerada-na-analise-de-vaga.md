# ADR-0013 — Evidência por linha numerada na análise de vaga

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 12/10/2026 |
| Decisores | Arquiteto de IA, Product Owner |
| Consultados | Arquiteto de software, QA, Designer UX |
| Requisitos | RF-17; RNF-05; RN-02, RN-06 |
| Relacionados | ADR-0004, ADR-0005 |

## Contexto

A análise de vaga (RF-17, *Should*) compara os requisitos visíveis da vaga com o Perfil Mestre. O risco é a IA afirmar que a pessoa "atende" a um requisito com base numa qualificação inventada, ou criar uma justificativa plausível mas falsa.

O protótipo (`server/src/jobs.js`) resolve isso assim:

1. Extrai da página os dados visíveis da vaga, com os requisitos marcados como "obrigatório" ou "desejável".
2. Gera os fatos do perfil em linhas (`profileFacts`), incluindo **fatos derivados calculados em código**, como o tempo total de experiência.
3. Numera as linhas: `[1] …`, `[2] …`.
4. Para cada requisito, o LLM devolve `analysis` (uma frase), `status` (`atende` | `parcial` | `nao_encontrado` | `confirmar`) e `line` (inteiro).
5. A evidência exibida é a **linha real do perfil** (`lines[line - 1]`), não um texto do modelo. Se o status é `atende` ou `parcial` e a linha não existe, o status vira `nao_encontrado`.
6. Pontos que dependem de decisão da pessoa (CNH, turnos, viagens, pretensão salarial, modalidade, mudança, tipo de contrato) são detectados por regra (`CONFIRM_RULES`) e viram "a confirmar", mostrando o valor atual do perfil.

Limitação observada: com `qwen2.5:7b`, a classificação às vezes erra (ex.: "parcial" no lugar de "atende", ou a linha errada para "viagens"), mas a evidência exibida torna o erro **visível**.

## Fatores de decisão

- Nunca afirmar uma qualificação que não existe no perfil (RNF-05).
- Nunca prometer compatibilidade (RN-06): sem nota, sem porcentagem.
- Erro do modelo precisa ser perceptível pela pessoa.
- Funcionar com modelos pequenos.

## Opções consideradas

### Opção A — Nota de compatibilidade gerada pela IA (ex.: "82% compatível")

- **Prós:** simples de mostrar; comum no mercado.
- **Contras:** viola RN-06; número sem base verificável; induz a pessoa a desistir ou a se candidatar com base num valor arbitrário.

### Opção B — Justificativa em texto livre da IA

- **Prós:** explicação mais rica.
- **Contras:** a justificativa pode citar fatos inexistentes; verificar exige comparação textual frágil.

### Opção C — Status por requisito com evidência por número de linha (protótipo)

- **Prós:** a evidência é, por construção, um fato do perfil; "atende" sem evidência é rebaixado automaticamente; o erro do modelo aparece como "evidência que não tem a ver", e a pessoa percebe.
- **Contras:** o modelo ainda pode escolher a linha errada; uma chamada por requisito aumenta a latência (20–30 s no protótipo).

## Decisão

Escolhemos a **Opção C**:

1. Cada requisito tem um destes status: **atende**, **parcial**, **não encontrado** ou **a confirmar**. Não há nota global, porcentagem ou ranking.
2. "Atende" e "parcial" exigem uma linha existente do perfil numerado, mostrada literalmente como evidência. Sem linha válida, o status é "não encontrado".
3. Fatos derivados (tempo de experiência e afins) são calculados em código e entram como linhas do perfil.
4. Pontos de decisão da pessoa são detectados por regra e mostrados como "a confirmar" com o valor atual do perfil.
5. A interface diz claramente: "Comparação com o seu perfil. Não é uma avaliação da empresa nem garantia de resultado."
6. Dados sensíveis não entram nas linhas do perfil ([ADR-0014](0014-dados-sensiveis-segregados-e-criptografados.md)).

Otimizações de latência (avaliar vários requisitos por chamada, paralelismo, streaming) são permitidas desde que mantenham as regras 1 a 3 e passem nos evals.

## Consequências

**Positivas**
- Garantia estrutural contra evidência inventada.
- Explicável: a pessoa vê por que cada requisito foi marcado.

**Negativas**
- Classificações erradas ainda acontecem; a garantia é sobre a **evidência**, não sobre o julgamento.
- Latência alta com modelos locais; a RF-17 pode ficar fora do MVP 1 se não atingir RNF-04.

**Riscos e mitigação**
- **A pessoa confia demais na classificação.** Mitigação: o texto de interface acima e a evidência sempre visível.
- **A linha numerada muda entre a análise e a exibição** (perfil editado no meio). Mitigação: a resposta carrega a linha copiada no momento da análise.

## Conformidade

- Teste unitário: com um LLM falso que devolve `atende` e `line` fora do intervalo, o resultado é `nao_encontrado` com evidência vazia.
- Evals com gabarito de vagas reais anonimizadas: 100% das evidências são linhas do perfil; concordância de status acompanhada contra a linha de base, sem gate até haver 50+ casos (limiares em [estratégia de testes §5.2](../../04-qualidade/estrategia-de-testes.md#52-métricas-e-limiares)).
- Teste de interface: nenhuma porcentagem ou nota aparece na tela de análise.

## Links

- [Arquitetura de IA](../ia.md)
- Protótipo: `server/src/jobs.js`, `server/src/profile.js` (`profileFacts`)
