# ADR-0020 — Monetização com anúncios e planos no MVP 1

| Campo | Valor |
|---|---|
| Status | Proposta |
| Data | 21/10/2026 |
| Decisores | CEO, Product Owner, Arquiteto de software |
| Consultados | Product Manager, Responsável por privacidade, Designer UX (dev frontend), Dev backend |
| Requisitos | RF-26, RF-27, RF-28, RF-29; RN-07, RN-06, RN-08 a RN-13; RNF-09, RNF-14, RNF-15, RNF-16 |
| Relacionados | [ADR-0001](0001-reposicionamento-mvp1-candidatura-assistida.md), [ADR-0015](0015-observabilidade-sem-dados-pessoais.md), [ADR-0014](0014-dados-sensiveis-segregados-e-criptografados.md), [ADR-0017](0017-backend-em-csharp-dotnet-clean-architecture.md) |

## Contexto

### É uma decisão de escopo (RN-07)

Até aqui, a documentação dizia que o MVP 1 não teria cobrança: o [DVS §4.3](../../02-viabilidade/dvs.md) ("Sem cobrança no MVP 1"), os [termos de uso §7.1](../../07-legal/termos-de-uso.md) ("Durante o MVP 1 o Zyft é gratuito") e o rascunho da política de privacidade ("não usamos seus dados para publicidade"). A regra RN-07 diz que nada do MVP 2 entra no MVP 1 sem decisão explícita registrada. **Este ADR é essa decisão**: ele traz monetização para o MVP 1 e, se aceito, exige atualizar o DVS, os termos e a política no mesmo movimento.

Este ADR **não** antecipa o RF-M2-09. O RF-M2-09 é a assinatura fixa sem comissão do **marketplace** (profissionais e empresas, PJ/CLT), que continua no MVP 2. O que entra aqui é outra coisa: o plano do **assistente de candidatura** para a própria pessoa candidata. Nenhum item do marketplace muda de lugar.

Quem decide: CEO (estratégia e posicionamento), Product Owner (escopo do DVP-E e DRP) e arquiteto de software (impacto técnico e de dados), com parecer obrigatório do responsável por privacidade, porque anúncios tocam dados pessoais.

### Por que agora

- O produto precisa mostrar, para a banca e para quem investiria, que tem um caminho de receita compatível com as regras de confiança. Decidir isso depois do beta deixaria a dúvida em aberto e arriscaria um modelo que contradiz os princípios.
- A pesquisa v2 trouxe sinais de preço e de modelo de uso ([resultados](../../05-pesquisa/resultados-v2.md), n = 15):
  - preferência de uso: **gratuita com limite de uso por mês, 60%** (9 de 15); pacotes avulsos 13%; assinatura sem limite 13%; só se totalmente gratuita 13%;
  - disposição a pagar por mês: não pagaria 20%; **até R$ 10, 47%**; R$ 10 a 20, 13%; R$ 20 a 40, 13%; mais de R$ 40, 7%. Ou seja, 80% pagariam algum valor e a faixa mais comum é até R$ 10;
  - barreiras: **preço 47%**, **privacidade 40%**, funcionar em poucos sites 40%.
- Com tão poucas respostas, isso é sinal, não prova. Por isso o pagamento é **simulado** no MVP 1 (RN-13): a decisão testa o modelo e a interface sem cobrar ninguém.

## Fatores de decisão

- Compatível com as regras inegociáveis: nada de dado pessoal para anunciante, nada de anúncio onde a pessoa confere dados, nada de promessa (RN-06).
- O essencial precisa ser gratuito de verdade (preço é a barreira mais citada; 13% só usariam se fosse gratuito).
- Privacidade nunca vira recurso pago (40% citam privacidade como barreira).
- Coerente com a preferência por "gratuito com limite".
- Viável sem gateway de pagamento real no prazo da disciplina.
- Não abrir a porta para a antipersona (candidatura em massa como "recurso premium").

## Opções consideradas

### Opção A — Manter o MVP 1 sem monetização

- **Prós:** nenhum risco de privacidade novo; nenhuma mudança em termos e política; foco total no fluxo principal.
- **Contras:** deixa sem resposta a pergunta "como isso se paga?"; o custo de IA por candidatura cresce com o uso e não há nenhum mecanismo de contenção; adia uma decisão que muda telas, dados e textos legais.

### Opção B — Só assinatura (sem plano gratuito ou com teste grátis curto)

- **Prós:** modelo simples; sem anúncio; receita previsível.
- **Contras:** contraria a preferência majoritária (60% querem gratuito com limite) e a barreira principal (preço, 47%); exclui a Bia, que só usaria se fosse gratuito; aumenta o atrito justamente no momento de validar valor.

