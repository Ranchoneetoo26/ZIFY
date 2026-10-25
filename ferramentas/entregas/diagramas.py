"""Diagramas da Entrega 2, desenhados com ReportLab graphics.

Cada função devolve um `Drawing` (vetorial). O PDF usa o desenho direto; o Markdown usa os PNG
gerados por `gerar_pngs()` em docs/entregas/img/.

Uso: python3 ferramentas/entregas/diagramas.py   (gera os PNG)
"""
import math
import os
import sys

from reportlab.graphics import renderPM
from reportlab.graphics.shapes import Drawing, Line, PolyLine, Polygon, Rect, String
from reportlab.lib import colors
from reportlab.pdfbase.pdfmetrics import stringWidth

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from estilo import AZUL, GRADE, LINHA_ALT  # noqa: E402  (também registra as fontes)

PRETO = colors.HexColor("#1A1A1A")
CINZA_TXT = colors.HexColor("#555555")
FUNDO_GRUPO = colors.HexColor("#F5F8FB")
VERDE = colors.HexColor("#2E7D32")
LARANJA = colors.HexColor("#B26A00")
VERMELHO = colors.HexColor("#B3261E")
F, FB, FI = "DejaVu", "DejaVu-Bold", "DejaVu-Oblique"


# ---------------------------------------------------------------- primitivas
def texto(d, x, y, s, fs=7, font=F, cor=PRETO, anchor="start"):
    d.add(String(x, y, s, fontName=font, fontSize=fs, fillColor=cor, textAnchor=anchor))


def quebrar(s, largura, fs, font=F):
    palavras, linhas, atual = s.split(), [], ""
    for p in palavras:
        cand = (atual + " " + p).strip()
        if stringWidth(cand, font, fs) <= largura or not atual:
            atual = cand
        else:
            linhas.append(atual)
            atual = p
    if atual:
        linhas.append(atual)
    return linhas


def caixa(d, x, y, w, h, titulo, linhas=(), fs=6.8, cab=AZUL, fundo=colors.white, borda=AZUL, tracejada=False,
          centralizar=True, fs_titulo=7.4):
    """Caixa com faixa de título. (x, y) é o canto inferior esquerdo."""
    r = Rect(x, y, w, h, rx=3, ry=3, fillColor=fundo, strokeColor=borda, strokeWidth=0.9)
    if tracejada:
        r.strokeDashArray = [3, 2]
    d.add(r)
    th = fs_titulo + 6
    if cab is not None:
        d.add(Rect(x, y + h - th, w, th, rx=3, ry=3, fillColor=cab, strokeColor=cab, strokeWidth=0.9))
        d.add(Rect(x, y + h - th, w, th / 2, fillColor=cab, strokeColor=cab, strokeWidth=0))
        texto(d, x + w / 2, y + h - th + 4, titulo, fs_titulo, FB, colors.white, "middle")
    else:
        texto(d, x + w / 2, y + h - th + 3, titulo, fs_titulo, FB, AZUL, "middle")
    yy = y + h - th - fs - 3
    for ln in linhas:
        for parte in quebrar(ln, w - 8, fs):
            if centralizar:
                texto(d, x + w / 2, yy, parte, fs, F, PRETO, "middle")
            else:
                texto(d, x + 4, yy, parte, fs, F, PRETO)
            yy -= fs + 2.2


def grupo(d, x, y, w, h, rotulo, fundo=FUNDO_GRUPO, tracejada=False, direita=False):
    r = Rect(x, y, w, h, rx=5, ry=5, fillColor=fundo, strokeColor=GRADE, strokeWidth=1)
    if tracejada:
        r.strokeDashArray = [4, 3]
    d.add(r)
    if direita:
        texto(d, x + w - 6, y + h - 11, rotulo, 7.6, FB, AZUL, "end")
    else:
        texto(d, x + 6, y + h - 11, rotulo, 7.6, FB, AZUL)


