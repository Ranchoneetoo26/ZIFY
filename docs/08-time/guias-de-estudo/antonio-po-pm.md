# Guia de estudo: Product Owner, Product Manager e gestão

| Campo | Valor |
|---|---|
| Integrante | Antonio Randolfo Barroso Neto |
| Papel | Product Owner (PO), Product Manager (PM) e gestão das entregas |
| Versão | 1.1 (produto v2, sincronizado com a implementação) |
| Data | 21/10/2026 |
| Relacionados | [Papéis e RACI](../papeis-e-raci.md), [DVP-E](../../01-produto/dvp-e.md), [DRP](../../01-produto/drp.md), [Resultados v2](../../05-pesquisa/resultados-v2.md), [Monetização](../../01-produto/monetizacao.md) |

Este é um guia de estudo. Ele diz o que você precisa dominar para responder à banca; não registra autoria de código nem de documento.

## 1. O projeto inteiro em uma página

**Problema.** Quem se candidata a vagas pela internet digita os mesmos dados em cada site. Na pesquisa v2, "preencher os mesmos dados em cada site" teve média 4,40 de 5, com 80% de notas 4 ou 5. As etapas que mais tomam tempo são preencher experiências e formação item a item (67%) e criar cadastro no site (60%).

**Pesquisa.** 15 respostas válidas (16 linhas; a de 30/09 era cópia da v1 e saiu), coletadas de 15 a 17/10/2026. Amostra enviesada: 60% de tecnologia, 67% entre 26 e 30 anos, 60% sem nenhuma candidatura no momento. Detalhes em [resultados-v2.md](../../05-pesquisa/resultados-v2.md).

**O que é o MVP 1 (v2).** Candidatura Assistida: Perfil Profissional Mestre montado num onboarding em passos (ou importando o currículo), extensão Chrome/Edge que lê o formulário do portal e preenche **só o que a pessoa aprovou**, IA que só usa fatos aprovados. A v2 acrescentou: onboarding em passos com perguntas adiáveis (RF-23, ADR-0018), área profissional dinâmica e campos personalizados (RF-24, RF-25, ADR-0019), planos, limites e anúncios (RF-26 a RF-29, ADR-0020).

**Regras inegociáveis.** Nunca enviar (RN-01). IA só aponta a chave e usa fatos aprovados (RN-02, RN-03). Sem senhas, sem contornar CAPTCHA (RNF-01, RNF-13). Sensíveis segregados e fora da IA (RN-04). Sem dados pessoais em logs (RNF-09). Sem nota nem promessa (RN-06). Nada do MVP 2 sem decisão registrada (RN-07). Na v2: anúncios nunca no formulário nem em telas de confiança (RN-08), nunca com dado pessoal (RN-09), sempre "Patrocinado" (RN-10), no máximo um por tela (RN-11); privacidade nunca é paga (RN-12); pagamento simulado (RN-13).

**Personas.** Rafael (primária: 28 anos, analista de tecnologia, 1–5 candidaturas por semana, "Já escrevi isso dez vezes. Só quero conferir e seguir."). Bia (secundária: 20 anos, estágio ou transição, 6–15 por semana, trava em perguntas abertas, "Não sei o que escrever e tenho medo de exagerar."). Beatriz Lima (técnica de enfermagem, Saúde: **hipótese**, só 1 de 15 respostas é de saúde). Antipersona: quem quer candidatura em massa. As necessidades PN1–PN5 continuam como categorias.

**Arquitetura em 5 linhas.** API C#/.NET 8 em Clean Architecture (ADR-0017), com CQRS (MediatR), EF Core e PostgreSQL (SQLite em desenvolvimento). Plataforma web em HTML/CSS/JS sem build, servida pela API. Extensão Manifest V3 com painel lateral. IA local (Ollama, `qwen2.5:7b`) atrás de uma porta: ela aponta chaves, não escreve valores. Pareamento da extensão por código de 6 dígitos.