### Opção C — Só anúncios, sem plano pago

- **Prós:** tudo gratuito para a pessoa; nenhuma cobrança.
- **Contras:** receita por pessoa baixa num público pequeno; empurra para mais anúncios por tela e mais segmentação, o que entra em conflito com privacidade (40% de barreira) e com as telas de confiança; não atende quem prefere pagar para não ver anúncio.

### Opção D — Freemium: Gratuito com limites e anúncios rotulados, Plus sem anúncios e sem limites, pacote avulso e anúncio recompensado opcional

- Gratuito: limites mensais (10 candidaturas assistidas, 5 sugestões de IA, 10 análises de vaga, 2 importações; banco de respostas até 15; 1 navegador) e até um anúncio por tela, nunca nas telas de confiança.
- Plus: **R$ 9,90/mês ou R$ 99/ano** (R$ 8,25/mês), 7 dias grátis, sem anúncios, sem limites, até 3 navegadores.
- Pacote avulso: **R$ 4,90**, +15 candidaturas assistidas e +10 sugestões de IA, válido por 60 dias.
- Recompensado: +3 candidaturas assistidas por oferta vista, até 3 vezes no mês (+9), sempre opcional.
- **Prós:** segue a preferência majoritária; o preço do Plus fica na faixa mais citada (até R$ 10); o pacote atende os 13% que preferem pagar só quando usam; o recompensado dá saída para quem não quer ou não pode pagar; limites também contêm o custo de IA.
- **Contras:** é a opção com mais regras, telas e dados; anúncios exigem cuidado de privacidade e de política das lojas; limites podem frustrar quem está numa busca intensa (a Bia faz de 6 a 15 candidaturas por semana).

### Opção E — Comissão por contratação ou venda de dados a recrutadores

- **Prós:** receita potencialmente maior por pessoa.
- **Contras:** **rejeitada por princípio.** Comissão cria incentivo para empurrar candidaturas (contraria a antipersona e RN-06); venda ou compartilhamento de dados contraria a LGPD, a política de privacidade e a razão de existir do produto ("seus dados são seus").

## Decisão (proposta)

Adotar a **Opção D**, com as regras abaixo como parte da decisão.

1. **Planos e limites** (RF-26, RF-27, RF-28): os valores acima. Contagem mensal por mês civil. Uma candidatura assistida conta uma vez por (pessoa, origem + caminho da página) no mês, em `POST /forms/mappings`; sugestões de IA em `POST /forms/open-answers`; análises em `POST /jobs/analyze`; importações em `POST /resume-imports`. Ordem de consumo: limite do mês, depois bônus recompensado, depois pacotes (RNF-16).
2. **Ao exceder**: HTTP **402** com `code: "plan.limit_reached"` e `extensions: {resource, used, limit}`. Os clientes oferecem recompensado (se houver), pacote e Plus.
3. **Anúncios** (RF-29): `GET /ads?placement=<p>[&topic=<requisito>]` devolve um anúncio ou 204. Placements: `web.profile.rail`, `web.applications.feed`, `web.banner`, `ext.panel.banner`, `ext.job.contextual`, `ext.after-complete`. Seleção num catálogo de demonstração (cerca de 15 anúncios de "Anunciante de demonstração", marcados como tal), filtrado por `workArea`, cargos desejados e `topic`, excluindo os ocultados, com rotação simples.
4. **Regras de anúncio**, que fazem parte desta decisão e não são negociáveis por receita:
   - **RN-08:** nunca no onboarding, entrar ou criar conta, importar currículo, dados sensíveis, conta e privacidade, painel "Ler formulário", nem para Plus.
   - **RN-09:** nunca dado pessoal identificável nem sensível; só área, cargos desejados e o texto do requisito. Nada vai para o anunciante; cliques contados de forma agregada.
   - **RN-10:** sempre "Patrocinado", separado da interface, com "Ocultar" (anúncio por 30 dias ou tema para sempre) e "Por que estou vendo isso?".
   - **RN-11:** no máximo um por tela.
5. **Sempre de graça (RN-12):** revisão campo a campo, sensíveis trancados, nunca pedir senha, consentimentos, exportar e apagar os dados. Limite atingido nunca bloqueia editar o perfil, exportar ou excluir a conta.
6. **Pagamento simulado (RN-13):** `POST /billing/subscribe`, `/cancel` e `/packs` simulam o checkout. Nenhum gateway, nenhum dado de cartão. A interface diz, de forma discreta: "Ambiente de demonstração: nenhuma cobrança real". Integrar um gateway real exige um ADR novo.
7. **Auditoria:** `plan.changed`, `pack.purchased`, `ad.hidden`, `rewarded.claimed`, sem conteúdo pessoal.
8. **Telas de planos mostram só o que existe.** Itens do mockup de Planos que não estão no contrato (versões do perfil por tipo de vaga, lembretes de retorno, currículo em PDF, suporte com prazo de resposta) não podem ser prometidos no MVP 1; lembretes, por exemplo, são RF-M2-02.

