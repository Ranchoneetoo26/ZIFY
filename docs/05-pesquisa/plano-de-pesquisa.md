# Plano de pesquisa — validação da Candidatura Assistida

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Plano de pesquisa (v2) |
| Versão | 2.0 |
| Status | Em campo |
| Dono | Product Manager |
| Relacionados | [Questionário v2](questionario-v2.md), [Personas](../01-produto/personas.md), [Métricas](../01-produto/metricas.md), [DVP-E §3](../01-produto/dvp-e.md#3-problema), [`ferramentas/pesquisa/`](../../ferramentas/pesquisa/) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 30/09/2026 | Pesquisa "Como você busca oportunidades em tecnologia?", sobre o marketplace de TI (PJ/CLT, assinatura, reputação). 1 resposta de teste. |
| 2.0 | 12/10/2026 | O mesmo formulário foi reescrito para a Candidatura Assistida. As respostas da v1 foram copiadas para a aba "Arquivo v1 — marketplace" da planilha. |

## 1. Objetivo

Validar, com pessoas candidatas de **diferentes trajetórias e áreas**, quais tarefas da candidatura são mais repetitivas e incômodas e quais necessidades (personas PN1–PN5) são mais comuns. Também queremos medir a confiança num assistente que preenche formulários com revisão. Esses resultados definem:

- quais problemas P1–P5 do [DVP-E](../01-produto/dvp-e.md#3-problema) são reais;
- quais campos e etapas o primeiro fluxo precisa cobrir (ponto V2 do DVP-E);
- as metas numéricas das [métricas](../01-produto/metricas.md), ponto V5;
- a mensagem de divulgação por persona e os receios a tratar na interface e na política de privacidade.

**Não** queremos medir intenção de compra com precisão. Preço aparece só como sinal de disposição; não haverá cobrança no MVP 1.

## 2. Hipóteses

| ID | Hipótese | Pergunta que testa | Validada se |
|---|---|---|---|
| H1 | Preencher os mesmos dados em cada site incomoda muito (P1) | Grade de dores, linha 1 | Critério da §5 |
| H2 | Manter currículo e perfis consistentes incomoda (P2) | Grade de dores, linha 2 | Critério da §5 |
| H3 | Perguntas abertas incomodam (P3) | Grade de dores, linha 3 | Critério da §5 |
| H4 | Saber se tem o perfil da vaga incomoda (P5) | Grade de dores, linha 4 | Critério da §5 |
| H5 | Entender o formulário e revisar sem erro incomodam (P4) | Grade de dores, linhas 5 e 6 | Critério da §5 em pelo menos uma das duas |
| H6 | Uma candidatura típica leva 15 minutos ou mais | Tempo por candidatura | ≥ 50% das pessoas que se candidatam marcam "15 a 30" ou "Mais de 30" |
| H7 | A confirmação antes do envio gera confiança | Escala de confiança | Média ≥ 3,5 |
| H8 | O público não se concentra em tecnologia | Área | Nenhuma área com mais de 50% das respostas. Se uma área passar disso, a amostra está enviesada: ampliar a divulgação |

As linhas 7 ("Lembrar onde já me candidatei") e 8 ("Não ter retorno das empresas") são **controles**:

- a linha 7 mede uma necessidade do MVP 2;
- a linha 8 mede uma dor que o Zyft não resolve.

Se uma delas vencer as dores do MVP 1, isso é sinal para rever a prioridade, não para mudar o escopo sem decisão (RN-07).

## 3. Público e amostra

- **Quem:** pessoas que se candidataram a ao menos uma vaga pela internet nos últimos 6 meses, de qualquer área, idade ou escolaridade.
- **Mínimo:** 15 respostas para qualquer conclusão; meta de 60, com pelo menos 10 em cada um destes grupos: primeiro emprego, empregados buscando mudança, desempregados e transição de carreira.
- **Onde divulgar:** grupos de vagas da região, faculdade (outros cursos além de TI), LinkedIn pessoal do time, grupos de WhatsApp de família e amigos. Evitar divulgar só em comunidades de tecnologia (H8).
- **Filtro:** quem marca "Não estou procurando vaga no momento" **e** "Nenhuma no momento" em candidaturas por semana entra na análise de perfil, mas fica fora das médias de dor.

## 4. Instrumento

O questionário v2 está em [questionario-v2.md](questionario-v2.md). A fonte de verdade é [`ferramentas/pesquisa/questionario-v2.json`](../../ferramentas/pesquisa/questionario-v2.json), aplicada ao Google Forms pelo [`aplicar_pesquisa_v2.py`](../../ferramentas/pesquisa/aplicar_pesquisa_v2.py).

| Seção | Objetivo |
|---|---|
| 1. Sobre você | Perfil: idade, momento, área, experiência, escolaridade, cidade, familiaridade digital |
| 2. Como você se candidata hoje | Volume, tempo, portais e etapas que mais tomam tempo (ponto V2 do DVP-E) |
| 3. O que atrapalha | Grade de dores de 1 a 5, relato livre, receios de preenchimento, confiança |
| 4. Com quem você se parece? | Escolha entre os perfis A–E (= PN1–PN5), identificação de 1 a 5 e o que não combina |
| 5. O que você acha da ideia? | Interesse, importância de cada recurso, modelo de uso, preço, barreiras, contato opcional |

Correspondência entre os perfis do questionário e as personas:

| Perfil | Persona |
|---|---|
| Perfil A — Quer agilidade | PN1 |
| Perfil B — Quer organizar | PN2 |
| Perfil C — Precisa de ajuda para escrever | PN3 |
| Perfil D — Quer saber se tem o perfil | PN4 |
| Perfil E — Precisa de orientação no formulário | PN5 |

## 5. Critérios de validação

Uma dor é **validada** quando atinge os três critérios ao mesmo tempo:

1. média ≥ **3,5** na escala de 1 a 5;
2. pelo menos **50%** das pessoas deram nota **4 ou 5**;
3. pelo menos **15** respostas válidas naquele recorte.

Com menos de 15 respostas, o resultado é "poucos dados": serve de pista, não de conclusão.

Uma persona é **confirmada** quando:

- ela é escolhida por pelo menos **15%** das respostas;
- a média de identificação com ela é ≥ **3,5**.

Personas com menos de 5% de escolha são revistas. Se "Nenhum deles" passar de 20%, os textos dos perfis são reescritos com base nas respostas abertas.

Os mesmos critérios valem dentro de cada recorte (persona, momento profissional, familiaridade digital) quando o recorte tiver ao menos 15 respostas.

## 6. Análise

1. Exportar a aba de respostas v2 da planilha. A aba "Arquivo v1 — marketplace" fica fora da análise.
2. Calcular média e % de notas 4–5 por dor, no geral e por persona.
3. Cruzar persona × dores, persona × familiaridade digital e persona × receios.
4. Ler todos os relatos livres e agrupá-los por tema (afinidade). Citar no relatório apenas trechos sem dados identificáveis.
5. Registrar o resultado em `docs/05-pesquisa/resultados-v2.md`, com data, número de respostas e o que mudou em personas, DVP-E e métricas.

O `Codigo.gs` em `ferramentas/pesquisa/` gera um relatório automático (persona, arquétipos e ranking de dores) num Google Docs. Ele serve de apoio e não substitui a leitura dos relatos.

## 7. Ética e privacidade

- Respostas anônimas. O contato é opcional, usado só para convidar para testes, e **não** entra em relatórios nem neste repositório.
- Nenhum dado da planilha é commitado no repositório.
- O texto da seção 5 descreve o produto sem prometer resultado (RN-06).
- Quem deixou contato pode pedir exclusão a qualquer momento. A remoção é feita direto na planilha.

## 8. Próximos passos

| Passo | Dono | Saída |
|---|---|---|
| Divulgar o formulário v2 | PM e todo o time | Meta de 60 respostas |
| Entrevistas curtas (20 min) com 5–8 pessoas que deixaram contato, cobrindo ao menos 3 personas | PM e Designer UX | Notas de entrevista e mapa de afinidade |
| Consolidar resultados | PM | `resultados-v2.md` |
| Revisar personas, DVP-E §3 e métricas | PO e PM | Novas versões dos documentos |
| Teste de usabilidade com o protótipo no portal de demonstração | Designer UX e QA | Tempo por candidatura e campos aceitos sem edição (O1, O2) |

## 9. Mudanças em relação à v1

| v1 (marketplace) | v2 (Candidatura Assistida) | Motivo |
|---|---|---|
| Público: profissionais de TI | Qualquer pessoa que se candidata a vagas | O produto não é exclusivo de uma área |
| CNPJ/MEI, PJ × CLT, formato de trabalho | Removidos | Marketplace e PJ/CLT são MVP 2 |
| Dificuldades do marketplace (preço baixo, comissões, cliente não confiável) | Grade de dores da candidatura | Validar P1–P5 |
| Informações que precisa ver na oportunidade | Receios de deixar um assistente preencher | Orientar privacidade e consentimentos |
| Perfis A/B/C (iniciante PJ, experiente remoto, tradicional) | Perfis A–E = personas de necessidade PN1–PN5 | Personas por necessidade, não por demografia |
| Confiança em processo 100% online | Confiança num assistente que só preenche com confirmação | Hipótese H7 |
| Recursos do marketplace e assinatura × comissão | Recursos do MVP 1 e modelo de uso | Comissão não se aplica |
| Arquétipo "Maratonista" com mensagem de volume | Perfil A "Quer agilidade", sem promessa de volume | Candidatura em massa está fora do produto |
