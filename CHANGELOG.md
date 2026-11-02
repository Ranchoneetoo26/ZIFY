# Changelog

Mudanças relevantes do projeto. O formato segue o [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/).

## [Não lançado]

### Adicionado (conta, concorrência do perfil e período do Plus, 02/11/2026)
- Verificação de e-mail (RF-01): `Users.EmailVerifiedAt`, tabela `UserTokens` (só o hash SHA-256, uso único, 24 h), `POST /auth/verify` e `POST /auth/verify/resend`, envio pela porta `IEmailSender` com `LoggingEmailSender` (não envia; registra o link com token só com `Email:LogLinks=true`, ligado apenas em `appsettings.Development.json`, e nunca o e-mail ou o nome) e `App:WebBaseUrl`. Reabrir o link que já confirmou a conta responde 204 (idempotente); link desconhecido, expirado sem uso ou invalidado continua 400. **Só informativa**: e-mail não confirmado não bloqueia nenhuma rota. Web: rota aberta `#/verificar`, aviso "Confirme seu e-mail" com **Reenviar link** e **Agora não** (`sessionStorage` `zyft.verifyBannerHidden`) e status em **Conta e privacidade**.
- Aceite dos termos (RF-01): `POST /auth/register` exige `acceptTerms: true` e `termsVersion` igual a `Terms.CurrentVersion` (`2026-10-22`, devolvida por `GET /auth/terms`); a conta guarda `TermsVersion` e `TermsAcceptedAt`, evento `termos.aceitos`. Caixa de aceite no cadastro com a versão lida da API.
- Senha (RF-30, novo): `POST /auth/password` troca a senha, revoga todos os refresh tokens e as extensões pareadas e devolve um par novo para a sessão atual (**Alterar senha** em Conta). `POST /auth/password/forgot` sempre 202 (sem enumeração) e `POST /auth/password/reset` com token de 30 min, uso único e só o hash no banco; redefinir encerra as sessões e confirma o e-mail. Web: `#/esqueci-senha` e `#/redefinir-senha`.
- Concorrência otimista do Perfil Mestre (RNF-18, novo): `Profiles.Version`, `ETag` nas respostas de `/profile*`, `If-Match` nas alterações e 409 `profile.conflict` com `currentVersion`; sem `If-Match`, nada muda para o cliente. O CORS expõe `ETag`. Web: `api.js` acompanha a versão e põe as gravações do perfil numa fila por aba, e o diálogo `conflict.js` oferece **Recarregar dados mais recentes**, **Sobrescrever mesmo assim** e **Cancelar**. O envio `keepalive` ao fechar a aba também manda `If-Match` (em conflito nada é gravado e o rascunho volta).
- Plus (RF-31, novo): `POST /billing/subscription/period` troca mensal ⇄ anual na hora, com rateio simulado (`proration`: crédito do que falta, preço novo, valor), tabela `SubscriptionPeriodChanges` (também na exportação) e evento `plan.period_changed`. Planos mostra a estimativa antes e o valor real depois. Pacote avulso também no Plus (RF-27): `BillingDto.packTotals` soma os pacotes ativos; no Plus os créditos ficam guardados e só são usados se a pessoa voltar ao Gratuito antes de vencerem (60 dias), e a tela diz isso antes da compra.
- Web: página 404 (`views/not-found.js`) no lugar do redirecionamento; `?query` no hash; Voltar/Avançar sobre formulário alterado pergunta antes de descartar (posição no histórico em `history.state.zyIdx`); modal não fecha enquanto salva; contador de candidaturas do menu relido a cada 30 s de navegação, ao voltar para a aba, depois de mudanças e no login. A API redireciona endereços sem hash (`/conta`) para `/#/conta` (`MapFallback`), mantendo a query (`/redefinir-senha?token=…` → `/#/redefinir-senha?token=…`) e sem sair do site.
- Extensão 0.4.1: na renovação do token, só 401/403 contam como desconexão (marca `disconnected` guardada e aviso "Extensão desconectada" com o motivo no painel); erro de rede, 5xx ou 429 não apaga mais os tokens. O medidor usa `packTotals.assistedRemaining`.
- Backend: `[AllowAnonymous]` por action no `AuthController` (na classe ele anulava o `[Authorize]` das rotas novas). SQLite de desenvolvimento ganha `SqliteSchemaUpgrader` (cria tabelas, colunas e índices que faltam, sem remover nada): não é mais preciso apagar `zyft-dev.db` quando o esquema muda. Migrations `20261102183823_VerificacaoDeEmail`, `20261102184148_AceiteDosTermos`, `20261102184704_VersaoDoPerfil` e `20261102185344_TrocaDePeriodoDoPlus`. 84 operações em `/api/v1` e 27 tabelas.
- Testes: 19 casos de unidade novos (`Auth/AccountSecurityTests`, `Billing/PeriodChangeTests`, `DomainTests`) e 22 de integração (`AccountSecurityTests`, `ProfileConcurrencyTests`, `BillingPeriodTests`, `SqliteUpgradeTests`). O total da suíte não foi recontado nesta entrada.
- Requisitos e critérios: DRP 2.3 (RF-01 ajustado, RF-30, RF-31, RNF-18, RF-27 no Plus); critérios de aceite 2.1 (CA-01 ajustado, CA-02.5, CA-27.6, CA-30, CA-31).
- Documentação: API 2.5, frontend web 2.8, extensão 2.5, backend 3.4, modelo de dados 3.2, segurança 2.4 (T28 a T31 e lacunas novas), DAT 2.4, runbook 3.4, guia do usuário 1.4, guias de estudo (Guilherme e Matheus) e nova lista de [pendências de revisão dos textos legais](docs/07-legal/pendencias-de-revisao.md).