**Monetização.** Gratuito com limites e anúncios; Plus R$ 9,90/mês ou R$ 99/ano (sem anúncios, sem limites, 7 dias grátis); pacote avulso R$ 4,90; anúncio recompensado opcional (+3 candidaturas, até +9 no mês). Pagamento simulado. Detalhes em [monetizacao.md](../../01-produto/monetizacao.md).

## 2. A sua área a fundo

### 2.1 Escopo e a regra RN-07

Seu papel é o dono do escopo. A regra RN-07 diz que nada do MVP 2 entra no MVP 1 sem decisão explícita. A v2 fez exatamente isso uma vez, de forma registrada: o [ADR-0020](../../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md) traz monetização para o MVP 1. Você precisa saber explicar:

- **O que era MVP 2:** RF-M2-09, "assinaturas fixas sem comissão", era a assinatura do **marketplace** (empresas e profissionais). O plano Plus da v2 é outra coisa: um plano do assistente, para a própria pessoa candidata.
- **Por que agora:** a pesquisa trouxe sinal de disposição (80% pagariam algum valor; 60% preferem gratuito com limite) e a banca/disciplina pede um produto com modelo de negócio defensável. Monetizar com pagamento **simulado** testa a experiência (limites, medidor, ofertas) sem risco financeiro nem gateway.
- **O que não entra:** itens que aparecem no mockup de Planos mas não estão no contrato do MVP 1: versões do perfil por tipo de vaga, lembretes de retorno, gerar currículo em PDF, suporte com prazo. São "planejado (MVP 2)" e não podem aparecer como promessa (RN-06, RN-07).
- **Documentos que mudaram por causa disso:** [DVS §4.3](../../02-viabilidade/dvs.md) (dizia "sem cobrança no MVP 1"), [termos de uso §7](../../07-legal/termos-de-uso.md) e [política de privacidade](../../07-legal/politica-de-privacidade.md) (dizia "não usamos para publicidade").

### 2.2 Requisitos novos que você precisa saber de cor

| ID | O quê | Prioridade | Por quê (dado) |
|---|---|---|---|
| RF-23 | Onboarding em passos, ~25 perguntas em 6 capítulos, toda pergunta com "Preencher mais tarde"; puladas viram alerta no Perfil Mestre | Must | Perfil único é o recurso mais valorizado (87%); confiança é baixa (3,4), então não dá para exigir tudo de uma vez (RN-14) |
| RF-24 | Área profissional e catálogo por área (8 áreas) | Must | O produto atende qualquer área; 40% da amostra já não era de tecnologia |
| RF-25 | Campos personalizados (nome + valor), recusa rótulo sensível | Should | Cada profissão pede coisas que o Zyft não conhece (COREN, OAB, plantão) |
| RF-26 | Planos e limites; 402 ao exceder | Must | Preferência por gratuito com limite (60%) |
| RF-27 | Pacote avulso R$ 4,90 | Should | 13% preferem pagar só quando usam |
| RF-28 | Recompensado (+3, até 3 vezes no mês) | Could | Dá saída para quem não pagaria (20%) e para quem estoura o limite |
| RF-29 | Anúncios no plano gratuito | Should | Sustenta o gratuito sem cobrar de quem não pode pagar |

As RN novas (RN-08 a RN-15) e as RNF novas (RNF-14 a RNF-17) estão no [DRP](../../01-produto/drp.md). Os critérios de aceite são CA-23 a CA-29, com o mesmo número do RF ([critérios de aceite](../../04-qualidade/criterios-de-aceite-mvp1.md)).

Atenção: RF-21 (melhorar texto) e RF-22 (versão do currículo) **já existiam** como Could. Por isso os novos começam em RF-23.

### 2.3 Pesquisa: o que validou e o que não validou

Critério do [plano de pesquisa](../../05-pesquisa/plano-de-pesquisa.md): média ≥ 3,5, ≥ 50% de notas 4–5 e ≥ 15 respostas.

