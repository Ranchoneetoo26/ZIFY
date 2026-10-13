# DVP-E — Documento de Visão do Produto e Escopo

## Controle do Documento

| Campo | Valor |
|---|---|
| Projeto | Zyft |
| Documento | Visão do Produto e Escopo (DVP-E) |
| Versão | 12.0 |
| Status | Aprovado para desenvolvimento |
| Documentos relacionados | DRP, DVS, DAT, Arquitetura de Backend |

**Histórico de revisões**

| Versão | Alteração |
|---|---|
| 1.0 | Versão inicial — visão, problema, diferencial e escopo do MVP |
| 2.0 | Adição de stakeholders, personas, matriz de sucesso quantificada e glossário; revisão de consistência com DRP/DVS/DAT |
| 3.0 | Adição de identidade visual (paleta primário amarelo / secundário preto) como pilar de posicionamento; referência ao Guia de Estilo e Design System |
| 4.0 | Adição ao escopo dos requisitos de robustez técnica e conformidade (termos, downgrade, segurança de integrações), formalizados no DRP como RF-17 a RF-19 e RNF-09 a RNF-12 |
| 5.0 | Definição do nome da marca: **Farol**. Nome incorporado ao sumário executivo, explicando a metáfora (reputação em destaque = prioridade), consistente com a paleta amarelo/preto já definida |
| 6.0 | Renomeação da marca de Farol para **Lighthouse** (tradução em inglês, mesma metáfora e racional de posicionamento) |
| 7.0 | Renomeação da marca de Lighthouse para **Zyft** — nome sem ligação semântica pretendida; justificativa da paleta de cores desvinculada da metáfora de "luz/farol" e reescrita com base em função de uso (destaque de prioridade) |
| 7.1 | Revisão final de consistência: adicionados os documentos Arquitetura de Frontend e Guia Mestre de Execução ao conjunto do projeto, referenciados em "Documentos relacionados" |
| 7.2 | Revisão técnica: nenhuma mudança de escopo; documento agora referencia o Guia do Agente Supervisor |
| 8.0 | **Expansão de escopo: produto passa a cobrir também contratação CLT, não só PJ.** Modalidade (PJ/CLT) tratada como dimensão ortogonal ao modo de contratação (projeto aberto/banco de talentos). Reescritos: problema, personas, pilares do diferencial, escopo do MVP, glossário; adicionada restrição explícita de que o Zyft não assume nenhuma obrigação trabalhista de contratações CLT |
| 8.1 | Corrigida referência residual a "mercado brasileiro de PJ TI" para cobrir as duas modalidades |
| 9.0 | **Alinhamento final de planos e acessos.** Revisão de preços (mensal, semestral e anual) e refinamento das regras de visibilidade cruzada (CLT acessa PJ sem candidatura) e exclusividade total de perfis. |
| 10.0 | **Adição do plano Dual (PJ + CLT).** Introdução da trilha de assinatura unificada para profissionais que desejam atuar em ambas as modalidades simultaneamente. |
| 11.0 | **Otimização de Limites e Competitividade.** Atualização do escopo com limites de candidaturas e oportunidades definidos para garantir vantagem competitiva no mercado de TI. |
| 12.0 | **Reescrita das personas.** Substituídas as personas genéricas por personas detalhadas (Júlia Martins — iniciante PJ; Leonardo Almeida — experiente PJ/CLT) e adicionada a antipersona Roberto Ferreira (tradicional, presencial/CLT) |

## 1. Sumário executivo

**Zyft** é um marketplace que conecta profissionais de tecnologia a empresas contratantes, cobrindo tanto contratação **PJ** (prestação de serviço, por projeto) quanto **CLT** (vínculo empregatício, via vaga) na mesma plataforma e mecânica, diferenciando-se por um modelo de assinatura fixa (sem comissão sobre a contratação, em nenhuma das duas modalidades) e por um sistema de reputação bilateral público. O nome foi escolhido por sonoridade e disponibilidade, sem ligação semântica pretendida com a proposta de valor. O MVP cobre cadastro, publicação de oportunidades (projeto PJ ou vaga CLT), banco de talentos com matching automático, avaliação bilateral e cobrança por assinatura, com foco inicial no mercado brasileiro.

