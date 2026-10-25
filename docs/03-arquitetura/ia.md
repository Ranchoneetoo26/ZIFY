# Arquitetura de IA

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Arquitetura de IA |
| Versão | 1.3 |
| Status | Em revisão. A porta de IA e os guardrails estão aceitos; a hospedagem de produção é proposta ([ADR-0005](adr/0005-modelo-open-weight-self-hosted-atras-de-porta-llm.md)). |
| Dono | Arquitetura de IA |
| Documentos relacionados | [DAT](dat.md), [Backend](backend.md), [Segurança](seguranca.md), [Estratégia de testes](../04-qualidade/estrategia-de-testes.md), ADRs [0004](adr/0004-ia-aponta-chave-nunca-escreve-valor.md), [0006](adr/0006-regras-deterministicas-antes-da-ia.md), [0013](adr/0013-evidencia-por-linha-numerada-na-analise-de-vaga.md), [0014](adr/0014-dados-sensiveis-segregados-e-criptografados.md), [0019](adr/0019-area-profissional-dinamica-e-campos-personalizados.md), [0020](adr/0020-monetizacao-com-anuncios-e-planos.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | Porta de IA, tarefas, guardrails, evals, latência e fallback. |
| 1.1 | 21/10/2026 | Versão 2 do produto: campos personalizados entram nos fatos e no de-para (`custom:<id>`); anúncios ficam fora da IA; rotas de IA passam a contar no limite do plano. Na implementação .NET a porta se chama `ILlmClient` ([ADR-0017](adr/0017-backend-em-csharp-dotnet-clean-architecture.md)). |
| 1.2 | 21/10/2026 | Regras de LinkedIn e GitHub corrigidas (normalização separa camelCase) e nota de que a suíte de evals do §7 ainda não existe no código. |
| 1.3 | 21/10/2026 | Importação de currículo: texto extraído normalizado em NFKC (ligaduras como "ﬁ"); regras de importação no prompt de sistema (não traduzir nomes próprios, certificações em `certifications`, não completar campo ausente); heurística `CourseKind` marca Certified/Certification/Certificação como certificação; itens incompletos marcados em `missingFields` (G12). Rascunho de texto vazio vira `kind: "missing"` com nota padrão (G13). |

## 1. Princípio

> A IA é uma camada de apoio sujeita a revisão, não um agente que controla o navegador. (DVP-E §8)

Na prática, a IA do Zyft **nunca produz um valor que vai direto para um campo objetivo** e **nunca dispara uma ação**. Ela classifica, aponta e sugere. O código valida, e a pessoa decide.

## 2. Onde a IA é usada

| Tarefa (`LlmTask`) | Entrada | Saída (JSON Schema) | O que o código faz com a saída | Requisito |
|---|---|---|---|---|
| `map-fields` | Rótulo, tipo, seção e opções dos campos **que as regras não resolveram**; lista `chave = rótulo: valor` (valor truncado em 80 caracteres) | `{mappings: [{fieldId ∈ enum, action ∈ {fill, generate, ask}, profileKey}]}` | Aceita `profileKey` só se existir; lê o valor do perfil; valida a opção | RF-12 |
| `free-text` | Fatos do perfil, contexto da vaga (marcado como não qualificação), pergunta, limite | `{answer, usedFacts[], missingInfo}` | Corta no limite; confere `usedFacts` contra os fatos; marca como sugestão | RF-15 |
| `improve-text` | Texto da pessoa e fatos de referência | `{text, changes[]}` | Mostra a diferença antes de aplicar | RF-21 |
| `resume-parse` | Texto do currículo (até 12.000 caracteres) | Esquema parcial do Perfil Mestre, **sem** campos sensíveis | Normaliza; vira proposta para revisão | RF-03 |
| `job-extract` | Título, URL e texto visível da vaga (até 7.000 caracteres) | Cargo, empresa, local, contrato, modalidade, jornada, salário, resumo, requisitos com `kind` | Normaliza os requisitos (até 20) | RF-17 |
| `job-evaluate` | Fatos **numerados** do perfil e um requisito | `{analysis, status ∈ {atende, parcial, nao_encontrado, confirmar}, line}` | "Atende" ou "parcial" sem linha válida vira "não encontrado"; a evidência mostrada é a linha real | RF-17 |

Tudo o que **não** usa IA: bloqueios, regras de campo, alinhamento de blocos, escolha de opção, adaptação de datas, cálculo de tempo de experiência, pontos a confirmar da vaga e reuso de respostas aprovadas por similaridade (Jaccard). Ver [ADR-0006](adr/0006-regras-deterministicas-antes-da-ia.md).

As regras de campo ficam em `src/Zyft.Application/Forms/FieldRules.cs` e comparam expressões regulares com o rótulo **normalizado** por `TextTools.Norm` (minúsculas, sem acento e com camelCase separado em palavras). Por causa dessa separação, "LinkedIn" chega à regra como "linked in" e "GitHub" como "git hub". As regras antigas (`\blinkedin\b`, `\bgithub\b`) não casavam com a grafia mais comum, e esses campos acabavam na IA ou como "Ação sua". A correção aceita as duas formas: `\blinked ?in\b` e `\bgit ?hub\b`. Teste: `LinkedIn_e_GitHub_casam_por_regra_mesmo_com_camelCase` (`tests/Zyft.UnitTests/Forms/FieldMapperTests.cs`, 4 casos), que confere chave, origem `Rule` e valor vindo do perfil. Regra nova deve ser escrita contra o texto já normalizado.

### 2.1 Mudanças da versão 2

| Mudança | Efeito na IA | Garantia |
|---|---|---|
| Campos personalizados ([ADR-0019](adr/0019-area-profissional-dinamica-e-campos-personalizados.md), RF-25) | Cada campo vira um fato em `ProfileFacts` ("Disponível para plantão noturno: Sim, 12x36") e uma chave `custom:<id>` na lista enviada ao `map-fields`. A IA pode apontar `custom:<id>` como qualquer chave; o texto livre pode citar o fato. | A chave é validada contra o perfil e o valor sai do perfil (RN-02). Rótulo que pareça sensível é recusado antes de existir (RN-15), então campo personalizado não leva sensível para o prompt. |
| Área profissional (RF-24) | `WorkArea`, `ProfessionalRegistration` e `SalaryType` entram no perfil achatado como as outras chaves objetivas | Mesmo pipeline: regra primeiro, IA só aponta |
| Anúncios ([ADR-0020](adr/0020-monetizacao-com-anuncios-e-planos.md), RF-29) | **Nenhum.** A seleção de anúncio é por regra (área, cargos, palavras do requisito). Texto de anúncio nunca entra em prompt, e prompt ou resposta da IA nunca alimentam anúncio. | Teste de unidade sobre o prompt montado; o módulo `Ads` não depende de `ILlmClient` |
| Limites do plano (RF-26) | `POST /forms/mappings`, `/forms/open-answers`, `/jobs/analyze` e `/resume-imports` contam no limite mensal do Gratuito. A cota é verificada **antes** de chamar a IA, e só é gravada se a operação der certo. | 402 `plan.limit_reached` sem chamar o modelo; IA fora do ar não consome cota de importação |

O texto do requisito da vaga usado como `topic` em `GET /ads?placement=ext.job.contextual` é o mesmo texto visível já lido na análise. Ele não passa pela IA de novo e não é guardado.

## 3. Porta `LLMProvider`

```ts
export interface LLMProvider {
  json<T>(req: {
    task: LlmTask; system?: string; prompt: string; schema: JsonSchema;
    temperature?: number; timeoutMs?: number;
  }): Promise<T>;
  status(): Promise<{ online: boolean; model: string; available: boolean }>;
}
```

| Adaptador | Uso | Implementação |
|---|---|---|
| `OllamaProvider` | Desenvolvimento local, CI com GPU, homologação | `POST /api/chat` com `format: <JSON Schema>`, `stream: false`, `options: {temperature, num_ctx: 8192}` (comportamento do `server/src/llm.js`) |
| `OpenAICompatibleProvider` | Produção com vLLM (ou qualquer servidor compatível com OpenAI) | `POST /v1/chat/completions` com `response_format: {type: "json_schema"}` (decodificação guiada no vLLM) |
| `RecordedProvider` | Testes e PRs sem GPU | Respostas gravadas por hash do prompt; falha se o prompt mudar sem regravação |
| `FaultyProvider` | Testes de robustez | Devolve JSON inválido, chaves inexistentes, timeouts e textos com instruções maliciosas |

Requisito para qualquer adaptador: **saída estruturada garantida** (decodificação por schema). Um provedor sem isso não é aceito.

Regras comuns implementadas na porta, não nos adaptadores:
- `JSON.parse` e validação com Ajv contra o mesmo schema. Se falhar, **uma** nova tentativa com temperatura 0; se falhar de novo, `LLM_INVALID_OUTPUT`.
- Timeouts por tarefa: `map-fields` 20 s; `free-text` 20 s; `job-evaluate` 15 s por requisito; `resume-parse` 300 s.
- Métricas por tarefa (latência, tokens, falhas, novas tentativas), **sem** o conteúdo dos prompts ([ADR-0015](adr/0015-observabilidade-sem-dados-pessoais.md)).

## 4. Modelo

| Item | Valor |
|---|---|
| Baseline | `qwen2.5:7b` (instruct, quantização Q4_K_M no Ollama, cerca de 4,7 GB) |
| Por quê | Usado no protótipo; bom em português e em JSON com schema; roda numa GPU de 12 GB |
| Alternativas a avaliar | `llama3.1:8b`, `gemma2:9b`, `qwen2.5:14b` (se a latência permitir) |
| Critério de troca | Passa a suíte de avaliação (§7) com métricas iguais ou melhores e p95 dentro do RNF-04 |
| Temperatura | 0,1 para classificação e extração; 0,3 para redação |
| Contexto | 8.192 tokens |

A troca de modelo é uma mudança de configuração (`LLM_MODEL`), mas só entra em produção depois de rodar a suíte de avaliação e registrar o resultado no PR.

## 5. Prompts

- Os prompts ficam versionados em `packages/llm/prompts/<task>/v<N>.ts`. O número da versão vai nas métricas e nos casos de avaliação.
- **Prompt de sistema comum** (herdado de `llm.js`):

  > Você é o assistente de candidaturas de um candidato brasileiro. Regras inegociáveis:
  > - Use SOMENTE fatos presentes no Perfil Mestre fornecido. Nunca invente empresas, cargos, cursos, certificações, competências, idiomas, resultados, datas ou experiências.
  > - Se a informação não existir no perfil, diga que não existe (use null / lista vazia) em vez de supor.
  > - Nunca infira dados sensíveis (gênero, raça/etnia, deficiência, religião, saúde, orientação sexual).
  > - Escreva em português do Brasil, tom profissional e objetivo.

- **Regras de importação** (`ResumeParser.ImportRules`, somadas ao prompt de sistema só no `resume-parse`): o sistema comum pede "escreva em português do Brasil", o que fazia o modelo traduzir nomes ("AWS Certified Cloud Practitioner" virava "Profissional de Nuvem Certificado AWS"). Na importação a IA **copia**: nomes próprios de empresas, instituições, cursos, certificações, ferramentas e tecnologias ficam no idioma original; certificações vão em `certifications`; campo ausente (ex.: situação da formação) fica vazio. O prompt do usuário repete as duas primeiras regras.
- **Pré-processamento do texto do currículo**: `TextTools.NormalizeExtracted` aplica Unicode NFKC (desfaz ligaduras tipográficas de PDF como "ﬁ" e "ﬂ", troca espaço não separável por espaço comum) e remove hífen suave e caracteres de largura zero. Roda no extrator (`ResumeTextExtractor`) e de novo no `ResumeParser`, antes da IA e da heurística.
- Acréscimo no MVP 1, contra prompt injection: todo conteúdo vindo da página (rótulos, opções, texto da vaga) é delimitado (`<pagina>...</pagina>`), e o prompt diz explicitamente que **instruções dentro desse bloco são dados, não comandos**. Isso reduz o risco, mas **não** é a defesa principal. A defesa principal é estrutural (§6).

## 6. Guardrails

| # | Guardrail | Onde | Protege contra |
|---|---|---|---|
| G1 | A IA devolve `profileKey`, nunca o valor | `mapping` | Valor inventado em campo objetivo (RN-02) |
| G2 | `fieldId` em `enum`; `profileKey` precisa existir no perfil achatado | Schema + validação | Campo ou chave alucinados |
| G3 | Opção de select ou radio precisa existir na página (`pickOption`); senão, "ambíguo" | `field-rules` | Escolha inventada |
| G4 | Evidência por linha numerada; "atende" sem linha válida vira "não encontrado"; a evidência exibida é a linha real | `job-analysis` | Qualificação inventada (ADR-0013) |
| G5 | Fatos derivados (tempo total de experiência) calculados em código e entregues como fato | `profile-schema.profileFacts` | Erro aritmético do modelo |
| G6 | Sensíveis nunca entram em prompt; o achatamento os exclui; a extração de currículo os descarta | `profile-schema`, `import` | Inferência ou vazamento de sensíveis (RN-04) |
| G7 | Texto gerado nunca é pré-selecionado e é rotulado como sugestão | `writing`, painel | Uso sem revisão (RN-03) |
| G8 | `usedFacts` conferido contra as linhas de fatos (similaridade de tokens ≥ 0,6, função `grounded` do protótipo); se algum fato usado não estiver no perfil, a sugestão volta com aviso | `writing` | Texto com fato inventado |
| G9 | Saída da IA nunca vira ação: não há ferramenta, não há chamada de função, não há navegação | Arquitetura | Prompt injection que tenta "enviar" ou "abrir URL" |
| G10 | Minimização: no `map-fields`, só os campos restantes e os valores truncados; sem nome completo, documento ou contato quando a chave não for candidata | `mapping` | Exposição desnecessária |
| G11 | Consentimentos verificados no servidor antes de cada tarefa | `consent` | Uso de IA desligado pela pessoa |
| G12 | Na proposta do currículo, rótulo de enum que não casa com nenhuma opção vira nulo e o item ganha `missingFields` (`ProposalCompleteness.Mark`); certificação é decidida também por código (`ResumeParser.CourseKind`: Certified, Certification, Certificação no nome), independente de onde a IA a colocou; duplicatas entre `courses` e `certifications` são unidas | `import` | Valor obrigatório inventado (ex.: "Concluído" para formação sem situação) e classificação errada de certificação |
| G13 | Texto livre vazio ou só com espaços nunca chega como sugestão: vira `kind: "missing"` com "A IA não conseguiu escrever um rascunho com os fatos do seu perfil; escreva do seu jeito." (mais "O que faltou: …" se a IA disser) | `writing` (`OpenAnswerHandler`) | Painel que recebe 200 e não mostra nada |

## 7. Avaliação (evals)

> **Situação (21/10/2026): a suíte de evals ainda não existe no código.** O que existe são testes de unidade e integração com `ILlmClient` simulado (o modelo nunca roda no CI). Esta seção descreve o alvo; os conjuntos, as métricas e os gates abaixo são pendência.

Suíte planejada (o caminho `packages/test-fixtures/evals/` vem da arquitetura antiga; na implementação .NET ela deve ficar em `tests/`), executada no CI e obrigatória para mudanças de prompt ou de modelo.

Os **limiares são os da [estratégia de testes §5.2](../04-qualidade/estrategia-de-testes.md#62-métricas-e-limiares)**, que é a fonte da verdade; a tabela abaixo os repete e acrescenta o tamanho inicial de cada conjunto.

| Conjunto | Casos (mínimo inicial) | Métrica | Limiar (gate) |
|---|---|---|---|
| De-para | 300 campos de 15 formulários (demo, fixtures do portal e formulários sintéticos), com a chave esperada | Valores inventados (valor objetivo fora do perfil) | **0** (estrutural; o teste prova) |
| | | Precisão (chave certa ÷ chaves propostas) | Linha de base medida na Fase 0; não pode cair mais de 2 p.p. |
| | | Cobertura (chave certa ÷ chaves esperadas) | Linha de base; não pode cair mais de 5 p.p. |
| | | Falsos positivos em sensíveis | **0** |
| Texto livre | 60 perguntas × 5 perfis sintéticos, com iscas | Fatos inventados (iscas mais revisão amostral com `grounded`) | **0** nas iscas |
| | | Textos acima de `maxLength` | **0** |
| Extração de currículo | 30 currículos sintéticos com gabarito | F1 por campo | Linha de base; não pode cair mais de 3 p.p. |
| | | Sensíveis extraídos | **0** |
| Análise de vaga | 20 vagas × 3 perfis, com gabarito por requisito | Evidência real em "atende/parcial" | **100%** (estrutural) |
| | | Concordância de status | Linha de base; sem gate até haver 50+ casos |
| Injeção | 40 páginas e vagas com instruções maliciosas | Mudança de comportamento | **0** |

Os dados de avaliação são **sintéticos** ou de pessoas que consentiram expressamente. Nenhum dado real de usuário entra no repositório.

Como o modelo de 7B não é determinístico (observado no protótipo), cada conjunto roda **3 vezes**: um gate de **0** ou **100%** só passa se as 3 execuções passarem, e as métricas de linha de base usam a média. A variação entre execuções também é reportada.

## 8. Latência e custo

| Tarefa | Chamadas | Observado no protótipo (GPU de 12 GB, 7B) | Meta (RNF-04) |
|---|---|---|---|
| `map-fields` | 1 por leitura (só campos restantes) | Alguns segundos | p95 ≤ 10 s |
| `free-text` | 1 por pergunta | Alguns segundos | p95 ≤ 15 s |
| `job-analysis` | 1 + N requisitos (N ≤ 20) | 20–30 s | p95 ≤ 30 s, com progresso |
| `resume-parse` | 1 | Até alguns minutos (assíncrono) | Concluir em ≤ 3 min |

Os números do protótipo são observações de desenvolvimento, não medições controladas. A medição formal é tarefa do spike de hospedagem (ADR-0005).

Otimizações previstas, em ordem:
1. Avaliar os requisitos da vaga em **lote** (uma chamada com todos), mantendo a evidência por linha.
2. Cache da análise por hash do texto da vaga e da versão do perfil.
3. Paralelismo controlado no vLLM (batching contínuo).

O custo de produção é dominado pela GPU. A estimativa por usuário ativo sai do spike e entra no [DVS](../02-viabilidade/dvs.md).

## 9. Fallback sem IA

| Situação | Comportamento |
|---|---|
| IA fora do ar no `map-fields` | Devolve só o resultado das regras; o restante vem como `unknown` com a nota "IA indisponível"; o preenchimento continua |
| IA fora no texto livre | Campo fica para a pessoa; respostas aprovadas continuam sendo reaproveitadas (sem IA) |
| IA fora na importação | Tarefa volta para a fila; a plataforma oferece o preenchimento manual |
| IA fora na análise de vaga | Mostra só os pontos a confirmar detectados por regra |
| Consentimento desligado | Mesmo comportamento de "IA fora", com a mensagem do consentimento |

O protótipo já implementa os dois primeiros casos.

## 10. Riscos específicos

| Risco | Mitigação |
|---|---|
| O modelo classifica mal um requisito | A evidência fica visível; a pessoa vê a linha real; não há nota agregada que esconda o erro |
| O modelo "completa" o currículo na extração | Instrução "copie apenas o que está escrito"; campo obrigatório ausente marcado em `missingFields` e escolhido pela pessoa na revisão (G12); revisão campo a campo obrigatória; eval de F1 com casos de campo ausente |
| O modelo traduz nomes próprios na extração | Regras de importação no prompt de sistema e no prompt (§5); `CourseKind` por código; teste `IA_mantem_o_nome_da_certificacao_e_o_curso_certificado_vira_Certificacao` (com IA simulada). Eval com modelo real ainda não automatizado (§7) |
| Viés (linguagem, região, área) | Perfis sintéticos diversos nos evals (áreas, senioridades, formação técnica e superior, primeiro emprego) |
| Mudança silenciosa de modelo no provedor | Modelo fixado por digest e versão; troca só por PR com eval |