### Corrigido (QA da conta e do conflito, 02/11/2026)
- **Sobrescrever mesmo assim** no 409 do `PUT /profile` não reverte mais o que a outra sessão mudou: `saveBasics` relê o perfil e aplica só os campos que a tela mudou (onboarding, "Responda agora" e o modal **Sobre você**).
- Foco volta ao botão **Salvar** (ou ao primeiro campo do modal aberto) depois de **Cancelar** no aviso de conflito; ações dos diálogos empilham e quebram linha em 320 px.
- Voltar com texto digitado pergunta também no Banco de respostas (nova e edição), em Informações da sua área, nova competência, "Responda agora" e endereço de site da Extensão; recarregar a aba avisa. Voltar com modal salvando não mostra mais um "Descartar?" falso: espera o salvamento e segue se deu certo.
- Abrir um link de redefinição com login nesta aba avisa a conta conectada e não encerra a sessão. Renovação recusada depois de estar logado diz "Sua sessão foi encerrada. Se você alterou a senha em outro lugar, entre com a senha nova."
- Reabrir o link de confirmação já usado mostra "E-mail confirmado".

### Alterado no contrato da API (02/11/2026)
- **Incompatível:** `POST /auth/register` sem `acceptTerms` e `termsVersion` passa a responder 400. Aditivos: `emailVerified` em `user` e em `GET /me` (mais `emailVerifiedAt`, `termsVersion`, `termsAcceptedAt`), `version` e `ETag` no perfil, `packTotals` no billing.

### Conhecido (02/11/2026)
- O envio de e-mail é só simulado; o link com o token só vai para o log em Development (`Email:LogLinks`), e fora do desenvolvimento ninguém recebe o link de verificação nem o de redefinição. Precisa de provedor real antes de uso com pessoas reais ([segurança §3.3](docs/03-arquitetura/seguranca.md)).
- O rascunho do onboarding passou do `sessionStorage` para o `localStorage` (`zyft.onboarding.draft.<id>`): guarda dado pessoal no navegador por até 7 dias e só some ao concluir, ao clicar em **Sair** ou ao vencer. Em computador compartilhado, outra pessoa pode lê-lo. Pede parecer de privacidade (segurança T31) e menção na política de privacidade.
- Na web, um `GET /profile` completo sobe a versão acompanhada pela aba; um modal aberto com dados mais antigos não é avisado ao salvar.
- Os termos e a política ainda falam de login Google (que não existe) e de conta "criada com e-mail verificado" (a confirmação é só um aviso); a versão `2026-10-22` não aparece nos textos. Lista em [pendências de revisão](docs/07-legal/pendencias-de-revisao.md). O `README.md` ainda cita a extensão 0.4.0, 77 operações e 25 tabelas.
- O limite de tentativas `auth` (10 por minuto por IP) é um só para cadastro, login, verificação, senha e pareamento.