## 2. Stakeholders

| Stakeholder | Papel |
|---|---|
| Idealizador / Product Owner | Define visão, prioriza escopo, aprova entregas |
| Empresas contratantes | Usuário pagante — publica projetos PJ ou vagas CLT, consulta banco de talentos, avalia profissionais |
| Profissionais (PJ e/ou CLT) | Usuário pagante — busca oportunidades na modalidade que aceita, é avaliado e avalia empresas |
| Equipe de desenvolvimento | Implementa o produto conforme DRP/DAT |
| Futuro investidor/parceiro comercial | Interessado na viabilidade econômica (ver DVS) |

## 3. Visão do produto

**Declaração de visão:**
> "Ser o marketplace de referência para contratação de TI no Brasil — PJ e CLT — com um modelo de assinatura justo (sem comissão sobre a contratação, em qualquer modalidade) e reputação bilateral pública, resolvendo a falta de confiança e a concorrência predatória de preço que hoje afastam bons profissionais e boas empresas dos marketplaces genéricos."

## 4. Problema a resolver

**Para profissionais (PJ ou CLT):**
- Concorrência predatória por preço em plataformas genéricas (mais relevante para PJ, onde o profissional negocia o próprio valor).
- Comissões e taxas de colocação que reduzem o retorno do processo — seja um pedaço do valor do projeto (PJ) ou uma taxa de contratação paga pela empresa que indiretamente pressiona o processo (CLT).
- Falta de informação prévia sobre a idoneidade/reputação da empresa contratante, em qualquer modalidade.

**Para empresas contratantes:**
- Processos de recrutamento tradicional lentos e caros, tanto para contratações pontuais (PJ) quanto para vagas efetivas (CLT).
- Dificuldade de validar senioridade real além do currículo.
- Falta de visibilidade sobre o histórico do profissional em projetos ou processos anteriores.

## 5. Público-alvo e personas

O Zyft atende duas **modalidades de contratação** dentro da mesma plataforma e mecânica — PJ (prestação de serviço, por projeto) e CLT (vínculo empregatício, via vaga). Modalidade é uma dimensão independente do modo de contratação (projeto aberto vs. banco de talentos, ver Pilar 5) — uma empresa pode buscar tanto um profissional PJ quanto contratar para uma vaga CLT usando o mesmo banco de talentos.

### Persona 1 — Júlia Martins ("Iniciante em busca do primeiro projeto")

| Campo | Valor |
|---|---|
| Idade | 23 anos |
| Profissão | Estudante de Engenharia de Software |
| Escolaridade | Graduação em andamento — último ano |
| Modelo de trabalho desejado | PJ / Home Office |
| Perfil | Iniciante na carreira, entusiasta de tecnologia e orientada ao desenvolvimento profissional |

**Contexto**

Júlia está no último ano de Engenharia de Software e busca oportunidades para iniciar sua carreira profissional antes mesmo de concluir a graduação. Apesar de possuir conhecimentos adquiridos na faculdade e em projetos pessoais, sente que precisa de mais experiências práticas para construir um portfólio competitivo.

Para facilitar a prestação de serviços, Júlia abriu um MEI em seu nome e passou a procurar projetos que possam ser realizados de forma remota. Seu objetivo inicial é encontrar trabalhos compatíveis com seu nível de experiência, mas ela pretende evoluir gradualmente para projetos mais complexos e de maior responsabilidade.

**Objetivos**
- Conseguir seus primeiros projetos profissionais na área de tecnologia.
- Construir um portfólio que demonstre suas habilidades técnicas.
- Adquirir experiência prática trabalhando com clientes e projetos reais.
- Encontrar oportunidades compatíveis com seu nível atual de conhecimento.
- Desenvolver novas habilidades por meio dos projetos.
- Aumentar gradualmente o valor e a complexidade dos projetos que consegue assumir.
- No futuro, conquistar uma posição profissional mais consolidada na área de tecnologia.

