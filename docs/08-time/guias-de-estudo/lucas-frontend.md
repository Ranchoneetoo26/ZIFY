# Guia de estudo: frontend web, painel da extensão e UX

| Campo | Valor |
|---|---|
| Integrante | Lucas Ulisses Clementino De Arruda E Silva |
| Papel | Dev frontend (plataforma web e painel lateral da extensão); acumula Designer UX |
| Versão | 1.1 (produto v2, sincronizado com a implementação) |
| Data | 21/10/2026 |
| Relacionados | [Papéis e RACI](../papeis-e-raci.md), [Frontend web](../../03-arquitetura/frontend-web.md), [Extensão](../../03-arquitetura/extensao.md), [Design system](../../03-arquitetura/design-system.md), [Fluxos](../../01-produto/fluxos.md), [Guia do usuário](../../06-operacao/guia-do-usuario.md) |

Este é um guia de estudo. Ele diz o que você precisa dominar para responder à banca; não registra autoria de código nem de documento.

## 1. O projeto inteiro em uma página

**Problema.** Redigitar os mesmos dados em cada site de vagas (4,40 de 5; 80% de notas 4–5). Preencher experiências e formação item a item (67%) e criar cadastro (60%) são as etapas que mais tomam tempo.

**Pesquisa.** 15 respostas válidas, 60% de tecnologia (viés registrado). Confiança num assistente: 3,4 de 5. Receios: CPF/RG 67%, endereço 40%, respostas abertas por IA 33%. 20% não confiam em IA escrevendo por elas. Familiaridade com formulários alta (4,07). Ver [resultados-v2.md](../../05-pesquisa/resultados-v2.md).

**MVP 1 (v2).** Perfil Mestre, importação de currículo, extensão que preenche só o aprovado, IA presa a fatos. Novidades: onboarding em passos com "Preencher mais tarde" (RF-23), área dinâmica e "Informações da sua área" (RF-24, RF-25), tela de Planos, medidor de uso e anúncios rotulados (RF-26 a RF-29).

**Regras inegociáveis.** Nunca enviar (RN-01), IA só com fatos (RN-02, RN-03), sem senha (RNF-01), sensíveis trancados (RN-04), sem promessa (RN-06). Na interface da v2: nenhum anúncio no onboarding, entrar, importar, sensíveis, conta e privacidade e no painel "Ler formulário" (RN-08); sempre "Patrocinado" com "Ocultar" e "Por que estou vendo isso?" (RN-10); um por tela (RN-11); aviso discreto "Ambiente de demonstração: nenhuma cobrança real" (RN-13).

**Personas.** Toda tela atende primeiro o Rafael ("Já escrevi isso dez vezes. Só quero conferir e seguir.") e nunca deixa a Bia para trás ("Não sei o que escrever e tenho medo de exagerar."). Beatriz (saúde) é hipótese e serve para testar a área dinâmica. Antipersona: candidatura em massa.

**Arquitetura em 5 linhas.** API .NET 8 em Clean Architecture serve a web em `/` e a API em `/api/v1`. Web em HTML, CSS e JavaScript sem build, rotas por hash. Extensão MV3: service worker (tokens e API), content script (lê e preenche a página) e painel lateral. IA atrás de uma porta, só aponta chaves. Erros sempre em ProblemDetails com `code`.

**Monetização.** Gratuito com limites e anúncios; Plus R$ 9,90/mês ou R$ 99/ano; pacote R$ 4,90; recompensado +3 (até +9/mês). Pagamento simulado.

## 2. A sua área a fundo

### 2.1 Plataforma web (`web/`)

