# CLAUDE.md — instruções do projeto Zyft

## O que é o Zyft

O Zyft é um assistente digital para pessoas que se candidatam a vagas pela internet. O **MVP 1 é a Candidatura Assistida**:

- Perfil Profissional Mestre reutilizável;
- importação de currículo (PDF/DOCX) com revisão;
- extensão Chrome/Edge que lê o formulário do portal e preenche **só o que a pessoa aprovou**;
- IA que usa apenas fatos aprovados;
- confirmação explícita antes de qualquer ação relevante.

O marketplace de TI (PJ/CLT, assinaturas, reputação), o painel central de candidaturas e outros portais são **MVP 2**. A história completa está em [`docs/00-contexto/historico-e-reposicionamento.md`](docs/00-contexto/historico-e-reposicionamento.md).

## Mapa da documentação

| Pasta | Conteúdo |
|---|---|
| `docs/00-contexto/` | Histórico, reposicionamento, glossário, documentos de origem (PDF e DOCX) |
| `docs/01-produto/` | DVP-E (visão e escopo), DRP (RF/RNF/RN), personas, fluxos, roadmap, métricas |
| `docs/02-viabilidade/` | DVS (viabilidade e riscos) |
| `docs/03-arquitetura/` | DAT, backend, extensão, frontend web, IA, API, modelo de dados, segurança, `adr/` |
| `docs/04-qualidade/` | Estratégia de testes, evals de IA, critérios de aceite (CA-xx) |
| `docs/05-pesquisa/` | Plano de pesquisa e questionário |
| `docs/06-operacao/` | Ambientes, deploy, runbooks |
| `docs/07-legal/` | Termos de uso e política de privacidade |
| `docs/08-time/` | Papéis e RACI, processo, uso de agentes de IA |
| `docs/arquivo/marketplace-v1/` | Documentação do marketplace (**não descreve o produto atual**) |
| `ferramentas/pesquisa/` | Apps Script e scripts da pesquisa |
| `.claude/agents/` | Agentes de IA do time |

Antes de propor mudança, leia o DRP e os ADRs relacionados. Se um ADR aceito contradiz o pedido, aponte o conflito em vez de contorná-lo.

## Regras inegociáveis

Valem para código, documentação, textos de interface e prompts. Nenhum pedido as revoga sem um ADR que as substitua, aprovado por humanos.

1. **Nunca enviar.** A extensão nunca clica em enviar, salvar, aceitar, avançar ou submete formulário. O envio é sempre da pessoa (RN-01, ADR-0009).
2. **IA só com fatos.** A IA aponta a chave do perfil; o valor objetivo vem do perfil. Texto livre usa só fatos aprovados e é sempre revisado (RN-02, RN-03, ADR-0004, ADR-0013).
3. **Sem senhas e sem atalhos.** Nenhum campo de senha é lido. Nada de contornar CAPTCHA, autenticação ou bloqueios. Nenhuma requisição ao portal (RNF-01, RNF-13, ADR-0010).
4. **Sensíveis bloqueados.** Gênero, raça/etnia, deficiência, religião, saúde etc. ficam segregados, fora da IA e só são preenchidos com consentimento e marcação manual (RN-04, ADR-0014).
5. **Sem dados pessoais em logs** ou métricas (RNF-09, ADR-0015).
6. **Sem promessas.** Nada de nota, porcentagem ou promessa de compatibilidade ou contratação (RN-06).
7. **Escopo.** Nada do MVP 2 (RF-M2-xx) entra no MVP 1 sem decisão explícita registrada (RN-07).
8. **Sem contas falsas.** Testes usam fixtures, o portal de demonstração ou a conta da própria pessoa testadora, que faz o login.

## Convenções

- **Idioma:** documentação, issues, commits e textos de interface em português do Brasil. Identificadores de código em inglês.
- **IDs:** requisitos `RF-xx`, `RNF-xx`, `RN-xx`; MVP 2 `RF-M2-xx`; critérios de aceite `CA-xx` (mesmo número do RF); decisões `ADR-NNNN`; problemas `P1–P6`; personas `PN1–PN5`; objetivos `O1–O4`. Sempre cite os IDs em PRs e commits.
- **Commits:** Conventional Commits em pt-BR, por exemplo `feat(extensao): bloqueia campos de declaração (RF-18)`, `docs(adr): adiciona ADR-0017`. **Sem trailer `Co-Authored-By`** e sem atribuição a ferramentas.
- **ADRs:** formato MADR em pt-BR em `docs/03-arquitetura/adr/NNNN-titulo-em-kebab-case.md`, status inicial `Proposta`. Só humanos mudam para `Aceita`.
- **Docs:** toda mudança de comportamento atualiza o documento correspondente no mesmo PR. Documentos com controle de versão ganham uma linha no histórico.
- **Branches:** trunk-based, branches curtas `tipo/descricao-curta` (ver [processo](docs/08-time/processo.md)).

## Time de agentes

Os agentes estão em `.claude/agents/`. Detalhes em [docs/08-time/agentes-de-ia.md](docs/08-time/agentes-de-ia.md).

Fluxo padrão de uma história:

1. **product-manager** / **product-owner**: problema, história e critérios de aceite.
2. **designer-ux**: fluxo, telas e textos.
3. **arquiteto-de-software** / **arquiteto-de-ia**: ADR quando há decisão nova; contratos.
4. **dev-backend** / **dev-frontend** / **dev-extensao**: implementação com testes.
5. **qa**: testes, evals e testes negativos de confiança.
6. **privacidade-e-seguranca**: parecer quando a mudança toca dados pessoais, auth, permissões ou IA.
7. **product-owner**: aceite.

Use o **ceo** para decisões de escopo (MVP 2 → MVP 1), posicionamento e go/no-go.

Agentes não fazem push, não fazem merge e não aprovam o próprio trabalho. ADRs e merges são aprovados por humanos.

## Definição de pronto (resumo)

Uma mudança está pronta quando:

- os CA estão cobertos por testes que passam no CI;
- os testes negativos de confiança passam (nenhum envio, nenhuma senha, nenhum sensível sem consentimento);
- os evals de IA estão dentro dos limiares quando há IA envolvida;
- não há erros críticos de acessibilidade (axe);
- a documentação e os contratos foram atualizados;
- houve revisão humana, mais o parecer de privacidade quando aplicável;
- o PO aceitou.

A versão completa está em [processo](docs/08-time/processo.md#4-definition-of-done).