def ponta(d, x1, y1, x2, y2, cor=PRETO, tam=5):
    ang = math.atan2(y2 - y1, x2 - x1)
    a1, a2 = ang + math.radians(155), ang - math.radians(155)
    d.add(Polygon([x2, y2, x2 + tam * math.cos(a1), y2 + tam * math.sin(a1), x2 + tam * math.cos(a2),
                   y2 + tam * math.sin(a2)], fillColor=cor, strokeColor=cor, strokeWidth=0.5))


def rotulo(d, x, y, s, fs=6.2, cor=CINZA_TXT, fundo=colors.white, font=FI):
    linhas = s.split("\n")
    w = max(stringWidth(t, font, fs) for t in linhas) + 4
    h = len(linhas) * (fs + 1.5) + 2
    d.add(Rect(x - w / 2, y - h / 2, w, h, fillColor=fundo, strokeColor=None))
    yy = y + h / 2 - fs - 0.5
    for t in linhas:
        texto(d, x, yy, t, fs, font, cor, "middle")
        yy -= fs + 1.5


def seta(d, pontos, txt=None, pos=0.5, dupla=False, cor=PRETO, tracejada=False, dx=0, dy=0):
    """Seta por uma polilinha [x1, y1, x2, y2, ...]; ponta no último ponto."""
    pl = PolyLine(pontos, strokeColor=cor, strokeWidth=0.9)
    if tracejada:
        pl.strokeDashArray = [3, 2]
    d.add(pl)
    ponta(d, pontos[-4], pontos[-3], pontos[-2], pontos[-1], cor)
    if dupla:
        ponta(d, pontos[2], pontos[3], pontos[0], pontos[1], cor)
    if txt:
        # rótulo no ponto `pos` do comprimento total
        segs = [(pontos[i], pontos[i + 1], pontos[i + 2], pontos[i + 3]) for i in range(0, len(pontos) - 2, 2)]
        total = sum(math.hypot(b[2] - b[0], b[3] - b[1]) for b in segs)
        alvo, acc = total * pos, 0
        for x1, y1, x2, y2 in segs:
            L = math.hypot(x2 - x1, y2 - y1)
            if acc + L >= alvo:
                t = (alvo - acc) / L if L else 0
                rotulo(d, x1 + (x2 - x1) * t + dx, y1 + (y2 - y1) * t + dy, txt)
                break
            acc += L


# ---------------------------------------------------------------- 1. componentes
def componentes():
    W, H = 453, 372
    d = Drawing(W, H)
    caixa(d, 178, 344, 98, 24, "Pessoa candidata", cab=None, fundo=LINHA_ALT, fs_titulo=7.4)

    grupo(d, 2, 178, 449, 152, "Navegador da pessoa (Chrome ou Edge)")
    caixa(d, 10, 188, 112, 118, "Plataforma web",
          ["HTML, CSS e JavaScript (módulos ES, sem build)", "SPA com rotas por hash", "Perfil Mestre, importação,",
           "consentimentos, privacidade", "Servida pela API (wwwroot)"])
    grupo(d, 130, 184, 206, 132, "Extensão Manifest V3", fundo=colors.white, direita=True)
    caixa(d, 138, 246, 90, 52, "Painel lateral", ["revisão campo a campo,", "estados e ações"])
    caixa(d, 238, 246, 90, 52, "Service worker", ["guarda tokens;", "única parte que", "chama a API"])
    caixa(d, 138, 190, 90, 46, "Content script", ["lê campos visíveis", "e preenche o DOM"])
    caixa(d, 346, 188, 98, 118, "Página do portal", ["site de terceiros", "(entrada não confiável)", "", "login feito pela",
                                                    "própria pessoa"], cab=GRADE, tracejada=True)
    seta(d, [228, 272, 238, 272], dupla=True)
    seta(d, [183, 246, 183, 236], dupla=True)
    seta(d, [228, 213, 346, 213], "lê e preenche;\nnunca envia", pos=0.78, dupla=True, dy=-13)

    grupo(d, 2, 2, 449, 150, "Servidor Zyft (Docker Compose)")
    caixa(d, 150, 12, 154, 104, "Zyft.Api (ASP.NET Core 8)",
          ["Controllers + middleware de erros", "MediatR (CQRS) + FluentValidation", "JWT, refresh token, policies",
           "Rate limiting, CORS, Swagger", "Serilog sem dados pessoais", "Health Checks (/health)"])
    caixa(d, 10, 34, 104, 58, "PostgreSQL 16", ["EF Core + Migrations", "dados relacionais", "(SQLite em testes)"])
    caixa(d, 340, 34, 104, 58, "Ollama (qwen2.5:7b)", ["porta ILlmClient", "opcional: sem IA,", "as regras seguem"],
          tracejada=True)
    seta(d, [150, 63, 114, 63], "EF Core", pos=0.5, dupla=True)
    seta(d, [304, 63, 340, 63], "HTTP\nJSON", pos=0.5)

    # pessoa -> web / painel
    seta(d, [178, 356, 66, 356, 66, 306], "usa", pos=0.4)
    seta(d, [183, 344, 183, 298], "usa", pos=0.2, dx=12)
    # web -> api ; sw -> api
    seta(d, [66, 188, 66, 162, 200, 162, 200, 116], "HTTPS + JWT (scope=web)", pos=0.4)
    seta(d, [283, 246, 283, 116], "HTTPS + JWT (scope=extension)", pos=0.64)
    return d