**Necessidades**
- Ter acesso a projetos confiáveis e bem descritos.
- Encontrar oportunidades adequadas para profissionais em início de carreira.
- Conseguir apresentar seu portfólio e suas habilidades de forma clara.
- Ter informações sobre requisitos, tecnologias utilizadas, prazo e remuneração antes de demonstrar interesse.
- Sentir segurança ao negociar e iniciar um projeto com um novo cliente.

**Dores e dificuldades**
- Pouca experiência profissional comprovada.
- Dificuldade para competir com profissionais mais experientes.
- Receio de aceitar projetos acima de sua capacidade técnica.
- Dificuldade para encontrar projetos que aceitem profissionais iniciantes.
- Insegurança sobre a confiabilidade dos clientes e das propostas.
- Poucas referências sobre quanto cobrar por determinados serviços.

**Comportamento**

Júlia pesquisa oportunidades principalmente pela internet e costuma comparar diferentes projetos antes de se candidatar. Dá bastante importância à descrição da vaga, às tecnologias envolvidas, ao nível de experiência exigido e às possibilidades de aprendizado.

Ela tende a priorizar oportunidades que possam contribuir para seu portfólio, mesmo que inicialmente a remuneração não seja o principal fator de decisão.

**Motivação principal**

Construir experiência profissional e transformar seus conhecimentos acadêmicos em projetos reais.

**Frase**

> "Quero uma oportunidade que me ajude a entrar no mercado e, ao mesmo tempo, me permita mostrar do que sou capaz."

### Persona 2 — Leonardo Almeida ("Experiente sem fronteiras geográficas")

| Campo | Valor |
|---|---|
| Idade | 27 anos |
| Profissão | Profissional de tecnologia |
| Escolaridade | Pós-graduação concluída |
| Experiência | Profissional com experiência no mercado de trabalho |
| Modelo de trabalho desejado | PJ ou CLT |
| Perfil | Experiente, pragmático e orientado a oportunidades |

**Contexto**

Leonardo já está inserido no mercado de trabalho há alguns anos e possui experiência em diferentes projetos e ambientes profissionais. Atualmente trabalha ou já trabalhou em posições no regime CLT, mas deseja ampliar suas possibilidades profissionais e aumentar sua exposição a novos projetos.

Por morar em uma cidade pequena, Leonardo encontra uma quantidade limitada de oportunidades presenciais na sua área. Por isso, utiliza a internet como principal meio para descobrir novos projetos e vagas, especialmente oportunidades que permitam trabalho remoto.

Embora esteja buscando inicialmente projetos no modelo PJ, Leonardo não descarta oportunidades CLT. Para ele, o mais importante é encontrar uma oportunidade profissional que seja compatível com sua experiência, remuneração e objetivos de carreira.

**Objetivos**
- Encontrar projetos profissionais que aproveitem sua experiência.
- Ampliar sua rede de contatos e oportunidades profissionais.
- Trabalhar em projetos mais desafiadores e relevantes para sua carreira.
- Encontrar oportunidades remotas que não dependam da localização geográfica.
- Avaliar propostas PJ e CLT de acordo com seus benefícios e condições.
- Aumentar sua remuneração e melhorar suas condições profissionais.
- Evitar períodos sem trabalho entre um projeto e outro.

**Necessidades**
- Ter acesso a uma quantidade maior de oportunidades.
- Conseguir filtrar projetos por tecnologia, remuneração, modelo de contratação e nível de experiência.
- Encontrar informações claras sobre as condições do projeto.
- Conseguir avaliar rapidamente se uma oportunidade vale seu tempo.
- Ter segurança de que a empresa ou contratante é confiável.
- Poder apresentar sua experiência profissional e qualificações de maneira objetiva.

**Dores e dificuldades**
- Baixa quantidade de oportunidades na cidade onde mora.
- Dependência de plataformas online para encontrar novas oportunidades.
- Muitas vagas com informações incompletas ou pouco transparentes.
- Processos seletivos demorados.
- Projetos incompatíveis com seu nível de experiência.
- Dificuldade para comparar propostas PJ e CLT.
- Perda de tempo analisando oportunidades que não correspondem às suas expectativas.

