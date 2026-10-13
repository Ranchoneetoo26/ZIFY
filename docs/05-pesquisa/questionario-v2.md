# Questionário v2 — Candidatura Assistida

| Campo | Valor |
|---|---|
| Documento | Questionário da pesquisa v2 |
| Versão | 2.0 |
| Status | Em campo |
| Fonte de verdade | [`ferramentas/pesquisa/questionario-v2.json`](../../ferramentas/pesquisa/questionario-v2.json); este arquivo é gerado por `ferramentas/pesquisa/gerar_questionario_md.py` |
| Formulário | Google Forms do time (o mesmo da v1, reescrito em 12/10/2026) |
| Plano | [plano-de-pesquisa.md](plano-de-pesquisa.md) |

**Título:** Pesquisa: como você se candidata a vagas pela internet?

**Descrição:**

> Estamos criando o Zyft, um assistente que ajuda a preparar e preencher candidaturas a vagas pela internet a partir das suas próprias informações, com revisão de cada resposta antes do envio.
>
> Queremos entender o que mais atrapalha na hora de se candidatar, em qualquer área ou nível de experiência. Leva cerca de 5 minutos. As respostas são anônimas; o contato no final é opcional.

**Mensagem de confirmação:** Obrigado! Suas respostas vão ajudar a construir um assistente de candidatura que respeita o seu tempo e as suas informações.

## 1. Sobre você

**1. Qual a sua idade?**  
*Múltipla escolha · obrigatória* · chave `idade`

- Até 20 anos
- 21 a 25 anos
- 26 a 30 anos
- 31 a 40 anos
- Acima de 40 anos

**2. Qual frase descreve melhor o seu momento profissional?**  
*Múltipla escolha · obrigatória · com opção "Outro"* · chave `momento`

- Estudante, procurando estágio ou primeiro emprego
- Empregado(a), buscando outra oportunidade
- Desempregado(a), buscando recolocação
- Mudando de área (transição de carreira)
- Trabalho por conta própria (autônomo, PJ, freelancer)
- Não estou procurando vaga no momento

**3. Em qual área você busca (ou buscaria) vagas?**  
*Múltipla escolha · obrigatória · com opção "Outro"* · chave `area`

- Administrativo / Financeiro
- Comercial / Vendas / Atendimento
- Tecnologia
- Saúde
- Indústria / Logística / Operações
- Educação
- Marketing / Comunicação
- Serviços gerais / Varejo

**4. Quanto tempo de experiência profissional você tem, somando todas as áreas?**  
*Múltipla escolha · obrigatória* · chave `experiencia`

- Nenhuma
- Menos de 1 ano
- 1 a 3 anos
- 3 a 5 anos
- 5 a 10 anos
- Mais de 10 anos

**5. Qual a sua escolaridade?**  
*Múltipla escolha · obrigatória* · chave `escolaridade`

- Ensino fundamental
- Ensino médio
- Técnico
- Superior em andamento
- Superior completo
- Pós-graduação

**6. Onde você mora?**  
*Múltipla escolha · obrigatória* · chave `cidade`

- Capital ou região metropolitana
- Cidade média (100 mil a 500 mil habitantes)
- Cidade pequena (menos de 100 mil habitantes)
- Fora do Brasil

**7. Quanto você se sente à vontade preenchendo cadastros e formulários pela internet?**  
*Escala · obrigatória* · chave `familiaridade`

1 (Tenho muita dificuldade) a 5 (Muito à vontade)

## 2. Como você se candidata hoje

**8. Quantas candidaturas você faz por semana, em média?**  
*Múltipla escolha · obrigatória* · chave `volume`

- Nenhuma no momento
- 1 a 5
- 6 a 15
- 16 a 30
- Mais de 30

**9. Quanto tempo leva, em média, cada candidatura (do clique em "candidatar" até o envio)?**  
*Múltipla escolha · obrigatória* · chave `tempo`

- Menos de 5 minutos
- 5 a 15 minutos
- 15 a 30 minutos
- Mais de 30 minutos
- Não sei dizer