# ---------------------------------------------------------------- 2. camadas
def camadas():
    W, H = 453, 300
    d = Drawing(W, H)
    caixa(d, 8, 262, 200, 32, "Clientes", ["plataforma web (wwwroot) e extensão MV3"], cab=None, fundo=LINHA_ALT)
    caixa(d, 8, 164, 200, 82, "Zyft.Api  (apresentação)",
          ["Controllers finos: recebem DTO e chamam IMediator", "Middleware global de exceções → ProblemDetails",
           "Autenticação JWT, policies WebUser / ExtensionOrWeb", "Rate limiting, CORS, Swagger, Serilog, /health",
           "Composição da injeção de dependência"], centralizar=False)
    caixa(d, 245, 164, 200, 82, "Zyft.Infrastructure",
          ["ZyftDbContext, configurações Fluent API, Migrations", "EfRepository<T>, ProfileRepository, UnitOfWork",
           "OllamaLlmClient / NullLlmClient", "BCryptPasswordHasher, JwtTokenService",
           "Extração PDF (PdfPig) e DOCX (OpenXml), AES-GCM"], centralizar=False)
    caixa(d, 80, 76, 270, 72, "Zyft.Application  (casos de uso)",
          ["Commands e Queries (MediatR) + Handlers; DTOs", "Validators (FluentValidation); Validation e LoggingBehavior",
           "Portas: IRepository<T>, IUnitOfWork, ILlmClient, IPasswordHasher, ITokenService, ICurrentUser, IClock",
           "Serviços: FieldMapper, JobAnalyzer, AnswerWriter"], centralizar=False)
    caixa(d, 110, 4, 213, 58, "Zyft.Domain  (núcleo)",
          ["Entidades: User, Profile, Experience, Consent…", "Enums, Guard (invariantes) e DomainException",
           "Regras puras: bloqueios de campo, consentimento", "Sem dependência de framework"], centralizar=False)
    seta(d, [108, 262, 108, 246], "HTTP/JSON", pos=0.5, dx=26)
    seta(d, [108, 164, 108, 148], "referencia", dx=26)
    seta(d, [330, 164, 330, 148], "implementa portas", dx=-40)
    seta(d, [216, 76, 216, 62], "referencia", dx=26)
    seta(d, [208, 214, 245, 214], "registra (DI)", pos=0.5, dy=7)
    for i, t in enumerate(["Dependências apontam", "sempre para dentro:", "o Domain não conhece", "nenhuma outra camada."]):
        texto(d, 449, 120 - i * 9, t, 6.6, FI, CINZA_TXT, "end")
    return d


