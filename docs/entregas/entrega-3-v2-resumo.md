# Entrega 3: resumo da versão 2 do Zyft

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida (MVP 1) |
| Documento | Resumo da v2 (complemento da Entrega 3) |
| Versão | 1.1 |
| Data | 21/10/2026 |
| Status | Complemento. Não substitui o documento já entregue da [Entrega 3](entrega-3-desenvolvimento-e-validacao.md) |
| Documentos relacionados | [Por que o Zyft](../00-contexto/por-que-o-zyft.md), [Resultados da pesquisa v2](../05-pesquisa/resultados-v2.md), [DRP](../01-produto/drp.md), [Monetização](../01-produto/monetizacao.md), [ADR-0018](../03-arquitetura/adr/0018-onboarding-em-passos-com-perguntas-adiaveis.md), [ADR-0019](../03-arquitetura/adr/0019-area-profissional-dinamica-e-campos-personalizados.md), [ADR-0020](../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 21/10/2026 | Primeira versão: o que mudou do MVP 1 entregue para a v2, por quê, e onde está cada coisa. |
| 1.1 | 21/10/2026 | Pendências atualizadas depois das correções pós-v2 (230 testes; desconexão da extensão, pacote só na web e timeout de anúncio resolvidos). |

## 1. Para que serve este documento

Os documentos da Entrega 2 e da Entrega 3 já foram enviados e ficam como estão. Depois deles, a pesquisa v2 fechou com 15 respostas válidas e o time revisou o produto. Este resumo conta, numa página, o que mudou, por que mudou e em que documento cada decisão está registrada. Ele é o ponto de partida para quem vai avaliar a v2.

## 2. O que a pesquisa mostrou

| Achado (n = 15) | Número | Leitura |
|---|---|---|
| Preencher os mesmos dados em cada site incomoda | média 4,40; 80% notas 4–5 | Dor central, validada. É o motivo do Zyft existir. |
| Manter currículo e perfis iguais | 3,60; 53% | Validada no limite. No recorte do plano (n = 12), cai para 42%. |
| Perguntas abertas | 3,27; 40% | Não validada como dor da maioria, mas 47% citam essa etapa entre as que mais tomam tempo. |
| Saber se tem o perfil da vaga | 2,33; 13% | Não é dor; comparar vaga e perfil, porém, é recurso valorizado (80%). |
| Não ter retorno das empresas | 4,40; 87% | A maior dor junto com redigitar, mas fora do que o Zyft resolve no MVP 1. |
| Confiança num assistente que não envia sem confirmação | 3,40 | Abaixo da meta de 3,5. Confiança precisa ser conquistada na interface. |
| Interesse | 3,73; 53% notas 4–5 | Moderado. |
| Recursos mais importantes | perfil único e importar currículo: 87% | Reforça Perfil Mestre e importação. |
| Barreiras | preço 47%, privacidade 40%, poucos sites 40% | Essencial gratuito; privacidade nunca é recurso pago. |
| Quanto pagaria por mês | não pagaria 3; até R$ 10: 7; R$ 10–20: 2; R$ 20–40: 2; mais de R$ 40: 1 | 80% pagariam algo; a faixa mais comum é até R$ 10. |
| Como prefere usar | gratuita com limite: 9 de 15 | Modelo freemium. |
| Área de quem respondeu | Tecnologia: 60% | Amostra enviesada. Outras áreas viram hipótese. |

A análise completa, com metodologia e limitações, está em [resultados-v2.md](../05-pesquisa/resultados-v2.md).

## 3. O que mudou no produto

| Mudança | Por quê | Requisitos | Decisão |
|---|---|---|---|
| Onboarding em passos ("passaporte"): cerca de 25 perguntas em 6 capítulos, uma por tela, todas com "Preencher mais tarde"; as puladas viram um alerta no Perfil Mestre | Montar o perfil de uma vez é a mesma digitação longa que a pessoa já odeia (redigitar 4,40). Uma pergunta por vez, sem obrigação, reduz o abandono e ainda deixa o perfil útil desde cedo. | RF-23, RN-14, RNF-17 | [ADR-0018](../03-arquitetura/adr/0018-onboarding-em-passos-com-perguntas-adiaveis.md) |
| Área profissional dinâmica (8 áreas) com sugestões de cargos, setores, ferramentas, competências, link e registro profissional | O produto é para qualquer área, mas o formulário e as sugestões eram de tecnologia. A pesquisa ficou concentrada em tecnologia, e por isso a área é configurável, não fixa. | RF-24 | [ADR-0019](../03-arquitetura/adr/0019-area-profissional-dinamica-e-campos-personalizados.md) |
| Campos personalizados ("Informações da sua área"), usados no de-para e nos fatos da IA | Cada profissão pede coisas que o Zyft não conhece (registro em conselho, disponibilidade para plantão). Recusam rótulos sensíveis. | RF-25, RN-15 | ADR-0019 |
| Planos: Gratuito com limites e anúncios, Plus a R$ 9,90/mês ou R$ 99/ano, pacote avulso a R$ 4,90, anúncio recompensado opcional | 60% preferem gratuito com limite; 47% aceitam até R$ 10; 13% preferem pagar por uso. Pagamento simulado nesta versão. | RF-26 a RF-28, RN-12, RN-13, RNF-16 | [ADR-0020](../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md) |
| Anúncios rotulados, no máximo 1 por tela, nunca no formulário nem em telas de privacidade, sem dados pessoais | Sustentar o gratuito sem trair a confiança (privacidade é barreira para 40%). | RF-29, RN-08 a RN-11, RNF-14, RNF-15 | ADR-0020 |
| Personas narrativas Rafael (primária) e Bia (secundária), mais Beatriz (Saúde) como hipótese | Os perfis A, B e D foram confirmados; as personas passaram a ter rosto e dados reais. | PN1–PN5 | [Personas](../01-produto/personas.md) |
| Design system próprio (Cuidador com traço de Sábio) | Confiança 3,4 e receio de IA (20% não confiam em IA escrevendo) pedem uma interface que explica e devolve a decisão. | RNF-06, RNF-11 | [Design system](../03-arquitetura/design-system.md) |

Trazer planos e anúncios para o MVP 1 é uma decisão de escopo sob a regra RN-07. Ela está registrada no ADR-0020, com as alternativas consideradas, e ainda depende de aprovação humana (status Proposta).

## 4. O que não mudou

As regras inegociáveis continuam iguais: o Zyft nunca envia a candidatura, a IA só aponta a chave do perfil e escreve só com fatos aprovados, nenhuma senha é lida, sensíveis ficam trancados e fora da IA, logs não têm dados pessoais e não há nota, porcentagem ou promessa de contratação. A arquitetura também é a mesma da Entrega 3 (.NET 8 em Clean Architecture, web sem build, extensão MV3).

## 5. Onde está cada coisa

| Assunto | Documento |
|---|---|
| A história e o porquê | [Por que o Zyft](../00-contexto/por-que-o-zyft.md) |
| Pesquisa completa | [Resultados v2](../05-pesquisa/resultados-v2.md) |
| Requisitos novos | [DRP](../01-produto/drp.md) (RF-23 a RF-29, RN-08 a RN-15, RNF-14 a RNF-17) |
| Critérios de aceite | [CA-23 a CA-29](../04-qualidade/criterios-de-aceite-mvp1.md) |
| Planos, preços e anúncios | [Monetização](../01-produto/monetizacao.md) |
| Endpoints novos | [API](../03-arquitetura/api.md) |
| Dados novos | [Modelo de dados](../03-arquitetura/modelo-de-dados.md) |
| Segurança de anúncios e pagamento simulado | [Segurança](../03-arquitetura/seguranca.md) |
| Telas e componentes | [Frontend web](../03-arquitetura/frontend-web.md), [Extensão](../03-arquitetura/extensao.md), [Design system](../03-arquitetura/design-system.md) |
| Como usar, passo a passo | [Guia do usuário](../06-operacao/guia-do-usuario.md) |
| Como rodar | [Runbook local](../06-operacao/runbook-local.md) |
| Preparação para a banca | [Guias de estudo](../08-time/guias-de-estudo/README.md) |

## 6. Pendências que a banca pode perguntar

| Pendência | Situação |
|---|---|
| ADRs 0017, 0018, 0019 e 0020 (e outros, como 0007, 0008, 0011, 0012 e 0015) | Proposta. Precisam de aprovação humana (CEO, PO e arquiteto no caso do 0020). O 0017 descreve a stack .NET já implementada. |
| Amostra da pesquisa | 15 respostas, 60% de tecnologia, 60% sem candidatura no momento. As conclusões são pistas fortes só para "redigitar dados". Entrevistas e uma segunda rodada fora de TI estão no plano. |
| Persona de Saúde (Beatriz) | Hipótese: só 1 resposta de Saúde. Será validada com entrevistas e testes. |
| Pagamento | Simulado. Nenhum gateway real, nenhum dado de cartão. |
| Anúncios na extensão | Antes de publicar na Chrome Web Store, revisar a conformidade com a política de dados de usuário da loja. O painel ainda não oferece "ocultar este tema" (só a web oferece). |
| Anúncio recompensado | O bônus é concedido sem confirmação de exibição pelo servidor e `POST /ads/rewarded/claim` não tem rate limit dedicado. Aceitável sem receita real; rever antes de anúncios reais. |
| Evals de IA | Descritos na [estratégia de testes](../04-qualidade/estrategia-de-testes.md) e em [IA §7](../03-arquitetura/ia.md), mas ainda não existem no código. |
| Resolvidas depois da primeira versão da v2 | "Desconectar extensão" libera a vaga de navegador (`DELETE /extension/devices/current`); comprar pacote só com token web; timeout de 1,5 s no anúncio (RNF-15); TN-14 e logs das rotas v2 com teste; regras de LinkedIn e GitHub corrigidas. Total: 230 testes passando. Depois, as correções da gravação do tour (importação com item incompleto, certificações, erros legíveis, teste grátis e rascunho vazio) levaram a 252 (156 + 84 + 12). |
| Registro de candidaturas | O ADR-0012 (Proposta) recomendava um registro sem status; a implementação guarda status e anotações. Falta a decisão humana que alinhe os dois. |
| Itens do mockup de Planos fora do MVP 1 | Versões do perfil por tipo de vaga, lembretes de retorno e currículo em PDF são MVP 2 e não podem ser vendidos como parte do Plus agora (RN-07). |
