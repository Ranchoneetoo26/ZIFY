# DVS — Documento de Viabilidade de Software

## Controle do Documento

| Campo | Valor |
|---|---|
| Projeto | Zyft |
| Documento | Viabilidade de Software (DVS) |
| Versão | 10.0 |
| Status | Aprovado para desenvolvimento |
| Documentos relacionados | DVP-E, DRP, DAT, Arquitetura de Backend |

**Histórico de revisões**

| Versão | Alteração |
|---|---|
| 1.0 | Versão inicial — viabilidade técnica, operacional e econômica |
| 2.0 | Adição de estimativa de esforço por módulo, cronograma indicativo do MVP e critérios de decisão go/no-go |
| 3.0 | Inclusão do módulo de UX/Design System no esforço e no cronograma do MVP |
| 4.0 | Inclusão dos itens de robustez técnica e conformidade (RF-17 a RF-19, RNF-09 a RNF-12) no esforço, cronograma e riscos |
| 5.0 | Atualização de nome do projeto para Farol, sem impacto na análise de viabilidade |
| 6.0 | Atualização de nome do projeto para Lighthouse (anteriormente Farol), sem impacto na análise de viabilidade |
| 7.0 | Atualização de nome do projeto para Zyft (anteriormente Lighthouse/Farol), sem impacto na análise de viabilidade |
| 7.1 | Revisão final de consistência: adicionados os documentos Arquitetura de Frontend e Guia Mestre de Execução ao conjunto do projeto, referenciados em "Documentos relacionados" |
| 7.2 | Revisão técnica: corrigida referência residual a "plataforma PJ TI" para Zyft no objetivo (seção 1); referência ao Guia do Agente Supervisor |
| 7.3 | Risco de "cold start" (seção 5) agora desenvolvido em ação concreta no novo Plano de Aquisição e Lançamento; lacuna fiscal (obrigação de emitir nota fiscal sobre a receita de assinatura) endereçada no novo Aspectos Fiscais e de Faturamento |
| 7.4 | "Atendimento a disputas de avaliação" (seção 3) ligado a RF-21/RF-22, que passaram a formalizar tecnicamente esse processo operacional |
| 7.5 | Expansão de escopo para CLT: seção de estrutura de receita corrigida — sem comissão aplica-se às duas modalidades |
| 8.0 | **Alinhamento final de planos e acessos.** Revisão da viabilidade econômica com os novos valores de assinatura (mensal, semestral e anual) e impacto das novas regras de acesso no modelo de negócio. |
| 9.0 | **Adição do plano Dual (PJ + CLT).** Inclusão da nova trilha de receita e impacto na viabilidade econômica do modelo de marketplace bilateral. |
| 10.0 | **Otimização de Limites.** Reavaliação da viabilidade econômica considerando o aumento de limites e o potencial de conversão trial -> pago com uma oferta mais agressiva. |

## 1. Objetivo

Avaliar a viabilidade técnica, operacional e econômica do desenvolvimento do **Zyft** descrito no DVP-E e no DRP, antes do início da construção.

## 2. Viabilidade técnica

### 2.1 Complexidade e esforço indicativo por módulo

| Módulo | Complexidade | Esforço indicativo | Observação |
|---|---|---|---|
| Cadastro de empresa e profissional (RF-01, RF-02) | Baixa | Curto | CRUD padrão com validações |
| Publicação de projeto — Modo 1 (RF-03, RF-04) | Baixa/Média | Curto/Médio | CRUD + notificação de interesse |
| Banco de talentos + matching — Modo 2 (RF-05) | Média/Alta | Médio/Longo | Núcleo do diferencial técnico; requer lógica de matching e filtros |
| Avaliação bilateral (RF-06) | Média | Médio | Regras de imutabilidade, controle de acesso por assinatura, impacto no ranking |
| Validação de senioridade (RF-07) | Média | Médio | Regra de negócio cruzando múltiplas fontes de dado |
| Chat interno (RF-08) | Média | Médio | Pode usar solução de terceiros para reduzir esforço |
| Liberação de contato (RF-09) | Baixa | Curto | Fluxo de consentimento simples |
| Assinaturas e cobrança (RF-10, RF-11, RF-12, RF-24) | Média/Alta | Médio/Longo | Integração com gateway, planos, periodicidades, trial e regras de acesso cruzado |
| UX / Design System (RF-13 a RF-16, RNF-07, RNF-08) | Baixa/Média | Curto/Médio | Implementação da paleta amarelo/preto, tipografia e componentes-base (Guia de Estilo); transversal a todas as telas |
| Robustez técnica e conformidade (RF-17 a RF-23, RNF-09 a RNF-12) | Média | Médio | Onboarding retomável, termos, downgrade, LGPD (RF-20), moderação (RF-21), denúncia (RF-22) e suspensão (RF-23); maioria são camadas transversais ou fluxos de admin |

