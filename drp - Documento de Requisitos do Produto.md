# DRP — Documento de Requisitos do Produto

## Controle do Documento

| Campo | Valor |
|---|---|
| Projeto | Zyft |
| Documento | Requisitos do Produto (DRP) |
| Versão | 12.0 |
| Status | Aprovado para desenvolvimento |
| Documentos relacionados | DVP-E, DVS, DAT, Arquitetura de Backend, Termos de Uso e Privacidade |

**Histórico de revisões**

| Versão | Alteração |
|---|---|
| 1.0 | Versão inicial dos requisitos funcionais e não funcionais |
| 2.0 | Adição de atores, priorização MoSCoW, critérios de aceite e matriz de rastreabilidade com o DVP-E |
| 3.0 | Adição de requisitos de UX (RF-13 a RF-16) e de design system (RNF-07, RNF-08), com referência ao Guia de Estilo e Design System |
| 4.0 | Adição de requisitos de robustez técnica e conformidade (RF-17 a RF-19, RNF-09 a RNF-12), formalizando os itens do documento Comandos Técnicos Complementares |
| 5.0 | Atualização de nome do projeto para Farol, sem impacto nos requisitos já definidos |
| 6.0 | Atualização de nome do projeto para Lighthouse (anteriormente Farol), sem impacto nos requisitos já definidos |
| 7.0 | Atualização de nome do projeto para Zyft (anteriormente Lighthouse/Farol), sem impacto nos requisitos já definidos |
| 7.1 | Revisão final de consistência: adicionados os documentos Arquitetura de Frontend e Guia Mestre de Execução ao conjunto do projeto, referenciados em "Documentos relacionados" |
| 7.2 | Revisão técnica: seções 5 e 6 reordenadas para bater com a ordem numérica dos RF (UX antes de Robustez); RF-19 removido do mapeamento duplicado na matriz; RNF-02 e RNF-06 com métricas mensuráveis; tags inline de pilar removidas (fonte única passa a ser a matriz da seção 7); referência ao Guia do Agente Supervisor |
| 7.3 | Adicionado RF-20 (exportação e exclusão de dados pessoais, LGPD), identificado como lacuna ao escrever o RIPD; matriz de rastreabilidade atualizada; referências ao RIPD, Termos de Uso e Aspectos Fiscais |
| 7.4 | Adicionado RF-21 (moderação de conteúdo), resolvendo contradição entre os Termos de Uso (que prometiam remoção de avaliação por moderação) e RNF-01 (que bloqueava qualquer edição/exclusão de avaliação); RNF-01 reescrito para distinguir "conteúdo imutável" de "status de moderação", que não é uma edição |
| 7.5 | RF-21 detalhado com os 3 endpoints reais (avaliação, mensagem, projeto) — a v7.4 prometia moderar os três tipos de conteúdo, mas só o de avaliação estava implementado nos demais documentos |
| 7.6 | Adicionado RF-22 (denúncia de conteúdo) — sem ele, RF-21 não tinha como ser acionado na prática, já que nada levava conteúdo à atenção do admin |
| 7.7 | Adicionado RF-23 (suspensão de conta), formalizando a cláusula de suspensão já prometida nos Termos de Uso (Parte 1, item 7) que não tinha mecanismo técnico correspondente; RF-22 estendido para cobrir denúncia de perfil, não só de conteúdo |
| 8.0 | **Expansão de escopo: requisitos passam a cobrir CLT, não só PJ.** RF-01/02/03/05/06/10/12/13 reescritos para tratar "modalidade de contratação" (PJ/CLT) como campo explícito, ortogonal ao modo de contratação (projeto aberto/banco de talentos); atores atualizados (Admin adicionado); matriz de rastreabilidade ajustada |
| 8.1 | Corrigidas menções residuais a "profissional PJ" para "profissional" em RNF-05, RF-17, RF-22, RF-23; DVP-E também corrigido |
| 9.0 | **Revisão completa de monetização e acesso, a pedido do usuário.** RF-10 reescrito: planos agora são 3 trilhas por tipo de usuário/modalidade (Empresa, PJ, CLT), cada uma com 3 tiers renomeados (Iniciante/Intermediário/Profissional), com preços definidos pelo usuário para o mensal e semestral/anual calculados por fórmula (20%/35% de desconto). Adicionado RF-24, formalizando a regra de acesso: profissional usa um único perfil para PJ/CLT, mas só pode se candidatar à modalidade da trilha assinada; visibilidade da modalidade oposta (sem poder se candidatar) é exclusiva do tier Profissional |
| 10.0 | **Alinhamento final de planos e acessos.** RF-10 revisado para confirmar valores semestrais e anuais com descontos otimizados. RF-24 reescrito para garantir exclusividade mútua total entre perfis (Empresa, PJ, CLT) e permitir que cadastros CLT visualizem projetos PJ sem candidatura em todos os tiers. |
| 11.0 | **Adição do plano Dual (PJ + CLT).** Introdução da trilha "PJ & CLT" para profissionais que desejam atuar nas duas modalidades simultaneamente, com candidatura liberada para ambos. |
| 12.0 | **Otimização de Limites e Vantagem Competitiva.** Aumento expressivo do número de candidaturas para profissionais e oportunidades/buscas para empresas. Definição dos valores para todos os tiers do plano Dual. |

