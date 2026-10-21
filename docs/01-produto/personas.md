# Personas do MVP 1

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Personas de necessidade e personas narrativas |
| Versão | 2.0 |
| Status | Validado em parte pela pesquisa v2 (n = 15). Beatriz Lima é hipótese a validar. |
| Dono | Product Owner (com Product Manager e UX) |
| Documentos relacionados | [Resultados da pesquisa v2](../05-pesquisa/resultados-v2.md), [DVP-E §4](dvp-e.md#4-público-alvo-e-personas), [Fluxos](fluxos.md), [Design system](../03-arquitetura/design-system.md), [ADR-0019](../03-arquitetura/adr/0019-area-profissional-dinamica-e-campos-personalizados.md), personas antigas em [`arquivo/marketplace-v1/dvp-e.md`](../arquivo/marketplace-v1/dvp-e.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | Personas de necessidade PN1–PN5 conforme o reposicionamento; arquétipos da pesquisa como lente secundária; antipersona. Substituem Júlia, Leonardo e Roberto, que eram personas do marketplace. |
| 2.0 | 21/10/2026 | Resultado da pesquisa v2 aplicado a cada PN (PN1, PN2 e PN4 confirmadas como perfil; PN3 e PN5 não confirmadas). Entram as personas narrativas Rafael (primária) e Bia (secundária), construídas a partir das respostas, e Beatriz Lima (Saúde), hipótese ligada à área profissional dinâmica. Entra o arquétipo de marca (Cuidador com traço de Sábio). |

### Por que as personas mudaram duas vezes

1. **Marketplace → necessidades (v1.0, 12/10).** Júlia (iniciante PJ), Leonardo (experiente PJ/CLT) e a antipersona Roberto descreviam quem contrata e quem é contratado num marketplace de TI. Com o reposicionamento ([ADR-0001](../03-arquitetura/adr/0001-reposicionamento-mvp1-candidatura-assistida.md)), o produto passou a servir quem se candidata, de qualquer área. Personas demográficas de TI deixaram de fazer sentido e foram trocadas por cinco **categorias de necessidade** (PN1–PN5).
2. **Necessidades → necessidades com dados e rostos (v2.0, 21/10).** A pesquisa v2 trouxe 15 respostas válidas. Os IDs PN1–PN5 continuam os mesmos, porque o DRP, os critérios de aceite e o questionário apontam para eles. O que muda é que cada PN agora tem um resultado medido, e o time ganhou três personas narrativas para tomar decisões de tela e de texto. Uma persona narrativa não substitui as PN: ela junta as necessidades mais fortes num retrato que dá para lembrar numa reunião.

## 1. Como ler este documento

- **PN1–PN5** são categorias de necessidade. Uma pessoa pode ter mais de uma, e a necessidade dominante muda com o momento.
- **Rafael, Bia e Beatriz** são personas narrativas. Rafael e Bia saem das respostas da pesquisa; Beatriz é hipótese.
- Todos os números vêm de [resultados-v2.md](../05-pesquisa/resultados-v2.md). Amostra pequena (n = 15) e concentrada em tecnologia (60%). Os números são sinal de direção, não prova estatística.

Critérios do [plano de pesquisa §5](../05-pesquisa/plano-de-pesquisa.md#5-critérios-de-validação):

- **Persona confirmada:** escolhida por pelo menos 15% das respostas, com identificação média de pelo menos 3,5.
- **Dor validada:** média de pelo menos 3,5, pelo menos 50% de notas 4–5 e pelo menos 15 respostas.

## 2. Resultado da validação

| ID | Persona de necessidade | Perfil no questionário | Escolha | Identificação média | Dor ligada (média; % notas 4–5) | Situação |
|---|---|---|---|---|---|---|
| PN1 | Quer reduzir tarefas repetitivas | A — Quer agilidade | 5 (33%) | 4,0 | Redigitar dados: 4,40; 80% | **Confirmada.** Persona e dor passam. É a necessidade principal. |
| PN2 | Precisa organizar suas informações | B — Quer organizar | 4 (27%) | 3,75 | Manter perfis: 3,60; 53% | **Confirmada**, no limite: no recorte do plano (n = 12) a dor cai para 42% de notas altas. |
| PN3 | Precisa de apoio na redação | C — Ajuda para escrever | 1 (7%) | 4,0 | Perguntas abertas: 3,27; 40% | **Não confirmada.** Mantida como necessidade secundária: 47% citam perguntas abertas entre as etapas que mais tomam tempo. |
| PN4 | Busca adequação à vaga | D — Quer saber se tem o perfil | 3 (20%) | 4,0 | Saber se tem o perfil: 2,33; 13% | **Confirmada como perfil, não como dor.** Comparar a vaga é recurso valorizado (80%), mas não incomoda. |
| PN5 | Precisa de orientação no preenchimento | E — Orientação no formulário | 0 (0%) | — | Entender o formulário: 2,00; 20%. Revisar: 2,20; 13% | **Não confirmada.** Nenhuma escolha e dores baixas. Fica no documento como "não validada". |

"Nenhum deles" teve 2 escolhas (13%), abaixo do corte de 20% que obrigaria a reescrever os perfis.

### O que isso muda

- **PN1 e PN2 guiam o produto.** Perfil único reaproveitado e importação de currículo foram os recursos mais valorizados (87% cada). O onboarding em passos e o Perfil Mestre existem por causa delas.
- **PN3 não some, mas deixa de ser o centro.** A IA para textos continua (RF-15), sempre como rascunho, e é o recurso de IA com limite no plano gratuito. A desconfiança também pesa: 33% têm receio de respostas abertas sugeridas por IA e 20% citam "não confiar em IA escrevendo por mim" como barreira.
- **PN4 vira recurso de apoio.** A análise de vaga (RF-17) continua Should. A comunicação fala em "entender o que a vaga pede", não em resolver uma angústia.
- **PN5 não justifica funcionalidade nova.** Os estados de campo e as explicações ficam, porque servem a todos, mas não há investimento específico para PN5 até haver evidência. O público da pesquisa estava à vontade com formulários (familiaridade média 4,07), o que explica o resultado e é também um viés: pessoas com pouca familiaridade digital quase não responderam.

## 3. Personas narrativas

### 3.1 Rafael Moreira, o profissional que não quer redigitar (primária)

> "Já escrevi isso dez vezes. Só quero conferir e seguir."

| Aspecto | Descrição |
|---|---|
| Quem é | 28 anos, analista de tecnologia (back-end), Uberlândia/MG. Empregado, buscando outra oportunidade. |
| Como se candidata | De 1 a 5 candidaturas por semana em LinkedIn, Gupy e sites de carreira das empresas. Cada uma leva de 15 a 30 minutos. |
| Necessidades | PN1 (principal) e PN2. |
| O que mais incomoda | Digitar experiências e formação item por item em cada site; criar cadastro de novo; manter currículo e perfis iguais. |
| Receios | Entregar CPF, RG e endereço; uma extensão que "lê tudo"; pagar por algo que funciona em poucos sites. |
| O que o Zyft faz por ele | Importa o currículo, monta o Perfil Mestre uma vez, mostra os campos Prontos de relance com "Aprovar os N" e guarda as respostas no Banco de respostas. |
| Plano provável | Gratuito no começo. Plus se a busca apertar. |
| Critério de sucesso | Terminar um formulário conhecido conferindo, não digitando, sem nenhum campo errado. |

**De onde veio.** É o retrato da maioria da amostra: 67% têm entre 26 e 30 anos, 60% buscam vagas em tecnologia, 67% têm de 5 a 10 anos de experiência, 73% usam LinkedIn e 47% usam Gupy. A frase dele resume a dor mais forte da pesquisa (redigitar dados: 4,40; 80% de notas 4–5). Entre os 6 respondentes que se candidatam toda semana, essa dor chega a 4,67.

**Usuário de demonstração:** `demo@zyft.local` / `Zyft@2026` (perfil de Tecnologia, completo). Ver o [guia do usuário](../06-operacao/guia-do-usuario.md).

### 3.2 Bia, a iniciante que trava nos textos (secundária)

> "Não sei o que escrever e tenho medo de exagerar."

| Aspecto | Descrição |
|---|---|
| Quem é | 20 anos, estudante procurando estágio (ou em transição de carreira). |
| Como se candidata | De 6 a 15 candidaturas por semana. Muitas vagas pedem texto livre ("Por que você quer este estágio?"). |
| Necessidades | PN3 e PN4. |
| O que mais incomoda | Perguntas abertas; não saber se tem o perfil da vaga; medo de exagerar e ser descartada. |
| Receios | Confia pouco num assistente (nota 2 de 5) e só usaria se fosse gratuito. |
| O que o Zyft faz por ela | "A vaga e você" mostra o que a vaga pede e onde isso aparece no perfil, sem nota. O rascunho da IA é "um ponto de partida", com os fatos usados à vista, e só é aprovado depois de ela marcar "Li e é verdade sobre mim". |
| Plano provável | Gratuito, com anúncio recompensado. |
| Critério de sucesso | Terminar uma pergunta aberta com as próprias palavras, partindo de um rascunho que não inventou nada. |

**De onde veio.** De uma resposta específica da pesquisa: até 20 anos, estudante, 6 a 15 candidaturas por semana, nota 5 para o incômodo com perguntas abertas, confiança 2, "não pagaria" e "só usaria se fosse totalmente gratuita". Ela não representa a maioria. Representa um grupo que o produto não pode deixar para trás: 47% citam perguntas abertas entre as etapas que mais tomam tempo, e quem ainda não tem trajetória longa é quem mais precisa de ajuda para escrever sem inventar.

**Tensão conhecida.** Com 6 a 15 candidaturas por semana (de 24 a 65 por mês), a Bia passa das 10 candidaturas assistidas do plano gratuito. O anúncio recompensado (+9 no mês) e o pacote avulso reduzem o problema, mas não o eliminam. Ver [monetização §8](monetizacao.md#8-riscos).

### 3.3 Beatriz Lima, técnica de enfermagem (hipótese a validar)

> "Os formulários de hospital pedem COREN, escala e plantão, e o currículo de TI não tem lugar para isso."

| Aspecto | Descrição |
|---|---|
| Quem é | 25 anos, técnica de enfermagem em Belo Horizonte/MG, área Saúde. Busca vagas em hospitais e clínicas, de preferência plantão ou escala. |
| Como se candidata | Sites de carreira de hospitais, portais regionais e grupos de WhatsApp. |
| Necessidades | PN1 e PN5 (hipótese). |
| O que ela precisa do produto | Registro profissional (COREN) no perfil; ferramentas da área (prontuário eletrônico, Tasy, MV Soul); tipo de pretensão "por plantão"; campos que nenhum catálogo conhece, como "disponível para plantão noturno". |
| O que o Zyft faz por ela | O onboarding pergunta pelo registro no conselho porque a área é Saúde; as sugestões de cargos, setores e competências são da área dela; "Informações da sua área" guarda os campos personalizados que a extensão usa quando o formulário perguntar algo parecido. |
| Critério de sucesso | Completar o perfil sem campos que não se aplicam a ela e ver a extensão reconhecer "COREN" num formulário. |

**Por que ela existe, e por que é hipótese.** A decisão de tornar a área profissional dinâmica ([ADR-0019](../03-arquitetura/adr/0019-area-profissional-dinamica-e-campos-personalizados.md)) diz que o Zyft serve qualquer área. Uma decisão dessas precisa de uma persona de fora da tecnologia que teste se ela se sustenta. Mas a pesquisa não dá base para afirmar como essa pessoa se comporta: **só 1 das 15 respostas é de Saúde (7%)**, e 60% são de tecnologia. Essa única resposta é de uma estudante procurando estágio que deu nota 5 para redigitar dados e para perguntas abertas, o que é coerente com a Beatriz, mas é uma pessoa só. Até haver respostas e entrevistas com profissionais de saúde, tudo o que está acima é suposição.

**Como validar:** divulgar a pesquisa em grupos de enfermagem e cursos técnicos de saúde (meta: pelo menos 10 respostas da área) e fazer 3 entrevistas com técnicos de enfermagem. Se as dores forem outras, a persona é reescrita ou descartada.

**Usuária de demonstração:** `beatriz@demo.zyft` / `Zyft@2026`, com onboarding parcialmente pulado para mostrar o alerta de pendentes.

### 3.4 Antipersona: quem quer candidatura em massa

| Aspecto | Descrição |
|---|---|
| Quem é | Quem quer disparar candidaturas sem ler as vagas ou automatizar a própria conta no portal ("auto apply"). |
| O que pede | Envio automático, várias vagas de uma vez, contorno de CAPTCHA, guardar a senha do portal, respostas "que aumentem a chance" mesmo exageradas. |
| Por que recusamos | Viola os termos dos portais, desperdiça o tempo de recrutadores, gera candidaturas ruins e contraria os princípios 1 a 3 do [DVP-E](dvp-e.md#6-princípios-do-produto). |
| Como o produto responde | Não há botão de envio, fila de vagas nem senha guardada. "Aprovar os N" vale só para campos Prontos e não sensíveis de **um** formulário. Nenhuma tela, anúncio ou botão usa "em massa", "com um clique", "piloto automático" ou "candidate-se a N vagas". |
| Relação com os planos | O Plus tira limites de uso, não tira a revisão. Plano pago nunca libera envio automático. |

## 4. Arquétipo de marca e arquétipos comportamentais

São duas coisas diferentes e não devem ser confundidas.

### 4.1 Arquétipo de marca: Cuidador, com traço de Sábio

Define a personalidade do Zyft (tom, cores, ícones). Detalhes em [design-system.md](../03-arquitetura/design-system.md).

| Arquétipo | Papel | Por quê (pesquisa) |
|---|---|---|
| **Cuidador** (principal) | Tira a repetição e guarda os dados com cuidado | A dor mais forte é redigitar (4,40). Os receios são de dados: 67% com CPF/RG, 40% com endereço, 40% citam privacidade como barreira. |
| **Sábio** (traço) | Explica de onde vem cada valor e o que a vaga pede, sem prometer | Comparar a vaga com o perfil é valorizado por 80%, e a confiança média (3,4) pede transparência, não entusiasmo. |
| **Mago** (evitar) | "A IA faz tudo por você" | 20% não confiam em IA escrevendo por eles e 33% temem respostas abertas sugeridas. Nada de brilhos, varinhas ou "mágica". |

### 4.2 Arquétipos comportamentais (lente secundária)

Descrevem como a pessoa age na busca. Continuam úteis para mensagens, mas não foram medidos diretamente na v2.

| Arquétipo | Como age | Necessidades | Mensagem |
|---|---|---|---|
| O Maratonista | Candidata-se com frequência | PN1, PN2 | "Pare de digitar as mesmas informações em cada site. Você revisa e decide o que vai." |
| O Estrategista | Escolhe poucas vagas | PN4, PN3 | "Entenda o que a vaga pede e mostre a melhor versão verdadeira de você." |
| O Iniciante Inseguro | Não sabe o que escrever | PN3, PN5 | "Um ponto de partida para o seu texto, com o que é verdade sobre você." |
| O Explorador | Está mudando de área | PN4, PN3, PN2 | "Mostre, com fatos do seu histórico, como o que você já fez serve para a nova área." |

A mensagem antiga do Maratonista ("candidate-se a 10 vagas no tempo que levava para 1") continua retirada: promete volume e se aproxima da antipersona.

## 5. Como usar as personas nas decisões

Diante de uma decisão de tela difícil, pergunte nesta ordem:

1. Isso ajuda o **Rafael** a conferir e seguir?
2. Isso deixa a **Bia** começar sem exagerar?
3. Isso funciona para a **Beatriz**, que não é de tecnologia? (Se a resposta depender de suposição, registre como hipótese.)
4. Isso abre alguma porta para a **antipersona**? Se sim, não entra.

## 6. Próximos passos de validação

| Lacuna | Ação | Dono |
|---|---|---|
| Amostra pequena (n = 15) e 60% de tecnologia | Segunda rodada da pesquisa com divulgação fora de tecnologia (meta de 60 respostas, nenhuma área acima de 50%) | PM |
| Beatriz sem base de dados | Pelo menos 10 respostas de Saúde e 3 entrevistas | PM, UX |
| PN5 sem escolha | Recrutar pessoas com baixa familiaridade digital para o teste de usabilidade | UX |
| Confiança abaixo da meta (3,4 < 3,5) | Medir a confiança depois do uso real no teste moderado (métrica M6) | PM, QA |
