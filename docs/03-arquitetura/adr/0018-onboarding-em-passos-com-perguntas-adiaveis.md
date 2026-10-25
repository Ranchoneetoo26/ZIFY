# ADR-0018 — Onboarding em passos com perguntas adiáveis

| Campo | Valor |
|---|---|
| Status | Proposta |
| Data | 21/10/2026 |
| Decisores | Product Owner, Arquiteto de software |
| Consultados | Designer UX (dev frontend), Dev backend, Responsável por privacidade |
| Requisitos | RF-23, RF-02, RF-05, RF-07; RN-14; RNF-06, RNF-17 |
| Relacionados | [ADR-0019](0019-area-profissional-dinamica-e-campos-personalizados.md), [ADR-0014](0014-dados-sensiveis-segregados-e-criptografados.md), [ADR-0017](0017-backend-em-csharp-dotnet-clean-architecture.md) |

## Contexto

Na Entrega 3, depois do cadastro, a pessoa caía numa tela de boas-vindas com os consentimentos e, em seguida, num Perfil Mestre longo, com seções recolhíveis e muitos campos vazios. Quem não tinha currículo à mão não sabia por onde começar, e o perfil incompleto fazia a extensão marcar muitos campos como "Precisa de você", o que enfraquece justamente a promessa do produto.

A pesquisa v2 ([resultados](../../05-pesquisa/resultados-v2.md), n = 15) mostra por que isso importa:

- A dor mais forte é redigitar os mesmos dados (média 4,40; 80% deram 4 ou 5). O valor do Zyft depende de o perfil estar razoavelmente completo.
- "Preencher experiências e formação item por item" é a etapa que mais toma tempo para 67%, e "criar conta ou cadastro" para 60%. Um onboarding que pareça mais um cadastro longo repete a dor que queremos tirar.
- 67% têm receio de deixar um assistente preencher CPF e RG, 40% citam pretensão salarial e endereço como receio. Perguntas obrigatórias sobre esses dados afastariam quem ainda não confia (confiança média 3,4 de 5).
- A familiaridade com formulários é alta (média 4,07), mas o público real do produto é mais amplo que a amostra.

O design da v2 propõe um "passaporte": cerca de 25 perguntas em 6 capítulos, uma por tela, e um perfil que mostra as pendentes.

## Fatores de decisão

- Chegar a um perfil útil (que deixe a maioria dos campos Prontos) sem parecer um formulário longo.
- Respeitar o receio com dados pessoais: nada obrigatório além da conta (RN-14).
- Retomar de onde parou, em qualquer navegador (RNF-17).
- Não duplicar o modelo de dados do perfil.
- Acessibilidade e linguagem simples (RNF-06, RNF-11).

## Opções consideradas

### Opção A — Manter o perfil em seções (como na Entrega 3)

- **Prós:** já existe e está testado; nenhuma API nova.
- **Contras:** a pessoa não sabe por onde começar; não há noção de progresso; seções longas lembram o cadastro dos portais, que é a dor.

### Opção B — Assistente curto com campos obrigatórios

Cinco ou seis telas com os campos mais pedidos pelos portais, todos obrigatórios para concluir.

- **Prós:** garante um mínimo de dados; simples de implementar.
- **Contras:** obriga a informar dados que muita gente tem receio de dar (nascimento, endereço, pretensão); quem trava numa pergunta abandona o produto; contraria o princípio "a pessoa decide".

### Opção C — Onboarding em passos, uma pergunta por tela, toda pergunta adiável, com progresso no servidor (passaporte)

Seis capítulos (Quem é você, Sua área, O que você busca, Sua bagagem, Sua apresentação, Combinados). Cada pergunta tem "Preencher mais tarde". As respostas vão para os endpoints de perfil que já existem; um recurso novo `/onboarding` guarda só o progresso (`answered`, `skipped`, `lastStep`, `completedAt`). As puladas viram alerta no topo do Perfil Mestre, com "Responda agora".

