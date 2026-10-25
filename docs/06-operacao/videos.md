# Vídeos de apresentação do Zyft

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Vídeos de apresentação (v2) |
| Versão | 1.1 |
| Data | 21/10/2026 |
| Status | Em produção |
| Documentos relacionados | [Guia do usuário](guia-do-usuario.md), [Runbook local](runbook-local.md), [Por que o Zyft](../00-contexto/por-que-o-zyft.md), [Resultados da pesquisa v2](../05-pesquisa/resultados-v2.md), [Guias de estudo](../08-time/guias-de-estudo/README.md) |

> **Nota sobre os números (21/10/2026).** Os vídeos foram gravados quando a suíte tinha **223 testes** (136 de unidade, 75 de integração, 12 de aceitação) e a API tinha **76 operações**; é isso que aparece na tela e na narração. Depois da gravação entraram **7 testes**: 4 casos de unidade para as regras de LinkedIn e GitHub (`LinkedIn_e_GitHub_casam_por_regra_mesmo_com_camelCase`) e 3 de integração (`Limite_atingido_nunca_bloqueia_exportar_nem_excluir_os_dados`, `Extensao_que_se_desconecta_libera_a_vaga_de_navegador_e_nao_compra_pacote` e `Logs_das_rotas_v2_nao_contem_dados_pessoais`). A contagem atual é **252 testes, todos passando** (156 + 84 + 12, com os 22 das correções da gravação do tour), e a API tem **77 operações** com a nova `DELETE /extension/devices/current`. As listas abaixo mantêm os números da gravação.

Três vídeos estão sendo produzidos para a apresentação da v2. Cada um tem um público e uma pergunta principal. Os arquivos finais ficam fora do repositório, em `entregas/videos/` (pasta irmã de `ZIFY/`), porque são binários grandes.

## Resumo

| # | Vídeo | Pasta | Para quem | Pergunta que responde |
|---|---|---|---|---|
| 1 | Executivo | `entregas/videos/1-executivo/` | Banca, professores e quem avalia o produto sem ler código | Que problema o Zyft resolve, com que evidência, e como se sustenta? |
| 2 | Técnico | `entregas/videos/2-tecnico/` | Banca técnica, professores de engenharia e quem vai manter o código | Como o Zyft é construído e como sabemos que as regras de confiança funcionam? |
| 3 | Onboarding no sistema | `entregas/videos/3-onboarding-sistema/` | Pessoas usuárias, testadoras do beta e integrantes novos do time | Como eu uso o Zyft do cadastro até registrar uma candidatura? |

## 1. Vídeo executivo

**Público:** banca e avaliadores de produto. Não pressupõe conhecimento técnico.

**O que cobre:**

1. O problema: redigitar os mesmos dados em cada portal de vagas.
2. A pesquisa v2 (15 respostas válidas): redigitar dados com média 4,40 e 80% de notas 4 e 5; confiança 3,4 (meta 3,5); barreiras de preço, privacidade e poucos sites; disposição a pagar. Inclui a limitação da amostra (60% de tecnologia).
3. Personas: Rafael (primária), Bia (secundária) e a antipersona da candidatura em massa.
4. A solução: Perfil Mestre, importação de currículo revisada e extensão que preenche só o que a pessoa aprova, sem nunca enviar.
5. Demonstração curta: onboarding da Beatriz (área Saúde, perguntas adiadas), Perfil Mestre do Rafael, extensão no portal de demonstração e a candidatura registrada.
6. Confiança e privacidade: sensíveis trancados e fora da IA, nunca pedir senha, registro de ações, exportar e excluir.
7. Modelo de negócio: Gratuito com limites e anúncios com regras, Plus (R$ 9,90/mês ou R$ 99/ano), pacote (R$ 4,90) e recompensado. Pagamento simulado.
8. Como é feito, em uma cena (API .NET 8, web, extensão, IA local com Ollama, 223 testes na gravação; hoje 252) e próximos passos (pesquisa fora da tecnologia, beta fechado, primeiro portal real, MVP 2).

## 2. Vídeo técnico

