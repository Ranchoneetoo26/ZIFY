"""Gera o PDF da Entrega 2 (Projeto e Arquitetura) a partir do Markdown fonte.

Fonte: docs/entregas/entrega-2-projeto-e-arquitetura.md
Diagramas: diagramas.py (vetoriais no PDF; PNG em docs/entregas/img para o Markdown)

Subconjunto de Markdown suportado: títulos ##/###/####, parágrafos, listas (- e 1.), tabelas com
pipes, blocos de código, imagens, **negrito**, *itálico*, `código` e links. Marcadores próprios:
  <!-- pdf:ignorar-inicio --> ... <!-- pdf:ignorar-fim -->  trecho só do Markdown
  <!-- pdf:quebra -->                                      quebra de página
  <!-- pdf:larguras 0.2,0.3,0.5 -->                         larguras da próxima tabela (frações)

Imagens: caminhos que estão em diagramas.DIAGRAMAS viram desenho vetorial; os demais (ex.: capturas de tela PNG)
são lidos do disco, relativos à pasta do Markdown, e entram com moldura fina.

O gerador é reaproveitado por outras entregas pela função gerar() (ver gerar_entrega3.py).

Uso: python3 ferramentas/entregas/gerar_entrega2.py [saida.pdf]
"""
import copy
import os
import re
import sys

from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import cm
from reportlab.pdfbase.pdfmetrics import stringWidth
from reportlab.platypus import (
    BaseDocTemplate,
    CondPageBreak,
    Frame,
    KeepTogether,
    Image,
    PageBreak,
    PageTemplate,
    Paragraph,
    Spacer,
    Table,
    TableStyle,
)
from reportlab.platypus.tableofcontents import TableOfContents

AQUI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, AQUI)
import diagramas  # noqa: E402
from estilo import (  # noqa: E402
    AZUL,
    ESTILOS,
    FUNDO_CODIGO,
    GRADE,
    LARGURA_TABELA,
    LARGURA_UTIL,
    MARGEM,
    MARGEM_VERT,
    PAGE,
    rodape,
    tabela,
)

RAIZ = os.path.abspath(os.path.join(AQUI, "..", ".."))
FONTE_MD = os.path.join(RAIZ, "docs", "entregas", "entrega-2-projeto-e-arquitetura.md")
SAIDA_PADRAO = os.path.abspath(os.path.join(RAIZ, "..", "entregas", "Zyft_Entrega2_Projeto_e_Arquitetura.pdf"))
TITULO = "Zyft – Entrega 2: Projeto e Arquitetura"

CORPO = ESTILOS["corpo"]
# Títulos sem keepWithNext: em vez disso, CondPageBreak antes de cada título (tabelas longas podem começar
# na mesma página do título e continuar na seguinte).
TITULOS = {n: ParagraphStyle(n, parent=ESTILOS[n], keepWithNext=0) for n in ("h1", "h2", "h3")}
ESTILO_TITULO_NEGRITO = ParagraphStyle("titulo_negrito", parent=CORPO, keepWithNext=0, spaceBefore=6, spaceAfter=4)
ESPACO_MINIMO_TITULO = 4 * cm
ESTILO_NUMERADO = ParagraphStyle("numerado", parent=ESTILOS["bullet"], bulletFontName="DejaVu-Bold", leftIndent=16)


# ---------------------------------------------------------------- inline
def inline(texto, tam_codigo=None, cor_codigo="#1F3A5A"):
    """Converte o Markdown de linha para o markup de Paragraph do ReportLab."""
    codigos = []

    def guardar(m):
        codigos.append(m.group(1))
        return f"\x00{len(codigos) - 1}\x00"

    t = re.sub(r"`([^`]+)`", guardar, texto)
    t = t.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
    t = t.replace("&lt;br&gt;", "<br/>")
    t = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", r"\1", t)
    t = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", t)
    t = re.sub(r"(?<![\w*])\*(?!\s)(.+?)(?<!\s)\*(?![\w*])", r"<i>\1</i>", t)

    def devolver(m):
        c = codigos[int(m.group(1))]
        c = c.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
        tam = f' size="{tam_codigo}"' if tam_codigo else ""
        return f'<font name="DejaVuMono"{tam} color="{cor_codigo}">{c}</font>'

    return re.sub(r"\x00(\d+)\x00", devolver, t)


