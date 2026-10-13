# DVP-E — Documento de Visão do Produto e Escopo

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft |
| Produto | Candidatura Assistida (MVP 1) |
| Documento | Visão do Produto e Escopo (DVP-E) |
| Versão | 2.0 (substitui a v12 do marketplace, arquivada em `docs/arquivo/marketplace-v1/dvp-e.md`) |
| Status | Aprovado para planejamento do MVP 1 |
| Dono | Product Owner |
| Documentos relacionados | [DRP](drp.md), [Personas](personas.md), [Fluxos](fluxos.md), [Roadmap](roadmap.md), [Métricas](metricas.md), [DVS](../02-viabilidade/dvs.md), [DAT](../03-arquitetura/dat.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.x–12.0 | até 05/10/2026 | Marketplace de TI (PJ/CLT). Arquivado. |
| 2.0 | 12/10/2026 | Reescrita completa para a Candidatura Assistida, conforme o documento de reposicionamento (MVP 1 × MVP 2) e o [ADR-0001](../03-arquitetura/adr/0001-reposicionamento-mvp1-candidatura-assistida.md). |

## 1. Sumário executivo

O **Zyft** é um assistente digital para pessoas que se candidatam a vagas de emprego pela internet. Ele reduz o esforço repetitivo da candidatura, ajuda a pessoa a apresentar informações profissionais **verdadeiras** e facilita o preenchimento de formulários de vagas, **mantendo a pessoa no controle de cada decisão**.

O MVP 1 entrega:

- um **Perfil Profissional Mestre** editável e reutilizável;
- **importação de currículo** (PDF ou Word) com revisão e aprovação;
- **preenchimento assistido** num primeiro portal suportado, por meio de uma extensão de navegador;
- **sugestões de IA** para textos livres usando apenas fatos aprovados;
- **revisão e confirmação explícita** antes de preencher, salvar ou enviar.

O produto não é exclusivo de nenhuma área profissional, nível de experiência, modalidade de trabalho ou tipo de candidato.

## 2. Declaração de visão

> Para pessoas que se candidatam a vagas pela internet e perdem tempo repetindo as mesmas informações em sites diferentes, o **Zyft** é um assistente de candidatura que prepara e preenche formulários com base nas suas próprias informações. Diferente de robôs de candidatura em massa, o Zyft não inventa nada, não guarda a senha dos sites e não envia nada sem a sua confirmação.

**Proposta de valor (frase de produto):**
> "Um assistente que ajuda você a preparar e preencher candidaturas com base nas suas informações, revisar cada resposta e confirmar cada ação antes do envio."

## 3. Problema

| # | Problema | Evidência hoje | Como validamos |
|---|---|---|---|
| P1 | A pessoa preenche repetidamente dados parecidos em sites de vagas diferentes. | Hipótese do time; observado no protótipo (formulários de currículo com 40+ campos). | Pesquisa v2, dor "Preencher os mesmos dados" |
| P2 | Currículos e perfis em plataformas diferentes ficam incompletos, desatualizados ou inconsistentes. | Hipótese | Pesquisa v2, dor "Manter currículo e perfis atualizados" |
| P3 | Campos abertos exigem bons textos, e a pessoa não quer (nem deve) inventar. | Hipótese | Pesquisa v2, dor "Responder perguntas abertas" |
| P4 | Formulários e etapas são demorados e difíceis de revisar. | Hipótese | Pesquisa v2, tempo por candidatura e etapas que mais tomam tempo |
| P5 | É difícil saber se o perfil atende ao que a vaga pede. | Hipótese | Pesquisa v2, dor "Saber se tenho o perfil" |
| P6 | Depois de várias candidaturas é difícil lembrar onde se candidatou. | Hipótese | Pesquisa v2. **Tratado no MVP 2.** |

As hipóteses só viram "validadas" segundo o critério do [plano de pesquisa](../05-pesquisa/plano-de-pesquisa.md#5-critérios-de-validação) (média ≥ 3,5, ≥ 50% de notas 4–5, ≥ 15 respostas). Nenhum resultado é assumido antes da pesquisa.

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

### 7.2 Fora do MVP 1

- Acompanhamento central de candidaturas, histórico completo, lembretes e gestão de etapas (**MVP 2**).
- Candidatura automática em massa ou envio sem aprovação (**nunca**).
- Suporte simultâneo a muitos portais (**MVP 2**, um portal por vez).
- Armazenamento de senhas ou controle autônomo da conta do candidato (**nunca**).
- Contorno de CAPTCHA, bloqueios, autenticação ou mecanismos de segurança (**nunca**).
- Promessas de contratação ou de compatibilidade garantida (**nunca**).
- Marketplace bilateral, matching, assinaturas, reputação e contratação PJ/CLT (**MVP 2**).
- Relatórios e automações adicionais (**MVP 2**).

### 7.3 Pontos a validar antes da implementação

| # | Ponto | Dono | Como decidir | Registro |
|---|---|---|---|---|
| V1 | Primeiro portal da integração real e quais permissões e regras se aplicam | Arquitetura + PO | Spike técnico em 2–3 candidatos (o documento de origem cita Maringá.Com, que **não** é decisão definitiva) e leitura dos termos de uso | [ADR-0011](../03-arquitetura/adr/0011-primeiro-portal-e-conectores-por-portal.md) |
| V2 | Campos do Perfil Mestre necessários no primeiro fluxo; tratamento de opcionais e sensíveis | PO + Arquitetura | Levantamento dos campos do portal escolhido mais pesquisa v2 | [Modelo de dados](../03-arquitetura/modelo-de-dados.md) |
| V3 | Só confirmação visual de conclusão ou registro mínimo | PO + Privacidade | Pesquisa e testes de usabilidade | [ADR-0012](../03-arquitetura/adr/0012-registro-minimo-de-conclusao-no-mvp1.md) |
| V4 | Formatos de currículo aceitos e como revisar a extração | PO + Dev back | PDF com texto e DOCX no início; PDF escaneado fora | [DRP RF-03](drp.md) |
| V5 | Métricas de validação | PM | Definidas **depois** da pesquisa com usuários | [Métricas](metricas.md) |

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
| O4 | Aprender o que priorizar no MVP 2 | Pesquisa v2 mais entrevistas pós-uso |

As metas numéricas ficam em [métricas](metricas.md) e só são fixadas depois da pesquisa v2.

## 11. Restrições e premissas

- **Restrições:** respeitar os termos de uso de cada portal; LGPD; a extensão roda só em Chrome e Edge (Manifest V3) no MVP 1; um portal suportado; o time é pequeno e o projeto tem calendário acadêmico.
- **Premissas:** a pessoa tem conta no portal e faz login nele por conta própria; o portal escolhido expõe formulários HTML legíveis (sem iframes de terceiros nem canvas); um modelo open-weight de 7–8B é suficiente para o de-para e os textos (provado no protótipo, com ressalvas).

## 12. Glossário

Ver [glossário](../00-contexto/glossario.md).
