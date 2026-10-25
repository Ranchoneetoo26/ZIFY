# Runbook — rodar o Zyft localmente

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Runbook local (API .NET, plataforma web, extensão e portal demo) |
| Versão | 3.3 |
| Status | Vigente |
| Dono | Dev backend |
| Documentos relacionados | [Ambientes](ambientes.md), [ADR-0017](../03-arquitetura/adr/0017-backend-em-csharp-dotnet-clean-architecture.md), [Entrega 3](../entregas/entrega-3-desenvolvimento-e-validacao.md), [Guia do usuário](guia-do-usuario.md), [Monetização](../01-produto/monetizacao.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | Passo a passo do protótipo Node de 05–06/10/2026 e problemas comuns. |
| 2.0 | 15/10/2026 | Reescrito para a API .NET 8 do repositório (SQLite, Docker, testes, extensão). O protótipo Node passou a ser só referência. |
| 3.0 | 21/10/2026 | v2: dois usuários de demonstração (Rafael e Beatriz), como recriar o seed, anúncios de demonstração, planos com pagamento simulado, como testar o limite (402) e o recompensado, novos problemas comuns. |
| 3.1 | 21/10/2026 | Sincronizado com a implementação: catálogo de anúncios estático (16 anúncios no código), rótulos reais do painel 0.4.0 e da tela Planos, 402 também no pareamento e no banco de respostas, `ads.rewarded_unavailable`, SQLite com `EnsureCreated`, 223 testes. |
| 3.2 | 21/10/2026 | 230 testes (140 + 78 + 12); "Desconectar extensão" no painel libera a vaga de navegador. |
| 3.3 | 21/10/2026 | 252 testes (156 + 84 + 12), com as correções da gravação do tour. |

> O protótipo Node (`server/`, porta 4317) **não é mais a forma de rodar o Zyft**. A lógica dele foi portada para a API .NET ([ADR-0017](../03-arquitetura/adr/0017-backend-em-csharp-dotnet-clean-architecture.md)) e ele fica só como referência histórica.

## 1. Pré-requisitos

| Item | Versão | Observação |
|---|---|---|
| .NET SDK | 8.0.400 ou mais novo da linha 8 | `global.json`. Se instalado na pasta do usuário: `export PATH=$HOME/.dotnet:$PATH DOTNET_ROOT=$HOME/.dotnet` |
| Docker e Docker Compose | Atual | Só para rodar com PostgreSQL |
| Chrome ou Edge | Atual | Para carregar a extensão |
| Ollama + `qwen2.5:7b` | Atual (~4,7 GB) | Opcional. Sem IA, as regras continuam funcionando. |

## 2. Rodar com SQLite (desenvolvimento)

```bash
dotnet build
dotnet run --project src/Zyft.Api        # http://localhost:5080
```

| Endereço | O que é |
|---|---|
| `http://localhost:5080/` | Plataforma web |
| `http://localhost:5080/swagger` | Swagger (botão **Authorize**: cole só o `accessToken`) |
| `http://localhost:5080/demo/` | Portal de vagas de demonstração |
| `http://localhost:5080/health` | Saúde do banco e da IA |

- O banco fica em `src/Zyft.Api/zyft-dev.db`, criado na primeira subida com `EnsureCreated` (o SQLite não usa as migrations; elas são do PostgreSQL).
- Usuários de demonstração (só em Development, com qualquer banco; desligue com `Seed__DemoUser=false`), todos fictícios. Os dois já vêm com os consentimentos de IA para textos e de análise de vaga ligados:

  | Pessoa | E-mail | Senha | Conteúdo |
  |---|---|---|---|
  | Rafael Moreira | `demo@zyft.local` | `Zyft@2026` | Área Tecnologia, perfil rico (experiências, competências, idiomas, formação, respostas aprovadas), onboarding concluído, plano gratuito com uso do mês já iniciado (7 de 10 candidaturas assistidas, 2 de 5 sugestões de IA, 4 de 10 análises, 1 de 2 importações). Até a v1 este mesmo e-mail trazia o perfil "Ana Paula Souza". |
  | Beatriz Lima | `beatriz@demo.zyft` | `Zyft@2026` | Área Saúde (técnica de enfermagem, COREN), campos personalizados, onboarding parado na pergunta de competências (22 de 25) com perguntas puladas: ao entrar, ela cai direto no onboarding; ao sair dele, o Perfil Mestre mostra o alerta de pendentes. 2 de 10 candidaturas assistidas usadas. |

  O seed só cria um usuário se o e-mail ainda não existir. Um banco criado antes da v2 continua com o perfil antigo: veja a seção 2.1.
- IA: se o Ollama responder em `http://localhost:11434` com `qwen2.5:7b`, ele é usado. Para desligar: `Llm__Provider=None dotnet run --project src/Zyft.Api`.

### 2.1 Recriar os dados de demonstração

```bash
# pare a API (Ctrl+C) e apague o banco local
rm src/Zyft.Api/zyft-dev.db
dotnet run --project src/Zyft.Api        # recria o banco e os dois usuários (zera uso, resgates e ocultações)
```

Para não criar os usuários de demonstração: `Seed__DemoUser=false dotnet run --project src/Zyft.Api`.

## 3. Rodar com Docker (PostgreSQL 16)

```bash
docker compose up -d --build                  # API + PostgreSQL; a migration é aplicada na subida
docker compose --profile ai up -d --build     # + Ollama no contêiner
docker compose exec ollama ollama pull qwen2.5:7b
docker compose logs -f api
docker compose down -v                        # para e apaga o volume
```

Segredos num `.env` na raiz (fora do Git): `ZYFT_JWT_SECRET` (32+ caracteres), `ZYFT_ENCRYPTION_KEY` (32 bytes em Base64), `ZYFT_DB_PASSWORD`, `ZYFT_LLM_PROVIDER` (`Ollama` ou `None`) e `ZYFT_LLM_URL`. Nesse modo não há usuário de demonstração: crie uma conta.

### 3.1 Planos, limites e anúncios de demonstração

Tudo aqui é simulado: não existe gateway de pagamento, nenhum dado de cartão é pedido e a interface mostra "Ambiente de demonstração: nenhuma cobrança real" (RN-13). Os anúncios vêm de um **catálogo estático no código** (`src/Zyft.Application/Ads/AdCatalog.cs`): 16 anúncios fictícios de "Anunciante de demonstração", de várias áreas, com ids de texto (por exemplo `demo-tec-cloud`) e `demo: true` na resposta. O banco guarda só as ocultações e os contadores agregados de clique.

| Quero ver | Como |
|---|---|
| O plano e o uso do mês | Plataforma → **Planos**, ou `GET /api/v1/billing` no Swagger (`demo: true`, preços 9.90 / 99.00 / 4.90, `usage` por recurso) |
| Assinar o Plus (trial de 7 dias) | **Planos → Testar 7 dias grátis** (escolha mensal ou anual), ou `POST /api/v1/billing/subscribe {"period":"monthly"}` (ou `"yearly"`) |
| Voltar ao gratuito | **Cancelar assinatura**, ou `POST /api/v1/billing/cancel`. Na demonstração a volta ao gratuito é imediata; cancelar no gratuito responde `billing.not_subscribed` |
| Comprar um pacote | **Comprar pacote**, ou `POST /api/v1/billing/packs` (responde **201**; +15 assistidas, +10 IA, 60 dias). O pacote cobre só candidaturas assistidas e sugestões de IA |
| Um anúncio | Abra o Perfil Mestre no gratuito (`web.profile.rail`), ou `GET /api/v1/ads?placement=web.banner`. Com `&topic=Inglês avançado`, a seleção usa o requisito |
| Por que não veio anúncio | `204` é esperado para Plus, placement desconhecido ou proibido, catálogo sem anúncio para a área ou tudo oculto. Em `ext.job.contextual`, só vem anúncio se o `topic` casar com o assunto de algum anúncio. Sem `placement` a resposta é `400` |
| Ocultar | `POST /api/v1/ads/{id}/hide {"scope":"ad"}` (30 dias) ou `{"scope":"topic"}` (sempre) |

#### Testar o limite (402)

Os limites do gratuito são por mês civil: 10 candidaturas assistidas, 5 sugestões de IA, 10 análises de vaga, 2 importações. Também respondem 402: a 16ª resposta no banco de respostas e o pareamento de um 2º navegador. O Rafael já começa o mês com 7 de 10 assistidas usadas, então o limite chega na 4ª página nova.

1. Entre com um usuário no plano gratuito e conecte a extensão.
2. Leia formulários em **páginas diferentes** (origem + caminho). Com o portal demo, use `curriculo.html`, `candidatura.html` e outras páginas, ou chame `POST /api/v1/forms/mappings` no Swagger variando `pageUrl`. Ler a mesma página de novo não conta.
3. Na 11ª página nova do mês, a API responde **402** com `code: "plan.limit_reached"` e `resource`, `used` e `limit` tanto no topo do ProblemDetails quanto em `extensions`. O painel mostra as opções: oferta recompensada, pacote ou Plus.
4. Confira que editar o perfil, exportar (`GET /me/export`) e excluir a conta continuam funcionando (RN-12).

Para testar mais rápido as sugestões de IA, o limite é 5 (`POST /forms/open-answers`, exige o consentimento de IA para textos e o Ollama ligado).

#### Testar o recompensado

1. No painel, use **Ganhar +3 vendo uma oferta** (ou `POST /api/v1/ads/rewarded/claim`). A oferta usa o placement `ext.panel.banner`. O bônus do mês sobe 3.
2. Repita até 3 vezes. A quarta responde **409** `ads.rewarded_exhausted`. Para quem é Plus a resposta é **409** `ads.rewarded_unavailable`.
3. A ordem de consumo é: limite do mês, depois bônus recompensado, depois pacotes (o que vence antes primeiro).

Para zerar o uso e os resgates do mês, recrie o banco (seção 2.1).

## 4. Rodar os testes

```bash
dotnet test                         # unidade, integração e aceitação
dotnet test tests/Zyft.UnitTests    # só unidade
```

São 252 testes: 156 de unidade, 84 de integração e 12 cenários de aceitação. Os testes de integração e aceitação usam SQLite em memória e IA desligada ou simulada. Não precisam de Docker nem de Ollama.

## 5. Carregar a extensão

1. Abra `chrome://extensions` (ou `edge://extensions`) e ligue o **Modo do desenvolvedor**.
2. Clique em **Carregar sem compactação** e selecione a pasta `extension/`.
3. Clique no ícone do Zyft para abrir o painel lateral (versão 0.4.0).
4. Na plataforma web, vá em **Extensão → Gerar código de pareamento**, digite o código de 6 dígitos no painel (vale 10 minutos), dê um nome ao navegador e toque em **Conectar**.
5. Se a API estiver em outro endereço, troque em **Ajustes** no painel (campo "Endereço da API"; padrão `http://localhost:5080`).

## 6. Testar no portal demo

1. Abra `http://localhost:5080/demo/`.
2. Em `vaga.html`, na aba **A vaga e você**, clique em **Analisar esta vaga** (exige o consentimento de Análise de vaga).
3. Em `curriculo.html` (e `curriculo.html?preenchido=1`) e `candidatura.html`, na aba **Formulário**, clique em **Ler o formulário desta página**, revise e use **Preencher N campos**. Para uma nova etapa do mesmo site, **Ler novamente (nova etapa)**.
4. Confira que nada foi enviado e que senha, CAPTCHA, anexo, termos e dados sensíveis ficaram como **Ação sua**.
5. Clique em **Concluí a candidatura** e veja o registro em **Candidaturas**.

Para usar a extensão em outros sites, ela pede permissão para o site no primeiro uso. **Não use portais reais com contas falsas**; para gravar demonstrações em portal real, a pessoa testadora faz login na própria conta e o fluxo para antes de salvar ou enviar.

## 7. Problemas comuns

| Sintoma | Causa provável | O que fazer |
|---|---|---|
| `address already in use` na porta 5080 | Outra instância da API ainda rodando | Encerre o processo antigo. A porta 5080 é fixa porque é o padrão da extensão. |
| A API não sobe: segredo JWT ou chave AES | `Jwt:Secret` vazio ou `Encryption:Key` sem 32 bytes | Em Docker, confira o `.env`; fora de Development, defina as variáveis |
| `/health` com `llm: Degraded` | Ollama parado ou modelo não baixado | `ollama serve` e `ollama pull qwen2.5:7b`, ou ignore: as regras funcionam sem IA |
| Primeira chamada com IA demora ~40 s | O Ollama está carregando o modelo | Esperado; as seguintes levam segundos |
| Painel mostra 403 em Analisar vaga ou Sugerir texto | Consentimento desligado | Ligue em **Conta** (ou na tela de boas-vindas) |
| Painel pede pareamento de novo | Dispositivo revogado ou refresh expirado (7 dias) | Gere outro código na plataforma |
| Extensão não reage após editar o código | Extensão em cache | Recarregue em `chrome://extensions` e recarregue a página |
| API no WSL e navegador no Windows | `localhost` não encaminhado | Confira o encaminhamento de portas do WSL; use `127.0.0.1` |
| Quero recomeçar do zero | — | Pare a API e apague `src/Zyft.Api/zyft-dev.db` (ou `docker compose down -v`) |
| `demo@zyft.local` ainda mostra "Ana Paula Souza" e `beatriz@demo.zyft` não existe | Banco criado antes da v2; o seed não sobrescreve usuário existente | Apague `zyft-dev.db` e suba de novo (seção 2.1) |
| Login dos usuários demo falha no Docker | Em Docker (Staging) não há seed | Crie uma conta ou rode com SQLite em Development |
| Painel mostra "limite do mês" logo no início dos testes | Leituras anteriores no mesmo mês contaram | Recrie o banco ou assine o Plus de demonstração |
| Conectar um segundo navegador responde 402 | Gratuito permite 1 navegador ativo | No navegador antigo, **Ajustes → Desconectar extensão** (chama `DELETE /extension/devices/current` e libera a vaga); se ele não estiver mais acessível ou a API estava fora do ar na hora de desconectar, revogue em **Extensão** na plataforma; ou assine o Plus de demonstração (até 3) |
| Nenhum anúncio aparece | Plano Plus, placement sem anúncio, tudo oculto ou área sem anúncio no catálogo. No painel, nunca há anúncio na aba Formulário nem desconectado | Esperado (204). Cancele o Plus ou recrie o banco para limpar ocultações |
| Não consigo salvar a 16ª resposta | Gratuito guarda até 15 respostas aprovadas | Esperado (402). Apague uma resposta ou use o Plus de demonstração |
| `422 profile.custom_field_sensitive` | O rótulo do campo personalizado parece dado sensível | Esperado (RN-15): use Dados sensíveis |