### Corrigido (QA completo de telas e fluxos, 01/11/2026)
- Perda de dados: importar currículo apagava área de atuação, registro profissional e tipo de pretensão; concluir **Competências** no onboarding apagava as ferramentas salvas; desligar o consentimento de dados sensíveis apagava tudo sem confirmar; limpar anotações da candidatura dizia que salvou e não limpava.
- Valores errados: `parseMoney` e `parseHours` únicas em `web/js/validate.js`. `4.500` agora é 4500, negativo, notação científica e texto dão erro no campo, e campo numérico inválido não vira nulo. "Adicionar outra" no onboarding não copia dados da experiência anterior.
- Sessão e navegação: telas não se misturam mais com rede lenta nem depois de sair (`.route-stage` em `web/js/app.js`); erro passageiro no refresh mantém a sessão; sessão expirada avisa e volta à rota anterior; `fetch` com tempo limite; modal fecha ao trocar de rota e pergunta antes de descartar edição. Sair limpa os rascunhos da aba.
- Planos: falha em `GET /billing` mostra erro e nunca afirma plano ou teste usado sem dado. Importação retoma a revisão pendente (`GET /resume-imports/pending`) e não gasta cota com F5. **Registro de ações** tem "Carregar mais".
- Validação: sites autorizados usam a mesma regra de origem da API; mensagens de validação do backend sempre em português; e-mail malformado e texto só com caracteres invisíveis são recusados. No `PUT /profile`, o formato estrito de e-mail, telefone e URL só vale para o que mudou, para dado antigo não travar o salvamento.
- Máscaras: Backspace e Delete atravessam o separador, o cursor fica no lugar e colar não corrompe telefone nem UF. Dado sensível trancado não revela letras e editar exige destrancar. Pendências do perfil olham o dado real.
- Interface: toast visível sobre o modal e legível em 320 px, formulário de login na primeira dobra do celular, estados de carregando, foco preservado, setas em grupos de opção, e páginas `web/legal/` (rascunho) com link no cadastro.

### Alterado no contrato da API
- `POST /resume-imports/{id}/apply` devolve `added` e `skipped`. `PUT /answers/{id}` responde 409 `answer.duplicated` para pergunta de outra resposta. Pretensão negativa passa de 422 para 400.

### Corrigido (QA de caos, 01/11/2026)
- Perda de dados nas máscaras: `enhanceInput` (`web/js/dom.js`) reescrevia o valor ao carregar, e abrir **Sobre você** e salvar sem tocar gravava `+55 44 99999-1111` como `(55) 44999-9911`, `Paraná` como `PA`, `1234-567 PT` como `12345-67` e `ACC` como `AC`. Agora o valor recebido nunca muda: se ele não bate com o formato da máscara, o campo fica sem máscara e com o limite da API, no perfil, no onboarding e na revisão da importação.
- Telefone: valor começando com `+` é internacional, não é formatado e vai até 40 caracteres. Texto colado chega inteiro à máscara, sem o corte do `maxlength`. O teclado do celular passa a ser `tel`, que tem a tecla `+`.
- CNH: a máscara aceita `ACC`.
- Contador de caracteres: lê o limite real do campo depois de `enhanceInput`, sem mostrar `6 / 2`.
- Limites de `maxlength` do perfil e do onboarding alinhados aos validadores e às colunas da API, campo a campo, por exemplo `fullName` 160, `city` 120, `headline` 200, `activities` 4000 e `tools` 1000 (antes cortavam texto colado) e `workArea` 80, `seniority` 60 e nome de idioma 60 (antes a API devolvia 400). URLs do perfil ficam em 300, o tamanho da coluna. Valor de dado sensível passa a 200 e texto do banco de respostas a 5000, como na API. Carga horária acima de 10.000 é recusada antes de chamar a API.
- Layout: texto longo sem espaços não abre mais rolagem horizontal. `overflow-wrap: anywhere` no `body`, `min-width: 0` nos filhos de flex e grid que faltavam, chips e competências com altura mínima em vez de fixa. A 320 px, a tela **Importar currículo** cabe mesmo com dados normais (o campo de arquivo empurrava a página), e a legenda oculta das tabelas não alarga mais **Extensão**.
- Portal de demonstração: `obj_sal`, `q_sal` e `curso_horas` voltam a ser texto, porque `type=number` descartava `R$ 3.500,00` e `40h` enquanto a extensão relatava sucesso. O contador de `curriculo.html` saiu de dentro do `<label>`, que a extensão lia como `Nome completo *0 / 150`.
- Documentação: frontend web 2.6.