## Consequências

**Positivas**
- O MVP 1 passa a ter um modelo de receita testável, coerente com a pesquisa e com as regras de confiança.
- Os limites contêm o custo de IA por pessoa no plano gratuito.
- A interface aprende a lidar com limite (402) antes de existir cobrança real.

**Negativas**
- Mais escopo no MVP 1: billing, uso, pacotes, recompensado, catálogo de anúncios, ocultação, contadores.
- Termos de uso, política de privacidade e DVS tiveram de ser revistos (feito nos rascunhos de 21/10/2026: termos e política 0.2, DVS 2.1); a revisão jurídica continua pendente.
- Mais superfície de dados (uso por mês, ocultações), ainda que sem conteúdo pessoal.

**Riscos e mitigação**
- **Política da Chrome Web Store.** A política de dados de usuário das lojas restringe o uso de dados para publicidade personalizada. O anúncio contextual na extensão usa o texto do requisito da vaga aberta e a área informada na plataforma. Mitigação: antes de publicar com anúncios, revisão de conformidade formal contra a política vigente; declarar anúncios na ficha da loja; se houver dúvida, desligar anúncios na extensão (`adsEnabled=false`) e manter só os da web. No MVP 1 acadêmico, a extensão é carregada em modo de desenvolvedor e os anunciantes são de demonstração.
- **Anúncio parecer recomendação do Zyft.** Mitigação: RN-10, selo fora do criativo, visual neutro, revisão de texto contra RN-06 e as palavras proibidas do [design system](../design-system.md).
- **Vazamento de dado pessoal pela seleção de anúncio.** Mitigação: a API lê área e cargos no servidor; o cliente só envia `placement` e `topic`; teste que falha se PII aparecer no payload, na resposta ou nos logs (RNF-14, TN-12).
- **Limite frustrar quem está numa busca intensa.** Mitigação: recompensado (+9), pacote de R$ 4,90 e contagem por página, não por leitura; medir a taxa de 402 por pessoa antes de mexer nos números.
- **Pagamento simulado ser confundido com cobrança real.** Mitigação: aviso discreto e permanente nas telas de plano; nenhum campo de cartão; termos atualizados.
- **Números com base fraca.** n = 15 e 60% de tecnologia. Mitigação: preços e limites ficam em configuração; revisar depois do beta e de uma nova rodada da pesquisa.

## Conformidade

- **TN-11:** `GET /ads` responde 204 para quem é Plus e para placements fora da lista; E2E confirma que nenhuma chamada a `/ads` acontece no onboarding, em entrar/criar conta, importar, sensíveis e conta.
- **TN-12:** teste de integração com perfil de valores-marcadores: nenhum marcador aparece no payload de `/ads`, na resposta, nos contadores de clique nem nos logs.
- **TN-14:** com todos os limites esgotados, `PUT /profile`, `GET /me/export` e `DELETE /me` continuam funcionando.
- **TN-15:** E2E da extensão: nenhum anúncio na revisão de campos ("Ler formulário").
- Teste de unidade da contagem: a mesma origem e caminho no mesmo mês contam uma vez; ordem de consumo limite → bônus → pacote (o que vence antes primeiro).
- Teste de integração: ao exceder, 402 com `code: plan.limit_reached` e `extensions.resource`, `used`, `limit`.
- Teste de integração: a quarta chamada a `POST /ads/rewarded/claim` no mês responde 409 `ads.rewarded_exhausted`.
- Revisão de privacidade obrigatória em PR que toque `/ads`, `/billing` ou o catálogo de anúncios.
- Antes de publicar na loja: checklist de conformidade com a política de dados de usuário da Chrome Web Store anexado ao PR de publicação.

## Links

- [Monetização](../../01-produto/monetizacao.md), [DRP RF-26 a RF-29 e RN-08 a RN-15](../../01-produto/drp.md), [Personas](../../01-produto/personas.md)
- [Resultados da pesquisa v2](../../05-pesquisa/resultados-v2.md), [Por que o Zyft](../../00-contexto/por-que-o-zyft.md)
- [Segurança](../seguranca.md), [Design system §6](../design-system.md), [Frontend web §5.10 e §6](../frontend-web.md), [Extensão §10 e §11](../extensao.md)
- Mockups `Planos.dc.html` e `Anuncios.dc.html` (design da v2)
