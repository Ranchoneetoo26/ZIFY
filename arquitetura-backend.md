# Arquitetura de Backend

## Controle do Documento

| Campo | Valor |
|---|---|
| Projeto | Zyft |
| Documento | Arquitetura de Backend |
| Versão | 12.0 |
| Status | Aprovado para desenvolvimento |
| Documentos relacionados | DVP-E, DRP, DVS, DAT |

**Histórico de revisões**

| Versão | Alteração |
|---|---|
| 1.0 | Versão inicial — módulos, modelo de dados e regras de negócio |
| 2.0 | Adição de exemplos de contrato de API, estratégia de testes e detalhamento de segurança |
| 3.0 | Endpoint de uso/limite de plano exposto para suportar aviso não bloqueante de UX (RF-14) |
| 4.0 | Formalização dos módulos de Termos e Aceite, Observabilidade, regra de downgrade e camada de adapter/fallback, alinhados a RF-17 a RF-19 e RNF-09 a RNF-12 |
| 5.0 | Atualização de nome do projeto para Farol, sem impacto nos módulos definidos |
| 6.0 | Atualização de nome do projeto para Lighthouse (anteriormente Farol), sem impacto nos módulos definidos |
| 7.0 | Atualização de nome do projeto para Zyft (anteriormente Lighthouse/Farol), sem impacto nos módulos definidos |
| 7.1 | Revisão final de consistência: adicionados os documentos Arquitetura de Frontend e Guia Mestre de Execução ao conjunto do projeto, referenciados em "Documentos relacionados" |
| 7.2 | Revisão técnica: especificado mecanismo exato de entrega do JWT (cookie httpOnly) e adicionada proteção CSRF, alinhado com a Arquitetura de Frontend; referência ao Guia do Agente Supervisor |
| 8.0 | Modelo de dados formalizado no novo documento Diagrama ER e Dicionário de Dados, corrigindo 3 furos: tabela `usuario` como supertipo (FKs polimórficas antes impossíveis), tabela `plano` (referenciada mas inexistente), entidade `conversa` (referenciada por `mensagem.conversa_id` mas inexistente); removida duplicação de `plano_id`/`periodicidade`/`status_trial` entre `empresa`/`profissional_pj` e `assinatura` |
| 8.1 | Contratos de API completos formalizados em `openapi.yaml` (27 rotas cobrindo os 12 módulos), acompanhado do Guia da Especificação de API |
| 8.2 | Adicionado Módulo de Privacidade e Direitos LGPD (2.13), implementando RF-20; endpoints correspondentes em openapi.yaml |
| 8.3 | Módulo de Avaliações atualizado com RF-21 (moderação): imutabilidade agora explicitamente restrita a colunas de conteúdo, com status de moderação como exceção auditável — resolve contradição entre RNF-01 e os Termos de Uso |
| 8.4 | RF-21 estendido aos módulos de Projetos e Chat (a v8.3 só cobria avaliações, mas o requisito já prometia cobrir mensagem e descrição de projeto também) |
| 8.5 | Novo Módulo de Denúncias e Moderação (2.14), implementando RF-22 — pré-requisito prático para RF-21 ser acionado, já que antes nada levava conteúdo à atenção do admin |
| 8.6 | Módulo renomeado para Denúncias, Moderação e Suspensão; adicionado `PATCH /api/usuarios/{id}/suspender` (RF-23), formalizando a suspensão de conta já prometida nos Termos de Uso sem mecanismo técnico correspondente |
| 9.0 | **Expansão de escopo: cobre também CLT.** Módulo de Usuários ganhou `modalidades_aceitas`; Módulo de Projetos renomeado para Módulo de Oportunidades, com `modalidade_contratacao` e campos condicionais (horas vs. faixa salarial); Módulo de Banco de Talentos filtra por modalidade; Módulo de Avaliações esclarece o gatilho de liberação diferente por modalidade |
| 10.0 | **Alinhamento final de planos e acessos.** Módulo de Autorização atualizado para suportar a exclusividade mútua total entre Empresa, PJ e CLT. Implementação da regra de visibilidade CLT -> PJ e restrição de candidatura no Módulo de Oportunidades. |
| 11.0 | **Adição do plano Dual (PJ + CLT).** Módulo de Autorização atualizado para permitir acesso total a ambas as modalidades na trilha Dual. |
| 12.0 | **Otimização de Limites.** Atualização dos middlewares de enforcement de limites para os novos valores de candidaturas e visualizações de banco de talentos. |