### Corrigido (bugs de QA do Trello, 01/11/2026)
- Senha: a API aceita letra de qualquer alfabeto (`\p{L}`), com ou sem acento, e a web usa a mesma regra; antes, `ãéíõçãé1` passava na web e voltava 400. A web também recusa mais de 128 caracteres antes de chamar a API, com `maxlength` no cadastro e no login, para que uma senha colada seja cortada igual nos dois (RF-01).
- Cadastro: o 409 de e-mail já cadastrado aparece no campo e-mail, não em banner geral.
- Login: o 401 vira aviso do formulário, sem marcar só a senha; o 400 deixa de ser mostrado como "E-mail ou senha incorretos".
- Onboarding: pretensão salarial sem dígitos (só "R$", ou só texto) fica como não informada, em vez de salvar R$ 0,00. Valor malformado, como `1.2.3`, continua pedindo correção.
- Perfil: o modal **Sobre você** não salva com o nome completo em branco.
- Campos personalizados: só `profile.custom_field_sensitive` mostra o aviso de dado sensível; os demais 422 mostram a mensagem da API (RF-25, RN-15).
- Acessibilidade (RNF-06): a coluna lateral do Perfil deixa de ser `<aside>`; o anúncio, que também é `<aside>`, ficava com um landmark dentro de outro (axe `landmark-complementary-is-top-level`).
- Testes: casos de senha com acento, outro alfabeto e limite de 128 caracteres em `HandlerTests`. **269 testes passando** (166 de unidade, 91 de integração, 12 de aceitação).
- Documentação: frontend web 2.5.

### Adicionado (rede de anúncios simulada, 22/10/2026)
- Rede de anúncios Google AdSense **simulada** ([ADR-0021](docs/03-arquitetura/adr/0021-rede-de-anuncios-google-adsense.md), Proposta): `GET /ads` pode devolver `format: "network"` com `network: {provider, client, slot, adFormat, mock}` e id `adsense-<slot>`. A rede vem antes do anúncio direto em `web.banner` e `ext.panel.banner` e preenche `web.profile.rail`, `web.applications.feed` e `ext.after-complete` sem anúncio direto; nunca para Plus, no contextual ou em espaço proibido. Ocultar `adsense-*` vale 30 dias; clique de rede não é contado.
- Web: `ads.js` desenha o bloco `<ins class="adsbygoogle" data-adtest="on">` com arte neutra "Anúncio do Google (simulado)" (banner até 728×90, retângulo 300×250 na coluna do Perfil, faixa horizontal no feed); selo "Patrocinado", "Por que estou vendo isso?" e "Ocultar" fora do bloco. Novo `web/js/adsense.js` com `adsenseUnit` e `loadAdSense`, que em modo mock não carrega nada (snippet real só em comentário). CSP inalterada.
- Extensão: mesmo bloco no banner do rodapé (368×64) e no cartão pós-conclusão, sem script remoto (MV3).
- Testes: `AdNetworkTests` (integração, 7 casos: banner da rede com `ca-pub-` e sem dados do perfil, Plus 204, ocultar `adsense-*` cai para direto ou 204, 4 espaços proibidos). **263 testes passando** (160 de unidade, 91 de integração, 12 de aceitação).
- Documentação: ADR-0021; monetização 1.3; API 2.4; frontend web 2.4; extensão 2.4; segurança 2.3 (T27, §5.2); política de privacidade 0.3 (§6.3); estratégia de testes 2.4; guias de estudo (pergunta sobre AdSense em extensão e LGPD).