**Comportamento**

Leonardo utiliza sites de emprego, plataformas profissionais e sites especializados em projetos para acompanhar novas oportunidades. Normalmente avalia primeiro remuneração, modelo de contratação, requisitos técnicos, localização e possibilidade de trabalho remoto.

Por já possuir experiência profissional, tende a ser mais seletivo do que alguém em início de carreira. Ele não procura simplesmente "qualquer oportunidade", mas uma proposta que faça sentido considerando seu conhecimento e experiência.

**Motivação principal**

Ter acesso a oportunidades melhores sem ficar limitado às empresas e vagas disponíveis na sua cidade.

**Frase**

> "Se a oportunidade for boa, não importa se é PJ ou CLT. O importante é fazer sentido para minha carreira."

### Antipersona — Roberto Ferreira ("Tradicional e presencial")

| Campo | Valor |
|---|---|
| Idade | 35 anos |
| Profissão | Gerente |
| Experiência | Profissional experiente no mercado de trabalho |
| Modelo de trabalho preferido | Presencial / CLT |
| Perfil | Tradicional, resistente a novas formas de contratação e pouco adaptado à busca digital por oportunidades |

**Contexto**

Roberto possui uma carreira consolidada e está acostumado com os métodos tradicionais de contratação. Quando busca uma nova oportunidade, prefere estabelecer contato presencial com empresas e participar de processos seletivos convencionais.

Ele não possui interesse em trabalhar como PJ, pois considera que esse modelo não oferece vantagens suficientes em comparação ao regime CLT. Também demonstra resistência em utilizar plataformas digitais para procurar oportunidades profissionais, pois acredita que uma conversa presencial permite conhecer melhor a empresa e avaliar a oportunidade.

**Objetivos**
- Encontrar uma vaga CLT estável.
- Trabalhar presencialmente.
- Ter contato direto com recrutadores e gestores.
- Manter os benefícios e a segurança associados ao regime CLT.
- Evitar processos de contratação realizados exclusivamente pela internet.

**Necessidades**
- Contato presencial com a empresa.
- Processos seletivos tradicionais.
- Informações claras sobre benefícios e estabilidade.
- Relação direta com recrutadores e gestores.

**Dores e dificuldades**
- Baixa confiança em processos seletivos totalmente digitais.
- Resistência ao modelo de contratação PJ.
- Dificuldade de adaptação a plataformas digitais de recrutamento.
- Pouco interesse em projetos temporários ou trabalhos independentes.
- Pode abandonar uma oportunidade quando o processo exige muitas etapas online.

**Comportamento**

Roberto prefere procurar empresas presencialmente, entregar currículos e conversar diretamente com responsáveis pela contratação. Quando utiliza a internet, seu uso é limitado e geralmente serve apenas para obter informações sobre uma empresa antes de realizar um contato presencial.

Ao encontrar uma oportunidade PJ, tende a descartá-la independentemente das demais características do projeto.

**Por que Roberto é uma antipersona?**

Roberto representa um usuário que não possui aderência ao modelo de contratação e ao comportamento que a plataforma pretende incentivar.

A solução é direcionada principalmente a profissionais que estão dispostos a encontrar projetos e oportunidades pela internet, especialmente no modelo PJ ou remoto. Como Roberto prefere contratação CLT, presencial e processos tradicionais, investir em funcionalidades específicas para atender suas necessidades teria baixo valor para o público principal.

**Frase**

> "Prefiro conversar pessoalmente com a empresa e ser contratado pela CLT. Não vejo vantagem em procurar esse tipo de oportunidade pela internet."

## 6. Diferencial competitivo (posicionamento)

> "Marketplace de TI com avaliação bilateral pública e sem comissão sobre a contratação — seja um projeto PJ ou uma vaga CLT — você paga uma assinatura previsível, não um pedágio."

Pilares do diferencial (referenciados nos requisitos do DRP e na arquitetura do DAT):

