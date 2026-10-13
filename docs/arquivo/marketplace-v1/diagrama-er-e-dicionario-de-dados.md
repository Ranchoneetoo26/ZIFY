# Diagrama ER e Dicionário de Dados — Zyft

## Controle do Documento

| Campo | Valor |
|---|---|
| Projeto | Zyft |
| Documento | Diagrama ER e Dicionário de Dados |
| Versão | 1.0 |
| Status | Aprovado para desenvolvimento |
| Documentos relacionados | [drp - Documento de Requisitos do Produto.md](file:///c:/Users/Antonio/Desktop/Zyft/drp%20-%20Documento%20de%20Requisitos%20do%20Produto.md), [arquitetura-backend.md](file:///c:/Users/Antonio/Desktop/Zyft/arquitetura-backend.md) |

---

## 1. Visão Geral
O banco de dados do Zyft utiliza o modelo relacional (**PostgreSQL**) para garantir integridade referencial, imutabilidade de avaliações e controle rigoroso de assinaturas.

---

## 2. Dicionário de Dados (Tabelas)

### 2.1 Módulo de Usuários e Perfis

#### Tabela: `usuarios`
Entidade base para autenticação e RBAC.
| Coluna | Tipo | Descrição |
|---|---|---|
| `id` | UUID (PK) | Identificador único. |
| `email` | VARCHAR(255) | E-mail único do usuário. |
| `senha_hash` | TEXT | Hash da senha (BCrypt/Argon2). |
| `tipo_usuario` | ENUM | 'empresa', 'profissional_pj', 'profissional_clt', 'profissional_dual', 'admin'. |
| `status` | ENUM | 'ativo', 'suspenso', 'pendente_onboarding'. |
| `criado_em` | TIMESTAMP | Data de criação da conta. |

#### Tabela: `perfis_profissionais`
Dados detalhados do talento.
| Coluna | Tipo | Descrição |
|---|---|---|
| `id` | UUID (PK) | FK para `usuarios.id`. |
| `nome_completo` | VARCHAR(255) | Nome real do profissional. |
| `bio` | TEXT | Descrição curta de apresentação. |
| `senioridade_declarada` | ENUM | 'junior', 'pleno', 'senior'. |
| `senioridade_validada` | ENUM | Calculada pelo sistema (RF-07). |
| `modalidade_aceita` | VARCHAR(20) | 'PJ', 'CLT' ou 'AMBAS'. |
| `nota_media` | DECIMAL(3,2) | Média de avaliações (0.00 a 5.00). |

#### Tabela: `perfis_empresas`
| Coluna | Tipo | Descrição |
|---|---|---|
| `id` | UUID (PK) | FK para `usuarios.id`. |
| `razao_social` | VARCHAR(255) | Nome da empresa. |
| `cidade` | VARCHAR(100) | Localização da sede. |
| `segmento` | VARCHAR(100) | Ex: Fintech, E-commerce, etc. |

---

### 2.2 Módulo de Assinaturas e Planos

#### Tabela: `planos`
| Coluna | Tipo | Descrição |
|---|---|---|
| `id` | UUID (PK) | Identificador único. |
| `nome` | VARCHAR(50) | 'Iniciante', 'Intermediário', 'Profissional'. |
| `trilha` | ENUM | 'empresa', 'pj', 'clt', 'dual'. |
| `preco_mensal` | DECIMAL(10,2) | Valor base. |

#### Tabela: `assinaturas`
| Coluna | Tipo | Descrição |
|---|---|---|
| `id` | UUID (PK) | Identificador único. |
| `usuario_id` | UUID (FK) | Relacionamento com `usuarios`. |
| `plano_id` | UUID (FK) | Relacionamento com `planos`. |
| `periodicidade` | ENUM | 'mensal', 'semestral', 'anual'. |
| `data_inicio` | DATE | Início da vigência. |
| `data_fim` | DATE | Fim da vigência (renovação). |
| `status` | ENUM | 'ativa', 'cancelada', 'inadimplente', 'trial'. |

---

### 2.3 Módulo de Oportunidades e Matching

#### Tabela: `oportunidades`
| Coluna | Tipo | Descrição |
|---|---|---|
| `id` | UUID (PK) | Identificador único. |
| `empresa_id` | UUID (FK) | Quem publicou. |
| `titulo` | VARCHAR(255) | Nome da vaga/projeto. |
| `modalidade` | ENUM | 'PJ', 'CLT'. |
| `faixa_salarial_min` | DECIMAL(10,2) | Opcional para PJ. |
| `faixa_salarial_max` | DECIMAL(10,2) | Opcional para PJ. |
| `valor_hora` | DECIMAL(10,2) | Opcional para CLT. |
| `status` | ENUM | 'aberta', 'concluida', 'moderada'. |
| `criada_em` | TIMESTAMP | Data de publicação. |

#### Tabela: `candidaturas`
| Coluna | Tipo | Descrição |
|---|---|---|
| `id` | UUID (PK) | Identificador único. |
| `oportunidade_id` | UUID (FK) | Relacionamento com `oportunidades`. |
| `profissional_id` | UUID (FK) | Relacionamento com `usuarios`. |
| `posicao_fila` | INTEGER | Posição no matching (Fila de Prioridade). |
| `status` | ENUM | 'interessado', 'em_conversa', 'contratado', 'recusado'. |

---

### 2.4 Módulo de Confiança e Reputação

#### Tabela: `avaliacoes`
**Nota:** Tabela com imutabilidade forçada via Trigger.
| Coluna | Tipo | Descrição |
|---|---|---|
| `id` | UUID (PK) | Identificador único. |
| `avaliador_id` | UUID (FK) | Quem avaliou. |
| `avaliado_id` | UUID (FK) | Quem foi avaliado. |
| `oportunidade_id` | UUID (FK) | Vínculo com a entrega/contratação. |
| `nota` | INTEGER | 1 a 5 estrelas. |
| `comentario` | TEXT | Texto da avaliação. |
| `status_moderacao` | ENUM | 'visivel', 'oculto'. |
| `criado_em` | TIMESTAMP | Data da avaliação (Imutável). |

---

## 3. Regras de Integridade e Performance

### 3.1 Imutabilidade (RNF-01)
As tabelas `avaliacoes` e `historico_assinaturas` devem possuir **Triggers** de banco de dados que impeçam o comando `UPDATE` em colunas sensíveis (nota, comentário, valores) e o comando `DELETE`.

### 3.2 Índices Sugeridos
- `idx_oportunidade_modalidade`: Busca rápida por PJ ou CLT.
- `idx_profissional_senioridade_validada`: Otimização do motor de matching.
- `idx_assinatura_usuario_status`: Verificação rápida de permissões de acesso.

### 3.3 Constraints Críticas
- **Check Constraint na Tabela `avaliacoes`:** `nota BETWEEN 1 AND 5`.
- **Unique Constraint na Tabela `candidaturas`:** `(oportunidade_id, profissional_id)` — Impede candidaturas duplicadas.
- **FK On Delete Restrict:** Impede a exclusão de um usuário que possua avaliações vinculadas (integridade da reputação).

---
**Zyft — Banco de Dados Estruturado.**
