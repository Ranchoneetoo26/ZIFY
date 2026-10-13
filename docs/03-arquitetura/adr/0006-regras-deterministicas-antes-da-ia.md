# ADR-0006 — Regras determinísticas antes da IA no de-para

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 12/10/2026 |
| Decisores | Arquiteto de software, Arquiteto de IA |
| Consultados | Dev backend, Dev extensão, QA |
| Requisitos | RF-12, RF-13, RF-18; RNF-04, RNF-05, RNF-12 |
| Relacionados | ADR-0004, ADR-0005, ADR-0011 |

## Contexto

Um formulário de currículo típico tem de 30 a 60 campos:

- dados pessoais;
- vários blocos de experiência, formação, cursos e idiomas;
- perguntas de triagem;
- textos abertos.

A maior parte é objetiva e tem rótulos previsíveis ("Nome completo", "CEP", "Data de início").

O protótipo (`server/src/mapper.js`) resolve o de-para em três camadas:

1. **Bloqueios por expressão regular** (`SENSITIVE`, `DECLARATION`, `CAPTCHA`, tipos `file`, `password`, `hidden`) antes de qualquer outra coisa.
2. **Regras determinísticas:**
   - `TOP_RULES` com cerca de 30 chaves de nível superior, em português e inglês, numa ordem de prioridade;
   - `GROUPS` para blocos repetidos (experiências, formação, cursos, certificações, idiomas), identificados pela seção (fieldset ou título) ou por um rótulo único;
   - índice explícito no `name` (ex.: `exp[1][empresa]`) ou contador;
   - `alignSlots`, que alinha cada bloco da página com o item do perfil pela empresa ou instituição (igualdade normalizada ou Jaccard ≥ 0,6).
3. **IA**, só para o que sobrou ([ADR-0004](0004-ia-aponta-chave-nunca-escreve-valor.md)).

Na prática, as regras resolvem a maioria dos campos em milissegundos, e a IA trata o restante em segundos.

## Fatores de decisão

- Latência (RNF-04: regras p95 ≤ 300 ms para 100 campos).
- Previsibilidade e testabilidade: o mesmo formulário produz o mesmo resultado.
- Custo de inferência.
- Funcionar com a IA fora do ar (RNF-12).
- Cobrir rótulos inesperados.

## Opções consideradas

### Opção A — Só IA

- **Prós:** nenhum código de regras para manter; cobre qualquer rótulo.
- **Contras:** de 10 a 30 s por formulário com modelo local; resultado varia entre execuções; tudo para quando a IA cai; custo por campo.

### Opção B — Só regras

- **Prós:** rápido, determinístico, testável.
- **Contras:** falha em rótulos fora do padrão, perguntas abertas e portais novos; cada portal exige muitas regras.

### Opção C — Bloqueios, depois regras, depois IA (protótipo)

- **Prós:** combina a velocidade e a previsibilidade das regras com a cobertura da IA; os bloqueios rodam primeiro e não dependem de modelo; degrada para "só regras" sem IA.
- **Contras:** duas lógicas para manter; as regras precisam de testes sobre fixtures reais para não regredir; a ordem das regras importa (ex.: "Sobrenome" antes de "Nome").

## Decisão

Escolhemos a **Opção C**, com esta ordem fixa:

1. **Bloqueios** (sempre, nunca desligáveis): senha, oculto, botões, arquivo, CAPTCHA, declarações e termos, dados sensíveis ([ADR-0010](0010-sem-credenciais-sem-contorno-de-seguranca-campos-bloqueados.md), [ADR-0014](0014-dados-sensiveis-segregados-e-criptografados.md)).
2. **Regras do conector do portal** ([ADR-0011](0011-primeiro-portal-e-conectores-por-portal.md)): seletores e rótulos específicos do portal suportado, com prioridade sobre as genéricas.
3. **Regras genéricas** (`packages/field-rules`): `TOP_RULES`, `GROUPS`, alinhamento de blocos, adaptação de valor (datas, telefone, CEP, selects).
4. **Respostas aprovadas** para perguntas abertas parecidas (Jaccard ≥ 0,6).
5. **IA**: aponta a chave, pede geração de texto ou devolve "perguntar".

Cada mapeamento registra a origem (`regra`, `conector`, `resposta aprovada`, `ia`) e a confiança, mostradas no painel (RF-13).

## Consequências

**Positivas**
- A maior parte do formulário aparece quase instantaneamente; a IA completa depois.
- Os testes de regressão das regras são rápidos e determinísticos.
- Sem IA, o produto continua útil.

**Negativas**
- As regras genéricas acumulam exceções. É preciso disciplina para mover regras de portal para o conector.
- Expressões regulares em português e inglês exigem normalização cuidadosa (`norm()`: sem acento, minúsculas, sem pontuação).

**Riscos e mitigação**
- **Uma regra genérica "rouba" um campo** (ex.: "Cidade" da empresa vira a cidade da pessoa). Mitigação: `GENERIC_TOP` só casa fora de grupos; testes com fixtures contendo esses casos.
- **A ordem das regras muda sem querer.** Mitigação: testes de tabela com rótulo → chave esperada.

## Conformidade

- Suíte de regras com tabela "rótulo, seção, tipo → chave esperada", rodando no CI.
- Teste de desempenho: 100 campos em ≤ 300 ms (p95) sem IA.
- Teste com a IA desligada: nenhum erro, e campos não resolvidos ficam como "ação sua".

## Links

- [Backend — módulo de mapeamento](../backend.md)
- Protótipo: `server/src/mapper.js`, `server/src/text.js`
