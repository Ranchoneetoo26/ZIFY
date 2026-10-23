# Guia do usuário

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Guia do usuário (plataforma web e extensão) |
| Versão | 1.3 |
| Status | Vigente para a v2 do MVP 1. Cada passo foi conferido contra o código da plataforma (`web/js/views/`) e do painel (`extension/sidepanel.js`, versão 0.4.0) em 21/10/2026. |
| Dono | Product Owner, com o dev frontend |
| Documentos relacionados | [Runbook local](runbook-local.md), [Fluxos](../01-produto/fluxos.md), [Monetização](../01-produto/monetizacao.md), [Critérios de aceite](../04-qualidade/criterios-de-aceite-mvp1.md), [Design system](../03-arquitetura/design-system.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 21/10/2026 | Primeira versão: passo a passo completo da v2 (onboarding em passos, área, campos personalizados, planos e anúncios), usuários de demonstração e roteiros de 10 minutos para a banca. |
| 1.1 | 21/10/2026 | Conferido contra o código: nomes reais de botões e abas, telas de abertura do onboarding, retomada automática do onboarding no login (Beatriz), revisão da importação item a item, dados sensíveis com os tipos da API, painel com abas "A vaga e você" e "Formulário", recompensado pelo botão **Ganhar**, ocultar anúncios, limites também em respostas e navegadores, limitações conhecidas (seção 21) e roteiros ajustados aos dados de demonstração. |
| 1.2 | 21/10/2026 | **Desconectar extensão** no painel libera a vaga de navegador; data de nascimento também aparece trancada no painel; comprar pacote só pela plataforma; nota de que "ocultar este tema" existe só na plataforma. |
| 1.3 | 21/10/2026 | Correções da gravação do tour: "Tipo" de curso começa em **Curso**; mensagens de erro dizem o nome do campo; na importação, item incompleto (ex.: formação sem situação) pede a escolha antes de **Aprovar**, e certificações mantêm o nome original; **Salvar no Perfil Mestre** só libera depois de revisar tudo; teste grátis do Plus só na primeira assinatura; rascunho de IA sempre mostra algo no cartão; **Não tenho interesse no tema** também no painel; exemplo do roteiro da Beatriz trocado (ela já tem "Disponível para plantão noturno"). Conferidos de novo: aceite dos termos no cadastro, "Como você quer começar?", código `123-456`, "Aparece em parte" com "Confirme você", conclusão que abre sozinha e **Excluir definitivamente**. |

## 1. Para quem é este guia

Para quem vai usar o Zyft pela primeira vez: pessoas da banca, testadores e o próprio time na hora de gravar demonstrações. Ele descreve **o que você vê e o que acontece** em cada tela, com os nomes de botões que aparecem na interface. A parte técnica (subir a API, Docker, testes) está no [runbook local](runbook-local.md).

O Zyft tem duas partes:

- **Plataforma web**: onde você prepara tudo. Monta o seu Perfil Mestre, guarda respostas, conecta a extensão, acompanha candidaturas e cuida da sua conta.
- **Extensão** (Chrome ou Edge): um painel lateral que, no site de vagas, lê o formulário que você abriu e propõe o preenchimento. Ela preenche só o que você aprovou e **nunca envia nada**. O botão de enviar é sempre seu, no site.

## 2. Antes de começar

1. Suba a API local: `dotnet run --project src/Zyft.Api` (detalhes e alternativas no [runbook](runbook-local.md#2-rodar-com-sqlite-desenvolvimento)).
2. Abra `http://localhost:5080/` no Chrome ou no Edge.
3. Para usar a extensão, carregue a pasta `extension/` em modo desenvolvedor (seção 9).
4. A IA é opcional. Sem o Ollama rodando, o preenchimento por regras funciona; sugestões de texto, análise de vaga e importação ficam mais limitadas e a tela avisa.

> **Ambiente de demonstração.** Planos, pacotes e pagamentos são simulados: nenhuma cobrança real acontece e nenhum dado de cartão é pedido. Os anúncios vêm de um catálogo fixo de anunciantes de demonstração.

## 3. Usuários de demonstração

Existem só quando a API roda em Development (o padrão do `dotnet run`). Todos os dados são fictícios.

| Pessoa | E-mail | Senha | O que mostra |
|---|---|---|---|
| **Rafael Moreira** (persona principal) | `demo@zyft.local` | `Zyft@2026` | Área Tecnologia, perfil completo: experiências, competências, idiomas, formação, respostas aprovadas, candidaturas e registro de ações. Plano gratuito com 7 de 10 candidaturas assistidas usadas no mês. Ideal para mostrar o preenchimento "de relance", o limite e a conclusão. |
| **Beatriz Lima** (outra área) | `beatriz@demo.zyft` | `Zyft@2026` | Área Saúde (técnica de enfermagem), registro COREN-MG, dois campos personalizados ("Disponível para plantão noturno" e "Experiência em UTI"). Onboarding **parado** na pergunta de competências, com links, pretensão salarial e cursos deixados para depois. Ideal para mostrar a área dinâmica, os campos personalizados e o alerta de pendentes. |

Nos dois, os consentimentos *IA para textos* e *Análise de vaga* já estão ligados e *Dados sensíveis* está desligado.

Quer recomeçar do zero? Pare a API e apague `src/Zyft.Api/zyft-dev.db`. Na próxima subida, os dois usuários são recriados.

## 4. Criar conta e entrar

1. Em `#/entrar`, use a aba **Criar conta**.
2. Informe nome, e-mail e uma senha com pelo menos 8 caracteres, com letra e número.
3. Marque "Li e aceito os termos de uso e a política de privacidade do Zyft." e clique em **Criar conta**. Sem esse aceite a conta não é criada: a tela avisa "Para criar a conta, marque que você leu e aceita os termos."
4. Você entra direto e vai para as boas-vindas (seção 5).

Para entrar depois, use a aba **Entrar**. Se você ainda não concluiu o onboarding, a primeira tela da sessão leva de volta a ele, na pergunta em que você parou; dali você pode sair com **Sair e continuar depois**. Depois de 10 tentativas por minuto do mesmo endereço, o login pede para esperar. O Zyft **nunca** pede a senha dos sites de vagas, em nenhuma tela.

Não há anúncio nas telas de entrar e criar conta.

## 5. Onboarding em passos (o "passaporte")

`#/boas-vindas` começa com duas telas:

1. **Boas-vindas**: "Seu passaporte profissional começa aqui." Clique em **Começar**.
2. **Como você quer começar?**: **Tenho um currículo** leva à importação (seção 7); **Responder aqui** segue com as perguntas.

Depois vêm as perguntas, **uma por tela**, em 6 capítulos. No topo aparecem o capítulo e "Pergunta N de M". Ao lado, o cartão do passaporte mostra, por capítulo, quantas você respondeu, o selo "Carimbado" quando o capítulo fecha e quantas ficaram "para depois".

| Capítulo | Perguntas |
|---|---|
| 1. Quem é você | Nome, data de nascimento, contato (telefone e e-mail), onde mora (cidade, UF, país e CEP) |
| 2. Sua área | Área profissional, vagas que busca, setores, registro profissional (só nas áreas que pedem), links |
| 3. O que você busca | Momento da carreira, modelo de trabalho, onde aceita trabalhar, tipo de contrato, quando pode começar, pretensão salarial (com o tipo: CLT bruto, PJ, por hora ou por plantão), deslocamento (viagens, mudança de cidade e CNH) |
| 4. Sua bagagem | Última experiência, dia a dia e resultados, ferramentas, formação, cursos, competências, idiomas |
| 5. Sua apresentação | Título profissional e resumo |
| 6. Combinados | O que o Zyft pode fazer por você (consentimentos) |

São 25 perguntas nas áreas que pedem registro profissional (Saúde, Administrativo e financeiro, Logística e operações, Jurídico) e 24 nas outras.

Botões de cada pergunta:

- **Carimbar e continuar**: grava a resposta no seu perfil na hora e segue. Na última pergunta, **Carimbar e concluir**.
- **Preencher mais tarde**: pula sem travar nada. Nenhuma pergunta é obrigatória.
- **Voltar**: volta uma pergunta.
- **Sair e continuar depois** (no topo): sai para o Perfil Mestre. O progresso fica salvo e você volta de onde parou.

O que vale saber:

- **A área muda as perguntas seguintes.** Escolha a sua (Saúde, Tecnologia, Vendas e comercial, Administrativo e financeiro, Educação, Logística e operações, Jurídico ou Outra área). As sugestões de cargos, setores, ferramentas e competências mudam. Em Saúde, por exemplo, aparece "Registro no conselho" (ex.: COREN-MG 123456) e o link pedido é o Currículo Lattes; em Tecnologia, o link é o GitHub. Em "Outra área", você escreve o nome da sua.
- **Em qualquer lista você pode escrever o seu valor** quando a sugestão não serve ("Outro cargo", "Outra ferramenta", "Outra competência", "Outro idioma") e clicar em **Adicionar**.
- **Experiência, formação e cursos**: a pergunta cadastra um item. Para mais de um, use **Adicionar outra experiência** (ou formação, ou curso) antes de continuar, ou cadastre depois no Perfil Mestre.
- **Combinados**: os três consentimentos começam desligados numa conta nova. Leia a finalidade de cada um e ligue o que quiser:
  - *Sugestões de texto com IA*: rascunhos para perguntas abertas, só com o que está carimbado no seu perfil;
  - *Análise de vagas*: mostrar onde cada requisito da vaga aparece no seu perfil;
  - *Dados sensíveis*: guardar dados como gênero ou deficiência trancados, para você destrancar um a um. Eles nunca vão para a IA.

  Você muda isso quando quiser em **Conta e privacidade**.
- **No fim**, a tela "Pronto, {seu nome}. Agora é com a vaga." lista as perguntas que ficaram para depois (cada uma com link) e oferece **Conectar a extensão** e **Ver meu Perfil Mestre**.

Algumas respostas são simplificadas ao gravar no perfil (seção 21): "Às vezes" para viagens vira sim, "Talvez" para mudança de cidade e "Tanto faz" para modelo de trabalho ficam em branco, e as ferramentas vão para a sua experiência mais recente.

Não há anúncio em nenhuma tela do onboarding.

## 6. Perfil Mestre

`#/perfil` é o seu "passaporte": um lugar só com as suas informações, que a extensão usa nos formulários. No topo há o atalho **Atualizar com um currículo**.

- **Alerta de pendentes.** Se faltam informações, um aviso diz quantas ficaram para depois (as perguntas que você pulou e os dados que costumam faltar nos formulários) e explica que, enquanto faltarem, a extensão vai marcar esses campos como "Precisa de você". **Continuar de onde parei** abre o onboarding na primeira pendente.
- **Responda agora, em 10 segundos.** Até três cartões rápidos com o que mais aparece nos formulários e ainda falta no seu perfil: pretensão salarial, disponibilidade, CNH, modelo de trabalho, viagens ou mudança de cidade. Escolha a resposta (ou digite o valor) e clique em **Carimbar**.
- **Sobre você**: dados básicos, área, registro profissional, título, o que você busca, pretensão salarial com o tipo, links. Clique em **Editar** e depois em **Salvar e carimbar**.
- **Experiências**: linha do tempo com cargo, empresa, período, resultado, atividades e ferramentas. Experiência atual não tem data de fim.
- **Competências**: as suas e, abaixo, até seis sugestões da sua área para tocar e adicionar; ou escreva em "Cadastrar do seu jeito".
- **Idiomas**, **Formação e cursos**: adicionar, editar e excluir. Em **Formação e cursos**, os botões **+ Formação** e **+ Curso** abrem o formulário; o campo **Tipo** do curso já vem em **Curso** (troque para **Certificação** quando for o caso).
- **Mensagens de erro** dizem qual campo conferir, com o nome que aparece na tela (ex.: "Formação 1 · Situação: Informe a situação."), e o campo fica destacado.
- **Informações da sua área** (campos personalizados): crie informações que a sua profissão pede e o Zyft não conhece. Preencha "Nome da informação" e "Sua resposta" e clique em **Adicionar informação**. Exemplos: "Curso de ACLS: Concluído em 2025", "Experiência em sala de vacina: 2 anos" (a Beatriz de demonstração já tem "Disponível para plantão noturno" e "Experiência em UTI"). A extensão usa quando um formulário perguntar algo parecido. Nome até 80 caracteres, valor até 500. Rótulos que pareçam dado sensível ou documento (raça, religião, saúde, deficiência, gênero, CPF, RG, senha, entre outros) são recusados, com a orientação de usar **Dados sensíveis**.
- **Dados sensíveis**: com o consentimento *Dados sensíveis* desligado, o bloco só explica e oferece **Ligar em Conta e privacidade**. Ligado, você pode guardar, um por tipo: gênero, raça ou etnia, deficiência, religião, orientação sexual, saúde, nome social ou outro dado sensível (**Adicionar dado sensível** e **Guardar trancado**). Eles ficam cifrados, mascarados na tela, nunca vão para a IA e só são preenchidos se você destrancar e aprovar aquele campo no painel, um a um. CPF e RG não são guardados aqui. Desligar o consentimento apaga os dados sensíveis guardados.
- **Passaporte** (coluna lateral): "N de 22 carimbos", com a porcentagem.

No plano gratuito pode aparecer **um** card patrocinado na coluna lateral, com "Por que estou vendo isso?" e "Não tenho interesse" (seção 18).

## 7. Importar currículo

`#/importar` monta ou atualiza o perfil a partir de um currículo.

1. Escolha um PDF com texto ou um DOCX de até 10 MB e clique em **Enviar e ler o currículo**. PDF escaneado (imagem) não é lido; a tela explica e sugere alternativas. A leitura pode levar até um minuto.
2. Na tela **Revise o que lemos**, cada item aparece com a origem ("Importado de …"):
   - item novo: **Aprovar**, **Editar** ou **Descartar**;
   - dado básico diferente do seu perfil: os dois valores lado a lado, com **Usar o do currículo**, **Manter o do perfil** ou **Editar antes de aceitar**;
   - **item incompleto**: quando o currículo não diz algo obrigatório (por exemplo, uma formação sem a situação: cursando, concluído, trancado ou incompleto), o campo que falta aparece destacado dentro do item, com a explicação "O currículo não diz se você ainda está cursando, já concluiu ou trancou…". **Aprovar** só libera depois que você escolhe. O Zyft não adivinha esse valor.
3. **Aceitar tudo que não conflita com o perfil** aprova de uma vez o que não bate de frente com o que você já tem. Itens incompletos ficam de fora, esperando a sua escolha.
4. O botão **Salvar no Perfil Mestre** só libera depois que **todos** os itens foram revisados (aprovados, corrigidos ou descartados); antes disso ele mostra "Faltam N para revisar". Só entra no perfil o que você aprovou ou corrigiu.
5. **Descartar importação** apaga a proposta sem mudar nada no perfil.

Nomes de cursos e certificações ficam como estão no currículo, sem tradução ("AWS Certified Cloud Practitioner" continua em inglês), e o que tem Certified, Certification ou Certificação no nome entra como **Certificação**. Letras ligadas de PDF (como "ﬁ") são desfeitas na leitura.

No plano gratuito são 2 importações por mês, com o medidor na própria tela. Não há anúncio nesta tela.

## 8. Banco de respostas

`#/respostas` guarda textos aprovados para perguntas que se repetem ("Por que você quer trabalhar aqui?").

- Em **Nova resposta**, escreva a pergunta e a sua resposta e clique em **Aprovar e guardar**.
- Quando um formulário fizer uma pergunta parecida, a extensão propõe a sua resposta aprovada, sempre para revisar antes de usar.
- Você também pode guardar uma resposta direto do painel da extensão.
- Edite ou exclua quando quiser. No plano gratuito são até **15 respostas** (a tela mostra "N de 15"); passar disso abre as opções do limite (seção 17). No Plus, sem limite.

## 9. Conectar a extensão

1. Instale em modo desenvolvedor: abra `chrome://extensions` (ou `edge://extensions`), ligue o **Modo do desenvolvedor**, clique em **Carregar sem compactação** e escolha a pasta `extension/` do projeto. Fixe o ícone do Zyft.
2. Na plataforma, abra **Extensão** e clique em **Gerar código de pareamento**. Aparece um código de 6 dígitos (mostrado como `123-456`), com contagem regressiva, **Copiar** e a opção de gerar outro.
3. Clique no ícone do Zyft para abrir o painel lateral ("Conecte este navegador à sua conta"). Digite os 6 números em **Código de 6 dígitos**, confira o **Nome deste navegador** e clique em **Conectar**. O código vale por **10 minutos** e **uma única vez**.
4. Pronto: o painel mostra "Conectada". Na plataforma, clique em "atualize a lista de navegadores" para ver o navegador em **Navegadores conectados**, com a data, o último uso e **Desconectar**.

No plano gratuito você conecta **1 navegador**; no Plus, até 3. Se o limite estiver cheio, o painel avisa e oferece **Ver navegadores conectados na plataforma** e **Conhecer o Plus**. Desconectar derruba o acesso da extensão na hora.

Para desconectar **pelo próprio painel**, abra **Ajustes** e clique em **Desconectar extensão** (o painel pede confirmação). O navegador sai da lista de **Navegadores conectados** e a vaga fica livre: no plano gratuito, você já pode conectar outro navegador. Se a API estiver fora do ar nesse momento, o painel desconecta mesmo assim; aí o navegador continua na lista da plataforma e você pode removê-lo lá em **Desconectar**.

Se a API não estiver em `http://localhost:5080`, abra **Ajustes** no painel e troque o **Endereço da API**.

## 10. Sites autorizados

A extensão só lê formulários em endereços autorizados. Na tela **Extensão**, o bloco **Sites autorizados** mostra cada um, com **Remover**.

- Em um site novo, o painel mostra "Site ainda não autorizado" e oferece **Usar neste site mesmo assim**. O navegador pede a sua permissão. Só então a extensão passa a ler aquela página, e só quando você pedir.
- Você também pode autorizar um site pela plataforma: informe o endereço (`https://vagas.exemplo.com`) e clique em **Autorizar site**. Só a origem é guardada, sem caminho.
- O portal de demonstração em `localhost` já funciona sem autorização ("Compatível com o portal de demonstração").

## 11. Usar no portal de demonstração

O portal de demonstração em `http://localhost:5080/demo/` imita um site de vagas para testar sem usar portais reais. A tela **Extensão** tem o link **Testar no portal de demonstração**.

| Página | Para quê |
|---|---|
| `vaga.html` | Analisar uma vaga (seção 12) |
| `curriculo.html` | Formulário de currículo vazio: ler e preencher |
| `curriculo.html?preenchido=1` | Currículo já preenchido no site: ver as divergências com o perfil |
| `candidatura.html` | Candidatura com pergunta aberta, dado sensível, termos, anexo e CAPTCHA |

Não use portais reais com contas falsas. Para demonstrar num portal real, a pessoa faz login na própria conta e para antes de salvar ou enviar.

## 12. Analisar a vaga ("A vaga e você")

O painel tem duas abas: **A vaga e você** e **Formulário**. A análise requer o consentimento *Análise de vagas*.

1. Abra `vaga.html` e, na aba **A vaga e você**, clique em **Analisar esta vaga**.
2. O painel mostra os dados visíveis da vaga (cargo, empresa, modalidade, contrato, local e salário quando houver) e o bloco **O que a vaga pede**, com "N de M aparecem no seu perfil".
3. Cada requisito aparece como **Aparece no seu perfil**, **Aparece em parte** ou **Não encontramos no seu perfil**. Itens que o perfil não deixa claros (CNH, pretensão, modalidade) aparecem "em parte", com "Confirme você".
4. Cada requisito encontrado mostra a **linha do seu perfil** que serve de evidência ("No perfil: CNH (categoria): B").
5. **Não há nota, porcentagem nem promessa de aprovação.** O painel lembra que requisito que não aparece pode só estar faltando no perfil. **Analisar de novo** refaz a leitura.

No plano gratuito são 10 análises por mês. Na aba **A vaga e você** pode aparecer **um** anúncio: se algum requisito não aparece no seu perfil, o Zyft tenta um card ligado a ele ("Sobre: Excel intermediário"); se não houver anúncio para esse tema, aparece um banner no lugar.

No rodapé da aba ficam o botão **Ler o formulário desta página** e o contador "Candidaturas assistidas em {mês}".

## 13. Ler e preencher o formulário

1. Abra `curriculo.html` ou `candidatura.html` e clique em **Ler o formulário desta página** (na aba **A vaga e você** ou na aba **Formulário**). O painel diz "Lendo só este formulário" e passa para a aba **Formulário (N)**.
2. O Zyft compara cada campo com o seu perfil e mostra um resumo: quantos estão prontos, quantos precisam de você, quantos são ação sua e quantas são sugestões da IA.
3. Cada campo aparece com um de quatro estados, sempre com cor, ícone e palavra:

| Estado | O que significa | O que você faz |
|---|---|---|
| **Pronto** | O valor veio do seu perfil aprovado (ou de uma resposta aprovada, ou das informações da sua área). | Confira e aprove. |
| **Precisa de você** | Falta a informação no perfil ("Falta no perfil"), há opções a escolher ("Escolha") ou o site já tem um valor diferente ("Já preenchido no site"). | **Responder**, escolher ou **Pular**. Na divergência, o padrão é **Manter o do site**; para trocar, marque **Usar o do perfil**. |
| **Ação sua** | O Zyft não pode agir: senha, CAPTCHA, anexo, aceite de termos, declarações, e dado sensível sem consentimento. | Resolva direto no site. **Mostrar no site** leva até o campo. |
| **Sugestão da IA – revise** | Texto para pergunta aberta, escrito só com fatos do seu perfil. | Leia, edite e aprove. |

4. Os **Prontos** aparecem em linhas compactas no topo ("Prontos · confira de relance"), com o botão **Aprovar os N**, que aprova só esses. Dados sensíveis nunca entram nesse lote.
5. **Dados sensíveis** (e campos de CPF, RG, endereço completo e data de nascimento) aparecem trancados, um a um. Clique em **Destrancar para conferir** e depois em **Aprovar**. Nunca em lote.
6. **Campo que falta no perfil**: clique em **Responder**, escreva e **Aprovar**. Marque "Guardar esta resposta no Banco de respostas" para não digitar de novo.
7. **Perguntas abertas**: clique em **Escrever** para usar as suas palavras ou em **Pedir um rascunho à IA**. O pedido sempre mostra algo no próprio cartão: o rascunho (com "De onde veio: a IA usou só isto do seu perfil"), uma resposta que você já aprovou para pergunta parecida ("Resposta já aprovada – confira") ou, quando o perfil não tem fatos suficientes, o aviso "A IA não conseguiu escrever um rascunho com os fatos do seu perfil; escreva do seu jeito." com a caixa de texto já aberta. O botão **Aprovar texto** só libera depois que você marca "Li e tudo o que está escrito é verdade sobre mim." Também há **Outro começo** e **Escrever do zero**. Depois de aprovar, você pode **Guardar no Banco de respostas**. No gratuito são 5 sugestões de IA por mês.
8. Clique em **Preencher N campos aprovados**. Só os aprovados são preenchidos. O **Resumo do preenchimento** mostra quantos foram preenchidos com a sua aprovação, quantos você pulou, quantos ficam para fazer no site e quais falharam.
9. Confira no site e envie quando estiver pronto. **O Zyft nunca envia por você.**

Formulário em várias etapas? Depois de avançar no site, clique em **Ler novamente (nova etapa)**. A mesma página não conta de novo no limite do mês.

**Nunca há anúncio na aba Formulário.**

## 14. Concluir e registrar a candidatura

1. Depois de enviar no site, clique em **Concluí a candidatura** no painel. Se você enviou pelo botão do próprio site, o painel abre esta tela sozinho.
2. A tela pergunta "Você enviou o formulário?" e mostra o resumo do preenchimento. Confira **Vaga** (obrigatório) e **Empresa** (opcional).
3. Se você realmente enviou, clique em **Confirmo que enviei: registrar**. Nada é registrado sem esse clique. **Voltar à revisão** desiste.
4. Aparece "Candidatura registrada", com **Abrir Candidaturas**. Neste momento, e só depois de concluir, pode aparecer um card patrocinado de "Próximo passo". **Voltar ao painel** recomeça.

O registro guarda a vaga, a empresa, o portal, o endereço e **quantos** campos foram preenchidos, nunca os valores.

## 15. Acompanhar candidaturas

`#/candidaturas` mostra no topo quantas candidaturas você registrou no mês, quantos campos o Zyft preencheu com a sua aprovação e uma estimativa do tempo de digitação poupado. Abaixo, a lista com data, vaga, empresa, portal, campos preenchidos e status, com filtro por status e busca por vaga ou empresa.

- Abra uma para ver o detalhe: endereço da vaga, quando você concluiu e quantos campos o Zyft preencheu. Por privacidade, o registro guarda só a quantidade, não quais campos nem os valores.
- Em **Acompanhamento**, escolha o status (Enviada por mim, Em análise pela empresa, Entrevista, Teste, Proposta recebida, Contratado(a), Não seguiu, Desisti, Sem resposta, Arquivada), escreva anotações e clique em **Salvar acompanhamento**.
- **Excluir registro** apaga só o registro no Zyft; a candidatura continua no site da empresa.
- Não há nota nem taxa de sucesso.

No gratuito pode haver uma faixa patrocinada entre o resumo e a lista, e um banner no detalhe.

## 16. Registro de ações

`#/registro` mostra, por dia e em linguagem simples, tudo o que o Zyft fez por você: "Formulário lido pela extensão · vagas.exemplo.com · 23 campos". Também aparecem conta, consentimentos, perfil, currículo, pareamentos, candidaturas, mudanças de plano, pacotes, ofertas patrocinadas, anúncios ocultados, campos personalizados e o fim do onboarding. **Nunca** aparecem os valores preenchidos. Dá para filtrar por Conta, Perfil, Currículo, Extensão, Candidaturas e Planos e anúncios.

## 17. Planos

`#/planos` mostra o que você tem e as opções. Lembre: aqui é tudo simulado ("Ambiente de demonstração: nenhuma cobrança real", logo abaixo do título).

| | Gratuito | Plus | Pacote avulso |
|---|---|---|---|
| Preço | R$ 0, para sempre | R$ 9,90/mês ou R$ 99/ano (2 meses grátis); 7 dias grátis na primeira assinatura | R$ 4,90, pagamento único |
| Candidaturas assistidas por mês | 10 (+3 por oferta patrocinada vista, até +9) | Sem limite | +15 |
| Sugestões de texto com IA por mês | 5 | Sem limite | +10 |
| Análises de vaga por mês | 10 | Sem limite | — |
| Importações de currículo por mês | 2 | Sem limite | — |
| Banco de respostas | Até 15 | Sem limite | — |
| Navegadores conectados | 1 | Até 3 | — |
| Anúncios | Até 1 por tela | Nenhum | — |
| Validade | — | Enquanto durar a assinatura | 60 dias |

O pacote cobre só candidaturas assistidas e sugestões de IA. Análises, importações, respostas e navegadores não são afetados por ele.

**Sempre de graça, em qualquer plano:** revisar cada campo antes de preencher, dados sensíveis trancados, nunca pedir a senha dos portais, consentimentos, baixar e apagar seus dados. Privacidade não é recurso pago.

- **Testar o Plus**: escolha **Mensal** ou **Anual** no topo e clique em **Testar 7 dias grátis**. Os anúncios somem e os limites viram "sem limite". O teste grátis vale **uma vez por conta**: quem já testou e cancelou vê **Assinar o Plus**, sem o selo "7 dias grátis", e a assinatura começa no mesmo dia.
- **Cancelar**: no cartão do Plus, **Cancelar assinatura** e confirme. Você volta ao gratuito e nada do seu perfil se perde.
- **Comprar pacote**: **Comprar pacote** e confirme. A compra é feita só aqui na plataforma; no painel da extensão, a opção de pacote abre esta tela. O saldo aparece em "Seus pacotes ativos" e vale por 60 dias.
- **Seu uso neste mês**: medidores de cada recurso e "Recomeça em 1º de {mês}".
- **Ganhar +3** (recompensado): no painel da extensão, abaixo do contador de candidaturas, clique em **Ganhar**. Aparece uma oferta patrocinada; clique em **Vi a oferta: ganhar +3** (não é preciso clicar no anúncio). Até 3 vezes no mês. É sempre opcional.

**O que acontece quando o limite acaba?** O Zyft para de propor o que aquele limite cobre (preencher páginas novas, sugerir texto, analisar vaga, importar, guardar mais respostas ou conectar outro navegador) e mostra as opções: ver uma oferta patrocinada e ganhar +3 (só para candidaturas assistidas, enquanto houver oferta no mês), comprar um pacote (só para candidaturas e IA) ou conhecer o Plus. Na plataforma, o diálogo "Você chegou ao limite deste mês" tem **Agora não**, que fecha sem perder o que você digitou. No painel, o aviso lembra que, sem IA, você ainda pode escrever com as suas palavras. Nada é apagado, e você continua podendo editar o perfil, exportar e excluir os seus dados. O uso volta a zero no primeiro dia de cada mês.

## 18. Anúncios: onde aparecem e como controlar

Só no plano gratuito, e sempre com o selo **Patrocinado**, separado da interface.

| Onde | Formato |
|---|---|
| Perfil Mestre (coluna lateral) | Card nativo |
| Candidaturas (entre o resumo e a lista) | Faixa |
| Banco de respostas, Extensão, detalhe da candidatura e Registro de ações | Banner horizontal |
| Painel da extensão, aba "A vaga e você" | Card ligado a um requisito que não aparece no seu perfil; sem ele, banner |
| Painel da extensão, ao escolher **Ganhar** | Banner da oferta recompensada |
| Painel da extensão, depois de registrar a candidatura | Card de próximo passo |

**Nunca há anúncio** no onboarding, em entrar e criar conta, em importar currículo, em dados sensíveis, em conta e privacidade, em planos, na aba **Formulário** do painel, no painel desconectado nem para quem é Plus. No máximo um por tela.

- **Por que estou vendo isso?** explica o motivo. O anúncio é escolhido só pela sua área, pelos cargos que você busca e pelo texto do requisito da vaga. Nunca pelo seu nome, contato ou dados sensíveis, e nada disso vai para o anunciante.
- **Ocultar**, **Não tenho interesse** ou o "x" do banner escondem aquele anúncio por 30 dias. Para esconder o **tema** para sempre: na plataforma, dentro de "Por que estou vendo isso?", **Não mostrar anúncios sobre {tema}**; no painel da extensão, **Não tenho interesse no tema**, ao lado de **Ocultar** no card do requisito e no de próximo passo (no banner, dentro de "Por que estou vendo isso?", quando o anúncio tem tema).
- Os anunciantes deste ambiente são de demonstração ("Anunciante de demonstração").

## 19. Privacidade: exportar e excluir

Em **Conta e privacidade** (`#/conta`):

- **Consentimentos**: ligue ou desligue cada finalidade. A finalidade "Conta e Perfil Mestre" é fixa (é o próprio serviço). O bloqueio vale na hora, na API, não só na tela.
- **Baixar meus dados (JSON)**: um arquivo com perfil, respostas, candidaturas, consentimentos e registro de ações.
- **Excluir minha conta**: apaga o Perfil Mestre, as respostas, as candidaturas e os dados sensíveis. As extensões conectadas perdem o acesso na hora. Digite `EXCLUIR`, clique em **Excluir conta** e confirme em **Excluir definitivamente**. Não dá para desfazer.

## 20. Problemas comuns

| O que acontece | Por quê | O que fazer |
|---|---|---|
| Ao entrar, caio no onboarding | O onboarding ainda não foi concluído | Responda, pule ou use **Sair e continuar depois** |
| O painel pede para conectar de novo | O navegador foi desconectado na plataforma ou a sessão expirou (7 dias sem uso) | Gere outro código em **Extensão** |
| "Código inválido ou expirado" ao conectar | O código passou de 10 minutos ou já foi usado | Gere um novo |
| Não consigo conectar um segundo navegador | O gratuito permite 1 | Use **Desconectar extensão** (em **Ajustes**) no navegador antigo, ou **Desconectar** na plataforma em **Extensão**, ou teste o Plus |
| O painel não fala com a API | A API está em outro endereço | Ajuste o **Endereço da API** em **Ajustes** |
| "A IA está indisponível" | O Ollama não está rodando | Os campos simples continuam funcionando; o resto pode ser preenchido à mão |
| A primeira sugestão demora uns 40 segundos | A IA está carregando o modelo | Espere; as seguintes levam segundos |
| Analisar vaga ou pedir rascunho não funcionam | O consentimento está desligado | Ligue em **Conta e privacidade** |
| O painel diz que o site não foi autorizado | A extensão só lê sites autorizados | Use **Usar neste site mesmo assim** e permita no navegador |
| O painel mostra "Você usou as 10 candidaturas assistidas deste mês" | Limite do gratuito | Escolha uma das opções (oferta, pacote, Plus) ou espere o próximo mês |
| "O Banco de respostas do plano gratuito está cheio" | Limite de 15 respostas | A resposta vale para o formulário atual; apague uma antiga ou veja os planos |
| Não consigo salvar um campo em "Informações da sua área" | O nome parece dado sensível ou documento | Use **Dados sensíveis** ou não informe |
| Não consigo guardar dado sensível | O consentimento *Dados sensíveis* está desligado | Ligue em **Conta e privacidade** |
| Um anúncio não aparece | Plus, anúncio oculto, tela sem anúncio, sem anúncio para o tema ou serviço lento | Esperado: o anúncio nunca atrapalha a tela |

## 21. Limitações conhecidas

- **Respostas do onboarding simplificadas.** "Topa viajar a trabalho? Às vezes" é gravado como sim. "Mudaria de cidade? Talvez" e "Como você prefere trabalhar? Tanto faz" ficam em branco no perfil. Por isso esses itens podem reaparecer em "Responda agora".
- **Ferramentas do onboarding** são gravadas na sua experiência mais recente. Sem experiência cadastrada, viram competências.
- **Um item por pergunta.** As perguntas de experiência, formação e curso editam o item mais recente; os demais se cadastram com "Adicionar outra…" ou no Perfil Mestre.
- **Detalhe da candidatura** mostra só quantos campos foram preenchidos, não quais.
- **Dados sensíveis** aceitam só os tipos listados na seção 6. CPF e RG não são guardados pelo Zyft como dado sensível.
- **Recompensado na web** só aparece no diálogo de limite; o caminho normal é o botão **Ganhar** no painel.

## 22. Roteiros de demonstração (10 minutos)

### 22.1 Rafael: "Já escrevi isso dez vezes. Só quero conferir e seguir."

| Min. | Passo | O que mostrar |
|---|---|---|
| 0–1 | Entrar com `demo@zyft.local` / `Zyft@2026` | Sem anúncio no login; cai direto no Perfil Mestre |
| 1–2 | Perfil Mestre | Passaporte quase completo, competências de Tecnologia, dados sensíveis pedindo consentimento, um único card patrocinado na lateral e "Por que estou vendo isso?" |
| 2–3 | Extensão: gerar código e conectar | Código de 6 dígitos, 10 minutos, "1 de 1 no seu plano" |
| 3–5 | `vaga.html` → **Analisar esta vaga** | "N de M aparecem no seu perfil", evidência com a linha do perfil, sem nota nem porcentagem; card "Sobre: {requisito}"; contador "7 de 10" e **Ganhar** |
| 5–8 | `candidatura.html` → **Ler o formulário desta página** | Prontos compactos e **Aprovar os N**; senha, CAPTCHA, anexo e termos como Ação sua; pergunta aberta com **Pedir um rascunho à IA** e "Li e tudo o que está escrito é verdade sobre mim."; **nenhum anúncio** na aba Formulário |
| 8–9 | **Preencher**, enviar no site, registrar | O Zyft não envia; registro só com **Confirmo que enviei: registrar**; card de próximo passo depois de concluir |
| 9–10 | Candidaturas, Registro de ações e Planos | Detalhe só com a contagem de campos; registro sem valores; medidores; aviso de pagamento simulado; "Sempre de graça" |

### 22.2 Beatriz: o Zyft fora da tecnologia

| Min. | Passo | O que mostrar |
|---|---|---|
| 0–2 | Entrar com `beatriz@demo.zyft` / `Zyft@2026` | O login leva direto ao onboarding, na pergunta 22 ("O que você sabe fazer bem?"), com sugestões de Saúde; o passaporte mostra "3 para depois". Uma pergunta por tela e **Preencher mais tarde** |
| 2–3 | **Sair e continuar depois** → Perfil Mestre | Alerta de pendentes (links, pretensão salarial, cursos…), **Continuar de onde parei**, "Responda agora" com **Carimbar** (ex.: pretensão salarial); área Saúde com COREN |
| 3–5 | Informações da sua área | Os dois campos que ela já tem ("Disponível para plantão noturno" e "Experiência em UTI", não crie de novo); criar "Curso de ACLS: Concluído em 2025"; tentar "Religião" e ver a recusa com **Ir para Dados sensíveis** |
| 5–8 | `curriculo.html` → **Ler o formulário desta página** | Campos que ela pulou aparecem como "Precisa de você"; campo personalizado reconhecido como Pronto quando o formulário pergunta algo parecido |
| 8–9 | Limite do gratuito | Explicar as opções do limite e o **Ganhar** opcional, sem forçar o 402 ao vivo |
| 9–10 | Conta e privacidade | Consentimentos, **Baixar meus dados (JSON)**, excluir só depois de digitar `EXCLUIR` |
