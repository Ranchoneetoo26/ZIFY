# Histórico e reposicionamento do Zyft

| Campo | Valor |
|---|---|
| Documento | Histórico e reposicionamento |
| Versão | 1.0 |
| Data | 12/10/2026 |
| Status | Vigente |
| Fontes | `ZYFT_visao_produto_reposicionamento_MVP1_MVP2.docx`, `plano-automacao-candidaturas.pdf` (nesta pasta), [ADR-0001](../03-arquitetura/adr/0001-reposicionamento-mvp1-candidatura-assistida.md) |

## 1. Resumo

O Zyft começou como um **marketplace de talentos de tecnologia** (PJ e CLT, assinatura fixa sem comissão, reputação bilateral). Em outubro de 2026, o foco da primeira entrega mudou. O **MVP 1** passa a ser a **Candidatura Assistida**: um assistente digital que ajuda qualquer pessoa a preparar e preencher candidaturas a vagas pela internet, a partir de um perfil profissional reutilizável, com IA que só usa fatos aprovados e com confirmação explícita antes de qualquer ação relevante.

O marketplace não foi descartado. Ele continua como direção estratégica e foi para o **MVP 2**, junto com o acompanhamento centralizado de candidaturas e a expansão para outros portais.

## 2. Linha do tempo

| Quando | O que aconteceu | Onde está registrado |
|---|---|---|
| Até 05/10/2026 | Documentação do marketplace (DVP-E até a v12, DRP, DAT, DVS, backend em C#/.NET, ER, termos) e mockups do painel empresa/profissional | [`docs/arquivo/marketplace-v1/`](../arquivo/marketplace-v1/) |
| 05/10/2026 | Documento *Automação Assistida de Candidaturas — Documento de Visão do Produto* propõe um assistente de candidatura (Perfil Mestre, extensão, IA com revisão humana) | [`plano-automacao-candidaturas.pdf`](plano-automacao-candidaturas.pdf) |
| 05–06/10/2026 | Protótipo funcional: plataforma web local, extensão MV3 com painel lateral e IA local (Ollama, `qwen2.5:7b`), testado num portal de demonstração e numa vaga pública real; vídeos de apresentação | Repositório local do protótipo (ver [DAT §2](../03-arquitetura/dat.md#2-estado-atual-protótipo)) |
| 07/10/2026 | Pesquisa de validação de persona, arquétipo e dor em Google Forms via Apps Script | [`ferramentas/pesquisa/`](../../ferramentas/pesquisa/) |
| 12/10/2026 | Documento de visão de reposicionamento (MVP 1 × MVP 2) | [`ZYFT_visao_produto_reposicionamento_MVP1_MVP2.docx`](ZYFT_visao_produto_reposicionamento_MVP1_MVP2.docx) |
| 12/10/2026 | Reescrita de toda a documentação, ADRs, agentes de IA do time e pesquisa v2 | Este repositório |

## 3. Por que mudou

1. **A dor da candidatura é imediata e ampla.** Preencher os mesmos dados em vários sites, manter currículos consistentes e escrever respostas abertas afeta qualquer pessoa que busca emprego, de qualquer área ou senioridade. O marketplace depende de ter dois lados (empresas e profissionais) ao mesmo tempo; a Candidatura Assistida entrega valor a um lado só, desde o primeiro uso.
2. **Já existe um protótipo funcional.** O protótipo de 05–06/10 provou o núcleo técnico (de-para de campos, IA presa a fatos, revisão campo a campo). Validar com usuários reais custa pouco a partir dele.
3. **Menor risco de partida.** Marketplace exige liquidez, cobrança, reputação e moderação antes de gerar valor. O MVP 1 pode ser validado com poucas pessoas e um portal.
4. **O aprendizado alimenta o MVP 2.** O Perfil Mestre construído no MVP 1 é a mesma base de que o marketplace precisa (perfil unificado, competências, evidências).

## 4. O que muda e o que fica

| Tema | Marketplace (documentação antiga) | Candidatura Assistida (atual) |
|---|---|---|
| Público | Profissionais de TI e empresas contratantes | Qualquer pessoa que se candidata a vagas pela internet, de qualquer área, senioridade ou modalidade |
| Proposta de valor | Contratação PJ/CLT sem comissão, com reputação | Preparar e preencher candidaturas com base nas suas informações, revisando cada resposta e confirmando cada ação |
| Entrega 1 | Cadastro, oportunidades, matching, avaliações, assinatura | Perfil Mestre, importação de currículo, preenchimento assistido num primeiro portal, IA para textos livres, confirmação explícita |
| Personas | Júlia (iniciante PJ), Leonardo (experiente PJ/CLT), antipersona Roberto | Cinco personas de **necessidade** (ver [personas](../01-produto/personas.md)) |
| Monetização | Assinatura fixa por trilha (PJ, CLT, Dual) | A validar na pesquisa v2 (gratuito com limite, assinatura ou pacotes); sem cobrança no MVP 1 |
| Stack proposta | C#/.NET 8, PostgreSQL | TypeScript/Node.js em monorepo, PostgreSQL, IA open-weight self-hosted (ver ADRs 0003–0006) |
| Mockups | Painel empresa, painel profissional, mensagens | A refazer para o fluxo do MVP 1 (painel lateral da extensão e plataforma do Perfil Mestre) |

**Fica para o MVP 2:** marketplace bilateral, PJ/CLT, assinaturas sem comissão, matching e fila de prioridade, reputação e avaliações, painel central de candidaturas, histórico completo, lembretes, filtros, conectores adicionais e relatórios. A lista completa está no [roadmap](../01-produto/roadmap.md).

## 5. Regra de escopo

> Nenhuma funcionalidade do MVP 2 entra no MVP 1 sem uma decisão explícita de escopo, registrada como ADR ou como alteração aprovada do DVP-E.

## 6. Divergências entre o protótipo e o escopo do MVP 1

O protótipo foi feito antes do reposicionamento e implementa coisas que agora são MVP 2. Elas ficam no código como experimento, mas **não fazem parte do MVP 1** até haver decisão:

| No protótipo | Situação no MVP 1 | Decisão |
|---|---|---|
| Histórico de candidaturas com 10 status | MVP 2 (painel central) | [ADR-0012](../03-arquitetura/adr/0012-registro-minimo-de-conclusao-no-mvp1.md) — proposta: só confirmação visual mais um registro mínimo opcional |
| Host permissions para Maringá.Com, Gupy, LinkedIn e Workana | MVP 1 tem **um** portal suportado | [ADR-0011](../03-arquitetura/adr/0011-primeiro-portal-e-conectores-por-portal.md) |
| Arquétipo "Maratonista" com a mensagem "candidate-se a 10 vagas no tempo que levava para 1" | Candidatura em massa está **fora** do produto | Mensagem reescrita na pesquisa v2 (ver [plano de pesquisa](../05-pesquisa/plano-de-pesquisa.md)) |
| Servidor local de um usuário, token fixo | MVP 1 precisa de contas e pareamento seguro da extensão | ADRs [0007](../03-arquitetura/adr/0007-autenticacao-e-pareamento-da-extensao.md) e [0008](../03-arquitetura/adr/0008-postgresql-com-perfil-em-jsonb.md) |

## 7. Documentos antigos

Os documentos do marketplace foram movidos para [`docs/arquivo/marketplace-v1/`](../arquivo/marketplace-v1/) sem alteração de conteúdo. Eles servem de ponto de partida para o planejamento do MVP 2 e **não** descrevem o produto atual.
