# Por que o Zyft existe e por que ele é assim

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | A história e o porquê das decisões (versão 2) |
| Versão | 1.0 |
| Status | Vigente |
| Dono | Product Owner |
| Documentos relacionados | [Histórico e reposicionamento](historico-e-reposicionamento.md), [Resultados da pesquisa v2](../05-pesquisa/resultados-v2.md), [Personas](../01-produto/personas.md), [Monetização](../01-produto/monetizacao.md), [DVP-E](../01-produto/dvp-e.md), [DRP](../01-produto/drp.md), ADRs [0001](../03-arquitetura/adr/0001-reposicionamento-mvp1-candidatura-assistida.md), [0018](../03-arquitetura/adr/0018-onboarding-em-passos-com-perguntas-adiaveis.md), [0019](../03-arquitetura/adr/0019-area-profissional-dinamica-e-campos-personalizados.md), [0020](../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 21/10/2026 | Primeira versão, escrita junto com a versão 2 do produto. |

Este texto conta, em ordem, de onde o Zyft veio, o que a pesquisa mostrou e por que cada decisão da versão 2 foi tomada. Quem quiser os números completos encontra em [resultados da pesquisa v2](../05-pesquisa/resultados-v2.md). Quem quiser as regras encontra no [DRP](../01-produto/drp.md).

## 1. O problema

Quem procura emprego pela internet repete o mesmo trabalho em cada site. Cada portal pede cadastro, dados pessoais, experiência por experiência, formação, cursos, idiomas e, no fim, perguntas abertas como "por que você quer trabalhar aqui?". O currículo já tem quase tudo isso, mas cada formulário pede de um jeito.

O resultado é cansaço, dados diferentes em cada site e a tentação de usar atalhos ruins: copiar textos prontos, exagerar no que escreve ou usar robôs que se candidatam sozinhos a dezenas de vagas.

## 2. De marketplace a assistente

O Zyft começou como um **marketplace de tecnologia** (profissionais PJ e CLT de um lado, empresas do outro, assinatura sem comissão e reputação). A documentação dessa fase está em [`arquivo/marketplace-v1/`](../arquivo/marketplace-v1/).

Em outubro de 2026 o foco mudou ([ADR-0001](../03-arquitetura/adr/0001-reposicionamento-mvp1-candidatura-assistida.md)). Três motivos pesaram:

1. **Um marketplace só funciona com os dois lados ao mesmo tempo.** Sem empresas, o profissional não vê valor; sem profissionais, a empresa não paga. Um assistente de candidatura entrega valor para uma pessoa sozinha, no primeiro uso.
2. **Já havia um protótipo funcionando.** Em 05 e 06/10 o time montou uma extensão de navegador que lia um formulário, cruzava com um perfil e preenchia só o que a pessoa aprovava, com uma IA local que apontava qual dado usar sem inventar valores.
3. **O perfil construído agora serve depois.** O Perfil Mestre é a mesma base de que um marketplace precisaria. O marketplace foi para o MVP 2, não para o lixo.

Assim nasceu o **MVP 1, a Candidatura Assistida**: Perfil Mestre, importação de currículo, extensão que preenche só o que foi aprovado, IA presa aos fatos do perfil e confirmação explícita antes de qualquer ação. Em 15/10 o backend passou a ser C#/.NET 8, por exigência da disciplina ([ADR-0017](../03-arquitetura/adr/0017-backend-em-csharp-dotnet-clean-architecture.md)). O comportamento do protótipo foi mantido; a tecnologia mudou.

## 3. O que a pesquisa mostrou

Entre 15 e 17/10/2026 rodamos a pesquisa v2 com quem se candidata a vagas. Ficaram **15 respostas válidas** (uma linha da planilha era cópia da pesquisa antiga do marketplace e foi excluída). É pouco, e a amostra tem viés, então tratamos os números como sinais fortes ou fracos, não como verdade estatística. As limitações estão na [seção 10 dos resultados](../05-pesquisa/resultados-v2.md#10-limitações).

### 3.1 O que se confirmou

| Achado | Número | O que quer dizer |
|---|---|---|
| Preencher os mesmos dados em cada site incomoda | média 4,40 de 5; 80% deram nota 4 ou 5 | É a dor central. Passou no critério que definimos antes de ir a campo (média ≥ 3,5 e ≥ 50% de notas 4–5). |
| Manter currículo e perfis iguais em todos os sites incomoda | 3,60; 53% | Passou, mas no limite. No recorte mais rigoroso do próprio plano (12 respostas) cai para 42%. |
| Preencher experiências e formação item por item é a etapa que mais toma tempo | 67% marcaram | Confirma onde o preenchimento assistido economiza. Criar conta no site vem logo atrás (60%). |
| Perfil único reaproveitado e importar o currículo são importantes | 87% cada | Os dois recursos de base do MVP 1 são os mais valorizados. |
| Revisar cada campo e poder exportar ou apagar os dados são importantes | 80% cada | A pessoa quer controle. Isso virou regra: essas funções nunca são pagas (RN-12). |

### 3.2 O que preocupa

| Achado | Número |
|---|---|
| Confiança num assistente que preenche, sem enviar nada sem confirmação | média 3,40 de 5 (a meta era 3,5) |
| Interesse em usar | média 3,73; 53% notas 4–5 |
| Receio de deixar o assistente preencher documentos (CPF, RG) | 67% |
| Barreiras para usar: preço, privacidade, funcionar em poucos sites | 47%, 40% e 40% |
| Não confiar em IA escrevendo por mim | 20% |

A confiança ficou um pouco abaixo da meta. Isso orientou a marca e a interface: o Zyft se apresenta como alguém que cuida e explica (arquétipo **Cuidador**, com traço de **Sábio**), não como uma IA que "faz tudo" (arquétipo Mago, que evitamos). Dados como CPF e endereço ficam trancados e são aprovados um a um. A IA aparece como rascunho, sempre marcado, e a pessoa só aprova depois de dizer que o texto é verdade sobre ela.

### 3.3 O que não se confirmou

Três dores que o produto assumia como importantes ficaram baixas:

| Dor | Média | % notas 4–5 |
|---|---|---|
| Responder perguntas abertas | 3,27 | 40% |
| Saber se eu tenho o perfil que a vaga pede | 2,33 | 13% |
| Entender o que o formulário está pedindo | 2,00 | 20% |

Isso mudou prioridades, sem tirar recursos do MVP 1:

- **Perguntas abertas** não é uma dor da maioria, mas é real para uma parte: 47% colocam essas perguntas entre as três etapas que mais tomam tempo. Por isso a sugestão de texto continua, como recurso de apoio, e a pessoa que sofre com isso virou a persona secundária (Bia).
- **Saber se tem o perfil** quase não incomoda, mas "comparar a vaga com o meu perfil" é considerado importante por 80%. A análise de vaga fica como recurso útil, não como promessa de resolver uma angústia. E segue sem nota nem porcentagem (RN-06).
- **Entender o formulário**: o público respondeu que se sente à vontade com formulários (média 4,07). A persona de orientação no preenchimento (PN5) não foi escolhida por ninguém e ficou marcada como não validada.

Duas perguntas de controle também deram sinais claros. "Não ter retorno das empresas" incomoda tanto quanto redigitar (4,40, 87%), e "lembrar onde já me candidatei" ficou em 3,00. O Zyft não resolve a primeira e a segunda é assunto do MVP 2. Elas ficaram registradas para o planejamento do MVP 2 e não mudaram o escopo atual (RN-07).

### 3.4 Preço

| Quanto pagaria por mês | Respostas |
|---|---|
| Não pagaria | 3 (20%) |
| Até R$ 10 | 7 (47%) |
| R$ 10 a R$ 20 | 2 (13%) |
| R$ 20 a R$ 40 | 2 (13%) |
| Mais de R$ 40 | 1 (7%) |

Sobre o modelo, 9 das 15 pessoas (60%) preferem **gratuito com limite de uso por mês**; 2 preferem pagar por pacote, 2 preferem assinatura sem limite e 2 só usariam se fosse totalmente gratuito.

Uma correção: a contagem bruta da planilha mostrava 4 pessoas que "não pagariam". Uma delas era a linha da pesquisa antiga. O número certo, com as 15 respostas válidas, é 3.

## 4. As decisões da versão 2 e o porquê de cada uma

### 4.1 Onboarding em passos, com toda pergunta adiável

**O que mudou.** Quem não tem currículo à mão monta o perfil respondendo cerca de 25 perguntas, uma por tela, em seis capítulos: Quem é você, Sua área, O que você busca, Sua bagagem, Sua apresentação e Combinados. Toda pergunta tem "Preencher mais tarde". As puladas aparecem no topo do Perfil Mestre, com "Responda agora".

**Por quê.** A etapa que mais toma tempo numa candidatura é preencher experiências e formação item por item (67%). Se o Zyft pedisse tudo de uma vez numa tela longa, repetiria o problema que quer resolver. Uma pergunta por vez reduz o esforço percebido, e poder pular evita que alguém desista por não saber, por exemplo, a pretensão salarial naquele momento. Perguntas sobre dados que 33% a 67% têm receio de entregar (pretensão salarial, documentos) não podem ser obrigatórias. Decisão: [ADR-0018](../03-arquitetura/adr/0018-onboarding-em-passos-com-perguntas-adiaveis.md); requisitos RF-23, RN-14 e RNF-17.

### 4.2 Área profissional dinâmica e campos personalizados

**O que mudou.** A pessoa escolhe uma área (Saúde, Tecnologia, Vendas e comercial, Administrativo e financeiro, Educação, Logística e operações, Jurídico ou Outra área). As sugestões de cargos, setores, ferramentas e competências, o link que costuma ser pedido (GitHub, Currículo Lattes) e a pergunta de registro profissional (COREN, OAB, CRC, cursos NR) mudam conforme a área. Em qualquer lista a pessoa pode cadastrar o próprio valor. E pode criar **campos personalizados**, como "Disponível para plantão noturno: sim", que a extensão usa quando um formulário pergunta algo parecido.

**Por quê.** O Zyft sempre se disse "para qualquer área", mas as sugestões e os exemplos eram de tecnologia. Um técnico de enfermagem que abrisse o produto veria "Git" e "Docker" como sugestão de ferramenta. Formulários de saúde, educação ou logística pedem coisas que um perfil genérico não tem.

Há um ponto que precisamos admitir: **60% das respostas da pesquisa vieram de pessoas de tecnologia**, e só uma veio da saúde. A pesquisa não prova que outras áreas sentem as mesmas dores. A decisão foi tomada pelo posicionamento do produto, não por evidência de outras áreas. Por isso a persona de outra área, a Beatriz (técnica de enfermagem), está marcada como **hipótese a validar**. O catálogo por área é estático e pequeno, fácil de corrigir quando ouvirmos pessoas dessas áreas.

Os campos personalizados têm uma trava: rótulos que pareçam dado sensível (raça, religião, saúde, deficiência, gênero, orientação, CPF, RG, senha) são recusados, e a pessoa é orientada a usar a área de dados sensíveis, que é trancada e fica fora da IA (RN-15). Decisão: [ADR-0019](../03-arquitetura/adr/0019-area-profissional-dinamica-e-campos-personalizados.md); requisitos RF-24 e RF-25.

### 4.3 Planos e anúncios

**O que mudou.** O Zyft passa a ter:

- plano **Gratuito**, com limites por mês (10 candidaturas assistidas, 5 sugestões de texto com IA, 10 análises de vaga, 2 importações de currículo, até 15 respostas no banco, 1 navegador) e anúncios;
- plano **Plus**, a R$ 9,90 por mês ou R$ 99 por ano, sem anúncios e sem limites, com 7 dias grátis;
- **pacote avulso** de R$ 4,90 (mais 15 candidaturas assistidas e 10 sugestões de IA, válido por 60 dias);
- uma **oferta recompensada** opcional: ver uma oferta patrocinada dá mais 3 candidaturas no mês, até 3 vezes.

O pagamento é **simulado**: não há cobrança real e a interface diz isso de forma discreta.

**Por quê.** A pesquisa deu três recados juntos. A maioria prefere gratuito com limite (60%). O preço é a barreira mais citada (47%). E quem pagaria algo aceita, quase sempre, até R$ 10 (todos os 12 que pagariam aceitam ao menos essa faixa). Daí o desenho: o essencial é gratuito de verdade, o Plus custa menos de R$ 10, e o pacote atende quem prefere pagar só quando usa (13%). O anúncio paga o plano gratuito.

Como a privacidade é a segunda barreira (40%), os anúncios vêm com regras duras: nunca no formulário nem nas telas de confiança (onboarding, login, importar currículo, dados sensíveis, conta e privacidade, revisão dos campos) e nunca para quem é Plus (RN-08); nunca escolhidos com dado pessoal identificável ou sensível, só com área, cargos desejados e o texto do requisito da vaga (RN-09); sempre rotulados como "Patrocinado", com "Ocultar" e "Por que estou vendo isso?" (RN-10); no máximo um por tela (RN-11). Revisar campos, trancar sensíveis, consentimentos, exportar e apagar dados nunca são pagos nem limitados (RN-12).

**É uma decisão de escopo.** Até a versão 1, o DVS dizia "sem cobrança no MVP 1", e assinaturas apareciam no roadmap só como parte do marketplace (MVP 2). Trazer planos e anúncios para o MVP 1 muda isso. A regra RN-07 exige decisão explícita, e ela está registrada no [ADR-0020](../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md), com as alternativas que descartamos. Os detalhes de preço, limites e a estimativa de receita (que é só hipótese) estão em [monetização](../01-produto/monetizacao.md). Requisitos: RF-26 a RF-29, RN-08 a RN-13.

Um risco ficou anotado: as lojas de extensões têm regras sobre o uso de dados para publicidade. Os anúncios da extensão precisam de revisão de conformidade antes de qualquer publicação na loja.

### 4.4 Personas com rosto

As cinco personas de necessidade (PN1 a PN5) continuam, agora com o resultado de cada uma. Em cima delas, a pesquisa permitiu desenhar personas com rosto:

- **Rafael** (primária), 28 anos, analista de tecnologia, faz de 1 a 5 candidaturas por semana: "Já escrevi isso dez vezes. Só quero conferir e seguir."
- **Bia** (secundária), 20 anos, procura estágio, faz de 6 a 15 candidaturas por semana e trava nas perguntas abertas: "Não sei o que escrever e tenho medo de exagerar."
- **Beatriz** (hipótese), técnica de enfermagem, representa as outras áreas.
- **Antipersona**: quem quer se candidatar em massa, sem ler as vagas.

Detalhes e evidências em [personas](../01-produto/personas.md).

## 5. O que o Zyft nunca vai fazer

Nenhuma decisão da versão 2 mexe nas regras de confiança. Pelo contrário, a monetização foi desenhada em volta delas:

- nunca envia a candidatura: o clique de enviar é sempre da pessoa, no site (RN-01);
- a IA nunca escreve valor objetivo e nunca inventa fatos (RN-02, RN-03);
- nunca pede nem guarda senha de portal, nunca contorna CAPTCHA (RNF-01);
- dados sensíveis ficam trancados, fora da IA e fora dos anúncios (RN-04, RN-09);
- nenhuma nota, porcentagem ou promessa de contratação, nem em anúncio (RN-06);
- nada de candidatura em massa, nem como recurso pago.

## 6. Limites desta versão e próximos passos

- **A amostra é pequena e enviesada.** 15 respostas, 60% de tecnologia e 60% sem nenhuma candidatura em andamento. A próxima rodada precisa sair da rede do time e chegar a outras áreas.
- **Ainda não fizemos entrevistas.** As personas narrativas são construídas a partir das respostas, não de conversas.
- **Os preços não foram testados com dinheiro real.** O pagamento é simulado; a estimativa de receita é hipótese.
- **A Beatriz é hipótese.** Só uma resposta veio da saúde.
- **Funcionar em poucos sites** (40%) continua sendo uma barreira que o MVP 1 não resolve por inteiro: começamos por um portal e pelo portal de demonstração.

Os próximos passos estão nos [resultados da pesquisa](../05-pesquisa/resultados-v2.md#12-próximos-passos) e no [roadmap](../01-produto/roadmap.md).
