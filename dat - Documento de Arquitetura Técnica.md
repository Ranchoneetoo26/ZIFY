# DAT — Documento de Arquitetura Técnica

## Controle do Documento

| Campo | Valor |
|---|---|
| Projeto | Zyft |
| Documento | Arquitetura Técnica (DAT) |
| Versão | 10.0 |
| Status | Aprovado para desenvolvimento |
| Documentos relacionados | DVP-E, DRP, DVS, Arquitetura de Backend |

**Histórico de revisões**

| Versão | Alteração |
|---|---|
| 1.0 | Versão inicial — componentes, integrações e segurança |
| 2.0 | Adição de mapeamento requisito→componente, diagrama de implantação e decisões de arquitetura justificadas |
| 3.0 | Adição da seção de Design System no frontend e tokens de tema no mapeamento requisito→componente |
| 4.0 | Adição da seção de Observabilidade e Resiliência (rota de diagnóstico, rate limiting, fallback de integrações) e mapeamento de RF-17 a RF-19 / RNF-09 a RNF-12 |
| 5.0 | Atualização de nome do projeto para Farol, sem impacto na arquitetura definida |
| 6.0 | Atualização de nome do projeto para Lighthouse (anteriormente Farol), sem impacto na arquitetura definida |
| 7.0 | Atualização de nome do projeto para Zyft (anteriormente Lighthouse/Farol), sem impacto na arquitetura definida |
| 7.1 | Revisão final de consistência: adicionados os documentos Arquitetura de Frontend e Guia Mestre de Execução ao conjunto do projeto, referenciados em "Documentos relacionados" |
| 7.2 | Revisão técnica: corrigida hierarquia de seções (8.1 promovida a seção 9, renumeração das seguintes); referência ao Guia do Agente Supervisor |
| 7.3 | Referência ao novo documento Diagrama ER e Dicionário de Dados, fonte de verdade do modelo de dados |
| 7.4 | Referência ao novo Guia da Especificação de API (`openapi.yaml`) |
| 7.5 | Expansão de escopo para CLT: "profissional PJ" corrigido para "profissional" nas seções de frontend e módulos; nota explícita de que folha de pagamento/compliance nunca é responsabilidade do Zyft em contratações CLT |
| 8.0 | **Alinhamento final de planos e acessos.** Revisão do modelo de autorização para garantir exclusividade mútua entre Empresa, PJ e CLT, e suporte técnico à visibilidade cruzada CLT -> PJ. |
| 9.0 | **Adição do plano Dual (PJ + CLT).** Atualização da arquitetura de autorização para suportar a nova trilha de assinatura unificada com acesso total a ambas as modalidades. |
| 10.0 | **Otimização de Limites.** Ajustes nos módulos de enforcement de limites para suportar os novos valores de candidaturas e visualizações de banco de talentos. |

## 1. Visão geral da arquitetura

Aplicação web com arquitetura cliente-servidor clássica, dividida em:
- **Frontend web** (SPA) consumindo API própria.
- **Backend/API** central, responsável por regras de negócio, autenticação e integrações.
- **Banco de dados relacional** para dados estruturados (usuários, projetos, avaliações, assinaturas).
- **Serviços de terceiros** para pagamento (assinaturas recorrentes) e, opcionalmente, chat/mensageria.

```
[Frontend Web (SPA)]
        │  HTTPS/REST (ou GraphQL)
        ▼
[Backend/API] ──── [Banco de Dados Relacional]
        │
        ├── [Gateway de Pagamento] (assinaturas, trial, planos)
        ├── [Serviço de Chat/Mensageria] (interno ou terceirizado)
        └── [Serviço de Notificações] (e-mail/push para alertas de interesse)
```

## 2. Diagrama de implantação (visão lógica)

```
[Usuário - Navegador]
        │ HTTPS
        ▼
[CDN / Frontend estático]
        │
        ▼
[Load Balancer]
        │
        ▼
[Instância(s) de Backend/API] ── [Banco de Dados Relacional gerenciado]
        │
        ├── [Fila assíncrona] → [Worker de Notificações]
        └── [Integrações externas: Gateway de Pagamento, Chat]
```

## 3. Componentes principais

### 3.1 Frontend
- Aplicação web single-page, responsiva (empresa e profissional usam a mesma base de UI com fluxos distintos por perfil; modalidade PJ/CLT é um filtro dentro do fluxo de profissional, não um fluxo separado).
- Telas principais: cadastro/login, perfil (empresa e profissional), publicação de oportunidade (PJ ou CLT), banco de talentos, chat, avaliações, planos/assinatura.
- Especificação completa (stack, estrutura de pastas, roteamento tela a tela, padrão de consumo de API, cache) está no documento **Arquitetura de Frontend** — este documento (DAT) mantém apenas a visão geral e a integração com o Design System.

