# ADR-0019 — Área profissional dinâmica e campos personalizados

| Campo | Valor |
|---|---|
| Status | Proposta |
| Data | 21/10/2026 |
| Decisores | Product Owner, Arquiteto de software |
| Consultados | Arquiteto de IA, Responsável por privacidade, Dev backend, Dev frontend |
| Requisitos | RF-24, RF-25, RF-02, RF-12, RF-15; RN-02, RN-03, RN-04, RN-15 |
| Relacionados | [ADR-0018](0018-onboarding-em-passos-com-perguntas-adiaveis.md), [ADR-0004](0004-ia-aponta-chave-nunca-escreve-valor.md), [ADR-0006](0006-regras-deterministicas-antes-da-ia.md), [ADR-0014](0014-dados-sensiveis-segregados-e-criptografados.md), [ADR-0017](0017-backend-em-csharp-dotnet-clean-architecture.md) |

## Contexto

Desde o reposicionamento ([ADR-0001](0001-reposicionamento-mvp1-candidatura-assistida.md)), o Zyft diz que serve para qualquer área. Na prática, o Perfil Mestre da Entrega 3 tinha cara de tecnologia: links para GitHub, competências e ferramentas pensadas para TI, e nenhum lugar para o que outras profissões pedem nos formulários, como registro em conselho (COREN, CRC, OAB), cursos obrigatórios (NR-11, MOPP) ou disponibilidade para plantão.

A pesquisa v2 ([resultados](../../05-pesquisa/resultados-v2.md)) tem um viés que precisa ser dito com clareza: 60% das respostas são de Tecnologia, acima do limite de 50% que o próprio plano definia como sinal de amostra enviesada (hipótese H8). Só 1 resposta é de Saúde. Ainda assim:

- os que não são de tecnologia (n = 6) deram nota maior para redigitar dados (4,50 contra 4,33) e para perguntas abertas (3,83 contra 2,89), o que é uma pista, não uma prova;
- "perfil único reaproveitado em vários sites" foi o recurso mais valorizado (87%);
- a persona Beatriz (técnica de enfermagem) foi criada como **hipótese a validar** para testar se o produto funciona fora de tecnologia ([personas](../../01-produto/personas.md)).

Se o produto continua dizendo "qualquer área", o perfil precisa acomodar áreas diferentes sem virar um formulário gigante.

## Fatores de decisão

- Sugestões relevantes para a área da pessoa, sem esconder as outras.
- Liberdade para cadastrar o que o Zyft não conhece.
- A IA continua só com fatos aprovados (RN-02, RN-03) e o de-para continua com regras antes da IA (ADR-0006).
- Nenhum caminho novo para dados sensíveis (RN-04).
- Pouco custo de manutenção num time pequeno.

## Opções consideradas

### Opção A — Perfil fixo, igual para todos (como na Entrega 3)

- **Prós:** nenhum trabalho novo; modelo de dados estável.
- **Contras:** reforça a cara de "produto de TI"; o que outras áreas pedem fica sem lugar e vira "Precisa de você" em todo formulário.

### Opção B — Um perfil diferente por área, com campos fixos próprios

- **Prós:** cada área com exatamente os campos que precisa.
- **Contras:** multiplica o modelo de dados e as regras do de-para por área; cada área nova exige migração; não resolve profissões que ninguém previu.

### Opção C — Área dinâmica (catálogo de sugestões por área) + campos personalizados livres

A pessoa escolhe uma área entre 8 (Saúde, Tecnologia, Vendas e comercial, Administrativo e financeiro, Educação, Logística e operações, Jurídico, Outra área). Um catálogo estático, no `Application`, devolve por área: cargos, setores, ferramentas, competências, o link que a área costuma pedir (GitHub, Currículo Lattes) e a pergunta de registro profissional (por exemplo, "Registro no conselho", "Ex.: COREN-MG 123456"). Em qualquer lista a pessoa cadastra valores próprios. Além disso, "Informações da sua área" permite criar campos personalizados (nome + valor) que o de-para e a IA podem usar.

- **Prós:** o modelo de dados quase não muda (três colunas em `Profiles` e uma tabela de campos personalizados); o catálogo é conteúdo, não código de regra; profissões não previstas cabem nos campos personalizados; a mesma tela serve para todas as áreas.
- **Contras:** o catálogo precisa de curadoria; campos personalizados são texto livre e podem ser usados para guardar o que não deviam (dados sensíveis); o de-para por rótulo livre é menos preciso que uma chave fixa.

