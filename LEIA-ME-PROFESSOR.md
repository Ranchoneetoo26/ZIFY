# Zyft: como avaliar (Entrega 3)

Repositório: **https://github.com/Ranchoneetoo26/ZIFY** (branch `main`)

O Zyft tem três partes que trabalham juntas:

1. **API em C#/.NET 8** (`src/`), que também serve a plataforma web (`web/`) em http://localhost:5080;
2. **Extensão do navegador** (`extension/`), que é onde a candidatura assistida acontece: ela lê o formulário da vaga e preenche só o que a pessoa aprova;
3. **Inteligência artificial local** (Ollama), usada pela API.

Sem a extensão e sem a IA, o professor vê apenas o cadastro e o Perfil Mestre. Para avaliar o produto inteiro, as três precisam estar rodando.

## 1. Pré-requisitos

| Item | Para quê | Obrigatório? |
|---|---|---|
| .NET 8 SDK | rodar a API e os testes | Sim |
| Ollama + modelo `qwen2.5:7b` | IA (ver seção 3) | Sim, para avaliar o fluxo completo |
| Google Chrome, Edge ou Brave | instalar a extensão | Sim, para o fluxo da candidatura |
| Docker | só para rodar com PostgreSQL | Não (em desenvolvimento usa SQLite) |

## 2. Subir a API e a plataforma web

```bash
dotnet run --project src/Zyft.Api
```

Abra http://localhost:5080. O banco SQLite é criado sozinho, com dois usuários de demonstração (senha `Zyft@2026`):

- `demo@zyft.local`: Rafael, área Tecnologia, perfil completo;
- `beatriz@demo.zyft`: Beatriz, área Saúde, com o onboarding pela metade.

A documentação da API (Swagger) fica em http://localhost:5080/swagger.

## 3. Por que a IA é necessária, e como instalar

A IA faz quatro coisas que as regras sozinhas não conseguem:

| Função | O que a IA faz | Sem IA |
|---|---|---|
| Ler o formulário da vaga | aponta qual dado do perfil corresponde a cada campo quando o rótulo não é óbvio (ex.: "Como prefere ser chamado?") | só os campos com rótulo conhecido (nome, e-mail, telefone, LinkedIn…) são reconhecidos; o resto fica "Precisa de você" |
| Importar currículo (PDF/DOCX) | transforma o texto do currículo em experiências, formação, cursos e competências para a pessoa revisar | a importação depende da IA para montar a proposta |
| Analisar a vaga | separa os requisitos da vaga e mostra onde cada um aparece no perfil | a análise depende da IA |
| Perguntas abertas | escreve um rascunho só com fatos aprovados do perfil, que a pessoa revisa | a pessoa escreve do zero |

Regras de segurança da IA: ela **nunca** inventa o valor de um campo (só aponta a chave do perfil), **nunca** recebe dados sensíveis e **todo** texto que escreve é marcado e revisado pela pessoa. Ela roda **localmente**, sem enviar dados para fora.

Instalação:

1. Instale o Ollama: https://ollama.com/download
2. Baixe o modelo (cerca de 4,7 GB): `ollama pull qwen2.5:7b`
3. Deixe o Ollama aberto. A API procura em `http://localhost:11434` (configurável em `src/Zyft.Api/appsettings.json`, seção `Llm`).
4. Conferência: http://localhost:5080/health deve mostrar `Healthy`. Se mostrar `Degraded`, a API está sem IA e funciona só com regras.

Com Docker, a IA também sobe com `docker compose --profile ai up -d --build` e depois `docker compose exec ollama ollama pull qwen2.5:7b`.

## 4. Instalar a extensão

1. No navegador, abra `chrome://extensions` (ou `edge://extensions` / `brave://extensions`).
2. Ligue o **Modo do desenvolvedor**.
3. Clique em **Carregar sem compactação** e escolha a pasta `extension/` do projeto.
4. Fixe o ícone do Zyft e abra o **painel lateral**.
5. Na plataforma web, entre como `demo@zyft.local`, vá em **Extensão** e clique em **Gerar código de pareamento**.
6. Digite o código de 6 dígitos no painel e clique em **Conectar**. No plano gratuito cabe 1 navegador; se já houver um conectado, desconecte-o em Extensão.

## 5. Roteiro de avaliação (cerca de 10 minutos)

1. Abra o portal de demonstração: http://localhost:5080/demo/
2. Na página da vaga, abra o painel do Zyft e clique em **Analisar esta vaga**: aparecem os requisitos e onde cada um está no perfil, sem nota nem porcentagem.
3. Vá para o formulário de candidatura e clique em **Ler o formulário desta página**.
4. Confira os estados dos campos (Pronto, Precisa de você, Ação sua, Sugestão da IA), use **Aprovar os N**, peça um rascunho de IA numa pergunta aberta e clique em **Preencher N campos**.
5. Note que o Zyft **não envia** o formulário, não marca termos, não resolve CAPTCHA e não anexa arquivos: isso é sempre da pessoa.
6. Clique em **Concluí a candidatura** e confirme; a candidatura aparece em **Candidaturas** na plataforma, com status e anotações.
7. Na plataforma, veja também: onboarding em passos (entre como `beatriz@demo.zyft`), Perfil Mestre, Importar currículo, Planos e Registro de ações.

## 6. Testes

```bash
dotnet test
```

263 testes (unidade, integração e aceitação). Não precisam de Ollama nem de Docker: a IA é substituída por um dublê nos testes.

Mais detalhes: `README.md`, `docs/06-operacao/guia-do-usuario.md` e `docs/06-operacao/runbook-local.md`.
