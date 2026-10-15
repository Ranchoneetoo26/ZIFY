# Entrega 3: frontend web e extensão

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida (MVP 1) |
| Documento | Frontend web e extensão do navegador (Entrega 3) |
| Versão | 1.0 |
| Data | 15/10/2026 |
| Código | `web/` (plataforma), `web/demo/` (portal de demonstração), `extension/` (MV3) |
| Documentos relacionados | [Fluxos](../01-produto/fluxos.md), [Frontend web](../03-arquitetura/frontend-web.md), [Extensão](../03-arquitetura/extensao.md), [API](../03-arquitetura/api.md) |

## 1. Visão geral

O frontend tem duas partes. As duas consomem a API .NET em `/api/v1`.

| Parte | Onde roda | Papel | Metáfora |
|---|---|---|---|
| **Plataforma web** (`web/`) | Navegador, servida pela própria API em `/` | Preparar: conta, consentimentos, Perfil Mestre, importação, respostas aprovadas, pareamento, candidaturas, registro, privacidade | **Passaporte**: o Perfil Mestre é um documento único e cada item aprovado é um "carimbo" |
| **Extensão** (`extension/`) | Painel lateral do Chrome/Edge | Usar: ler o formulário do portal, mostrar o de-para, preencher só o que a pessoa aprovou | **Copiloto**: mostra e sugere, a pessoa decide e envia |

Escolhas técnicas:

- **Sem build.** HTML, CSS e JavaScript com ES modules. Basta copiar `web/` para o `wwwroot` da API. Assim a entrega fica simples de rodar e de corrigir, e a stack React/Vite do documento de arquitetura fica para depois do MVP 1.
- **Nenhum HTML vindo da API.** Todo o DOM é criado com `createElement` e `textContent`, pelo helper `h()` em `web/js/dom.js` (a extensão tem um helper igual). Isso fecha a porta para XSS com dados do perfil ou da página do portal.
- **CSP** na página (`script-src 'self'`, `connect-src 'self'`) e na extensão (`script-src 'self'; object-src 'none'`).

## 2. Plataforma web

### 2.1 Estrutura de arquivos

```
web/
  index.html              shell: link "pular para o conteúdo", barra de navegação, <main>, região aria-live
  styles.css              design tokens (claro e escuro), estados com cor + texto + símbolo, foco visível
  js/app.js               roteador por hash, guarda de autenticação, foco no título a cada troca de tela
  js/api.js               cliente HTTP único (Bearer, refresh com rotação, ProblemDetails → ApiError)
  js/dom.js               h(), field() acessível, showErrors(), confirmDialog(), announce(), toast()
  js/validate.js          validações que espelham o backend
  js/views/
    auth.js               entrar e criar conta
    onboarding.js         boas-vindas e consentimentos
    consents.js           painel de consentimentos (reusado em Conta)
    profile.js            Perfil Mestre: passaporte, "o que falta", dados básicos, listas com CRUD
    profile-schema.js     campos, enums (nome, rótulo pt-BR, número) e conversões de/para a API
    import.js             envio do currículo, revisão campo a campo, aplicar ou descartar
    answers.js            respostas aprovadas (CRUD)
    extension.js          código de pareamento, dispositivos, sites autorizados
    applications.js       candidaturas: lista, detalhe, status, anotações, exclusão
    audit.js              registro de ações (linha do tempo, filtro por tipo)
    account.js            conta, consentimentos, exportar JSON, excluir conta
  demo/                   portal de vagas de demonstração (cópia do protótipo)
```

### 2.2 Navegação

![Mapa de navegação da plataforma web](img/05-navegacao-web.png)

| Rota | Tela | Acesso |
|---|---|---|
| `#/entrar` | Entrar | Pública. Quem já está logado vai para `#/perfil`. |
| `#/cadastrar` | Criar conta | Pública. Depois do cadastro, vai para `#/boas-vindas`. |
| `#/boas-vindas` | Onboarding: como o copiloto funciona e consentimentos | Logada |
| `#/perfil` | Perfil Mestre (rota padrão) | Logada |
| `#/importar` | Importar currículo | Logada |
| `#/respostas` | Respostas aprovadas | Logada |
| `#/extensao` | Extensão: pareamento, dispositivos, sites | Logada |
| `#/candidaturas`, `#/candidaturas/{id}` | Lista e detalhe | Logada |
| `#/registro` | Registro de ações | Logada |
| `#/conta` | Conta e privacidade | Logada |