## 1. Stack Tecnológica (Implementação em C#)

Para garantir produtividade, segurança de memória e alta escalabilidade, o backend do Zyft será desenvolvido em **C# (.NET 8/9)**.

- **Framework**: ASP.NET Core Web API (Minimal APIs para máxima performance).
- **Banco de Dados**: PostgreSQL com **Entity Framework Core** (ORM) para produtividade ou **Dapper** para consultas críticas.
- **Autenticação**: ASP.NET Core Identity + JWT Bearer Authentication.
- **Segurança**: Proteção nativa contra CSRF, XSS e SQL Injection.
- **Documentação**: Swagger/OpenAPI integrada nativamente.
- **Build & Deploy**: .NET CLI e Docker para containerização.
- **Mensageria**: Azure Service Bus ou RabbitMQ via **MassTransit**.

## 2. Organização em módulos

*Os exemplos de contrato abaixo são ilustrativos. A especificação completa e formal de todos os endpoints (todas as rotas, payloads, respostas e códigos de erro) está em `openapi.yaml`, descrita no Guia da Especificação de API.*

### 2.1 Módulo de Autenticação
- Cadastro/login de empresa e profissional (independente da modalidade que o profissional aceita).
- Emissão e renovação de tokens (JWT), entregues via `Set-Cookie` (`httpOnly`, `Secure`, `SameSite=Lax`) — nunca retornados no corpo da resposta JSON para evitar armazenamento em `localStorage`.
- Recuperação de senha.

### 2.2 Módulo de Usuários (RF-01, RF-02, RF-17)
**Entidade Empresa**
- nome, cidade, tipo(s) de trabalho oferecido, dados de contato, plano ativo.

**Entidade Profissional** (tabela `profissional_pj`, nome mantido por estabilidade de schema — ver nota de nomenclatura no Diagrama ER, seção 3.3)
- dados pessoais, experiências de trabalho, cursos, diplomas, grau de experiência declarado, grau de experiência validado (calculado), **modalidades aceitas (PJ e/ou CLT)**, plano ativo.

**Onboarding retomável (RF-17)**
- Entidade `onboarding_progresso` (usuario_id, tipo_usuario, etapa_atual, dados_parciais, concluido).
- `GET /api/onboarding/progresso` retorna etapa atual e dados salvos; `PATCH /api/onboarding/progresso` salva a etapa atual e avança.
- Ao concluir a última etapa, marca `concluido = true` e dispara a inclusão automática no banco de talentos (RF-02), elegível para as modalidades marcadas em `modalidades_aceitas`.

### 2.3 Módulo de Oportunidades (RF-03, RF-04, RF-21)
*Chamado de "Módulo de Projetos" em versões anteriores desta documentação — renomeado porque a tabela `projeto` (nome de schema mantido, ver Diagrama ER seção 3.9) agora cobre tanto projeto PJ quanto vaga CLT.*
- CRUD de oportunidades (empresa): `modalidade_contratacao` (pj/clt), campos condicionais (horas necessárias para PJ, faixa salarial para CLT), grau de experiência exigido, descrição, status.
- Endpoint de demonstração de interesse (profissional): registra interesse + dispara notificação para a empresa.
- Validação de limite de oportunidades conforme plano ativo da empresa (Iniciante/Intermediário/Profissional) — limite soma PJ e CLT.
- **Moderação (RF-21):** endpoint restrito a `usuario.tipo_usuario = admin` que altera `status_moderacao`/`moderado_por`/`moderado_em`/`motivo_moderacao` de uma oportunidade cuja descrição viole os Termos de Uso, em qualquer modalidade. Oportunidade moderada some das buscas (RF-03, RF-05), mas seu `status` de workflow não é afetado — são eixos independentes.