# ---------------------------------------------------------------- 3. pipeline de requisição
def pipeline():
    W, H = 453, 196
    d = Drawing(W, H)
    y1 = 132
    nos = [("Extensão", ["POST /api/v1/", "forms/mappings"]), ("FormsController", ["valida JWT e", "policy"]),
           ("IMediator", ["MapFields", "Command"]), ("Validation", ["Behavior", "(FluentValidation)"]),
           ("Handler", ["carrega perfil,", "consentimentos"])]
    w, gap = 78, 12.75
    for i, (t, l) in enumerate(nos):
        caixa(d, 4 + i * (w + gap), y1, w, 50, t, l)
        if i:
            seta(d, [4 + i * (w + gap) - gap, y1 + 25, 4 + i * (w + gap), y1 + 25])
    grupo(d, 4, 4, 445, 108, "FieldMapper (Application) — um item por campo recebido, nada além")
    etapas = [("1. Bloqueios", ["senha, CAPTCHA e", "anexo: ignorados;", "sensível e termos:", "bloqueados"]),
              ("2. Regras", ["determinísticas", "(PT/EN) → chave", "do perfil"]),
              ("3. Respostas", ["aprovadas para", "perguntas", "parecidas"]),
              ("4. IA aponta", ["só a chave (ILlmClient);", "se fora do ar,", "pula a etapa"]),
              ("5. Validação", ["chave existe, valor", "vem do perfil, opção", "existe no select"])]
    w2, gap2 = 78, 10
    for i, (t, l) in enumerate(etapas):
        x = 12 + i * (w2 + gap2)
        caixa(d, x, 14, w2, 66, t, l, cab=AZUL if i != 3 else LARANJA)
        if i:
            seta(d, [x - gap2, 47, x, 47])
    seta(d, [4 + 4 * (w + gap) + w / 2, y1, 4 + 4 * (w + gap) + w / 2, 122, 51, 122, 51, 80], "FieldMapper.Map(campos, perfil)",
         pos=0.4)
    texto(d, 12, 88, "Cada etapa trata só os campos que as anteriores não resolveram.", 6.4, FI, CINZA_TXT)
    texto(d, 449, 186, "Resposta: {fieldId, profileKey, value, source, state}", 6.6, FI, CINZA_TXT, "end")
    return d


