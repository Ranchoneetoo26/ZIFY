"""Aplica a pesquisa v2 (questionario-v2.json) no Google Forms usando o navegador da própria pessoa.

Fluxo:
  1. Conecta num Chromium já aberto e logado no Google (porta CDP, padrão 9333).
  2. Cria um projeto no Apps Script, cola migracao-v2.gs com a especificação embutida e salva.
  3. Executa a função pedida (inspecionar ou migrarParaV2) e imprime o registro de execução.

A autorização do Apps Script (tela "Permitir") é sempre feita pela pessoa, na janela do navegador.

Uso:
  python aplicar_pesquisa_v2.py inspecionar
  python aplicar_pesquisa_v2.py migrarParaV2
  python aplicar_pesquisa_v2.py migrarParaV2 --projeto https://script.google.com/home/projects/<id>/edit
"""
import argparse
import json
import re
import sys
import time
from pathlib import Path

from playwright.sync_api import sync_playwright

AQUI = Path(__file__).resolve().parent
NOME_PROJETO = "Zyft — migração da pesquisa v2"


def codigo_gs() -> str:
    spec = json.loads((AQUI / "questionario-v2.json").read_text(encoding="utf-8"))
    gs = (AQUI / "migracao-v2.gs").read_text(encoding="utf-8")
    return gs.replace("/*SPEC*/ null", json.dumps(spec, ensure_ascii=False, indent=2))


def colar_codigo(page, codigo: str) -> None:
    editor = page.locator(".monaco-editor").first
    editor.wait_for(timeout=60_000)
    editor.click()
    page.evaluate("t => navigator.clipboard.writeText(t)", codigo)
    page.keyboard.press("Control+A")
    page.keyboard.press("Control+V")
    page.wait_for_timeout(1500)
    page.keyboard.press("Control+S")
    page.wait_for_timeout(3000)


def escolher_funcao(page, nome: str) -> None:
    page.keyboard.press("Escape")
    page.locator("[aria-label='Selecione a função para executar'], [aria-label='Select function to run']").first.click()
    page.wait_for_timeout(800)
    page.locator("[role=menuitem], [role=option]").filter(has_text=re.compile(rf"^\s*{nome}\s*$")).first.click()
    page.wait_for_timeout(800)


def executar(page, nome: str, limite_s: int = 600) -> str:
    page.locator("[aria-label='Executar a função selecionada'], [aria-label='Run the selected function']").first.click()
    inicio = time.time()
    avisou = False
    while time.time() - inicio < limite_s:
        page.wait_for_timeout(2000)
        texto = page.locator("body").inner_text()
        if re.search(r"Autorização necessária|Authorization required", texto) and not avisou:
            print(">> Autorize o script na janela do navegador (Revisar permissões → conta → Avançado → Acessar → Permitir).", flush=True)
            avisou = True
        if re.search(r"Execução concluída|Execution completed", texto):
            break
        if re.search(r"Erro\s|Error\s|Exception", texto) and re.search(r"Notice|Aviso|Info|Informações", texto):
            if re.search(r"Execução iniciada|Execution started", texto):
                pass
    return page.locator("body").inner_text()


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("funcao", choices=["inspecionar", "arquivarRespostasV1", "migrarParaV2"])
    ap.add_argument("--cdp", default="http://localhost:9333")
    ap.add_argument("--projeto", help="URL de um projeto do Apps Script já criado por este script")
    ap.add_argument("--print", dest="screenshot", help="salva uma captura de tela ao final")
    args = ap.parse_args()

    with sync_playwright() as p:
        navegador = p.chromium.connect_over_cdp(args.cdp)
        ctx = navegador.contexts[0]
        ctx.grant_permissions(["clipboard-read", "clipboard-write"], origin="https://script.google.com")
        page = ctx.new_page()
        if args.projeto:
            page.goto(args.projeto)
        else:
            page.goto("https://script.google.com/create")
            page.wait_for_url(re.compile(r"/projects/.+/edit"), timeout=120_000)
            print("PROJETO", page.url, flush=True)
        page.wait_for_timeout(4000)
        colar_codigo(page, codigo_gs())
        escolher_funcao(page, args.funcao)
        texto = executar(page, args.funcao)
        if args.screenshot:
            page.screenshot(path=args.screenshot, full_page=True)
        registro = texto[texto.find("Registro de execução") if "Registro de execução" in texto else max(0, len(texto) - 6000):]
        print(registro[-6000:])
    return 0


if __name__ == "__main__":
    sys.exit(main())