| # | Pilar | Requisito relacionado (DRP) |
|---|---|---|
| 1 | Assinatura fixa nos dois lados, sem comissão sobre a contratação — nem sobre o valor do projeto PJ, nem como taxa de colocação sobre a vaga CLT | RF-10, RF-11, RF-12 |
| 2 | Avaliação bilateral pública | RF-06 |
| 3 | Foco exclusivo em TI, cobrindo as duas modalidades de contratação mais usadas no setor no Brasil (PJ e CLT) | Escopo (seção 7) |
| 4 | Validação de senioridade cruzando experiência + certificações + avaliações | RF-07 |
| 5 | Dois modos de contratação nativos (projeto aberto e banco de talentos) — ortogonal à modalidade (PJ/CLT); qualquer modo funciona para qualquer modalidade | RF-03, RF-04, RF-05 |
| 6 | Identidade visual própria (primário amarelo / secundário preto), reforçando o conceito de prioridade e sobriedade profissional | RF-13 a RF-16, RNF-07, RNF-08 — ver Guia de Estilo e Design System |

Concorrentes de referência: Workana, 99Freelas (generalistas, majoritariamente PJ), GeekHunter, Revelo (recrutamento CLT tradicional, cobram taxa de colocação), Comunidade Crowd (especialistas em TI). Nenhum combina os seis pilares acima simultaneamente, e nenhum aplica o mesmo modelo de assinatura sem comissão às duas modalidades ao mesmo tempo.

**Identidade de marca:** o nome Zyft não carrega significado pretendido — a escolha de cor não deriva mais de uma metáfora do nome. A paleta primário amarelo (`#FFC629`) / secundário preto (`#121212`) permanece definida pela função que exerce na interface: amarelo sinaliza prioridade e destaque (ex.: nota de avaliação alta, CTAs), preto comunica solidez e seriedade (é uma ferramenta de trabalho, não um app casual). Detalhamento completo de cor, tipografia e princípios de UX está no Guia de Estilo e Design System.

**Domínio:** `usefarol.com.br` e `uselighthouse.com` eram reservas para nomes anteriores e não se aplicam a Zyft. Sugestões a validar: `zyft.com`, `zyft.io`, `usezyft.com` — disponibilidade a confirmar diretamente no registrador antes de qualquer compra.

## 7. Escopo do produto (MVP)

### Dentro do escopo
- Cadastro e perfil de empresa (nome, cidade, tipo de trabalho oferecido).
- Cadastro e perfil de profissional (dados pessoais, experiências, cursos, diplomas, grau de experiência) — um único cadastro, independente da modalidade de contratação que o profissional aceita (PJ, CLT, ou ambas).
- Publicação de oportunidades por empresas — **projeto PJ** (horas necessárias, grau de experiência exigido) ou **vaga CLT** (faixa salarial, grau de experiência exigido), com a modalidade escolhida explicitamente na publicação.
- Envio de interesse pelo profissional em um clique, em oportunidades de qualquer modalidade.
- Banco de talentos com pedido de perfil específico pela empresa (incluindo a modalidade desejada) e matching automático.
- Sistema de avaliação por estrelas (1-5) bilateral, visível apenas para assinantes ativos — aplicável a projetos PJ concluídos e a vagas CLT após a contratação confirmada.
- Validação de senioridade cruzando experiência + certificações + avaliações.
- Chat interno entre empresa e profissional.
- Liberação manual de dados de contato (e-mail/WhatsApp).
- Modelo de assinatura: 3 tiers (Iniciante, Intermediário, Profissional) × 3 periodicidades (mensal, semestral, anual), valores distintos para empresa e profissional — inclusão da trilha Dual (PJ + CLT) com acesso total às duas modalidades.
- Trial gratuito de 30 dias no tier Iniciante, sem cartão de crédito.
- Aceite de Termos de Uso e Política de Privacidade, versionado e com reaceite obrigatório em caso de mudança.
- Downgrade de plano sem exclusão de dados, com bloqueio apenas de novas oportunidades acima do novo limite.
- Robustez técnica de lançamento: rate limiting em endpoints sensíveis, varredura de segredos expostos e fallback para integrações externas críticas (pagamento, e-mail).

