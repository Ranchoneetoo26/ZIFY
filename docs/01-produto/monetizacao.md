# Monetização: planos, limites e anúncios

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Monetização do MVP 1 (v2) |
| Versão | 1.2 |
| Status | Proposta, já implementada na demonstração (API, web e extensão). Depende do [ADR-0020](../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md). Pagamento simulado. |
| Dono | Product Owner (com Product Manager e CEO) |
| Documentos relacionados | [DRP RF-26 a RF-29, RN-08 a RN-15](drp.md), [Resultados da pesquisa v2](../05-pesquisa/resultados-v2.md), [Por que o Zyft](../00-contexto/por-que-o-zyft.md), [Fluxos §5](fluxos.md#5-planos-limites-e-anúncios), [Métricas](metricas.md), [Segurança](../03-arquitetura/seguranca.md), [DVS §4.3](../02-viabilidade/dvs.md#43-monetização) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 21/10/2026 | Primeira versão: planos Gratuito e Plus, pacote avulso, anúncio recompensado, formatos e regras de anúncio, estimativa de receita como hipótese. |
| 1.1 | 21/10/2026 | Ajustes ao que foi implementado: cancelamento imediato na demonstração, formato de `web.applications.feed` (card nativo), catálogo de 16 anúncios estáticos, 402 com `resource`/`used`/`limit` no topo e em `extensions`, recompensado recusado no Plus (`409 ads.rewarded_unavailable`), contextual só quando o requisito casa com um anúncio, onde o recompensado aparece na extensão. |
| 1.2 | 21/10/2026 | Pendência resolvida: "Desconectar extensão" no painel agora libera a vaga de navegador (`DELETE /extension/devices/current`). Comprar pacote passa a ser só pela plataforma web (a extensão recebe 403). Timeout de 1,5 s do anúncio (RNF-15) implementado na web e no painel. Registradas as pendências que continuam: "ocultar este tema" fora do painel e recompensado sem confirmação de exibição nem rate limit dedicado. |
| 1.3 | 22/10/2026 | Rede de anúncios Google AdSense **simulada** como preenchimento do inventário (§6.5, [ADR-0021](../03-arquitetura/adr/0021-rede-de-anuncios-google-adsense.md)): prioridade nos banners, preenchimento nos demais espaços sem anúncio direto, nunca no contextual, no Plus ou nos espaços proibidos. Novos riscos na §8. |

## 1. Resumo

O essencial do Zyft é gratuito para sempre, com limites mensais e anúncios rotulados. Quem está numa busca intensa pode assinar o **Plus** (R$ 9,90 por mês ou R$ 99 por ano), sem anúncios e sem limites, ou comprar um **pacote avulso** de R$ 4,90 quando estourar o limite num mês. Quem não quer pagar pode ganhar candidaturas extras vendo uma oferta patrocinada, se quiser.

Nada disso cobra de verdade no MVP 1: o pagamento é **simulado** e a interface diz isso (RN-13). O objetivo é testar se o modelo faz sentido para as pessoas, não faturar.

## 2. Por que monetizar já no MVP 1

Até a v1 da documentação, monetização era assunto para depois ([DVS §4.3](../02-viabilidade/dvs.md#43-monetização) e [termos de uso §7](../07-legal/termos-de-uso.md)). Três coisas mudaram:

1. **A pesquisa respondeu à pergunta de preço.** 60% preferem uma versão gratuita com limite; 80% pagariam algum valor; a faixa mais citada é "até R$ 10" (47%). Sem um modelo na mão, o time não aprende se essa disposição declarada vira comportamento.
2. **Preço é a maior barreira** (47%), à frente de privacidade (40%) e de "funcionar em poucos sites" (40%). Desenhar o gratuito junto com o produto evita descobrir tarde que o essencial ficou atrás de um pagamento.
3. **Limites mudam o desenho das telas.** Medidor de uso, mensagem de limite, espaço de anúncio e regras de onde nunca há anúncio afetam a extensão e a plataforma. Deixar para o MVP 2 significaria refazer telas.

Trazer monetização para o MVP 1 é uma decisão de escopo (RN-07). Ela está registrada no [ADR-0020](../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md), com as alternativas consideradas. As assinaturas do marketplace (RF-M2-09) continuam no MVP 2: são outro produto, outro público e outro modelo (assinatura de profissional sem comissão), e nada daquele desenho foi reaproveitado.

## 3. Planos

| | Gratuito | Plus | Pacote avulso |
|---|---|---|---|
| Preço | R$ 0, para sempre | R$ 9,90/mês ou R$ 99/ano (R$ 8,25/mês) | R$ 4,90, pagamento único |
| Teste | — | 7 dias grátis | — |
| Anúncios | Sim, no máximo 1 por tela, só onde é permitido | Nenhum | Não muda (segue o plano da pessoa) |
| Candidaturas assistidas | 10 por mês | Sem limite | +15 |
| Sugestões de texto com IA | 5 por mês | Sem limite | +10 |
| Análises de vaga | 10 por mês | Sem limite | — |
| Importações de currículo | 2 por mês | Sem limite | — |
| Banco de respostas | Até 15 respostas | Sem limite | — |
| Navegadores conectados | 1 | Até 3 | — |
| Validade | — | Até cancelar | 60 dias |
| Recompensado | +3 por oferta vista, até +9 no mês | Não se aplica | — |

### Sempre de graça, em qualquer plano (RN-12)

Revisar cada campo antes de preencher, dados sensíveis trancados, nunca pedir a senha dos portais, consentimentos, baixar e apagar os dados, editar o Perfil Mestre e o registro de ações. **Privacidade e confiança não são recurso pago.** Atingir um limite nunca impede a pessoa de editar o perfil, exportar ou excluir a conta.

### Cancelamento

Cancelar o Plus devolve a pessoa ao Gratuito no fim do período pago. **Na demonstração implementada, a volta é imediata**, porque não há período pago de verdade. Nada do perfil se perde. Se ela tiver mais respostas no banco do que o limite gratuito, as respostas continuam guardadas e utilizáveis; só não é possível criar novas até ficar abaixo de 15. Se tiver mais de um navegador conectado, os conectados continuam; um novo pareamento só é aceito dentro do limite.

**Liberar a vaga de navegador.** "Desconectar extensão" (em Ajustes, no painel) revoga o próprio navegador na API (`DELETE /extension/devices/current`) e libera a vaga na hora; no Gratuito, a pessoa pode conectar outro navegador em seguida sem receber o 402. Também dá para desconectar qualquer navegador pela plataforma, em **Extensão**. Até a primeira versão da v2, desconectar no painel só encerrava a sessão e a vaga continuava ocupada; isso foi corrigido.

## 4. Por que estes preços

| Decisão | Dado da pesquisa (n = 15) | Raciocínio |
|---|---|---|
| Existe um plano gratuito útil de verdade | 60% preferem "gratuita com limite"; 13% "só usaria se fosse totalmente gratuita"; preço é a barreira nº 1 (47%) | O gratuito precisa resolver a dor principal (redigitar) sem pagamento. 10 candidaturas assistidas por mês cobrem quem faz de 1 a 2 por semana, e o recompensado leva a 19. |
| Plus a R$ 9,90/mês | "Até R$ 10" é a faixa mais citada (7 de 15, 47%); 12 de 15 (80%) pagariam algum valor, e todos esses aceitam pelo menos essa faixa | Fica dentro da faixa que todos os pagantes declarados aceitam e abaixo do limiar psicológico de R$ 10. Cobrar R$ 14,90 alcançaria só 5 de 15 (33%) pelo que foi declarado. |
| Plano anual a R$ 99 | — | Equivale a R$ 8,25 por mês (dois meses grátis). Uma busca de emprego costuma durar meses; o anual premia quem já sabe que vai usar. |
| Pacote de R$ 4,90 | 13% preferem "pagar só quando usar" | Resolve o mês de busca intensa sem criar assinatura, que pesa para quem está desempregado. Custa metade do Plus e vale 60 dias, para não expirar no meio de um processo. |
| Teste de 7 dias | Confiança média de 3,4 de 5 | A pessoa experimenta antes de pagar. Sete dias bastam para algumas candidaturas reais. |
| Anúncio recompensado opcional | Mesma barreira de preço; perfil "Bia" (6–15 candidaturas por semana, só usaria se gratuito) | Dá uma saída para quem não pode pagar sem tornar o anúncio obrigatório. |

Importante: disposição declarada a pagar não é comportamento. A amostra é pequena, 60% de tecnologia e com experiência média alta. Os preços são ponto de partida para teste, não conclusão.

## 5. Contagem e consumo de uso

| Recurso | O que conta | Limite gratuito |
|---|---|---|
| Candidatura assistida | `POST /forms/mappings`, contada **uma vez** por pessoa e por página (origem + caminho) no mês. Ler o mesmo formulário de novo, ou a etapa seguinte na mesma página, não conta outra vez. | 10/mês |
| Sugestão de IA | Cada `POST /forms/open-answers` | 5/mês |
| Análise de vaga | Cada `POST /jobs/analyze` | 10/mês |
| Importação de currículo | Cada `POST /resume-imports` | 2/mês |

- **Mês civil** (`AAAA-MM`), virando no dia 1.
- **Ordem de consumo** (RNF-16): primeiro o limite do mês, depois o bônus do recompensado, depois os pacotes (o que vence antes primeiro).
- **Ao exceder**, a API responde `402` com `code: "plan.limit_reached"` e `resource`, `used` e `limit` (no topo da resposta e repetidos em `extensions`). O mesmo 402 vale para o banco de respostas (`answers`, ao criar a 16ª resposta) e para o pareamento (`devices`, ao conectar um segundo navegador no Gratuito). A extensão e a web mostram as opções: ver uma oferta patrocinada (se ainda houver no mês), comprar um pacote ou testar o Plus. A mensagem é calma, sem vermelho e sem urgência.
- **Pacote:** cobre só candidaturas assistidas e sugestões de IA. Análises de vaga, importações, respostas guardadas e navegadores dependem apenas do plano. A compra (simulada) acontece só na plataforma web: a extensão abre `#/planos` e a API recusa a compra com token da extensão (403), como faz com assinar e cancelar.
- **O que não conta:** editar o perfil, usar o Banco de respostas (o limite é de quantidade guardada, não de uso; reaprovar uma pergunta que já existe só substitui o texto), preencher (o preenchimento é a mesma candidatura do de-para), registrar a conclusão, consultar candidaturas e o registro de ações.

**Por que contar por página e não por chamada.** Um formulário de várias etapas pode exigir várias leituras. Contar cada leitura puniria quem revisa com cuidado, que é exatamente o comportamento que o Zyft quer. Limitação conhecida: portais que mudam o caminho da URL a cada etapa contam mais de uma vez. Isso será medido no teste de usabilidade (ver §8).

## 6. Anúncios

### 6.1 Formatos e lugares

| Placement | Onde | Formato |
|---|---|---|
| `web.profile.rail` | Coluna lateral do Perfil Mestre | Card nativo |
| `web.applications.feed` | Entre o resumo e a lista de Candidaturas | Card nativo |
| `web.banner` | Abaixo do título em Banco de respostas, Extensão, Detalhe da candidatura e Registro de ações | Banner horizontal |
| `ext.panel.banner` | Rodapé do painel ao conectar e em "A vaga e você" | Banner do painel |
| `ext.job.contextual` | Em "A vaga e você", ligado a um requisito que não aparece no perfil; substitui o banner. Só aparece se o texto do requisito casar com as palavras-chave de algum anúncio; senão, o painel mostra o banner | Card contextual |
| `ext.after-complete` | Depois de registrar a candidatura | Card "Próximo passo" |

O anúncio recompensado aparece junto do medidor de uso na extensão ("Ganhar +3") e na mensagem de limite. Na extensão, a oferta mostrada é um anúncio do espaço `ext.panel.banner`; não existe placement próprio para o recompensado.

### 6.2 Onde nunca há anúncio (RN-08)

Onboarding; entrar e criar conta; importar currículo; dados sensíveis; conta e privacidade; painel "Ler formulário" (revisão dos campos); e qualquer tela para quem é Plus.

**Por quê.** São as telas em que a pessoa entrega dados ou toma decisões sobre eles. Um anúncio ali competiria com a revisão, que é o centro da promessa do produto, e pareceria que os dados que ela está digitando alimentam a publicidade. O formulário é o lugar mais sensível de todos: a regra é que o anúncio só aparece **antes** de a pessoa começar ou **depois** de terminar.

### 6.3 Regras

| Regra | ID |
|---|---|
| No máximo um anúncio por tela | RN-11 |
| Sempre rotulado "Patrocinado", fora da moldura da interface, com "Ocultar" e "Por que estou vendo isso?" | RN-10 |
| "Ocultar" pergunta o escopo: este anúncio (por 30 dias) ou este tema (para sempre). **Pendência:** a API aceita os dois escopos, mas o painel da extensão só oferece ocultar o anúncio | RF-29 |
| Seleção só por área, cargos desejados e texto do requisito da vaga. Nunca nome, contato, localização exata, dados sensíveis ou histórico de candidaturas | RN-09 |
| Nada da pessoa vai para o anunciante. Clique é contado de forma agregada, sem identificar quem clicou | RN-09, RNF-14 |
| Anúncio carrega depois do conteúdo principal; se falhar ou demorar mais de 1,5 s, o espaço some (implementado na web, `web/js/ads.js`, e no painel, `fetchAd` em `sidepanel.js`) | RNF-15 |
| O conteúdo do anúncio segue as regras do produto: sem promessa de contratação (RN-06), sem "em massa", "com um clique" ou "piloto automático" | RN-06, antipersona |
| Anunciantes do MVP 1 são de demonstração, marcados como tal (`demo: true` na API). O catálogo tem 16 anúncios de várias áreas e fica no código, não no banco; o banco guarda só as ocultações de cada pessoa e contadores agregados de exibição e clique | ADR-0020 |

**"Por que estou vendo isso?"** responde em linguagem simples. Texto real da API, por exemplo: "Porque você escolheu a área Tecnologia no seu perfil. Usamos só a sua área, os cargos que você busca e o texto da vaga — nunca nome, contato ou dados sensíveis."

### 6.4 Anúncio recompensado

- Opcional, sempre iniciado pela pessoa.
- Cada oferta vista dá **+3 candidaturas assistidas** no mês, até 3 vezes (**+9**). A quarta tentativa responde `409 ads.rewarded_exhausted`. No Plus a oferta não se aplica: `409 ads.rewarded_unavailable`.
- Na demonstração, a API não confere se a oferta foi de fato vista: o clique em "Vi a oferta: ganhar +3" é a confirmação, e `POST /ads/rewarded/claim` não tem rate limit dedicado (só o teto de 3 por mês). **Pendência** antes de usar com anunciantes reais: a confirmação precisa vir do provedor de anúncios, e a rota precisa de limite próprio.
- Não existe recompensado para sugestões de IA: a IA tem custo de processamento maior e é o recurso que mais exige revisão.
- O bônus vale só no mês em que foi ganho.

### 6.5 Rede de anúncios (Google AdSense, simulada)

Proposta na [ADR-0021](../03-arquitetura/adr/0021-rede-de-anuncios-google-adsense.md). Sem anunciantes diretos, a maior parte do inventário ficaria vazia; a rede preenche esses espaços. **Nesta versão é uma simulação**: nada é carregado nem enviado ao Google.

| Espaço | Rede |
|---|---|
| `web.banner`, `ext.panel.banner` | Vem **antes** do anúncio direto (banner é o formato padrão de rede) |
| `web.profile.rail`, `web.applications.feed`, `ext.after-complete` | Só quando não há anúncio direto |
| `ext.job.contextual` | Nunca (usa o texto da vaga, que não sai do Zyft) |
| Espaços proibidos (RN-08) e plano Plus | Nunca |

- A API devolve `format: "network"` e os dados do bloco (`client`, `slot`, `adFormat`, `mock`); o cliente desenha o `<ins class="adsbygoogle">` com a arte "Anúncio do Google (simulado)".
- Selo "Patrocinado", "Por que estou vendo isso?" e "Ocultar" ficam fora do bloco (RN-10). Ocultar vale 30 dias para o bloco (`adsense-<slot>`); depois disso o espaço cai para o anúncio direto ou some.
- Anúncios não personalizados; nada do perfil vai para a rede (RN-09). Clique de rede não é contado pelo Zyft.
- **Antes de ativar de verdade:** ADR-0021 aceita, CMP de consentimento (LGPD), CSP liberando domínios do Google só na web, confirmação das políticas do AdSense. Na extensão, produção fica **só com anúncios diretos** (as políticas do AdSense não permitem anúncios dentro de software ou extensões; confirmar na política vigente).

## 7. Pagamento simulado

- Não há gateway de pagamento no MVP 1. "Assinar", "Testar 7 dias" e "Comprar pacote" mudam o plano na conta de demonstração e geram eventos de auditoria (`plan.changed`, `pack.purchased`).
- Nenhum dado de cartão é pedido ou guardado.
- A interface mostra, de forma discreta, "Ambiente de demonstração: nenhuma cobrança real" nas telas de plano e de confirmação (RN-13).
- Antes de cobrar de verdade será necessário: escolher um provedor de pagamento, atualizar termos de uso e política de privacidade, tratar nota fiscal e reembolso, e revisar o direito de arrependimento do Código de Defesa do Consumidor (7 dias para contratação a distância).

## 8. Riscos

| Risco | Efeito | Mitigação |
|---|---|---|
| Política de dados da Chrome Web Store e da loja do Edge | As lojas restringem o uso de dados de usuário para publicidade personalizada. Um anúncio contextual escolhido pelo texto da vaga lida na página pode ser interpretado como uso de dado coletado pela extensão. | Seleção feita na API com área e cargos do perfil e só o texto do requisito; nada da página é guardado; revisão de conformidade com a política da loja **antes** de publicar a extensão; se houver dúvida, desligar `ext.job.contextual` na versão da loja. |
| Política de privacidade desatualizada | O rascunho dizia "não usamos seus dados para publicidade". | Texto revisto para explicar a seleção contextual sem dados pessoais e sem compartilhamento com anunciantes. |
| Anúncio corrói a confiança | A confiança já está em 3,4 de 5 e privacidade é barreira para 40%. | RN-08 a RN-11; teste de usabilidade pergunta se o anúncio atrapalhou ou assustou; métrica de ocultação (ver [métricas](metricas.md)). |
| O limite gratuito não serve para quem se candidata muito | Quem faz de 6 a 15 candidaturas por semana (13% da amostra, perfil da Bia) estoura 10 por mês na primeira ou segunda semana. | Recompensado (+9) e pacote; acompanhar a taxa de pessoas que batem o limite e o que escolhem; rever o número se a maioria dos ativos estourar. |
| Contagem por página injusta em alguns portais | Portais que mudam a URL a cada etapa contam uma candidatura várias vezes. | Medir no teste; se acontecer, contar por sessão de painel ou por vaga analisada. |
| Disposição declarada não vira pagamento | Receita muito abaixo da estimativa. | Pagamento simulado no MVP 1 justamente para medir clique em "Assinar" e "Comprar" antes de cobrar. |
| Rede de anúncios (AdSense) em produção | Cookies de publicidade sem consentimento ferem a LGPD; as políticas do AdSense não permitem anúncios dentro de extensões; script do Google pesa na tela (RNF-15). | Rede **simulada** nesta versão; ativar só com ADR-0021 aceita, CMP e CSP revista; extensão só com anúncios diretos; carregar depois do conteúdo, com espaço reservado. |
| Pressão para cobrar por privacidade | Fere a promessa do produto. | RN-12 lista o que é sempre gratuito. |

## 9. Estimativa de receita (hipótese)

**Isto não é previsão.** A pesquisa não mede conversão, CPM nem comportamento real de compra. Os números abaixo servem para saber se o modelo tem ordem de grandeza razoável e o que precisa ser medido. Não incluem impostos, taxa de gateway, custo de IA nem inadimplência.

Suposições comuns aos três cenários:

- 1.000 pessoas ativas no mês.
- Assinantes Plus: 70% no mensal (R$ 9,90) e 30% no anual (R$ 8,25 por mês), o que dá **R$ 9,41 por assinante por mês**. O teste de 7 dias não gera receita.
- Pacote: cada comprador compra um pacote no mês.
- Anúncios só para quem está no gratuito, respeitando "no máximo um por tela".

| Suposição | Conservador | Base | Otimista |
|---|---|---|---|
| Conversão para Plus | 2% | 4% | 6% |
| Compram pacote no mês | 2% | 4% | 6% |
| Telas com anúncio vistas por pessoa no gratuito, por mês | 10 | 20 | 30 |
| CPM (R$ por mil exibições) | R$ 5 | R$ 10 | R$ 15 |
| Pessoas do gratuito que usam o recompensado | 20%, 1 vez | 30%, 2 vezes | 40%, 3 vezes |
| Receita por oferta recompensada vista | R$ 0,02 | R$ 0,04 | R$ 0,06 |

| Receita mensal | Conservador | Base | Otimista |
|---|---|---|---|
| Plus | 20 × R$ 9,41 = R$ 188 | 40 × R$ 9,41 = R$ 376 | 60 × R$ 9,41 = R$ 565 |
| Pacotes | 20 × R$ 4,90 = R$ 98 | 40 × R$ 4,90 = R$ 196 | 60 × R$ 4,90 = R$ 294 |
| Anúncios | 9.800 exibições = R$ 49 | 19.200 exibições = R$ 192 | 28.200 exibições = R$ 423 |
| Recompensado | 196 vistas = R$ 4 | 576 vistas = R$ 23 | 1.128 vistas = R$ 68 |
| **Total** | **R$ 339** | **R$ 787** | **R$ 1.349** |
| Por pessoa ativa | R$ 0,34 | R$ 0,79 | R$ 1,35 |

Leituras:

- Mesmo no cenário otimista, a assinatura é a maior fonte de receita. Os anúncios existem mais para financiar o gratuito do que para sustentar o negócio.
- Com 1.000 pessoas ativas, a receita é pequena para pagar um servidor de IA dedicado. A viabilidade depende de escala e do custo de inferência por candidatura, que ainda será medido ([DVS §4](../02-viabilidade/dvs.md)).
- O que precisa ser medido no beta: taxa de clique em "Assinar" e "Comprar pacote" mesmo com pagamento simulado, pessoas que batem o limite, uso do recompensado, ocultação de anúncios.

## 10. O que a tela de Planos não pode prometer

O mockup de Planos lista no Plus alguns recursos que **não fazem parte do MVP 1** e não estão no contrato da API: versões do perfil por tipo de vaga, lembretes de retorno, gerar currículo em PDF a partir do perfil e suporte com resposta em até um dia útil. Lembretes e acompanhamento ampliado são MVP 2 (RF-M2-02). Pela RN-07 e pela RN-06 (sem promessas), esses itens não aparecem como benefício do Plus até haver decisão registrada. Na interface, ou saem da lista ou aparecem como "Planejado", sem data.

O que o Plus entrega no MVP 1: sem anúncios, sem limites de uso e até 3 navegadores conectados.