**Público:** avaliação técnica e desenvolvedores. Pressupõe noções de API, banco e testes.

**O que cobre:**

1. Arquitetura: Clean Architecture em quatro projetos (`Zyft.Domain`, `Zyft.Application`, `Zyft.Infrastructure`, `Zyft.Api`), CQRS com MediatR e FluentValidation ([ADR-0017](../03-arquitetura/adr/0017-backend-em-csharp-dotnet-clean-architecture.md)).
2. Componentes: API em `/api/v1` (76 operações na gravação; hoje 77), web sem build servida pela API, extensão MV3 0.4.0 (service worker, content script, painel lateral) e IA atrás da porta `ILlmClient`.
3. O pipeline do de-para: bloqueios, regras determinísticas, campos personalizados (`custom:<id>`), IA que só aponta a chave e validação do valor contra o perfil ([ADR-0004](../03-arquitetura/adr/0004-ia-aponta-chave-nunca-escreve-valor.md), [ADR-0006](../03-arquitetura/adr/0006-regras-deterministicas-antes-da-ia.md)).
4. Segurança: JWT de 15 minutos e refresh com rotação, pareamento por código de 6 dígitos, sensíveis em AES-256-GCM, logs sem dados pessoais.
5. A v2 no código: planos e limites (402 `plan.limit_reached`), contagem idempotente por origem e caminho, anúncios por catálogo estático sem dado pessoal, onboarding e campos personalizados. Banco com 25 tabelas.
6. Qualidade: execução de `dotnet test` com 223 testes (136 de unidade, 75 de integração, 12 cenários de aceitação em Reqnroll), a contagem do dia da gravação; hoje são 252 (156 + 84 + 12) e os testes das regras inegociáveis.
7. Divergências conhecidas, ditas com honestidade (por exemplo, o [ADR-0012](../03-arquitetura/adr/0012-registro-minimo-de-conclusao-no-mvp1.md) e o status das candidaturas).

## 3. Vídeo de onboarding no sistema

**Público:** quem vai usar o Zyft pela primeira vez (beta) e integrantes novos do time. Segue o [guia do usuário](guia-do-usuario.md).

**O que cobre:**

1. Criar conta e passar pelo onboarding em passos: uma pergunta por tela, "Preencher mais tarde" sempre disponível, área profissional com sugestões.
2. Perfil Mestre: alerta de perguntas puladas, "Informações da sua área" (campos personalizados) e importação de currículo com revisão.
3. Banco de respostas, consentimentos e dados sensíveis.
4. Instalar e conectar a extensão com o código de pareamento.
5. No portal de demonstração: analisar a vaga, ler o formulário, revisar os estados ("Pronto", "Precisa de você", "Ação sua", "Sugestão da IA"), preencher só o aprovado e enviar por conta própria.
6. Concluir e ver a candidatura registrada; Planos, uso do mês e o que acontece ao atingir um limite.
7. Conta e privacidade: registro de ações, exportar e excluir.

## Formato e produção

Cada vídeo é entregue como:

- MP4 H.264, 1920×1080, 30 fps, áudio AAC 48 kHz com volume normalizado;
- uma segunda versão com legendas queimadas (`*-legendado.mp4`) e o arquivo `.srt` ao lado;
- `roteiro.md` com o texto de cada cena e a duração.

Narração em pt-BR com voz sintetizada. As telas são gravadas com Playwright em uma instância isolada da API (SQLite com seed limpo e porta própria), com os usuários de demonstração Rafael (`demo@zyft.local`) e Beatriz (`beatriz@demo.zyft`). Nenhum portal real nem dado real de pessoa aparece nos vídeos; os anúncios são do catálogo de demonstração e o pagamento é simulado.

Regras de conteúdo: os números citados são os da pesquisa v2 e da implementação descritos nestes documentos. Nada de número inventado nem promessa de recurso que não existe (RN-06, RN-07).

Os arquivos de trabalho (áudio por cena, gravações, slides, bancos temporários) ficam em `_trabalho/` dentro de cada pasta e não fazem parte da entrega.