# ---------------------------------------------------------------- 4. DER
DER = {
    "User": ["Id uuid PK", "Email varchar(254) UQ", "PasswordHash varchar(100)", "Name varchar(120)",
             "DeletedAt timestamptz NULL"],
    "RefreshToken": ["Id uuid PK", "UserId uuid FK", "DeviceId uuid FK NULL", "TokenHash varchar(64) UQ",
                     "ExpiresAt timestamptz", "RevokedAt timestamptz NULL", "ReplacedByHash varchar(64) NULL"],
    "ExtensionDevice": ["Id uuid PK", "UserId uuid FK", "Name varchar(80)", "PairedAt timestamptz",
                        "LastSeenAt timestamptz", "RevokedAt timestamptz NULL"],
    "PairingCode": ["Id uuid PK", "UserId uuid FK", "CodeHash varchar(64)", "ExpiresAt timestamptz",
                    "UsedAt timestamptz NULL"],
    "AuthorizedSite": ["Id uuid PK", "UserId uuid FK", "Origin varchar(300)", "UQ (UserId, Origin)"],
    "Consent": ["Id uuid PK", "UserId uuid FK", "Purpose varchar(30) (enum)", "Granted bool", "UQ (UserId, Purpose)"],
    "ApprovedAnswer": ["Id uuid PK", "UserId uuid FK", "Question varchar(500)", "Text varchar(5000)",
                       "ApprovedAt timestamptz", "LastUsedAt timestamptz NULL"],
    "SensitiveData": ["Id uuid PK", "UserId uuid FK", "Kind varchar(30) (enum)", "EncryptedValue varchar(1024)",
                      "UQ (UserId, Kind)"],
    "ResumeImport": ["Id uuid PK", "UserId uuid FK", "FileName varchar(255)", "Status varchar(20) (enum)",
                     "ProposalJson jsonb"],
    "Application": ["Id uuid PK", "UserId uuid FK", "Portal varchar(200)", "JobTitle varchar(300)",
                    "Company varchar(200) NULL", "JobUrl varchar(2000) NULL", "Status varchar(20) (enum)",
                    "FieldsFilled int", "Notes varchar(2000) NULL", "ConfirmedAt timestamptz"],
    "AuditEvent": ["Id uuid PK", "UserId uuid FK", "Action varchar(60)", "Portal varchar(200) NULL",
                   "FieldCount int NULL", "OccurredAt timestamptz"],
    "Profile": ["Id uuid PK", "UserId uuid FK UQ", "FullName varchar(160) NULL", "Email varchar(254) NULL",
                "Phone varchar(40) NULL", "BirthDate date NULL", "City / State / Country NULL", "ZipCode varchar(20) NULL",
                "Linkedin / Github / PortfolioUrl NULL", "Headline varchar(200) NULL", "Summary varchar(4000) NULL",
                "DesiredRoles / Areas varchar(500) NULL", "Seniority varchar(60) NULL", "WorkModel varchar(20) NULL",
                "LocationPreference / Availability NULL", "SalaryExpectation numeric NULL",
                "AvailableToTravel / Relocate NULL", "ContractTypes varchar(200) NULL", "HasDriverLicense bool NULL",
                "DriverLicenseCategory NULL"],
    "Experience": ["Id uuid PK", "ProfileId uuid FK", "Company varchar(200)", "Role varchar(200)", "StartDate date",
                   "EndDate date NULL", "IsCurrent bool", "Activities varchar(4000) NULL", "Tools varchar(1000) NULL",
                   "Results varchar(4000) NULL"],
    "Education": ["Id uuid PK", "ProfileId uuid FK", "Institution varchar(200)", "Course varchar(200)",
                  "Level varchar(30) (enum)", "Status varchar(30) (enum)", "StartDate date", "EndDate date NULL"],
    "Course": ["Id uuid PK", "ProfileId uuid FK", "Type varchar(20) (enum)", "Name varchar(200)",
               "Institution varchar(200) NULL", "Hours int NULL", "CompletedAt date NULL"],
    "Skill": ["Id uuid PK", "ProfileId uuid FK", "Name varchar(100)", "Level varchar(20) NULL", "UQ (ProfileId, Name)"],
    "Language": ["Id uuid PK", "ProfileId uuid FK", "Name varchar(60)", "Proficiency varchar(20) (enum)"],
}


def _entidade(d, x, ytop, w, nome):
    linhas = DER[nome]
    lh, th = 7.9, 12
    h = th + len(linhas) * lh + 4
    y = ytop - h
    d.add(Rect(x, y, w, h, rx=2, ry=2, fillColor=colors.white, strokeColor=AZUL, strokeWidth=0.9))
    d.add(Rect(x, y + h - th, w, th, rx=2, ry=2, fillColor=AZUL, strokeColor=AZUL, strokeWidth=0.9))
    d.add(Rect(x, y + h - th, w, th / 2, fillColor=AZUL, strokeColor=AZUL, strokeWidth=0))
    texto(d, x + w / 2, y + h - th + 3.4, nome, 7.2, FB, colors.white, "middle")
    yy = y + h - th - lh + 1
    for i, ln in enumerate(linhas):
        if i % 2 == 1:
            d.add(Rect(x + 0.5, yy - 2.2, w - 1, lh, fillColor=LINHA_ALT, strokeColor=None))
        font = FB if (" PK" in ln) else F
        cor = AZUL if (" FK" in ln or ln.startswith("UQ")) else PRETO
        texto(d, x + 4, yy, ln, 6.1, font, cor)
        yy -= lh
    return {"x": x, "y": y, "w": w, "h": h, "cy": y + h - th / 2 - 4}