**Design System**
- Implementação orientada a tokens de tema, não a valores fixos espalhados pelo código: `--color-primary` (`#FFC629`), `--color-secondary` (`#121212`), `--color-bg`, `--color-bg-alt`, `--color-text-secondary`, `--color-border`, `--color-success`, `--color-danger`, conforme definido no Guia de Estilo e Design System.
- Biblioteca de componentes-base (botão primário/secundário, badge de nota, card, estado vazio) construída sobre esses tokens, para garantir RNF-07 (aderência ao design system) e RNF-08 (contraste WCAG AA) em toda a aplicação, sem retrabalho tela a tela.
- Tipografia carregada como tokens também: `--font-display` (Space Grotesk), `--font-body` (IBM Plex Sans), `--font-mono` (IBM Plex Mono).

### 3.2 Backend/API
- Camada de API central baseada em **ASP.NET Core (.NET 8/9)** expondo endpoints REST para todas as operações do frontend.
- Módulos internos (detalhamento completo em `arquitetura-backend.md`):
  - Autenticação e autorização
  - Usuários (empresa e profissional, com modalidades PJ/CLT aceitas)
  - Projetos (publicação, interesse)
  - Banco de talentos (matching)
  - Avaliações e reputação
  - Assinaturas e cobrança
  - Chat/mensagens
  - Notificações
  - Termos e aceite (RF-18)
  - Observabilidade (rota de diagnóstico — RNF-09)
- Camadas transversais aplicadas a todos os módulos acima: rate limiting (RNF-10) e adapter/fallback para integrações externas críticas (RNF-12).

### 3.3 Banco de dados
- Banco relacional (ex.: PostgreSQL) para garantir integridade referencial entre empresas, profissionais, projetos, avaliações e assinaturas.
- Uso de constraints e/ou trilha de auditoria para garantir imutabilidade das avaliações (RNF-01 do DRP).

### 3.4 Integrações externas
- **Gateway de pagamento**: processamento de assinaturas recorrentes (mensal/semestral/anual), gestão de trial sem cartão, upgrade/downgrade de plano (RF-19). Acessado via camada de abstração (adapter) com fallback para provedor secundário (RNF-12).
- **Serviço de chat**: pode ser implementado internamente (simples, baseado em mensagens persistidas) ou via provedor terceirizado, dependendo do orçamento e prazo do MVP.
- **Serviço de e-mail/notificação**: alertas de interesse em projeto, resultados de matching, avisos de fim de trial. Também acessado via adapter com fallback (RNF-12).

## 4. Mapeamento requisito → componente (rastreabilidade com o DRP)

| Requisito (DRP) | Componente responsável |
|---|---|
| RF-01, RF-02 | Módulo de Usuários |
| RF-03, RF-04 | Módulo de Projetos |
| RF-05 | Módulo de Banco de Talentos (matching) |
| RF-06 | Módulo de Avaliações e Reputação |
| RF-07 | Módulo de Validação de Senioridade |
| RF-08 | Módulo de Chat |
| RF-09 | Módulo de Liberação de Contato |
| RF-10, RF-11, RF-12 | Módulo de Assinaturas e Cobrança |
| RNF-01, RNF-04 | Módulo de Avaliações e Reputação + camada de autorização |
| RNF-02 | Módulo de Banco de Talentos (matching) |
| RNF-03 | Camada de segurança/API Gateway |
| RNF-05 | Módulo de Usuários + camada de autorização |
| RF-13, RF-14, RF-15, RF-16 | Frontend — fluxos de onboarding, avisos de plano, avaliação e estados vazios |
| RNF-07, RNF-08 | Frontend — Design System (tokens de tema e biblioteca de componentes-base) |
| RF-17 | Módulo de Usuários — estado de progresso do onboarding persistido |
| RF-18 | Módulo de Termos e Aceite |
| RF-19 | Módulo de Assinaturas e Cobrança — regra de downgrade |
| RF-20 | Módulo de Usuários + Camada de Segurança (LGPD) |
| RF-21, RF-22, RF-23 | Módulos de Avaliações/Projetos/Chat + Admin Portal |
| RF-24 | Camada de Autorização (RBAC) |
| RNF-09 | Módulo de Observabilidade (rota `/api/health`) |
| RNF-10 | Camada transversal de rate limiting |
| RNF-11 | Pipeline de CI — varredura de segredos expostos |
| RNF-12 | Camada de abstração (adapter) + circuit breaker sobre integrações externas |