## 1. Atores do sistema

| Ator | Descrição |
|---|---|
| Empresa | Usuário pagante que publica oportunidades (PJ ou CLT) e/ou consulta o banco de talentos |
| Profissional | Usuário pagante que busca oportunidades na(s) modalidade(s) que aceita (PJ, CLT, ou ambas) e é elegível ao banco de talentos |
| Admin | Usuário interno (não autocadastro), responsável por moderação, denúncias e suspensão de conta (RF-21, RF-22, RF-23) |
| Sistema (motor de matching) | Componente automatizado responsável por sugerir compatibilidade entre oportunidade/pedido e profissionais, respeitando a modalidade |

## 2. Priorização (MoSCoW)

| Prioridade | Significado |
|---|---|
| Must | Obrigatório para o MVP — produto não funciona sem isso |
| Should | Importante, mas o MVP pode ir ao ar com limitação temporária |
| Could | Desejável, pode ficar para uma iteração seguinte |
| Won't (nesta versão) | Fora do escopo do MVP, mapeado para o futuro |

## 3. Requisitos funcionais

*O mapeamento de cada requisito ao pilar de diferencial competitivo que ele implementa está centralizado na Matriz de rastreabilidade (seção 7) — não repetido requisito a requisito, para evitar divergência entre as duas fontes.*

### Cadastro e perfil

**RF-01 — Cadastro de empresa** · *Prioridade: Must*
- Campos obrigatórios: nome da empresa, cidade, tipo(s) de trabalho que oferece.
- Empresa pode editar seus dados a qualquer momento.
- **Critério de aceite:** empresa não consegue publicar oportunidade (projeto PJ ou vaga CLT) nem consultar banco de talentos sem completar os campos obrigatórios.

**RF-02 — Cadastro de profissional** · *Prioridade: Must*
- Campos obrigatórios: dados pessoais, experiências de trabalho, cursos, diplomas, grau de experiência declarado (júnior, pleno, sênior), **modalidade(s) de contratação aceita(s)** (PJ, CLT, ou ambas).
- Ao concluir o cadastro, o profissional é automaticamente incluído no banco de talentos, elegível para oportunidades nas modalidades que aceita.
- **Critério de aceite:** ao salvar cadastro completo, o profissional aparece em consultas de banco de talentos compatíveis com sua(s) modalidade(s), sem ação manual adicional; profissional que só aceita PJ nunca aparece em busca filtrada por CLT, e vice-versa.

### Publicação e busca de oportunidades

