# Arquitetura do frontend web (plataforma)

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Arquitetura do frontend web |
| Versão | 2.3 |
| Status | Vigente para a v2 do MVP 1, conferido contra o código em `web/` em 21/10/2026. As telas de planos e anúncios dependem do [ADR-0020](adr/0020-monetizacao-com-anuncios-e-planos.md) (proposto). |
| Dono | Arquitetura de software, com o dev frontend (que acumula UX) |
| Documentos relacionados | [Design system](design-system.md), [API](api.md), [Extensão](extensao.md), [Fluxos](../01-produto/fluxos.md), [DRP](../01-produto/drp.md), [Monetização](../01-produto/monetizacao.md), [ADR-0017](adr/0017-backend-em-csharp-dotnet-clean-architecture.md), [ADR-0018](adr/0018-onboarding-em-passos-com-perguntas-adiaveis.md), [ADR-0019](adr/0019-area-profissional-dinamica-e-campos-personalizados.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | Arquitetura-alvo em React 18 + Vite, TanStack Query e Radix, dentro do monorepo TypeScript do ADR-0003. |
| 2.0 | 21/10/2026 | Reescrito para o que existe: HTML, CSS e JavaScript sem build, servidos pela API .NET (ADR-0017). Acrescenta as telas da v2 (onboarding em passos, área e campos personalizados, planos, anúncios), o tratamento do HTTP 402 e o aviso de pagamento simulado. React/Vite passa a ser evolução futura (§10). |
| 2.1 | 21/10/2026 | Sincronizado com a implementação: CSS dividido em `css/tokens.css`, `components.css` e `app.css` (`styles.css` removido); módulos `ui.js`, `ads.js`, `billing.js`, `catalog.js` e `store.js`; tela de Planos em `views/plans.js`. Registra as cópias de segurança quando um endpoint falha, o que a tabela de Planos mostra, a forma real de ocultar anúncios, o diálogo do 402, a CSP real e as limitações conhecidas (§11). |
| 2.2 | 21/10/2026 | Timeout de 1,5 s no pedido de anúncio (RNF-15) implementado em `fetchAd` (`web/js/ads.js`); a linha saiu das limitações. Comprar pacote só com token web, como assinar e cancelar. |
| 2.3 | 21/10/2026 | Correções da gravação do tour: `select` sem opção vazia nunca abre em branco (Tipo de curso = Curso); erros 400 com nome legível do campo (`fieldLabel`, `describeErrors`, `registerFieldLabels` em `api.js`); item incompleto da importação pede o campo que falta antes de **Aprovar** (`missingFields`); Planos lê `trialAvailable` e só oferece "7 dias grátis" quando disponível. |
| 2.4 | 22/10/2026 | Rede de anúncios Google AdSense **simulada** ([ADR-0021](adr/0021-rede-de-anuncios-google-adsense.md)): `ads.js` desenha o bloco `<ins class="adsbygoogle">` quando `format === "network"`; novo `web/js/adsense.js` com `adsenseUnit` e `loadAdSense` (não carrega nada em modo mock). CSP inalterada (§6.1). |

## 1. Escopo

A plataforma web é onde a pessoa **prepara** as candidaturas: conta, Perfil Mestre, currículo, banco de respostas, extensão, privacidade e plano. O **uso** acontece no painel lateral da extensão ([extensao.md](extensao.md)). As duas partes seguem o mesmo [design system](design-system.md).

| Área | Rota | Requisitos |
|---|---|---|
| Entrar e criar conta | `#/entrar`, `#/cadastrar` | RF-01 |
| Onboarding em passos ("passaporte") | `#/boas-vindas` | RF-23, RF-24, RF-07 |
| Perfil Mestre | `#/perfil` | RF-02, RF-05, RF-23, RF-24, RF-25 |
| Importar currículo | `#/importar` | RF-03, RF-04 |
| Banco de respostas | `#/respostas` | RF-06 |
| Extensão (pareamento, navegadores, sites autorizados) | `#/extensao` | RF-09, RF-10 |
| Candidaturas e detalhe | `#/candidaturas`, `#/candidaturas/:id` | RF-19 ([ADR-0012](adr/0012-registro-minimo-de-conclusao-no-mvp1.md)) |
| Registro de ações | `#/registro` | RF-20 |
| Conta e privacidade | `#/conta` | RF-07, RF-08 |
| Planos | `#/planos` | RF-26, RF-27, RF-28 |
| Anúncios (componentes dentro das telas) | — | RF-29 |

## 2. Stack real e por quê

| Necessidade | Escolha | Motivo |
|---|---|---|
| UI | HTML, CSS e JavaScript com ES modules, **sem etapa de build** | Entrega simples de rodar e corrigir dentro do prazo da disciplina. A API .NET serve a pasta `web/` em `/`, sem Node no ambiente. |
| Rotas | Roteador próprio por hash (`#/rota`) em `web/js/app.js` | Poucas telas; não precisa de configuração de servidor para rotas profundas. |
| DOM | Helper `h()` em `web/js/dom.js` (`createElement` + `textContent`) | Nenhum HTML vindo da API ou da página é inserido como HTML. Fecha a porta para XSS com dados do perfil. |
| Componentes | `web/js/ui.js`: os componentes do [design system](design-system.md) em JavaScript puro (`Button`, `StatusBadge`, `Stamp`, `TextField`, `Switch`, `ConsentRow`, `PairingCode`, `AppNav`, `SensitiveValue`, `WhyThis`, `UsageMeter`, `ReviewItem`, `PageHead`, `Card`, `Chip`, `SponsoredTag`, `Logo`) | Mesmas classes `zy-*` do pacote do design system, sem React. |
| Dados do servidor | Cliente único `web/js/api.js` | Bearer, renovação com rotação, ProblemDetails convertido em `ApiError`, desvio do 402 para o diálogo de limite. |
| Estado compartilhado | `web/js/store.js` | Perfil (o `PUT /profile` substitui tudo, então o front sempre envia o conjunto completo de campos básicos), progresso do onboarding e cache de 30 s do `GET /billing`. |
| Validação | `web/js/validate.js` | Espelha regras simples do backend para evitar idas e voltas; o backend continua sendo a fonte da verdade. |
| Estilo | `web/css/tokens.css`, `components.css` e `app.css` | Tokens do design system em custom properties (claro, escuro por `prefers-color-scheme` ou `[data-theme]`), componentes `zy-*` e layout das telas. Fontes Archivo e JetBrains Mono vindas do Google Fonts. |
| Testes | Playwright + Chromium; axe manual | Ver §9 |

## 3. Estrutura

```
web/
  index.html              shell: "Pular para o conteúdo", menu lateral, <main>, região aria-live, toast
  css/tokens.css          tokens do design system (claro e escuro)
  css/components.css      componentes zy-* (botão, badge, campo, switch, consentimento, pareamento, medidor…)
  css/app.css             layout e telas; regra global [hidden] { display: none !important; }
  assets/                 logos (zyft-mark, lockup claro e escuro)
  js/app.js               rotas por hash, guarda de autenticação, menu lateral, foco no título a cada troca
  js/api.js               cliente HTTP (Bearer, refresh com rotação, ProblemDetails, aciona o diálogo do 402)
  js/dom.js               h(), field(), showErrors(), modal(), confirmDialog(), errorBox(), announce(), toast()
  js/ui.js                componentes do design system em JS puro
  js/store.js             perfil básico, onboarding (GET/PUT /onboarding) e plano (GET /billing)
  js/catalog.js           GET /catalog/areas, com cópia estática das 8 áreas como reserva
  js/ads.js               espaço de anúncio (adSlot) e "Por que estou vendo isso?"
  js/billing.js           diálogo do 402, pacote, recompensado, medidores e aviso de demonstração
  js/validate.js          validações que espelham o backend
  js/views/
    auth.js               entrar e criar conta
    onboarding.js         passaporte em 6 capítulos e consentimentos
    profile.js            Perfil Mestre: pendentes, "Responda agora", sobre você, listas, campos da área, sensíveis
    profile-schema.js     campos, enums e conversões de/para a API
    import.js             envio do currículo e revisão item a item
    answers.js            banco de respostas
    extension.js          código de pareamento, navegadores, sites autorizados
    applications.js       candidaturas e detalhe
    audit.js              registro de ações
    account.js            conta, consentimentos, exportar, excluir
    consents.js           linhas de consentimento (usadas em Conta)
    plans.js              planos, uso do mês, pacote, tabela comparativa
  demo/                   portal de vagas de demonstração (servido em /demo/)
```

## 4. Navegação

Menu lateral (componente `AppNav`), em três grupos. No rodapé do menu: "Você entrou como", o nome, o plano atual ("Plano gratuito" ou "Zyft Plus", com link para Planos) e **Sair**. Em telas estreitas, o menu vira um botão **Menu**.

| Grupo | Itens |
|---|---|
| Seu perfil | Perfil Mestre, Importar currículo, Banco de respostas |
| Candidaturas | Extensão, Candidaturas (com a contagem) |
| Conta | Planos, Registro de ações, Conta e privacidade |

Regras do roteador:

- Rotas públicas: `#/entrar` e `#/cadastrar`. Sem login, qualquer outra rota leva a `#/entrar`; com login, as públicas levam a `#/perfil`.
- `#/boas-vindas` (e `#/boas-vindas/<passo>`) abre em tela cheia, sem menu.
- Depois de criar a conta, a pessoa vai para `#/boas-vindas`. Na primeira tela de cada sessão (aba), se `GET /onboarding` não tiver `completedAt`, o roteador leva para `#/boas-vindas`, que retoma do `lastStep`. Por isso a Beatriz, cuja demonstração tem o onboarding parado, entra direto na pergunta de competências. A checagem acontece uma vez por sessão: depois disso, a pessoa navega livremente.
- A cada troca de tela, o foco vai para o `<h1>` e o título é anunciado em `aria-live`.
- O menu não aparece nas telas de entrar, criar conta e onboarding.

## 5. Telas

### 5.1 Entrar e criar conta (RF-01)

Duas abas, **Entrar** e **Criar conta**. Entrar: e-mail e senha; erro de credencial sempre como "E-mail ou senha incorretos", sem revelar se o e-mail existe. Criar conta: nome, e-mail, senha (8 caracteres, com letra e número) e a caixa "Li e aceito os termos de uso e a política de privacidade do Zyft", obrigatória. **Sem anúncio** (RN-08).

### 5.2 Onboarding em passos: o passaporte (RF-23, RF-24, ADR-0018)

- Abre com uma tela de boas-vindas (**Começar**) e uma escolha: **Tenho um currículo** (vai para `#/importar`) ou **Responder aqui**.
- Depois, uma pergunta por tela em 6 capítulos: Quem é você (nome, nascimento, contato, cidade/UF/país/CEP), Sua área (área, cargos, setores, registro profissional só quando a área pede, links), O que você busca (momento da carreira, modelo, localidade, contrato, disponibilidade, pretensão salarial com o tipo, mobilidade e CNH), Sua bagagem (experiência, dia a dia e resultados, ferramentas, formação, cursos, competências, idiomas), Sua apresentação (título, resumo) e Combinados (consentimentos). São **25 perguntas** quando a área tem registro profissional (Saúde, Administrativo, Logística, Jurídico) e 24 nas outras.
- Botões de cada pergunta: **Carimbar e continuar** (na última, **Carimbar e concluir**), **Preencher mais tarde** e **Voltar**. No topo, "Capítulo N · nome", "Pergunta N de M" e **Sair e continuar depois**. Toda pergunta pode ser adiada (RN-14).
- O cartão do passaporte, ao lado, mostra por capítulo "N de M respondidas", o selo "Carimbado" quando o capítulo fecha e "K para depois".
- A área escolhida carrega as sugestões de `GET /catalog/areas` (cargos, setores, ferramentas, competências, o link pedido e a pergunta de registro). Em toda lista há um campo "Outro…" para valor próprio, e "Outra área" abre um campo livre (ADR-0019). Quando a área pede um link que não é GitHub (ex.: Currículo Lattes), ele é gravado como campo personalizado com esse nome.
- As respostas são gravadas na hora, em cada passo, nos endpoints de perfil (`PUT /profile`, `POST|PUT /profile/experiences`, `educations`, `courses`, `POST|DELETE /profile/skills`, `languages`, `POST|PUT /profile/custom-fields`, `PUT /consents/{purpose}`). O `PUT /onboarding` guarda só o progresso: `answered`, `skipped`, `lastStep` e `completed` (RNF-17).
- Experiência, formação e curso editam o **primeiro item** da lista do perfil (a mais recente). "Adicionar outra experiência/formação/curso" guarda o atual e abre um novo em branco.
- Tela final: "Pronto, {nome}. Agora é com a vaga.", a lista das perguntas puladas (cada uma com link para o passo) e os botões **Conectar a extensão** e **Ver meu Perfil Mestre**.
- Ids de passo: `nome`, `nascimento`, `contato`, `endereco`, `area`, `cargos`, `setores`, `registro`, `links`, `nivel`, `modelo`, `local`, `contrato`, `disponibilidade`, `salario`, `mobilidade`, `experiencia`, `diaadia`, `ferramentas`, `formacao`, `cursos`, `competencias`, `idiomas`, `titulo`, `resumo`, `combinados`.
- **Sem anúncio** (RN-08). Limitações de conversão estão no §11.

### 5.3 Perfil Mestre (RF-02, RF-05, RF-23 a RF-25)

| Bloco | O que faz |
|---|---|
| Cabeçalho | "Seu passaporte · Perfil Mestre" e o atalho **Atualizar com um currículo**. |
| Alerta de pendentes | "N informações ficaram para depois no seu passaporte". Junta os passos pulados no onboarding com o que `GET /profile/missing` aponta, em etiquetas. Explica que a extensão vai marcar esses campos como "Precisa de você". **Continuar de onde parei** abre o primeiro passo pendente. |
| "Responda agora, em 10 segundos" | Até 3 cartões, entre: pretensão salarial, disponibilidade, CNH, modelo de trabalho, viagens e mudança de cidade (só os que faltam). Cada um com opções ou um valor e **Carimbar**. Carimbar também tira o passo da lista de pulados. |
| Sobre você | Dados básicos, incluindo área (`workArea`), registro profissional e pretensão com o tipo (`salaryType`: "CLT (bruto)", "PJ", "Por hora", "Por plantão"). **Editar** abre um formulário com **Salvar e carimbar**. |
| Experiências | Linha do tempo com cargo, empresa, período, resultado, atividades e ferramentas. |
| Competências | Lista da pessoa, até 6 sugestões da área ("toque para adicionar") e "Cadastrar do seu jeito". |
| Idiomas, Formação e cursos | Listas com adicionar, editar e excluir. O `select` **Tipo** do curso abre em **Curso** (`default: 'Course'` no schema; `field()` nunca deixa um `select` sem opção vazia em branco). |
| Informações da sua área | Campos personalizados (RF-25): "Nome da informação" e "Sua resposta", botão **Adicionar informação**; editar e excluir em cada linha. Rótulo que pareça dado sensível volta com 422 `profile.custom_field_sensitive` e a tela mostra o aviso com **Ir para Dados sensíveis** (RN-15). |
| Dados sensíveis | Sem o consentimento *Dados sensíveis*, o bloco só explica e leva a **Ligar em Conta e privacidade**. Com ele, lista os tipos que a API aceita (gênero, raça ou etnia, deficiência, religião, orientação sexual, saúde, nome social, outro), mascarados, com **Adicionar dado sensível** e **Guardar trancado**. CPF e RG **não** são tipos de dado sensível na API. **Sem anúncio neste bloco.** |
| Passaporte | Cartão lateral com "N de 22 carimbos" e a porcentagem. |
| Anúncio | Um card nativo na coluna lateral (`web.profile.rail`), só quando a API devolve anúncio. |

### 5.4 Importar currículo (RF-03, RF-04)

Envio de PDF com texto ou DOCX até 10 MB (**Enviar e ler o currículo**). No gratuito, mostra o medidor "Importações de currículo neste mês" (2 por mês). A revisão ("Revise o que lemos") mostra cada item com **Aprovar**, **Editar** e **Descartar**; dado básico diferente do perfil aparece lado a lado, com **Usar o do currículo**, **Manter o do perfil** e **Editar antes de aceitar**. Há **Aceitar tudo que não conflita com o perfil**. O botão **Salvar no Perfil Mestre** só libera quando nenhum item está pendente ("Faltam N para revisar"). **Descartar importação** apaga a proposta. **Sem anúncio** (RN-08).

**Item incompleto.** `missingOf()` junta a validação da seção (`profile-schema.js`), `select` obrigatório cujo valor não é uma opção válida e o `missingFields` que a API marcou na proposta. Se falta algo, o item mostra, dentro do `ReviewItem` (opção `extra`), um aviso acolhedor e só os campos que faltam, destacados (`has-error`, `aria-invalid`); **Aprovar** fica desabilitado (`approveDisabled`) até a pessoa escolher. Para formação sem situação o texto é "O currículo não diz se você ainda está cursando, já concluiu ou trancou. Escolha a situação abaixo para poder aprovar; o Zyft não adivinha por você." **Aceitar tudo que não conflita** pula itens incompletos. O `missingFields` é retirado do corpo do `apply`. Se mesmo assim o `apply` responder 400, o aviso usa a mensagem da API, que nomeia o item.

### 5.5 Banco de respostas (RF-06)

"Responda uma vez e o Zyft guarda." Formulário **Nova resposta** (pergunta e resposta, **Aprovar e guardar**), lista com editar e excluir e a contagem ("N de 15 respostas no plano gratuito"). Ao passar do limite, a API responde 402 e o diálogo de limite aparece (§7). Banner `web.banner` abaixo do título.

### 5.6 Extensão (RF-09, RF-10)

Passo a passo de instalação (com "Instalar em modo de desenvolvedor"), link **Testar no portal de demonstração**, **Gerar código de pareamento** (código no formato `123-456`, contagem regressiva, copiar e gerar outro), tabela **Navegadores conectados** com "N de M no seu plano" e **Desconectar**, e **Sites autorizados** (adicionar pela origem e **Remover**). Banner `web.banner` abaixo do título.

### 5.7 Candidaturas e detalhe (RF-19)

Lista com três números de resumo (candidaturas registradas no mês, campos preenchidos com aprovação e tempo de digitação poupado, estimado em 30 s por campo), filtro por status e busca por vaga ou empresa; tabela com data, vaga, empresa, portal, campos preenchidos e status. Faixa `web.applications.feed` entre o resumo e a lista.

O detalhe mostra endereço da vaga, data de conclusão e **só a quantidade** de campos preenchidos: o registro não guarda quais campos nem os valores. O acompanhamento tem status (Enviada por mim, Em análise pela empresa, Entrevista, Teste, Proposta recebida, Contratado(a), Não seguiu, Desisti, Sem resposta, Arquivada) e anotações, com **Salvar acompanhamento**. **Excluir registro** apaga só o registro no Zyft. Banner `web.banner` abaixo do título. Status e anotações divergem da recomendação do [ADR-0012](adr/0012-registro-minimo-de-conclusao-no-mvp1.md) (ver a nota de divergência no próprio ADR).

### 5.8 Registro de ações (RF-20)

Linha do tempo agrupada por dia, sem valores de campos, com filtros Tudo, Conta, Perfil, Currículo, Extensão, Candidaturas e Planos e anúncios. Os tipos da v2 aparecem em linguagem simples: "Plano alterado", "Pacote avulso ativado", "Anúncio ocultado", "Candidaturas extras por oferta patrocinada", "Passaporte concluído", "Informação da sua área alterada". Banner `web.banner` abaixo do título.

### 5.9 Conta e privacidade (RF-07, RF-08)

Dados da conta, consentimentos (a finalidade "Conta e Perfil Mestre" aparece fixa, como execução de contrato), **Baixar meus dados (JSON)** e **Excluir conta**, que só libera depois de digitar `EXCLUIR` e ainda pede uma última confirmação. **Sem anúncio** (RN-08). Exportar e excluir nunca dependem do plano nem dos limites (RN-12).

### 5.10 Planos (RF-26 a RF-28)

| Elemento | Conteúdo |
|---|---|
| Alternância | Mensal / "Anual · 2 meses grátis" |
| Gratuito | R$ 0, "Para sempre, com anúncios úteis". Lista: Perfil Mestre e onboarding, 10 candidaturas assistidas por mês, +3 por oferta patrocinada (até +9), 5 sugestões de IA por mês, histórico de candidaturas e registro de ações. Medidor das candidaturas do mês quando é o plano atual. |
| Plus | R$ 9,90/mês ou R$ 99/ano (R$ 8,25/mês). Lista: sem anúncios, candidaturas sem limite, IA, análise e importação sem limite, banco de respostas sem limite, até 3 navegadores. Com `trialAvailable: true` (GET /billing): selo "{trialDays} dias grátis" e botão **Testar 7 dias grátis**. Com `false` (já testou, mesmo tendo cancelado): sem selo, botão **Assinar o Plus** e a nota "O teste grátis de 7 dias já foi usado nesta conta; a assinatura começa hoje." O toast depois de assinar usa o `trialEndsAt` devolvido. Para quem já é Plus, a data do teste ou da renovação e **Cancelar assinatura**. O diálogo do 402 também só cita os dias grátis quando disponíveis. |
| Pacote avulso | R$ 4,90, pagamento único: +15 candidaturas assistidas e +10 sugestões de IA, válido por 60 dias. **Comprar pacote** pede confirmação. Lista os pacotes ativos com o saldo. |
| Seu uso neste mês | Medidores (`UsageMeter`) de cada recurso devolvido por `GET /billing.usage`, limites de respostas e navegadores, ofertas patrocinadas vistas no mês e "Recomeça em 1º de {mês}". |
| Compare | Tabela Gratuito × Plus só com o que existe no MVP 1: importação, banco de respostas, candidaturas assistidas, ofertas patrocinadas, análise de vaga, IA, navegadores, histórico e anúncios. |
| Sempre de graça | "Revisar cada campo antes de preencher, dados sensíveis trancados, nunca pedir senha dos portais, consentimentos, baixar e apagar seus dados. Privacidade não é recurso pago." (RN-12) |
| Aviso | Discreto e sempre visível, abaixo do título: "Ambiente de demonstração: nenhuma cobrança real." (RN-13) |

Chamadas: `GET /billing`, `POST /billing/subscribe {period}`, `POST /billing/cancel`, `POST /billing/packs` (as três exigem o token da plataforma; a extensão recebe 403 e por isso abre esta tela). Não há formulário de cartão: o pagamento é simulado. A tela de Planos não tem anúncio.

Itens do mockup de Planos que não estão no contrato (versões do perfil por tipo de vaga, lembretes de retorno, currículo em PDF, suporte com prazo) **não aparecem** nem no Plus nem na tabela (RN-06, RN-07; ver [monetização](../01-produto/monetizacao.md)). O anúncio recompensado não tem botão próprio na web: ele aparece no diálogo do 402 e no painel da extensão.

## 6. Anúncios na web (RF-29, ADR-0020)

| Placement | Formato | Telas |
|---|---|---|
| `web.profile.rail` | Card nativo | Perfil Mestre, coluna lateral |
| `web.applications.feed` | Faixa no feed | Candidaturas, entre o resumo e a lista |
| `web.banner` | Banner horizontal | Banco de respostas, Extensão, Detalhe da candidatura, Registro de ações |

**Nunca**: entrar, criar conta, onboarding, importar currículo, Perfil Mestre fora da coluna (inclusive o bloco Dados sensíveis), conta e privacidade, planos. Para quem é Plus, a API responde 204 em todos os placements. Ver RN-08.

Comportamento do componente (`adSlot` em `web/js/ads.js`):

1. A tela monta o conteúdo principal; o espaço nasce oculto e chama `GET /ads?placement=<p>` em paralelo, sem bloquear a tela.
2. Com anúncio: mostra o selo **Patrocinado** fora do criativo, título, anunciante, a chamada e **Por que estou vendo isso?** (que mostra o campo `reason`). `204`, erro, resposta inválida ou resposta que demora mais de **1,5 s**: o espaço é removido e nada quebra (RNF-15). O timeout é um `Promise.race` entre o pedido e um temporizador de 1.500 ms em `fetchAd`; quando o temporizador vence, `fetchAd` devolve `null` e a resposta atrasada é descartada.
3. Ocultar: **Não tenho interesse** (card nativo), **Ocultar** (faixa) ou o "x" (banner) chamam `POST /ads/{id}/hide {scope:"ad"}` (30 dias). Dentro de "Por que estou vendo isso?", **Não mostrar anúncios sobre {tema}** chama `{scope:"topic"}` (para sempre). Mesmo se a chamada falhar, o anúncio some da tela.
4. Clique na chamada: `POST /ads/{id}/click` (contador agregado, sem dado pessoal) e abre o `url` em nova aba com `rel="noopener noreferrer sponsored"`.
5. Uma tela tem no máximo um espaço de anúncio (RN-11): cada tela monta um só `adSlot`.

O front não envia nada pessoal para escolher o anúncio: a API lê área e cargos desejados no servidor (RN-09, RNF-14). Os anúncios vêm do catálogo estático de demonstração da API.

### 6.1 Rede de anúncios (Google AdSense simulado, ADR-0021)

Quando a API responde `format: "network"`, `adSlot` troca o card por um bloco da rede:

- **Fora do bloco (do Zyft):** selo **Patrocinado** + "Rede de anúncios do Google", **Por que estou vendo isso?** (com o `reason`) e **Ocultar** (`POST /ads/adsense-<slot>/hide {scope:"ad"}`, 30 dias).
- **Dentro do bloco (da rede):** `<ins class="adsbygoogle" style="display:block" data-ad-client data-ad-slot data-ad-format data-full-width-responsive="true" data-adtest="on">`, com uma arte neutra "Anúncio do Google (simulado)". Sem logo nem marca do Google. O `style` vai por CSSOM (CSP sem `'unsafe-inline'`).
- **Tamanhos:** `banner` até 728×90 responsivo (`data-ad-format="horizontal"`); `native` (coluna do Perfil) 300×250; `feed` faixa horizontal responsiva com 100 px de altura. Altura reservada no CSS (`.adsense--*` em `css/app.css`) para não deslocar o layout (RNF-15).
- **Sem clique contado:** não há link nem `POST /click`; o clique de rede seria do Google.
- **`web/js/adsense.js`:** `adsenseUnit(ad, variant)` monta o bloco; `loadAdSense(client, { mock })` carregaria `https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=…` uma vez por sessão, mas **em modo mock não carrega nada** (o snippet real está só em comentário).
- **Antes de produção:** a CSP do `index.html` teria de liberar os domínios do Google (`script-src pagead2.googlesyndication.com`, `frame-src googleads.g.doubleclick.net tpc.googlesyndication.com`, além de `img-src`/`connect-src`), e o script só poderia carregar depois do consentimento num CMP (LGPD). Enquanto a ADR-0021 for Proposta, a CSP não muda.

## 7. Erros e o HTTP 402

O cliente `api.js` converte toda resposta de erro em `ApiError` com `status`, `code`, `title`, `detail`, `errors` e o `problem` completo.

| Situação | Comportamento |
|---|---|
| 400 / 422 | Mensagem de `errors` junto do campo, `aria-invalid`, foco no primeiro inválido. Chave sem campo na tela vai para o resumo do topo como "Rótulo: mensagem". `ApiError.message` também vira "Rótulo: mensagem" quando há `errors` (usado nos toasts), em vez do genérico "Um ou mais campos são inválidos." |
| 401 | Renova uma vez com o refresh; se falhar, volta a `#/entrar` |
| 402 `plan.limit_reached` | Qualquer tela abre o mesmo diálogo, "Você chegou ao limite deste mês": "Você usou {used} de {limit} {recurso} do plano gratuito neste mês" (lê `resource`, `used` e `limit` do topo do ProblemDetails ou de `extensions`). Opções: **Ver oferta** e depois **Ganhar +3 candidaturas** (só para candidaturas assistidas e enquanto houver oferta no mês), **Comprar por R$ 4,90** (só para candidaturas assistidas e IA, os recursos que o pacote cobre) e **Conhecer o Plus**. **Agora não** fecha sem perder o que a pessoa digitou. |
| 403 | Mensagem da API; no Perfil Mestre, o bloco de dados sensíveis explica o consentimento e leva a Conta |
| 404 | "Não encontramos o que você procurou." |
| 409 `ads.rewarded_exhausted` | "Você já usou as 3 ofertas deste mês." |
| 429 | "Muitas tentativas em pouco tempo. Aguarde um minuto e tente de novo." (o cabeçalho `Retry-After` não é lido) |
| 5xx e rede | Mensagem genérica e **Tentar de novo** |

O 402 nunca aparece em editar perfil, exportar ou excluir conta (RN-12).

**Nomes legíveis dos campos.** `fieldLabel(chave)` em `api.js` converte a chave da API no nome visto na tela: `educations[0].status` → "Formação 1 · Situação", `type` → "Tipo", `fullName` → "Nome completo". O dicionário base fica em `api.js`; `profile-schema.js` registra os rótulos dos dados básicos com `registerFieldLabels`. Chave desconhecida vira texto legível ("salaryType" → "Salary type" só como último recurso). Se a mensagem da API já nomeia o item ("Formação “ADM – UEM”: …"), o rótulo não é repetido.

**Quando um endpoint da v2 falha**, a tela continua funcionando:

| Endpoint | Reserva |
|---|---|
| `GET /catalog/areas` | Cópia estática das 8 áreas em `catalog.js`, com o mesmo conteúdo do contrato |
| `GET/PUT /onboarding` | Progresso guardado em `sessionStorage` na aba; 404 ou 405 desligam as chamadas até recarregar |
| `GET /billing` | Telas sem medidor; em Planos, aviso "Não foi possível carregar o seu uso agora" e preços padrão (R$ 9,90, R$ 99, R$ 4,90) |
| `GET /profile/custom-fields` | Usa os campos que vierem no perfil; com 404 ou 405, avisa que o recurso não está disponível |
| `GET /ads` | O espaço some |
| `GET /profile/missing`, `GET /consents` (no onboarding) | Lista vazia |

## 8. Acessibilidade e segurança

**Acessibilidade (RNF-06, WCAG 2.1 AA):** `label` em todo controle; erros ligados por `aria-describedby`; foco visível (`--focus-ring`); `<dialog>` nativo com Esc e foco devolvido a quem abriu; "Pular para o conteúdo"; estados com cor, ícone e texto; `aria-live` para salvamento, código de pareamento e medidores; `prefers-reduced-motion`; funciona até 390 px de largura. No onboarding, uma pergunta por tela, opções em `radiogroup` com setas e "Preencher mais tarde" sempre alcançável por teclado (RNF-17). Anúncios ficam em `<aside>` com `aria-label` "Conteúdo patrocinado" ou "Publicidade".

**Segurança:**

- Access token só em memória; refresh token em `sessionStorage` (some ao fechar a aba). Nada em `localStorage`. O `sessionStorage` também guarda a marca de "onboarding já verificado nesta sessão" e, só como reserva, o progresso do onboarding (ids de passo, sem respostas).
- CSP do `index.html`: `default-src 'self'; script-src 'self'; style-src 'self' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data: https:; connect-src 'self'; base-uri 'self'; form-action 'self'`. As fontes vêm do Google Fonts (o painel da extensão, ao contrário, empacota as suas). `img-src https:` permite imagem de anúncio de outro domínio; hoje o catálogo de demonstração não usa imagens externas, mas abrir isso para anunciantes reais pede revisão com o parecer de privacidade.
- Nenhum HTML vindo da API, inclusive nos textos de anúncio: título, corpo e chamada são inseridos com `textContent`. URLs passam por `safeUrl` (só `http`/`https`).
- Links de anúncio abrem em nova aba com `noopener noreferrer`, sem `Referer`, para não vazar a rota da plataforma.

## 9. Testes

| Nível | Cobertura |
|---|---|
| E2E (Playwright) | Cadastro → onboarding com uma pergunta pulada → alerta no perfil → "Responda agora" → campo personalizado (e recusa de rótulo sensível) → importação → pareamento → candidaturas → registro → planos (assinar, cancelar, pacote) → exportar e excluir |
| Anúncios | Nenhum anúncio em `#/entrar`, `#/boas-vindas`, `#/importar`, `#/conta`, `#/planos` e para Plus (TN-11); no máximo um por tela; `204` não quebra o layout |
| 402 | Simular limite: o diálogo mostra as opções certas para cada recurso e nada do formulário se perde |
| Acessibilidade | axe nas rotas principais; zero violações críticas ou sérias |

Na Entrega 3 v2, as telas foram conferidas por roteiros Playwright com os dois usuários de demonstração, em 1440 px (claro) e 390 px (escuro). Os testes automatizados do repositório cobrem a API (ver [estratégia de testes](../04-qualidade/estrategia-de-testes.md)); a suíte E2E da web ainda não está no CI.

## 10. Evolução

A arquitetura-alvo da v1.0 deste documento (React 18, Vite, TanStack Query, Radix, cookie `HttpOnly` com CSRF) continua sendo a direção para depois do MVP 1. O pacote de componentes React do [design system](design-system.md) facilita essa migração, porque as telas já seguem os mesmos componentes e tokens.

## 11. Limitações conhecidas

| Onde | Limitação |
|---|---|
| Onboarding, mobilidade | "Topa viajar a trabalho?": **Às vezes** vira sim (`availableToTravel = true`). "Mudaria de cidade?": **Talvez** vira vazio (`availableToRelocate = null`). O perfil só guarda sim, não ou vazio. |
| Onboarding, modelo | **Tanto faz** não é um valor do perfil: o modelo de trabalho fica vazio. Por isso "Modelo de trabalho" pode reaparecer em "Responda agora". |
| Onboarding, ferramentas | As ferramentas vão para a experiência que o onboarding está editando (a primeira da lista, a mais recente). Sem experiência cadastrada, viram competências. |
| Onboarding, listas | Experiência, formação e curso editam o primeiro item da lista; os demais se cadastram com "Adicionar outra…" ou no Perfil Mestre. |
| Candidaturas | O detalhe mostra só a quantidade de campos preenchidos, não quais. É uma escolha de privacidade: o registro não guarda os campos. |
| Dados sensíveis | Só os tipos da API (gênero, raça ou etnia, deficiência, religião, orientação sexual, saúde, nome social, outro), com o consentimento ligado. CPF, RG e endereço completo não são guardados como dado sensível; no painel da extensão, campos com essas chaves (e a data de nascimento) aparecem trancados. |
| Recompensado | Na web só aparece dentro do diálogo do 402. O caminho normal é o painel da extensão. |
| Rate limit | `Retry-After` não é lido; a mensagem pede um minuto. |
