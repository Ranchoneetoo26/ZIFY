# Métricas

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Métricas do MVP 1 |
| Versão | 1.0 |
| Status | **Candidatas** — metas numéricas só depois da pesquisa v2 |
| Dono | Product Manager |
| Documentos relacionados | [DVP-E §10](dvp-e.md#10-objetivos-do-mvp-1), [Roadmap](roadmap.md), [Observabilidade (ADR-0015)](../03-arquitetura/adr/0015-observabilidade-sem-dados-pessoais.md), [Plano de pesquisa](../05-pesquisa/plano-de-pesquisa.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | Métricas candidatas, forma de coleta sem dados pessoais e regra para fixar metas. |

## 1. Regra

> Nenhuma meta numérica é fixada antes da pesquisa v2 e das entrevistas. Os números servem para decidir, não para confirmar o que o time já acredita.

Depois da pesquisa, o PM propõe as metas, o time revisa e este documento ganha a versão 2.0 com as metas e a justificativa de cada uma.

## 2. North Star candidata

**Candidaturas concluídas com assistência e revisadas por semana.**

Uma candidatura conta quando, numa mesma página do portal suportado, a pessoa (1) leu o formulário com o Zyft, (2) revisou e preencheu pelo menos um campo pelo painel e (3) confirmou a conclusão. Ela mede valor entregue (uso real) sem incentivar volume sem revisão, porque exige revisão em cada candidatura.

Riscos da métrica: pode crescer com uso superficial (um campo só). Por isso é acompanhada das métricas de qualidade abaixo.

## 3. Métricas candidatas

| # | Métrica | Pergunta que responde | Como coletar | Objetivo (DVP-E) |
|---|---|---|---|---|
| M1 | Tempo por candidatura | O Zyft reduz o esforço? | Teste moderado: com e sem o Zyft, no mesmo formulário. No produto: tempo entre "Ler formulário" e "Concluído" | O1 |
| M2 | % de campos aceitos sem edição | O que propomos está certo? | Contagem por sessão no painel: aceitos, editados, desmarcados | O2 |
| M3 | Taxa de campos reconhecidos | O conector entende o portal? | Campos com proposta / campos visíveis não bloqueados, por versão do conector | O1, RNF-10 |
| M4 | Taxa de ambíguos e ausentes | Onde o perfil ou o de-para falham? | Contagem por estado no painel | O2 |
| M5 | Uso de sugestões de texto | A redação ajuda? | Sugestões usadas (com e sem edição) / sugestões geradas | O1 |
| M6 | Confiança declarada | A pessoa confia no que foi preenchido? | Pergunta de 1 a 5 ao final do fluxo, opcional, e entrevistas | O2 |
| M7 | Valores inventados pela IA | A IA inventa? | Suíte de evals no CI e amostra auditada do beta. Meta fixa: **zero** | O3 |
| M8 | Evidência real na análise de vaga | A análise é honesta? | Evals: % de "atende/parcial" com linha real do perfil. Meta fixa: **100%** | O3 |
| M9 | Conclusão do fluxo | A pessoa consegue terminar? | Sessões que chegam a "Concluído" / sessões que leram o formulário | O1 |
| M10 | Retenção do perfil | O Perfil Mestre vira a fonte de dados? | Pessoas que voltam a usar a extensão em outra candidatura em 30 dias | O4 |

M7 e M8 são as duas únicas metas fixadas desde já, porque são regras de produto (RNF-05), não hipóteses.

## 4. Coleta sem dados pessoais

- Eventos de produto carregam só: identificador pseudônimo da conta, identificador da sessão do painel, versão do conector, contagens e durações. **Nunca** valores de campos, rótulos com dados pessoais, URL completa da vaga ou texto gerado.
- O domínio do portal é registrado; o caminho da URL, não.
- A pergunta de confiança (M6) é opcional e não pede texto livre.
- A lista de campos permitidos nos eventos é a mesma dos logs técnicos ([ADR-0015](../03-arquitetura/adr/0015-observabilidade-sem-dados-pessoais.md)) e tem teste automatizado.
- O registro de ações visível para a pessoa (RF-20) é separado da telemetria e não alimenta métricas agregadas sem anonimização.
- A pessoa pode desligar a telemetria de produto nas configurações sem perder nenhuma funcionalidade.

## 5. Métricas da pesquisa v2

As métricas da pesquisa (média por dor, % de notas 4–5, escolha de perfil, disposição de uso) estão no [plano de pesquisa](../05-pesquisa/plano-de-pesquisa.md). Elas definem as metas desta página, mas não se misturam com as métricas de uso.