**RF-03 — Publicação de oportunidade (Modo 1 — Projeto aberto)** · *Prioridade: Must*
- Empresa publica uma oportunidade escolhendo a **modalidade de contratação**: **PJ** (projeto — horas necessárias, grau de experiência exigido, descrição) ou **CLT** (vaga — faixa salarial, grau de experiência exigido, descrição). A modalidade é obrigatória e definida no momento da publicação.
- Oportunidade fica visível para profissionais compatíveis com o grau de experiência exigido e que aceitam a modalidade publicada.
- **Critério de aceite:** oportunidade publicada respeita o limite do plano ativo da empresa (ver RF-10), somando PJ e CLT no mesmo limite; ao exceder o limite, publicação é bloqueada com aviso de upgrade de plano. Uma oportunidade CLT nunca aparece para um profissional que só aceita PJ, e vice-versa.

**RF-04 — Demonstração de interesse** · *Prioridade: Must*
- Profissional interessado clica em botão de interesse.
- Sistema envia alerta ao contratante com as informações do profissional.
- **Critério de aceite:** empresa recebe notificação em até poucos minutos do clique (assíncrono aceitável via fila/e-mail).

**RF-05 — Banco de talentos (Modo 2)** · *Prioridade: Must*
- Empresa envia pedido especificando o perfil desejado (grau de experiência, especialidade, **modalidade de contratação desejada — PJ ou CLT**, etc.).
- Sistema retorna lista de profissionais compatíveis automaticamente (matching), filtrando por quem aceita a modalidade pedida, sem depender de curadoria manual.
- **Critério de aceite:** resultado do matching retorna sem intervenção humana, respeita o limite de consultas do plano ativo da empresa, e nunca inclui profissional que não aceita a modalidade solicitada.

### Reputação e validação

**RF-06 — Avaliação bilateral** · *Prioridade: Must*
- Para oportunidades PJ: ao final do projeto (status `concluido`), empresa avalia profissional e profissional avalia empresa (nota de 1 a 5 estrelas + comentário).
- Para oportunidades CLT: a avaliação é habilitada após a contratação ser confirmada (status `concluido`, reinterpretado para CLT como "processo seletivo concluído com contratação" — ver Diagrama ER, seção 3.9). Como não há uma "entrega" que marque o fim como em um projeto PJ, a avaliação é liberada uma única vez, não é recorrente durante o vínculo empregatício em andamento.
- Avaliações não podem ser editadas ou excluídas após publicadas, em nenhuma modalidade.
- Nota mais alta aumenta a prioridade em buscas e no matching do banco de talentos, independente da modalidade da oportunidade que a originou.
- Avaliações (nota + comentário) visíveis apenas para usuários com assinatura ativa.
- **Critério de aceite:** não existe endpoint nem ação de UI que permita editar/excluir avaliação publicada; usuário sem assinatura ativa não visualiza nota nem comentário de terceiros; avaliação de vaga CLT só fica disponível após `status = concluido`, nunca antes.

**RF-07 — Validação de grau de experiência** · *Prioridade: Must*
- Sistema cruza: anos de experiência declarados + certificações cadastradas + avaliações recebidas, para compor o grau de experiência validado do profissional.
- **Critério de aceite:** grau de experiência exibido no perfil é sempre o valor calculado pelo sistema, não um campo livre editável pelo profissional.

### Comunicação

**RF-08 — Chat interno** · *Prioridade: Must*
- Empresa e profissional podem se comunicar via chat interno da plataforma.
- **Critério de aceite:** histórico de mensagens é persistido e recuperável pelas duas partes.

**RF-09 — Liberação de contato** · *Prioridade: Should*
- Ambas as partes podem, de forma manual e consensual, liberar dados de contato (e-mail/WhatsApp) um para o outro.
- **Critério de aceite:** dado de contato só é exibido após confirmação registrada das duas partes; liberação de apenas um lado não expõe o dado.

### Monetização e assinaturas

