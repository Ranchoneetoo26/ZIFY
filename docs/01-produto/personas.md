# Personas do MVP 1

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Personas de necessidade |
| Versão | 1.0 |
| Status | **Hipótese** — a validar com a [pesquisa v2](../05-pesquisa/plano-de-pesquisa.md) |
| Dono | Product Owner (com Product Manager e UX) |
| Documentos relacionados | [DVP-E §4](dvp-e.md#4-público-alvo-e-personas), [Fluxos](fluxos.md), [Questionário v2](../05-pesquisa/questionario-v2.md), personas antigas em [`arquivo/marketplace-v1/dvp-e.md`](../arquivo/marketplace-v1/dvp-e.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | Personas de necessidade PN1–PN5 conforme o reposicionamento; arquétipos da pesquisa como lente secundária; antipersona. Substituem Júlia, Leonardo e Roberto (marketplace). |

## 1. Como ler este documento

- As personas do MVP 1 são **categorias de necessidade**, não perfis demográficos. Uma mesma pessoa pode ter mais de uma necessidade, e a necessidade dominante muda com o momento (ex.: no primeiro uso, PN2; depois, PN1).
- Nada aqui é dado de pesquisa. **Tudo é hipótese** até a pesquisa v2 atingir o critério de validação (≥ 15 respostas, média ≥ 3,5 e ≥ 50% de notas 4–5 por dor). Depois dela, este documento ganha a versão 2.0 com os resultados reais, inclusive personas descartadas.
- As trajetórias de exemplo (primeiro emprego, experiente, transição de carreira, desemprego, baixa familiaridade digital) mostram que cada necessidade atravessa perfis diferentes. Elas não definem idade, gênero, renda ou área.

## 2. Resumo

| ID | Persona | Necessidade principal | Dores da pesquisa v2 ligadas | Funcionalidades do MVP 1 |
|---|---|---|---|---|
| PN1 | Quer reduzir tarefas repetitivas | Reutilizar informações e preencher formulários com menos digitação | Repetir os mesmos dados | RF-02, RF-11 a RF-14 |
| PN2 | Precisa organizar suas informações | Dados e experiências consistentes, revisados antes de usar | Manter currículo e perfis atualizados | RF-02 a RF-05, RF-16 |
| PN3 | Precisa de apoio na redação | Respostas claras para campos abertos, só com fatos confirmados | Responder perguntas abertas | RF-06, RF-15, RF-21 |
| PN4 | Busca adequação à vaga | Entender os requisitos visíveis e comparar com o próprio perfil, sem promessas | Saber se tenho o perfil | RF-17 |
| PN5 | Precisa de orientação no preenchimento | Entender campos, descobrir o que falta e revisar antes de salvar ou enviar | Entender o que o formulário pede; revisar sem errar | RF-05, RF-13, RF-18, RF-19 |

## 3. Personas

### PN1 — Quer reduzir tarefas repetitivas

| Aspecto | Descrição |
|---|---|
| Necessidade | Reaproveitar informações profissionais e terminar cada formulário com menos digitação, sem perder o controle do que vai. |
| Cenário típico | Encontrou uma vaga num portal que ainda não usava. O portal pede cadastro completo: dados pessoais, três experiências, formação, cursos e idiomas, que ela já digitou em outros sites. |
| Comportamentos | Candidata-se com frequência; tem um currículo pronto; copia e cola de um documento; usa o autofill do navegador quando funciona. |
| Frustrações | Formulários longos; blocos repetidos (Experiência 1, 2, 3); datas em formatos diferentes; perder o que digitou quando a página expira. |
| Como o Zyft ajuda | Perfil Mestre único; a extensão lê o formulário, propõe o preenchimento de cada campo (inclusive blocos repetidos) e preenche só o que ela aprovou. |
| O que **não** fazemos | Não enviamos a candidatura; não nos candidatamos a várias vagas de uma vez; não navegamos sozinhos pelo portal. |
| Critério de sucesso (hipótese) | Concluir o cadastro no portal suportado em menos tempo do que sem o Zyft, aceitando a maioria dos campos sem editar. |
| Perguntas da pesquisa v2 | Candidaturas por semana; tempo por candidatura; etapas que mais tomam tempo; dor "Preencher os mesmos dados em cada site"; perfil A. |
| Trajetórias de exemplo | Pessoa desempregada que se candidata a várias vagas por semana; profissional experiente com muitas experiências para cadastrar. |

### PN2 — Precisa organizar suas informações

| Aspecto | Descrição |
|---|---|
| Necessidade | Ter um lugar com as informações profissionais corretas e atualizadas, e revisar antes de usá-las. |
| Cenário típico | Tem um currículo de dois anos atrás, um perfil no LinkedIn mais novo e um cadastro antigo num portal regional. As datas e os cargos não batem entre eles. |
| Comportamentos | Mantém o currículo num arquivo Word ou PDF; atualiza só quando precisa; não lembra o que cadastrou em cada site. |
| Frustrações | Inconsistências entre sites; medo de passar informação errada; não saber qual versão está certa. |
| Como o Zyft ajuda | Importa o currículo como **proposta**, mostra cada campo para aprovar, pergunta só o que falta e compara o que já está no portal com o perfil (com "manter portal" por padrão). |
| O que **não** fazemos | Não substituímos dados do portal sem escolha da pessoa; não "corrigimos" fatos por conta própria. |
| Critério de sucesso (hipótese) | Sair do primeiro uso com um Perfil Mestre aprovado e sem divergências não resolvidas com o portal suportado. |
| Perguntas da pesquisa v2 | Dor "Manter currículo e perfis atualizados e iguais entre sites"; perfil B; importância de "Importar meu currículo". |
| Trajetórias de exemplo | Pessoa empregada que voltou a procurar vaga depois de anos; pessoa em transição de carreira que precisa reorganizar o histórico. |

### PN3 — Precisa de apoio na redação

| Aspecto | Descrição |
|---|---|
| Necessidade | Escrever respostas claras para campos abertos ("Fale sobre você", "Por que esta vaga?") usando só o que é verdade. |
| Cenário típico | O formulário pede um texto de até 500 caracteres sobre a experiência com atendimento ao cliente. Ela tem a experiência, mas não sabe como descrever. |
| Comportamentos | Deixa campos abertos por último; às vezes desiste da vaga; pede ajuda a amigos ou copia textos prontos da internet. |
| Frustrações | Não saber se o texto está bom; medo de exagerar ou de parecer genérica; escrever a mesma coisa várias vezes. |
| Como o Zyft ajuda | Sugere um texto baseado **só** em fatos aprovados do perfil, respeitando o limite do campo; reaproveita respostas aprovadas para perguntas parecidas; tudo editável. |
| O que **não** fazemos | Não inventamos experiências, resultados ou competências; não aprovamos texto por ela. |
| Critério de sucesso (hipótese) | Usar a sugestão (com ou sem edição) na maioria dos campos abertos e declarar confiança no texto final. |
| Perguntas da pesquisa v2 | Dor "Responder perguntas abertas"; perfil C; receio de "respostas abertas escritas por IA"; importância de "Sugestão de texto só com fatos meus". |
| Trajetórias de exemplo | Pessoa em busca do primeiro emprego; pessoa com baixa familiaridade com escrita formal; profissional experiente sem tempo para personalizar cada texto. |

### PN4 — Busca adequação à vaga

| Aspecto | Descrição |
|---|---|
| Necessidade | Entender o que a vaga pede e o que no próprio histórico atende ou não, antes de decidir se candidatar. |
| Cenário típico | A vaga lista oito requisitos, dois deles "desejáveis". Ela não sabe se o curso técnico conta como "formação na área" nem se dois anos bastam. |
| Comportamentos | Escolhe poucas vagas; lê a descrição inteira; pesquisa a empresa. |
| Frustrações | Descrições confusas; requisitos implícitos; perder tempo com vagas que não combinam. |
| Como o Zyft ajuda | Lê os requisitos **visíveis**, marca cada um como "atende", "parcial", "não encontrado" ou "confirmar", e mostra a linha do perfil que serve de evidência. |
| O que **não** fazemos | Não damos nota, porcentagem ou promessa de compatibilidade; não deduzimos o que não está escrito na vaga. |
| Critério de sucesso (hipótese) | Considerar a análise útil para decidir e não encontrar nenhuma evidência falsa. |
| Perguntas da pesquisa v2 | Dor "Saber se tenho o perfil que a vaga pede"; perfil D; importância de "Comparar a vaga com meu perfil". |
| Trajetórias de exemplo | Pessoa em transição de carreira; pessoa experiente e seletiva; estudante que não sabe se pode concorrer. |

### PN5 — Precisa de orientação no preenchimento

| Aspecto | Descrição |
|---|---|
| Necessidade | Saber o que cada campo pede, o que está faltando e revisar tudo antes de salvar ou enviar. |
| Cenário típico | O formulário tem campos obrigatórios sem explicação ("Regime pretendido", "Disponibilidade"), uma pergunta sobre deficiência e um aceite de termos. Ela não sabe o que é obrigatório responder. |
| Comportamentos | Preenche devagar; tem medo de errar e não conseguir corrigir; pede ajuda; usa o celular mais do que o computador. |
| Frustrações | Formulários confusos; mensagens de erro vagas; insegurança sobre dados pessoais. |
| Como o Zyft ajuda | Painel lateral com cada campo e seu estado (pronto, ambíguo, ausente, ação sua), explicações em linguagem simples, aviso sobre campos sensíveis e de termos, e um resumo antes de preencher. |
| O que **não** fazemos | Não aceitamos termos nem respondemos dados sensíveis por ela; não resolvemos CAPTCHA. |
| Critério de sucesso (hipótese) | Concluir o fluxo sem ajuda externa e dizer que entendeu o que foi preenchido. |
| Perguntas da pesquisa v2 | Dores "Entender o que o formulário está pedindo" e "Revisar tudo antes de enviar sem errar"; perfil E; confiança numa ferramenta com revisão. |
| Trajetórias de exemplo | Pessoa com baixa familiaridade digital; pessoa desempregada há muito tempo voltando ao mercado; estudante no primeiro cadastro. |

## 4. Arquétipos comportamentais (lente secundária)

A pesquisa usa quatro arquétipos para entender **como** a pessoa age ao buscar vaga. Eles não substituem as personas de necessidade; servem para ajustar a comunicação. Também são hipóteses.

| Arquétipo | Como age | Necessidades mais prováveis | Mensagem de comunicação |
|---|---|---|---|
| O Maratonista | Candidata-se com frequência e quer agilidade. | PN1, PN2 | "Pare de digitar as mesmas informações em cada site. Você revisa e decide o que vai." |
| O Estrategista | Escolhe poucas vagas e quer acertar cada uma. | PN4, PN3 | "Entenda o que a vaga pede e mostre a melhor versão verdadeira de você." |
| O Iniciante Inseguro | Não sabe se tem o perfil nem o que escrever; às vezes desiste. | PN3, PN5 | "A gente ajuda a escrever e a entender cada campo, e você revisa tudo antes de enviar." |
| O Explorador | Está mudando de área e quer mostrar como a experiência se aplica. | PN4, PN3, PN2 | "Mostre, com fatos do seu histórico, como o que você já fez serve para a nova área." |

A mensagem original do Maratonista ("candidate-se a 10 vagas no tempo que levava para 1") foi **retirada**: promete volume e se aproxima da candidatura em massa, que está fora do produto. A nova mensagem fala de esforço repetitivo, não de quantidade de candidaturas.

## 5. Antipersona

| Aspecto | Descrição |
|---|---|
| Quem é | Quem quer **disparar candidaturas em massa** sem ler as vagas, ou automatizar a própria conta no portal sem supervisão (robôs, "auto apply"). |
| O que pede | Envio automático, várias vagas de uma vez, contorno de CAPTCHA, guardar a senha do portal, respostas "que aumentem a chance" mesmo que exageradas. |
| Por que recusamos | Viola os termos dos portais, desperdiça o tempo de recrutadores, gera candidaturas de baixa qualidade e contraria os princípios 1 a 3 do [DVP-E](dvp-e.md#6-princípios-do-produto). |
| Como o produto responde | Não existe botão de envio, nem fila de vagas, nem armazenamento de senha. A comunicação nunca promete volume. |

## 6. Como as personas serão validadas

1. A pesquisa v2 mede as dores ligadas a cada persona (seção 2) e pergunta com qual perfil (A–E, um por persona) a pessoa mais se identifica.
2. Uma persona é **confirmada** quando a dor ligada a ela passa no critério de validação **e** pelo menos 15% das pessoas escolhem o perfil correspondente com identificação ≥ 4. Esse corte de 15% é uma proposta inicial a revisar com o PM.
3. Personas não confirmadas continuam no documento, marcadas como "não validada", com o que as respostas mostraram.
4. Depois da pesquisa, as cinco a oito pessoas que aceitarem contato participam de entrevistas e dos testes de usabilidade do MVP 1.