### Fora do escopo (nesta versão, mas extensível — ver seção 6 do DAT)
- Comissão sobre projeto fechado (em standby, avaliada em versão futura).
- Categorias fora de TI (redação, fotografia, etc.).
- Escrow/custódia de pagamento entre as partes.
- Folha de pagamento, benefícios e compliance trabalhista (diferente da Revelo).
- Aplicativo mobile nativo (fase futura).

## 8. Objetivos de negócio

- Validar a tração do modelo de assinatura dupla sem comissão frente ao mercado brasileiro de TI, nas duas modalidades (PJ e CLT).
- Atingir massa crítica de profissionais cadastrados no banco de talentos para tornar o matching relevante desde o lançamento.
- Construir uma base de avaliações bilaterais que sirva como ativo de confiança e barreira de entrada para concorrentes.

## 9. Métricas de sucesso

| Métrica | Definição | Meta de referência para o MVP |
|---|---|---|
| Conversão trial → pago | % de trials do tier Iniciante que viram assinatura paga | A validar com dados reais; acompanhar desde o lançamento |
| Usuários ativos pagantes | Empresas e profissionais com assinatura ativa | Massa crítica mínima para viabilizar matching (ver DVS, seção 4) |
| Projetos fechados por canal | Nº de projetos fechados via banco de talentos vs. projeto aberto | Acompanhar proporção para calibrar investimento em cada modo |
| Preenchimento de avaliação bilateral | % de projetos concluídos com avaliação registrada dos dois lados | Meta alta, pois é o ativo central de confiança do produto |
| Churn mensal por plano | % de cancelamento mensal, por plano e tipo de usuário | Acompanhar por plano para calibrar limites de projeto |

## 10. Restrições e premissas

- Modelo de monetização já definido (ver tabela de planos no DRP) — não deve ser alterado nesta fase sem nova validação de mercado.
- Produto nasce web-first.
- Foco geográfico inicial: Brasil.
- Receita depende inteiramente de assinatura nesta fase (sem comissão) — ver análise de risco no DVS.
- **Exclusividade de Perfis e Acessos:** O sistema garante separação total entre Empresa, PJ e CLT. Um usuário Empresa não acessa fluxos de profissionais, e um profissional (PJ ou CLT) não acessa fluxos de empresa. Entre profissionais, a candidatura é restrita à modalidade da assinatura, mas usuários CLT possuem privilégio de visualização de projetos PJ em todos os tiers.
- **O Zyft não é parte na relação de trabalho, seja PJ (prestação de serviço) ou CLT (vínculo empregatício)** — atua só como intermediário de conexão, comunicação e avaliação. Todas as obrigações trabalhistas de uma contratação CLT (registro em carteira, FGTS, férias, 13º salário, rescisão) são inteiramente da empresa contratante, nunca do Zyft. Essa distinção precisa estar clara nos Termos de Uso.

## 11. Glossário

| Termo | Definição |
|---|---|
| PJ | Modalidade de contratação por prestação de serviço via pessoa jurídica (CNPJ/MEI), por projeto — sem vínculo empregatício |
| CLT | Modalidade de contratação por vínculo empregatício formal (carteira assinada), regida pela Consolidação das Leis do Trabalho — via vaga, não por projeto |
| Oportunidade | Termo guarda-chuva usado nesta documentação para "projeto PJ" ou "vaga CLT" — a empresa publica uma oportunidade especificando a modalidade |
| Modalidade de contratação | Dimensão que distingue PJ de CLT, independente do modo de contratação (projeto aberto ou banco de talentos) |
| Banco de talentos | Base de profissionais disponíveis para matching automático por perfil solicitado pela empresa, em qualquer modalidade |
| Matching | Processo de sugerir profissionais compatíveis com um perfil solicitado |
| Avaliação bilateral | Avaliação em que empresa e profissional avaliam um ao outro — ao final do projeto (PJ) ou após a contratação confirmada (CLT) |
| Trial | Período de teste gratuito, aqui de 30 dias, sem necessidade de cartão de crédito |
| Grau de experiência validado | Nível de senioridade calculado pelo sistema, cruzando experiência declarada, certificações e avaliações |