*Esforço indicativo em termos relativos (curto/médio/longo), a ser convertido em pontos/horas pela equipe de desenvolvimento na fase de planejamento.*

### 2.2 Cronograma indicativo do MVP (alto nível)

| Fase | Conteúdo | Depende de |
|---|---|---|
| 0. Design System | Definição e implementação da paleta, tipografia e componentes-base (RF-13 a RF-16, RNF-07, RNF-08) | — |
| 1. Fundação | Cadastro, autenticação, perfis, onboarding retomável e aceite de termos (RF-01, RF-02, RF-17, RF-18) | Fase 0 |
| 2. Núcleo de contratação | Publicação de projeto, interesse, banco de talentos e matching v1 (RF-03 a RF-05) | Fase 1 |
| 3. Confiança | Avaliação bilateral e validação de senioridade (RF-06, RF-07) | Fase 2 |
| 4. Monetização | Planos, trial, cobrança recorrente e downgrade (RF-10 a RF-12, RF-19) | Fase 1 (pode ser paralela às fases 2-3) |
| 5. Comunicação | Chat e liberação de contato (RF-08, RF-09) | Fase 2 |
| 6. Frontend completo | Todas as telas da Arquitetura de Frontend, integradas aos endpoints reais das fases 1-5 | Fases 1-5 |
| 7. Hardening (pós-build) | Rota de diagnóstico, rate limiting, varredura de segredos, fallback de integrações (RNF-09 a RNF-12) — executado via Comandos Técnicos Complementares e Workflow de Desenvolvimento, Motion e Observabilidade | **Somente após Fases 1-6 completas** (backend e frontend prontos) — nunca em paralelo, conforme pré-condição do Guia Mestre de Execução |
| 8. Homologação e lançamento | Testes integrados, ajustes de matching, deploy em produção | Fases 1-7 |

*Cronograma apresentado em fases lógicas, não em datas — prazos dependem do tamanho da equipe definida na fase de planejamento.*

### 2.3 Riscos técnicos

| Risco | Descrição | Mitigação |
|---|---|---|
| Qualidade do matching | Matching ruim compromete a proposta de valor central | Começar com regras determinísticas (filtros por senioridade/certificação/nota) antes de evoluir |
| Integração de cobrança recorrente | Múltiplas periodicidades e descontos aumentam a superfície de teste | Testes dedicados de ciclo de cobrança, trial e cancelamento |
| Imutabilidade de avaliações | Falha de desenho pode permitir edição indevida | Constraints a nível de banco de dados, não só de aplicação |
| Vazamento de segredos (ENV, chaves de API) | Exposição acidental compromete segurança de dados e integrações | Varredura de segredos no CI (RNF-11), segredos nunca versionados |
| Indisponibilidade de provedor externo (pagamento/e-mail) | Falha de terceiro derruba fluxo crítico do usuário | Camada de abstração com fallback e circuit breaker (RNF-12) |

## 3. Viabilidade operacional

- Equipe necessária mínima para o MVP: 1-2 desenvolvedores full-stack, 1 designer/UX, 1 responsável por produto.
- Processos operacionais novos exigidos: moderação de cadastros (evitar perfis falsos — ainda sem requisito funcional formal), atendimento a disputas de avaliação (suportado por RF-21/RF-22 desde a formalização da moderação de conteúdo), suporte a cobrança/assinatura.
- Dependência de terceiros: gateway de pagamento (assinaturas recorrentes), possivelmente provedor de chat/mensageria.

