# Changelog

Mudanças relevantes do projeto. O formato segue o [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/).

## [Não lançado]

### Alterado
- **Reposicionamento do produto** ([ADR-0001](docs/03-arquitetura/adr/0001-reposicionamento-mvp1-candidatura-assistida.md)): o MVP 1 passa a ser a Candidatura Assistida. O marketplace de TI (PJ/CLT), o painel central de candidaturas e os outros portais vão para o MVP 2.
- Toda a documentação foi reescrita: DVP-E 2.0, DRP 2.0, DAT, DVS, personas de necessidade (PN1–PN5), fluxos, roadmap e métricas.
- A pesquisa (Google Forms) foi reescrita para a v2 com foco na Candidatura Assistida. As respostas da v1 foram arquivadas na planilha.
- O `Codigo.gs` agora se vincula ao formulário existente e gera o relatório da v2. Respostas simuladas no formulário oficial deixaram de ser possíveis.

### Adicionado
- ADRs 0001 a 0016 e o template MADR em pt-BR.
- Arquitetura detalhada: backend, extensão, frontend web, IA, contrato da API v1, modelo de dados e modelo de ameaças.
- Estratégia de testes com evals de IA e critérios de aceite CA-01 a CA-22.
- Agentes de IA do time em `.claude/agents/` (CEO, PO, PM, arquitetos de software e de IA, devs backend, frontend e extensão, QA, designer UX, privacidade e segurança), `CLAUDE.md`, papéis e RACI, processo e templates de PR e issues.
- Ferramentas da pesquisa: `questionario-v2.json` como fonte de verdade, `migracao-v2.gs`, `aplicar_pesquisa_v2.py` e os geradores de Markdown.
- Rascunhos de termos de uso e política de privacidade para o MVP 1.

### Movido
- A documentação e os mockups do marketplace foram para `docs/arquivo/marketplace-v1/`, sem alteração de conteúdo.

### Segurança
- Foi registrada uma lacuna do protótipo: o crawler não exclui campos `password` ao ler o formulário ([ADR-0010](docs/03-arquitetura/adr/0010-sem-credenciais-sem-contorno-de-seguranca-campos-bloqueados.md)). A correção é obrigatória antes de usar o protótipo em portais reais.