def der():
    W, H = 453, 600
    d = Drawing(W, H)
    wa, wb, wc = 132, 132, 138
    xa, xb, xc = 0, 158, 302
    gap = 11
    pos = {}
    # coluna B: User no topo e filhas de User
    y = H - 2
    for nome in ["User", "ApprovedAnswer", "SensitiveData", "ResumeImport", "Application", "AuditEvent"]:
        pos[nome] = _entidade(d, xb, y, wb, nome)
        y = pos[nome]["y"] - (gap + 14 if nome == "User" else gap)
    # coluna A: autenticação, extensão, consentimento
    y = H - 2
    for nome in ["RefreshToken", "ExtensionDevice", "PairingCode", "AuthorizedSite", "Consent"]:
        pos[nome] = _entidade(d, xa, y, wa, nome)
        y = pos[nome]["y"] - (gap + 16 if nome == "RefreshToken" else gap + 4)
    # coluna C: perfil e seções
    y = H - 2
    for nome in ["Profile", "Experience", "Education", "Course", "Skill", "Language"]:
        pos[nome] = _entidade(d, xc, y, wc, nome)
        y = pos[nome]["y"] - (gap + 2)

    def card(x, y, s, anchor="middle"):
        texto(d, x, y, s, 6.6, FB, LARANJA, anchor)

    # espinha de User (x = 145): filhas das colunas A e B
    sx = (xa + wa + xb) / 2
    u = pos["User"]
    filhos_a = ["RefreshToken", "ExtensionDevice", "PairingCode", "AuthorizedSite", "Consent"]
    filhos_b = ["ApprovedAnswer", "SensitiveData", "ResumeImport", "Application", "AuditEvent"]
    ys = [pos[n]["cy"] for n in filhos_a + filhos_b] + [u["cy"]]
    d.add(Line(sx, max(ys), sx, min(ys), strokeColor=PRETO, strokeWidth=0.9))
    d.add(Line(sx, u["cy"], xb, u["cy"], strokeColor=PRETO, strokeWidth=0.9))
    card(xb - 4, u["cy"] + 2, "1", "end")
    for n in filhos_a:
        e = pos[n]
        d.add(Line(sx, e["cy"], xa + wa, e["cy"], strokeColor=PRETO, strokeWidth=0.9))
        card(xa + wa + 4, e["cy"] + 2, "N", "start")
    for n in filhos_b:
        e = pos[n]
        d.add(Line(sx, e["cy"], xb, e["cy"], strokeColor=PRETO, strokeWidth=0.9))
        card(xb - 4, e["cy"] + 2, "N", "end")
    # ExtensionDevice 1:N RefreshToken (DeviceId opcional)
    rt, ed = pos["RefreshToken"], pos["ExtensionDevice"]
    lx = xa + 8
    d.add(Line(lx, ed["y"] + ed["h"], lx, rt["y"], strokeColor=PRETO, strokeWidth=0.9, strokeDashArray=[3, 2]))
    card(lx + 4, rt["y"] - 8, "N", "start")
    card(lx + 4, ed["y"] + ed["h"] + 3, "0..1", "start")
    texto(d, lx + 26, (rt["y"] + ed["y"] + ed["h"]) / 2 - 2, "DeviceId (opcional)", 6, FI, CINZA_TXT)
    # User 1:1 Profile
    p = pos["Profile"]
    d.add(Line(xb + wb, u["cy"], xc, u["cy"], strokeColor=PRETO, strokeWidth=0.9))
    card(xb + wb + 4, u["cy"] + 2, "1", "start")
    card(xc - 4, u["cy"] + 2, "1", "end")
    # espinha de Profile à direita (x = 449)
    px = xc + wc + 10
    filhos_c = ["Experience", "Education", "Course", "Skill", "Language"]
    d.add(Line(xc + wc, p["cy"], px, p["cy"], strokeColor=PRETO, strokeWidth=0.9))
    d.add(Line(px, p["cy"], px, pos["Language"]["cy"], strokeColor=PRETO, strokeWidth=0.9))
    card(xc + wc + 2, p["cy"] + 2, "1", "start")
    for n in filhos_c:
        e = pos[n]
        d.add(Line(px, e["cy"], xc + wc, e["cy"], strokeColor=PRETO, strokeWidth=0.9))
        card(xc + wc + 2, e["cy"] + 2, "N", "start")
    # legenda (coluna da esquerda, abaixo de Consent)
    linhas_leg = ["PK: chave primária (negrito)", "FK: chave estrangeira (azul)", "UQ: único",
                  "1 / N: cardinalidade", "Tracejado: relação opcional", "Todas as tabelas têm CreatedAt",
                  "e UpdatedAt (timestamptz).", "ON DELETE CASCADE a partir", "de User e de Profile."]
    ly = 150
    texto(d, xa, ly + 10, "Legenda", 6.8, FB, AZUL)
    for k, t in enumerate(linhas_leg):
        texto(d, xa, ly - 8.5 * k, t, 6.0, F, PRETO)
    return d