### Corrigido (gravação do vídeo de tour, 21/10/2026)
- Perfil (web): o select **Tipo** de curso abria vazio e salvar dava só "Valor ausente ou em formato inválido.". Agora abre em **Curso** (`field()` nunca deixa `select` sem opção vazia em branco) e a API trata `type` ausente ou `null` como `Course`.
- Erros 400 legíveis: `ApiError.message` e o resumo de `showErrors` dizem "Rótulo: mensagem" com o nome do campo na tela (`fieldLabel`, `describeErrors`, `registerFieldLabels` em `web/js/api.js`). A API deixa de devolver a chave do parâmetro da action (`cmd`) em erro de formato e aponta o campo (`level`, `startDate`); só usa `body` quando nenhum campo explica o erro.
- Importação: formação sem situação (ou com rótulo não reconhecido) não é mais aprovável às cegas. A proposta marca `missingFields` sem inventar valor; a revisão mostra o campo que falta destacado, com explicação, e só libera **Aprovar** depois da escolha; **Aceitar tudo que não conflita** pula itens incompletos. O `apply` responde 400 com mensagem que nomeia o item ("Formação “Administração – UEM”: escolha a situação…").
- Importação: texto de PDF/DOCX normalizado em NFKC (ligaduras como "ﬁ"); nomes de certificações não são traduzidos (regras de importação no prompt de sistema) e Certified/Certification/Certificação no nome viram **Certificação** (`ResumeParser.CourseKind`); duplicatas entre cursos e certificações são unidas.
- Planos: `GET /billing` ganha `trialAvailable` e `trialDays`; a web só oferece "7 dias grátis" quando disponível (quem cancelou vê **Assinar o Plus**).
- Extensão: todo 200 de `/forms/open-answers` aparece no cartão (rascunho, resposta aprovada reaproveitada ou "A IA não conseguiu escrever um rascunho com os fatos do seu perfil; escreva do seu jeito." com a caixa aberta). A API nunca devolve `value` vazio: texto vazio da IA vira `kind: "missing"` com essa nota.
- Extensão: **Não tenho interesse no tema** (`scope: "topic"`) ao lado de **Ocultar** nos anúncios do painel, como na web.
- Documentação: guia do usuário 1.3, API 2.3, frontend web 2.3, extensão 2.3, IA 1.3.
- Testes: `TourFixesTests` (integração, 6 casos: curso sem tipo e erro de formato, formação sem situação na importação e no `apply`, teste grátis depois de cancelar, rascunho vazio em 3 variações) e `ResumeImportFixesTests` (unidade, 16 casos: NFKC, extrator DOCX, heurística de certificação, prompt e `missingFields`). **252 testes passando** (156 de unidade, 84 de integração, 12 de aceitação).

### Adicionado (correções pós-v2, 21/10/2026)
- API: `DELETE /api/v1/extension/devices/current` (`ExtensionOrWeb`, 204): a extensão revoga o próprio dispositivo e libera a vaga de navegador do plano; token web, sem `device_id`, recebe 404. São 77 operações em `/api/v1`.
- Extensão: "Desconectar extensão" (Ajustes) chama a rota acima antes do logout; uma falha nessa chamada não impede desconectar.
- Swagger documenta o 402 (`ProducesResponseType`) em `POST /forms/mappings`, `/forms/open-answers`, `/jobs/analyze`, `/resume-imports`, `/answers` e `/extension/pair`.
- RNF-15: timeout de 1,5 s no pedido de anúncio na web (`web/js/ads.js`) e no painel (`fetchAd` em `extension/sidepanel.js`), com `Promise.race`; resposta atrasada deixa o espaço vazio.
- Painel: `personal.birthDate` entra em `SENSITIVE_KEYS` (trancado, destrancar antes de aprovar, nunca em lote).
- Testes: `LinkedIn_e_GitHub_casam_por_regra_mesmo_com_camelCase` (unidade, 4 casos), `Limite_atingido_nunca_bloqueia_exportar_nem_excluir_os_dados` (TN-14), `Extensao_que_se_desconecta_libera_a_vaga_de_navegador_e_nao_compra_pacote` e `Logs_das_rotas_v2_nao_contem_dados_pessoais` (integração). **230 testes passando** (140 de unidade, 78 de integração, 12 de aceitação).

### Corrigido (pós-v2)
- Regras de LinkedIn e GitHub em `FieldRules` não casavam: `TextTools.Norm` separa camelCase ("LinkedIn" vira "linked in"). As regex passaram a `\blinked ?in\b` e `\bgit ?hub\b`.
- `POST /billing/packs` passou a exigir `Policies.WebUser`, como assinar e cancelar; token da extensão recebe 403.
- "Desconectar" no painel agora libera a vaga de navegador (antes só revogava o refresh token, e reconectar no gratuito respondia 402).