# ---------------------------------------------------------------- blocos
def bloco_codigo(linhas):
    largura = LARGURA_UTIL - 12
    fs = 7.4
    maior = max((stringWidth(l, "DejaVuMono", fs) for l in linhas), default=0)
    if maior > largura:
        fs = max(5.6, fs * largura / maior)
    estilo = ParagraphStyle("cod", parent=ESTILOS["codigo"], fontSize=fs, leading=fs * 1.3)
    paras = []
    for l in linhas:
        esc = l.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace(" ", "&nbsp;")
        paras.append([Paragraph(esc or "&nbsp;", estilo)])
    t = Table(paras, colWidths=[LARGURA_UTIL], hAlign="CENTER")
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, -1), FUNDO_CODIGO),
        ("BOX", (0, 0), (-1, -1), 0.5, GRADE),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("RIGHTPADDING", (0, 0), (-1, -1), 6),
        ("TOPPADDING", (0, 0), (-1, -1), 0),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 0),
        ("TOPPADDING", (0, 0), (-1, 0), 5),
        ("BOTTOMPADDING", (0, -1), (-1, -1), 5),
    ]))
    return [KeepTogether([t]), Spacer(1, 8)]


def larguras_auto(linhas):
    """Distribui 16 cm entre as colunas pelo tamanho do conteúdo, sem quebrar palavras curtas."""
    n = len(linhas[0])
    limpo = lambda s: re.sub(r"[`*]", "", s)  # noqa: E731
    medias, minimos = [], []
    for c in range(n):
        textos = [limpo(l[c]) if c < len(l) else "" for l in linhas]
        comp = [len(t) for t in textos]
        medias.append(max(4, (sum(comp) / len(comp)) * 0.6 + min(max(comp), 80) * 0.4))
        palavras = [w for t in textos[1:] for w in t.split()]
        maior = max((stringWidth(w, "DejaVu-Bold", 8.6) for w in palavras if len(w) <= 20), default=20)
        maior = max([maior] + [stringWidth(w, "DejaVu-Bold", 8.6) for w in textos[0].split()])
        minimos.append(min(maior + 12, LARGURA_TABELA * 0.3))
    total = sum(medias)
    ws = [LARGURA_TABELA * m / total for m in medias]
    for _ in range(5):
        falta = sum(max(0, minimos[i] - ws[i]) for i in range(n))
        if falta <= 0.5:
            break
        livres = [i for i in range(n) if ws[i] > minimos[i]]
        sobra = sum(ws[i] - minimos[i] for i in livres)
        for i in range(n):
            if ws[i] < minimos[i]:
                ws[i] = minimos[i]
        for i in livres:
            ws[i] -= falta * (ws[i] - minimos[i]) / sobra if sobra else 0
    return ws


# Larguras fixas para tabelas repetidas (fichas de endpoint e dicionário de dados), por cabeçalho.
LARGURAS_CONHECIDAS = {
    ("Item", "Descrição"): [0.15, 0.85],
    ("Coluna", "Tipo", "Nulo", "Restrições", "Descrição"): [0.19, 0.165, 0.075, 0.215, 0.355],
    ("Tabela.Coluna", "Tipo", "Nulo", "Restrições", "Descrição"): [0.25, 0.155, 0.075, 0.19, 0.33],
}


def bloco_tabela(linhas_md, larguras):
    linhas = []
    for i, l in enumerate(linhas_md):
        if i == 1 and re.match(r"^\|[\s:|-]+\|$", l.strip()):
            continue
        celulas = [c.strip() for c in re.split(r"(?<!\\)\|", l.strip())[1:-1]]
        linhas.append(celulas)
    n = len(linhas[0])
    linhas = [(r + [""] * n)[:n] for r in linhas]
    if not larguras:
        larguras = LARGURAS_CONHECIDAS.get(tuple(linhas[0]))
    ws = [f * LARGURA_TABELA for f in larguras] if larguras else larguras_auto(linhas)
    conv = [[inline(c, 7.8, "#FFFFFF" if i == 0 else "#1F3A5A") for c in r] for i, r in enumerate(linhas)]
    return [tabela(conv, ws), Spacer(1, 8)]


BASE_IMAGENS = os.path.dirname(FONTE_MD)


def bloco_raster(alt, caminho):
    arquivo = os.path.join(BASE_IMAGENS, caminho)
    if not os.path.exists(arquivo):
        raise SystemExit(f"Imagem não encontrada: {arquivo}")
    from reportlab.lib.utils import ImageReader

    w, h = ImageReader(arquivo).getSize()
    maxw = LARGURA_UTIL - 8
    maxh = PAGE[1] - 2 * MARGEM_VERT - 12 - 80
    s = min(maxw / w, maxh / h)
    img = Image(arquivo, width=w * s, height=h * s)
    moldura = Table([[img]], colWidths=[w * s + 2], hAlign="CENTER")
    moldura.setStyle(TableStyle([
        ("BOX", (0, 0), (-1, -1), 0.6, GRADE),
        ("LEFTPADDING", (0, 0), (-1, -1), 1),
        ("RIGHTPADDING", (0, 0), (-1, -1), 1),
        ("TOPPADDING", (0, 0), (-1, -1), 1),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 1),
    ]))
    return [KeepTogether([Spacer(1, 4), moldura, Spacer(1, 4), Paragraph(inline(alt), ESTILOS["legenda"])])]