| Arquivo | Papel |
|---|---|
| `web/index.html` | Shell: "pular para o conteúdo", navegação, `<main>`, região `aria-live` |
| `web/css/tokens.css`, `components.css`, `app.css` | Tokens claro e escuro do design system, componentes (botões, campos, estados com cor, texto e ícone, anúncio) e layout das telas. O antigo `web/styles.css` foi removido |
| `web/js/app.js` | Roteador por hash e guarda de autenticação |
| `web/js/api.js` | Cliente HTTP único: Bearer, refresh com rotação, ProblemDetails vira `ApiError` |
| `web/js/dom.js` | `h()` com `createElement`/`textContent`: nenhum HTML da API vira HTML (fecha XSS) |
| `web/js/validate.js` | Validações que espelham o backend |
| `web/js/ui.js` | Componentes do design system em JS puro, com as classes `zy-*` (Button, StatusBadge, TextField, Switch, ConsentRow, PairingCode, UsageMeter, ReviewItem, WhyThis...) |
| `web/js/ads.js` | Busca e monta anúncios (`web.profile.rail`, `web.applications.feed`, `web.banner`); erro, demora ou 204 escondem o espaço |
| `web/js/billing.js` | Plano e uso do mês, tratamento do 402 (recompensado, pacote, Plus) |
| `web/js/catalog.js` | Catálogo por área (`GET /catalog/areas`) com lista de reserva se o endpoint falhar |
| `web/js/store.js` | Estado compartilhado entre telas: dados básicos do perfil (o `PUT /profile` sempre leva o conjunto completo), progresso do onboarding e uso do plano |
| `web/js/views/*.js` | Uma view por tela: `auth`, `onboarding`, `profile` (com `profile-schema`), `import`, `answers`, `extension`, `applications`, `audit`, `account`, `consents`, `plans` |

Rotas da v2 (contrato): `#/entrar`, `#/cadastrar`, `#/boas-vindas` (onboarding com passaporte), `#/perfil`, `#/importar`, `#/respostas`, `#/extensao`, `#/candidaturas`, `#/candidaturas/:id`, `#/registro`, `#/conta`, `#/planos`. Menu: Seu perfil (Perfil Mestre, Importar currículo, Banco de respostas), Candidaturas (Extensão, Candidaturas), Conta (Planos, Registro de ações, Conta e privacidade).

**Por que sem build?** Para a entrega ser simples de rodar e de corrigir: basta a API servir a pasta. React/Vite, previsto na arquitetura antiga, ficou para depois do MVP 1.

**Quando um endpoint da v2 falha**, a tela segue funcionando: catálogo, plano e anúncios têm reserva (lista local, plano tratado como gratuito sem medidor, espaço do anúncio some).

**Limitações conhecidas da web (seja honesto se perguntarem):**
- No onboarding, respostas de três opções como "Às vezes", "Talvez" e "Tanto faz" viram "sim" ou ficam vazias, porque o perfil guarda só sim ou não nesses campos.
- As ferramentas escolhidas no onboarding vão para a experiência mais recente.
- O detalhe da candidatura mostra só a quantidade de campos preenchidos, não quais foram (os valores não são guardados).
- Dados sensíveis usam os tipos da API (gênero, etnia e outros, com consentimento). CPF e RG não são dado sensível no Zyft: no painel, eles ficam travados como "Ação sua".

### 2.2 Onboarding em passos (RF-23, ADR-0018)

- ~25 perguntas em 6 capítulos: Quem é você, Sua área, O que você busca, Sua bagagem, Sua apresentação, Combinados (consentimentos).
- Uma pergunta por tela. **Toda** pergunta tem "Preencher mais tarde" (RN-14). A pergunta de registro profissional só aparece quando a área tem registro (Saúde, Jurídico, Administrativo e financeiro, Logística).
- As respostas vão para os endpoints do perfil que já existiam (`PUT /profile`, `POST /profile/experiences` etc.). `/onboarding` guarda só o progresso: `answered`, `skipped`, `lastStep` (RNF-17: fechar o navegador não perde nada).
- As puladas aparecem como alerta no topo do Perfil Mestre, com "Responda agora". Enquanto faltarem, a extensão marca esses campos como "Precisa de você".
- **Por quê:** confiança 3,4 e receio de entregar documentos. Exigir 25 respostas de uma vez afasta. Uma por tela, com saída sempre visível, respeita o ritmo de cada pessoa.