**RF-10 — Planos de assinatura** · *Prioridade: Must*
- 3 trilhas de plano por **tipo de usuário e modalidade** — Empresa, PJ e CLT são precificadas separadamente, cada uma com 3 tiers (Iniciante, Intermediário, Profissional) × 3 periodicidades (mensal, semestral, anual).
- O profissional usa **um único perfil de cadastro** para PJ e CLT (não são contas separadas) — qual modalidade ele pode efetivamente usar (candidatar-se a oportunidades) depende de qual trilha de plano (PJ ou CLT) ele assinou. Trocar de trilha (assinar a outra) muda a modalidade nativa da conta.
- Desconto de ~20% no plano semestral e ~35% no anual sobre o valor mensal, em todas as trilhas e tiers (fórmula: `preço_periodicidade = preço_mensal × (1 − desconto)`, arredondado à casa dos centavos).

**Empresa**

| Tier | Mensal | Semestral (mês) | Semestral (total 6m) | Anual (mês) | Anual (total 12m) |
|---|---|---|---|---|---|
| Iniciante | R$ 59,90 | R$ 47,92 | R$ 287,52 | R$ 38,94 | R$ 467,28 |
| Intermediário | R$ 99,99 | R$ 79,99 | R$ 479,94 | R$ 64,99 | R$ 779,88 |
| Profissional | R$ 129,99 | R$ 103,99 | R$ 623,94 | R$ 84,49 | R$ 1.013,88 |

**PJ**

| Tier | Mensal | Semestral (mês) | Semestral (total 6m) | Anual (mês) | Anual (total 12m) |
|---|---|---|---|---|---|
| Iniciante | R$ 39,99 | R$ 31,99 | R$ 191,94 | R$ 25,99 | R$ 311,88 |
| Intermediário | R$ 59,99 | R$ 47,99 | R$ 287,94 | R$ 38,99 | R$ 467,88 |
| Profissional | R$ 89,99 | R$ 71,99 | R$ 431,94 | R$ 58,49 | R$ 701,88 |

**CLT**

| Tier | Mensal | Semestral (mês) | Semestral (total 6m) | Anual (mês) | Anual (total 12m) |
|---|---|---|---|---|---|
| Iniciante | R$ 19,99 | R$ 15,99 | R$ 95,94 | R$ 12,99 | R$ 155,88 |
| Intermediário | R$ 49,99 | R$ 39,99 | R$ 239,94 | R$ 32,49 | R$ 389,88 |
| Profissional | R$ 69,99 | R$ 55,99 | R$ 335,94 | R$ 45,49 | R$ 545,88 |

**PJ & CLT (Dual)**

| Tier | Mensal | Semestral (mês) | Semestral (total 6m) | Anual (mês) | Anual (total 12m) |
|---|---|---|---|---|---|
| Iniciante | R$ 144,99 | R$ 115,99 | R$ 695,94 | R$ 94,24 | R$ 1.130,88 |
| Intermediário | R$ 199,99 | R$ 159,99 | R$ 959,94 | R$ 129,99 | R$ 1.559,88 |
| Profissional | R$ 249,99 | R$ 199,99 | R$ 1.199,94 | R$ 162,49 | R$ 1.949,88 |

- **Limites por Tier (Empresa):**
  - **Iniciante:** 5 oportunidades ativas (PJ ou CLT) + 50 visualizações de perfis no banco/mês.
  - **Intermediário:** 15 oportunidades ativas + 200 visualizações de perfis no banco/mês.
  - **Profissional:** Ilimitado (Oportunidades e Banco).

- **Limites por Tier (Profissional - PJ, CLT ou Dual):**
  - **Iniciante:** 20 candidaturas/mês.
  - **Intermediário:** 60 candidaturas/mês.
  - **Profissional:** Ilimitado.

- **Critério de aceite:** valor cobrado corresponde exatamente à tabela da trilha (Empresa/PJ/CLT/Dual) e periodicidade escolhidas; profissional só é cobrado pela trilha da assinatura que efetivamente escolheu.