**Exemplo de contrato — publicar oportunidade PJ**
```
POST /api/projetos
Body: {
  "modalidade_contratacao": "pj",
  "horas_necessarias": 40,
  "grau_experiencia_exigido": "pleno",
  "descricao": "Desenvolvimento de API REST em Node.js"
}
Resposta 201: { "id": "uuid", "status": "aberto", "modalidade_contratacao": "pj" }
Resposta 403 (limite excedido): { "erro": "limite_oportunidades_plano_excedido" }
```

**Exemplo de contrato — publicar oportunidade CLT**
```
POST /api/projetos
Body: {
  "modalidade_contratacao": "clt",
  "faixa_salarial_min": 800000,
  "faixa_salarial_max": 1200000,
  "grau_experiencia_exigido": "senior",
  "descricao": "Vaga efetiva para Tech Lead"
}
Resposta 201: { "id": "uuid", "status": "aberto", "modalidade_contratacao": "clt" }
```
*Valores de faixa salarial em centavos — ver Diagrama ER, seção 3.9.*

### 2.4 Módulo de Banco de Talentos / matching (RF-05)
- Endpoint para empresa enviar especificação de perfil desejado, incluindo `modalidade_contratacao` desejada.
- Motor de matching (MVP): filtro determinístico por grau de experiência, especialidade, **modalidade aceita pelo profissional** e ordenação por nota de avaliação.
- Registro automático do profissional no banco de talentos ao concluir cadastro, elegível conforme `modalidades_aceitas`.
- Validação de limite de consultas/oportunidades conforme plano ativo.

**Exemplo de contrato — buscar talentos**
```
POST /api/banco-talentos/busca
Body: { "grau_experiencia": "senior", "especialidade": "backend", "modalidade_contratacao": "clt" }
Resposta 200: {
  "resultados": [
    { "profissional_id": "uuid", "nome": "...", "nota_media": 4.8, "grau_experiencia_validado": "senior", "modalidades_aceitas": ["pj", "clt"] }
  ]
}
```

### 2.5 Módulo de Avaliações e Reputação (RF-06, RF-21, RNF-01, RNF-04)
- Criação de avaliação (nota 1-5 + comentário) vinculada a uma oportunidade com `status = concluido`, nos dois sentidos (empresa→profissional e profissional→empresa), em qualquer modalidade.
- Gatilho de liberação difere por `modalidade_contratacao`: para `pj`, `status = concluido` significa entrega do projeto; para `clt`, significa contratação confirmada ao fim do processo seletivo. Em ambos os casos, a avaliação é liberada uma única vez — não há reavaliação periódica durante um vínculo CLT em andamento (RF-06).
- Regra de imutabilidade: sem endpoints de edição ou exclusão de **conteúdo** após criação; apenas leitura. A única exceção é o status de moderação (ver abaixo), que não altera conteúdo.
- Cálculo de nota média/ranking usado pelo motor de matching e pela busca — **exclui avaliações com `status_moderacao = removida_por_moderacao`**, somando avaliações de oportunidades PJ e CLT no mesmo cálculo de nota do profissional/empresa.
- Middleware de controle de acesso: leitura de nota + comentário liberada apenas para usuários com assinatura ativa, e apenas avaliações com `status_moderacao = ativa`.

**Regra de imutabilidade (nível de banco):** trigger que bloqueia UPDATE das colunas de conteúdo (`nota`, `comentario`, `avaliador_id`, `avaliado_id`, `tipo`, `projeto_id`, `criada_em`) e bloqueia DELETE incondicionalmente, garantindo RNF-01 mesmo em caso de falha na camada de aplicação. Ver Diagrama ER e Dicionário de Dados, seção 3.13, para a definição completa.

**Moderação (RF-21):** endpoint restrito a `usuario.tipo_usuario = admin` que altera apenas `status_moderacao`/`moderado_por`/`moderado_em`/`motivo_moderacao` — nunca o conteúdo da avaliação. Resolve a exigência dos Termos de Uso (Parte 1, item 5) sem violar RNF-01: o conteúdo original permanece intacto e auditável, só a visibilidade pública muda.

