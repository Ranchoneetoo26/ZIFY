# Runbook — rodar o Zyft localmente

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Runbook local (API .NET, plataforma web, extensão e portal demo) |
| Versão | 2.0 |
| Status | Vigente |
| Dono | Dev backend |
| Documentos relacionados | [Ambientes](ambientes.md), [ADR-0017](../03-arquitetura/adr/0017-backend-em-csharp-dotnet-clean-architecture.md), [Entrega 3](../entregas/entrega-3-desenvolvimento-e-validacao.md) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | Passo a passo do protótipo Node de 05–06/10/2026 e problemas comuns. |
| 2.0 | 15/10/2026 | Reescrito para a API .NET 8 do repositório (SQLite, Docker, testes, extensão). O protótipo Node passou a ser só referência. |

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

- O banco fica em `src/Zyft.Api/zyft-dev.db`, criado na primeira subida.
- Usuário de demonstração (só em Development): **demo@zyft.local / Zyft@2026**, com perfil fictício.
- IA: se o Ollama responder em `http://localhost:11434` com `qwen2.5:7b`, ele é usado. Para desligar: `Llm__Provider=None dotnet run --project src/Zyft.Api`.

## 3. Rodar com Docker (PostgreSQL 16)

```bash
docker compose up -d --build                  # API + PostgreSQL; a migration é aplicada na subida
docker compose --profile ai up -d --build     # + Ollama no contêiner
docker compose exec ollama ollama pull qwen2.5:7b
docker compose logs -f api
docker compose down -v                        # para e apaga o volume
```

Segredos num `.env` na raiz (fora do Git): `ZYFT_JWT_SECRET` (32+ caracteres), `ZYFT_ENCRYPTION_KEY` (32 bytes em Base64), `ZYFT_DB_PASSWORD`, `ZYFT_LLM_PROVIDER` (`Ollama` ou `None`) e `ZYFT_LLM_URL`. Nesse modo não há usuário de demonstração: crie uma conta.

## 4. Rodar os testes

```bash
dotnet test                         # unidade, integração e aceitação
dotnet test tests/Zyft.UnitTests    # só unidade
```

Os testes de integração e aceitação usam SQLite em memória e IA desligada ou simulada. Não precisam de Docker nem de Ollama.

## 5. Carregar a extensão

1. Abra `chrome://extensions` (ou `edge://extensions`) e ligue o **Modo do desenvolvedor**.
2. Clique em **Carregar sem compactação** e selecione a pasta `extension/`.
3. Clique no ícone do Zyft para abrir o painel lateral.
4. Na plataforma web, vá em **Extensão → Gerar código de pareamento** e digite o código de 6 dígitos no painel (vale 10 minutos).
5. Se a API estiver em outro endereço, troque em **Ajustes** (engrenagem) no painel.

## 6. Testar no portal demo

1. Abra `http://localhost:5080/demo/`.
2. Em `vaga.html`, clique em **Analisar vaga** no painel (exige o consentimento de Análise de vaga).
3. Em `curriculo.html` (e `curriculo.html?preenchido=1`) e `candidatura.html`, clique em **Ler formulário**, revise e preencha.
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
