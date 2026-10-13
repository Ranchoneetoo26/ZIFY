# ADR-0004 — A IA aponta a chave do perfil, nunca escreve o valor

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 12/10/2026 |
| Decisores | Arquiteto de IA, Arquiteto de software, Product Owner |
| Consultados | QA, Dev backend |
| Requisitos | RF-03, RF-12, RF-15; RNF-05; RN-02, RN-03 |
| Relacionados | ADR-0005, ADR-0006, ADR-0013 |

## Contexto

O princípio 2 do DVP-E ("só fatos verdadeiros") exige que o Zyft nunca preencha uma informação que a pessoa não deu. Modelos de linguagem, em especial os de 7–8B usados localmente, alucinam: completam datas, ajustam nomes de empresas e "melhoram" cargos.

O protótipo resolveu isso no de-para (`server/src/mapper.js`, função `aiResolve`):

- o schema de saída do LLM só permite `fieldId` (enum dos campos pendentes), `action` (`fill` | `generate` | `ask`) e `profileKey`;
- se `action = fill`, o servidor confere `keys.includes(profileKey)` e o valor vem de `byKey[profileKey].value`, isto é, do perfil;
- opções de select passam por `pickOption`, que só aceita uma opção existente na página; se nenhuma corresponde, o campo vira `ambiguous`.

Textos livres (`generate`) seguem outra regra: são gerados em `writer.js` a partir de `profileFacts(profile)`, com `usedFacts` e `missingInfo`, e sempre entram como sugestão a revisar.

## Fatores de decisão

- Zero fatos inventados em campos objetivos (RNF-05).
- Auditabilidade: deve ser possível dizer de onde veio cada valor.
- Funcionar com modelos pequenos e locais, que erram mais.
- Experiência: a IA precisa resolver rótulos que as regras não cobrem (ex.: "Como prefere ser chamado?", rótulos em inglês, seções sem título).

## Opções consideradas

### Opção A — A IA devolve o valor de cada campo

- **Prós:** mais simples; cobre qualquer campo.
- **Contras:** qualquer alucinação vai direto para o formulário; impossível provar a origem do valor; validação só heurística.

### Opção B — A IA devolve o valor e o servidor confere se ele "aparece" no perfil

- **Prós:** flexível.
- **Contras:** comparação textual frágil (datas reformatadas, abreviações); falsos positivos deixam passar invenções parecidas com fatos reais.

### Opção C — A IA só aponta uma chave existente; o valor vem do perfil (protótipo)

- **Prós:** o valor objetivo, por construção, é um dado do perfil; a saída do modelo é restrita por schema (enum de campos), o que limita formato inválido; erro do modelo vira, no pior caso, "chave errada", que é visível na revisão com o rótulo do perfil.
- **Contras:** a IA não consegue preencher campos que exigem transformação (ex.: "anos de experiência" quando o perfil só tem datas). Para isso, os fatos derivados são calculados em código (`profileFacts` calcula o tempo total de experiência) e expostos como chaves.

## Decisão

Escolhemos a **Opção C** para todo campo objetivo:

1. A saída do LLM no de-para é um JSON validado por schema, com `profileKey` restrita às chaves existentes do perfil achatado (`flattenProfile`). Chave inexistente equivale a `ask`.
2. O valor preenchido vem **sempre** do Perfil Mestre (ou de uma edição feita pela pessoa no painel), nunca do texto do LLM (RN-02).
3. Selects, radios e checkboxes só recebem opções que existem na página. Sem correspondência, o campo fica `ambiguous` e a pessoa decide (RN-05).
4. Fatos derivados (tempo de experiência, idade quando necessário, nome e sobrenome) são calculados em código e expostos como chaves.
5. Texto livre é a única saída redigida pelo LLM. Ele usa só `profileFacts`, retorna `usedFacts` e `missingInfo`, nunca é pré-aprovado e segue o consentimento (a) da RF-07 (RN-03).
6. Na importação de currículo (RF-03), o LLM estrutura o texto do arquivo, mas o resultado é uma **proposta** que a pessoa aprova campo a campo (RF-04).

## Consequências

**Positivas**
- Garantia estrutural contra invenção de dados objetivos, verificável em teste.
- O painel pode mostrar a origem de cada valor ("do seu perfil: Experiência 2 — empresa").

**Negativas**
- Campos com transformação não prevista ficam como "ação sua" até alguém criar uma chave derivada.
- Mais código de validação no servidor.

**Riscos e mitigação**
- **A IA aponta a chave errada** (ex.: cidade da empresa em vez da cidade da pessoa). Mitigação: regras determinísticas primeiro ([ADR-0006](0006-regras-deterministicas-antes-da-ia.md)); confiança "média" para origem IA; o campo vem marcado como revisável.
- **Texto livre com fato inventado.** Mitigação: `usedFacts` conferido contra o perfil com `grounded()` (`text.js`) nas avaliações de IA; revisão humana obrigatória.

## Conformidade

- Teste unitário: com um LLM falso que devolve chaves inexistentes ou valores no lugar de chaves, nenhum valor fora do perfil chega à resposta de `POST /map-fields`.
- Suíte de avaliação de IA (RNF-05): 0 valores objetivos fora do perfil.
- Revisão de PR: qualquer novo uso de LLM declara se a saída é "aponta" ou "redige"; se redige, precisa de revisão humana na interface.

## Links

- [Arquitetura de IA](../ia.md)
- Protótipo: `server/src/mapper.js` (`aiResolve`, `resolveValue`, `pickOption`), `server/src/writer.js`, `server/src/profile.js` (`profileFacts`)