**10. Onde você se candidata a vagas hoje?**  
*Caixas de seleção · obrigatória · com opção "Outro"* · chave `portais`

- LinkedIn
- Gupy
- Indeed
- Catho
- InfoJobs
- Vagas.com
- Sites de carreira das próprias empresas
- Portais de vagas da minha região
- Grupos de WhatsApp / Telegram
- Entregando currículo pessoalmente

**11. Numa candidatura típica, o que mais toma o seu tempo? (até 3)**  
*Caixas de seleção · obrigatória · no máximo 3* · chave `etapas`

- Criar conta ou cadastro no site
- Digitar dados pessoais e de contato
- Preencher experiências e formação, item por item
- Adaptar ou anexar o currículo
- Responder perguntas abertas (ex.: "por que você quer trabalhar aqui?")
- Responder perguntas de triagem (pretensão salarial, disponibilidade...)
- Ler a vaga e entender se eu tenho o perfil
- Revisar tudo antes de enviar

## 3. O que atrapalha

**12. O quanto cada situação te incomoda hoje?**  
*Grade · obrigatória* · chave `dores`

Ajuda: 1 = não me incomoda · 5 = me incomoda muito

| Linha | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|
| Preencher os mesmos dados em cada site | | | | | |
| Manter meu currículo e meus perfis atualizados e iguais em todos os sites | | | | | |
| Responder perguntas abertas | | | | | |
| Saber se eu tenho o perfil que a vaga pede | | | | | |
| Entender o que o formulário está pedindo | | | | | |
| Revisar tudo antes de enviar sem deixar passar erro | | | | | |
| Lembrar onde já me candidatei | | | | | |
| Não ter retorno das empresas | | | | | |

**13. Conte a última vez que se candidatar a uma vaga foi cansativo ou frustrante. O que aconteceu?**  
*Parágrafo · opcional* · chave `relato`

**14. Quais destas informações você teria receio de deixar um assistente preencher, mesmo revisando antes?**  
*Caixas de seleção · obrigatória* · chave `receios`

- Dados de contato (e-mail, telefone)
- Documentos (CPF, RG)
- Endereço
- Pretensão salarial
- Dados sensíveis (gênero, raça/cor, deficiência)
- Experiências e formação
- Respostas abertas sugeridas por IA
- Nenhuma, se eu puder revisar tudo

**15. Quanto você confiaria num assistente que preenche formulários de vaga com as suas informações, se nada fosse enviado sem a sua confirmação?**  
*Escala · obrigatória* · chave `confianca`

1 (Não confiaria) a 5 (Confiaria totalmente)

## 4. Com quem você se parece?

> Leia os cinco perfis abaixo e diga qual mais se parece com você na hora de se candidatar.

**Perfil A — Quer agilidade**

> Se candidata a várias vagas e perde tempo digitando os mesmos dados em cada site. Quer reaproveitar o que já escreveu e terminar mais rápido, sem perder o controle do que vai em cada candidatura.

**Perfil B — Quer organizar**

> Tem currículo em vários sites, cada um de um jeito, e nem sempre atualizado. Quer um lugar só com as suas informações certas, revisadas, para usar sempre que precisar.

**Perfil C — Precisa de ajuda para escrever**

> Trava nas perguntas abertas e nunca sabe se o texto está bom. Quer sugestões baseadas no que realmente fez, para revisar e ajustar com as próprias palavras.

**Perfil D — Quer saber se tem o perfil**

> Antes de se candidatar, quer entender o que a vaga pede e o que no seu histórico atende ou não, para decidir se vale a pena e o que destacar.

**Perfil E — Precisa de orientação no formulário**

> Acha os formulários longos e confusos. Quer entender o que cada campo pede, saber o que está faltando e revisar tudo com calma antes de salvar ou enviar.

**16. Qual perfil mais se parece com você?**  
*Múltipla escolha · obrigatória* · chave `persona`

- Perfil A
- Perfil B
- Perfil C
- Perfil D
- Perfil E
- Nenhum deles

**17. O quanto você se identifica com o perfil escolhido?**  
*Escala · obrigatória* · chave `identificacao`

1 (Pouco) a 5 (Totalmente)

