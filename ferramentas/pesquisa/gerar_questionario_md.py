"""Gera docs/05-pesquisa/questionario-v2.md a partir de questionario-v2.json."""
import json
from pathlib import Path

AQUI = Path(__file__).resolve().parent
SAIDA = AQUI.parents[1] / "docs" / "05-pesquisa" / "questionario-v2.md"
TIPOS = {"escolha": "Múltipla escolha", "caixas": "Caixas de seleção", "escala": "Escala", "grade": "Grade",
         "paragrafo": "Parágrafo", "curta": "Resposta curta"}


def main() -> None:
    spec = json.loads((AQUI / "questionario-v2.json").read_text(encoding="utf-8"))
    L = ["# Questionário v2 — Candidatura Assistida", "",
         "| Campo | Valor |", "|---|---|",
         "| Documento | Questionário da pesquisa v2 |", f"| Versão | {spec['versao']} |", "| Status | Em campo |",
         "| Fonte de verdade | [`ferramentas/pesquisa/questionario-v2.json`](../../ferramentas/pesquisa/questionario-v2.json); "
         "este arquivo é gerado por `ferramentas/pesquisa/gerar_questionario_md.py` |",
         "| Formulário | Google Forms do time (o mesmo da v1, reescrito em 12/10/2026) |",
         "| Plano | [plano-de-pesquisa.md](plano-de-pesquisa.md) |", "",
         f"**Título:** {spec['titulo']}", "", "**Descrição:**", ""]
    L += ["> " + l if l else ">" for l in spec["descricao"].split("\n")]
    L += ["", f"**Mensagem de confirmação:** {spec['confirmacao']}", ""]
    n = 0
    for it in spec["itens"]:
        t = it["tipo"]
        if t in ("secao", "secao_inicial"):
            L += [f"## {it['titulo']}", ""] + (["> " + it["ajuda"], ""] if it.get("ajuda") else [])
            continue
        if t == "texto":
            L += [f"**{it['titulo']}**", "", "> " + it["ajuda"], ""]
            continue
        n += 1
        extra = []
        if it.get("outro"):
            extra.append('com opção "Outro"')
        if "(até 3)" in it["titulo"]:
            extra.append("no máximo 3")
        req = "obrigatória" if it.get("obrigatoria") else "opcional"
        L += [f"**{n}. {it['titulo']}**  ",
              f"*{TIPOS[t]} · {req}{' · ' + ', '.join(extra) if extra else ''}* · chave `{it['chave']}`", ""]
        if it.get("ajuda"):
            L += [f"Ajuda: {it['ajuda']}", ""]
        if t in ("escolha", "caixas"):
            L += [f"- {o}" for o in it["opcoes"]] + [""]
        elif t == "escala":
            L += [f"{it['min']} ({it['rotuloMin']}) a {it['max']} ({it['rotuloMax']})", ""]
        elif t == "grade":
            L += ["| Linha | " + " | ".join(it["colunas"]) + " |", "|---|" + "---|" * len(it["colunas"])]
            L += [f"| {r} |" + " |" * len(it["colunas"]) for r in it["linhas"]] + [""]
    L += ["## Perguntas removidas da v1", "", "| Pergunta da v1 | Motivo |", "|---|---|"]
    L += [f"| {r['titulo']} | {r['motivo']} |" for r in spec["removidos"]] + [""]
    SAIDA.write_text("\n".join(L), encoding="utf-8")
    print(f"{SAIDA} ({n} perguntas)")


if __name__ == "__main__":
    main()