### 2.3 Área dinâmica e "Informações da sua área" (RF-24, RF-25, ADR-0019)

- `GET /catalog/areas` traz, por área, cargos, setores, ferramentas, competências, o link pedido (GitHub, Currículo Lattes) e o registro profissional (ex.: "Registro no conselho", COREN).
- Toda lista aceita valor próprio ("Outro cargo", "Cadastrar do seu jeito").
- "Informações da sua área" usa `/profile/custom-fields`. O 422 `profile.custom_field_sensitive` precisa aparecer em linguagem simples, orientando a usar Dados sensíveis.

### 2.4 Planos, limites e anúncios na interface

- `#/planos` (`web/js/views/plans.js`): Gratuito, Plus (mensal e anual, "7 dias grátis", botão "Testar 7 dias grátis"), pacote avulso ("Comprar pacote"), comparação entre os planos e o bloco "Sempre de graça, em qualquer plano" (RN-12). O Plus lista só o que existe no código. O aviso de pagamento simulado é discreto, mas presente (RN-13).
- Itens do mockup que **não** devem ser mostrados como recurso do Plus enquanto não houver decisão: versões do perfil por tipo de vaga, lembretes de retorno, currículo em PDF, suporte com prazo (são MVP 2).
- 402 `plan.limit_reached`: a tela oferece, nesta ordem, recompensado (se houver), pacote e Plus. Sem vermelho, sem urgência: o medidor de uso informa com calma. O corpo traz `resource`, `used` e `limit` no topo e em `extensions`.
- Placements web: `web.profile.rail` (card nativo na coluna do perfil), `web.applications.feed` (faixa em Candidaturas), `web.banner` (banner em respostas, extensão, detalhe da candidatura e registro). Anúncio carrega depois do conteúdo; 204, erro ou demora acima de 1,5 s: o espaço some (RNF-15; `Promise.race` em `fetchAd`, em `web/js/ads.js` e no painel).

### 2.5 Painel da extensão (`extension/`)

| Arquivo | Papel |
|---|---|
| `extension/manifest.json` | MV3, permissões mínimas, outros sites por `optional_host_permissions` |
| `extension/background.js` | Único que fala com a API e guarda tokens (access em `chrome.storage.session`, refresh em `chrome.storage.local`) |
| `extension/content.js` | Lê e preenche a página; descarta senha antes de ler; nunca chama `submit()` nem clica em botões |
| `extension/sidepanel.html/.js/.css` | Reescritos na versão 0.4.0. Fluxo: conectar, "A vaga e você" (analisar), "Formulário" (ler, revisar, preencher), concluir. Travas de CPF, RG, endereço e data de nascimento por lista de chaves (`SENSITIVE_KEYS`). "Desconectar extensão" (Ajustes) chama `DELETE /extension/devices/current` antes do logout e libera a vaga de navegador |
| `extension/icons/`, `extension/fonts/` | Ícones do Zyft e fontes Archivo e JetBrains Mono empacotadas (licença OFL), sem depender de CDN |

Estados de campo do design system: **Pronto** (sálvia), **Precisa de você** (âmbar), **Ação sua** (borda tracejada), **Sugestão da IA – revise** (roxo). Sempre cor, ícone e palavra. O painel tem 6 estados internos (pronto, ambíguo, ausente, divergente, bloqueado e rascunho da IA) mapeados para esses 4 visuais. Prontos em linhas compactas com "Aprovar os N" (só não sensíveis, de um formulário). Sensíveis um a um. Texto da IA só é aprovado depois de marcar "Li e tudo o que está escrito é verdade sobre mim."