Regras do roteador (`app.js`):

- Rota protegida sem sessão vai para `#/entrar`. Uma resposta 401 sem refresh possível também.
- A cada troca de tela, o roteador atualiza `document.title` e marca o item do menu com `aria-current="page"`. Ele também leva o foco ao `<h1>` e anuncia "Tela carregada" na região `aria-live`.
- Enquanto a tela carrega, aparece um aviso com `aria-busy` e texto. Se der erro, aparece a mensagem do ProblemDetails com o botão "Tentar de novo".

### 2.3 Cliente HTTP (`js/api.js`)

| Comportamento | Detalhe |
|---|---|
| Base | `/api/v1` (mesma origem da API, sem CORS) |
| Autenticação | `Authorization: Bearer <accessToken>`. O access token (15 min) fica só em memória. |
| Refresh | Ao receber 401, chama `POST /auth/refresh {refreshToken}` **uma vez** e repete a chamada. Chamadas simultâneas compartilham a mesma renovação. O refresh token novo substitui o antigo (rotação). Se a renovação falhar, a sessão é limpa e a pessoa volta ao login. |
| Onde fica o refresh token | `sessionStorage`, que some ao fechar a aba. Nada vai para `localStorage`. |
| Erros | Viram `ApiError {status, title, detail, errors}`. As chaves de `errors` (ex.: `Email`, `Experiences[0].StartDate`) são normalizadas para camelCase. `showErrors()` mostra cada mensagem junto do input com o mesmo `name` (`aria-invalid` + `aria-describedby`). O que não casa com nenhum campo vai para um alerta no topo do formulário. |
| Rede fora | Mensagem "Não foi possível falar com o servidor…" (status 0) |
| Envelopes | `asList()` aceita lista pura ou `{items}`/`{data}`/`{value}` |

### 2.4 Telas, formulários, validações e chamadas à API

As validações do frontend espelham as do backend (FluentValidation) e só evitam ida e volta à toa: a API continua sendo a fonte de verdade, e os erros dela aparecem no mesmo lugar.

#### Entrar / Criar conta (RF-01)

| Campo | Validação no frontend |
|---|---|
| Nome (cadastro) | obrigatório, até 120 caracteres |
| E-mail | obrigatório, formato `nome@dominio.tld`, até 254 |
| Senha | cadastro: ≥ 8 caracteres, com pelo menos uma letra e um número; login: obrigatória |

API: `POST /auth/register {name, email, password}` e `POST /auth/login {email, password}`, que devolvem `AuthResult {accessToken, refreshToken, expiresIn, user…}`. O 401 do login mostra "E-mail ou senha incorretos". O 409 do cadastro aparece no campo e-mail. O 429 (rate limit) mostra "Muitas tentativas…".

#### Boas-vindas e consentimentos (RF-07)

Explica o copiloto em quatro frases, incluindo "quem envia a candidatura é sempre você". Em seguida vem um interruptor por finalidade (`role="switch"`), com estado em texto e símbolo ("● Ligado" / "○ Desligado") e a data da última alteração:

| Finalidade | O que libera |
|---|---|
| `AiText` | Sugestões de texto (respostas abertas) com fatos aprovados |
| `JobAnalysis` | Análise de vaga na extensão, sem nota nem porcentagem |
| `SensitiveData` | Dados sensíveis segregados. **Começa desligado.** Mesmo ligado, só preenche com marcação manual do campo. |

API: `GET /consents` e `PUT /consents/{purpose} {granted}`. Cada mudança é gravada na hora. Se falhar, o interruptor volta ao estado anterior e a mensagem de erro aparece.

#### Perfil Mestre (RF-02, RF-05)

- **Passaporte:** cabeçalho com nome, título e um "carimbo" por seção, com a contagem de itens aprovados (experiências, formação, cursos, competências, idiomas).
- **O que falta:** `GET /profile/missing` lista as perguntas (`{key, question}`) num aviso. Nada é obrigatório.
- **Dados básicos:** `GET /profile` e `PUT /profile` (campos do `ProfileDto`, em camelCase). O formulário mostra "Alterações não salvas" e "Salvo às HH:MM".
  - Validações: nome completo obrigatório; e-mail válido se informado; LinkedIn, GitHub e portfólio com URL http(s); pretensão salarial numérica ≥ 0; nascimento não futuro; resumo até 2.000 caracteres.
  - Enums: `workModel` (`OnSite`/`Hybrid`/`Remote`). Os booleanos `availableToTravel`, `availableToRelocate` e `hasDriverLicense` usam um select Sim/Não/Não informado, que vira `true`/`false`/`null`.