**18. O que no perfil escolhido NÃO combina com você, ou o que está faltando?**  
*Parágrafo · opcional* · chave `personaFalta`

Ajuda: Opcional, mas ajuda muito.

## 5. O que você acha da ideia?

> O Zyft é um assistente para quem se candidata a vagas pela internet. Você monta um perfil profissional uma vez (ou importa do seu currículo) e revisa tudo. Quando estiver num site de vagas, uma extensão do navegador sugere o preenchimento dos campos com base no seu perfil e ajuda a escrever respostas abertas usando só informações que você aprovou. Você confere cada campo e decide se salva ou envia: nada é enviado sem a sua confirmação, e o Zyft nunca pede a senha dos sites de vagas.

**19. Qual o seu interesse em usar um assistente assim?**  
*Escala · obrigatória* · chave `interesse`

1 (Nenhum) a 5 (Muito)

**20. Qual a importância de cada recurso para você?**  
*Grade · obrigatória* · chave `recursos`

| Linha | Nada importante | Pouco importante | Importante | Muito importante |
|---|---|---|---|---|
| Um perfil único, reaproveitado em vários sites | | | | |
| Importar meu currículo (PDF ou Word) para montar o perfil | | | | |
| Preenchimento assistido dos formulários de vaga | | | | |
| Sugestão de texto para perguntas abertas, só com informações minhas | | | | |
| Comparar a vaga com o meu perfil (o que atende e o que falta) | | | | |
| Revisar e confirmar cada campo antes de enviar | | | | |
| Não precisar informar a senha dos sites de vagas | | | | |
| Poder exportar ou apagar todos os meus dados | | | | |

**21. Como você preferiria usar uma ferramenta assim?**  
*Múltipla escolha · obrigatória* · chave `modelo`

- Gratuita, com limite de uso por mês
- Assinatura mensal, sem limite
- Pagando só quando usar (pacotes avulsos)
- Só usaria se fosse totalmente gratuita

**22. Quanto você pagaria por mês por uma ferramenta assim?**  
*Múltipla escolha · obrigatória* · chave `preco`

- Não pagaria
- Até R$ 10
- De R$ 10 a R$ 20
- De R$ 20 a R$ 40
- Mais de R$ 40

**23. O que te impediria de usar?**  
*Caixas de seleção · obrigatória · com opção "Outro"* · chave `barreiras`

- Medo de errar alguma informação numa candidatura
- Não confiar em IA para escrever por mim
- Preocupação com a privacidade dos meus dados
- Ter que instalar uma extensão no navegador
- Funcionar em poucos sites de vagas
- Preço
- Prefiro fazer tudo à mão
- Nada, eu usaria

**24. O que faria você usar o Zyft em vez de se candidatar do jeito que faz hoje?**  
*Parágrafo · opcional* · chave `escolheria`

**25. Quer testar o Zyft antes do lançamento? Deixe seu e-mail ou WhatsApp (opcional)**  
*Resposta curta · opcional* · chave `contato`

Ajuda: Usamos só para chamar para o teste. Não entra no relatório.

## Perguntas removidas da v1

| Pergunta da v1 | Motivo |
|---|---|
| Você tem CNPJ ou MEI ativo? | Específico do marketplace PJ (MVP 2) |
| Qual modelo de contratação você prefere hoje? | PJ/CLT é MVP 2 |
| E qual formato de trabalho? | Preferência de vaga, não valida a dor de candidatura |
| O que mais pesa na sua decisão ao escolher uma oportunidade? (até 3) | Decisão de oportunidade é do marketplace |
| Quais dessas dificuldades você já enfrentou? | Dificuldades do marketplace; substituída pela grade de dores da candidatura |
| Antes de se candidatar, quais informações você precisa ver na oportunidade? | Informação de vaga é do marketplace; substituída por receios de preenchimento |
| Qual a importância de cada recurso para você? (marketplace) | Recursos do marketplace; substituída pela grade de recursos da Candidatura Assistida |
| Você prefere pagar uma assinatura fixa ou uma comissão sobre cada trabalho fechado? | Comissão é do marketplace; substituída por modelo de uso |