### 2.6 Módulo de Validação de Senioridade (RF-07)
- Serviço que cruza: anos de experiência declarados + certificações cadastradas + histórico de avaliações recebidas.
- Retorna um grau de experiência validado, usado no matching e exibido no perfil.
- Campo `grau_experiencia_validado` é somente leitura para o profissional (calculado pelo backend).

### 2.7 Módulo de Chat (RF-08, RF-21)
- Toda mensagem pertence a uma `conversa` (criada a partir de um `projeto` ou de um `interesse` — ver Diagrama ER e Dicionário de Dados, seção 3.11), nunca solta.
- Endpoints para criar/recuperar a conversa entre empresa e profissional, e para envio/recebimento de mensagens dentro dela.
- Persistência do histórico de conversa.
- **Moderação (RF-21):** endpoint restrito a `usuario.tipo_usuario = admin` que altera `status_moderacao`/`moderado_por`/`moderado_em`/`motivo_moderacao` de uma mensagem. Diferente de `avaliacao`, `mensagem` não tem RNF de imutabilidade — o padrão de status separado foi adotado por consistência com o restante do RF-21, não por exigência técnica. Mensagem moderada tem `conteudo` substituído por um placeholder na leitura (`GET /api/conversas/{id}/mensagens`), preservando o texto original só para auditoria interna.

### 2.8 Módulo de Liberação de Contato (RF-09)
- Endpoint de solicitação/aceite de liberação de dados de contato (e-mail/WhatsApp), vinculado à `conversa` (não mais a `projeto_id/interesse_id` separadamente — a conversa já carrega essa origem).
- Dados de contato só retornados pela API após confirmação de ambas as partes (`empresa_aceite = true AND profissional_aceite = true`).

### 2.9 Módulo de Assinaturas e Cobrança (RF-10, RF-11, RF-12)
- Gestão dos planos (Iniciante, Intermediário, Profissional) × periodicidades (mensal, semestral, anual), com valores distintos por trilha (Empresa, PJ, CLT, Dual).
- Aplicação de descontos (~20% semestral, ~35% anual).
- Gestão de trial de 30 dias (somente tier Iniciante, sem cartão), com transição automática para cobrança obrigatória ao final do período.
- Integração com gateway de pagamento externo para processar cobranças recorrentes, upgrade/downgrade e cancelamento.
- Enforcement de limites de uso (número de projetos) conforme plano ativo — verificado nos módulos de Projetos e Banco de Talentos.
- Endpoint de consulta de uso (`GET /api/assinatura/uso`) retornando quantidade consumida vs. limite do plano, usado pelo frontend para exibir o aviso não bloqueante de limite (RF-14 do DRP).

**Downgrade de plano (RF-19)**
- `PATCH /api/assinatura/downgrade`: o downgrade só entra em vigor ao final do ciclo de cobrança vigente.
- Projetos já existentes não são excluídos mesmo que excedam o novo limite; apenas a criação de novos projetos/consultas é bloqueada enquanto o uso estiver acima do limite do novo plano.
- Notifica o usuário (Módulo de Notificações) confirmando a data em que o downgrade passa a valer.

### 2.10 Módulo de Notificações
- Disparo de e-mail/notificação para: novo interesse em projeto, resultado de matching, aviso de fim de trial, nova mensagem no chat.
- Acesso ao provedor de e-mail via camada de abstração (adapter) com fallback (ver seção 5, RNF-12).

### 2.11 Módulo de Termos e Aceite (RF-18)
- Entidade `termo_aceite` (id, usuario_id, tipo_usuario, versao_termo, data_aceite, ip_aceite).
- `GET /api/termos/atual` retorna a versão vigente dos Termos de Uso e da Política de Privacidade.
- `POST /api/termos/aceitar` registra o aceite do usuário autenticado para a versão vigente (data e IP).
- Cadastro (RF-01/RF-02) exige aceite explícito antes de concluir o onboarding. Nova versão publicada força reaceite no próximo login, bloqueando ações críticas (publicar projeto, enviar interesse, avaliar) até a confirmação.

### 2.12 Módulo de Observabilidade (RNF-09)
- `GET /api/health`: status do serviço, conexão com banco de dados, status da fila (se houver) e versão da API.
- Ativo apenas com `DEBUG_ROUTES_ENABLED=true`; em produção, exige também header `X-Debug-Token` válido — sem ele, retorna 404 (não revela existência da rota).
- Nunca retorna variáveis de ambiente, senhas, tokens ou strings de conexão no corpo da resposta.

