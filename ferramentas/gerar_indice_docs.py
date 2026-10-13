"""Gera docs/README.md (índice com título e status de cada documento). Rode na raiz do repositório."""
import re
from pathlib import Path

SECOES = [("00-contexto", "Contexto"), ("01-produto", "Produto"), ("02-viabilidade", "Viabilidade"),
          ("03-arquitetura", "Arquitetura"), ("04-qualidade", "Qualidade"), ("05-pesquisa", "Pesquisa"),
          ("06-operacao", "Operação"), ("07-legal", "Legal"), ("08-time", "Time")]


def titulo(p: Path) -> str:
    for linha in p.read_text(encoding="utf-8").splitlines():
        if linha.startswith("# "):
            return linha[2:].strip()
    return p.name


def status(p: Path) -> str:
    m = re.search(r"^\|\s*Status\s*\|\s*(.+?)\s*\|\s*$", p.read_text(encoding="utf-8"), re.M)
    if not m:
        return ""
    s = re.sub(r"\[([^\]]+)\]\([^)]*\)", r"\1", m.group(1))
    return s.replace("*", "")


def main() -> None:
    docs = Path("docs")
    L = ["# Documentação do Zyft", "",
         "Índice de todos os documentos, gerado por `ferramentas/gerar_indice_docs.py`. O produto atual é a "
         "**Candidatura Assistida (MVP 1)**. A documentação do marketplace está em "
         "[`arquivo/marketplace-v1/`](arquivo/marketplace-v1/README.md) e **não** descreve o produto atual.", "",
         "Para quem está chegando, a ordem de leitura sugerida é: "
         "[histórico](00-contexto/historico-e-reposicionamento.md) → [DVP-E](01-produto/dvp-e.md) → "
         "[DRP](01-produto/drp.md) → [DAT](03-arquitetura/dat.md) → [ADRs](03-arquitetura/adr/README.md) → "
         "[processo](08-time/processo.md).", ""]
    for pasta, nome in SECOES:
        L += [f"## {nome}", "", "| Documento | Status |", "|---|---|"]
        for p in sorted((docs / pasta).glob("*.md")):
            L.append(f"| [{titulo(p)}]({pasta}/{p.name}) | {status(p)} |")
        for p in sorted((docs / pasta).glob("*.pdf")) + sorted((docs / pasta).glob("*.docx")):
            L.append(f"| [{p.name}]({pasta}/{p.name}) (documento de origem) | Referência |")
        if pasta == "03-arquitetura":
            L += ["", "### Decisões de arquitetura (ADRs)", "", "| ADR | Status |", "|---|---|"]
            for p in sorted((docs / pasta / "adr").glob("0*.md")):
                if not p.name.startswith("0000"):
                    L.append(f"| [{titulo(p)}]({pasta}/adr/{p.name}) | {status(p)} |")
        L.append("")
    (docs / "README.md").write_text("\n".join(L), encoding="utf-8")


if __name__ == "__main__":
    main()
