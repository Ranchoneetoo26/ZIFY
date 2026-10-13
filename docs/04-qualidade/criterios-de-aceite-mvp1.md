# Critérios de aceite do MVP 1

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Critérios de aceite (CA) do MVP 1 |
| Versão | 1.0 |
| Status | Aprovado para refinamento |
| Dono | Product Owner (com QA) |
| Documentos relacionados | [DRP](../01-produto/drp.md), [Fluxos](../01-produto/fluxos.md), [Estratégia de testes](estrategia-de-testes.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | CA-01 a CA-22, um por requisito funcional do DRP (mesma numeração). |

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

| # | Dado | Quando | Então |
|---|---|---|---|
| 09.1 | a extensão recém-instalada | a pessoa clica em "Conectar" e autoriza a extensão na tela da plataforma (Authorization Code + PKCE) | a extensão fica pareada e aparece na lista de extensões da conta |
| 09.1b | o fluxo `launchWebAuthFlow` falhou | a pessoa usa o código de pareamento mostrado na extensão e o confirma na plataforma | a extensão fica pareada da mesma forma |
| 09.2 | uma extensão pareada | a pessoa revoga o pareamento na plataforma | a próxima chamada da extensão falha com 401 e o painel volta para "Conectar" |
| 09.3 (negativo) | um código de pareamento expirado ou já usado | alguém tenta confirmá-lo | a confirmação é recusada |

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

| # | Dado | Quando | Então |
|---|---|---|---|
| 19.1 | campos preenchidos pelo Zyft | a pessoa envia no portal e confirma "Concluí" no painel | o painel mostra a confirmação visual com a quantidade de campos preenchidos |
| 19.2 | o [ADR-0012](../03-arquitetura/adr/0012-registro-minimo-de-conclusao-no-mvp1.md) aceito com registro mínimo | a pessoa confirma a conclusão | o registro guarda só os campos definidos no ADR |
| 19.3 (negativo) | o ADR-0012 sem decisão ou recusado | a pessoa confirma a conclusão | nenhum registro de candidatura é criado além do registro de ações |

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