- **Listas com CRUD:** cada seção tem "Adicionar", e cada item tem "Editar" (editor em linha) e "Excluir" (diálogo de confirmação). Cada item mostra o carimbo "✓ Carimbado". Esc cancela a edição, e o foco volta ao botão de origem.

| Seção | Endpoint | Campos | Validações |
|---|---|---|---|
| Experiências | `/profile/experiences[/{id}]` | company*, role*, startDate*, endDate, isCurrent, tools, activities, results | início não futuro; **fim ≥ início**; "Trabalho aqui atualmente" desabilita e limpa o fim |
| Formação | `/profile/educations[/{id}]` | institution*, course*, level*, status*, startDate*, endDate | **fim ≥ início** |
| Cursos e certificações | `/profile/courses[/{id}]` | name*, type (`Course`/`Certification`), institution, hours, completedAt | horas > 0; conclusão não futura |
| Competências | `/profile/skills[/{id}]` | name*, level (`Basic`/`Intermediate`/`Advanced`) | até 100 caracteres |
| Idiomas | `/profile/languages[/{id}]` | name*, proficiency* (`Basic`…`Native`) | — |

Datas de mês usam `<input type="month">` e vão para a API como `AAAA-MM-01` (`DateOnly`). O `GET` da lista é usado quando existe. Se a rota responder 404 ou 405, a tela usa a lista que veio dentro do `GET /profile`.

#### Importar currículo (RF-03, RF-04)

1. **Envio:** campo de arquivo com área de arrastar e soltar. Validação: só `.pdf` ou `.docx` (extensão e MIME), não vazio, **até 10 MB**. Depois, `POST /resume-imports` (multipart, campo `file`) com aviso de progresso em `aria-live`. Se a resposta vier sem proposta e com status `Pending`/`processing`, a tela consulta `GET /resume-imports/{id}` a cada 2 s, por até 2 minutos.
   - 422/415: "Não conseguimos ler o texto deste arquivo…" (FA-07).
   - 413: arquivo grande.
   - 503: "A leitura automática está indisponível…" (FA-01).
2. **Revisão:**
   - Dados básicos: tabela com **No currículo**, **No seu perfil hoje** e **Decisão** (Aceitar / Editar / Descartar). Diferenças aparecem com o selo "! Diferente do perfil".
   - Itens de lista: cartões com os mesmos três botões. "Editar antes de aceitar" abre o editor completo do item. Itens que já existem no perfil aparecem com o selo "! Já existe no seu perfil".
   - **Tudo começa como Descartar.** O botão "Aceitar tudo que não conflita com o perfil" exige um clique explícito e não mexe em conflitos nem duplicados.
   - Um contador ao vivo mostra "N para aceitar, N editados, N descartados".
3. **Aplicar:** os itens aceitos ou editados são validados com as mesmas regras do Perfil Mestre. Um item inválido abre no modo Editar, com o erro junto do campo. Depois, `POST /resume-imports/{id}/apply` com `{profile:{…campos aceitos}, experiences:[…], educations:[…], courses:[…], skills:[…], languages:[…]}`. A tela de resultado mostra "N itens aplicados e N descartados" e leva ao Perfil Mestre.
4. **Descartar:** pede confirmação e chama `POST /resume-imports/{id}/discard`.

#### Respostas aprovadas (RF-06)

CRUD em `/answers`: `GET` lista, `POST {question, text}` cria, `PUT /answers/{id}` edita, `DELETE /answers/{id}` exclui (com confirmação). Cada resposta mostra a data em que foi aprovada e o último uso. Validações: pergunta obrigatória (≤ 500); resposta obrigatória (≤ 4.000).

#### Extensão (RF-09, RF-10)