### Alterado (documentação, pós-v2)
- [API](docs/03-arquitetura/api.md), [backend](docs/03-arquitetura/backend.md), [extensão](docs/03-arquitetura/extensao.md), [frontend web](docs/03-arquitetura/frontend-web.md), [IA](docs/03-arquitetura/ia.md), [segurança](docs/03-arquitetura/seguranca.md), [monetização](docs/01-produto/monetizacao.md), [guia do usuário](docs/06-operacao/guia-do-usuario.md), [runbook](docs/06-operacao/runbook-local.md), [estratégia de testes](docs/04-qualidade/estrategia-de-testes.md), [vídeos](docs/06-operacao/videos.md) (gravados com 223 testes e 76 operações) e guias de estudo atualizados para as correções acima.
- [DAT](docs/03-arquitetura/dat.md) 2.2: seções 3 a 6 reescritas para o sistema real (.NET 8 com Clean Architecture, SPA em JavaScript puro servida pela API, extensão MV3), no lugar do alvo antigo em TypeScript/Node.

### Conhecido (pendências que continuam abertas)
- O painel não oferece "ocultar este tema" (a API e a web oferecem).
- O anúncio recompensado é concedido sem confirmação de exibição pelo servidor e `POST /ads/rewarded/claim` não tem rate limit dedicado.
- Os evals de IA descritos em [IA §7](docs/03-arquitetura/ia.md) e na estratégia de testes ainda não existem no código.
- [ADR-0017](docs/03-arquitetura/adr/0017-backend-em-csharp-dotnet-clean-architecture.md) e outros ADRs (0018 a 0020, entre outros) continuam com status Proposta.
- Nenhum cenário de aceitação (Reqnroll) novo para a v2.

### Adicionado (implementação da v2, 21/10/2026)
- API: `BillingController` (`GET /billing`, `POST /billing/subscribe`, `/billing/cancel`, `/billing/packs` com 201), `AdsController` (`GET /ads`, `POST /ads/{id}/hide`, `/ads/{id}/click`, `/ads/rewarded/claim`), `CatalogController` (`GET /catalog/areas`), `OnboardingController` (`GET/PUT /onboarding`) e rotas `/profile/custom-fields` no `ProfileController`. São 76 operações em `/api/v1` (eram 61).
- Limites do plano Gratuito com 402 `plan.limit_reached` (`resource`, `used` e `limit` no topo e em `extensions`), também para a 16ª resposta aprovada e para o 2º navegador pareado. Recompensado com 409 `ads.rewarded_exhausted` e `ads.rewarded_unavailable` (Plus). Pacotes cobrem só candidaturas assistidas e sugestões de IA.
- Catálogo de anúncios estático (`AdCatalog`, 16 anúncios de demonstração com ids de texto e `demo: true`); o banco guarda só ocultações e contadores agregados.
- Migration PostgreSQL `20261021233314_V2PlanosAnunciosOnboarding`: 8 tabelas novas (`CustomFields`, `OnboardingProgress`, `Subscriptions`, `MonthlyUsages`, `UsagePacks`, `UsageMarks`, `AdHides`, `AdStats`), 25 no total, e 3 colunas em `Profiles`. O SQLite segue com `EnsureCreated`.
- Eventos de auditoria `plan.changed`, `pack.purchased`, `ad.hidden`, `rewarded.claimed`, `onboarding.completed` e `custom_field.changed`.
- Seeds de demonstração Rafael (`demo@zyft.local`) e Beatriz (`beatriz@demo.zyft`), senha `Zyft@2026`.
- Web: `web/css/{tokens,components,app}.css`, `web/js/{ui,ads,billing,catalog,store}.js` e a tela `views/plans.js`; reservas quando um endpoint da v2 falha.
- Extensão 0.4.0: painel reescrito (`sidepanel.{html,js,css}`), ícones em `extension/icons/`, fontes Archivo e JetBrains Mono empacotadas em `extension/fonts/` (OFL), 6 estados internos mostrados em 4 estados visuais, anúncio contextual com até 3 tentativas e queda para o banner, nenhum anúncio na aba Formulário nem desconectado.
- 223 testes passando (136 de unidade, 75 de integração, 12 de aceitação) na primeira contagem da v2; depois das correções pós-v2, 230.
- [Vídeos de apresentação](docs/06-operacao/videos.md): executivo, técnico e onboarding no sistema (em produção).

### Corrigido (implementação da v2)
- `background.js` lia os tokens do pareamento no lugar errado: a API devolve `{device, tokens: {accessToken, refreshToken}}`.
- O service worker passou a aceitar mensagens só de páginas da própria extensão.

