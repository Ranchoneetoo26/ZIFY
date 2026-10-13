# Runbook — rodar o protótipo localmente

## Controle do documento

| Campo | Valor |
|---|---|
| Projeto | Zyft — Candidatura Assistida |
| Documento | Runbook local do protótipo |
| Versão | 1.0 |
| Status | Vigente até a migração do protótipo para o monorepo ([ADR-0003](../03-arquitetura/adr/0003-monorepo-typescript-node.md)) |
| Dono | Dev backend |
| Documentos relacionados | [Ambientes](ambientes.md), [DAT §2](../03-arquitetura/dat.md#2-estado-atual-protótipo) |

**Histórico de revisões**

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 12/10/2026 | Passo a passo do protótipo de 05–06/10/2026 e problemas comuns. |

> **Atenção:** o código do protótipo (`server/` e `extension/`) ainda está **fora deste repositório**, na cópia local de quem o desenvolveu. Ele entra no monorepo na tarefa F0.6 do [roadmap](../01-produto/roadmap.md#3-fase-0--descoberta-e-redução-de-risco). Até lá, os caminhos abaixo são relativos à pasta do protótipo.

## 1. Pré-requisitos

| Item | Versão | Observação |
|---|---|---|
| Node.js | 22 ou superior | O servidor usa `import.meta.dirname` |
| Ollama | Atual | IA local |
| Modelo `qwen2.5:7b` | ~4,7 GB | Roda bem numa GPU de 12 GB; em CPU funciona, mas devagar |
| Chrome ou Edge | Atual | Para carregar a extensão |

## 2. Subir a IA local

```bash
ollama pull qwen2.5:7b
ollama list          # confirma que o modelo está disponível
```

Se o Ollama não estiver rodando como serviço, inicie com `ollama serve`.

## 3. Subir a plataforma e a API

```bash
cd server
npm install
npm start            # ou: npm run dev (reinicia ao salvar)
```

O terminal mostra:

- `Plataforma:  http://localhost:4317`
- `Demo portal: http://localhost:4317/demo/`
- `Token da extensão: <token>`
- o estado da IA local (pronta, modelo não baixado ou Ollama offline).

Variáveis opcionais:

| Variável | Padrão | Uso |
|---|---|---|
| `PORT` | `4317` | Porta da plataforma e da API |
| `OLLAMA_URL` | `http://localhost:11434` | Endereço do Ollama |
| `OLLAMA_MODEL` | `qwen2.5:7b` | Modelo; ex.: `OLLAMA_MODEL=llama3.1:8b npm start` |
| `DATA_DIR` | `server/data` | Onde fica o `db.json` |

Perfil de exemplo:

```bash
curl -X PUT localhost:4317/api/profile \
  -H "authorization: Bearer <token>" \
  -H 'content-type: application/json' \
  -d @server/examples/perfil-exemplo.json
```

O token também aparece na plataforma, em **Configurações**.

## 4. Carregar a extensão

1. Abra `chrome://extensions` (ou `edge://extensions`).
2. Ative o **Modo do desenvolvedor**.
3. Clique em **Carregar sem compactação** e selecione a pasta `extension/`.
4. Clique no ícone da extensão para abrir o painel lateral.
5. Clique em ⚙ e cole o token.

## 5. Testar no portal demo

1. Abra `http://localhost:4317/demo/`.
2. Na página de vaga, clique em **Analisar vaga**.
3. Nas páginas de currículo e de candidatura, clique em **Ler formulário**, revise e preencha.
4. Confira que nada foi enviado e que senha, CAPTCHA, anexo e termos ficaram para você.

Para usar a extensão em outros sites, ela pede permissão para o site no primeiro uso. **Não use portais reais com contas falsas**; para gravar demonstrações em portal real, a pessoa testadora faz login na própria conta e o fluxo para antes de salvar ou enviar.

## 6. Problemas comuns

| Sintoma | Causa provável | O que fazer |
|---|---|---|
| "Token inválido" no painel | Token não colado ou dados recriados | Copie o token em Configurações e cole de novo no painel (⚙) |
| `403 Somente localhost` | Acesso por IP da rede ou outro host | Use `http://localhost:4317` ou `127.0.0.1` |
| "Ollama offline" | Ollama parado | `ollama serve` e confira `OLLAMA_URL` |
| "modelo não baixado" | Modelo ausente | `ollama pull qwen2.5:7b` ou ajuste `OLLAMA_MODEL` |
| Análise de vaga demora 20–30 s | Esperado com 7B; mais lento em CPU | Use GPU ou um modelo menor para demonstração |
| Resultado da análise muda entre execuções | Variação natural do modelo 7B | Esperado; a evidência mostrada permite conferir |
| "Ana Paula Souza" vira "Ana" + "Paula Souza" | Limitação conhecida da divisão de nome | Corrija no painel antes de preencher |
| Campos não aparecem | Campos dentro de iframe ou shadow DOM | Limitação conhecida do protótipo |
| Extensão não reage após editar o código | Extensão carregada em cache | Clique em recarregar em `chrome://extensions` e recarregue a página |
| Servidor no WSL e navegador no Windows | `localhost` não encaminhado | Confira o encaminhamento de portas do WSL; use `127.0.0.1` |
| Quero recomeçar do zero | — | Pare o servidor e apague `server/data/db.json` (gera novo token) |
