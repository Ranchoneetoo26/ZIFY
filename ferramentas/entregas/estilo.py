"""Estilo comum dos PDFs de entrega do Zyft (ReportLab).

Reproduz o visual da Entrega 1: A4, margens de 2,5 cm, DejaVu Sans, títulos em azul
#1F4E79, tabelas com cabeçalho azul e linhas alternadas, rodapé cinza com o título e a página.
"""
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import cm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import Paragraph, Table, TableStyle

FONT_DIR = "/usr/share/fonts/truetype/dejavu/"

AZUL = colors.Color(0.121569, 0.305882, 0.47451)  # #1F4E79
GRADE = colors.Color(0.721569, 0.768627, 0.823529)  # #B8C4D2
LINHA_ALT = colors.Color(0.909804, 0.933333, 0.960784)  # #E8EEF5
CINZA = colors.Color(0.501961, 0.501961, 0.501961)
FUNDO_CODIGO = colors.HexColor("#F3F5F8")

PAGE = A4
MARGEM = 2.5 * cm
MARGEM_VERT = 2 * cm
LARGURA_UTIL = PAGE[0] - 2 * MARGEM - 12  # 12 = padding padrão do Frame
LARGURA_TABELA = 16 * cm  # tabelas da Entrega 1 têm 16 cm, centralizadas


def registrar_fontes():
    for nome, arq in [
        ("DejaVu", "DejaVuSans.ttf"),
        ("DejaVu-Bold", "DejaVuSans-Bold.ttf"),
        ("DejaVu-Oblique", "DejaVuSans-Oblique.ttf"),
        ("DejaVu-BoldOblique", "DejaVuSans-BoldOblique.ttf"),
        ("DejaVuMono", "DejaVuSansMono.ttf"),
        ("DejaVuMono-Bold", "DejaVuSansMono-Bold.ttf"),
    ]:
        try:
            pdfmetrics.getFont(nome)
        except KeyError:
            pdfmetrics.registerFont(TTFont(nome, FONT_DIR + arq))
    pdfmetrics.registerFontFamily(
        "DejaVu", normal="DejaVu", bold="DejaVu-Bold", italic="DejaVu-Oblique", boldItalic="DejaVu-BoldOblique"
    )
    pdfmetrics.registerFontFamily(
        "DejaVuMono", normal="DejaVuMono", bold="DejaVuMono-Bold", italic="DejaVuMono", boldItalic="DejaVuMono-Bold"
    )


registrar_fontes()

ESTILOS = {
    "corpo": ParagraphStyle("corpo", fontName="DejaVu", fontSize=10, leading=14.5, spaceAfter=6),
    "h1": ParagraphStyle(
        "h1", fontName="DejaVu-Bold", fontSize=15, leading=19, textColor=AZUL, spaceBefore=10, spaceAfter=8, keepWithNext=1
    ),
    "h2": ParagraphStyle(
        "h2", fontName="DejaVu-Bold", fontSize=11.5, leading=15, textColor=AZUL, spaceBefore=8, spaceAfter=4, keepWithNext=1
    ),
    "h3": ParagraphStyle(
        "h3", fontName="DejaVu-Bold", fontSize=10, leading=13.5, textColor=AZUL, spaceBefore=4, spaceAfter=3, keepWithNext=1
    ),
    "bullet": ParagraphStyle(
        "bullet", fontName="DejaVu", fontSize=10, leading=14.5, leftIndent=14, spaceAfter=3,
        bulletFontName="Helvetica", bulletFontSize=10, bulletIndent=3,
    ),
    "celula": ParagraphStyle("celula", fontName="DejaVu", fontSize=8.6, leading=11.5),
    "celula_cab": ParagraphStyle("celula_cab", fontName="DejaVu-Bold", fontSize=8.6, leading=11.5, textColor=colors.white),
    "codigo": ParagraphStyle("codigo", fontName="DejaVuMono", fontSize=7.4, leading=9.6),
    "legenda": ParagraphStyle(
        "legenda", fontName="DejaVu-Oblique", fontSize=8.6, leading=11.5, textColor=CINZA, alignment=TA_CENTER, spaceAfter=8
    ),
    "capa_titulo": ParagraphStyle("capa_titulo", fontName="DejaVu-Bold", fontSize=30, leading=36, textColor=AZUL, alignment=TA_CENTER),
    "capa_sub": ParagraphStyle("capa_sub", fontName="DejaVu", fontSize=14, leading=20, alignment=TA_CENTER),
    "capa_cab": ParagraphStyle("capa_cab", fontName="DejaVu-Bold", fontSize=11, leading=17, alignment=TA_CENTER),
    "capa_txt": ParagraphStyle("capa_txt", fontName="DejaVu", fontSize=11, leading=17, alignment=TA_CENTER),
    "sumario1": ParagraphStyle("sumario1", fontName="DejaVu-Bold", fontSize=10, leading=15, leftIndent=0),
    "sumario2": ParagraphStyle("sumario2", fontName="DejaVu", fontSize=9.4, leading=13, leftIndent=16),
}


def bullet(texto, estilo="bullet"):
    """Item de lista com o marcador '•' em Helvetica, como na Entrega 1."""
    return Paragraph(texto, ESTILOS[estilo], bulletText="•")


def tabela(linhas, larguras, cabecalho=True):
    """Tabela no padrão da Entrega 1. `linhas` é uma lista de listas de strings (markup ReportLab)."""
    dados = []
    for i, linha in enumerate(linhas):
        estilo = ESTILOS["celula_cab"] if (cabecalho and i == 0) else ESTILOS["celula"]
        dados.append([c if not isinstance(c, str) else Paragraph(c, estilo) for c in linha])
    if all(isinstance(x, float) and x <= 1.0 for x in larguras):
        larguras = [x * LARGURA_TABELA for x in larguras]
    t = Table(dados, colWidths=larguras, repeatRows=1 if cabecalho else 0, hAlign="CENTER")
    cmds = [
        ("GRID", (0, 0), (-1, -1), 0.5, GRADE),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 5),
        ("RIGHTPADDING", (0, 0), (-1, -1), 5),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
    ]
    if cabecalho:
        cmds.append(("BACKGROUND", (0, 0), (-1, 0), AZUL))
    inicio = 1 if cabecalho else 0
    for r in range(inicio, len(dados)):
        if (r - inicio) % 2 == 1:
            cmds.append(("BACKGROUND", (0, r), (-1, r), LINHA_ALT))
    t.setStyle(TableStyle(cmds))
    return t


def rodape(texto):
    """Função onPage que desenha o rodapé (título à esquerda, página à direita)."""

    def desenhar(canvas, doc):
        canvas.saveState()
        canvas.setFillColor(CINZA)
        canvas.setFont("DejaVu", 8)
        canvas.drawString(2 * cm, 1.2 * cm, texto)
        canvas.drawRightString(PAGE[0] - 2 * cm, 1.2 * cm, str(canvas.getPageNumber()))
        canvas.restoreState()

    return desenhar
