# ADR-0009 — O Zyft nunca envia; confirmação explícita da pessoa

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 12/10/2026 |
| Decisores | Product Owner, Arquiteto de software, CEO |
| Consultados | Dev extensão, Designer UX, QA |
| Requisitos | RF-13, RF-14, RF-19; RN-01, RN-05 |
| Relacionados | ADR-0002, ADR-0010, ADR-0012 |

## Contexto

A proposta de valor do Zyft é "revisar cada resposta e confirmar cada ação antes do envio". O documento de origem já dizia que o produto deve ser "um assistente confiável, e não um robô autônomo de disparo de currículos", e o reposicionamento lista candidatura automática em massa e envio sem aprovação como fora do produto.

O protótipo já segue essa linha:

- o content script (`extension/content.js`) só preenche os itens enviados pelo painel;
- campos ambíguos, ausentes, sensíveis e declarações vêm desmarcados (`selected: false`);
- o painel mostra a lista para revisão antes de "Preencher selecionados";
- o content script só **escuta** o evento `submit` da página para avisar o painel de que a pessoa enviou. Ele nunca o dispara.

## Fatores de decisão

- Confiança da pessoa candidata (O2 do DVP-E).
- Termos de uso dos portais e reputação com recrutadores.
- Responsabilidade: quem envia uma candidatura deve ser a pessoa.
- Diferenciação contra robôs de candidatura em massa.

## Opções consideradas

### Opção A — Envio automático opcional ("modo rápido")

- **Prós:** atende quem quer volume (arquétipo "Maratonista" da pesquisa v1).
- **Contras:** contraria o posicionamento e o escopo; aumenta erros enviados sem revisão; viola termos de muitos portais; transfere ao Zyft a responsabilidade por candidaturas erradas.

### Opção B — Preencher tudo e pedir uma confirmação geral no fim

- **Prós:** menos cliques.
- **Contras:** a pessoa tende a confirmar sem ler; campos ambíguos passam despercebidos.

### Opção C — Revisão por campo, preenchimento só do selecionado, envio sempre pela pessoa no portal

- **Prós:** controle real; o estado de cada campo é visível; nenhum envio sem um clique humano no próprio portal.
- **Contras:** mais passos; o volume de candidaturas por hora é menor (aceitável e desejado).

## Decisão

Escolhemos a **Opção C**:

1. A extensão **nunca** chama `submit()`, `requestSubmit()`, `click()` em botões de envio, salvar, avançar, aceitar ou confirmar, nem simula Enter em formulários.
2. O painel mostra, **antes** de preencher, quantos campos serão preenchidos e quais precisam de ação ("Vamos preencher 18 campos. 3 precisam de você.").
3. Só campos com estado "pronto" vêm pré-selecionados. Ambíguos, ausentes, sensíveis, declarações e textos gerados pela IA vêm **desmarcados** e só são preenchidos se a pessoa marcar.
4. Depois do preenchimento, os campos alterados ficam destacados na página para revisão visual.
5. Salvar ou enviar é sempre um clique da pessoa no portal. A extensão pode **detectar** o envio (evento `submit`) para mostrar a confirmação de conclusão (RF-19); o que é guardado depende do [ADR-0012](0012-registro-minimo-de-conclusao-no-mvp1.md).
6. Não existe, em nenhuma configuração, modo de envio automático ou em lote.

## Consequências

**Positivas**
- Coerência total entre o discurso do produto e o comportamento.
- Menos risco jurídico e reputacional.

**Negativas**
- Quem quer volume máximo não é atendido (antipersona do DVP-E).
- O fluxo tem mais passos; a UX precisa ser muito clara para não cansar.

**Riscos e mitigação**
- **Um portal dispara o envio ao mudar um campo** (ex.: select com `onchange` que submete). Mitigação: o conector do portal marca esses campos como "ação sua"; o teste E2E verifica que nenhuma navegação ocorre durante o preenchimento.
- **Pressão futura por um "modo rápido".** Mitigação: qualquer mudança exige um novo ADR que substitua este, com decisão do CEO.

## Conformidade

- Teste estático: falha se o código da extensão contiver `requestSubmit`, `.submit(` ou `click()` em seletores de envio.
- Teste E2E: depois de "Preencher", nenhum evento `submit` e nenhuma navegação disparados pela extensão nas fixtures.
- Item obrigatório no checklist de PR da extensão.

## Links

- [Fluxos do MVP 1](../../01-produto/fluxos.md)
- Protótipo: `extension/content.js` (`fill`, listener de `submit`), `extension/sidepanel.js` (`fillSelected`)