**Conclusão operacional:** viável com equipe enxuta para o MVP, desde que o escopo do DVP-E seja respeitado (sem comissão, sem escrow, sem folha de pagamento nesta fase).

## 4. Viabilidade econômica

### 4.1 Estrutura de receita
Receita 100% via assinatura (sem comissão sobre a contratação, em nenhuma modalidade — PJ ou CLT), cobrada das quatro trilhas (Empresa, PJ, CLT e Dual PJ & CLT), cada uma com 3 tiers (Iniciante, Intermediário, Profissional) × 3 periodicidades (ver tabela de valores atualizada no DRP, RF-10). Os valores foram otimizados para oferecer descontos progressivos de ~20% no semestral e ~35% no anual, incentivando a retenção a longo prazo.

### 4.2 Pontos de atenção
- O trial gratuito de 30 dias sem cartão de crédito reduz fricção de entrada, mas exige uma base mínima de conversão trial → pago para sustentar o modelo, já que não há receita de comissão como rede de segurança.
- Custos variáveis principais: infraestrutura (hospedagem, banco de dados), gateway de pagamento (taxas por transação recorrente), eventual custo de aquisição de usuários (marketing).

### 4.3 Conclusão econômica
O modelo é viável se a plataforma atingir massa crítica de usuários pagantes nos dois lados (efeito de rede: mais profissionais atraem mais empresas e vice-versa). O maior risco econômico não é de custo de desenvolvimento, mas de velocidade de aquisição de usuários dos dois lados simultaneamente (problema clássico de marketplace bilateral / "cold start").

## 5. Análise de riscos consolidada

| Risco | Impacto | Probabilidade | Mitigação |
|---|---|---|---|
| Cold start (poucos usuários dos dois lados) | Alto | Alta | Trial gratuito, foco inicial em nicho/região, aquisição pareada (ex.: trazer empresas parceiras antes do lançamento) |
| Matching de baixa qualidade | Alto | Média | Começar com regras simples e determinísticas, validar com usuários reais |
| Baixa conversão trial → pago | Alto | Média | Ajustar limites do tier Iniciante, acompanhar métricas de conversão desde o MVP |
| Fraude em cadastros/avaliações | Médio | Média | Moderação, verificação de identidade/CNPJ, trilha de auditoria |

## 6. Critérios de decisão (go / no-go)

| Critério | Sinal de continuidade (Go) | Sinal de alerta (No-Go / Pivot) |
|---|---|---|
| **Taxa de Conversão Trial → Pago** | ≥ 15% de conversão nos primeiros 6 meses de MVP | < 5% de conversão, indicando baixa percepção de valor dos planos |
| **Liquidez do Marketplace** | Média de 3 a 5 profissionais qualificados (nota > 4.0) por oportunidade publicada | < 1 profissional por vaga, indicando escassez de talento e risco de abandono das empresas |
| **Engajamento com Reputação** | ≥ 70% dos projetos/vagas concluídos com avaliação bilateral registrada | < 30% de avaliações, esvaziando o pilar de "Confiança" da plataforma |
| **Mix de Receita (Plano Dual)** | ≥ 10% da base de profissionais assinando a trilha Dual (PJ & CLT) | Concentração excessiva apenas no tier Iniciante CLT, comprometendo o ticket médio (ARPU) |
| **Qualidade do Matching** | ≥ 60% de taxa de aceite de candidatos sugeridos via Modo 2 (Banco de Talentos) | Alto volume de denúncias ou feedbacks de "matching irrelevante" (> 20%) |

## 7. Recomendação

A construção do MVP é **viável**, desde que:
1. O escopo seja mantido enxuto conforme o DVP-E (sem comissão, sem escrow, sem folha de pagamento nesta fase).
2. O motor de matching do banco de talentos comece simples (regras determinísticas) e evolua com dados reais de uso.
3. Haja acompanhamento próximo das métricas de conversão trial → pago desde o lançamento, dado que a receita depende inteiramente de assinatura.
4. Os critérios de decisão da seção 6 sejam revisados formalmente após os primeiros meses de operação do MVP.