| Hipótese | Resultado | Leitura honesta |
|---|---|---|
| H1 Redigitar dados | 4,40 / 80% | Validada. Também se sustenta no recorte do próprio plano (n=12: 4,33 / 75%) |
| H2 Manter perfis | 3,60 / 53% | Validada no limite. No recorte n=12 cai para 42% de notas 4–5 |
| H3 Perguntas abertas | 3,27 / 40% | Não validada. Mas 47% citam perguntas abertas entre as etapas que mais tomam tempo |
| H4 Saber se tem o perfil | 2,33 / 13% | Não validada como dor. "Comparar a vaga com o perfil" é recurso valorizado por 80% |
| H5 Entender o formulário / revisar | 2,00 / 20% e 2,20 / 13% | Não validada. Público muito familiarizado com formulários (média 4,07) |
| H6 Candidatura leva 15 min ou mais | 53% de todos; 67% dos 6 ativos | Validada, com amostra pequena |
| H7 Confiança ≥ 3,5 | 3,40 | Não atingida. Por isso a interface reforça revisão, carimbo e origem de cada dado |
| H8 Público não concentrado em tecnologia | 60% tecnologia | Falhou. Amostra enviesada; é a principal limitação |

Controles: "sem retorno das empresas" (4,40 / 87%) venceu, mas é dor que o Zyft não resolve no MVP 1; "lembrar onde se candidatou" (3,00 / 47%) é MVP 2.

Personas (perfis A–E): A 33% (identificação 4,0), B 27% (3,75), D 20% (4,0) confirmadas; C 7% e E 0% não confirmadas. "Nenhum deles" 13%.

### 2.4 Preço: de onde saiu cada número

- **R$ 9,90/mês:** a resposta mais comum de disposição a pagar foi "até R$ 10" (7 de 15, 47%). Todos os 12 que pagariam algo aceitam ao menos essa faixa.
- **R$ 99/ano:** R$ 8,25/mês, dois meses grátis, para busca longa.
- **R$ 4,90 o pacote:** 2 de 15 (13%) preferem pacotes avulsos.
- **Gratuito com limite:** 9 de 15 (60%).
- **Correção importante:** o número "não pagaria 4" que circulou no time somava 16 respostas, porque contava a linha da v1. O correto é **3** (20%).

A estimativa de receita é hipótese, com cenários e suposições de mercado (conversão para o Plus, CPM), não dado da pesquisa. Veja [monetizacao.md](../../01-produto/monetizacao.md).

### 2.5 Métricas e roadmap

- North Star candidata: candidaturas concluídas com assistência e revisadas por semana ([métricas](../../01-produto/metricas.md)). Métricas de monetização medem tempo poupado e campos conferidos, nunca "candidaturas enviadas" (antipersona).
- Metas fixas desde o início: zero valor objetivo escrito pela IA (M7) e 100% de evidência real (M8).
- [Roadmap](../../01-produto/roadmap.md): o que é v2 do MVP 1 e o que continua MVP 2.

## 3. Perguntas difíceis prováveis

