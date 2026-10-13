# ADR-0012 — Registro mínimo de conclusão no MVP 1

| Campo | Valor |
|---|---|
| Status | Proposta |
| Data | 12/10/2026 |
| Decisores | Product Owner, Arquiteto de software |
| Consultados | Responsável por privacidade, Designer UX, Product Manager |
| Requisitos | RF-19, RF-20; RN-07; RF-M2-01, RF-M2-02 |
| Relacionados | ADR-0001, ADR-0008, ADR-0009 |

## Contexto

O reposicionamento deixa uma pergunta aberta (ponto V3 do DVP-E): o MVP 1 terá apenas uma confirmação visual de conclusão, ou algum registro mínimo, sem antecipar o painel completo de acompanhamento do MVP 2?

O protótipo já tem um **histórico completo**:

- **Registro:** `POST /api/applications`, com cargo, empresa, portal, URL, versão do currículo, notas e status.
- **Status:** dez valores (salva, em revisão, candidatura iniciada, enviada, em análise, entrevista, teste, reprovada, encerrada, proposta recebida).
- **Interface:** aba "Candidaturas" na plataforma.
- **Registro automático:** a extensão detecta o `submit` da página e oferece registrar.

Pelo reposicionamento, esse histórico é **MVP 2** (RF-M2-01, RF-M2-02).

## Fatores de decisão

- Regra de escopo RN-07: não antecipar o MVP 2.
- Minimização de dados (LGPD): só guardar o que tem finalidade clara no MVP 1.
- Medição: o time precisa saber se as candidaturas assistidas foram concluídas (O1 e O2 do DVP-E).
- Valor para a pessoa: lembrar o que foi feito.

## Opções consideradas

### Opção A — Só confirmação visual, nada guardado

A extensão mostra "Você enviou a candidatura para <cargo>. Preenchemos 18 campos." e não guarda nada além do registro de ações já previsto em RF-20 (sem valores).

- **Prós:** mínimo de dados; zero risco de escopo.
- **Contras:** a pessoa não tem onde consultar depois; a métrica de conclusão vem só da auditoria agregada.

### Opção B — Confirmação visual mais registro mínimo opcional (opt-in)

Com consentimento explícito, guarda-se: data, portal, cargo e empresa como aparecem na página, e URL. Sem status, sem notas, sem versão de currículo, sem valores preenchidos. Na plataforma, o registro é uma lista simples, apenas para leitura e exclusão.

- **Prós:** atende a necessidade de "lembrar onde me candidatei" de forma mínima; dá dado de conclusão consentido; serve de semente para o MVP 2.
- **Contras:** risco de "crescer" para um painel aos poucos; mais um consentimento para explicar.

### Opção C — Histórico completo do protótipo (status, notas, filtros)

- **Prós:** já existe no código.
- **Contras:** é explicitamente MVP 2; aumenta a superfície de dados e de interface; desvia o foco do fluxo principal.

## Decisão (proposta)

Recomendamos a **Opção B**:

1. A confirmação visual de conclusão é sempre exibida (RF-19).
2. Na primeira conclusão, o painel pergunta: "Quer guardar um lembrete desta candidatura (data, portal, cargo e empresa)?" A resposta vira o consentimento "registro de candidaturas", desligável a qualquer momento.
3. Campos guardados: `data`, `portal`, `cargo`, `empresa`, `url`. **Nada mais.**
4. Na plataforma, uma lista somente leitura com exclusão individual e total. Sem status, filtros, lembretes nem edição. Qualquer um desses itens exige novo ADR, por ser MVP 2.
5. O código do histórico completo do protótipo **não** é migrado para o MVP 1.

Se a pesquisa v2 e os testes de usabilidade mostrarem que a pessoa não valoriza o registro, adotamos a **Opção A**.

## Consequências

**Positivas**
- Atende a dor P6 de forma mínima, sem antecipar o MVP 2.
- Dado de conclusão consentido para as métricas do MVP 1.

**Negativas**
- Mais um consentimento e mais uma tela.
- Parte do trabalho do protótipo fica sem uso até o MVP 2.

**Riscos e mitigação**
- **Pressão para adicionar status e lembretes.** Mitigação: RN-07; a lista é explicitamente apenas para leitura.
- **Detecção de envio errada** (o `submit` acontece numa etapa intermediária). Mitigação: o conector define qual evento ou página caracteriza a conclusão; na dúvida, o painel pergunta "Você concluiu a candidatura?".

## Conformidade

- Teste de contrato: `conclusao_candidatura` só aceita as cinco colunas definidas.
- Teste: com o consentimento desligado, nenhuma linha é gravada.
- Revisão de PO em qualquer PR que toque a tela de registro.

## Links

- [DRP RF-19](../../01-produto/drp.md#35-vaga-conclusão-e-registro), [Roadmap](../../01-produto/roadmap.md)
- Protótipo: `server/public/app.js` (`STATUSES`), `server/src/index.js` (`/api/applications`)