**RF-11 — Trial gratuito** · *Prioridade: Must*
- 30 dias grátis, disponível apenas no tier Iniciante (qualquer trilha — Empresa, PJ, CLT ou Dual), sem necessidade de cartão de crédito.
- Após o trial, pagamento obrigatório para continuar utilizando a plataforma.
- **Critério de aceite:** usuário em trial não é solicitado a inserir cartão; ao final dos 30 dias, acesso a funcionalidades pagas é bloqueado até confirmação de pagamento.

**RF-12 — Sem comissão sobre a contratação** · *Prioridade: Must*
- Nesta versão, não há cobrança de comissão sobre projetos PJ fechados, nem taxa de colocação sobre vagas CLT preenchidas — em nenhuma das duas modalidades, em nenhum tier. Receita vem exclusivamente das assinaturas.
- Requisito de arquitetura: manter extensibilidade para uma futura comissão/taxa (feature flag ou módulo separado, possivelmente diferenciado por modalidade), sem implementá-la agora.
- **Critério de aceite:** nenhum fluxo de fechamento de projeto PJ ou confirmação de contratação CLT solicita ou desconta valor de comissão/taxa, em nenhum tier.

**RF-24 — Regras de acesso e exclusividade por tipo de usuário** · *Prioridade: Must*
- **Exclusividade Mútua Total:** Empresa, PJ, CLT e PJ & CLT possuem conjuntos de permissões e acessos rigorosamente separados. Um usuário logado como Empresa não pode acessar funcionalidades de profissional; um profissional acessa apenas o fluxo da sua trilha assinada.
- **Modalidade Nativa:** Por padrão, um profissional só pode se candidatar a oportunidades da modalidade nativa da sua assinatura ativa (PJ ou CLT, conforme a trilha assinada).
- **Trilha Dual (PJ & CLT):** Profissionais nesta trilha podem **visualizar e se candidatar** a oportunidades de ambas as modalidades (PJ e CLT) simultaneamente.
- **Acesso CLT a Projetos PJ:** Todos os usuários da trilha **CLT** (independente do tier) podem **visualizar** projetos da modalidade PJ, mas **não podem se candidatar** a eles.
- **Visibilidade Cruzada Profissional (PJ):** Profissionais da trilha **PJ** no tier **Profissional** podem visualizar oportunidades CLT, mas sem poder se candidatar.
- **Critério de aceite:** `POST /interesse` (RF-04) liberado para ambas as modalidades apenas na trilha PJ & CLT; usuários CLT visualizam projetos PJ sem permissão de escrita; usuários PJ só visualizam CLT no tier Profissional; empresas nunca visualizam fluxos internos de profissionais.

## 4. Requisitos não funcionais

| ID | Requisito | Prioridade | Métrica de aceite |
|---|---|---|---|
| RNF-01 | Integridade da reputação: **conteúdo** de avaliação (nota, comentário, autor, avaliado) imutável após publicação, com trilha de auditoria | Must | 0 endpoints de edição/exclusão de conteúdo; log de criação auditável. Status de moderação (RF-21) é a única exceção permitida, e não é edição de conteúdo — ver RF-21 |
| RNF-02 | Desempenho do matching do banco de talentos | Must | Resposta em até 2 segundos (p95) sob carga esperada do MVP |
| RNF-03 | Segurança e privacidade (LGPD; contato só após liberação explícita) | Must | Nenhum dado de contato retornado pela API sem liberação registrada |
| RNF-04 | Controle de acesso a avaliações restrito a assinantes ativos | Must | Verificação de assinatura ativa em toda leitura de avaliação, no backend |
| RNF-05 | Separação de entidades "empresa" e "profissional", com permissões e fluxos próprios | Must | Testes de autorização por papel cobrindo os dois perfis |
| RNF-06 | Disponibilidade compatível com uso comercial | Should | Meta inicial de 99,5% de uptime mensal para o MVP, revisável com o provedor de hospedagem escolhido |
| RNF-07 | Aderência ao Guia de Estilo e Design System (paleta primário amarelo `#FFC629` / secundário preto `#121212`) em todas as telas | Must | Nenhuma tela em produção usa cor fora dos tokens definidos no Guia de Estilo |
| RNF-08 | Contraste de texto conforme WCAG AA em todas as combinações de cor da paleta | Must | Texto sobre amarelo sempre em preto; nenhum texto branco sobre amarelo |
| RNF-09 | Rota de diagnóstico (`/api/health`) protegida, sem exposição de dados sensíveis | Should | Rota inacessível sem flag de ambiente + token; resposta nunca contém segredos |
| RNF-10 | Rate limiting em endpoints sensíveis (autenticação, matching, uso geral da API) | Must | Requisições acima do limite retornam 429 com `Retry-After`; limite válido mesmo com múltiplas instâncias |
| RNF-11 | Varredura contínua de segredos expostos (código, logs, respostas de API) | Must | Pipeline de CI bloqueia merge com segredo detectado; nenhuma resposta de API expõe variável de ambiente |
| RNF-12 | Resiliência de integrações externas críticas (pagamento, e-mail, chat) via camada de abstração e fallback | Should | Falha do provedor primário não derruba o fluxo do usuário; alternância ou falha controlada, com alerta registrado |