1. **"Quinze respostas validam alguma coisa?"** Validam pouco, e dizemos isso no próprio relatório. Quinze é o mínimo do nosso critério. Só a dor de redigitar dados passou em todos os cortes. O resto tratamos como pista. A próxima rodada precisa de mais pessoas fora de tecnologia.
2. **"60% da amostra é de tecnologia. Como vocês dizem que é para qualquer área?"** A hipótese H8 falhou e está registrada como limitação. A decisão de área dinâmica (ADR-0019) é de produto, não um achado da pesquisa. Por isso a Beatriz, de saúde, aparece marcada como hipótese.
3. **"60% não está se candidatando. Por que ouvir essas pessoas?"** O plano previa isso: quem não procura vaga e não se candidata sai das médias de dor. Fizemos o recorte (n=12). Redigitar continua forte (4,33); "manter perfis" cai abaixo de 50% de notas altas.
4. **"Perguntas abertas não passaram. Por que a Bia, que trava nelas, é persona?"** Ela é secundária, não primária. Perguntas abertas são a terceira etapa que mais toma tempo (47%) e a respondente mais parecida com a Bia deu nota 5 para essa dor. É uma minoria relevante, não a maioria.
5. **"A confiança ficou em 3,4, abaixo da meta de 3,5. O produto não deveria parar?"** Não é um go/no-go; é um alerta de desenho. Por isso: revisão campo a campo, carimbo, origem de cada valor, sensíveis trancados e privacidade nunca paga (RN-12).
6. **"O DVS dizia 'sem cobrança no MVP 1'. Por que mudou?"** Mudou por decisão registrada (ADR-0020), seguindo a RN-07. O DVS e os termos foram atualizados. O pagamento é simulado: nenhuma cobrança real (RN-13).
7. **"Assinatura não era MVP 2?"** A assinatura do MVP 2 (RF-M2-09) era do marketplace. O Plus é do assistente, para a pessoa candidata. Mesmo assim, por ser monetização, passou por ADR.
8. **"Anúncio num produto que promete privacidade não é contraditório?"** É o risco mais sério e está tratado por regra: nunca no formulário (RN-08), nunca dado pessoal (RN-09), sempre "Patrocinado" com "Ocultar" e "Por que estou vendo isso?" (RN-10), no máximo um por tela (RN-11), nenhum para quem é Plus.
9. **"De onde veio R$ 9,90?"** Da faixa mais escolhida, "até R$ 10" (47%). Não é preço validado por compra real; é sinal declarado.
10. **"Quantas pessoas não pagariam?"** Três de 15 (20%). Um número diferente (4) circulou porque incluía a linha da v1; foi corrigido.
11. **"A Bia faz 6 a 15 candidaturas por semana. O gratuito dá 10 por mês. Ela vai embora?"** Pode ir, e isso está escrito como tensão. Ela tem o recompensado (+9) e o pacote. Vamos medir quantos atingem o limite antes de mexer nele.
12. **"O mockup de Planos promete lembretes e currículo em PDF."** Esses itens não estão no contrato do MVP 1. São MVP 2 e não podem ser mostrados como recurso do Plus enquanto não forem decididos. A tela implementada (`#/planos`) lista no Plus só o que existe hoje: sem anúncios, candidaturas assistidas, IA, análises, importações e banco de respostas sem limite, e até 3 navegadores, com 7 dias grátis.
13. **"Por que onboarding com 25 perguntas, se a pessoa quer agilidade?"** Uma por tela, todas adiáveis (RN-14). O que for pulado vira alerta no perfil e a extensão marca "Precisa de você" no formulário. A pessoa nunca fica presa.
14. **"Qual é a sua North Star e por que não 'candidaturas enviadas'?"** Candidaturas concluídas com assistência e revisadas. "Enviadas" premiaria volume, que é a antipersona.
15. **"O ADR-0012 dizia registro mínimo sem status, mas a tela de Candidaturas tem status."** É uma divergência conhecida: o ADR ainda está em Proposta e a implementação foi além (10 status, anotações, filtro e gravação sem consentimento próprio, ao tocar em "Concluí a candidatura"). Está registrada numa nota de divergência no próprio ADR, com a proposta: aceitar o status revisando o ADR, ou esconder status e anotações até o MVP 2. A decisão é do PO. Não escondemos.
16. **"Quem aprova os ADRs?"** Humanos nos papéis da tabela de aprovação ([ADRs](../../03-arquitetura/adr/README.md)). Agentes de IA só redigem. Os novos (0018 a 0020) estão em Proposta.
17. **"Como vocês vão saber se a monetização funcionou?"** Métricas de uso dos limites, conversão para o Plus no ambiente de demonstração, taxa de "Ocultar" anúncio e confiança declarada depois do uso. Sem metas inventadas antes de medir.
18. **"E se a pesquisa estivesse errada sobre o preço?"** O preço está numa constante do plano e no contrato (`prices` em `GET /billing`). Mudar é barato; o que não muda são as regras de privacidade.
19. **"O que fica fora do MVP 1 de propósito?"** Envio automático e candidatura em massa (nunca), painel central e lembretes, outros portais, marketplace (MVP 2).
20. **"Por que a persona E sumiu?"** Ninguém escolheu o perfil E (0%). Ela continua no documento como "não validada", como o plano manda.
21. **"Rede de anúncios do Google num produto que promete privacidade? Na extensão?"** Está só **simulada** e em Proposta (ADR-0021): nada vai para o Google. A rede existe para preencher espaços sem anunciante direto. Antes de ativar: consentimento de cookies (LGPD, com CMP), só anúncios não personalizados, nada do perfil para a rede, e a extensão fica **só com anúncios diretos**, porque as políticas do AdSense não permitem anúncios dentro de extensões (confirmar na política vigente). Plus continua sem anúncio nenhum.

