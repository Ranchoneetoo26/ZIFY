# ADR-0012 — Registro de conclusão no MVP 1 (com status e anotações)

| Campo | Valor |
|---|---|
| Status | Aceita (Opção B ampliada com status e anotações; decisão de Guilherme Brogio, 22/10/2026) |
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

## Nota de divergência (21/10/2026)

> Esta nota não muda o status do ADR. Ela registra que a implementação da Entrega 3 e da v2 **não seguiu** a Opção B recomendada e propõe como resolver.

**O que foi implementado** (`src/Zyft.Domain/Applications/JobApplication.cs`, `ApplicationsController`, `web/js/views/applications.js`):

| Ponto | Opção B (recomendada) | Implementação |
|---|---|---|
| Quando grava | Só com consentimento "registro de candidaturas" (opt-in na primeira conclusão) | Quando a pessoa toca em "Concluí a candidatura" no painel (`POST /forms/completions`). Não há consentimento específico; o toque é a ação explícita |
| Campos | data, portal, cargo, empresa, URL | Os cinco, mais `fieldsFilled` (quantidade de campos preenchidos), `status` e `notes` |
| Status | Nenhum | Enum `ApplicationStatus` com 10 valores herdados do protótipo (Confirmed, InReview, Interview, Test, Offer, Hired, Rejected, Withdrawn, NoResponse, Archived). Toda candidatura nasce como `Confirmed` |
| Edição | Nenhuma (só leitura e exclusão) | `PATCH /applications/{id}` altera status e anotações (até 2000 caracteres) |
| Interface | Lista simples | Tabela com busca e filtro por status; detalhe com status, anotações e a contagem de campos (os valores preenchidos não são guardados) |
| Exclusão | Individual e total | Individual (`DELETE /applications/{id}`); a exclusão total acontece com a exclusão da conta |

O que se manteve fiel ao ADR: nenhum valor preenchido é guardado, a conclusão nunca é automática (ADR-0009) e a auditoria registra só `application.confirmed` com portal e quantidade de campos.

**Proposta de atualização** (para decisão do PO e do arquiteto):

1. Reconhecer o que existe como **Opção B ampliada**: aceitar `fieldsFilled` (útil para a métrica de valor, sem dado pessoal) e manter o registro disparado pelo toque em "Concluí a candidatura", explicando isso na política de privacidade em vez de criar mais um consentimento.
2. Para status e anotações, escolher um caminho: (a) **manter** e registrar a exceção à RN-07 neste ADR, com a justificativa de que a dor "lembrar onde me candidatei" teve 3,00 de média e 47% de notas 4 e 5 na pesquisa v2; ou (b) **esconder** status, filtro e anotações na interface até o MVP 2, mantendo as colunas no banco sem uso.
3. Atualizar a seção "Conformidade": o teste de contrato das "cinco colunas" não existe e não passaria hoje.

Até a decisão, a documentação de arquitetura descreve o comportamento real e aponta para esta nota.

## Decisão final (22/10/2026)

**Aceita a Opção B ampliada, com status e anotações** (caminho 2a da nota acima), por decisão de Guilherme Brogio, responsável pela arquitetura do time.

1. O registro acontece quando a pessoa toca em "Concluí a candidatura" no painel e confirma "Confirmo que enviei". É uma ação explícita dela; não há registro automático (ADR-0009).
2. Campos guardados: data, portal, cargo, empresa, URL, `fieldsFilled`, `status` e `notes`. Os **valores preenchidos nunca são guardados**.
3. Status (10 valores) e anotações (até 2000 caracteres) ficam no MVP 1, como **exceção registrada à RN-07**. A justificativa vem da pesquisa v2: "lembrar onde me candidatei" teve média 3,00 e 47% de notas 4 e 5, e "sem retorno das empresas" foi a dor mais forte (4,40; 87%). O status é o mínimo para a pessoa acompanhar o retorno.
4. Continuam **fora** do MVP 1, exigindo novo ADR: lembretes automáticos de retorno, integração com e-mail ou portais para atualizar status, e o painel de acompanhamento do RF-M2-01/RF-M2-02.
5. A política de privacidade explica o registro, e ele é apagado individualmente ou com a exclusão da conta.

**Conformidade atualizada:** os testes de integração de candidaturas (criação por `POST /forms/completions`, `PATCH` de status e anotações, exclusão) substituem o teste das "cinco colunas" proposto originalmente.

## Links

- [DRP RF-19](../../01-produto/drp.md#35-vaga-conclusão-e-registro), [Roadmap](../../01-produto/roadmap.md)
- Protótipo: `server/public/app.js` (`STATUSES`), `server/src/index.js` (`/api/applications`)
