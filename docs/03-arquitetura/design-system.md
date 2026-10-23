# Design system do Zyft

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Design system (marca, tokens, componentes, estados e voz) |
| Versão | 1.0 |
| Status | Vigente para a v2 do MVP 1 |
| Dono | Designer UX (papel acumulado pelo dev frontend), com o arquiteto de software |
| Documentos relacionados | [Frontend web](frontend-web.md), [Extensão](extensao.md), [Personas](../01-produto/personas.md), [Resultados da pesquisa v2](../05-pesquisa/resultados-v2.md), [Monetização](../01-produto/monetizacao.md), [ADR-0020](adr/0020-monetizacao-com-anuncios-e-planos.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 21/10/2026 | Primeira versão. Substitui a paleta de partida do [frontend web §5](frontend-web.md) (v1.0) e consolida o design system desenhado para a v2: arquétipo, tokens, 18 componentes, estados de campo, formatos de anúncio e voz. |

## 1. Para que serve

O design system é o conjunto de decisões visuais e de linguagem que a plataforma web e o painel da extensão compartilham. Ele existe por dois motivos práticos:

1. **Confiança se constrói com consistência.** A pessoa precisa reconhecer, em qualquer tela, o que está pronto, o que precisa dela, o que o Zyft não pode fazer e o que foi escrito pela IA. Se cada tela inventar sua cor, isso se perde.
2. **O time é pequeno.** Tokens e componentes definidos uma vez evitam discutir a mesma cor ou o mesmo texto de botão em cada história.

## 2. Personalidade: Cuidador, com traço de Sábio

A marca foi escolhida a partir da pesquisa v2 ([resultados](../05-pesquisa/resultados-v2.md)), não de gosto pessoal.

| Arquétipo | Por que | Como aparece |
|---|---|---|
| **Cuidador** (principal) | A dor mais forte é redigitar dados (4,40 de 5; 80% deram 4 ou 5). As pessoas também têm medo: 67% têm receio de deixar um assistente preencher CPF e RG, e 40% citam privacidade como barreira. O produto precisa tirar o trabalho chato **e** proteger. | Dados sensíveis trancados por padrão (`SensitiveValue`), carimbo em cada item aprovado (`Stamp`), linguagem acolhedora. |
| **Sábio** (apoio) | A confiança num assistente que preenche ficou em 3,4 de 5, abaixo da meta de 3,5. Confiança vem de explicar: o que o site pediu, de onde veio o valor, o que o Zyft não fez. | Origem visível em cada valor, "Por que o Zyft sugeriu isso?" (`WhyThis`), evidência da vaga (`RequirementMatch`) sem nota nem porcentagem. |
| **Mago** (evitar) | "A IA faz tudo por você" assusta: 20% não confiam em IA escrevendo por eles e 33% têm receio de respostas abertas sugeridas pela IA. | Nada de brilhos, estrelas, varinhas, "mágica" ou envio automático. A IA aparece como rascunho, com o ícone `draft` (linhas e lápis). |

Três verbos guiam toda tela:

- **Acolher**: começar pelo que a pessoa pode fazer, não pelo erro. "Isso ainda não está no seu perfil. Se quiser, responda aqui", e não "Campo obrigatório não preenchido".
- **Explicar**: toda sugestão responde o que o site pediu, de onde veio o valor e o que o Zyft não fez.
- **Devolver a decisão**: o último gesto é sempre da pessoa (aprovar, carimbar, enviar no site).

### 2.1 Para quem desenhamos

Toda tela atende primeiro o **Rafael** e nunca deixa a **Bia** para trás ([personas](../01-produto/personas.md)). Diante de uma decisão difícil: "isso ajuda o Rafael a conferir e seguir? Isso ajuda a Bia a começar sem exagerar?"

| Persona | O que a interface faz por ela |
|---|---|
| Rafael (primária) | Campos Prontos em linhas compactas com "Aprovar os N"; contagem antes do detalhe; tudo nasce do currículo importado; `SiteSupport` diz a verdade sobre cada site (40% citam "funcionar em poucos sites" como barreira). |
| Bia (secundária) | `RequirementMatch` explica a vaga sem nota; `AIDraft` oferece "um ponto de partida" e só libera a aprovação depois de "Li e tudo o que está escrito é verdade sobre mim"; `UsageMeter` mostra o limite do gratuito com calma. |
| Beatriz (hipótese, outra área) | Sugestões da área de Saúde no onboarding e campos personalizados ("Registro no conselho", "Disponível para plantão"). Valida a ideia de que o visual e a voz não são "de tecnologia". |
| Antipersona (candidatura em massa) | Não existe seleção de várias vagas nem botão de envio. "Aprovar os N" vale só para campos Prontos e não sensíveis de **um** formulário. |

## 3. Tokens

A fonte da verdade são os arquivos do design system entregues em `tokens.json` e `zyft-tokens.css`. No código, os tokens viram CSS custom properties em `web/css/tokens.css` (plataforma; componentes em `web/css/components.css` e telas em `web/css/app.css`) e `extension/sidepanel.css` (painel). Nomes são os mesmos nos dois arquivos.

### 3.1 Cores

| Token | Claro | Escuro | Uso |
|---|---|---|---|
| `surface` | #F6F1E7 | #0F2228 | Fundo da página (areia; no escuro, petróleo noturno). Cerca de 70% da tela. |
| `surface-raised` | #FFFDF8 | #163038 | Cartões, painéis, campos, menus. |
| `surface-sunken` | #ECE5D7 | #0A181C | Barra lateral, trilhos, explicações. |
| `line` | #DDD4C4 | #2A474F | Divisores e bordas decorativas. |
| `line-strong` | #857A6D | #7A9297 | Bordas de controles (3:1 sobre `surface`). |
| `taupe` | #8A7F72 | #8A7F72 | Grafismos. Não é cor de texto. |
| `ink` | #12313A | #F3EEE4 | Texto principal. |
| `ink-muted` | #4D5D61 | #A7B6B6 | Texto secundário e metadados. |
| `petrol` | #12313A | #1E4651 | Bloco da marca: cabeçalho do painel, botão `secondary`, contadores. |
| `on-petrol` | #F6F1E7 | #F3EEE4 | Texto sobre `petrol`. |
| `zyft` | #FFD400 | #FFD400 | Amarelo da **decisão da pessoa**: ação principal, item ativo, switch ligado, logo. No máximo 5% da tela e uma ação principal por tela. |
| `on-zyft` | #12313A | #12313A | Texto sobre `zyft`. |
| `zyft-hover` | #F2C800 | #FFE14D | Hover e pressionado. |
| `zyft-soft` | #FFF1B3 | #3A3712 | Fundo tingido (seleção, caixas do código de pareamento). |
| `zyft-text` | #6B5800 | #FFD400 | Amarelo usado como texto ou ícone. |
| `ready` / `ready-soft` | #3F6B57 / #E0EAE2 | #8CC3A6 / #173A2F | Pronto e carimbo (sálvia). |
| `needs` / `needs-soft` | #A14A00 / #FBE6CD | #F5A14A / #3C2C14 | Precisa de você (âmbar alaranjado, de propósito diferente do amarelo da marca). |
| `blocked` / `blocked-soft` | #B42318 / #FBE0DB | #FF8B7C / #45211D | Ação sua. |
| `ai` / `ai-soft` | #6A3DC4 / #ECE4FA | #BBA2FF / #2B2449 | Exclusivo do texto escrito pela IA. |
| `focus` | #12313A | #FFD400 | Anel de foco. |

Regras que não mudam:

- O amarelo da marca **nunca** sinaliza estado.
- O roxo `ai` não é usado como decoração, para continuar significando "isto foi escrito pela IA".
- Indicadores que não são estado de campo (compatibilidade do site, requisitos da vaga, limite do plano, anúncio) usam visual neutro em `ink` e `ink-muted`.

### 3.2 Tipografia

Família `sans` = **Archivo**; `mono` = **JetBrains Mono** (código de pareamento e nomes técnicos de campo).

| Estilo | Tamanho / linha | Peso | Uso |
|---|---|---|---|
| `display-xl` | 56/56 | 800 | Uma vez por tela de boas-vindas (login, onboarding) |
| `display` | 40/44 | 800 | Título de página da plataforma |
| `title` | 28/32 | 700 | Seção ou modal |
| `heading` | 18/24 | 700 | Cartão, painel |
| `body-lg` | 17/26 | 400 | Pergunta do onboarding |
| `body` | 15/22 | 400 | Texto da plataforma |
| `body-sm` | 14/20 | 400 | Texto do painel da extensão (cerca de 360 px) |
| `label` | 13/18 | 600 | Rótulos e botões pequenos |
| `caption` | 12/16 | 500 | Metadados, auditoria, base legal |
| `overline` | 11/16 | 700 | Grupos (única caixa alta permitida) |
| `code-xl` | 32/40 mono | 600 | Código de pareamento |
| `code` | 13/18 mono | 500 | Nome técnico do campo (`input#email`) |

### 3.3 Espaço, forma e profundidade

| Grupo | Tokens |
|---|---|
| Espaço (escala de 4 px) | `space-1` 4 · `space-2` 8 · `space-3` 12 · `space-4` 16 · `space-5` 24 · `space-6` 32 · `space-7` 48 · `space-8` 64. Painel: gutter `space-4`, entre campos `space-3`. Plataforma: cartões `space-5`, seções `space-6`. |
| Raio | `radius-sm` 4 (selos, código) · `radius-md` 8 (botões, inputs, FieldCard) · `radius-lg` 14 (cartões, modais, logo) · `radius-pill` 999 (switch, progresso) |
| Sombra | `shadow-card` discreta nos cartões; `shadow-hard` (3 px deslocada, petróleo no claro e amarela no escuro) em **um** elemento-herói por tela (cartão do código de pareamento, CTA do onboarding) |
| Foco | `focus-ring`: 2 px de respiro mais 2 px sólidos na cor `focus`, em todo controle |
| Movimento | 120 a 150 ms, só funcional; respeita `prefers-reduced-motion` |

## 4. Componentes

O pacote de referência expõe 18 componentes em `window.Zyft` (React 18). A plataforma e a extensão atuais são em JavaScript sem build ([frontend web](frontend-web.md), [extensão](extensao.md)), então os componentes são **reproduzidos** em HTML e CSS com as mesmas classes, tokens e textos. O pacote React é a referência visual e de comportamento, não uma dependência do código.

| Componente | Uso | Regras principais |
|---|---|---|
| `Button` | Ações | Variantes `primary` (amarelo, uma por tela), `secondary` (petróleo), `quiet` (contorno), `ghost` (texto). Rótulo = verbo + resultado, com número quando houver ("Preencher 6 campos aprovados"). Nunca "Enviar" numa ação do Zyft. |
| `StatusBadge` | Estado do campo | Ícone + palavra, nunca só cor. Só para estados de preenchimento. |
| `ProgressSummary` | Topo do painel | Barra segmentada com contagem por estado, na ordem Pronto, Precisa de você, Sugestões da IA, Ação sua. Rótulo acessível "N de T campos prontos". |
| `FieldCard` | Campo proposto no painel | Rótulo, nome técnico, valor, origem, "Por que", estado e ações. Modo `compact` só para Prontos não sensíveis. Aprovado ganha carimbo e "Desfazer". |
| `ReviewItem` | Revisão do currículo importado e do perfil | Pendente com faixa amarela e Editar/Aprovar; "Salvar no Perfil Mestre" bloqueado até tudo ser revisado. |
| `TextField` | Campos de formulário | Rótulo sempre visível; erro diz o que fazer ("Faltam dígitos. O CPF tem 11 números."). |
| `Switch` | Consentimentos e preferências | Liga e desliga na hora; não serve para ações que pedem confirmação. |
| `ConsentRow` | Linha de consentimento LGPD | Finalidade em linguagem simples primeiro; base legal em `caption`. |
| `PairingCode` | Conectar extensão | Código em `code-xl`, validade, copiar e renovar; único `shadow-hard` da tela; lido dígito a dígito por leitor de tela. |
| `AppNav` | Menu lateral da plataforma | Grupos Seu perfil, Candidaturas e Conta. Item ativo em `zyft`. |
| `SensitiveValue` | Dado sensível ou de alto impacto (CPF, RG, endereço, nascimento) | Trancado por padrão, valor mascarado, "Destrancar para conferir". Nunca entra em "Aprovar os N". |
| `SiteSupport` | Topo do painel | Diz se o site é `full`, `partial` ou `none`, sem esconder a compatibilidade parcial. Visual neutro. |
| `RequirementMatch` | "A vaga e você" | Aparece / aparece em parte / não encontramos, com a evidência do perfil. Sem nota, sem porcentagem, sem verde e vermelho. |
| `AIDraft` | Pergunta aberta | "Um ponto de partida. Termine com as suas palavras." Mostra os fatos usados. "Aprovar texto" só depois de "Li e tudo o que está escrito é verdade sobre mim." Sempre oferece "Escrever do zero". |
| `UsageMeter` | Limite do plano gratuito | Informativo e calmo: sem vermelho, sem urgência, mostra quando o contador recomeça. |
| `Stamp` | Carimbo de aprovação | "Carimbado por você · hora", em `ready`, sem animação de comemoração. Cada carimbo corresponde a uma ação no registro. |
| `WhyThis` | "Por que o Zyft sugeriu isso?" | Diz o que o site pediu, de onde veio o valor e o que o Zyft não fez. Nunca promete resultado. |
| `Icon` | Ícones | Lineares, 16 px, traço 1,75, `currentColor`, estilo Lucide. Estados: `check`, `alert`, `hand`, `draft`. |

Há também três telas de referência que juntam os componentes: **ExtensionPanel** (painel do Rafael), **AssistPanel** (painel da Bia: vaga e texto) e **PlatformScreen** (revisão do currículo importado).

## 5. Estados de campo

Os quatro estados são o centro do produto. Cada um tem cor, ícone e palavra, para quem não distingue cores (RNF-06).

| Estado | Token | Ícone | Quando | Ações |
|---|---|---|---|---|
| Pronto | `ready` | `check` | O valor veio do Perfil Mestre aprovado. | Aprovar, Editar, Pular |
| Precisa de você | `needs` | `alert` | Falta a informação no perfil, há ambiguidade ou o site já tem um valor diferente. | Responder ou escolher, Pular |
| Ação sua | `blocked` | `hand` | O Zyft não age: senha, CAPTCHA, anexo, termos, declaração, sensível sem consentimento. Borda tracejada. | Só "Mostrar no site" |
| Sugestão da IA – revise | `ai` | `draft` | Texto gerado pela IA, em caixa `ai-soft` com borda `ai`. | Editar, aprovar com a declaração de veracidade |

Correspondência com a API: `Ready` → Pronto; `Ambiguous`, `Missing` e `Divergent` → Precisa de você (o texto explica qual dos três); `Blocked` → Ação sua; texto de `/forms/open-answers` → Sugestão da IA.

Ordem no painel: Prontos compactos no topo com "Aprovar os N"; depois, um a um, os sensíveis e o que precisa de atenção. Um campo aprovado mostra "Carimbado por você · 09:41", "será preenchido" e "Desfazer".

## 6. Anúncios

Os anúncios existem só no plano gratuito ([monetização](../01-produto/monetizacao.md), [ADR-0020](adr/0020-monetizacao-com-anuncios-e-planos.md)). O design trata o anúncio como algo **separado** da interface do Zyft, para que nunca pareça uma sugestão do produto.

### 6.1 Regras visuais

- Selo **"Patrocinado"** sempre visível e **fora do criativo** (a moldura é do Zyft), para não se confundir com a interface (RN-10).
- Sempre com "Ocultar" (ou "Não tenho interesse") e "Por que estou vendo isso?".
- Visual neutro (`surface-raised`, `line`, `ink-muted`). Nunca usa amarelo `zyft`, cores de estado nem o roxo da IA.
- No máximo um por tela (RN-11) e só depois que a pessoa terminou o que veio fazer na tela.
- Texto do anúncio segue a mesma régua: sem promessa de contratação (RN-06) e sem as palavras proibidas da seção 7.

### 6.2 Formatos

| # | Formato | Onde | Placement da API | Tamanho |
|---|---|---|---|---|
| 1 | Card nativo | Web, coluna lateral do Perfil Mestre | `web.profile.rail` | 320 px; imagem 640×240 |
| 2 | Faixa no feed | Web, entre o resumo e a lista de Candidaturas | `web.applications.feed` | largura do conteúdo |
| 3 | Retângulo de rede | Web, Banco de respostas e Registro, quando não há patrocinador direto | `web.banner` | 300×250 |
| 4 | Card contextual | Extensão, "A vaga e você", ligado a um requisito que não aparece no perfil | `ext.job.contextual` | 368 px |
| 5 | Próximo passo | Extensão, depois de "Candidatura registrada" | `ext.after-complete` | 368 px; imagem 736×200 |
| 6 | Recompensado (opcional) | Extensão, junto do medidor do plano gratuito | `POST /ads/rewarded/claim` | linha com "Ganhar" |
| 7 | Banner horizontal | Web, abaixo do título em Banco de respostas, Extensão, Detalhe da candidatura e Registro | `web.banner` | até 970×90; quebra linha no celular |
| 8 | Banner do painel | Extensão, rodapé ao conectar e na aba da vaga | `ext.panel.banner` | 368×64 |

**Nunca há anúncio** em: onboarding, entrar e criar conta, importar currículo, dados sensíveis, conta e privacidade, painel "Ler formulário" (revisão dos campos) e para quem é Plus (RN-08).

## 7. Voz e conteúdo

- Fale com "você", em português do Brasil, frases curtas, voz ativa. O Zyft fala de si na terceira pessoa ("O Zyft só preenche o que você aprovou").
- Diga o que vai acontecer antes de acontecer, com número: "Preencher 6 campos aprovados", não "Continuar".
- Caixa de frase em títulos e botões. Sem emoji, sem exclamação, sem superlativos.
- Mostre a origem de cada dado: "Perfil Mestre · Contato", "Importado de curriculo.pdf", "Gerado a partir de: Banco de respostas".
- A IA se declara: selo "Sugestão da IA – revise" e lista dos dados usados.
- Com a Bia, encorajar sem inflar: "Aparece em parte" em vez de "Fraco"; "talvez só falte no seu perfil" em vez de "requisito não atendido".
- LGPD em linguagem simples: finalidade primeiro, base legal depois, em `caption`.
- Planos: o limite é informado com calma ("Você usou 10 de 10 candidaturas assistidas deste mês. O contador recomeça em 1º de novembro."), sem "Últimas!" nem contagem regressiva. O aviso "Ambiente de demonstração: nenhuma cobrança real" aparece em `caption` nas telas de plano (RN-13).

**Palavras proibidas** em qualquer tela, botão ou anúncio:

| Proibido | Motivo |
|---|---|
| "em massa", "com um clique", "automático/automaticamente" (para envio), "candidate-se a N vagas", "enviar para todas", "piloto automático" | Antipersona e RN-01 |
| "Enviar", "Aplicar", "Candidatar-se" como ação do Zyft | O envio é sempre da pessoa, no site |
| "mágica", "num passe de mágica", "deixa com a gente" | Arquétipo Mago |
| "monitorar", "acompanhar sua navegação", "acesso total" | Medo de extensão que "lê tudo" |
| nota, porcentagem, "compatível", "aumenta suas chances" | RN-06 |

## 8. Acessibilidade

- WCAG 2.1 AA (RNF-06): contraste de texto de 4,5:1, bordas de controle de 3:1 (`line-strong`).
- Estados sempre com cor, ícone e palavra.
- Foco visível em todo controle (`focus-ring`).
- Código de pareamento e contagens anunciados em `aria-live`.
- Uma pergunta por tela no onboarding, com "Preencher mais tarde" sempre visível e alcançável por teclado (RNF-17).
- Anúncio com `aria-label` que começa por "Patrocinado", para leitores de tela também separarem o anúncio da interface.

## 9. Logo

- Símbolo: tile amarelo `radius-lg` com um **z** em petróleo cuja barra de baixo sobe num tique de aprovação ("você aprova"). Wordmark em minúsculas: **zyft** (no texto corrido, "Zyft").
- Arquivos: `zyft-lockup.svg` (fundos claros), `zyft-lockup-light.svg` (fundos escuros), `zyft-mark.svg` (ícone da extensão e favicon), `zyft-mark-petrol.svg` (sobre amarelo).
- Respiro mínimo igual à altura do z. Não recolorir, não girar, não aplicar o amarelo sobre branco puro sem o tile.

## 10. Onde ficam os arquivos e como o código consome

| O quê | Onde |
|---|---|
| Referência do design system (tokens, README, componentes React, logos, telas desenhadas) | Pacote entregue pelo design (`tokens.json`, `zyft-tokens.css`, `bundle.css`, `bundle.js`, `index.d.ts`, `ds-components/*/README.md`, `*.dc.html`). Os mockups das telas v2 são a fonte da verdade do visual. |
| Tokens na plataforma web | `web/css/tokens.css` (custom properties em `:root` e tema escuro por `prefers-color-scheme`) |
| Tokens no painel da extensão | `extension/sidepanel.css` |
| Logos | ícones da extensão em `extension/icons/`; logo da web em `web/` |

Como o código consome: as telas usam as variáveis CSS (`var(--surface)`, `var(--ready)`), nunca valores hexadecimais soltos. Um token novo entra primeiro no design system e só depois nos dois arquivos CSS, no mesmo PR. Mudança de texto de botão ou de estado segue a seção 7 e passa pela revisão de UX writing (RNF-11).
