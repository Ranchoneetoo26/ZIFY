# ADR-0021 — Rede de anúncios Google AdSense para preencher o inventário (simulada)

| Campo | Valor |
|---|---|
| Status | Proposta |
| Data | 22/10/2026 |
| Decisores | CEO, Product Owner, Arquiteto de software |
| Consultados | Responsável por privacidade, Dev frontend, Dev extensão, Dev backend |
| Requisitos | RF-29; RN-08, RN-09, RN-10, RN-11; RNF-09, RNF-14, RNF-15 |
| Relacionados | [ADR-0020](0020-monetizacao-com-anuncios-e-planos.md), [ADR-0002](0002-extensao-de-navegador-mv3-com-painel-lateral.md), [ADR-0015](0015-observabilidade-sem-dados-pessoais.md) |

## Contexto

A [ADR-0020](0020-monetizacao-com-anuncios-e-planos.md) colocou anúncios no plano Gratuito em seis espaços (`web.profile.rail`, `web.applications.feed`, `web.banner`, `ext.panel.banner`, `ext.job.contextual`, `ext.after-complete`). Hoje esses espaços só têm anúncios **diretos**: um catálogo de demonstração com 16 anúncios, escolhidos na API por área, cargos desejados e texto do requisito da vaga.

Fatos que pedem uma decisão:

