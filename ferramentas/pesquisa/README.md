# Pesquisa — ferramentas

Scripts da pesquisa de validação da Candidatura Assistida. O plano, as hipóteses e os critérios estão em [`docs/05-pesquisa/plano-de-pesquisa.md`](../../docs/05-pesquisa/plano-de-pesquisa.md).

| Arquivo | Para que serve |
|---|---|
| `questionario-v2.json` | **Fonte de verdade** do questionário v2: título, descrição, perguntas em ordem, perguntas removidas da v1 e IDs do formulário e da planilha |
| `migracao-v2.gs` | Apps Script que aplica o JSON no Google Forms. Copia as respostas antigas para a aba "Arquivo v1 — marketplace", cria, edita, reordena e remove perguntas. Pode rodar de novo sem duplicar nada |
| `aplicar_pesquisa_v2.py` | Python (Playwright) que usa o seu navegador logado no Google para criar o projeto do Apps Script, colar o `migracao-v2.gs` com o JSON embutido e executar uma função |
| `gerar_questionario_md.py` | Gera `docs/05-pesquisa/questionario-v2.md` a partir do JSON |
| `Codigo.gs` | Apps Script do relatório: lê as respostas e gera um Google Docs com persona, ranking de dores (validada, não validada ou poucos dados) e disposição de uso |

## Como alterar o questionário

1. Edite `questionario-v2.json`. Para manter uma pergunta existente (e as respostas dela), deixe o campo `de` com o ID do item no formulário. Sem `de`, o item é criado.
2. Regere a versão legível: `python gerar_questionario_md.py` (atualiza `docs/05-pesquisa/questionario-v2.md`).
3. Abra um Chromium com perfil próprio e a porta de depuração (exemplo: `chromium --remote-debugging-port=9333 --user-data-dir=~/.zyft-chrome`) e faça login no Google com uma conta que edita o formulário.
4. Rode:

   ```bash
   python -m venv .venv && .venv/bin/pip install playwright
   .venv/bin/python aplicar_pesquisa_v2.py inspecionar      # só lê: itens do formulário e abas da planilha
   .venv/bin/python aplicar_pesquisa_v2.py migrarParaV2     # arquiva a v1 e aplica o JSON
   ```

   Na primeira execução, o Google pede autorização. Autorize você mesmo na janela do navegador: Revisar permissões → conta → Avançado → Acessar → Permitir.

## Como gerar o relatório

1. Em script.google.com, crie um projeto e cole o `Codigo.gs`.
2. Rode `vincularFormulario()` uma vez. Ele acha as perguntas pelo título e falha se alguma não existir.
3. Rode `gerarPlano()`. O link do Docs aparece no registro de execução.
4. (Opcional) Rode `ativarAtualizacaoAutomatica()` para refazer o relatório a cada resposta.

Não rode respostas simuladas no formulário oficial. A função `simularRespostas` foi removida justamente para isso não acontecer.

## Privacidade

Não commite exportações da planilha. O contato deixado pelas pessoas é usado só para convidar para testes.
