# DVP-E — Documento de Visão do Produto e Escopo

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft |
| Produto | Candidatura Assistida (MVP 1) |
| Documento | Visão do Produto e Escopo (DVP-E) |
| Versão | 2.1 (a 2.0 substituiu a v12 do marketplace, arquivada em `docs/arquivo/marketplace-v1/dvp-e.md`) |
| Status | Aprovado para planejamento do MVP 1. O escopo v2 (E10 a E13) depende dos ADRs 0018, 0019 e 0020 (propostos). |
| Dono | Product Owner |
| Documentos relacionados | [Por que o Zyft](../00-contexto/por-que-o-zyft.md), [Resultados da pesquisa v2](../05-pesquisa/resultados-v2.md), [DRP](drp.md), [Personas](personas.md), [Monetização](monetizacao.md), [Fluxos](fluxos.md), [Roadmap](roadmap.md), [Métricas](metricas.md), [DVS](../02-viabilidade/dvs.md), [DAT](../03-arquitetura/dat.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.x–12.0 | até 05/10/2026 | Marketplace de TI (PJ/CLT). Arquivado. |
| 2.0 | 12/10/2026 | Reescrita completa para a Candidatura Assistida, conforme o documento de reposicionamento (MVP 1 × MVP 2) e o [ADR-0001](../03-arquitetura/adr/0001-reposicionamento-mvp1-candidatura-assistida.md). |
| 2.1 | 21/10/2026 | Versão 2 do produto. Problemas P1–P6 com o resultado da pesquisa v2 (n = 15). Personas narrativas (Rafael, Bia, Beatriz). Escopo ganha E10 a E13: onboarding em passos, área profissional e campos personalizados, planos e limites, anúncios ([ADR-0018](../03-arquitetura/adr/0018-onboarding-em-passos-com-perguntas-adiaveis.md), [ADR-0019](../03-arquitetura/adr/0019-area-profissional-dinamica-e-campos-personalizados.md), [ADR-0020](../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md)). Monetização sai de "fora do MVP 1" por decisão de escopo registrada (RN-07). |

## 1. Sumário executivo

O **Zyft** é um assistente digital para pessoas que se candidatam a vagas de emprego pela internet. Ele reduz o esforço repetitivo da candidatura, ajuda a pessoa a apresentar informações profissionais **verdadeiras** e facilita o preenchimento de formulários de vagas, **mantendo a pessoa no controle de cada decisão**.

O MVP 1 entrega:

- um **Perfil Profissional Mestre** editável e reutilizável;
- **importação de currículo** (PDF ou Word) com revisão e aprovação;
- **preenchimento assistido** num primeiro portal suportado, por meio de uma extensão de navegador;
- **sugestões de IA** para textos livres usando apenas fatos aprovados;
- **revisão e confirmação explícita** antes de preencher, salvar ou enviar.

O produto não é exclusivo de nenhuma área profissional, nível de experiência, modalidade de trabalho ou tipo de candidato. Na versão 2, a pessoa escolhe a área no onboarding e as sugestões se adaptam a ela.

O essencial é gratuito. Quem precisa de mais pode assinar o Plus ou comprar um pacote avulso, e o plano gratuito mostra anúncios rotulados, nunca no formulário e nunca escolhidos por dados pessoais ([monetização](monetizacao.md)).

## 2. Declaração de visão

> Para pessoas que se candidatam a vagas pela internet e perdem tempo repetindo as mesmas informações em sites diferentes, o **Zyft** é um assistente de candidatura que prepara e preenche formulários com base nas suas próprias informações. Diferente de robôs de candidatura em massa, o Zyft não inventa nada, não guarda a senha dos sites e não envia nada sem a sua confirmação.

**Proposta de valor (frase de produto):**
> "Um assistente que ajuda você a preparar e preencher candidaturas com base nas suas informações, revisar cada resposta e confirmar cada ação antes do envio."

## 3. Problema

| # | Problema | Evidência da pesquisa v2 (n = 15) | Situação |
|---|---|---|---|
| P1 | A pessoa preenche repetidamente dados parecidos em sites de vagas diferentes. | "Preencher os mesmos dados em cada site": média 4,40, 80% de notas 4–5. Preencher experiências e formação item por item é a etapa que mais toma tempo (67%); criar cadastro vem em seguida (60%). | **Validado.** Sustenta-se também no recorte do plano (n = 12: 4,33; 75%). |
| P2 | Currículos e perfis em plataformas diferentes ficam incompletos, desatualizados ou inconsistentes. | "Manter currículo e perfis atualizados e iguais": 3,60, 53%. | **Validado no limite.** No recorte do plano cai para 3,50 e 42%. |
| P3 | Campos abertos exigem bons textos, e a pessoa não quer (nem deve) inventar. | "Responder perguntas abertas": 3,27, 40%. Mas 47% citam perguntas abertas entre as três etapas que mais tomam tempo. | **Não validado** como dor geral. Relevante para um grupo (persona Bia). |
| P4 | Formulários e etapas são demorados e difíceis de revisar. | 53% levam 15 minutos ou mais por candidatura (67% entre os que se candidatam toda semana). "Entender o formulário": 2,00; "Revisar sem errar": 2,20. | **Parcial.** Demora confirmada; dificuldade de entender ou revisar, não. |
| P5 | É difícil saber se o perfil atende ao que a vaga pede. | "Saber se tenho o perfil": 2,33, 13%. Mas "comparar a vaga com meu perfil" é importante para 80%. | **Não validado** como dor; valorizado como recurso. |
| P6 | Depois de várias candidaturas é difícil lembrar onde se candidatou. | 3,00, 47% (controle). | **Não validado.** Continua no MVP 2. |

A dor mais alta da pesquisa empatou com a de redigitar: **"não ter retorno das empresas"** (4,40; 87%). Ela era uma pergunta de controle e o Zyft não a resolve: depende das empresas. O registro de candidaturas ajuda a pessoa a acompanhar, mas o produto não promete retorno (RN-06).

Critério do [plano de pesquisa](../05-pesquisa/plano-de-pesquisa.md#5-critérios-de-validação): média ≥ 3,5, ≥ 50% de notas 4–5 e ≥ 15 respostas. A amostra é pequena e 60% dela é de tecnologia, então os resultados orientam, não encerram a discussão. Os números completos e as limitações estão em [resultados-v2.md](../05-pesquisa/resultados-v2.md).

## 4. Público-alvo e personas

O público é **qualquer pessoa que se candidata a vagas pela internet**: quem busca o primeiro emprego, profissionais experientes, pessoas empregadas buscando mudança, pessoas desempregadas, estudantes e pessoas em transição de carreira.

As personas do MVP 1 são **categorias de necessidade**, não perfis demográficos rígidos. O detalhamento, com cenários e critérios de sucesso por persona, está em [personas.md](personas.md).

| ID | Persona | Necessidade principal |
|---|---|---|
| PN1 | Quer reduzir tarefas repetitivas | Reutilizar informações profissionais e preencher formulários com menos digitação |
| PN2 | Precisa organizar suas informações | Manter dados e experiências consistentes e revisar o conteúdo antes de usar |
| PN3 | Precisa de apoio na redação | Preparar respostas claras para campos abertos usando somente fatos confirmados |
| PN4 | Busca adequação à vaga | Entender requisitos visíveis e comparar com o próprio perfil, sem promessa de compatibilidade |
| PN5 | Precisa de orientação no preenchimento | Identificar campos, esclarecer informações ausentes e revisar antes de salvar ou enviar |

A pesquisa confirmou PN1, PN2 e PN4 como perfis (escolhidos por 33%, 27% e 20%); PN3 (7%) e PN5 (0%) não foram confirmadas e ficam como necessidades secundárias.

Para decisões de tela e de texto, o time usa três personas narrativas construídas a partir das respostas:

| Persona | Papel | Necessidades | Frase |
|---|---|---|---|
| Rafael, 28, analista de tecnologia, 1–5 candidaturas por semana | Primária | PN1, PN2 | "Já escrevi isso dez vezes. Só quero conferir e seguir." |
| Bia, 20, estágio ou transição, 6–15 por semana | Secundária | PN3, PN4 | "Não sei o que escrever e tenho medo de exagerar." |
| Beatriz Lima, técnica de enfermagem | Hipótese a validar (só 1 de 15 respostas é de Saúde) | PN1, PN5 | Testa se a área dinâmica serve fora da tecnologia. |

**Antipersona:** quem quer **disparar candidaturas em massa sem revisar** ou automatizar a conta do portal sem supervisão. O Zyft recusa esse uso por princípio (ver §8).

## 5. Stakeholders

| Stakeholder | Interesse | Papel no projeto |
|---|---|---|
| Pessoas candidatas | Candidatar-se com menos esforço e mais confiança | Usuárias; participantes da pesquisa e dos testes |
| CEO / idealizadores | Validar o produto e preparar o MVP 2 | Decisão estratégica e de investimento |
| Product Owner | Valor entregue, backlog e aceite | Dono do DVP-E e do DRP |
| Product Manager | Roadmap, métricas, descoberta e lançamento | Dono do roadmap e das métricas |
| Arquitetura | Decisões técnicas sustentáveis | Donos do DAT e dos ADRs |
| Desenvolvimento (back, front, extensão) | Construir com qualidade | Implementação |
| QA | Garantir as regras de confiança | Estratégia e execução de testes |
| Portais de vagas | Termos de uso respeitados, sem tráfego automatizado abusivo | Externos; restringem o que a extensão pode fazer |
| Encarregado de dados (LGPD) | Tratamento adequado de dados pessoais | Revisão de privacidade |

Os papéis do time (humanos e agentes de IA) estão em [`docs/08-time/`](../08-time/).

## 6. Princípios do produto

1. **A pessoa decide.** Nada é preenchido, salvo ou enviado sem revisão; nada é enviado sem confirmação explícita.
2. **Só fatos verdadeiros.** A IA nunca inventa empresas, cargos, cursos, certificações, competências, idiomas, resultados, datas ou experiências.
3. **Sem senhas, sem atalhos.** O Zyft não pede, não guarda e não usa a senha dos portais. CAPTCHA, autenticação adicional e verificações de segurança são resolvidos pela própria pessoa.
4. **Mínimo de dados.** A extensão recebe só o necessário para a tela em que a pessoa está.
5. **Inclusivo.** Útil para qualquer trajetória, nível de familiaridade digital e tipo de vaga. Linguagem simples e acessível.
6. **Transparente.** Toda ação automatizada fica registrada de forma que a pessoa entenda o que aconteceu.

## 7. Escopo do MVP 1

### 7.1 Dentro do escopo

| # | Capacidade | Requisitos (DRP) |
|---|---|---|
| E1 | Conta na plataforma, consentimentos, exportação e exclusão de dados | RF-01, RF-07, RF-08 |
| E2 | Perfil Profissional Mestre editável e reutilizável | RF-02, RF-05, RF-06 |
| E3 | Importação de currículo PDF ou Word com extração sujeita a revisão e aprovação | RF-03, RF-04 |
| E4 | Extensão de navegador pareada com a conta | RF-09 |
| E5 | Preenchimento assistido no **primeiro portal suportado**, escolhido após validação técnica | RF-10 a RF-14, RF-16, RF-18 |
| E6 | Sugestões de IA para textos livres, só com dados aprovados | RF-15 |
| E7 | Leitura das informações visíveis da vaga e apoio à revisão, **se viável no fluxo inicial** | RF-17 (Should) |
| E8 | Revisão e confirmação explícita antes de preencher, salvar ou enviar | RF-13, RF-14, RF-19 |
| E9 | Registro compreensível das ações automatizadas | RF-20 |
| E10 | Onboarding em passos com perguntas adiáveis e alerta de pendentes no perfil | RF-23 |
| E11 | Área profissional com sugestões por área e campos personalizados | RF-24, RF-25 |
| E12 | Plano gratuito com limites, Plus, pacote avulso e anúncio recompensado, com pagamento simulado | RF-26, RF-27, RF-28 |
| E13 | Anúncios rotulados no plano gratuito, sem dados pessoais e fora das telas de confiança | RF-29 |

**Por que E10 a E13 entraram (v2).** A pesquisa confirmou que o valor está no Perfil Mestre reaproveitado (P1 e P2). Um perfil vazio não ajuda ninguém, e um formulário longo de cadastro repete exatamente a dor que o produto quer tirar: daí o onboarding em passos, em que tudo pode ficar para depois (E10). O produto se declara para qualquer área, mas o perfil foi desenhado com cara de tecnologia; a área dinâmica e os campos personalizados corrigem isso (E11). E a pesquisa respondeu às perguntas de modelo de uso e de preço, o que permitiu desenhar um gratuito útil e testar a disposição a pagar sem cobrar (E12, E13). As decisões e alternativas estão nos ADRs 0018, 0019 e 0020, e a história completa em [por que o Zyft](../00-contexto/por-que-o-zyft.md).

### 7.2 Fora do MVP 1

- Acompanhamento central de candidaturas, histórico completo, lembretes e gestão de etapas (**MVP 2**).
- Candidatura automática em massa ou envio sem aprovação (**nunca**).
- Suporte simultâneo a muitos portais (**MVP 2**, um portal por vez).
- Armazenamento de senhas ou controle autônomo da conta do candidato (**nunca**).
- Contorno de CAPTCHA, bloqueios, autenticação ou mecanismos de segurança (**nunca**).
- Promessas de contratação ou de compatibilidade garantida (**nunca**).
- Marketplace bilateral, matching, assinaturas do marketplace (RF-M2-09), reputação e contratação PJ/CLT (**MVP 2**). O plano Plus do assistente não é a assinatura do marketplace (ver [ADR-0020](../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md)).
- Cobrança real: o pagamento do MVP 1 é simulado (RN-13).
- Recursos citados no mockup de Planos que não estão no contrato: versões do perfil por tipo de vaga, lembretes de retorno, currículo em PDF gerado pelo perfil (**MVP 2**, ver [monetização §10](monetizacao.md#10-o-que-a-tela-de-planos-não-pode-prometer)).
- Anúncios em telas de confiança e qualquer uso de dado pessoal para escolher anúncio (**nunca**, RN-08 e RN-09).
- Relatórios e automações adicionais (**MVP 2**).

### 7.3 Pontos a validar antes da implementação

| # | Ponto | Dono | Como decidir | Registro |
|---|---|---|---|---|
| V1 | Primeiro portal da integração real e quais permissões e regras se aplicam | Arquitetura + PO | Spike técnico em 2–3 candidatos (o documento de origem cita Maringá.Com, que **não** é decisão definitiva) e leitura dos termos de uso | [ADR-0011](../03-arquitetura/adr/0011-primeiro-portal-e-conectores-por-portal.md) |
| V2 | Campos do Perfil Mestre necessários no primeiro fluxo; tratamento de opcionais e sensíveis | PO + Arquitetura | Levantamento dos campos do portal escolhido mais pesquisa v2 | [Modelo de dados](../03-arquitetura/modelo-de-dados.md) |
| V3 | Só confirmação visual de conclusão ou registro mínimo | PO + Privacidade | Pesquisa e testes de usabilidade | [ADR-0012](../03-arquitetura/adr/0012-registro-minimo-de-conclusao-no-mvp1.md) |
| V4 | Formatos de currículo aceitos e como revisar a extração | PO + Dev back | PDF com texto e DOCX no início; PDF escaneado fora | [DRP RF-03](drp.md) |
| V5 | Métricas de validação | PM | Metas fixadas em 21/10/2026 com base na pesquisa v2 | [Métricas](metricas.md) |
| V6 | Conformidade dos anúncios da extensão com a política das lojas | Privacidade + PO | Revisão da política de dados da Chrome Web Store e da loja do Edge antes de publicar | [ADR-0020](../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md) |
| V7 | Limite gratuito adequado para quem se candidata muito | PM | Medir no beta quantas pessoas batem o limite e o que escolhem | [Monetização §8](monetizacao.md#8-riscos) |

## 8. Papel da IA

A IA é uma **camada de apoio sujeita a revisão**, não um agente que controla livremente o navegador.

| A IA pode | A IA não pode |
|---|---|
| Organizar o currículo importado numa proposta de Perfil Mestre | Inventar ou "completar" fatos que a pessoa não informou |
| Indicar **qual chave do perfil** corresponde a um campo da página | Escrever o valor de um campo objetivo (o valor vem sempre do perfil) |
| Sugerir texto para campos abertos usando apenas fatos aprovados | Inferir dados sensíveis (gênero, raça/etnia, deficiência, religião, saúde, orientação sexual) |
| Comparar requisitos visíveis da vaga com o perfil, **apontando a evidência** | Prometer compatibilidade ou aprovação |
| Melhorar a redação de um texto escrito pela pessoa | Clicar em enviar, aceitar termos, anexar arquivos ou resolver CAPTCHA |

As garantias técnicas estão em [arquitetura de IA](../03-arquitetura/ia.md) e nos ADRs [0004](../03-arquitetura/adr/0004-ia-aponta-chave-nunca-escreve-valor.md) e [0013](../03-arquitetura/adr/0013-evidencia-por-linha-numerada-na-analise-de-vaga.md).

## 9. Diferencial

| Alternativa hoje | Limitação | Zyft |
|---|---|---|
| Preencher à mão em cada site | Lento e repetitivo; dados inconsistentes | Perfil único reaproveitado, com revisão |
| Autofill do navegador | Só nome, e-mail e endereço; não entende experiências, formação nem perguntas abertas | De-para semântico de blocos repetidos e perguntas abertas |
| "Candidatura fácil" dos portais | Só dentro de um portal; perfil preso a ele | Mesmo perfil em vários portais (MVP 2), começando por um |
| Robôs de candidatura em massa | Enviam sem revisão, violam termos, geram candidaturas ruins | Confirmação explícita, um portal autorizado, sem automação de envio |
| Chatbots genéricos de IA | Inventam fatos; a pessoa copia e cola | IA presa ao perfil aprovado, com evidência visível |

## 10. Objetivos do MVP 1

| # | Objetivo | Como medimos |
|---|---|---|
| O1 | Provar que a Candidatura Assistida reduz o esforço de candidatura | Tempo para concluir uma candidatura no portal suportado, com e sem o Zyft (teste moderado) |
| O2 | Provar que a pessoa confia no que é preenchido | Taxa de campos aceitos sem edição; nota de confiança após o uso |
| O3 | Provar que a IA não inventa | Zero valores objetivos escritos pela IA; 100% dos "atende" com evidência real (testes automatizados e auditoria) |
| O4 | Aprender o que priorizar no MVP 2 e se o modelo de uso se sustenta | Pesquisa v2, entrevistas pós-uso e, no beta, cliques em "Assinar" e "Comprar pacote" com pagamento simulado |

As metas numéricas estão em [métricas](metricas.md), fixadas a partir da pesquisa v2.

## 11. Restrições e premissas

- **Restrições:** respeitar os termos de uso de cada portal; LGPD; a extensão roda só em Chrome e Edge (Manifest V3) no MVP 1; um portal suportado; o time é pequeno e o projeto tem calendário acadêmico.
- **Premissas:** a pessoa tem conta no portal e faz login nele por conta própria; o portal escolhido expõe formulários HTML legíveis (sem iframes de terceiros nem canvas); um modelo open-weight de 7–8B é suficiente para o de-para e os textos (provado no protótipo, com ressalvas).

## 12. Glossário

Ver [glossário](../00-contexto/glossario.md).