- **Gerar código de pareamento:** `POST /extension/pairing-codes` devolve `{code, expiresAt}`. O código de 6 dígitos aparece grande, com contagem regressiva de 10 minutos, e é anunciado dígito a dígito no `aria-live`.
- **Dispositivos pareados:** `GET /extension/devices` em tabela com nome, data de pareamento, último uso e situação ("● Ativo" / "○ Desconectado"). "Desconectar" pede confirmação e chama `DELETE /extension/devices/{id}`.
- **Sites autorizados:** `GET /authorized-sites`; `POST {origin}` aceita só a origem, com protocolo e domínio e sem caminho; `DELETE /authorized-sites/{id}` remove.
- O cartão de instalação explica como carregar a extensão e aponta para o portal de demonstração.

#### Candidaturas (RF-19, ADR-0012)

- **Lista:** `GET /applications`, em tabela com data, vaga, empresa, portal, campos preenchidos e status.
- **Detalhe:** `GET /applications/{id}`. Alterar status e anotações usa `PATCH /applications/{id} {status, notes}`; os status são os nomes do enum `ApplicationStatus` (`Confirmed`, `InReview`, `Interview`, `Test`, `Offer`, `Hired`, `Rejected`, `Withdrawn`, `NoResponse`, `Archived`), com rótulos em pt-BR. Excluir chama `DELETE`, com confirmação que explica que a candidatura continua no portal.
- Não há métricas de sucesso, nota nem porcentagem (RN-06). O registro só nasce do clique da pessoa na extensão.

#### Registro de ações (RF-20)

`GET /audit-events` mostra a linha do tempo "data — ação em portal · N campos", com filtro por tipo. As ações da lista fechada `AuditActions` (`conta.criada`, `formulario.mapeado`, `candidatura.confirmada`…) aparecem em linguagem simples. **Nunca há valores de campos.**

#### Conta e privacidade (RF-07, RF-08)

- Dados da conta (`GET /me`) e o painel de consentimentos.
- "Baixar meus dados (JSON)" chama `GET /me/export` e baixa `zyft-meus-dados-AAAA-MM-DD.json`.
- "Excluir minha conta" abre um diálogo modal que explica o que é apagado, inclusive o acesso das extensões pareadas. O botão só fica ativo depois de a pessoa **digitar `EXCLUIR`**. Então chama `DELETE /me`, limpa a sessão e volta ao login.

### 2.5 Acessibilidade (WCAG 2.1 AA)

| Critério | Como foi atendido |
|---|---|
| Rótulos | Todo controle tem `<label for>`. Os opcionais mostram "(opcional)", e os obrigatórios têm `required` e texto para leitor de tela. Dicas e erros ficam ligados por `aria-describedby`. |
| Erros | Ficam em texto ("Erro: …") junto do campo, com `aria-invalid`. O foco vai para o primeiro campo inválido, e um resumo é anunciado em `aria-live`. |
| Teclado | Os elementos são nativos (links, botões, `<dialog>` modal com foco preso e Esc). O link "Pular para o conteúdo" está presente, e o foco vai ao `<h1>` a cada troca de tela. Os editores em linha usam `aria-expanded` e devolvem o foco ao fechar. |
| Foco visível | `:focus-visible` com contorno de 3 px em cor de alto contraste, nos dois temas |
| Cor | Estados sempre com texto e símbolo, além da cor ("✓ Carimbado", "! Diferente do perfil", "● Ligado"). Os tokens foram escolhidos para contraste ≥ 4,5:1. |
| Mensagens assíncronas | Uma região `#live` (`aria-live="polite"`) e o toast com `role="status"` |
| Movimento | `prefers-reduced-motion` desliga animações |
| Responsivo | Funciona até 390 px de largura. O menu rola na horizontal, e a tabela de revisão vira uma lista com o nome de cada coluna. |
| Linguagem | Frases curtas, sem jargão (RNF-11) |

## 3. Extensão (`extension/`)

### 3.1 Componentes

