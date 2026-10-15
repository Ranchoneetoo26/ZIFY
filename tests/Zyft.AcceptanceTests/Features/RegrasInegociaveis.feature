# language: pt
Funcionalidade: Regras inegociáveis do Zyft
  Para confiar no copiloto de candidaturas
  Como pessoa candidata
  Quero que o Zyft nunca envie nada, nunca leia senhas, não invente valores e proteja meus dados sensíveis

  Contexto:
    Dado que sou uma pessoa cadastrada com o perfil
      | campo    | valor              |
      | fullName | Ana Paula Souza    |
      | email    | ana@exemplo.com    |
      | phone    | (44) 99999-2222    |

  Esquema do Cenário: Campos que exigem ação humana nunca recebem valor (RF-18)
    Quando a extensão pede sugestões para um campo "<tipo>" com rótulo "<rotulo>"
    Então o campo vem bloqueado
    E o campo não traz nenhum valor
    E o campo não vem marcado para preencher

    Exemplos:
      | tipo     | rotulo                                   |
      | password | Senha                                    |
      | text     | Confirme sua senha                       |
      | text     | Digite o código do captcha               |
      | file     | Anexe seu currículo                      |
      | checkbox | Declaro que as informações são verdadeiras |

  Cenário: Valor objetivo vem do perfil por regra determinística (RN-02)
    Quando a extensão pede sugestões para um campo "email" com rótulo "E-mail"
    Então o campo traz o valor "ana@exemplo.com"
    E a origem do valor é "Rule"

  Cenário: A IA só aponta a chave; o valor escrito por ela é ignorado (ADR-0004)
    Dado que a IA responde ao de-para apontando "personal.email" e escrevendo o valor "hacker@mal.com"
    Quando a extensão pede sugestões para um campo "text" com rótulo "Como falamos com você por escrito"
    Então o campo traz o valor "ana@exemplo.com"
    E a origem do valor é "Ai"

  Cenário: Dado sensível bloqueado sem consentimento (RN-04)
    Quando a extensão pede sugestões para um select "Gênero" com a opção "Feminino"
    Então o campo vem bloqueado
    E o campo não traz nenhum valor

  Cenário: Dado sensível com consentimento vem desmarcado (RN-04)
    Dado que autorizei o uso de dados sensíveis
    E cadastrei o dado sensível "Gender" com o valor "Feminino"
    Quando a extensão pede sugestões para um select "Gênero" com a opção "Feminino"
    Então o campo traz um valor
    E o campo não vem marcado para preencher

  Cenário: Análise de vaga sem consentimento é recusada pela API (RF-07)
    Quando peço a análise da vaga "Requisitos:\n- Excel avançado"
    Então a resposta é 403 com o código "consent_required"

  Cenário: Análise de vaga não aceita evidência inventada (ADR-0013, RN-06)
    Dado que autorizei a análise de vaga
    E que a IA diz que o requisito "Doutorado em física" é atendido pela linha 999
    Quando peço a análise da vaga "Requisitos:\n- Doutorado em física"
    Então o requisito "Doutorado em física" fica com status "nao_encontrado"
    E a resposta não tem porcentagem nem nota

  Cenário: Token da extensão não exporta os dados da conta (RF-08)
    Dado que pareei a extensão com a conta
    Quando a extensão tenta exportar os dados da conta
    Então a resposta é 403 com o código "forbidden"