# ---------------------------------------------------------------- 5. mapa de navegação (web)
def navegacao():
    W, H = 453, 318
    d = Drawing(W, H)
    caixa(d, 40, 280, 110, 32, "Entrar", ["#/entrar"])
    caixa(d, 300, 280, 110, 32, "Criar conta", ["#/cadastrar"])
    seta(d, [150, 296, 300, 296], "links entre as telas", dupla=True)
    caixa(d, 300, 220, 110, 40, "Boas-vindas", ["#/boas-vindas", "escolhe consentimentos"], cab=LARANJA)
    seta(d, [355, 280, 355, 260], "1º acesso", dx=24)
    grupo(d, 4, 2, 445, 204, "Área logada")
    caixa(d, 130, 148, 192, 38, "Barra de navegação (fixa)",
          ["Perfil · Importar · Respostas · Extensão", "Candidaturas · Registro · Conta · Sair"], fs=6.2)
    seta(d, [95, 280, 95, 167, 130, 167], "login ok", pos=0.4, dx=-20)
    seta(d, [355, 220, 355, 167, 322, 167], "continuar", pos=0.35, dx=24)
    telas = [("Perfil Mestre", ["#/perfil", "seções e", "o que falta"]),
             ("Importar", ["#/importar", "envio do", "currículo"]),
             ("Respostas", ["#/respostas", "respostas", "aprovadas"]),
             ("Extensão", ["#/extensao", "código, aparelhos", "e sites"]),
             ("Candidaturas", ["#/candidaturas", "registro", "mínimo"]),
             ("Registro", ["#/registro", "ações sem", "valores"]),
             ("Conta", ["#/conta", "consentimentos,", "sensíveis, LGPD"])]
    w, gap = 56, 6.8
    xs = [10 + i * (w + gap) for i in range(7)]
    for x, (t, l) in zip(xs, telas):
        caixa(d, x, 74, w, 54, t, l, fs=5.8, fs_titulo=6.4)
        d.add(Line(x + w / 2, 138, x + w / 2, 128, strokeColor=PRETO, strokeWidth=0.8))
        ponta(d, x + w / 2, 138, x + w / 2, 128)
    d.add(Line(xs[0] + w / 2, 138, xs[6] + w / 2, 138, strokeColor=PRETO, strokeWidth=0.8))
    d.add(Line(226, 148, 226, 138, strokeColor=PRETO, strokeWidth=0.8))
    sub = [(0, "O que falta", ["perguntas só do", "que o portal pede"], AZUL),
           (1, "Revisão", ["aceitar, editar", "ou descartar"], AZUL),
           (4, "Detalhe", ["#/candidaturas/{id}", "nota e status"], AZUL),
           (6, "Excluir conta", ["digitar EXCLUIR;", "volta a Entrar"], VERMELHO)]
    for i, t, l, cor in sub:
        caixa(d, xs[i], 18, w, 42, t, l, fs=5.6, fs_titulo=6.4, cab=cor)
        seta(d, [xs[i] + w / 2, 74, xs[i] + w / 2, 60])
    seta(d, [xs[1] + w / 2, 18, xs[1] + w / 2, 10, xs[0] + w / 2, 10, xs[0] + w / 2, 18], "aplicar", pos=0.5)
    return d