Anúncios na extensão: `ext.panel.banner` (rodapé em "A vaga e você"), `ext.job.contextual` (o painel tenta até 3 requisitos ausentes; se nenhum casar, cai no banner), `ext.after-complete` (depois de registrar a candidatura). **Nunca** na aba Formulário nem com o painel desconectado (RN-08). Medidor de uso e botão "Ganhar +3 vendo uma oferta" (recompensado, que usa o placement `ext.panel.banner`).

### 2.6 Voz e acessibilidade

- Fale com "você", frases curtas, sem emoji, sem exclamação. Botões dizem o resultado com número: "Preencher 6 campos aprovados".
- Nunca "Enviar", "Candidatar-se", "em massa", "com um clique", "automático" para envio. Nada de brilhos ou "mágica" (arquétipo Mago, a evitar). Arquétipo da marca: Cuidador com traço de Sábio.
- WCAG 2.1 AA: rótulos associados, erros por `aria-describedby`, foco no `<h1>` a cada troca de tela, `aria-live` para mensagens, `prefers-reduced-motion`.

## 3. Perguntas difíceis prováveis

1. **"Por que não React?"** Sem build, a entrega roda com a API e não tem cadeia de ferramentas para quebrar. O custo é mais código de DOM à mão; o `h()` padroniza e evita XSS. React fica para depois do MVP 1.
2. **"Como evitam XSS se mostram dados do perfil e da página do portal?"** Nenhum HTML vindo da API ou da página é inserido como HTML: tudo via `textContent`. CSP `script-src 'self'` na web e na extensão.
3. **"25 perguntas no onboarding não é muito?"** É uma por tela e toda pergunta pode ser adiada (RN-14). A pessoa pode pular tudo e usar o produto; o que faltar aparece como "Precisa de você".
4. **"Por que a extensão guarda token em dois lugares?"** Access curto em `storage.session` (some ao fechar o navegador, inacessível ao content script); refresh em `storage.local` para sobreviver ao reinício, com rotação e revogação.
5. **"A extensão pode enviar o formulário por engano?"** Não existe código que chame `submit()`, `requestSubmit()` ou clique em botão. O observador de envio só avisa o painel e não chama `preventDefault`.
6. **"Anúncio na extensão não atrapalha quem está preenchendo?"** Não há anúncio na aba Formulário nem na tela de conexão (RN-08). Ele aparece em "A vaga e você" e depois de concluir, no máximo um por tela (RN-11).
7. **"Por que o amarelo não é usado para alerta?"** Amarelo é a cor da decisão da pessoa (ação principal). Alerta usa âmbar alaranjado, para não confundir.
8. **"Como um daltônico distingue os estados?"** Cada estado tem ícone e palavra, além da cor.
9. **"Por que não mostrar porcentagem de compatibilidade? As pessoas gostam."** RN-06: nenhuma nota nem porcentagem. A tela diz "X de Y requisitos aparecem no seu perfil" e "Não encontramos no seu perfil", nunca "você não atende".
10. **"A tela de Planos promete lembretes e currículo em PDF?"** Não deve. Esses itens são MVP 2; mostrá-los seria promessa sem decisão (RN-06, RN-07).
11. **"Como a pessoa sabe que o pagamento é de mentira?"** Aviso discreto na tela de Planos e no checkout simulado: "Ambiente de demonstração: nenhuma cobrança real" (RN-13). Nenhum campo de cartão.
12. **"O que acontece na tela quando o limite acaba?"** O 402 vira uma mensagem calma com as opções: ver uma oferta (+3), pacote ou Plus. Editar perfil, exportar e excluir continuam livres (RN-12).
13. **"A confiança na pesquisa foi 3,4. O que o frontend fez com isso?"** Origem visível em cada valor, carimbo de aprovação, sensíveis trancados, "Por que o Zyft sugeriu isso?", e o último gesto sempre da pessoa.
14. **"Por que a Bia é secundária se o painel tem tanta coisa para ela?"** Perguntas abertas não passaram no critério (3,27), mas 47% citam essa etapa. O desenho atende primeiro o Rafael (conferir de relance) e oferece para a Bia o rascunho com fatos usados, sem escrever por ela.
15. **"A Beatriz é persona real?"** É hipótese: só 1 de 15 respostas é de saúde. Ela existe como usuária demo para provar que a área dinâmica funciona fora de tecnologia.
16. **"Como o painel sabe que o site é autorizado?"** Compara a origem com `GET /authorized-sites`; fora da lista, explica e oferece "Usar neste site mesmo assim", que pede a permissão do navegador.
17. **"A extensão tem teste automatizado no navegador?"** Ainda não com a extensão carregada (limitação registrada na Entrega 3). As chamadas foram validadas contra a API real e por testes de integração com token de extensão.
18. **"Esse 'Anúncio do Google' no banner é AdSense de verdade? Na extensão também?"** É **simulado** (ADR-0021). Quando a API manda `format: "network"`, `web/js/ads.js` (e `networkAd` no `sidepanel.js`) desenha o `<ins class="adsbygoogle" data-adtest="on">` do formato do AdSense com uma caixa cinza "Anúncio do Google (simulado)"; selo "Patrocinado", "Por que estou vendo isso?" e "Ocultar" ficam fora do bloco. `loadAdSense` em `web/js/adsense.js` não carrega nada em modo mock: a CSP (`script-src 'self'`) bloquearia o Google e, pela LGPD, precisaria de consentimento num CMP antes. Na extensão nunca haverá script remoto (MV3) e, para produção, ela fica só com anúncios diretos, porque as políticas do AdSense não permitem anúncios em extensões (confirmar na política vigente).

