# Critérios de aceite do MVP 1

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Critérios de aceite (CA) do MVP 1 |
| Versão | 2.0 |
| Status | Aprovado para refinamento |
| Dono | Product Owner (com QA) |
| Documentos relacionados | [DRP](../01-produto/drp.md), [Fluxos](../01-produto/fluxos.md), [Estratégia de testes](estrategia-de-testes.md), [Monetização](../01-produto/monetizacao.md), ADRs [0018](../03-arquitetura/adr/0018-onboarding-em-passos-com-perguntas-adiaveis.md), [0019](../03-arquitetura/adr/0019-area-profissional-dinamica-e-campos-personalizados.md) e [0020](../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | CA-01 a CA-22, um por requisito funcional do DRP (mesma numeração). |
| 2.0 | 21/10/2026 | CA-09 ajustado ao pareamento implementado (código de 6 dígitos gerado na plataforma; PKCE ficou para depois, [ADR-0017](../03-arquitetura/adr/0017-backend-em-csharp-dotnet-clean-architecture.md)). CA-19 ajustado ao registro de conclusão implementado. Novos CA-23 a CA-29 para onboarding em passos, área e catálogo, campos personalizados, planos e limites, pacote avulso, anúncio recompensado e anúncios (RF-23 a RF-29). |

## Convenções

- **CA-NN** corresponde ao **RF-NN** do [DRP](../01-produto/drp.md). Cada cenário tem um sufixo (CA-12.3).
- Formato Dado / Quando / Então. Cenários marcados **(negativo)** verificam o que **não** pode acontecer e viram testes negativos de confiança quando aplicável.
- "Pessoa" é a pessoa candidata autenticada, salvo indicação contrária.

## CA-01 — Conta e autenticação (RF-01)

| # | Dado | Quando | Então |
|---|---|---|---|
| 01.1 | uma pessoa sem conta | ela se cadastra com e-mail e confirma o link de verificação | a conta fica ativa e ela entra na plataforma |
| 01.2 | uma pessoa sem conta | ela escolhe "Entrar com Google" e autoriza | a conta é criada com o e-mail do Google |
| 01.3 (negativo) | qualquer tela de cadastro ou configuração | a pessoa percorre a plataforma | não existe campo para senha de portal de vagas |
| 01.4 (negativo) | uma conta com e-mail não verificado | a pessoa tenta parear a extensão | o pareamento é recusado com a instrução de verificar o e-mail |

## CA-02 — Perfil Mestre (RF-02)

| # | Dado | Quando | Então |
|---|---|---|---|
| 02.1 | um perfil vazio | a pessoa preenche só nome e e-mail e salva | o perfil é salvo sem exigir outros campos |
| 02.2 | um perfil | a pessoa adiciona três experiências com organização, cargo, período, atividades, ferramentas e resultados | as três ficam salvas na ordem escolhida e podem ser reordenadas, editadas e excluídas |
| 02.3 | uma experiência marcada como "atual" | a pessoa salva | o fim do período fica vazio e aparece como "Atual" |
| 02.4 | a tela do perfil | a pessoa navega só pelo teclado | todas as seções e ações são alcançáveis e têm rótulo acessível |

## CA-03 — Importação de currículo (RF-03)

| # | Dado | Quando | Então |
|---|---|---|---|
| 03.1 | um PDF com texto de até 10 MB | a pessoa importa | uma proposta de perfil é exibida para revisão, sem alterar o perfil |
| 03.2 | um DOCX | a pessoa importa | idem 03.1 |
| 03.3 | um PDF escaneado (sem texto) | a pessoa importa | aparece a mensagem de arquivo não legível com as alternativas, e nenhuma proposta é criada |
| 03.4 | um arquivo acima de 10 MB ou de outro formato | a pessoa importa | o envio é recusado com mensagem clara |
| 03.5 (negativo) | um currículo que menciona estado civil, gênero ou deficiência | a pessoa importa | esses dados não aparecem na proposta |

## CA-04 — Revisão da proposta (RF-04)

| # | Dado | Quando | Então |
|---|---|---|---|
| 04.1 | uma proposta com 20 campos | a pessoa aprova 15, edita 3 e descarta 2 | o perfil recebe só os 18 aprovados ou editados |
| 04.2 | um campo da proposta diferente do perfil atual | a pessoa abre a revisão | os dois valores aparecem lado a lado |
| 04.3 (negativo) | uma proposta aberta | a pessoa fecha a tela sem aprovar | o perfil não muda |

## CA-05 — Perguntar só o que falta (RF-05)

| # | Dado | Quando | Então |
|---|---|---|---|
| 05.1 | um perfil sem telefone, que é exigido pelo portal suportado | a pessoa termina a importação | a plataforma pede o telefone em linguagem simples |
| 05.2 | um perfil sem CNH, que o portal não pede | a pessoa termina a importação | a plataforma não pergunta sobre CNH |
| 05.3 | a lista de perguntas | a pessoa escolhe "pular" | a pergunta some da lista e pode ser respondida depois no perfil |

## CA-06 — Respostas aprovadas (RF-06)

| # | Dado | Quando | Então |
|---|---|---|---|
| 06.1 | uma sugestão de texto aprovada no painel | a pessoa marca "guardar esta resposta" | a resposta fica salva com a pergunta original e a data |
| 06.2 | uma resposta aprovada para "Por que você quer trabalhar aqui?" | um formulário pergunta "Por que deseja trabalhar conosco?" | a resposta aprovada é proposta, marcada como "resposta aprovada", e precisa ser revisada antes do uso |
| 06.3 | uma resposta aprovada | a pessoa a edita ou exclui na plataforma | a mudança vale para os próximos usos |

## CA-07 — Consentimentos (RF-07)

| # | Dado | Quando | Então |
|---|---|---|---|
| 07.1 | uma conta nova | a pessoa abre os consentimentos | (a) e (b) aparecem com explicação de finalidade e (c) aparece desligado |
| 07.2 | o consentimento (b) desligado | a extensão chama a análise de vaga | a API responde 403 e o painel explica como ligar (negativo na API, TN-09) |
| 07.3 | o consentimento (a) desligado | a pessoa pede sugestão de texto | o painel informa que a IA para textos está desligada e não chama a IA |
| 07.4 | qualquer mudança de consentimento | a pessoa salva | a mudança aparece no registro de ações |

## CA-08 — Exportar e excluir (RF-08)

| # | Dado | Quando | Então |
|---|---|---|---|
| 08.1 | uma conta com perfil, respostas e registro de ações | a pessoa exporta | recebe um JSON legível com perfil, respostas aprovadas, consentimentos e registro de ações, sem tokens |
| 08.2 | uma conta com extensão pareada | a pessoa exclui a conta e confirma | os dados são apagados, a extensão perde o acesso imediatamente e um novo login não encontra dados |
| 08.3 (negativo) | a exclusão pedida | a confirmação não é feita | nada é apagado |

## CA-09 — Pareamento da extensão (RF-09)

O MVP 1 usa o fluxo de **código de pareamento** do [ADR-0007](../03-arquitetura/adr/0007-autenticacao-e-pareamento-da-extensao.md), na forma ajustada pelo [ADR-0017](../03-arquitetura/adr/0017-backend-em-csharp-dotnet-clean-architecture.md): a plataforma gera o código e a pessoa o digita na extensão. O fluxo Authorization Code + PKCE ficou para depois do MVP 1 acadêmico.

| # | Dado | Quando | Então |
|---|---|---|---|
| 09.1 | a pessoa logada na plataforma | ela abre **Extensão** e clica em "Gerar código de pareamento" | aparece um código de 6 dígitos válido por 10 minutos (`POST /extension/pairing-codes`) |
| 09.2 | um código válido e a extensão recém-instalada | a pessoa digita o código no painel e clica em "Conectar" | a extensão recebe tokens com `scope=extension` (`POST /extension/pair`) e aparece na lista de navegadores conectados |
| 09.3 | uma extensão pareada | a pessoa clica em "Desconectar" na plataforma | a próxima chamada da extensão falha com 401 (access e refresh revogados na hora) e o painel volta para "Conectar" |
| 09.4 (negativo) | um código expirado ou já usado | alguém tenta pareá-lo | o pareamento é recusado |
| 09.5 (negativo) | um token com `scope=extension` | a extensão chama `GET /me/export` ou `GET /profile` | a API responde 403: a extensão nunca recebe o perfil inteiro nem a exportação |
| 09.6 | o plano Gratuito com 1 navegador já conectado | a pessoa tenta conectar um segundo navegador | o pareamento explica o limite de 1 navegador e oferece desconectar o atual ou conhecer o Plus (até 3); nada é desconectado sem escolha da pessoa (RF-26) |

## CA-10 — Página autorizada e ações (RF-10)

| # | Dado | Quando | Então |
|---|---|---|---|
| 10.1 | uma página de formulário do portal suportado | a pessoa abre o painel | aparecem as ações "Ler formulário" e "Importar do portal" |
| 10.2 | uma página de vaga do portal suportado com o consentimento (b) ligado | a pessoa abre o painel | aparece também "Analisar vaga" |
| 10.3 | um site fora do portal suportado | a pessoa abre o painel | o painel diz que o site não é suportado e oferece pedir permissão; sem permissão, nada é lido |

## CA-11 — Leitura dos campos (RF-11)

| # | Dado | Quando | Então |
|---|---|---|---|
| 11.1 | a página de cadastro do portal demo | a pessoa clica em "Ler formulário" | todos os campos visíveis aparecem com rótulo, seção, tipo, opções e valor atual |
| 11.2 | um modal de candidatura aberto sobre a página | a pessoa clica em "Ler formulário" | só os campos do modal são lidos |
| 11.3 (negativo) | uma página com campo de senha, CAPTCHA e upload | a pessoa lê o formulário | esses campos não são lidos e não aparecem no payload enviado à API (TN-02, TN-03) |
| 11.4 | campos ocultos (`display:none`, `hidden`) | a pessoa lê o formulário | eles não são lidos |

## CA-12 — De-para (RF-12)

| # | Dado | Quando | Então |
|---|---|---|---|
| 12.1 | campos "Nome completo", "E-mail" e "CPF" | o de-para roda | as três chaves são resolvidas por regra, sem chamar a IA |
| 12.2 | um campo com rótulo incomum, sem regra | o de-para roda | a IA aponta uma chave e o valor exibido é exatamente o valor do perfil (TN-08) |
| 12.3 (negativo) | a IA aponta uma chave que não existe no perfil | o de-para valida | o campo fica "ausente", não "pronto" |
| 12.4 (negativo) | um select cujas opções não incluem o valor do perfil | o de-para valida | o campo fica "ambíguo", sem opção selecionada |
| 12.5 | blocos "Experiência 1" e "Experiência 2" com empresas do perfil em ordem inversa | o de-para roda | cada bloco recebe a experiência da empresa correspondente |
| 12.6 | o serviço de IA fora do ar | o de-para roda | os campos de regra são resolvidos, os outros ficam "ausente" e o aviso de IA indisponível aparece |

## CA-13 — Revisão no painel (RF-13)

| # | Dado | Quando | Então |
|---|---|---|---|
| 13.1 | propostas com estados variados | o painel exibe a lista | cada campo mostra valor, origem e estado, com cor e texto |
| 13.2 (negativo) | campos ambíguos, ausentes, divergentes e sugestões de texto | o painel exibe a lista | nenhum deles vem marcado para preenchimento |
| 13.3 | um campo pronto | a pessoa edita o valor | o valor editado é o que será preenchido |
| 13.4 | a lista de revisão | a pessoa abre o painel | o resumo informa quantos campos serão preenchidos e quantos precisam dela |

## CA-14 — Preenchimento (RF-14)

| # | Dado | Quando | Então |
|---|---|---|---|
| 14.1 | 18 campos marcados | a pessoa clica em "Preencher selecionados" | os 18 campos recebem os valores e o portal reconhece as mudanças (eventos input, change, blur) |
| 14.2 | um campo que falha ao preencher | o preenchimento termina | o resumo mostra o campo com falha e o que fazer |
| 14.3 (negativo) | o preenchimento concluído | — | nenhum botão de enviar, salvar, avançar ou aceitar foi clicado e nenhum formulário foi submetido (TN-01) |
| 14.4 (negativo) | campos desmarcados | a pessoa preenche | eles continuam com o valor original |

## CA-15 — Sugestão de texto (RF-15)

| # | Dado | Quando | Então |
|---|---|---|---|
| 15.1 | um campo aberto com limite de 500 caracteres e o consentimento (a) ligado | a pessoa pede sugestão | o texto respeita o limite e usa só fatos do perfil |
| 15.2 (negativo) | um perfil sem experiência em vendas | o campo pergunta sobre experiência em vendas | a sugestão não afirma experiência em vendas |
| 15.3 | uma sugestão exibida | a pessoa edita e clica em "Usar este texto" | o texto editado é o que será preenchido |
| 15.4 (negativo) | uma sugestão exibida | a pessoa não aprova | o campo não é preenchido |

## CA-16 — Portal × perfil (RF-16)

| # | Dado | Quando | Então |
|---|---|---|---|
| 16.1 | o telefone do portal diferente do perfil | o painel exibe o campo | os dois valores aparecem e a opção marcada é "manter portal" |
| 16.2 (negativo) | o campo divergente sem escolha da pessoa | a pessoa preenche | o valor do portal não muda (TN-10) |
| 16.3 | a pessoa escolhe "levar para o perfil" | ela confirma | o perfil é atualizado e a ação vai para o registro |
| 16.4 | a ação "Importar do portal" | a pessoa a executa | os valores do portal aparecem como proposta, que precisa de aprovação antes de entrar no perfil |

## CA-17 — Análise de vaga (RF-17, Should)

| # | Dado | Quando | Então |
|---|---|---|---|
| 17.1 | uma página de vaga com requisitos visíveis | a pessoa clica em "Analisar vaga" | aparecem cargo, empresa, local, contrato, modalidade, jornada e salário (quando visíveis) e cada requisito com seu status |
| 17.2 | um requisito marcado como "atende" | a pessoa olha a evidência | a evidência é uma linha existente do perfil |
| 17.3 (negativo) | a IA marca "atende" sem linha válida | a API valida | o requisito vira "não encontrado" |
| 17.4 | uma vaga que pede CNH | a análise termina | CNH aparece em "a confirmar" com o valor do perfil |
| 17.5 (negativo) | qualquer análise | — | não aparece nota, porcentagem ou frase de compatibilidade garantida |

## CA-18 — Campos bloqueados (RF-18)

| # | Dado | Quando | Então |
|---|---|---|---|
| 18.1 | um campo "Você é pessoa com deficiência?" e o consentimento (c) desligado | o painel exibe o campo | ele aparece como "ação sua", sem valor proposto (TN-05) |
| 18.2 | o consentimento (c) ligado | a pessoa marca manualmente o campo sensível e preenche | só esse campo é preenchido |
| 18.3 (negativo) | uma caixa "Declaro que as informações são verdadeiras" | a pessoa preenche tudo | a caixa continua desmarcada (TN-04) |
| 18.4 (negativo) | qualquer fluxo com campo sensível | — | o dado sensível não é enviado à IA (TN-06) |

## CA-19 — Conclusão (RF-19)

Implementação atual: depois de enviar no portal, a pessoa clica em "Concluí a candidatura" e confirma "Confirmo que enviei: registrar". O registro (`POST /forms/completions`) guarda portal, cargo, empresa, endereço da vaga e quantidade de campos preenchidos, e aparece em **Candidaturas**. O acompanhamento por status e anotações vai além da Opção B do [ADR-0012](../03-arquitetura/adr/0012-registro-minimo-de-conclusao-no-mvp1.md), que continua **Proposta**; essa diferença está registrada como decisão pendente.

| # | Dado | Quando | Então |
|---|---|---|---|
| 19.1 | campos preenchidos pelo Zyft | a pessoa envia no portal e confirma "Concluí a candidatura" no painel | o painel mostra o resumo (preenchidos, pulados, para fazer no site) e "Candidatura registrada" |
| 19.2 | o registro criado | a pessoa abre **Candidaturas** na plataforma | a candidatura aparece com data, cargo, empresa, portal e campos preenchidos, **sem** os valores preenchidos |
| 19.3 (negativo) | a pessoa não confirma o envio | ela fecha o painel | nenhum registro de candidatura é criado; só o registro de ações do preenchimento existe |
| 19.4 (negativo) | o painel detectou o `submit` feito pela pessoa | — | o Zyft só destaca o botão de registrar; nada é registrado sem o clique dela e o envio do formulário nunca é interceptado |
| 19.5 | um registro existente | a pessoa exclui o registro | o registro some do Zyft, com o aviso de que a candidatura continua no site da empresa |

## CA-20 — Registro de ações (RF-20)

| # | Dado | Quando | Então |
|---|---|---|---|
| 20.1 | um preenchimento de 18 campos | a pessoa abre o registro na plataforma | aparece uma entrada com data, ação, domínio do portal e "18 campos" |
| 20.2 (negativo) | qualquer entrada do registro | — | não contém valores preenchidos nem textos gerados |
| 20.3 | a exportação de dados | a pessoa exporta | o registro está incluído |

## CA-21 — Melhorar texto (RF-21, Could)

| # | Dado | Quando | Então |
|---|---|---|---|
| 21.1 | um texto escrito pela pessoa com erros de gramática | ela pede "melhorar" | aparece a versão corrigida com as diferenças destacadas |
| 21.2 (negativo) | um texto sem menção a certificações | ela pede "melhorar" | a versão melhorada não acrescenta certificações nem outros fatos |
| 21.3 | a versão melhorada | a pessoa rejeita | o texto original é mantido |

## CA-22 — Versão do currículo (RF-22, Could)

| # | Dado | Quando | Então |
|---|---|---|---|
| 22.1 | um formulário com campo de anexo | o painel exibe o campo | ele aparece como "ação sua", com a opção de registrar qual versão do currículo a pessoa anexou |
| 22.2 (negativo) | qualquer fluxo | — | o Zyft não anexa arquivos |

## CA-23 — Onboarding em passos (RF-23)

| # | Dado | Quando | Então |
|---|---|---|---|
| 23.1 | uma conta recém-criada | a pessoa entra pela primeira vez | ela é levada a `#/boas-vindas`, com uma pergunta por tela, o capítulo atual ("Capítulo 2 · Sua área") e "Pergunta N de M" |
| 23.2 | qualquer pergunta do onboarding | a pessoa clica em "Preencher mais tarde" | a pergunta é marcada como pulada (`PUT /onboarding` com o id em `skipped`) e a próxima aparece |
| 23.3 | uma resposta preenchida | a pessoa avança | a resposta é gravada nos endpoints do perfil (`PUT /profile`, `POST /profile/experiences` etc.) e o passo entra em `answered` |
| 23.4 | o onboarding parado no capítulo 3 | a pessoa fecha o navegador e volta outro dia | o onboarding retoma no último passo (`lastStep`), sem perder respostas (RNF-17) |
| 23.5 | três perguntas puladas | a pessoa abre o Perfil Mestre | um alerta no topo diz quantas perguntas ficaram para depois e oferece "Continuar de onde parei" e "Responda agora" |
| 23.6 | o capítulo "Combinados" | a pessoa chega ao fim | os consentimentos aparecem desligados, cada um com a finalidade explicada; ao concluir, `completed=true` e o registro de ações ganha `onboarding.completed`, sem conteúdo pessoal |
| 23.7 (negativo) | a pessoa pulou todas as perguntas | ela tenta usar a plataforma e parear a extensão | nada é bloqueado (RN-14); a extensão marca os campos sem dado como "Precisa de você" |
| 23.8 (negativo) | o onboarding | a pessoa percorre todas as telas | não há anúncio em nenhuma delas (RN-08) |

## CA-24 — Área profissional e catálogo por área (RF-24)

| # | Dado | Quando | Então |
|---|---|---|---|
| 24.1 | a pergunta "Em que área você trabalha ou quer trabalhar?" | a pessoa escolhe **Saúde** | as sugestões de cargos, setores, ferramentas e competências passam a ser as de Saúde (ex.: "Técnico(a) de enfermagem", "Prontuário eletrônico") e aparece a pergunta "Registro no conselho" com o exemplo "COREN-MG 123456" |
| 24.2 | a área **Tecnologia** | a pessoa chega à pergunta de links | o link pedido é "GitHub" e não há pergunta de registro profissional |
| 24.3 | a área **Outra área** | a pessoa chega às listas | as listas vêm sem sugestões e a pessoa cadastra os próprios valores |
| 24.4 | qualquer lista de sugestões | a pessoa digita um valor que não está na lista ("Cuidador(a) de idosos") | o valor é salvo como os demais e aparece no perfil |
| 24.5 | o perfil | a pessoa salva área, registro profissional e tipo de pretensão ("Por plantão") | `GET /profile` devolve `workArea`, `professionalRegistration` e `salaryType` com esses valores |
| 24.6 | `GET /catalog/areas` autenticado | a plataforma pede o catálogo | a resposta traz as 8 áreas com `roles`, `sectors`, `tools`, `skills`, `linkLabel` e `registration` (ou `null`) |
| 24.7 (negativo) | uma pessoa sem login | ela chama `GET /catalog/areas` | a API responde 401 |
| 24.8 (negativo) | a área escolhida | a análise de vaga ou o de-para rodam | a área nunca vira nota, filtro de vaga nem promessa de compatibilidade (RN-06) |

## CA-25 — Campos personalizados (RF-25, Should)

| # | Dado | Quando | Então |
|---|---|---|---|
| 25.1 | o bloco "Informações da sua área" | a pessoa cria "Disponível para plantão noturno" = "Sim, 12x36" | o campo é salvo (`POST /profile/custom-fields`) e aparece em `GET /profile` em `customFields`, com data de atualização |
| 25.2 | um campo personalizado | a pessoa edita ou exclui | a mudança vale nas próximas leituras de formulário; o registro de ações ganha `custom_field.changed`, sem o rótulo nem o valor |
| 25.3 | o campo "Registro no COREN" no perfil e um formulário que pergunta "Número do COREN" | a extensão lê o formulário | o campo aparece como Pronto com origem "Informações da sua área" e chave `custom:<id>`, e o valor é exatamente o do perfil |
| 25.4 | uma pergunta aberta da vaga sobre plantões | a pessoa pede sugestão de texto | a IA pode usar o campo personalizado como fato aprovado e o mostra entre os fatos usados |
| 25.5 (negativo) | um rótulo como "Religião", "Tenho deficiência", "CPF" ou "Senha do portal" | a pessoa tenta salvar | a API responde 422 `profile.custom_field_sensitive` e a tela orienta usar **Dados sensíveis** ou não informar (RN-15, TN-13) |
| 25.6 (negativo) | um rótulo com mais de 80 caracteres ou um valor com mais de 500 | a pessoa tenta salvar | a API responde 400 com a mensagem do campo |
| 25.7 (negativo) | um campo personalizado de outra pessoa | alguém chama `PUT` ou `DELETE` com o id | a API responde 404 |

## CA-26 — Planos e limites de uso (RF-26)

| # | Dado | Quando | Então |
|---|---|---|---|
| 26.1 | uma conta nova | a pessoa abre **Planos** | ela vê o plano Gratuito como atual, o Plus por R$ 9,90/mês ou R$ 99/ano, o pacote avulso de R$ 4,90, a tabela comparativa, o bloco "Sempre de graça" e o aviso "Ambiente de demonstração: nenhuma cobrança real" (RN-13) |
| 26.2 | o plano Gratuito | a pessoa abre o medidor de uso | ela vê, por recurso e pelo mês corrente, quanto usou e o limite: 10 candidaturas assistidas, 5 sugestões de IA, 10 análises de vaga, 2 importações; banco de respostas até 15; 1 navegador |
| 26.3 | o plano Gratuito | a pessoa escolhe "Testar 7 dias grátis" (mensal ou anual) e confirma | `POST /billing/subscribe` deixa o plano `plus` com `trialEndsAt` em 7 dias; anúncios somem e os limites viram "sem limite"; o registro de ações ganha `plan.changed` |
| 26.4 | o plano Plus | a pessoa cancela | `POST /billing/cancel` volta ao Gratuito (na demonstração pode ser imediato); **nenhum dado do perfil é apagado** |
| 26.5 | 10 candidaturas assistidas usadas no mês | a pessoa lê um formulário numa página nova | a API responde 402 `plan.limit_reached` com `resource`, `used` e `limit`, e o painel mostra as opções: ver uma oferta patrocinada (se ainda houver), comprar pacote ou conhecer o Plus |
| 26.6 | uma página já contada no mês | a pessoa clica em "Ler novamente" na mesma página (origem e caminho iguais) | o uso não aumenta (RNF-16) |
| 26.7 | bônus recompensado e um pacote ativos | o limite do mês acaba | o consumo segue a ordem limite do mês, depois bônus recompensado, depois pacotes (o que vence antes primeiro) |
| 26.8 (negativo) | o limite atingido | a pessoa edita o perfil, exporta os dados ou exclui a conta | tudo funciona normalmente (RN-12, TN-14) |
| 26.9 (negativo) | qualquer tela de planos | a pessoa percorre a assinatura | nenhum dado de cartão é pedido nem coletado (RN-13) |
| 26.10 (negativo) | o banco de respostas com 15 respostas no Gratuito | a pessoa tenta guardar a 16ª | a API recusa com 402 e a tela explica o limite; as 15 existentes continuam editáveis e excluíveis |

## CA-27 — Pacote avulso (RF-27, Should)

| # | Dado | Quando | Então |
|---|---|---|---|
| 27.1 | o plano Gratuito | a pessoa clica em "Comprar pacote" e confirma | `POST /billing/packs` cria um pacote com +15 candidaturas assistidas e +10 sugestões de IA, com validade de 60 dias; o registro de ações ganha `pack.purchased` |
| 27.2 | um pacote ativo | a pessoa abre o medidor de uso | ela vê o saldo restante e a data de validade |
| 27.3 | dois pacotes com validades diferentes | o limite do mês acaba | é consumido primeiro o pacote que vence antes |
| 27.4 (negativo) | um pacote vencido | o limite do mês acaba | o pacote vencido não é usado e a API responde 402 |
| 27.5 (negativo) | o pacote comprado | — | nenhuma cobrança real acontece e nenhum dado de pagamento é guardado (RN-13) |

## CA-28 — Anúncio recompensado (RF-28, Could)

| # | Dado | Quando | Então |
|---|---|---|---|
| 28.1 | o plano Gratuito | a pessoa clica em "Ganhar +3" e vê a oferta patrocinada até o fim | `POST /ads/rewarded/claim` soma +3 candidaturas assistidas de bônus no mês; o registro de ações ganha `rewarded.claimed` |
| 28.2 | 3 resgates no mês | a pessoa tenta um quarto | a API responde 409 `ads.rewarded_exhausted` e o botão some até o mês seguinte |
| 28.3 (negativo) | a pessoa não quer ver ofertas | ela ignora o botão | nada muda no uso nem no perfil: o recompensado é sempre opcional |
| 28.4 (negativo) | o plano Plus | a pessoa abre o painel | não aparece o botão de recompensado |

## CA-29 — Anúncios no plano gratuito (RF-29, Should)

| # | Dado | Quando | Então |
|---|---|---|---|
| 29.1 | o plano Gratuito, área Tecnologia | a pessoa abre o Perfil Mestre | aparece no máximo um card nativo na coluna lateral (`web.profile.rail`), com o selo "Patrocinado", "Por que estou vendo isso?" e "Não tenho interesse" |
| 29.2 | um anúncio exibido | a pessoa clica em "Por que estou vendo isso?" | o texto (`reason`) explica em linguagem simples que o anúncio usa só a área, os cargos buscados ou o requisito da vaga |
| 29.3 | um anúncio exibido | a pessoa escolhe ocultar este anúncio | ele não volta por 30 dias (`scope=ad`); ao ocultar o tema, nenhum anúncio daquele tema volta (`scope=topic`); o registro de ações ganha `ad.hidden` |
| 29.4 | a análise da vaga com um requisito que não aparece no perfil ("Inglês avançado") | o painel mostra "A vaga e você" | o banner do rodapé pode ser trocado por um card contextual (`ext.job.contextual`) com "Sobre: Inglês avançado" |
| 29.5 | a candidatura registrada no painel | a pessoa vê a confirmação | pode aparecer um card de próximo passo (`ext.after-complete`), sempre depois da conclusão |
| 29.6 | um anúncio | a pessoa clica nele | `POST /ads/{id}/click` aumenta só um contador agregado, sem identificar a pessoa |
| 29.7 (negativo) | os placements proibidos (onboarding, entrar e criar conta, importar currículo, dados sensíveis, conta e privacidade, painel "Ler formulário") | a interface pede anúncio ou a pessoa navega por essas telas | não há anúncio; a API responde 204 se for chamada (RN-08, TN-11, TN-15) |
| 29.8 (negativo) | o plano Plus | qualquer tela pede anúncio | a API responde 204 e nenhum espaço de anúncio aparece |
| 29.9 (negativo) | qualquer chamada a `GET /ads` | — | a requisição só leva `placement` e `topic`; a resposta não tem nome, contato, dado sensível nem identificador da pessoa; os logs não têm dado pessoal (RN-09, RNF-14, TN-12) |
| 29.10 (negativo) | qualquer tela com anúncio | — | há no máximo um anúncio na tela (RN-11) |
| 29.11 (negativo) | a API de anúncios fora do ar ou lenta (mais de 1,5 s) | a tela carrega | o espaço do anúncio some e a tela funciona normalmente (RNF-15) |
| 29.12 (negativo) | o catálogo de anúncios | — | todo anunciante é marcado como "Anunciante de demonstração" e nenhum anúncio promete contratação ou candidatura em massa (RN-06, RN-10) |

