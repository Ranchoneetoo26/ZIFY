# Fluxos do MVP 1

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Fluxos de uso do MVP 1 |
| Versão | 2.0 |
| Status | Aprovado para refinamento |
| Dono | Product Owner (com UX) |
| Documentos relacionados | [DVP-E](dvp-e.md), [DRP](drp.md), [Personas](personas.md), [Critérios de aceite](../04-qualidade/criterios-de-aceite-mvp1.md), [Arquitetura da extensão](../03-arquitetura/extensao.md), [Monetização](monetizacao.md), [Design system](../03-arquitetura/design-system.md), [Guia do usuário](../06-operacao/guia-do-usuario.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | Fluxo principal (8 passos do reposicionamento §6), fluxos alternativos e estados do painel lateral. |
| 2.0 | 21/10/2026 | Versão 2: onboarding em passos com "Preencher mais tarde" (RF-23), área e campos personalizados (RF-24, RF-25), planos, limite atingido (402), pacote, recompensado e anúncios (RF-26 a RF-29). Estados de campo alinhados aos quatro estados do design system. |

## 1. Fluxo principal

| Passo | O que acontece | Quem age | Requisitos |
|---|---|---|---|
| 1 | A pessoa cria uma conta e passa pelo onboarding em passos (escolhe a área, responde ou adia cada pergunta) ou importa um currículo. | Pessoa, plataforma | RF-01, RF-02, RF-03, RF-23, RF-24 |
| 2 | A plataforma apresenta os dados extraídos ou organizados para revisão. A pessoa corrige e aprova o que quer manter. | Pessoa, plataforma | RF-04, RF-05 |
| 3 | A pessoa abre, por conta própria, o site de vagas e faz login nele. O Zyft não pede nem guarda a senha do portal. | Pessoa | RNF-01 |
| 4 | No portal suportado, a extensão reconhece a página autorizada e mostra as ações disponíveis naquela tela. | Extensão | RF-09, RF-10 |
| 5 | A pessoa escolhe quais dados quer usar. A extensão propõe ou preenche campos compatíveis e sinaliza ambiguidades e informações ausentes. | Pessoa, extensão, API | RF-11 a RF-14, RF-16, RF-18 |
| 6 | Para campos de texto livre, a IA sugere uma redação baseada só em fatos aprovados. A pessoa revisa e edita. | IA, pessoa | RF-06, RF-15 |
| 7 | A pessoa confere os campos e decide se quer salvar ou enviar. Nenhuma candidatura é enviada sem confirmação explícita: quem clica em enviar é a pessoa, no portal. | Pessoa | RN-01, RF-14 |
| 8 | A extensão mostra a conclusão. O histórico central completo é MVP 2; um registro mínimo no MVP 1 depende do [ADR-0012](../03-arquitetura/adr/0012-registro-minimo-de-conclusao-no-mvp1.md). | Extensão | RF-19, RF-20 |

```mermaid
flowchart TD
    A([Início]) --> B{Já tem currículo?}
    B -- Sim --> C[Importar PDF/DOCX]
    B -- Não --> D[Preencher Perfil Mestre à mão]
    C --> E[Revisar proposta campo a campo]
    D --> F[Plataforma pergunta só o que falta]
    E --> F
    F --> G[Perfil Mestre aprovado]
    G --> H[Pessoa abre o portal e faz login sozinha]
    H --> I{Página do portal suportado<br/>ou site autorizado?}
    I -- Não --> I2[Extensão fica inativa<br/>e oferece pedir permissão]
    I -- Sim --> J[Painel lateral mostra ações da tela]
    J --> K[Ler formulário]
    K --> L[De-para: regras, IA aponta chave, validação]
    L --> M[Painel: pronto / ambíguo / ausente / ação sua]
    M --> N[Pessoa revisa, edita e seleciona]
    N --> O{Há texto livre?}
    O -- Sim --> P[IA sugere texto só com fatos aprovados]
    P --> Q[Pessoa edita e aprova]
    O -- Não --> R[Preencher só os campos aprovados]
    Q --> R
    R --> S[Resumo: preenchidos e pendentes]
    S --> T[Pessoa resolve pendências no portal]
    T --> U[Pessoa clica em salvar/enviar no portal]
    U --> V[Extensão mostra a conclusão]
    V --> W([Fim])
```

### 1.1 Sequência do preenchimento (passos 4 a 7)

```mermaid
sequenceDiagram
    actor P as Pessoa
    participant X as Extensão (painel lateral)
    participant C as Crawler (página do portal)
    participant API as API Zyft
    participant IA as Serviço de IA

    P->>X: Ler formulário
    X->>C: coletar campos visíveis
    C-->>X: campos (rótulo, seção, tipo, opções, valor atual)
    X->>API: POST /v1/mappings (campos, sem valores sensíveis)
    API->>API: regras determinísticas
    API->>IA: campos restantes + chaves do perfil (sem valores)
    IA-->>API: chave apontada por campo
    API->>API: validação (chave existe, opção existe, valor vem do perfil)
    API-->>X: propostas só para os campos desta tela
    X-->>P: lista com estados e origem
    P->>X: edita, seleciona e confirma "Preencher"
    X->>C: preencher campos aprovados
    C-->>X: resultado por campo
    X->>API: registro de ação (sem valores)
    X-->>P: resumo e pendências
    Note over P,C: O envio é sempre um clique da pessoa no portal
```

## 2. Fluxos alternativos

### FA-01 — IA indisponível

| Gatilho | Comportamento | Requisitos |
|---|---|---|
| O serviço de IA não responde ou passa do tempo limite. | O de-para continua só com regras determinísticas; campos que dependiam da IA ficam "ausente" com o aviso "A IA está indisponível. Os campos simples continuam funcionando." Sugestão de texto e análise de vaga ficam desabilitadas com a mesma mensagem. Nada é preenchido por suposição. | RNF-12, RF-12 |

### FA-02 — Campo ambíguo

| Gatilho | Comportamento |
|---|---|
| Duas chaves do perfil servem para o campo, ou a opção do select não corresponde claramente ao valor do perfil. | O campo aparece como **ambíguo**, desmarcado, com as alternativas possíveis. A pessoa escolhe uma alternativa ou digita o valor. Nada é preenchido até a escolha (RN-05). |

### FA-03 — Valor divergente entre portal e perfil

```mermaid
flowchart LR
    A[Campo do portal já tem valor] --> B{Igual ao perfil?}
    B -- Sim --> C[Mostra como pronto, sem alteração]
    B -- Não --> D[Mostra os dois valores lado a lado]
    D --> E[Padrão: manter portal]
    D --> F[Usar o perfil]
    D --> G[Levar para o perfil]
    G --> H[Aprovação explícita antes de alterar o perfil]
```

| Regra | Requisito |
|---|---|
| A opção padrão é **manter o valor do portal**. Levar um valor do portal para o perfil sempre pede aprovação e fica no registro de ações. | RF-16 |

### FA-04 — Campo sensível

| Gatilho | Comportamento | Requisitos |
|---|---|---|
| O rótulo indica gênero, raça/etnia, deficiência, religião, saúde, orientação sexual, nome social, estado civil, filhos e similares. | O campo aparece como **ação sua**, com a explicação "Este é um dado sensível. Responda direto no formulário, se quiser." Só pode ser preenchido pelo Zyft se o consentimento (c) estiver ligado **e** a pessoa marcar o campo manualmente. O dado sensível nunca é enviado à IA. | RF-18, RN-04, [ADR-0014](../03-arquitetura/adr/0014-dados-sensiveis-segregados-e-criptografados.md) |

### FA-05 — CAPTCHA, senha, código de verificação, anexo ou termos

| Gatilho | Comportamento |
|---|---|
| A página tem CAPTCHA, campo de senha, código de verificação, upload de arquivo ou caixa de aceite de termos ou declaração. | O crawler ignora senha, CAPTCHA e upload; termos e declarações são listados como **ação sua**. O painel explica o que a pessoa precisa fazer. O Zyft nunca tenta resolver ou contornar ([ADR-0010](../03-arquitetura/adr/0010-sem-credenciais-sem-contorno-de-seguranca-campos-bloqueados.md)). |

### FA-06 — Modal aberto

| Gatilho | Comportamento |
|---|---|
| A candidatura acontece num modal ("candidatura simplificada") sobre a página. | O crawler lê só o modal visível mais recente. Se o modal mudar de etapa, a pessoa clica de novo em "Ler formulário"; o painel avisa quando detecta que a etapa mudou. |

### FA-07 — Currículo não suportado (PDF escaneado)

| Gatilho | Comportamento | Requisitos |
|---|---|---|
| O PDF não tem texto extraível (imagem escaneada), o arquivo passa de 10 MB ou o formato não é PDF nem DOCX. | Mensagem clara: "Não conseguimos ler o texto deste arquivo. Envie um PDF gerado por editor de texto ou um DOCX, ou preencha o perfil à mão." Nenhuma proposta parcial é criada. | RF-03 |

### FA-08 — Página fora do portal suportado

| Gatilho | Comportamento |
|---|---|
| A pessoa abre o painel numa página que não é do portal suportado. | O painel diz que o site não é suportado no MVP 1 e oferece "Usar neste site mesmo assim", que pede permissão do navegador para aquele domínio. Sem permissão, a extensão não lê nada da página ([ADR-0011](../03-arquitetura/adr/0011-primeiro-portal-e-conectores-por-portal.md)). |

### FA-09 — Extensão não pareada ou sessão expirada

| Gatilho | Comportamento |
|---|---|
| A extensão não tem pareamento ou o token foi revogado. | O painel mostra "Conectar à sua conta Zyft" e inicia o pareamento ([ADR-0007](../03-arquitetura/adr/0007-autenticacao-e-pareamento-da-extensao.md)). Nenhum dado do perfil fica guardado na extensão além do necessário para a tela atual. |

## 3. Estados do painel lateral

### 3.1 Estados da tela

```mermaid
stateDiagram-v2
    [*] --> NaoPareado
    NaoPareado --> Ocioso: pareamento confirmado
    Ocioso --> PaginaNaoSuportada: site fora da lista
    PaginaNaoSuportada --> Ocioso: permissão concedida
    Ocioso --> Lendo: "Ler formulário"
    Lendo --> Revisao: propostas recebidas
    Lendo --> Erro: falha na leitura ou na API
    Revisao --> Preenchendo: "Preencher selecionados"
    Preenchendo --> Resumo: campos preenchidos
    Resumo --> Revisao: "Ler novamente" (nova etapa)
    Resumo --> Concluido: pessoa salvou/enviou no portal
    Erro --> Ocioso: tentar de novo
    Concluido --> Ocioso
```

| Estado | O que a pessoa vê |
|---|---|
| Não pareado | Botão "Conectar à sua conta Zyft". |
| Ocioso | Ações disponíveis para a tela: "Ler formulário", "Analisar vaga" (se for página de vaga e houver consentimento), "Importar do portal". |
| Página não suportada | Explicação e opção de autorizar o site. |
| Lendo | Progresso ("Lendo 42 campos…"). |
| Revisão | Lista de campos com estado, valor proposto, origem e edição. Resumo no topo: "Vamos preencher 18 campos. 3 precisam de você." |
| Preenchendo | Progresso por campo. |
| Resumo | Quantos campos foram preenchidos, quais falharam e o que ainda é ação da pessoa. Lembrete: "Confira no portal e envie quando estiver pronto(a)." |
| Concluído | Confirmação visual da conclusão (RF-19). |
| Erro | Mensagem que diz o que fazer e botão para tentar de novo. |

### 3.2 Estados de cada campo

Na versão 2 o painel usa os **quatro estados** do [design system](../03-arquitetura/design-system.md#5-estados-de-campo). Cada um tem cor, ícone e palavra, nunca só cor (RNF-06). Os estados técnicos devolvidos pela API (`FieldSuggestion.state` e `kind`) continuam os mesmos; a tabela mostra como cada um aparece.

| Estado na tela | Estado técnico (API) | Significado | Vem marcado? |
|---|---|---|---|
| **Pronto** (sálvia, check) | `ready` | Valor veio do Perfil Mestre aprovado, de um campo personalizado (`custom:<id>`) ou de uma resposta aprovada. | Sim, exceto sensível com consentimento, que vem desmarcado e é aprovado um a um. |
| **Precisa de você** (âmbar, alerta) | `ambiguous`, `missing`, `divergent` | Falta no perfil, há mais de uma opção, ou o portal já tem um valor diferente (padrão: manter o do portal). | Não |
| **Ação sua** (borda tracejada, bloqueio) | `blocked` | Sensível sem consentimento, termos, anexo, CAPTCHA, senha. O Zyft não age. | Nunca |
| **Sugestão da IA – revise** (roxo, rascunho) | `kind=generated` | Texto livre escrito pela IA a partir dos fatos do perfil. Só é aprovado depois de a pessoa marcar "Li e é verdade sobre mim". | Não |

Ordem no painel: os Prontos em linhas compactas no topo, com "Aprovar os N" (só Prontos não sensíveis de **um** formulário); depois, um a um, os sensíveis e o que precisa de atenção.

## 4. Onboarding em passos (RF-23, RF-24)

Por que existe: a dor mais forte é redigitar dados (4,40), e um cadastro longo de uma vez repetiria essa dor dentro do próprio Zyft. Uma pergunta por tela, com a opção de adiar qualquer uma, deixa a pessoa começar em poucos minutos e completar depois ([ADR-0018](../03-arquitetura/adr/0018-onboarding-em-passos-com-perguntas-adiaveis.md)).

| Capítulo | Perguntas (ids de passo) |
|---|---|
| 1. Quem é você | `nome`, `nascimento`, `contato`, `endereco` |
| 2. Sua área | `area`, `cargos`, `setores`, `registro` (só nas áreas que pedem), `links` |
| 3. O que você busca | `nivel`, `modelo`, `local`, `contrato`, `disponibilidade`, `salario`, `mobilidade` |
| 4. Sua bagagem | `experiencia`, `diaadia`, `ferramentas`, `formacao`, `cursos`, `competencias`, `idiomas` |
| 5. Sua apresentação | `titulo`, `resumo` |
| 6. Combinados | `combinados` (consentimentos, todos desligados por padrão) |

```mermaid
flowchart TD
    A([Conta criada]) --> B[Boas-vindas: o que vai acontecer]
    B --> C{Tem currículo?}
    C -- Sim --> D[Importar currículo e revisar]
    D --> E
    C -- Não --> E[Pergunta atual<br/>uma por tela]
    E --> F{Responder?}
    F -- Responder --> G[Salva no Perfil Mestre<br/>e marca como respondida]
    F -- Preencher mais tarde --> H[Marca como pulada]
    G --> I[PUT /onboarding<br/>answered, skipped, lastStep]
    H --> I
    I --> J{Última pergunta?}
    J -- Não --> E
    J -- Sim --> K[Combinados: consentimentos]
    K --> L[PUT /onboarding completed=true<br/>auditoria onboarding.completed]
    L --> M[Perfil Mestre]
    M --> N{Há puladas?}
    N -- Sim --> O[Alerta no topo: N perguntas ficaram para depois<br/>Continuar de onde parei / Responda agora]
    N -- Não --> P([Perfil pronto para a extensão])
```

| Regra | Requisito |
|---|---|
| Toda pergunta tem "Preencher mais tarde" e nenhuma é obrigatória. | RN-14 |
| A escolha da área muda as sugestões de cargos, setores, ferramentas e competências, o link pedido (GitHub, Currículo Lattes) e a pergunta de registro (COREN, OAB, CRC, NR/MOPP). Em qualquer lista a pessoa pode digitar o próprio valor. | RF-24 |
| Fechar o navegador não perde nada; a pessoa volta para `lastStep`. | RNF-17 |
| Enquanto houver puladas, a extensão marca os campos correspondentes como **Precisa de você**. O alerta explica isso. | RF-23 |
| "Responda agora" mostra primeiro as pendentes que mais aparecem nos formulários (pretensão salarial, disponibilidade, CNH). | RF-23 |
| Nenhum anúncio no onboarding. | RN-08 |

### 4.1 Campos personalizados (RF-25)

```mermaid
flowchart LR
    A[Perfil: Informações da sua área] --> B[Nome + valor]
    B --> C{Rótulo parece sensível<br/>ou documento?}
    C -- Sim --> D[422: use Dados sensíveis<br/>ou não informe]
    C -- Não --> E[Salvo; auditoria custom_field.changed]
    E --> F[No formulário: regra compara rótulo<br/>ou IA aponta custom:id]
    F --> G[Valor vem do perfil<br/>estado Pronto]
```

## 5. Planos, limites e anúncios

### 5.1 Limite atingido (RF-26 a RF-28)

```mermaid
flowchart TD
    A[Pessoa clica em Ler formulário] --> B[POST /forms/mappings]
    B --> C{Dentro do limite<br/>ou com bônus/pacote?}
    C -- Sim --> D[Revisão normal]
    C -- Não --> E[402 plan.limit_reached<br/>resource, used, limit]
    E --> F[Painel: Você usou as 10 candidaturas assistidas deste mês]
    F --> G{Escolha da pessoa}
    G -- Ver uma oferta patrocinada --> H[POST /ads/rewarded/claim<br/>+3 no mês]
    H --> I{Ainda há ofertas no mês?}
    I -- Sim --> B
    I -- Não: 409 --> F
    G -- Comprar pacote R$ 4,90 --> J[POST /billing/packs<br/>pagamento simulado] --> B
    G -- Testar o Plus --> K[POST /billing/subscribe<br/>7 dias grátis] --> B
    G -- Agora não --> L[Pessoa preenche no site por conta própria]
```

| Regra | Requisito |
|---|---|
| A mensagem é calma: sem vermelho, sem contagem regressiva, sem "última chance". | Design system, RN-12 |
| O limite nunca bloqueia editar o perfil, exportar ou excluir a conta, nem o registro de ações. | RN-12 |
| A opção recompensada só aparece se ainda houver ofertas no mês (máximo 3). | RF-28 |
| Toda tela de compra mostra "Ambiente de demonstração: nenhuma cobrança real". | RN-13 |

### 5.2 Assinar e cancelar o Plus (RF-26)

| Passo | O que acontece |
|---|---|
| 1 | Em **Planos**, a pessoa escolhe mensal (R$ 9,90) ou anual (R$ 99) e clica em "Testar 7 dias grátis". |
| 2 | `POST /billing/subscribe {period}`: o plano vira `plus`, com `trialEndsAt` em 7 dias. Auditoria `plan.changed`. |
| 3 | Os anúncios somem na próxima tela (`GET /ads` responde `204`), os limites passam a `null` e o limite de navegadores sobe para 3. |
| 4 | "Cancelar" chama `POST /billing/cancel`: o plano volta ao gratuito no fim do período (na demonstração, pode ser na hora). Nada do perfil se perde. |

### 5.3 Anúncio na tela (RF-29)

```mermaid
sequenceDiagram
    actor P as Pessoa
    participant C as Web ou extensão
    participant API as API Zyft
    C->>C: carrega o conteúdo principal primeiro
    C->>API: GET /ads?placement=ext.job.contextual&topic=Inglês avançado
    API->>API: plano Plus? placement proibido? tudo oculto?
    alt nenhum anúncio
        API-->>C: 204
        C-->>P: o espaço não aparece
    else anúncio escolhido por área, cargos e tema
        API-->>C: 200 {title, advertiser, body, cta, reason}
        C-->>P: cartão "Patrocinado" com Ocultar e "Por que estou vendo isso?"
    end
    opt pessoa clica em Ocultar
        P->>C: este anúncio (30 dias) ou este tema (sempre)
        C->>API: POST /ads/{id}/hide {scope}
    end
    opt pessoa clica na chamada
        C->>API: POST /ads/{id}/click (contador agregado)
    end
```

| Regra | Requisito |
|---|---|
| Nunca em onboarding, entrar/criar conta, importar currículo, dados sensíveis, conta e privacidade, painel "Ler formulário", nem para Plus. | RN-08 |
| A requisição leva só o placement e o tema do requisito; área e cargos são lidos no servidor; nada vai para o anunciante. | RN-09, RNF-14 |
| Sempre "Patrocinado", com "Ocultar" e "Por que estou vendo isso?"; no máximo um por tela. | RN-10, RN-11 |
| Erro, timeout ou `204`: o espaço some sem quebrar a tela. | RNF-15 |
| Na extensão, o anúncio contextual substitui o banner do rodapé quando há requisito que não aparece no perfil; o "Próximo passo" só aparece depois de registrar a candidatura. | RF-29 |