# ---------------------------------------------------------------- 6. estados do painel lateral
def estados_painel():
    W, H = 453, 206
    d = Drawing(W, H)
    w, gap = 70, 24.5
    X = [4 + i * (w + gap) for i in range(5)]
    Y1, Y2, Y3, h = 158, 84, 10, 40

    def est(i, y, nome, desc, cor=AZUL):
        caixa(d, X[i], y, w, h, nome, desc, cab=cor, fs=5.8, fs_titulo=6.6)

    est(0, Y1, "Não pareado", ["“Conectar à", "sua conta”"], VERMELHO)
    est(1, Y1, "Ocioso", ["ações da", "tela atual"])
    est(2, Y1, "Lendo", ["“Lendo 42", "campos…”"])
    est(3, Y1, "Revisão", ["campos com", "estado e origem"])
    est(4, Y1, "Preenchendo", ["progresso", "por campo"])
    est(0, Y2, "Analisando vaga", ["requisitos e", "evidências"])
    est(1, Y2, "Não suportada", ["página fora da", "lista: autorizar?"], LARANJA)
    est(2, Y2, "Erro", ["diz o que fazer"], VERMELHO)
    est(3, Y2, "Concluído", ["confirmação;", "volta a Ocioso"], VERDE)
    est(4, Y2, "Resumo", ["preenchidos e", "pendentes"])
    est(0, Y3, "Resultado da vaga", ["sem nota nem %;", "volta a Ocioso"])
    m = Y1 + h / 2
    seta(d, [X[0] + w, m, X[1], m], "código", dy=8)
    seta(d, [X[1] + w, m, X[2], m], "Ler", dy=8)
    seta(d, [X[2] + w, m, X[3], m], "dados", dy=8)
    seta(d, [X[3] + w, m, X[4], m], "Preencher", dy=8)
    seta(d, [X[4] + w / 2, Y1, X[4] + w / 2, Y2 + h])
    seta(d, [X[4], Y2 + h / 2, X[3] + w, Y2 + h / 2], "pessoa\nenvia", dy=-12)
    seta(d, [X[4] + 6, Y2 + h, X[3] + w - 10, Y1], "ler de novo", pos=0.5, dx=-8)
    seta(d, [X[2] + w / 2, Y1, X[2] + w / 2, Y2 + h], "falha", dx=12)
    seta(d, [X[2] + 4, Y2 + h, X[1] + w - 4, Y1], "tentar\nde novo", pos=0.5, dx=4, tracejada=True)
    seta(d, [X[1] + 20, Y1, X[1] + 20, Y2 + h], "site fora\nda lista", dx=-2)
    seta(d, [X[1] + 52, Y2 + h, X[1] + 52, Y1])
    seta(d, [X[1] + 4, Y1, X[0] + w - 6, Y2 + h], "Analisar\nvaga", pos=0.5, dx=-4)
    seta(d, [X[0] + w / 2, Y2, X[0] + w / 2, Y3 + h])
    texto(d, 449, 40, "Nenhum estado leva a “enviar”:", 6.6, FI, CINZA_TXT, "end")
    texto(d, 449, 31, "o envio é sempre um clique", 6.6, FI, CINZA_TXT, "end")
    texto(d, 449, 22, "da pessoa no portal.", 6.6, FI, CINZA_TXT, "end")
    return d


DIAGRAMAS = {
    "img/01-componentes.png": componentes,
    "img/02-camadas.png": camadas,
    "img/03-pipeline-mapeamento.png": pipeline,
    "img/04-der.png": der,
    "img/05-navegacao-web.png": navegacao,
    "img/06-estados-painel.png": estados_painel,
}


def gerar_pngs(base_dir):
    for rel, fn in DIAGRAMAS.items():
        caminho = os.path.join(base_dir, rel)
        os.makedirs(os.path.dirname(caminho), exist_ok=True)
        renderPM.drawToFile(fn(), caminho, fmt="PNG", dpi=200)
        print(caminho)


if __name__ == "__main__":
    raiz = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    gerar_pngs(os.path.join(raiz, "docs", "entregas"))