### 2.13 Módulo de Privacidade e Direitos LGPD (RF-20)
- `GET /api/usuarios/{id}/exportar-dados`: retorna todos os dados pessoais do titular, conforme o inventário do RIPD — Relatório de Impacto à Proteção de Dados.
- `DELETE /api/usuarios/{id}`: processa a solicitação de exclusão/anonimização de forma assíncrona. Anonimiza `nome`, `email` e dados pessoais em `experiencia_profissional`/`formacao`/`certificacao`; **não remove** registros em `avaliacao` onde o usuário é `avaliado_id` (RNF-01 e RIPD, seção 5 — a avaliação pertence ao histórico de reputação de quem a recebeu, não é dado pessoal exclusivo de quem a deu).

### 2.14 Módulo de Denúncias, Moderação e Suspensão (RF-21, RF-22, RF-23)
- `POST /api/denuncias`: qualquer usuário autenticado denuncia uma avaliação, mensagem, projeto ou **perfil** (`tipo_conteudo` + `conteudo_id` + `motivo`). Backend valida que `conteudo_id` existe no destino indicado por `tipo_conteudo` antes de aceitar (integridade garantida na aplicação, não no banco — ver Diagrama ER, seção 3.17). Para `tipo_conteudo = perfil`, valida ainda que `conteudo_id` é um `usuario.id` com `tipo_usuario` em `empresa`/`profissional_pj`.
- `GET /api/denuncias?status=pendente`: restrito a `usuario.tipo_usuario = admin`. Lista a fila de denúncias pendentes, ordenada por `criada_em`.
- `PATCH /api/denuncias/{id}`: restrito a admin. Marca `resultado` (`procedente`/`improcedente`) e `status = avaliada`. Marcar como `procedente` **não modera nem suspende automaticamente** — é uma ação separada e intencional do admin chamar o endpoint correspondente (`PATCH /api/avaliacoes/{id}/moderar`, `.../mensagens/{id}/moderar`, `.../projetos/{id}/moderar` ou `PATCH /api/usuarios/{id}/suspender`), evitando que uma denúncia sozinha (potencialmente infundada) já produza efeito sem revisão humana.
- `PATCH /api/usuarios/{id}/suspender` (RF-23): restrito a admin. Altera `usuario.status` para `suspenso` (ou de volta para `ativo`, na reversão). Conta suspensa recebe 403 em qualquer ação autenticada, exceto consulta ao próprio status. Não afeta avaliações já dadas/recebidas pela conta (RNF-01) nem exclui dados (distinto de RF-20).

## 3. Modelo de dados

Especificação completa (tipos, constraints, índices, diagrama ER) está no documento **Diagrama ER e Dicionário de Dados** — este documento (Arquitetura de Backend) mantém apenas o resumo de entidades para contexto rápido.

**Correção importante em relação a versões anteriores deste documento:** `empresa` e `profissional_pj` são subtipos de uma tabela supertipo `usuario` (necessária para que `assinatura`, `avaliacao`, `mensagem`, `onboarding_progresso` e `termo_aceite` tenham uma foreign key válida, já que precisam referenciar "qualquer tipo de usuário"). Os campos `plano_id`, `periodicidade` e `status_trial` deixaram de existir em `empresa`/`profissional_pj` — eles vivem exclusivamente em `assinatura`, que é a fonte única da verdade sobre o plano de cada usuário. Uma entidade `plano` (antes inexistente) e uma entidade `conversa` (antes implícita, referenciada por `mensagem.conversa_id` sem existir) também foram formalizadas. Detalhes completos: Diagrama ER e Dicionário de Dados.

- `usuario` (supertipo — `empresa` e `profissional_pj` em relação 1:1)
- `empresa`, `profissional_pj`
- `experiencia_profissional`, `formacao`, `certificacao`
- `plano`, `assinatura`
- `projeto`, `interesse`
- `conversa`, `mensagem`
- `avaliacao`, `liberacao_contato`
- `onboarding_progresso`, `termo_aceite`

