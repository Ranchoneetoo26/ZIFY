# ADR-0011 — Primeiro portal e conectores por portal

| Campo | Valor |
|---|---|
| Status | Proposta |
| Data | 12/10/2026 |
| Decisores | Arquiteto de software, Product Owner |
| Consultados | Product Manager, Dev extensão, QA, Responsável por privacidade (termos de uso) |
| Requisitos | RF-10, RF-11, RF-12, RF-16; RNF-10, RNF-13 |
| Relacionados | ADR-0002, ADR-0006, ADR-0010 |

## Contexto

O DVP-E define que o MVP 1 tem **um** portal suportado, escolhido após validação técnica (ponto V1). O documento de origem cita o **Maringá.Com** como possibilidade inicial, mas o reposicionamento diz explicitamente que essa escolha não é definitiva.

O protótipo é genérico:

- `host_permissions` fixas para Maringá.Com, Gupy, LinkedIn e Workana, além de localhost;
- regras de de-para sem nada específico de portal;
- foi testado no portal de demonstração local (`/demo/`) e na leitura de uma vaga pública real da Gupy, mas não foi calibrado em nenhum portal real (limitação registrada no README do protótipo).

## Fatores de decisão

- **Termos de uso:** o portal não pode proibir extensões ou preenchimento assistido feito pela própria pessoa.
- **Estrutura técnica:** formulários HTML legíveis, sem iframes de terceiros, canvas nem shadow DOM fechado.
- **Volume de vagas e público diverso:** áreas, senioridades e regiões variadas (o produto não é só para TI).
- **Tamanho do formulário:** quanto mais campos repetidos, maior o valor demonstrado (P1, P4).
- **Estabilidade do HTML** ao longo do tempo.
- **Acesso para testes:** a pessoa testadora consegue criar a própria conta e fazer login.

## Opções consideradas

### Quanto ao portal (candidatos para o spike)

| Candidato | Prós | Contras / a verificar |
|---|---|---|
| Maringá.Com (portal regional) | Citado no documento de origem; cadastro de currículo longo, com muitos blocos repetidos; público diverso e não técnico | Volume regional; verificar os termos e a estrutura atual |
| Gupy (páginas de candidatura das empresas) | Volume nacional muito alto; mesma plataforma para muitas empresas; vaga pública já lida no protótipo | Perguntas de triagem variam por empresa; possível uso de componentes dinâmicos; verificar os termos |
| Sites de carreira de empresas com formulário próprio | Diversidade | Cada site é diferente; não escala para um "primeiro portal" |
| LinkedIn | Volume altíssimo | Termos restritivos sobre automação e extensões; "candidatura simplificada" em modal; risco à conta da pessoa. **Não recomendado** para o MVP 1. |

### Quanto à arquitetura do conector

#### Opção A — Só regras genéricas

- **Prós:** funciona "em qualquer lugar".
- **Contras:** qualidade imprevisível; impossível garantir a taxa de reconhecimento num portal; sem testes de regressão.

#### Opção B — Conector inteiramente específico, sem regras genéricas

- **Prós:** precisão máxima.
- **Contras:** reescrita a cada portal; quebra a qualquer mudança de layout.

#### Opção C — Conector por portal sobre o motor genérico

O conector contém:

- reconhecimento de páginas (URL e marcadores no DOM), com as ações disponíveis em cada uma;
- regras específicas de campos (seletor ou rótulo → chave);
- campos que disparam navegação ou envio, tratados como "ação sua";
- fixtures HTML gravadas de cada etapa.

O motor genérico ([ADR-0006](0006-regras-deterministicas-antes-da-ia.md)) cobre o resto.

- **Prós:** precisão no portal suportado e degradação aceitável fora dele; regressão testável; versionado.
- **Contras:** manutenção por portal; as fixtures precisam ser atualizadas quando o portal muda.

## Decisão (proposta)

1. **Arquitetura:** **Opção C**. Cada conector é um módulo em `packages/connectors/<portal>/` com `manifest` (domínios, páginas, versão), regras, lista de campos perigosos e fixtures em `fixtures/<data>/<etapa>.html`.
2. **Escolha do portal:** spike de até **1 sprint** avaliando Maringá.Com e Gupy (LinkedIn excluído) pelos critérios acima, com uma matriz de pontuação registrada como anexo deste ADR. A decisão final do portal vira a seção "Decisão final" deste ADR, que então passa a **Aceita**.
3. **Permissões:** no MVP 1, `host_permissions` fixas contêm só o portal escolhido e a API do Zyft. Outros sites só com `optional_host_permissions` sob pedido explícito da pessoa, e o painel avisa: "Este site não é suportado oficialmente; revise com atenção."
4. **Coleta de fixtures:** as páginas são gravadas pela própria pessoa testadora, logada na própria conta, com os dados pessoais substituídos por dados fictícios **antes** de entrar no repositório. Contas falsas em portais reais não são criadas (decisão do time de 06/10/2026).
5. **Monitoramento:** taxa de campos reconhecidos por etapa do portal (sem valores). Uma queda acima de 20% em relação à semana anterior abre um alerta de "conector possivelmente desatualizado".

## Consequências

**Positivas**
- Qualidade mensurável no portal do MVP 1.
- Expandir para outro portal (MVP 2, RF-M2-03) vira "escrever um conector", não mudar a arquitetura.

**Negativas**
- Até o spike terminar, histórias que dependem do portal ficam bloqueadas.
- O Zyft fica dependente da estabilidade de um único portal no MVP 1.

**Riscos e mitigação**
- **Os termos do portal escolhido proíbem o uso.** Mitigação: análise dos termos faz parte do spike; o portal com proibição é eliminado.
- **O portal muda o layout durante o beta.** Mitigação: fixtures datadas, alerta de reconhecimento, conector com versão e *hotfix* rápido da extensão.

## Conformidade

- O CI roda os testes do conector contra todas as fixtures da versão atual.
- O PR que altera `manifest.json` para incluir domínio novo exige referência a este ADR ou a um novo.
- Fixtures passam por verificação automática de dados pessoais (CPF, e-mail e telefone reais não podem aparecer).

## Links

- [Extensão](../extensao.md), [Estratégia de testes](../../04-qualidade/estrategia-de-testes.md)
- Protótipo: `extension/manifest.json`, `server/public/demo/`
