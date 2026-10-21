# Métricas

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Métricas do MVP 1 |
| Versão | 2.0 |
| Status | Metas fixadas a partir da pesquisa v2 (n = 15). Revisar depois do teste de usabilidade e do beta. |
| Dono | Product Manager |
| Documentos relacionados | [DVP-E §10](dvp-e.md#10-objetivos-do-mvp-1), [Roadmap](roadmap.md), [Monetização](monetizacao.md), [Resultados da pesquisa v2](../05-pesquisa/resultados-v2.md), [Observabilidade (ADR-0015)](../03-arquitetura/adr/0015-observabilidade-sem-dados-pessoais.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | Métricas candidatas, forma de coleta sem dados pessoais e regra para fixar metas. |
| 2.0 | 21/10/2026 | Metas fixadas com base na pesquisa v2, cada uma com a justificativa. Novas métricas de onboarding (M11), planos (M12, M13, M16) e anúncios (M14, M15, M17), todas sem dados pessoais. Lista de métricas proibidas. |

## 1. Regra

A v1.0 dizia que nenhuma meta seria fixada antes da pesquisa. A pesquisa terminou (15 respostas válidas), e as metas abaixo saem dela. Duas ressalvas valem para todas:

- **A amostra é pequena e enviesada** (60% de tecnologia, familiaridade alta com formulários). Uma meta que não for atingida no beta é motivo para olhar o dado, não para mudar o número sem justificativa.
- **A pesquisa mede o que as pessoas dizem, não o que fazem.** Por isso as metas de uso real (tempo, campos aceitos, conclusão) serão confirmadas no teste moderado antes de virarem critério de saída do MVP 1.

## 2. North Star

**Candidaturas concluídas com assistência e revisadas por semana.**

Conta quando, numa mesma página, a pessoa (1) leu o formulário com o Zyft, (2) revisou e preencheu pelo menos um campo pelo painel e (3) confirmou a conclusão. Mede valor entregue sem incentivar volume sem revisão, porque exige revisão em cada candidatura. Risco: pode crescer com uso superficial; por isso anda junto com M2 e M9.

A North Star não é "candidaturas enviadas". O Zyft não envia nada, e contar envios premiaria o comportamento da antipersona.

## 3. Métricas de produto e metas

| # | Métrica | Meta | Por que este número | Como coletar | Objetivo |
|---|---|---|---|---|---|
| M1 | Tempo por candidatura | Mediana com o Zyft **≤ 50%** do tempo sem o Zyft, no mesmo formulário | 53% levam 15 minutos ou mais por candidatura (67% entre quem se candidata toda semana). A etapa mais demorada é preencher experiências e formação item por item (67%), que é justamente o que o de-para resolve. Cortar pela metade é o mínimo para a pessoa perceber diferença. | Teste moderado com e sem o Zyft. No produto: tempo entre "Ler formulário" e "Concluí" | O1 |
| M2 | Campos aceitos sem edição | **≥ 80%** dos campos Prontos | O Rafael quer "conferir e seguir". Se mais de 1 em cada 5 campos precisar de correção, conferir vira redigitar. | Contagem no painel: aprovados, editados, desmarcados | O2 |
| M3 | Campos reconhecidos | **≥ 90%** no portal de demonstração; linha de base no primeiro portal real | O portal demo é controlado; num portal real o número só faz sentido depois de medido. | Campos com proposta / campos visíveis não bloqueados, por versão do conector | O1, RNF-10 |
| M4 | Campos "Precisa de você" | **≤ 20%** dos campos para quem terminou o onboarding | Acima disso o perfil não está cumprindo o papel de fonte única. Com perguntas puladas, o número sobe, e isso é esperado (ver M11). | Contagem por estado no painel | O2 |
| M5 | Uso de sugestões de texto | **≥ 50%** das sugestões usadas, com ou sem edição | 60% acham a sugestão importante, mas 33% têm receio de respostas por IA. Metade usada indica que o rascunho ajuda sem ser imposto. | Sugestões aprovadas / sugestões geradas | O1 |
| M6 | Confiança depois do uso | Média **≥ 4,0** de 5 | Antes de usar, a confiança declarada foi 3,4 (meta da pesquisa era 3,5). O produto precisa ganhar confiança na prática; ficar no mesmo patamar significa que a revisão campo a campo não convenceu. | Pergunta opcional de 1 a 5 ao final do fluxo e no teste moderado | O2 |
| M7 | Valores objetivos escritos pela IA | **0** | Regra de produto (RN-02, RNF-05), não hipótese. | Evals no CI e amostra auditada | O3 |
| M8 | "Atende/parcial" com linha real | **100%** | Regra de produto (ADR-0013). | Evals | O3 |
| M9 | Conclusão do fluxo | **≥ 80%** das sessões que leram o formulário chegam a "Concluí" | Abaixo disso há abandono no meio do painel, o que contradiz a promessa de menos esforço. | Sessões com "Concluí" / sessões com leitura | O1 |
| M10 | Retorno em 30 dias | **≥ 40%** das pessoas que usaram a extensão usam de novo em outra candidatura | 60% da amostra não estava se candidatando no momento da pesquisa: a busca é intermitente. 40% é um corte conservador para dizer que o perfil virou a fonte de dados. | Pessoas com nova leitura em 30 dias | O4 |
| M11 | Onboarding | Mediana **≤ 10 minutos** até completar os capítulos 1 a 4; **≥ 50%** das perguntas puladas respondidas em 14 dias | Uma pergunta por tela só faz sentido se for rápido. O alerta de pendentes só funciona se as pessoas voltarem para responder. | Eventos de passo (`answered`, `skipped`, duração), sem os valores | O1 |

## 4. Métricas de monetização e anúncios

O pagamento é simulado no MVP 1 (RN-13). O que se mede é **intenção e comportamento**, não receita.

| # | Métrica | Meta ou alerta | Por quê | Como coletar |
|---|---|---|---|---|
| M12 | Pessoas que batem algum limite no mês | Alerta se **> 30%** das pessoas ativas no gratuito | O gratuito foi desenhado para quem faz de 1 a 2 candidaturas por semana. Se quase um terço bate o limite, o limite está baixo ou o público é outro (perfil Bia). | Contagem de respostas `402` por recurso, por pessoa pseudônima |
| M13 | Intenção de compra | **≥ 3%** das pessoas ativas clicam em "Testar o Plus" ou "Comprar pacote" no mês | O cenário base da [estimativa de receita](monetizacao.md#9-estimativa-de-receita-hipótese) supõe 4% de Plus. Abaixo de 3%, a disposição declarada (80% pagariam algo) não está virando comportamento. | Eventos `plan.changed` e `pack.purchased` |
| M14 | Ocultação de anúncios | Alerta se **> 20%** das exibições terminam em "Ocultar" | Ocultar muito indica anúncio irrelevante ou incômodo. Como privacidade é barreira para 40%, incômodo com anúncio custa confiança. | Contador agregado por anúncio e placement |
| M15 | Clique em anúncio | Linha de base; sem meta | Não é objetivo do produto maximizar clique. Serve para saber se o modelo de anúncio paga alguma coisa. | Contador agregado, sem `userId` (RNF-14) |
| M16 | Uso do recompensado | Linha de base | Diz se a saída para quem não pode pagar está sendo usada. | Eventos `rewarded.claimed` |
| M17 | Anúncio percebido como incômodo | **≤ 20%** dizem que o anúncio atrapalhou ou assustou | Testa RN-08 a RN-11 do ponto de vista da pessoa. | Pergunta no teste de usabilidade |

## 5. Métricas proibidas

| Métrica | Por que não |
|---|---|
| Candidaturas enviadas, vagas aplicadas, "candidaturas por minuto" | O Zyft não envia nada. Premiaria a antipersona e incentivaria volume sem revisão. |
| Porcentagem de compatibilidade ou nota por vaga | Proibido pela RN-06. |
| Taxa de contratação atribuída ao Zyft | Promessa que o produto não pode cumprir (RN-06), e a dor "sem retorno das empresas" não depende do Zyft. |
| Tempo de tela, sessões por dia ou impressões de anúncio como meta | Medem atenção, não valor. Incentivariam mais anúncio por tela, o que fere a RN-11. |
| Qualquer métrica que exija valor de campo, rótulo com dado pessoal, URL completa ou texto gerado | Proibido pela RNF-09 e pelo ADR-0015. |

## 6. Coleta sem dados pessoais

- Eventos de produto carregam só: identificador pseudônimo da conta, identificador da sessão do painel, versão do conector, contagens e durações. **Nunca** valores de campos, rótulos com dados pessoais, URL completa da vaga ou texto gerado.
- O domínio do portal é registrado; o caminho da URL, não. A deduplicação da candidatura assistida usa um hash de origem + caminho, que não volta a ser URL.
- Contadores de anúncio (exibição, clique, ocultação) são agregados por anúncio, placement e dia, sem identificador da pessoa. A preferência de ocultação é guardada na conta só para respeitar a escolha dela, nunca para métricas.
- A pergunta de confiança (M6) é opcional e não pede texto livre.
- A lista de campos permitidos nos eventos é a mesma dos logs técnicos ([ADR-0015](../03-arquitetura/adr/0015-observabilidade-sem-dados-pessoais.md)) e tem teste automatizado.
- O registro de ações visível para a pessoa (RF-20) é separado da telemetria.
- A pessoa pode desligar a telemetria de produto sem perder nenhuma funcionalidade.

## 7. Métricas da pesquisa

Os números que sustentam as metas (dores, confiança, interesse, disposição a pagar) estão em [resultados-v2.md](../05-pesquisa/resultados-v2.md). Eles definem as metas desta página, mas não se misturam com as métricas de uso.