- **Prós:** uma decisão por vez; a pessoa avança mesmo sem saber uma resposta; o perfil continua sendo a única fonte da verdade; o progresso sobrevive a fechar o navegador; as pendentes ficam visíveis onde a pessoa vai voltar.
- **Contras:** muitas telas; um recurso de API a mais; risco de a pessoa pular tudo e ficar com um perfil vazio.

### Opção D — Começar só pela importação do currículo

- **Prós:** aproveita o recurso mais valorizado (importar currículo: 87% "importante" ou "muito importante").
- **Contras:** nem todo mundo tem currículo atualizado em PDF ou DOCX; a importação sem IA traz pouca coisa; a escolha da área e os consentimentos continuariam sem lugar.

## Decisão (proposta)

Adotar a **Opção C**, mantendo a importação como atalho: no início do passaporte, quem tem currículo pode importar primeiro e as perguntas já respondidas pela importação aparecem como respondidas.

1. Ids de passo fixos (contrato da API): `nome`, `nascimento`, `contato`, `endereco`, `area`, `cargos`, `setores`, `registro`, `links`, `nivel`, `modelo`, `local`, `contrato`, `disponibilidade`, `salario`, `mobilidade`, `experiencia`, `diaadia`, `ferramentas`, `formacao`, `cursos`, `competencias`, `idiomas`, `titulo`, `resumo`, `combinados`. O passo `registro` só aparece quando a área pede (ADR-0019).
2. `GET /onboarding` → `{answered, skipped, lastStep, completedAt}`; `PUT /onboarding` com o mesmo corpo (sem `completedAt`) e `completed: bool`.
3. O front grava as respostas em `PUT /profile`, `POST /profile/experiences` e demais listas, e em `PUT /consents/{purpose}`. O `/onboarding` nunca guarda valores do perfil.
4. Toda pergunta pode ser adiada (RN-14), inclusive os consentimentos, que continuam nascendo desligados.
5. O endereço completo não é perguntado no onboarding: só cidade, UF e CEP. O restante fica em Dados sensíveis.
6. Concluir gera o evento de auditoria `onboarding.completed`, sem conteúdo.
7. Sem anúncio em nenhuma tela do onboarding (RN-08).

## Consequências

**Positivas**
- Perfil mais completo com menos atrito, e a pessoa sabe o que falta.
- Nada obrigatório: respeita quem tem receio de dar certos dados.
- O perfil continua sendo a única fonte da verdade; `/onboarding` é só progresso.

**Negativas**
- Mais telas para manter e testar.
- Mais um recurso na API e uma tabela pequena (`OnboardingProgress`).

**Riscos e mitigação**
- **A pessoa pula tudo.** Mitigação: alerta no Perfil Mestre com as pendentes que mais aparecem nos formulários e "Responda agora, em 10 segundos"; a extensão marca esses campos como "Precisa de você" com "Responda aqui".
- **Progresso e perfil fora de sincronia** (passo marcado como respondido sem o dado no perfil). Mitigação: o alerta de pendentes é calculado também a partir do perfil (`GET /profile/missing`), não só do `skipped`.
- **Perguntas da área erradas para quem escolheu "Outra área".** Mitigação: listas vazias com cadastro livre (ADR-0019).

## Conformidade

- Teste de integração: `PUT /onboarding` com um id de passo desconhecido responde 400; com ids válidos, `GET` devolve o mesmo conteúdo.
- Teste de integração: `PUT /onboarding` não aceita nem guarda valores do perfil (só ids e `completed`).
- E2E: pular uma pergunta faz ela aparecer no alerta do Perfil Mestre; fechar e reabrir retoma em `lastStep`.
- E2E: nenhuma chamada a `GET /ads` durante o onboarding (TN-11).
- Revisão de UX: "Preencher mais tarde" visível e alcançável por teclado em todas as perguntas.

## Links

- [DRP RF-23](../../01-produto/drp.md), [Fluxos](../../01-produto/fluxos.md), [Frontend web §5.2](../frontend-web.md)
- [Resultados da pesquisa v2](../../05-pesquisa/resultados-v2.md), [Por que o Zyft](../../00-contexto/por-que-o-zyft.md)
- Mockup `BoasVindas.dc.html` (design da v2)