### Alterado (documentação sincronizada com a implementação, 21/10/2026)
- Arquitetura ([extensão](docs/03-arquitetura/extensao.md), [web](docs/03-arquitetura/frontend-web.md), [API](docs/03-arquitetura/api.md), [backend](docs/03-arquitetura/backend.md), [modelo de dados](docs/03-arquitetura/modelo-de-dados.md), [segurança](docs/03-arquitetura/seguranca.md), [design system](docs/03-arquitetura/design-system.md)), [guia do usuário](docs/06-operacao/guia-do-usuario.md), [runbook local](docs/06-operacao/runbook-local.md), [estratégia de testes](docs/04-qualidade/estrategia-de-testes.md), [monetização](docs/01-produto/monetizacao.md), [DRP](docs/01-produto/drp.md) e [guias de estudo](docs/08-time/guias-de-estudo/README.md) passaram a descrever o que foi implementado, com rótulos reais de telas e botões, códigos de erro reais e as limitações conhecidas.
- [ADR-0012](docs/03-arquitetura/adr/0012-registro-minimo-de-conclusao-no-mvp1.md) ganhou uma nota de divergência (a implementação tem status e anotações) com proposta de atualização. O status do ADR não mudou.

### Conhecido (pendências registradas na documentação)
- ~~"Desconectar" no painel não revoga o navegador~~ (resolvido pós-v2).
- ~~Sem o timeout de 1,5 s para anúncios (RNF-15)~~ (resolvido pós-v2); o resgate do recompensado não é verificado no servidor nem tem rate limit próprio (continua aberto); ~~`POST /billing/packs` aceita token da extensão~~ (resolvido pós-v2).
- ~~TN-14 sem teste automatizado~~ (resolvido pós-v2); nenhum cenário de aceitação novo para a v2.

### Adicionado (documentação da v2, 21/10/2026)
- [Por que o Zyft](docs/00-contexto/por-que-o-zyft.md): a história do produto, a pesquisa e o porquê de cada decisão da v2.
- [Resultados da pesquisa v2](docs/05-pesquisa/resultados-v2.md): 15 respostas válidas (a linha copiada da v1 foi excluída), metodologia, tabelas, validação das hipóteses H1–H8 nos dois cortes (n = 15 e o recorte do plano, n = 12), limitações e o que cada achado mudou no produto.
- [Monetização](docs/01-produto/monetizacao.md): planos Gratuito, Plus (R$ 9,90/mês ou R$ 99/ano) e pacote avulso (R$ 4,90), limites, anúncio recompensado, formatos e regras de anúncio e estimativa de receita como hipótese.
- [Design system](docs/03-arquitetura/design-system.md): tokens, componentes, estados de campo, padrões de anúncio e voz.
- ADRs [0018](docs/03-arquitetura/adr/0018-onboarding-em-passos-com-perguntas-adiaveis.md) (onboarding em passos com perguntas adiáveis), [0019](docs/03-arquitetura/adr/0019-area-profissional-dinamica-e-campos-personalizados.md) (área profissional dinâmica e campos personalizados) e [0020](docs/03-arquitetura/adr/0020-monetizacao-com-anuncios-e-planos.md) (monetização com anúncios e planos no MVP 1, decisão de escopo sob RN-07), todos como Proposta.
- Requisitos RF-23 a RF-29, regras RN-08 a RN-15, requisitos não funcionais RNF-14 a RNF-17, critérios de aceite CA-23 a CA-29 e testes negativos TN-11 a TN-15. A numeração nova começa em RF-23 porque RF-21 e RF-22 já existiam.
- [Guia do usuário](docs/06-operacao/guia-do-usuario.md) com o passo a passo completo e os usuários de demonstração (Rafael e Beatriz).
- [Guias de estudo](docs/08-time/guias-de-estudo/README.md) por integrante, para a apresentação à banca.
- [Resumo da v2 para a Entrega 3](docs/entregas/entrega-3-v2-resumo.md).

### Alterado (documentação da v2)
- Personas 2.0: PN1–PN5 com o resultado da validação (PN1, PN2 e PN4 confirmadas como perfil; PN3 e PN5 não confirmadas) e personas narrativas Rafael (primária), Bia (secundária) e Beatriz (Saúde, hipótese a validar).
- DVP-E, DRP, fluxos, roadmap, métricas (metas fixadas a partir da pesquisa), glossário, DVS (§4.3), histórico, plano de pesquisa e rascunhos de termos e política de privacidade atualizados para planos, anúncios e pagamento simulado.
- Documentos de arquitetura (API, backend, modelo de dados, frontend web, extensão, segurança) reescritos para a implementação real em .NET 8 e para os contratos da v2; IA e DAT com notas da v2.
- Critérios de aceite, estratégia de testes e runbook local atualizados.