def bloco_imagem(alt, caminho):
    fn = diagramas.DIAGRAMAS.get(caminho)
    if fn is None:
        return bloco_raster(alt, caminho)
    d = fn()
    maxh = PAGE[1] - 2 * MARGEM_VERT - 12 - 60
    s = min(1.0, LARGURA_UTIL / d.width, maxh / d.height)
    d.scale(s, s)
    d.width, d.height = d.width * s, d.height * s
    d.hAlign = "CENTER"
    return [KeepTogether([Spacer(1, 4), d, Spacer(1, 4), Paragraph(inline(alt), ESTILOS["legenda"])])]


# ---------------------------------------------------------------- parser
def ler_markdown(caminho):
    texto = open(caminho, encoding="utf-8").read()
    texto = re.sub(r"<!-- pdf:ignorar-inicio -->.*?<!-- pdf:ignorar-fim -->", "", texto, flags=re.S)
    return texto.split("\n")


def converter(linhas):
    fl = []
    i, larguras = 0, None
    para = []

    def anexar(blocos):
        """Mantém um título (ou título em negrito) junto do bloco que vem logo depois."""
        if fl and isinstance(fl[-1], Paragraph) and fl[-1].style.name in ("h1", "h2", "h3", "titulo_negrito"):
            titulo = fl.pop()
            fl.append(KeepTogether([titulo, blocos[0]]))
            fl.extend(blocos[1:])
        else:
            fl.extend(blocos)

    def fechar_paragrafo():
        if para:
            t = " ".join(x.strip() for x in para)
            if re.fullmatch(r"\*\*.+\*\*", t):
                fl.append(CondPageBreak(ESPACO_MINIMO_TITULO))
                fl.append(Paragraph(inline(t), ESTILO_TITULO_NEGRITO))
            else:
                fl.append(Paragraph(inline(t), CORPO))
            para.clear()

    while i < len(linhas):
        l = linhas[i]
        s = l.strip()
        m_larg = re.match(r"<!-- pdf:larguras ([\d.,\s]+) -->", s)
        if m_larg:
            fechar_paragrafo()
            larguras = [float(x) for x in m_larg.group(1).split(",")]
            i += 1
            continue
        if s == "<!-- pdf:quebra -->":
            fechar_paragrafo()
            fl.append(PageBreak())
            i += 1
            continue
        if s.startswith("<!--"):
            i += 1
            continue
        if not s:
            fechar_paragrafo()
            i += 1
            continue
        if s.startswith("```"):
            fechar_paragrafo()
            j = i + 1
            cod = []
            while j < len(linhas) and not linhas[j].strip().startswith("```"):
                cod.append(linhas[j].rstrip())
                j += 1
            anexar(bloco_codigo(cod))
            i = j + 1
            continue
        m = re.match(r"^(#{2,4}) (.+)$", s)
        if m:
            fechar_paragrafo()
            nivel = len(m.group(1))
            estilo = {2: TITULOS["h1"], 3: TITULOS["h2"], 4: TITULOS["h3"]}[nivel]
            p = Paragraph(inline(m.group(2)), estilo)
            if not (fl and isinstance(fl[-1], PageBreak)):
                fl.append(CondPageBreak(ESPACO_MINIMO_TITULO))
            p._toc = (nivel - 2, re.sub(r"[`*]", "", m.group(2))) if nivel <= 3 else None
            fl.append(p)
            i += 1
            continue
        m = re.match(r"^!\[([^\]]*)\]\(([^)]+)\)$", s)
        if m:
            fechar_paragrafo()
            anexar(bloco_imagem(m.group(1), m.group(2)))
            i += 1
            continue
        if s.startswith("|"):
            fechar_paragrafo()
            j = i
            tab = []
            while j < len(linhas) and linhas[j].strip().startswith("|"):
                tab.append(linhas[j])
                j += 1
            fl.extend(bloco_tabela(tab, larguras))
            larguras = None
            i = j
            continue
        m = re.match(r"^(- |(\d+)\. )(.+)$", s)
        if m:
            fechar_paragrafo()
            item = m.group(3)
            j = i + 1
            while j < len(linhas) and linhas[j].startswith("  ") and linhas[j].strip() and \
                    not re.match(r"^\s*(- |\d+\. )", linhas[j]):
                item += " " + linhas[j].strip()
                j += 1
            if m.group(2):
                fl.append(Paragraph(inline(item), ESTILO_NUMERADO, bulletText=f"{m.group(2)}."))
            else:
                fl.append(Paragraph(inline(item), ESTILOS["bullet"], bulletText="•"))
            i = j
            continue
        if s.startswith("# "):
            i += 1
            continue
        para.append(l)
        i += 1
    fechar_paragrafo()
    return fl