## 4. Regras de negócio críticas no backend

- **Exclusividade Mútua Total:** Cada requisição deve validar se o `usuario.tipo_usuario` (Empresa, PJ ou CLT) tem permissão para a ação solicitada. Um tipo de usuário nunca pode acessar endpoints ou dados exclusivos dos outros dois tipos.
- **Visibilidade vs. Candidatura (CLT):** O Módulo de Oportunidades deve permitir que usuários CLT acessem o `GET` de projetos PJ, mas deve bloquear o `POST /interesse` com erro 403.
- Nenhuma avaliação pode ser alterada ou removida após criada (endpoint não deve existir; garantir também a nível de banco).
- Conteúdo de avaliações só é retornado para usuários com assinatura ativa (checagem no backend, não só no frontend).
- Limite de projetos/consultas deve ser validado no backend antes de permitir nova publicação/consulta, conforme o plano ativo.
- Dados de contato nunca devem ser expostos por padrão; só após liberação registrada de ambas as partes.
- Grau de experiência validado é calculado pelo backend (não pode ser autodeclarado diretamente pelo usuário).

## 5. Segurança e conformidade

- Todas as chamadas de API sobre HTTPS.
- Senhas armazenadas com hash (ex.: bcrypt/argon2), nunca em texto plano.
- **Proteção CSRF**: como a autenticação usa cookie `httpOnly` (seção 1), toda rota que altera estado (`POST`, `PATCH`, `DELETE`) exige token CSRF (padrão double-submit cookie ou header customizado validado no backend), além do `SameSite=Lax` do cookie de sessão — o cookie sozinho não é proteção suficiente contra CSRF.
- **Rate limiting (RNF-10):** middleware aplicado por IP e por usuário autenticado, com contador compartilhado (ex.: Redis) entre instâncias. Limites por grupo de endpoint: autenticação (5/min por IP), matching (30/min por usuário), demais endpoints (100/min por usuário). Acima do limite, retorna HTTP 429 com header `Retry-After`.
- Validação e sanitização de entrada em todos os endpoints públicos (proteção contra injeção).
- Conformidade com LGPD: consentimento explícito para uso de dados pessoais (reforçado pelo Módulo de Termos e Aceite, RF-18), endpoint de exportação/exclusão de dados do usuário.
- **Varredura de segredos expostos (RNF-11):** pipeline de CI com ferramenta de secret scanning (ex.: gitleaks) em cada push/PR, bloqueando merge se detectar chave/token/senha no código ou histórico de commits; `.env` e variantes no `.gitignore` desde o primeiro commit; nenhum log ou resposta de API expõe `process.env` (ou equivalente) completo.
- **Resiliência de integrações externas (RNF-12):** gateway de pagamento, e-mail e chat acessados por camada de abstração (adapter) com interface interna comum (ex.: `processarCobranca()`, `enviarEmail()`); circuit breaker alterna automaticamente para provedor secundário configurado após limite de falhas do primário, registrando alerta para a equipe.

## 6. Estratégia de testes

| Tipo de teste | Cobertura prioritária |
|---|---|
| Testes unitários | Regras de negócio críticas: cálculo de grau de experiência validado, limites de plano, imutabilidade de avaliação |
| Testes de integração | Fluxo completo de publicação de projeto → interesse → avaliação; fluxo de assinatura/trial → cobrança |
| Testes de carga | Endpoint de matching do banco de talentos (RNF-02) |
| Testes de segurança | Autorização por papel (RNF-05), exposição de dados de contato (RF-09), acesso a avaliações por assinatura (RNF-04) |

## 7. Próximos passos técnicos sugeridos

1. ~~Modelagem detalhada do banco de dados (diagrama ER completo)~~ — concluída, ver Diagrama ER e Dicionário de Dados.
2. ~~Definição dos contratos de API completos (endpoints, payloads, códigos de erro) por módulo~~ — concluída, ver `openapi.yaml` e Guia da Especificação de API.
3. Protótipo do motor de matching com regras determinísticas simples, para validação antes de evoluir a lógica.
4. Escolha definitiva do gateway de pagamento compatível com trial sem cartão e múltiplas periodicidades.
