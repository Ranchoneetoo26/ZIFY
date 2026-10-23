# Arquitetura da extensão de navegador

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Arquitetura da extensão |
| Versão | 2.3 |
| Status | Vigente para a v2 do MVP 1. O primeiro portal real depende do [ADR-0011](adr/0011-primeiro-portal-e-conectores-por-portal.md); anúncios e limites dependem do [ADR-0020](adr/0020-monetizacao-com-anuncios-e-planos.md) (ambos propostos). |
| Dono | Arquitetura de software, com o dev frontend (painel) |
| Documentos relacionados | [Design system](design-system.md), [API](api.md), [Frontend web](frontend-web.md), [Segurança](seguranca.md), [ADR-0002](adr/0002-extensao-de-navegador-mv3-com-painel-lateral.md), [ADR-0007](adr/0007-autenticacao-e-pareamento-da-extensao.md), [ADR-0009](adr/0009-nunca-enviar-confirmacao-explicita.md), [ADR-0010](adr/0010-sem-credenciais-sem-contorno-de-seguranca-campos-bloqueados.md), [ADR-0017](adr/0017-backend-em-csharp-dotnet-clean-architecture.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | Arquitetura-alvo em React dentro do monorepo TypeScript, com pareamento por PKCE e um portal em `host_permissions`. |
| 2.0 | 21/10/2026 | Reescrito para a extensão real (JavaScript sem build, pareamento por código de 6 dígitos, API .NET em `/api/v1`) e para a v2: visual do design system, quatro estados de campo, Prontos compactos com "Aprovar os N", sensível um a um, rascunho da IA com declaração de veracidade, anúncios no rodapé, contextual e após concluir, medidor de uso, "Ganhar +3" e tratamento do HTTP 402. |
| 2.1 | 21/10/2026 | Sincronizado com a extensão 0.4.0 (painel reescrito): ícones e fontes empacotadas, correções do `background.js` (formato dos tokens do pareamento e origem das mensagens), duas abas no painel, mapeamento dos 6 estados internos para os 4 visuais, regras reais de anúncio (nenhum anúncio desconectada nem na aba Formulário; contextual com até 3 requisitos; recompensado no espaço `ext.panel.banner`), travas de sensível por lista de chaves e divergências conhecidas (§14). |
| 2.2 | 21/10/2026 | Correções conferidas no código: "Desconectar extensão" revoga o próprio dispositivo (`DELETE /extension/devices/current`) e libera a vaga de navegador; timeout de 1,5 s no pedido de anúncio (RNF-15); `personal.birthDate` entra em `SENSITIVE_KEYS`; regras de LinkedIn e GitHub voltaram a casar ([IA](ia.md)); `POST /billing/packs` recusa o token da extensão (403). Pendências resolvidas saíram do §14. |
| 2.3 | 21/10/2026 | Rascunho da IA: todo 200 de `/forms/open-answers` aparece no cartão (rascunho, resposta aprovada reaproveitada ou aviso "A IA não conseguiu escrever um rascunho…" com a caixa já aberta); aceita `value`, `text`, `answer` ou `draft` e ignora texto só com espaços. Anúncios: **Não tenho interesse no tema** (`scope: "topic"`) ao lado de **Ocultar**; a pendência "Ocultar assunto" saiu do §14. |
| 2.4 | 22/10/2026 | Rede de anúncios Google AdSense **simulada** ([ADR-0021](adr/0021-rede-de-anuncios-google-adsense.md)) no banner do rodapé e no cartão pós-conclusão: bloco `<ins class="adsbygoogle">` com arte neutra, sem script remoto (MV3) (§10.1). Para produção, a extensão fica só com anúncios diretos. |

## 1. Papel da extensão

A extensão é a única parte do Zyft que toca a página do portal. Ela:

- reconhece a aba ativa e diz a verdade sobre o site (RF-10);
- lê os campos visíveis e o texto da vaga, só quando a pessoa pede (RF-11, RF-17);
- mostra o de-para proposto pela API para revisão (RF-12, RF-13, RF-16, RF-18);
- preenche só os campos que a pessoa aprovou (RF-14).

Ela **não** decide nada sozinha, **não** envia, **não** faz requisições ao portal e **não** guarda o Perfil Mestre.

## 2. Componentes (Manifest V3, versão 0.4.0)

| Arquivo | Responsabilidade |
|---|---|
| `extension/manifest.json` | MV3, "Zyft — Candidatura Assistida", versão `0.4.0`. Permissões, ícones e CSP (§3). |
| `extension/background.js` | Service worker. **Único** que fala com a API e guarda tokens. Abre o painel ao clicar no ícone, renova o access token uma vez ao receber 401 (uma renovação por vez, porque o refresh é rotativo), manda `X-Zyft-Extension-Version` e **só aceita mensagens de páginas da própria extensão** (§4). |
| `extension/content.js` | Crawler e filler, injetado sob demanda com `chrome.scripting.executeScript`. Valida `sender.id`, descarta `password` antes de ler o valor e recusa `file`, `password` e botões. |
| `extension/sidepanel.html`, `sidepanel.js`, `sidepanel.css` | Painel lateral, reescrito na 0.4.0: conectar, aba "A vaga e você", aba "Formulário" e tela de conclusão. Os componentes do design system (`StatusBadge`, `Stamp`, `WhyThis`, `ProgressSummary`, `SiteSupport`, `RequirementMatch`, `UsageMeter`) são recriados em DOM puro dentro de `sidepanel.js`. Os tokens de cor ficam em `sidepanel.css`, com tema escuro por `prefers-color-scheme`. |
| `extension/icons/` | `icon-16/32/48/128.png` (ícone da barra e da loja) e `zyft-mark.svg` (logo do cabeçalho e imagem padrão dos anúncios). |
| `extension/fonts/` | Archivo e JetBrains Mono em `woff2` (subconjunto latino), empacotadas na extensão e carregadas por `@font-face`. As duas usam a SIL Open Font License 1.1 (`LICENCA-FONTES.txt`). Nada é baixado de CDN: a CSP da extensão só permite `font-src 'self'`. |

O content script não é declarado em `content_scripts`: só entra na página quando a pessoa aciona uma ação. Isso reduz a superfície de ataque e o custo em páginas que não interessam.

## 3. Manifesto real

```json
{
  "manifest_version": 3,
  "name": "Zyft — Candidatura Assistida",
  "version": "0.4.0",
  "icons": { "16": "icons/icon-16.png", "32": "icons/icon-32.png", "48": "icons/icon-48.png", "128": "icons/icon-128.png" },
  "permissions": ["sidePanel", "storage", "scripting", "activeTab", "tabs"],
  "host_permissions": ["http://localhost/*", "http://127.0.0.1/*"],
  "optional_host_permissions": ["https://*/*", "http://*/*"],
  "background": { "service_worker": "background.js", "type": "module" },
  "side_panel": { "default_path": "sidepanel.html" },
  "action": { "default_title": "Abrir o Zyft", "default_icon": { "16": "icons/icon-16.png", "…": "…" } },
  "content_security_policy": { "extension_pages": "script-src 'self'; object-src 'none'; font-src 'self'" }
}
```

| Permissão | Por quê |
|---|---|
| `sidePanel` | O painel lateral é a interface (ADR-0002) |
| `storage` | Tokens, endereço da API e última vaga analisada (§7) |
| `scripting` | Injetar o content script sob demanda |
| `activeTab` | Agir só na aba em que a pessoa clicou |
| `tabs` | Ler URL e título da aba para mostrar no painel e perceber troca de aba |
| `host_permissions` localhost e 127.0.0.1 | API local e portal de demonstração |
| `optional_host_permissions` | Qualquer outro site só com o clique da pessoa em "Usar neste site mesmo assim", que dispara o pedido de permissão do navegador e registra a origem em `POST /authorized-sites`. Também é pedida quando a pessoa aponta a API para um endereço fora do localhost em "Ajustes". |

Para publicar na loja: trocar `localhost` pelo domínio da API de produção e pelo(s) portal(is) do ADR-0011, revisar se `tabs` ainda é necessário (o `activeTab` cobre a maior parte) e justificar cada permissão na ficha da loja.

## 4. Mensagens

Há dois canais, com formatos diferentes.

**Painel → service worker** (`chrome.runtime.sendMessage`, campo `type`):

| `type` | Payload | Resposta |
|---|---|---|
| `api` | `{method, path, body}` | `{ok, status, data}` ou `{ok: false, status, problem}` (ProblemDetails). Com `paired: false` quando a conexão expirou ou foi revogada. |
| `pair` | `{code, deviceName}` | `{ok, data: {paired: true}}` ou o erro da API (inclusive 402) |
| `unpair` | — | Chama `DELETE /extension/devices/current` (revoga o próprio dispositivo e libera a vaga de navegador; uma falha aqui não impede desconectar), depois `POST /auth/logout` com o refresh token, e apaga os tokens locais |
| `status` | — | `{paired, apiBase}` |
| `setApiBase` | `{url}` | Igual a `status` |

**Painel → content script** (`chrome.tabs.sendMessage`, campo `cmd`):

| `cmd` | Payload | Resposta |
|---|---|---|
| `scan` | — | `{fields, url, title, host}` |
| `job` | — | `{url, title, text}` (texto visível da vaga, até 15.000 caracteres) |
| `fill` | `{items: {fieldId, value}[]}` | `{report: {fieldId, ok, error?}[]}` |
| `highlight` | `{fieldId}` | `{}` (rola até o campo e destaca por 1,6 s) |
| `ping` | — | `{ok: true}` |

**Content script → painel**: `{type: 'submitted', url, title}`, aviso de que **a pessoa** enviou o formulário. O painel só reage se a mensagem vier da aba que ele acompanha e se a candidatura ainda não foi registrada; então abre a tela de conclusão.

Regras:

- O service worker só atende mensagens de páginas da própria extensão: `sender.id` precisa ser o da extensão **e** `sender.url` precisa começar com `chrome-extension://<id>/`. Mensagens de content scripts (que trazem a URL do portal) são ignoradas. Isso vale também para o painel aberto numa aba comum com `?tabId=N`, usado nos testes.
- O content script nunca fala com a API e não tem tokens; valida `sender.id === chrome.runtime.id`; ignora `window.postMessage` da página; os `fieldId` (atributo `data-zyft-id`) valem só para aquela leitura.
- Se o content script ainda não estiver na página, o painel injeta `content.js` e repete a mensagem.

## 5. Crawler (leitura)

1. **Raiz**: se houver diálogo visível (`[role=dialog]`, `[aria-modal=true]`, `dialog[open]`), lê só o último; senão, o documento.
2. **Campos**: `input`, `select` e `textarea` visíveis e habilitados. Descarta `hidden`, `submit`, `button`, `reset`, `image` e **`password` sem ler o valor** (RNF-01).
3. **Rótulo**, em ordem: `aria-labelledby`, `label[for]`, `label` envolvente, `aria-label`, texto do irmão anterior, texto do pai (se curto), `placeholder`.
4. **Seção**: `legend` do `fieldset` ou o último título antes do campo.
5. **Radios** agrupados por `name`; o rótulo do grupo é o texto do contêiner comum.
6. **Metadados**: tipo, `name`, `id`, `placeholder`, obrigatório, `maxLength`, opções e valor atual (`file` sempre vazio).

Leitura da vaga: `main`, `article` ou `[role=main]` (senão `body`), título do `h1`, texto visível.

| Limitação | Efeito |
|---|---|
| Iframes e shadow DOM | Campos não lidos |
| Selects de bibliotecas e autocomplete | Opções não lidas; o campo vira "Precisa de você" |
| Formulários em várias etapas | A pessoa clica em "Ler novamente (nova etapa)". Se a página muda dentro do mesmo site, o painel mantém a aba Formulário e conta a etapa. |

## 6. Preenchimento e o que a extensão nunca faz

- Texto, e-mail, telefone, data e textarea: setter nativo de `value`, depois `input`, `change` e `blur` com `bubbles: true` (frameworks como React e Vue percebem a mudança).
- Select só com opção existente (por valor ou pelo texto); radio e checkbox por `click()` no próprio input, só quando o estado precisa mudar.
- `file`, `password`, `submit`, `button`, `image`, `reset` e `hidden` são recusados mesmo que cheguem num `fill`.
- Cada campo preenchido recebe um contorno verde por 4 s; falha, contorno vermelho. O painel mostra o resumo com as falhas.

| Nunca | Como é garantido |
|---|---|
| Enviar, salvar, avançar ou aceitar | Não há código que chame `submit()`, `requestSubmit()` ou clique em botões. O observador de `submit` só avisa o painel e não chama `preventDefault`. Teste TN-01. |
| Ler ou preencher senha | Descartada antes de ler; recusada no fill. TN-02. |
| CAPTCHA, código de verificação, anexo, termos | Classificados como "Ação sua" pela API e, como defesa extra, pelo próprio painel (`file`, `password` e rótulos com "captcha" ou "código de segurança" viram "Ação sua" sem valor). TN-03, TN-04. |
| Requisições ao portal | O content script não usa `fetch`; só o service worker fala com a API. TN-07. |
| Agir sem a pessoa | Toda leitura e todo preenchimento começam por um clique no painel. |

## 7. Armazenamento

| Dado | Onde |
|---|---|
| Access token (15 min) | `chrome.storage.session`, com `setAccessLevel({accessLevel: 'TRUSTED_CONTEXTS'})`: content scripts não leem |
| Refresh token (7 dias, rotativo) e `deviceId` | `chrome.storage.local` |
| Endereço da API | `chrome.storage.local` (`apiBase`), padrão `http://localhost:5080`, alterável em Ajustes |
| Última vaga analisada (título, empresa, URL) | `chrome.storage.session` (`lastJob`); a pessoa pode "esquecer" |
| Perfil Mestre | **Não é armazenado** (RNF-02) |
| Resultado do de-para, plano e uso, anúncios exibidos | Memória do painel; some ao fechar |

## 8. Pareamento

1. Na plataforma, em Extensão, a pessoa gera um código de 6 dígitos (`POST /extension/pairing-codes`), válido por 10 minutos e uma única vez.
2. No painel ("Conecte este navegador à sua conta"), digita o código e o nome do navegador (sugerido, por exemplo "Chrome no Linux").
3. O service worker chama `POST /extension/pair`. A API responde `{device: {id, …}, tokens: {accessToken, refreshToken, …}}`. O service worker guarda os tokens de `tokens` e o `device.id`. (Até a 0.3 o código lia os tokens no nível de cima da resposta e o pareamento não guardava nada; a 0.4.0 corrigiu e ainda aceita o formato plano antigo.)
4. Revogar o navegador na plataforma derruba access e refresh na hora; na próxima chamada o painel volta para "Conectar".

No plano gratuito, a conta tem **1 navegador** conectado; no Plus, até 3. Parear além do limite responde 402 (`plan.limit_reached`, `resource=devices`) e o painel mostra a mensagem da API com os links "Ver navegadores conectados na plataforma" e "Conhecer o Plus".

"Desconectar extensão" (em Ajustes) chama primeiro `DELETE /extension/devices/current`: a API revoga o próprio dispositivo (o `device_id` do token), derruba os refresh tokens dele e **libera a vaga de navegador** do plano. Depois o service worker chama `POST /auth/logout` e apaga os tokens locais. Se a chamada de revogação falhar (API fora do ar, por exemplo), o painel desconecta mesmo assim; nesse caso o navegador continua na lista da plataforma e pode ser revogado em Extensão. A rota aceita `ExtensionOrWeb`, mas um token web não tem `device_id` e recebe 404. Teste: `Extensao_que_se_desconecta_libera_a_vaga_de_navegador_e_nao_compra_pacote`.

O fluxo PKCE do ADR-0007 continua como evolução.

## 9. O painel na v2

O painel segue o [design system](design-system.md): cabeçalho em petróleo com o logo e um texto de situação ("Não conectada", "Conectada", "Lendo só esta vaga", "Lendo só este formulário"), texto de 14 px em Archivo, rótulos técnicos em JetBrains Mono, tema claro e escuro. Há link "Pular para o conteúdo", foco visível, abas navegáveis por setas e avisos em `aria-live`.

### 9.1 Telas

| Tela | O que a pessoa vê | API |
|---|---|---|
| Não conectada | "Conecte este navegador à sua conta", campo do código de 6 dígitos, nome do navegador, "Ajustes" (endereço da API) e a nota "O Zyft nunca pede a senha dos portais. A extensão só lê o formulário da página aberta, quando você pede." **Sem anúncio.** | `POST /extension/pair` |
| Página atual | Endereço da página e `SiteSupport`. No `localhost` e em `127.0.0.1` (portal de demonstração): "Compatível com o portal de demonstração". Em outros sites: "Compatibilidade parcial com este site". Se o site ainda não foi autorizado, aparece o quadro "Site ainda não autorizado" com "Usar neste site mesmo assim". | `GET/POST /authorized-sites` |
| Aba "A vaga e você" | "Analisar esta vaga"; depois, dados da vaga (cargo, empresa, modalidade, contrato, local, salário) e `RequirementMatch` ("Aparece no seu perfil", "Aparece em parte", "Não encontramos no seu perfil"), com "Isto não é uma nota e não garante aprovação." Um anúncio (§10). No rodapé: "Ler o formulário desta página", medidor de uso, "Ganhar" e o link para o Plus. | `POST /jobs/analyze`; `GET /billing`; `GET /ads` |
| Aba "Formulário" | `ProgressSummary` no topo; Prontos compactos com "Aprovar os N"; depois, um a um, os sensíveis, as sugestões da IA, o que precisa de você e o que é "Ação sua". Rodapé com "Preencher N campos aprovados", "Ler novamente (nova etapa)" e "Concluí a candidatura". **Sem anúncio.** | `POST /forms/mappings`, `/forms/open-answers`, `POST /answers` |
| Resumo do preenchimento | No topo da aba Formulário: "N preenchidos com a sua aprovação · N pulados por você · N para fazer no site", lista de falhas e "Ler novamente (nova etapa)". | — |
| Concluir | "Você enviou o formulário?" e "O Zyft não envia nada…", campos Vaga e Empresa (pré-preenchidos com a última vaga analisada) e "Confirmo que enviei: registrar". Abre sozinha quando a pessoa envia o formulário no site. **Sem anúncio.** | `POST /forms/completions` |
| Candidatura registrada | "Candidatura registrada", carimbo "Registrado por você", link "Abrir Candidaturas" e, depois, o anúncio "Próximo passo". | `GET /ads?placement=ext.after-complete` |

Mensagens de erro aparecem em linguagem simples, com "Tentar de novo" quando faz sentido. Um 403 de consentimento orienta a ativar o recurso em "Conta e privacidade" na plataforma.

### 9.2 Seis estados internos, quatro estados visuais

A API devolve cinco estados por campo (`Ready`, `Ambiguous`, `Missing`, `Divergent`, `Blocked`, como texto ou número). O painel acrescenta um sexto, o rascunho da IA. Os seis viram os quatro estados do design system:

| Estado interno | Estado visual | Rótulo no cartão | Vem aprovado? | Ações |
|---|---|---|---|---|
| `ready` | Pronto | Pronto | Não. Aprovação por linha ou em "Aprovar os N" (não sensíveis) | Aprovar, Pular; Desfazer depois |
| `ambiguous` | Precisa de você | Escolha | Não | Escolher entre as opções do site, Aprovar, Pular |
| `missing` | Precisa de você | Falta no perfil (ou "Precisa de você", se for pergunta aberta) | Não | Responder ou Escrever, "Pedir um rascunho à IA" (pergunta aberta), Pular; "Guardar esta resposta no Banco de respostas" vem marcado |
| `divergent` | Precisa de você | Já preenchido no site | Não. "Manter o do site" é o padrão; escolher "Usar o do perfil" já aprova | Escolher |
| `blocked` | Ação sua | Ação sua | Nunca, sem caixa de seleção | "Mostrar no site" e o motivo (anexo, código de segurança, sensível sem consentimento, termos e declarações) |
| rascunho da IA | Sugestão da IA – revise | Sugestão da IA – revise, "Resposta já aprovada – confira" (reaproveitada do Banco de respostas), "Rascunho editado por você", "Rascunho aprovado por você" ou "Resposta aprovada por você" | Não | Editar, aprovar com a declaração, "Outro começo", "Escrever do zero" |

Campos com chave `custom:<id>` (campos personalizados, RF-25) aparecem como qualquer outro Pronto, com a origem "Perfil Mestre · Informações da sua área · <rótulo>". Todo cartão mostra a origem do valor ("Perfil Mestre · Contato", "Banco de respostas" etc.) e, quando o Zyft não preencheu, "Por que o Zyft não preencheu?".

### 9.3 Prontos compactos e "Aprovar os N"

Os campos Prontos e **não sensíveis** aparecem em linhas compactas no topo ("Prontos · confira de relance"), com o botão "Aprovar os N". Ele aprova só essas linhas, do formulário lido. Cada linha tem o botão de aprovar e desfazer; clicar no rótulo mostra o campo no site. O botão principal descreve o resultado: "Preencher 14 campos aprovados".

### 9.4 Sensível um a um

O painel trata como sensível o campo que a API marca com `kind = sensitive` ou cuja chave está na lista `SENSITIVE_KEYS`: `personal.cpf`, `personal.rg`, `personal.address`, `personal.birthDate` e qualquer `sensitive.*` (os tipos da [ADR-0014](adr/0014-dados-sensiveis-segregados-e-criptografados.md)). Esses campos:

- aparecem trancados, com o valor mascarado (só os 6 últimos caracteres visíveis);
- só podem ser aprovados depois de "Destrancar para conferir";
- nunca entram em "Aprovar os N" e aparecem primeiro na lista "Um a um".

Com o consentimento de dados sensíveis e valor cadastrado, a API devolve o campo como `Blocked` desmarcado, mas com valor; o painel o converte em Pronto trancado. Sem o consentimento, o campo é "Ação sua" e a API não devolve valor.

### 9.5 Texto da IA

Perguntas abertas usam o cartão de rascunho: "Um ponto de partida. Termine com as suas palavras." O rascunho mostra "De onde veio: a IA usou só isto do seu perfil" (`usedFacts`) e o contador de caracteres quando o site define limite. "Aprovar texto" só fica ativo depois que a pessoa marca **"Li e tudo o que está escrito é verdade sobre mim."** Há "Outro começo" e "Escrever do zero"; depois de aprovar, "Guardar no Banco de respostas" (`POST /answers`). Cada pedido de rascunho conta no limite `aiSuggestions`.

**Toda resposta 200 vira algo visível no cartão** (`suggestText` e `draftText` em `sidepanel.js`):

| Resposta da API | O que o painel mostra |
|---|---|
| Texto em `value` (ou `text`, `answer`, `draft`, de versões antigas), depois de `trim` | Cartão de rascunho. Com `kind: "approved"` ou `approvedAnswerId`, o cartão diz que é uma resposta aprovada antes para pergunta parecida, com a nota da API, e o selo "Resposta já aprovada – confira" |
| Sem texto (vazio, só espaços, `kind: "missing"`) | Aviso no próprio cartão (`it.aiNote`, `role="status"`): "A IA não conseguiu escrever um rascunho com os fatos do seu perfil; escreva do seu jeito." (ou a `note` da API, que começa com essa frase), com a caixa de texto **já aberta** para a pessoa escrever |
| `kind: "unknown"` (IA fora) | A `note` da API ("IA indisponível no momento…") no cartão, com a caixa aberta |

Antes, um 200 sem texto só mudava a linha de status do rodapé e parecia que nada tinha acontecido. A API garante o mesmo do lado dela (nunca `value` vazio; ver [API](api.md)).

## 10. Anúncios na extensão (RF-29, ADR-0020)

| Placement | Onde | Formato no painel |
|---|---|---|
| `ext.job.contextual` | Aba "A vaga e você", depois da análise, quando há requisitos que não aparecem no perfil | Cartão "Patrocinado" com "Sobre: <requisito>" |
| `ext.panel.banner` | Aba "A vaga e você", quando não há contextual; também dentro da oferta recompensada (§11.1) | Banner de 368×64 com selo "Patrocinado", "Ver" e "Ocultar" ("x"); quando o anúncio tem `topic`, **Não tenho interesse no tema** dentro de "Por que estou vendo isso?" |
| `ext.after-complete` | Depois de "Candidatura registrada" | Cartão "Próximo passo" |

Como o painel escolhe:

- **Contextual primeiro, banner depois.** O painel pega até **3** requisitos marcados como "Não encontramos no seu perfil" e pede `ext.job.contextual` com cada um em `topic`, em ordem. A API só devolve contextual se o assunto casar com algum anúncio do catálogo; senão responde 204. Se nenhum dos três render anúncio (ou se a vaga ainda não foi analisada), o painel pede `ext.panel.banner`.
- **Onde nunca há anúncio**: tela "Não conectada" (não há API autenticada), aba **Formulário** (revisão de campos e resumo do preenchimento), tela de conclusão antes do registro, Ajustes e para quem é Plus (RN-08). TN-15 cobre a revisão.
- **Um por tela** (RN-11): enquanto a oferta recompensada está aberta, o espaço do anúncio da vaga não é montado.
- **Plus**: se `GET /billing` diz `plan = plus` ou `adsEnabled = false`, o painel nem pede anúncio; a API também responde 204 para Plus.
- **Falha não quebra a tela** (RNF-15): o anúncio é pedido depois que o conteúdo já apareceu; erro, 404 (API sem v2), 204 ou resposta que demora mais de **1,5 s** deixam o espaço vazio. O timeout fica em `fetchAd` (`sidepanel.js`), com `Promise.race` entre o pedido e um temporizador de 1.500 ms; resposta atrasada conta como "sem anúncio". O resultado fica em cache por espaço, assunto e página enquanto o painel está aberto.
- **Rótulo e controle** (RN-10): todo formato tem o selo "Patrocinado" e "Ocultar". "Por que estou vendo isso?" aparece quando a API manda `reason` (todo anúncio do catálogo traz um). "Ocultar" chama `POST /ads/{id}/hide` com `scope: "ad"` (30 dias). **Não tenho interesse no tema** chama o mesmo endpoint com `scope: "topic"` (para sempre), como "Não mostrar anúncios sobre {tema}" na web: aparece ao lado de "Ocultar" nos cartões contextual e de próximo passo e, no banner, dentro de "Por que estou vendo isso?". Só aparece quando há tema (`ad.topic` ou o requisito usado no pedido). Depois, o status diz "Pronto. Você não verá mais anúncios sobre {tema}."
- **Privacidade** (RN-09, RNF-14): o `topic` é o texto do requisito da vaga (até 200 caracteres), não um dado da pessoa. O painel nunca envia nome, contato, URL da página nem valores do formulário para `/ads`.
- **Imagem**: o catálogo de demonstração não tem imagem (`imageUrl = null`); o painel mostra o logo do Zyft. Se um dia houver `imageUrl`, só `http(s)`, com `referrerpolicy="no-referrer"`.
- **Clique**: `POST /ads/{id}/click` (contador agregado) e abertura do link em nova aba com `chrome.tabs.create`.

### 10.1 Rede de anúncios (Google AdSense simulado, ADR-0021)

Quando `GET /ads` devolve `format: "network"` (em `ext.panel.banner`, onde a rede vem antes do direto, ou em `ext.after-complete`, quando não há direto), `renderAd` chama `networkAd` (`sidepanel.js`):

- **Banner do rodapé:** linha com o selo "Patrocinado" e o "x" de **Ocultar**, o bloco `<ins class="adsbygoogle" data-ad-client data-ad-slot data-ad-format="horizontal" data-adtest="on">` com 368×64 e "Anúncio do Google (simulado)", e **Por que estou vendo isso?** com o `reason`.
- **Cartão pós-conclusão:** selo "Patrocinado" + "Rede de anúncios", bloco de 120 px de altura, "Por que estou vendo isso?" e **Ocultar**.
- **Ocultar** chama `POST /ads/adsense-<slot>/hide` (30 dias). Não há "Não tenho interesse no tema" (a rede não tem tema) nem clique contado.
- **Sem código remoto:** a extensão **nunca** carrega `adsbygoogle.js`. O Manifest V3 proíbe código remoto e a CSP do manifesto é `script-src 'self'`. O bloco é só HTML local, sem logo nem marca do Google.
- **Produção:** as políticas do AdSense não permitem anúncios dentro de software ou extensões de navegador (confirmar na política vigente). Por isso, em produção a extensão fica **só com anúncios diretos**: basta tirar `ext.panel.banner` e `ext.after-complete` de `Ads:Network:Slots`.

## 11. Uso, limites e o HTTP 402

### 11.1 Medidor e "Ganhar"

No rodapé da aba "A vaga e você", o painel mostra o `UsageMeter` com as candidaturas assistidas do mês (de `GET /billing`): "Candidaturas assistidas em outubro · 7 de 10", com a nota "O contador recomeça em 1º de novembro." O limite exibido já soma o bônus do mês ("Inclui +3 de bônus") e a nota cita o saldo de pacotes avulsos. No Plus: "Zyft Plus: sem limite e sem anúncios." Embaixo, o link "Sem anúncios e sem limite com o Zyft Plus" abre `#/planos` na plataforma.

Enquanto houver ofertas no mês (`rewarded.claimedThisMonth < maxPerMonth`), aparece "+3 candidaturas este mês ao ver uma oferta patrocinada" e o botão "Ganhar". Ao clicar, o painel mostra a "Oferta patrocinada" (um anúncio do espaço `ext.panel.banner`), avisa que não é preciso clicar no anúncio e oferece "Vi a oferta: ganhar +3" ou "Agora não". "Vi a oferta" chama `POST /ads/rewarded/claim`. Até 3 vezes por mês (+9). Se a API responder 409 `ads.rewarded_exhausted`, o painel diz "Você já usou as ofertas deste mês. Elas voltam no mês que vem."; o botão some quando o `GET /billing` seguinte mostra o máximo atingido. É sempre opcional.

### 11.2 Quando o limite acaba

Uma candidatura assistida conta **uma vez** por página (origem + caminho) no mês, na primeira leitura do formulário. Ler de novo a mesma página não conta de novo (RNF-16).

Se `POST /jobs/analyze`, `POST /forms/mappings` ou `POST /forms/open-answers` responderem **402** `plan.limit_reached` (com `resource`, `used` e `limit` no topo do ProblemDetails e em `extensions`), o painel mostra um cartão calmo, sem vermelho, no lugar onde a pessoa estava (aba da vaga, aba Formulário ou o próprio cartão da pergunta):

1. Título por recurso: "Você usou as 10 candidaturas assistidas deste mês.", "Você usou as 5 sugestões de texto da IA deste mês." ou "Você usou as 10 análises de vaga deste mês."
2. "Tudo o que está no seu perfil continua salvo. O contador recomeça em 1º de <mês>." Para a IA: "Você ainda pode escrever a resposta com as suas palavras."
3. Opções, nesta ordem: "Ganhar +3 vendo uma oferta" (só para candidaturas assistidas e se ainda houver oferta), "Pacote avulso R$ 4,90: +15 candidaturas e +10 sugestões" e "Zyft Plus R$ 9,90/mês: sem limite". Pacote e Plus abrem a plataforma em `#/planos`; a compra não acontece no painel.
4. "Ambiente de demonstração: nenhuma cobrança real."

Outros limites tratados no painel: guardar no Banco de respostas com o banco cheio (402 `answers`) mostra um aviso e a resposta continua valendo para aquele formulário; parear além do limite (402 `devices`) está no §8. O portal continua da pessoa (RN-12).

## 12. Distribuição

Distribuição não listada na Chrome Web Store e no Microsoft Edge Add-ons durante os testes. Na Entrega 3 a extensão é carregada sem compactação (`chrome://extensions`, modo de desenvolvedor). Antes de publicar com anúncios, é obrigatória a revisão de conformidade com a política de dados de usuário da Chrome Web Store, que restringe o uso de dados para publicidade (risco registrado no [ADR-0020](adr/0020-monetizacao-com-anuncios-e-planos.md)). A ficha da loja precisa declarar que a extensão exibe anúncios no plano gratuito e a licença das fontes empacotadas.

## 13. Testes

| Nível | Situação |
|---|---|
| Integração com a API (token `scope=extension`) | Coberto pelos testes .NET: `POST /forms/mappings`, `/forms/open-answers`, `/forms/completions`, `/jobs/analyze`, `GET /billing`, `/ads` e 402 com `resource`, `used` e `limit`; a extensão lê plano e anúncios mas não assina (teste `Extensao_pode_ler_billing_e_anuncios_mas_nao_assinar`) nem compra pacote (403), e ao se desconectar libera a vaga de navegador (teste `Extensao_que_se_desconecta_libera_a_vaga_de_navegador_e_nao_compra_pacote`) |
| E2E no portal demo | Roteiros com Playwright e a extensão carregada no Chromium (scripts de apoio fora do repositório, usados para as capturas da Entrega 3): parear (inclusive código errado), analisar a vaga, oferta recompensada, ler o formulário, rascunho da IA, aprovar, preencher, concluir, limite de IA e tema escuro. Conferem que o formulário **não** é enviado e que não há anúncio na aba Formulário |
| Unitário do painel e do content script | Não há. Pendência: rótulo por `aria-labelledby`, radios agrupados, modal com precedência, `password` descartado, mapeamento de estados e `SENSITIVE_KEYS` |
| Segurança | Pendência: página hostil tenta `postMessage` para o content script e `chrome.runtime.sendMessage` para o service worker; tenta ler `chrome.storage.session`; injeta rótulos com instruções para a IA |

Os roteiros E2E ainda não rodam no CI.

## 14. Divergências conhecidas e pendências

| Tema | Situação na 0.4.0 | O que fazer |
|---|---|---|
| "Por que estou vendo isso?" | Exibido só se a API mandar `reason`; o painel não deixa de mostrar o anúncio sem ele | Hoje todo anúncio do catálogo tem `reason`; tornar a regra explícita no painel |
| Oferta recompensada | O bônus é concedido quando a pessoa clica em "Vi a oferta"; o servidor não confirma que o anúncio foi exibido e `POST /ads/rewarded/claim` não tem rate limit dedicado (só o limite de 3 por mês) | Aceitável na demonstração (sem receita real); rever antes de anúncios reais |