## 4. Onde está cada coisa

| Tema | Onde |
|---|---|
| Telas web | `web/js/views/` |
| Roteamento e sessão | `web/js/app.js`, `web/js/api.js` |
| Estilos e tokens | `web/css/tokens.css`, `components.css`, `app.css`; referência em [design system](../../03-arquitetura/design-system.md) |
| Extensão | `extension/` |
| Portal de demonstração | `web/demo/` (servido em `/demo/`) |
| Fluxos e estados | [fluxos.md](../../01-produto/fluxos.md) |
| Arquitetura web e extensão | [frontend-web.md](../../03-arquitetura/frontend-web.md), [extensao.md](../../03-arquitetura/extensao.md) |
| Contrato | [api.md](../../03-arquitetura/api.md) |
| Critérios de aceite | [CA-23 a CA-29](../../04-qualidade/criterios-de-aceite-mvp1.md) |
| Como usar o sistema | [guia do usuário](../../06-operacao/guia-do-usuario.md), [runbook local](../../06-operacao/runbook-local.md) |
| Relatório da Entrega 3 (front) | [entrega-3-frontend.md](../../entregas/entrega-3-frontend.md) |

## 5. Exercício de 30 minutos

1. (5 min) Rode a API e abra `http://localhost:5080/`. Entre como Beatriz (`beatriz@demo.zyft` / `Zyft@2026`): como o onboarding dela não terminou, o roteador abre `#/boas-vindas` na pergunta 22 de 25. Saia para o Perfil Mestre, veja o alerta de perguntas puladas e responda uma em "Responda agora".
2. (5 min) No onboarding, troque a área para Tecnologia e depois para Saúde e veja a pergunta de registro aparecer e sumir.
3. (5 min) Navegue pela plataforma só com o teclado, do login até Planos. Anote qualquer lugar onde o foco some.
4. (10 min) Carregue `extension/`, pareie com o código, abra `/demo/candidatura.html`, leia o formulário e confirme: nenhum anúncio na revisão; senha, anexo, termos e sensível como "Ação sua"; nada enviado.
5. (5 min) Abra as DevTools na tela de Perfil e confira a chamada `GET /ads`: o que vai na query e o que volta. Nada de nome, e-mail ou telefone.