| Arquivo | Papel |
|---|---|
| `manifest.json` | MV3, "Zyft — Candidatura Assistida" 0.3.0.<br>Permissões: `sidePanel`, `storage`, `scripting`, `activeTab`, `tabs` (URL e título da aba no painel).<br>`host_permissions`: localhost e 127.0.0.1 (API e portal demo). Outros sites entram por `optional_host_permissions`, pedidos com um clique da pessoa. |
| `background.js` | Service worker. **Único** arquivo que fala com a API e guarda tokens:<br>• access token em `chrome.storage.session` (`TRUSTED_CONTEXTS`);<br>• refresh token e URL da API em `chrome.storage.local`.<br>Renova uma vez ao receber 401, com rotação. Manda `X-Zyft-Extension-Version` e ignora mensagens vindas de abas. |
| `content.js` | Crawler e filler injetados sob demanda (`scripting.executeScript`). Valida `sender.id`, descarta `password` antes de ler e recusa `file`, `password` e botões. **Nunca** chama `submit()` ou `requestSubmit()` nem clica em botões. O observador de `submit` só avisa o painel e não chama `preventDefault`. |
| `sidepanel.html/js/css` | Painel lateral: pareamento, leitura, revisão, preenchimento e conclusão |

Mensagens do painel para o service worker: `{type:'api', method, path, body}`, `{type:'pair', code, deviceName}`, `{type:'unpair'}`, `{type:'status'}`, `{type:'setApiBase', url}`. Do painel para a página: `scan`, `job`, `fill`, `highlight`, `ping`. Da página para o painel: `submitted`.

### 3.2 Estados do painel e chamadas à API

![Estados do painel lateral](img/06-estados-painel.png)

| Estado | O que a pessoa vê | API |
|---|---|---|
| Não pareado | Campo do código de 6 dígitos (`inputmode=numeric`, validado) e nome do dispositivo sugerido (ex.: "Chrome no Linux") | `POST /extension/pair {code, deviceName}` → `AuthResult` |
| Ocioso | Página e conector atuais, "Analisar vaga" e "Ler formulário" | — |
| Página não suportada | Origem fora de `GET /authorized-sites` e diferente de localhost: explicação e "Usar neste site mesmo assim", que pede a permissão do navegador e registra o site | `GET` / `POST /authorized-sites` |
| Lendo | "Lendo N campos…" | `POST /forms/mappings {pageUrl, fields[]}` |
| Revisão | Resumo "Vamos preencher N campos. M precisam de você." e um cartão por campo | — |
| Preenchendo / Resumo | Campos preenchidos e falhas, mais o lembrete "Confira no portal e envie quando estiver pronto(a). O Zyft nunca envia por você." | — |
| Concluído | "Concluí a candidatura": formulário com portal, cargo, empresa e campos preenchidos, enviado **só com o clique da pessoa**. O cartão é destacado quando a página avisa que o formulário foi enviado pela pessoa. | `POST /forms/completions {portal, jobTitle, company, fieldsFilled}` |
| Erro | Título e detalhe do ProblemDetails e "Tentar de novo". No 403 de consentimento, aponta para a plataforma. | — |

Estados de cada campo (resposta `FieldSuggestion.state`), sempre com texto, cor e ícone:

| Estado | Rótulo | Vem marcado? |
|---|---|---|
| `ready` | Pronto | Sim. Sensível com consentimento sempre vem desmarcado. |
| `ambiguous` | Escolha (com as alternativas) | Não |
| `missing` | Falta no perfil (a pessoa pode digitar) | Não |
| `divergent` | Já preenchido no portal: "Manter o do portal" (padrão) ou "Usar o do perfil" | Não |
| `blocked` | Ação sua: sensível, termos, anexo ou CAPTCHA, com a explicação. **Sem caixa de seleção.** | Nunca |
| texto da IA | Sugestão — revise | Não |

- "Sugerir texto", em campos de texto livre ou `kind=openQuestion`, chama `POST /forms/open-answers {question, maxLength}`. A sugestão chega desmarcada e editável, com os "fatos aprovados usados". "Salvar como resposta aprovada" chama `POST /answers {question, text}`.
- "Analisar vaga" chama `POST /jobs/analyze {text}`. A tela mostra os requisitos com status (atende / parcial / não encontrado / confirmar) e a evidência, que é a linha numerada do perfil. A contagem aparece como "X de Y requisitos aparecem no seu perfil", **sem barra, nota ou porcentagem** (RN-06).
- O preenchimento envia ao content script só os campos marcados e não bloqueados. Como defesa extra, anexo e CAPTCHA são bloqueados pelo próprio painel, mesmo se a API errar.

### 3.3 Como carregar a extensão (modo desenvolvedor)