## 5. Requisitos de experiência do usuário (UX)

Referência completa: Guia de Estilo e Design System. Requisitos funcionais de UX que impactam fluxo, não só visual:

**RF-13 — Seleção de perfil no onboarding** · *Prioridade: Must*
- Primeira tela do cadastro pergunta explicitamente "Sou empresa" ou "Sou profissional", direcionando para o formulário correto. Profissional não escolhe PJ/CLT nesta tela — escolhe a(s) modalidade(s) que aceita como parte dos dados de cadastro (RF-02), não como um fluxo de onboarding separado.
- **Critério de aceite:** não existe formulário único genérico misturando cadastro de empresa e de profissional; não existem dois onboardings de profissional separados por modalidade (PJ e CLT compartilham o mesmo fluxo).

**RF-14 — Aviso de limite de plano** · *Prioridade: Should*
- Sistema exibe aviso não bloqueante quando restar 1 projeto/consulta disponível no plano ativo, antes do bloqueio efetivo (RF-10).
- **Critério de aceite:** aviso aparece pelo menos uma vez antes do limite ser atingido.

**RF-15 — Avaliação integrada ao fluxo de conclusão de projeto** · *Prioridade: Must*
- Ao marcar um projeto como concluído, o sistema direciona automaticamente para a tela de avaliação bilateral (RF-06), não apenas por e-mail.
- **Critério de aceite:** fluxo de conclusão de projeto não é considerado finalizado pela UI sem passar pela tela de avaliação (usuário pode adiar, mas a tela é apresentada).

**RF-16 — Estados vazios orientativos** · *Prioridade: Should*
- Telas sem resultado (banco de talentos sem match, perfil sem avaliações) exibem orientação de próximo passo, não apenas mensagem de "vazio".
- **Critério de aceite:** todo estado vazio mapeado no Guia de Estilo (seção 6) tem copy de orientação definida antes da implementação.

## 6. Requisitos de robustez técnica e conformidade

Referência completa: documento Comandos Técnicos Complementares. Requisitos funcionais adicionais, voltados a maturidade operacional e conformidade legal do produto:

**RF-17 — Onboarding retomável** · *Prioridade: Should* · *complementa RF-13*
- Progresso do onboarding (empresa ou profissional) é salvo a cada etapa e pode ser retomado de onde parou, sem perda de dados já preenchidos.
- **Critério de aceite:** usuário que interrompe o cadastro e retorna depois é levado exatamente à etapa em que parou, com os dados anteriores preenchidos.