### Corrigido (documentação)
- Disposição a pagar: a contagem bruta da planilha ("não pagaria" = 4) incluía a linha da v1. Com as 15 respostas válidas, são 3 (20%).

### Adicionado (Entrega 3 — desenvolvimento e validação)
- MVP 1 implementado em C#/.NET 8 com Clean Architecture ([ADR-0017](docs/03-arquitetura/adr/0017-backend-em-csharp-dotnet-clean-architecture.md)): `Zyft.Domain`, `Zyft.Application` (CQRS com MediatR e FluentValidation), `Zyft.Infrastructure` (EF Core 8 com PostgreSQL/SQLite, JWT, BCrypt, AES-GCM, Ollama, PdfPig/OpenXml) e `Zyft.Api` (61 operações em `/api/v1`, ProblemDetails em pt-BR, Swagger, Serilog, rate limiting, health checks).
- Plataforma web (HTML/CSS/JS sem build) servida pela API, extensão MV3 integrada à API e portal de vagas de demonstração em `/demo/`.
- 157 testes automatizados (90 unidade, 55 integração, 12 aceitação com Reqnroll), `docker-compose.yml` (PostgreSQL 16 + Ollama opcional), Dockerfile e CI no GitHub Actions.
- Documento da Entrega 3 ([desenvolvimento e validação](docs/entregas/entrega-3-desenvolvimento-e-validacao.md)) com evidências e capturas, e gerador `ferramentas/entregas/gerar_entrega3.py` (o gerador da Entrega 2 passou a aceitar fonte, título, capa e imagens PNG).

### Corrigido
- `POST /forms/completions` recusava o host enviado pelo painel no portal demo (`localhost:5080`) com 422: `TextTools.HostOf` só considera URLs http(s) com host (RF-19).
- O menu da plataforma aparecia na tela de login: `[hidden]` agora vence regras de `display` em `web/styles.css`.

### Alterado
- O protótipo Node passou a ser apenas referência; o runbook local descreve a execução da API .NET.

### Alterado (reposicionamento)
- **Reposicionamento do produto** ([ADR-0001](docs/03-arquitetura/adr/0001-reposicionamento-mvp1-candidatura-assistida.md)): o MVP 1 passa a ser a Candidatura Assistida. O marketplace de TI (PJ/CLT), o painel central de candidaturas e os outros portais vão para o MVP 2.
- Toda a documentação foi reescrita: DVP-E 2.0, DRP 2.0, DAT, DVS, personas de necessidade (PN1–PN5), fluxos, roadmap e métricas.
- A pesquisa (Google Forms) foi reescrita para a v2 com foco na Candidatura Assistida. As respostas da v1 foram arquivadas na planilha.
- O `Codigo.gs` agora se vincula ao formulário existente e gera o relatório da v2. Respostas simuladas no formulário oficial deixaram de ser possíveis.

### Adicionado
- ADRs 0001 a 0016 e o template MADR em pt-BR.
- Arquitetura detalhada: backend, extensão, frontend web, IA, contrato da API v1, modelo de dados e modelo de ameaças.
- Estratégia de testes com evals de IA e critérios de aceite CA-01 a CA-22.
- Agentes de IA do time em `.claude/agents/` (CEO, PO, PM, arquitetos de software e de IA, devs backend, frontend e extensão, QA, designer UX, privacidade e segurança), `CLAUDE.md`, papéis e RACI, processo e templates de PR e issues.
- Ferramentas da pesquisa: `questionario-v2.json` como fonte de verdade, `migracao-v2.gs`, `aplicar_pesquisa_v2.py` e os geradores de Markdown.
- Rascunhos de termos de uso e política de privacidade para o MVP 1.

### Movido
- A documentação e os mockups do marketplace foram para `docs/arquivo/marketplace-v1/`, sem alteração de conteúdo.

### Segurança
- Foi registrada uma lacuna do protótipo: o crawler não exclui campos `password` ao ler o formulário ([ADR-0010](docs/03-arquitetura/adr/0010-sem-credenciais-sem-contorno-de-seguranca-campos-bloqueados.md)). A correção é obrigatória antes de usar o protótipo em portais reais.
