"""Gera o PDF da Entrega 3 (Desenvolvimento e Validação) a partir do Markdown fonte.

Reaproveita o conversor e o estilo da Entrega 2 (gerar_entrega2.gerar). As capturas de tela ficam em
docs/entregas/img/e3-*.png e entram no PDF como imagem.

Uso: python3 ferramentas/entregas/gerar_entrega3.py [saida.pdf]
"""
import os
import sys

AQUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, AQUI)
import gerar_entrega2  # noqa: E402

RAIZ = gerar_entrega2.RAIZ
FONTE_MD = os.path.join(RAIZ, "docs", "entregas", "entrega-3-desenvolvimento-e-validacao.md")
SAIDA_PADRAO = os.path.abspath(os.path.join(RAIZ, "..", "entregas", "Zyft_Entrega3_Desenvolvimento_e_Validacao.pdf"))
TITULO = "Zyft – Entrega 3: Desenvolvimento e Validação"


def main():
    gerar_entrega2.gerar(
        fonte_md=FONTE_MD,
        saida=sys.argv[1] if len(sys.argv) > 1 else SAIDA_PADRAO,
        titulo=TITULO,
        entrega="Entrega 3 – Desenvolvimento e Validação",
        data="25 de outubro de 2026",
    )


if __name__ == "__main__":
    main()
