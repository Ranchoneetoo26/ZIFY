/**
 * Migra o formulário da pesquisa do marketplace (v1) para a pesquisa da Candidatura Assistida (v2).
 *
 * A especificação vem de questionario-v2.json e é injetada em SPEC por aplicar_pesquisa_v2.py.
 * O script é idempotente: pode rodar de novo sem duplicar itens.
 *
 *   arquivarRespostasV1() → copia a aba de respostas atual para "Arquivo v1 — marketplace" (só valores)
 *   migrarParaV2()        → arquiva (se ainda não arquivou) e reescreve o formulário conforme SPEC
 *   inspecionar()         → lista os itens do formulário e as abas da planilha
 */

const SPEC = /*SPEC*/ null;

const TIPOS = {
  secao_inicial: FormApp.ItemType.SECTION_HEADER,
  texto: FormApp.ItemType.SECTION_HEADER,
  secao: FormApp.ItemType.PAGE_BREAK,
  escolha: FormApp.ItemType.MULTIPLE_CHOICE,
  caixas: FormApp.ItemType.CHECKBOX,
  escala: FormApp.ItemType.SCALE,
  grade: FormApp.ItemType.GRID,
  paragrafo: FormApp.ItemType.PARAGRAPH_TEXT,
  curta: FormApp.ItemType.TEXT,
};

const NOME_ARQUIVO = 'Arquivo v1 — marketplace';

function inspecionar() {
  const form = FormApp.openById(SPEC.formId);
  const saida = { titulo: form.getTitle(), respostas: form.getResponses().length, itens: [], abas: [] };
  form.getItems().forEach((it) => saida.itens.push([it.getId(), String(it.getType()), it.getTitle()]));
  const ss = SpreadsheetApp.openById(SPEC.planilhaId);
  ss.getSheets().forEach((sh) => saida.abas.push([sh.getName(), sh.getLastRow(), sh.getLastColumn(), sh.getFormUrl() || '']));
  Logger.log(JSON.stringify(saida));
  return saida;
}

function abaDoFormulario_(ss, form) {
  const id = form.getId();
  return ss.getSheets().find((sh) => (sh.getFormUrl() || '').indexOf(id) >= 0) || null;
}

function arquivarRespostasV1() {
  const form = FormApp.openById(SPEC.formId);
  const ss = SpreadsheetApp.openById(SPEC.planilhaId);
  if (ss.getSheetByName(NOME_ARQUIVO)) {
    Logger.log('Arquivo já existe: ' + NOME_ARQUIVO);
    return;
  }
  const origem = abaDoFormulario_(ss, form) || ss.getSheets()[0];
  const destino = ss.insertSheet(NOME_ARQUIVO);
  const linhas = origem.getLastRow(), colunas = origem.getLastColumn();
  if (linhas && colunas) {
    destino.getRange(1, 1, linhas, colunas).setValues(origem.getRange(1, 1, linhas, colunas).getValues());
    destino.setFrozenRows(1);
  }
  destino.getRange(1, colunas + 2).setValue('Cópia das respostas da pesquisa v1 (marketplace de TI), feita antes da migração para a v2 em ' +
    Utilities.formatDate(new Date(), Session.getScriptTimeZone(), 'dd/MM/yyyy HH:mm'));
  Logger.log('Arquivadas ' + Math.max(0, linhas - 1) + ' respostas em "' + NOME_ARQUIVO + '" (origem: ' + origem.getName() + ')');
}

function migrarParaV2() {
  arquivarRespostasV1();
  const form = FormApp.openById(SPEC.formId);
  form.setTitle(SPEC.titulo).setDescription(SPEC.descricao).setConfirmationMessage(SPEC.confirmacao).setProgressBar(true);

  // Remove os itens do marketplace.
  SPEC.removidos.forEach((r) => {
    const it = form.getItemById(r.id);
    if (it) form.deleteItem(it);
  });

  // Cria ou atualiza cada item e o coloca na posição certa.
  const criados = {};
  SPEC.itens.forEach((e, posicao) => {
    let item = e.de ? form.getItemById(e.de) : acharPorTitulo_(form, e);
    if (item && item.getType() !== TIPOS[e.tipo]) {
      form.deleteItem(item);
      item = null;
    }
    if (!item) item = criar_(form, e.tipo);
    aplicar_(item, e);
    criados[e.chave] = item.getId();
    if (item.getIndex() !== posicao) form.moveItem(item.getIndex(), posicao);
  });

  // Qualquer item que sobrar no fim e não esteja na especificação é removido.
  const validos = new Set(Object.values(criados).map(String));
  form.getItems().forEach((it) => { if (!validos.has(String(it.getId()))) form.deleteItem(it); });

  PropertiesService.getScriptProperties().setProperties({ FORM_ID: SPEC.formId, ITENS_V2: JSON.stringify(criados) });
  Logger.log('RESULTADO ' + JSON.stringify({ itens: form.getItems().length, ids: criados, publicado: form.getPublishedUrl() }));
}

function acharPorTitulo_(form, e) {
  return form.getItems(TIPOS[e.tipo]).find((it) => it.getTitle() === e.titulo) || null;
}

function criar_(form, tipo) {
  switch (tipo) {
    case 'secao_inicial':
    case 'texto': return form.addSectionHeaderItem();
    case 'secao': return form.addPageBreakItem();
    case 'escolha': return form.addMultipleChoiceItem();
    case 'caixas': return form.addCheckboxItem();
    case 'escala': return form.addScaleItem();
    case 'grade': return form.addGridItem();
    case 'paragrafo': return form.addParagraphTextItem();
    case 'curta': return form.addTextItem();
  }
  throw new Error('Tipo desconhecido: ' + tipo);
}

function aplicar_(item, e) {
  item.setTitle(e.titulo);
  item.setHelpText(e.ajuda || '');
  switch (e.tipo) {
    case 'secao_inicial':
    case 'texto':
      return;
    case 'secao':
      return;
    case 'escolha': {
      const x = item.asMultipleChoiceItem();
      x.setChoiceValues(e.opcoes).showOtherOption(!!e.outro).setRequired(!!e.obrigatoria);
      return;
    }
    case 'caixas': {
      const x = item.asCheckboxItem();
      x.setChoiceValues(e.opcoes).showOtherOption(!!e.outro).setRequired(!!e.obrigatoria);
      if (/\(até 3\)/.test(e.titulo)) {
        x.setValidation(FormApp.createCheckboxValidation().requireSelectAtMost(3).build());
      } else {
        x.clearValidation();
      }
      return;
    }
    case 'escala': {
      const x = item.asScaleItem();
      x.setBounds(e.min, e.max).setLabels(e.rotuloMin, e.rotuloMax).setRequired(!!e.obrigatoria);
      return;
    }
    case 'grade': {
      const x = item.asGridItem();
      x.setRows(e.linhas).setColumns(e.colunas).setRequired(!!e.obrigatoria);
      return;
    }
    case 'paragrafo':
      item.asParagraphTextItem().setRequired(!!e.obrigatoria);
      return;
    case 'curta':
      item.asTextItem().setRequired(!!e.obrigatoria);
      return;
  }
}