### Opção D — Deixar a IA inferir a área e os campos a partir do currículo

- **Prós:** nenhuma pergunta a mais.
- **Contras:** a IA decidiria fatos sobre a pessoa sem aprovação, o que contraria o princípio "só fatos aprovados"; não funciona sem IA nem sem currículo.

## Decisão (proposta)

Adotar a **Opção C**.

1. **Perfil:** `GET/PUT /profile` ganha `workArea` (texto), `professionalRegistration` (texto) e `salaryType` ("CLT (bruto)", "PJ", "Por hora", "Por plantão"). `GET /profile` passa a incluir `customFields: [{id, label, value, updatedAt}]`.
2. **Catálogo:** `GET /catalog/areas` (autenticado) devolve `[{id, name, roles[], sectors[], tools[], skills[], linkLabel|null, registration: {label, placeholder}|null}]`, estático no `Application`, com o mesmo conteúdo do design da v2. A escolha da área muda só sugestões; nada impede escolher fora delas.
3. **Campos personalizados:** `GET/POST /profile/custom-fields` e `PUT/DELETE /profile/custom-fields/{id}`; `label` até 80 caracteres e `value` até 500.
4. **Sem sensíveis em campo personalizado (RN-15):** rótulos que pareçam raça, religião, saúde, deficiência, gênero, orientação, CPF, RG ou senha são recusados com 422 `profile.custom_field_sensitive` e a mensagem orienta usar Dados sensíveis. A lista de termos fica no domínio, com testes.
5. **De-para e IA:** campos personalizados entram nos fatos da IA (`ProfileFacts`) e no mapeamento com a chave `custom:<id>`. O mapeador por regras compara o rótulo do campo do formulário com o `label`; a IA pode apontar `custom:<id>`, e o valor continua vindo do perfil (ADR-0004).
6. **Auditoria:** `custom_field.changed`, sem rótulo nem valor.

## Consequências

**Positivas**
- O produto fica coerente com "qualquer área" sem multiplicar o modelo de dados.
- Profissões não previstas cabem nos campos personalizados.
- A regra "IA aponta a chave" continua valendo, agora também para `custom:<id>`.

**Negativas**
- O catálogo é conteúdo editorial e envelhece; precisa de revisão por área.
- O de-para por rótulo livre erra mais que por chave fixa: mais campos podem cair em "Precisa de você".
- Uma migração (três colunas e a tabela `CustomFields`).

**Riscos e mitigação**
- **Dado sensível disfarçado num campo personalizado** (por exemplo, "Condição de saúde"). Mitigação: bloqueio por lista de termos com testes (TN-13), texto de ajuda na tela e revisão da lista pelo responsável por privacidade. O bloqueio é por rótulo; um valor sensível com rótulo neutro não é detectado, e isso fica registrado como risco residual.
- **Prompt injection por rótulo personalizado.** Mitigação: rótulos e valores entram no prompt como dados delimitados; a resposta da IA continua validada por schema e só aceita chaves existentes.
- **Viés da amostra.** Mitigação: a persona Beatriz é tratada como hipótese; a próxima rodada da pesquisa precisa de mais respostas fora de tecnologia antes de investir em catálogos por área.

## Conformidade

- Teste de unidade: rótulos sensíveis (incluindo variações com e sem acento e maiúsculas) são recusados; rótulos comuns ("Registro no conselho", "Disponível para plantão") são aceitos.
- Teste de integração: `POST /profile/custom-fields` com rótulo sensível responde 422 `profile.custom_field_sensitive` (TN-13); com `label` acima de 80 ou `value` acima de 500, 400.
- Teste de unidade do de-para: campo do formulário "Número do COREN" é ligado a `custom:<id>` com rótulo "Registro no conselho" ou à `professionalRegistration`; o valor vem do perfil.
- Teste de unidade: a IA que aponta `custom:<id>` inexistente tem a chave descartada.
- Teste de contrato: `GET /catalog/areas` devolve as 8 áreas e "Outra área" com listas vazias.

## Links

- [DRP RF-24, RF-25, RN-15](../../01-produto/drp.md), [Personas](../../01-produto/personas.md), [Modelo de dados](../modelo-de-dados.md), [API](../api.md)
- [Resultados da pesquisa v2](../../05-pesquisa/resultados-v2.md), [Por que o Zyft](../../00-contexto/por-que-o-zyft.md)
- Mockups `BoasVindas.dc.html` (função `areas()`) e `Main.dc.html` ("Informações da sua área")