1. Suba a API em `http://localhost:5080`, que também serve a plataforma em `/` e o portal demo em `/demo/`.
2. Abra `chrome://extensions` (ou `edge://extensions`) e ligue o **Modo do desenvolvedor**.
3. Clique em **Carregar sem compactação** e escolha a pasta `extension/` do repositório.
4. Fixe o ícone do Zyft e clique nele para abrir o painel lateral.
5. Na plataforma web, entre em **Extensão → Gerar código de pareamento** e digite o código no painel.
6. Abra `http://localhost:5080/demo/` e teste:
   - `vaga.html`: Analisar vaga;
   - `curriculo.html` e `curriculo.html?preenchido=1`: leitura, preenchimento e divergência;
   - `candidatura.html`: pergunta aberta, dado sensível, termos, anexo e CAPTCHA aparecem como "Ação sua". O formulário **não** é enviado pela extensão.
7. Se a API rodar em outro endereço, troque em **Ajustes** (engrenagem) no painel. Endereços fora do localhost pedem permissão do navegador.

## 4. Testes realizados

- `node --check` (e `--input-type=module`) em todos os arquivos JS da web e da extensão.
- Teste ponta a ponta da plataforma com Playwright (Chromium headless) contra um mock da API com o contrato desta entrega. Todos os passos passaram, sem erros no console:
  - redirecionamento para o login;
  - validação do cadastro (e-mail e senha) e cadastro concluído;
  - interruptor de consentimento;
  - Perfil Mestre: enum carregado no select, data de fim antes do início bloqueada junto do campo, nova experiência carimbada, salvar dados básicos;
  - importação: arquivo `.txt` recusado; revisão; "aceitar tudo que não conflita" sem aceitar o conflito de nome nem a competência duplicada; aplicar;
  - resposta aprovada criada;
  - código de pareamento exibido e site autorizado (origem com caminho recusada);
  - status da candidatura alterado;
  - registro de ações exibido;
  - exclusão da conta bloqueada até digitar `EXCLUIR`.
- A extensão não foi exercitada contra a API real, porque o backend ainda está em implementação. Ver as suposições abaixo.

## 5. Suposições de contrato (a confirmar com o backend)

| Endpoint | Suposição |
|---|---|
| `POST /auth/register` | Devolve `AuthResult` com tokens. Se não devolver, o frontend faz login em seguida. |
| `POST /auth/logout` | Corpo `{refreshToken}` |
| `GET /consents`, `PUT /consents/{purpose}` | `purpose` serializado como texto (`AiText`, `JobAnalysis`, `SensitiveData`). A mesma forma é usada na URL. |
| Enums em geral | Serializados como **nome** (`JsonStringEnumConverter`). A web também aceita número e rótulo; a extensão aceita número para `state`/`source`. |
| `GET /profile/{lista}` | Existe e devolve a lista. Se não existir, a lista vem do `GET /profile`. `POST` devolve o item criado com `id`; `PUT /{id}` edita. |
| `POST /resume-imports` | Devolve `{id, fileName, status, proposal}`, com `proposal` em objeto ou JSON string, no formato `{profile:{…}, experiences, educations, courses, skills (texto ou objeto), languages}` |
| `POST /resume-imports/{id}/apply` | Corpo `{profile:{campos aceitos}, experiences:[], educations:[], courses:[], skills:[], languages:[]}` (sem `id`) |
| `PUT /answers/{id}` | Edição de resposta aprovada, corpo `{question, text}` |
| `POST /extension/pairing-codes` | Devolve `{code, expiresAt}` |
| `DELETE /authorized-sites/{id}` | Usa o `id` do site (ou a origem, se o item não tiver `id`) |
| `PATCH /applications/{id}` | `{status, notes}` |
| `GET /audit-events` | Itens `{action, portal?, fieldCount?, occurredAt}` |
| `POST /forms/mappings` | Cada campo vai com `id` **e** `fieldId`. A resposta `{mappings:[FieldSuggestion]}` é tratada conforme `Forms/FormModels.cs` (`state`, `source`, `kind`, `selected`, `profileLabel`, `profileValue`, `note`). |
| `POST /forms/open-answers` | Devolve `{text\|value, usedFacts?, note?}` |
| `POST /jobs/analyze` | `{job?, requirements:[{text, kind?, status, evidence}], disclaimer?}`, com `evidence` em texto ou `{line, text}` |
| Serviço da web | A API publica `web/` no `wwwroot`, ou seja, em `/`, e roda em `http://localhost:5080` (padrão da extensão) |
