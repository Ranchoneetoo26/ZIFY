# DVS — Documento de Viabilidade de Software

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Viabilidade de Software (DVS) |
| Versão | 2.1 |
| Status | Aprovado para a Fase 0 |
| Dono | Product Manager (com Arquitetura) |
| Documentos relacionados | [DVP-E](../01-produto/dvp-e.md), [DRP](../01-produto/drp.md), [Roadmap](../01-produto/roadmap.md), [DAT](../03-arquitetura/dat.md), DVS antigo em [`arquivo/marketplace-v1/dvs.md`](../arquivo/marketplace-v1/dvs.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.x | até 05/10/2026 | Viabilidade do marketplace. Arquivado. |
| 2.0 | 12/10/2026 | Reescrita para a Candidatura Assistida, com base no protótipo de 05–06/10/2026. |
| 2.1 | 21/10/2026 | §4.3 atualizada: a pesquisa v2 respondeu às hipóteses de monetização, e o MVP 1 passa a ter plano gratuito com limites, Plus, pacote avulso e anúncios, com pagamento simulado ([ADR-0020](../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md)). Linha do time ajustada. |

## 1. Objetivo

Avaliar se o MVP 1 da Candidatura Assistida é viável técnica, operacional, econômica e legalmente, listar os riscos e definir os critérios de go/no-go do [roadmap](../01-produto/roadmap.md#7-marcos-de-decisão).

## 2. Viabilidade técnica

### 2.1 O que o protótipo já provou

O protótipo de 05–06/10/2026 (servidor Node.js local, extensão MV3 com painel lateral, IA local `qwen2.5:7b` via Ollama) foi testado num portal de demonstração local e numa vaga pública real.

| Capacidade | Situação no protótipo | Conclusão |
|---|---|---|
| Ler campos de formulários reais (label, aria, placeholder, texto vizinho, seção, modal) | Funciona no portal demo e em páginas comuns | Viável |
| De-para com regras PT/EN, incluindo blocos repetidos | Funciona; regras em milissegundos | Viável |
| IA aponta chave do perfil, valor vem do perfil | Implementado e validado | Viável; elimina a invenção de valores objetivos |
| Texto livre com fatos do perfil | Funciona; qualidade aceitável com 7B | Viável, com revisão humana obrigatória |
| Análise de vaga com evidência por linha numerada | Funciona; o 7B às vezes erra a classificação (ex.: "parcial" em vez de "atende") e o resultado varia entre execuções | Viável como Should; a evidência real torna o erro visível |
| Importação de PDF/DOCX para proposta de perfil | Funciona para PDF com texto e DOCX | Viável; PDF escaneado fora do MVP 1 |
| Garantias (nunca enviar, bloqueios, manter portal) | Implementadas | Viável |

### 2.2 O que o protótipo não provou

| Lacuna | Por que importa | Como reduzir |
|---|---|---|
| Portais reais calibrados | Os conectores do protótipo são genéricos | Spike F0.3 e [ADR-0011](../03-arquitetura/adr/0011-primeiro-portal-e-conectores-por-portal.md) |
| Iframes e shadow DOM | Alguns portais usam formulários embutidos | Avaliar no spike; fora do MVP 1 se o portal escolhido não exigir |
| Multiusuário e nuvem | Protótipo é local, um usuário, token fixo, JSON | ADRs [0003](../03-arquitetura/adr/0003-monorepo-typescript-node.md), [0007](../03-arquitetura/adr/0007-autenticacao-e-pareamento-da-extensao.md), [0008](../03-arquitetura/adr/0008-postgresql-com-perfil-em-jsonb.md) |
| IA servida para muitas pessoas | Ollama local não é servidor de produção | [ADR-0005](../03-arquitetura/adr/0005-modelo-open-weight-self-hosted-atras-de-porta-llm.md) |
| Latência | Análise de vaga levou 20–30 s no protótipo | Metas do RNF-04, paralelismo por requisito, progresso visível |

### 2.3 Complexidade por módulo (indicativa)

| Módulo | Complexidade | Observação |
|---|---|---|
| Conta, consentimento, exportação e exclusão | Baixa | Padrão de mercado |
| Perfil Mestre (web) | Média | Formulário extenso, listas repetidas, acessibilidade |
| Importação de currículo | Média | Extração e tela de revisão campo a campo |
| Pareamento e autenticação da extensão | Média | Segurança de tokens em MV3 |
| Crawler e conector do primeiro portal | **Alta** | Maior risco técnico; depende do portal |
| De-para (regras, IA, validação) | Média | Base pronta no protótipo |
| Texto livre e respostas aprovadas | Média | Evals de alucinação |
| Análise de vaga | Média | Should; pode sair do MVP 1 sem bloquear |
| Infraestrutura de IA | Média–alta | GPU, fila e custo |

### 2.4 Riscos técnicos

| Risco | Detalhe |
|---|---|
| Mudanças de layout do portal | Um redesenho quebra o conector sem aviso. Mitigação: fixtures gravadas, testes de contrato, métrica de campos reconhecidos com alerta (RNF-10). |
| Manifest V3 | Service worker efêmero, limites de permissões e revisão nas lojas. Mitigação: estado no painel e no `chrome.storage.session`, permissões mínimas, publicação antecipada como "não listada". |
| Latência e qualidade da IA | Modelos pequenos erram; modelos grandes custam mais e demoram mais. Mitigação: regras primeiro, IA só no que sobra, evals com limiares, porta LLM para trocar de modelo. |
| Componentes não padronizados | Selects customizados, datas com máscara, autocompletes. Mitigação: adaptadores por componente no conector. |

## 3. Viabilidade operacional

| Aspecto | Avaliação |
|---|---|
| Time | Pequeno e acadêmico. O escopo do MVP 1 (um portal, pagamento simulado sem cobrança real, sem marketplace) cabe se a Fase 0 fechar as decisões cedo. Agentes de IA apoiam papéis do time ([ADR-0016](../03-arquitetura/adr/0016-time-de-agentes-de-ia-no-desenvolvimento.md)). |
| Suporte | Beta fechado com poucas pessoas; canal direto (e-mail ou WhatsApp) com o time. |
| Manutenção do conector | Exige monitoramento contínuo do portal; definir responsável pelo conector. |
| Publicação da extensão | Chrome Web Store e Edge Add-ons exigem política de privacidade, justificativa de permissões e revisão; prever prazo de revisão das lojas. |

## 4. Viabilidade econômica

### 4.1 Custos principais do MVP 1

| Item | Natureza | Observação |
|---|---|---|
| Hospedagem da API, web e banco | Fixo mensal baixo | Plano pequeno de nuvem gerenciada |
| Inferência de IA | Variável, maior custo | Ver 4.2 |
| Contas de desenvolvedor das lojas | Taxa única (Chrome Web Store) | Edge Add-ons sem taxa |
| Domínio e e-mail transacional | Fixo baixo | Verificação de conta |

Os valores serão estimados com cotações reais na Fase 0; este documento não fixa números.

### 4.2 IA self-hosted × API gerenciada

| Critério | Modelo open-weight self-hosted (Ollama em dev, servidor de inferência em produção) | API gerenciada de terceiros |
|---|---|---|
| Privacidade | Currículo e perfil ficam na nossa infraestrutura | Dados vão a um operador; exige contrato, DPA e transferência internacional |
| Custo com pouco uso | GPU parada custa igual | Paga por uso; barato no beta |
| Custo com muito uso | Previsível; cai por requisição | Cresce linearmente |
| Qualidade | 7–8B suficiente para de-para; limitado na análise de vaga | Modelos maiores, melhor análise |
| Operação | Exige manter GPU, fila, atualização de modelos | Mínima |

Decisão em [ADR-0005](../03-arquitetura/adr/0005-modelo-open-weight-self-hosted-atras-de-porta-llm.md): porta LLM única com modelo open-weight self-hosted como padrão, preservando a possibilidade de trocar de provedor. O custo por candidatura assistida será medido no beta.

### 4.3 Monetização

- **Sem cobrança real no MVP 1.** O objetivo continua sendo validar valor e confiança. Os planos existem, mas o pagamento é simulado e a interface avisa (RN-13).
- **O que a pesquisa v2 respondeu** (n = 15, ver [resultados](../05-pesquisa/resultados-v2.md)): 60% preferem gratuito com limite mensal, 13% pacotes avulsos, 13% assinatura sem limite e 13% só usariam se fosse totalmente gratuito. Sobre preço, 20% não pagariam, 47% pagariam até R$ 10, 13% de R$ 10 a 20, 13% de R$ 20 a 40 e 7% mais de R$ 40. Preço é a barreira mais citada (47%).
- **Modelo adotado** ([ADR-0020](../03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md), proposto): Gratuito com limites mensais e anúncios rotulados; Plus a R$ 9,90/mês ou R$ 99/ano, sem anúncios e sem limites; pacote avulso de R$ 4,90; anúncio recompensado opcional. Detalhes, justificativas e estimativa de receita em [monetização](../01-produto/monetizacao.md).
- **A estimativa de receita é hipótese.** Com 1.000 pessoas ativas, os cenários vão de cerca de R$ 340 a R$ 1.350 por mês, antes de impostos, taxas e custo de IA. Não sustenta um servidor de IA dedicado nessa escala; a viabilidade depende de escala e do custo de inferência por candidatura, ainda a medir (§4.2).
- **Riscos novos:** política de dados das lojas de extensões para anúncios (revisão de conformidade antes de publicar); limite gratuito baixo para quem se candidata muito; disposição declarada que não vira compra.
- O modelo de assinatura sem comissão do marketplace continua no MVP 2 e não foi reaproveitado.

### 4.4 Conclusão econômica

O MVP 1 é barato de operar no tamanho de um beta. O custo que define a viabilidade em escala é a inferência de IA por candidatura. Os preços da v2 saíram da disposição declarada na pesquisa, não do custo; antes de qualquer cobrança real, o custo por candidatura medido no beta precisa caber no preço.

## 5. Viabilidade legal

| Tema | Avaliação | Ação |
|---|---|---|
| LGPD | Tratamos dados pessoais e, potencialmente, sensíveis. Viável com consentimento por finalidade, minimização, segregação de sensíveis, direitos do titular e RIPD. | Política de privacidade, RIPD, encarregado nomeado ([política](../07-legal/politica-de-privacidade.md)) |
| Termos de uso dos portais | Muitos portais proíbem automação, robôs e scraping. O Zyft atua só na página aberta pela pessoa, sem requisições próprias e sem envio automático, mas a interpretação varia por portal. | Leitura dos termos de cada portal candidato no spike; escolher um portal sem impedimento ([ADR-0011](../03-arquitetura/adr/0011-primeiro-portal-e-conectores-por-portal.md)) |
| Políticas das lojas de extensões | Exigem uso limitado de dados, divulgação clara e permissões mínimas | Seguir a política de "uso limitado" da Chrome Web Store |
| Responsabilidade pelo conteúdo | A pessoa revisa e envia; a IA não inventa | Termos de uso deixam claro que a pessoa é responsável pelo que envia ([termos](../07-legal/termos-de-uso.md)) |

## 6. Riscos consolidados

Probabilidade e impacto: Baixa (B), Média (M), Alta (A).

| # | Risco | Prob. | Impacto | Mitigação | Dono |
|---|---|---|---|---|---|
| R1 | Nenhum portal viável tecnicamente e nos termos de uso | M | A | Spike com 2–3 candidatos; portal com formulário HTML padrão; contato com o portal se necessário | Arquitetura |
| R2 | Layout do portal muda e quebra o conector | A | M | Fixtures, testes de contrato, alerta de queda de reconhecimento, conector versionado | Dev extensão |
| R3 | IA inventa um valor ou evidência | M | A | IA só aponta chave; evidência por linha; evals no CI com gate de zero | Arquiteto de IA, QA |
| R4 | Latência da IA prejudica o fluxo | M | M | Regras primeiro; paralelismo; progresso visível; degradação para só regras | Dev back |
| R5 | Custo de GPU alto para o uso do beta | M | M | Escalar a zero fora do horário; avaliar API gerenciada atrás da porta LLM | Arquitetura |
| R6 | Vazamento de dados pessoais | B | A | Criptografia, sensíveis segregados, logs sem PII, revisão de segurança | Privacidade e segurança |
| R7 | Pesquisa não valida as dores do núcleo | M | A | Marco 0 com no-go explícito; entrevistas para descobrir a dor real | PM |
| R8 | Rejeição na Chrome Web Store | B | M | Permissões mínimas, política clara, envio antecipado | Dev extensão |
| R9 | Pessoas não confiam em extensão que lê formulários | M | A | Transparência no painel, nada sem aprovação, registro de ações, comunicação clara | PO, UX |
| R10 | Escopo do MVP 2 invade o MVP 1 | M | M | RN-07; ADR obrigatório; revisão de escopo a cada incremento | PO |
| R11 | Time pequeno não fecha o MVP 1 no calendário | M | M | Fase 0 curta; análise de vaga (Should) cortável; agentes de IA de apoio | PM |

## 7. Critérios de decisão (go/no-go)

| Marco | Go | No-go |
|---|---|---|
| Marco 0 (fim da Fase 0) | Ao menos uma dor do núcleo validada; um portal viável; ADRs da Fase 0 decididos | Nenhuma dor do núcleo validada **ou** nenhum portal viável: rever proposta de valor antes de construir |
| Marco 1 (fim do beta) | Critérios de saída S1–S6 do [roadmap](../01-produto/roadmap.md#5-critérios-de-saída-do-mvp-1-marco-1) | S2 ou S3 falham: não sai, independentemente do resto |

## 8. Recomendação

**Seguir para a Fase 0.** O núcleo técnico já foi demonstrado no protótipo e o MVP 1 tem custo baixo. Os dois riscos que podem invalidar o MVP 1 (R1, portal viável, e R7, dor validada) são baratos de testar antes de construir, por isso a Fase 0 vem antes do Incremento 1.