- Sem anunciantes reais, boa parte do inventário fica vazio (`204`), e a estimativa de receita da [monetização §9](../../01-produto/monetizacao.md#9-estimativa-de-receita-hipótese) depende de vender todos os espaços. Um time de quatro pessoas não tem como vender anúncio direto em escala no MVP 1.
- Redes de anúncios preenchem espaços vazios sem venda direta. O Google AdSense é a mais conhecida, aceita sites pequenos e tem formato responsivo (`<ins class="adsbygoogle">`).
- A rede é **terceiro**: carrega script, define cookies e decide o anúncio. Isso toca LGPD, CSP, desempenho e as regras inegociáveis do Zyft (nenhum dado do perfil para anunciante, RN-09).
- A banca e quem investiria precisam ver como o inventário seria preenchido, sem que nenhum dado real saia do Zyft nesta fase.

## Fatores de decisão

- Nada do perfil vai para a rede (RN-09) e o Zyft continua controlando onde há anúncio (RN-08, RN-11).
- Selo "Patrocinado", "Por que estou vendo isso?" e "Ocultar" continuam valendo para o espaço da rede (RN-10).
- LGPD: cookies de publicidade exigem base legal (consentimento) e transparência.
- Políticas da rede e das lojas de extensão (Chrome Web Store, Edge Add-ons) e o Manifest V3, que proíbe código remoto.
- Desempenho: anúncio nunca atrasa a tela (RNF-15).
- Prazo e tamanho do time.

## Opções consideradas

### Opção A — Só anúncios diretos

Manter apenas o catálogo próprio (hoje de demonstração) e vender diretamente.

- **Prós:** controle total do conteúdo; nenhum terceiro, nenhum cookie; já implementado e testado.
- **Contras:** inventário vazio sem equipe comercial; receita depende de vendas que o time não consegue fazer no MVP 1.

### Opção B — Google AdSense como rede de preenchimento (escolhida, simulada nesta versão)

A API decide se o espaço é da rede; o cliente monta o bloco no formato do AdSense.

- **Prós:** preenche espaços sem venda direta; formato responsivo pronto; integração pequena (um script e um `<ins>`); aceita publicadores pequenos.
- **Contras:** terceiro com cookies (LGPD, CMP); políticas do AdSense restritivas para software e extensões; CSP precisa abrir domínios do Google; custo de desempenho; menos controle do conteúdo do anúncio.

### Opção C — Outras redes (Ad Manager, Carbon Ads, EthicalAds, redes de afiliados)

- **Prós:** algumas são mais respeitosas com privacidade (EthicalAds e Carbon Ads não usam rastreamento) e combinam melhor com público técnico.
- **Contras:** Carbon e EthicalAds são focadas em desenvolvedores (o Zyft atende várias áreas: saúde, varejo, administrativo); exigem tráfego mínimo e aprovação; Ad Manager exige operação comercial. Ficam como alternativa se o AdSense for recusado.

## Decisão

Escolhemos a **Opção B**: o Google AdSense passa a ser a **rede de preenchimento** do inventário, **simulado nesta versão**.

Obrigatório:

1. **Simulação.** `Ads:Network:Mock = true` por padrão. Nenhuma chamada sai para o Google: a API devolve `format: "network"` com `network: {provider, client, slot, adFormat, mock}` (id de publicador fictício `ca-pub-0000000000000000`) e o cliente desenha um `<ins class="adsbygoogle" data-adtest="on">` com uma arte neutra "Anúncio do Google (simulado)". Sem logos nem marca do Google.
2. **Prioridade.** O anúncio direto vem antes da rede, **exceto nos banners** (`web.banner` e `ext.panel.banner`), que são o formato padrão de rede. Em `web.profile.rail`, `web.applications.feed` e `ext.after-complete` a rede só entra quando não há anúncio direto. `ext.job.contextual` nunca é da rede (o contextual usa o texto da vaga e não pode sair do Zyft).
3. **Mesmas regras do Zyft.** Nunca para Plus, nunca nos espaços proibidos (RN-08), no máximo um por tela (RN-11). Selo "Patrocinado", "Por que estou vendo isso?" e "Ocultar" ficam **fora** do bloco da rede (RN-10). "Ocultar" vale para o bloco (`adsense-<slot>`, 30 dias); a rede não tem "tema".
4. **Não personalizado e sem dados do perfil.** O bloco é o mesmo para todo mundo; a API não passa área, cargos nem nada do perfil para a rede. Em produção, só anúncios não personalizados (`requestNonPersonalizedAds`). O Zyft não conta clique de rede (quem conta é a rede).
5. **Extensão sem código remoto.** A extensão nunca carrega o script do AdSense (MV3 e CSP `script-src 'self'`).

Proibido nesta versão: carregar `pagead2.googlesyndication.com`, definir cookies de terceiros, mudar `Mock` para `false`.

## Consequências

**Positivas**
- O inventário deixa de ficar vazio na demonstração e a estimativa de receita fica explicável.
- A integração real é pequena: o contrato da API e o markup do cliente já estão no formato do AdSense.
- Nenhum dado sai do Zyft nesta versão.

**Negativas**
- Mais um caminho no `GetAdHandler` e nos clientes (direto x rede).
- Em produção, menos controle sobre o conteúdo dos anúncios (pode aparecer anúncio que contraria RN-06, como "emprego garantido"); exige bloquear categorias no painel do AdSense.

**Riscos e mitigação**

| Risco | Mitigação |
|---|---|
| **LGPD:** cookies de publicidade sem consentimento. | Antes de ativar: CMP (ferramenta de gestão de consentimento) certificada pelo Google, banner de cookies com recusa tão fácil quanto aceitar, script do Google só depois do consentimento; sem consentimento, nenhum script. Atualizar a política de privacidade (seção de rede, hoje "simulada") e o registro de operações. |
| **Políticas do AdSense não permitem anúncios dentro de software ou extensões de navegador** (o AdSense é para páginas web com conteúdo próprio). **Confirmar na política vigente do programa antes de qualquer ativação.** | Para produção, **a extensão fica só com anúncios diretos** (`ext.panel.banner` e `ext.after-complete` sem rede: `Ads:Network:Slots` sem esses espaços). A rede só seria ativada na plataforma web. |
| Chrome Web Store e MV3 proíbem código remoto e restringem uso de dados para publicidade. | Extensão nunca carrega script remoto (o bloco simulado é só HTML local). |
| **CSP**: a CSP de `web/index.html` (`script-src 'self'`) bloqueia o AdSense. | Em produção, liberar `script-src` (`pagead2.googlesyndication.com`, `partner.googleadservices.com`), `frame-src` (`googleads.g.doubleclick.net`, `tpc.googlesyndication.com`) e os `img-src`/`connect-src` correspondentes, só depois do consentimento; revisão de segurança dessa mudança. |
| **Desempenho (RNF-15):** o script do AdSense é pesado e pode atrasar a tela. | Carregar `async`, depois do conteúdo principal, uma vez por sessão (`loadAdSense` em `web/js/adsense.js`); espaço com altura reservada para não deslocar o layout; medir LCP/CLS antes e depois. |
| Conteúdo do anúncio contraria RN-06 (promessa de contratação). | Bloquear categorias e anunciantes no painel do AdSense; revisão amostral; se não der para controlar, desligar a rede. |
| Receita baixa da rede para o volume do MVP 1. | É preenchimento, não modelo principal; o Plus continua sendo a receita principal (ADR-0020). |

## Conformidade

- Testes de integração em `tests/Zyft.IntegrationTests/AdNetworkTests.cs`: `web.banner` devolve `format: "network"` com `network.client` começando com `ca-pub-` e `mock: true`; Plus recebe `204`; ocultar `adsense-*` faz o espaço cair para anúncio direto ou `204`; espaço proibido responde `204`; resposta sem nome, telefone nem e-mail.
- Revisão de PR: nenhum `<script src="https://pagead2...">` ativo no repositório; `extension/` sem URL remota de script; CSP de `web/index.html` inalterada enquanto este ADR estiver como Proposta.
- Ativar a rede de verdade exige este ADR **Aceito**, parecer de privacidade, CMP funcionando e confirmação das políticas do AdSense.

## Links

- [ADR-0020 — Monetização com anúncios e planos](0020-monetizacao-com-anuncios-e-planos.md)
- [Monetização §6](../../01-produto/monetizacao.md#6-anúncios)
- [API — anúncios](../api.md), [Frontend web](../frontend-web.md), [Extensão](../extensao.md), [Segurança](../seguranca.md)
- [Política de privacidade](../../07-legal/politica-de-privacidade.md)
- Código: `src/Zyft.Application/Ads/IAdNetwork.cs`, `src/Zyft.Infrastructure/Ads/AdSenseNetwork.cs`, `GetAdHandler` em `src/Zyft.Application/Ads/AdFeatures.cs`, `web/js/adsense.js`, `web/js/ads.js`, `extension/sidepanel.js`