**RF-18 — Aceite de Termos de Uso e Política de Privacidade** · *Prioridade: Must*
- Cadastro exige aceite explícito (não pré-marcado) da versão vigente dos Termos de Uso e da Política de Privacidade, com registro de data, IP e versão aceita.
- Nova versão dos termos publicada exige reaceite no próximo login, bloqueando ações críticas (publicar projeto, enviar interesse, avaliar) até a confirmação.
- **Critério de aceite:** nenhum usuário executa ação crítica sem um registro de aceite válido para a versão vigente dos termos.

**RF-19 — Downgrade de plano** · *Prioridade: Should*
- Usuário pode solicitar downgrade de tier dentro da mesma trilha (ex.: Profissional → Intermediário); a mudança só entra em vigor ao final do ciclo de cobrança vigente.
- Para profissional, downgrade do tier Profissional para Intermediário/Iniciante remove a visibilidade cruzada de modalidade (RF-24) a partir do próximo ciclo — não afeta candidaturas já enviadas.
- Trocar de **trilha** (profissional migrar de assinatura PJ para CLT, ou vice-versa) é uma operação distinta de downgrade de tier — muda a modalidade nativa da conta (RF-24), não é coberta por este requisito; tratar como novo requisito se e quando for necessária essa troca dentro do produto (hoje, o profissional cancelaria a assinatura de uma trilha e assinaria a outra).
- Oportunidades já existentes não são excluídas mesmo que excedam o novo limite; apenas a criação de novas oportunidades/consultas é bloqueada enquanto o uso estiver acima do limite do novo tier.
- **Critério de aceite:** downgrade nunca remove dado do usuário; bloqueio de criação só ocorre quando o uso excede o novo limite, nunca retroativamente.

**RF-20 — Exportação e exclusão de dados pessoais (LGPD)** · *Prioridade: Must*
- Usuário pode solicitar exportação de todos os seus dados pessoais em formato legível (portabilidade, LGPD art. 18).
- Usuário pode solicitar exclusão/anonimização de sua conta; avaliações que ele recebeu de terceiros são mantidas (fazem parte do histórico de reputação de outra parte que segue ativa na plataforma — ver RIPD, seção 5), mas seus dados pessoais identificáveis são anonimizados.
- **Critério de aceite:** exportação retorna todos os dados listados no inventário do RIPD referentes ao titular; exclusão anonimiza dados de identificação (nome, e-mail, dados pessoais) sem apagar avaliações que outros usuários receberam dele.

**RF-21 — Moderação de conteúdo** · *Prioridade: Should*
- O Zyft pode ocultar da visibilidade pública uma avaliação, mensagem ou descrição de projeto que viole as regras de conduta dos Termos de Uso (ex.: conteúdo ofensivo, discriminatório ou ilegal), mediante ação de um usuário com `tipo_usuario = admin`, via `PATCH /api/avaliacoes/{id}/moderar`, `PATCH /api/mensagens/{id}/moderar` ou `PATCH /api/projetos/{id}/moderar`.
- **A moderação nunca edita ou apaga o conteúdo original** — aplica um status de moderação em cima do registro existente, preservando o conteúdo e o histórico da ação (quem moderou, quando, motivo) para auditoria. Isso é o que reconcilia esta funcionalidade com RNF-01 no caso de avaliações: o conteúdo continua imutável; o que muda é sua visibilidade.
- Avaliação moderada é excluída do cálculo de nota média e do ranking de matching (RF-05), e não aparece em `GET /perfis/{id}/avaliacoes` para o público. Projeto moderado some das buscas e do banco de talentos, sem afetar seu `status` de workflow. Mensagem moderada tem o conteúdo substituído por placeholder na leitura. Em nenhum dos três casos o registro original é removido do banco.
- **Critério de aceite:** não existe nenhum caminho (nem para admin) que altere `nota`, `comentario`, `avaliador_id` ou `avaliado_id` de uma avaliação já criada; a ação de moderação em qualquer um dos três tipos de conteúdo só grava colunas de status/motivo/autor da moderação, em registro auditável e reversível (a reversão também é uma nova entrada de auditoria, não uma edição retroativa).