## 5. Decisões de arquitetura (ADR resumido)

| Decisão | Justificativa | Alternativa considerada |
|---|---|---|
| Backend em **C# (.NET 8/9)** | Alta performance com Minimal APIs, tipagem forte, ecossistema empresarial maduro e segurança nativa. | Node.js ou Python — descartados pela preferência por escalabilidade e robustez do ecossistema .NET. |
| Banco relacional (PostgreSQL) | Forte necessidade de integridade referencial e constraints (ex.: imutabilidade de avaliações, limites de plano). | Banco NoSQL — descartado por exigir mais controle manual de integridade. |
| Matching inicial com regras determinísticas | Reduz risco técnico no MVP e permite validar a hipótese de valor rapidamente | Modelo de recomendação com aprendizado de máquina — adiado para pós-MVP, quando houver volume de dados suficiente |
| Autenticação via JWT | Simples de escalar horizontalmente, sem estado no servidor | Sessão baseada em servidor — descartada por dificultar escalabilidade |

## 6. Autenticação e autorização

- Autenticação via e-mail/senha (com possibilidade futura de login social).
- **Autorização Baseada em Papéis (RBAC):** Controle rigoroso de acesso garantindo que usuários logados como `empresa`, `profissional_pj`, `profissional_clt` ou `profissional_dual` acessem apenas seus respectivos fluxos (RNF-05 do DRP).
- **Exclusividade Mútua:** O backend deve validar em cada requisição se o tipo de usuário tem permissão para a funcionalidade solicitada, impedindo que um perfil PJ acesse rotas de Empresa ou CLT, e vice-versa.
- **Privilégio CLT:** Implementação de permissão de leitura (view-only) para usuários CLT em projetos PJ, sem permissão de escrita (candidatura).
- Controle de acesso a conteúdo de avaliações restrito a assinantes ativos (RNF-04 do DRP) — deve ser verificado a nível de API, não só de UI.

## 7. Considerações de desempenho

- O módulo de matching do banco de talentos é o ponto crítico de desempenho (diferencial competitivo de velocidade vs. vetting manual dos concorrentes). Recomenda-se:
  - Indexação adequada dos campos usados em filtros (grau de experiência, especialidade, nota).
  - Início com regras determinísticas (filtros + ranking por nota), evitando complexidade desnecessária no MVP.

## 8. Segurança

- Dados de contato (e-mail/WhatsApp) só devem ser retornados pela API após confirmação de liberação de ambas as partes.
- Conformidade com LGPD para dados pessoais de profissionais e dados cadastrais de empresas.
- Trilha de auditoria para avaliações (criação, sem edição/exclusão).
- Varredura contínua de segredos expostos no pipeline de CI, cobrindo código-fonte, histórico de commits e logs em runtime (RNF-11 do DRP).
- Rate limiting aplicado como camada transversal em endpoints sensíveis, com contador compartilhado (ex.: Redis) entre instâncias (RNF-10 do DRP).

## 9. Observabilidade e resiliência

- **Rota de diagnóstico** (`/api/health`, RNF-09): ativa apenas com flag de ambiente e token interno; reporta status do serviço, banco e fila, sem expor segredos.
- **Resiliência de integrações externas** (RNF-12): gateway de pagamento, e-mail e chat acessados por camada de abstração (adapter), com circuit breaker para alternância automática a um provedor secundário em caso de falha do primário, e alerta registrado para a equipe.
- Esses componentes são transversais — não pertencem a um único módulo de negócio, mas atravessam todos os que dependem de integrações externas ou precisam ser monitorados em produção.

## 10. Extensibilidade planejada (pós-MVP)

- Módulo de comissão sobre projeto fechado (feature flag, hoje fora de escopo).
- Módulo de escrow/custódia de pagamento.
- Módulo de folha de pagamento/compliance para PJ. (Nunca se aplica a CLT — pagamento e compliance trabalhista de contratações CLT são sempre da empresa contratante, nunca do Zyft, ver Termos de Uso, item 1.)
- Aplicativo mobile consumindo a mesma API.

## 11. Ambientes e infraestrutura

- Ambientes recomendados: desenvolvimento, homologação/staging e produção.
- Hospedagem em nuvem (provedor a definir), com banco de dados gerenciado para reduzir esforço operacional no MVP.
- Pipeline de CI/CD básico para deploy contínuo do frontend e backend.