# ---------------------------------------------------------------- documento
class Documento(BaseDocTemplate):
    def __init__(self, arquivo, titulo=TITULO, **kw):
        super().__init__(arquivo, pagesize=PAGE, leftMargin=MARGEM, rightMargin=MARGEM, topMargin=MARGEM_VERT,
                         bottomMargin=MARGEM_VERT, title=titulo, author="Equipe Zyft", **kw)
        frame = Frame(self.leftMargin, self.bottomMargin, self.width, self.height, id="normal")
        self.addPageTemplates([
            PageTemplate(id="capa", frames=[frame], onPage=lambda c, d: None),
            PageTemplate(id="corpo", frames=[frame], onPage=rodape(titulo)),
        ])
        self._n_titulo = 0

    def afterFlowable(self, f):
        toc = getattr(f, "_toc", None)
        if toc:
            nivel, texto = toc
            self._n_titulo += 1
            chave = f"t{self._n_titulo}"
            self.canv.bookmarkPage(chave)
            self.canv.addOutlineEntry(texto, chave, level=nivel, closed=nivel > 0)
            self.notify("TOCEntry", (nivel, texto, self.page, chave))

    def build_multi(self, story):
        self._n_titulo = 0
        super().multiBuild(story)

    def handle_documentBegin(self):
        self._n_titulo = 0
        super().handle_documentBegin()


def capa(entrega="Entrega 2 – Projeto e Arquitetura", data="18 de outubro de 2026"):
    from reportlab.platypus.doctemplate import NextPageTemplate

    return [
        Spacer(1, 5 * cm),
        Paragraph("Zyft", ESTILOS["capa_titulo"]),
        Spacer(1, 10),
        Paragraph("Assistente de Candidatura a Vagas", ESTILOS["capa_sub"]),
        Spacer(1, 6),
        Paragraph(entrega, ESTILOS["capa_sub"]),
        Spacer(1, 4 * cm),
        Paragraph("Integrantes", ESTILOS["capa_cab"]),
        Paragraph("Antonio Randolfo Barroso Neto", ESTILOS["capa_txt"]),
        Paragraph("Guilherme Brogio Macedo da Silva", ESTILOS["capa_txt"]),
        Paragraph("Lucas Ulisses Clementino De Arruda E Silva", ESTILOS["capa_txt"]),
        Paragraph("Matheus Guilherme Moreira De Oliveira", ESTILOS["capa_txt"]),
        Spacer(1, 2 * cm),
        Paragraph(data, ESTILOS["capa_txt"]),
        NextPageTemplate("corpo"),
        PageBreak(),
    ]


def sumario():
    toc = TableOfContents()
    toc.levelStyles = [
        ParagraphStyle("toc0", parent=ESTILOS["sumario1"], spaceBefore=5),
        ParagraphStyle("toc1", parent=ESTILOS["sumario2"]),
    ]
    toc.dotsMinLevel = 0
    return [Paragraph("Sumário", ESTILOS["h1"]), Spacer(1, 4), toc, PageBreak()]


def gerar(fonte_md=FONTE_MD, saida=SAIDA_PADRAO, titulo=TITULO, entrega="Entrega 2 – Projeto e Arquitetura",
          data="18 de outubro de 2026"):
    """Gera o PDF de uma entrega: capa, sumário e o corpo convertido do Markdown."""
    global BASE_IMAGENS
    BASE_IMAGENS = os.path.dirname(os.path.abspath(fonte_md))
    os.makedirs(os.path.dirname(os.path.abspath(saida)), exist_ok=True)
    corpo = converter(ler_markdown(fonte_md))
    doc = Documento(saida, titulo=titulo)
    doc.multiBuild(capa(entrega, data) + sumario() + corpo)
    print(saida)
    return saida


def main():
    gerar(saida=sys.argv[1] if len(sys.argv) > 1 else SAIDA_PADRAO)


if __name__ == "__main__":
    main()