**RF-22 — Denúncia de conteúdo** · *Prioridade: Should* · *pré-requisito prático de RF-21*
- Qualquer usuário autenticado pode denunciar uma avaliação, mensagem, projeto **ou perfil** (empresa ou profissional), informando um motivo.
- Denúncias entram em uma fila com status `pendente`, visível apenas para `tipo_usuario = admin`.
- Admin pode marcar uma denúncia como `procedente` (e então aplicar a moderação correspondente via RF-21, ou a suspensão via RF-23 no caso de perfil) ou `improcedente` (descartar sem alterar o conteúdo/conta denunciada).
- **Sem este requisito, RF-21 é tecnicamente funcional mas operacionalmente inútil** — sem alguma forma de conteúdo chegar à atenção do admin, a moderação nunca seria acionada na prática.
- **Critério de aceite:** toda denúncia registra quem denunciou, o quê, quando e o motivo; o conteúdo/perfil denunciado não é ocultado/suspenso automaticamente só por ser denunciado — só a ação explícita de moderação (RF-21) ou suspensão (RF-23) surte efeito.

**RF-23 — Suspensão de conta** · *Prioridade: Should*
- O Zyft pode suspender a conta de uma empresa ou profissional que viole os Termos de Uso (ex.: perfil falso, fraude em avaliação, descumprimento reiterado de conduta), mediante ação de `tipo_usuario = admin`, tipicamente originada de uma denúncia de perfil (RF-22) marcada como procedente.
- Conta suspensa não consegue fazer login, publicar projeto, demonstrar interesse ou enviar mensagem — mas seus dados **não são excluídos** (distinto de RF-20, que é a pedido do próprio titular). Avaliações que a conta suspensa já recebeu ou deu permanecem visíveis (RNF-01).
- Formaliza tecnicamente a cláusula de suspensão já prevista nos Termos de Uso (Parte 1, item 7), que antes não tinha nenhum mecanismo correspondente no backend.
- **Critério de aceite:** conta com `status = suspenso` recebe 403 em qualquer tentativa de ação autenticada, exceto consulta ao próprio status; suspensão é reversível por outra ação de admin (registrada, não uma edição retroativa da suspensão original).

## 7. Matriz de rastreabilidade (DVP-E → DRP)

| Pilar do diferencial (DVP-E, seção 6) | Requisitos que o implementam |
|---|---|
| 1. Assinatura sem comissão | RF-10, RF-11, RF-12, RF-19, RF-24 |
| 2. Avaliação bilateral pública | RF-06, RNF-01, RNF-04 |
| 3. Foco exclusivo em TI, cobrindo PJ e CLT | Escopo do DVP-E (seção 7) — sem requisito funcional dedicado, é um filtro de categoria aplicado a todos os cadastros |
| 4. Validação de senioridade | RF-07 |
| 5. Dois modos de contratação nativos | RF-03, RF-04, RF-05 |
| 6. Identidade visual (Guia de Estilo) | RF-13, RF-14, RF-15, RF-16, RNF-07, RNF-08 |
| Robustez técnica e conformidade (transversal, sem pilar de diferencial dedicado) | RF-17, RF-18, RF-20, RF-21, RF-22, RF-23, RNF-09, RNF-10, RNF-11, RNF-12 |

*Nota: RF-19 (downgrade de plano) aparece só no Pilar 1 — é uma regra de monetização, não um item de robustez técnica pós-build. RF-17 e RF-18 (onboarding retomável e termos) sim são construídos junto com o backend/frontend principal (ver DVS, Fase 1), mas são auditados como robustez/conformidade porque não fazem parte de nenhum pilar de diferencial competitivo.*

## 8. Requisitos futuros (fora do MVP, mapeados)

- Comissão sobre projeto fechado.
- Escrow/custódia de pagamento.
- Gestão de folha de pagamento, benefícios e compliance para o profissional PJ.
- Aplicativo mobile nativo.
- Expansão para outras categorias de freelancer fora de TI.