## 4. Onde está cada coisa

| Tema | Documento |
|---|---|
| História e porquê | [por-que-o-zyft.md](../../00-contexto/por-que-o-zyft.md), [histórico](../../00-contexto/historico-e-reposicionamento.md) |
| Visão e escopo | [DVP-E](../../01-produto/dvp-e.md) |
| Requisitos e regras | [DRP](../../01-produto/drp.md) |
| Personas | [personas.md](../../01-produto/personas.md) |
| Fluxos | [fluxos.md](../../01-produto/fluxos.md) |
| Roadmap e métricas | [roadmap.md](../../01-produto/roadmap.md), [metricas.md](../../01-produto/metricas.md) |
| Monetização | [monetizacao.md](../../01-produto/monetizacao.md), [ADR-0020](../../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md) |
| Pesquisa | [plano](../../05-pesquisa/plano-de-pesquisa.md), [questionário](../../05-pesquisa/questionario-v2.md), [resultados v2](../../05-pesquisa/resultados-v2.md) |
| Viabilidade | [DVS](../../02-viabilidade/dvs.md) |
| Critérios de aceite | [criterios-de-aceite-mvp1.md](../../04-qualidade/criterios-de-aceite-mvp1.md) |
| Uso do sistema | [guia do usuário](../../06-operacao/guia-do-usuario.md) |
| Legal | [termos](../../07-legal/termos-de-uso.md), [privacidade](../../07-legal/politica-de-privacidade.md) |
| Processo e RACI | [processo](../processo.md), [papéis e RACI](../papeis-e-raci.md) |
| Entregas | [`docs/entregas/`](../../entregas/) |
| Telas desenhadas | [design system](../../03-arquitetura/design-system.md) |

## 5. Exercício de 30 minutos

1. (5 min) Rode a API (`dotnet run --project src/Zyft.Api`) e entre como Rafael (`demo@zyft.local` / `Zyft@2026`).
2. (10 min) Siga o [guia do usuário](../../06-operacao/guia-do-usuario.md) com o Rafael (onboarding completo, 7 de 10 candidaturas assistidas usadas no mês). Depois entre como Beatriz (`beatriz@demo.zyft` / `Zyft@2026`): ela cai no onboarding inacabado (pergunta 22 de 25); vá para o Perfil Mestre, veja o alerta de perguntas puladas e compare as sugestões da área Saúde com as de Tecnologia.
3. (5 min) Abra Planos e confira se a tela mostra o aviso de pagamento simulado (RN-13) e se não promete itens do MVP 2.
4. (10 min) Escolha três critérios de aceite novos (por exemplo CA-23, CA-26, CA-29) e verifique cada um na tela. Anote o que não bate e leve para o time como pendência, não como opinião.
