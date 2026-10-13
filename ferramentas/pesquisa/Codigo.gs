/**
 * Relatório da pesquisa v2 — persona, dores e disposição — Google Forms + Google Docs
 *
 * O formulário é reescrito por migracao-v2.gs (via aplicar_pesquisa_v2.py) a partir de questionario-v2.json.
 *
 * 1. vincularFormulario()        → encontra as perguntas do formulário pelo título
 * 2. gerarPlano()                → lê as respostas e escreve o relatório num Google Docs
 *
 * Extras:
 *   ativarAtualizacaoAutomatica() → refaz o relatório a cada nova resposta
 *   limparRespostas()             → apaga as respostas do formulário (irreversível)
 *   resetarConfiguracao()         → esquece o vínculo e o relatório atuais (não apaga arquivos)
 *
 * Tudo o que é específico da pesquisa fica em CONFIG. Critérios em docs/05-pesquisa/plano-de-pesquisa.md.
 */

const CONFIG = {
  produto: 'Zyft — Candidatura Assistida',

  // Perfil de quem respondeu. Os títulos precisam ser iguais aos do formulário (questionario-v2.json).
  persona: [
    { chave: 'idade', rotulo: 'Faixa de idade', titulo: 'Qual a sua idade?' },
    { chave: 'momento', rotulo: 'Momento profissional', titulo: 'Qual frase descreve melhor o seu momento profissional?' },
    { chave: 'area', rotulo: 'Área', titulo: 'Em qual área você busca (ou buscaria) vagas?' },
    { chave: 'experiencia', rotulo: 'Experiência', titulo: 'Quanto tempo de experiência profissional você tem, somando todas as áreas?' },
    { chave: 'escolaridade', rotulo: 'Escolaridade', titulo: 'Qual a sua escolaridade?' },
    { chave: 'cidade', rotulo: 'Onde mora', titulo: 'Onde você mora?' },
    { chave: 'familiaridade', rotulo: 'Familiaridade digital (1–5)', titulo: 'Quanto você se sente à vontade preenchendo cadastros e formulários pela internet?' },
    { chave: 'volume', rotulo: 'Candidaturas por semana', titulo: 'Quantas candidaturas você faz por semana, em média?' },
    { chave: 'tempo', rotulo: 'Tempo por candidatura', titulo: 'Quanto tempo leva, em média, cada candidatura (do clique em "candidatar" até o envio)?' },
    { chave: 'portais', rotulo: 'Onde se candidata', titulo: 'Onde você se candidata a vagas hoje?' },
    { chave: 'etapas', rotulo: 'O que mais toma tempo', titulo: 'Numa candidatura típica, o que mais toma o seu tempo? (até 3)' },
    { chave: 'receios', rotulo: 'Receios de preenchimento', titulo: 'Quais destas informações você teria receio de deixar um assistente preencher, mesmo revisando antes?' },
  ],

  // Grade de dores (1 a 5), na mesma ordem das linhas do formulário.
  // hipotese: true = dor que o MVP 1 aposta resolver (H1–H5). As duas últimas são controles.
  tituloDores: 'O quanto cada situação te incomoda hoje?',
  dores: [
    { chave: 'repetir', texto: 'Preencher os mesmos dados em cada site', hipotese: true },
    { chave: 'consistencia', texto: 'Manter meu currículo e meus perfis atualizados e iguais em todos os sites', hipotese: true },
    { chave: 'perguntas', texto: 'Responder perguntas abertas', hipotese: true },
    { chave: 'adequacao', texto: 'Saber se eu tenho o perfil que a vaga pede', hipotese: true },
    { chave: 'entender', texto: 'Entender o que o formulário está pedindo', hipotese: true },
    { chave: 'revisar', texto: 'Revisar tudo antes de enviar sem deixar passar erro', hipotese: true },
    { chave: 'controle', texto: 'Lembrar onde já me candidatei', hipotese: false },
    { chave: 'retorno', texto: 'Não ter retorno das empresas', hipotese: false },
  ],

  // Uma dor é "validada" quando atinge os dois critérios, com um mínimo de respostas.
  criterio: { media: 3.5, top2: 0.5, minimoRespostas: 15 },

  // Personas de necessidade (docs/01-produto/personas.md). A pessoa escolhe um perfil no formulário.
  tituloPersona: 'Qual perfil mais se parece com você?',
  tituloIdentificacao: 'O quanto você se identifica com o perfil escolhido?',
  arquetipos: {
    pn1: { opcao: 'Perfil A', nome: 'PN1 — Quer agilidade', descricao: 'Quer reaproveitar informações e preencher com menos digitação.',
      mensagem: 'Fale de tempo e controle: "preencha seus dados uma vez e revise cada candidatura em minutos".' },
    pn2: { opcao: 'Perfil B', nome: 'PN2 — Quer organizar', descricao: 'Quer as informações certas, consistentes e revisadas num lugar só.',
      mensagem: 'Fale de organização: "um perfil profissional só, sempre atualizado, para usar em qualquer site".' },
    pn3: { opcao: 'Perfil C', nome: 'PN3 — Precisa de ajuda para escrever', descricao: 'Trava nas perguntas abertas e quer sugestões baseadas no que realmente fez.',
      mensagem: 'Fale de apoio: "a gente sugere o texto com o que você já fez, e você revisa tudo antes de usar".' },
    pn4: { opcao: 'Perfil D', nome: 'PN4 — Quer saber se tem o perfil', descricao: 'Quer entender o que a vaga pede e o que no seu histórico atende.',
      mensagem: 'Fale de clareza, sem promessa: "veja o que a vaga pede e o que no seu histórico comprova cada ponto".' },
    pn5: { opcao: 'Perfil E', nome: 'PN5 — Precisa de orientação', descricao: 'Acha os formulários confusos e quer entender cada campo e revisar com calma.',
      mensagem: 'Fale de tranquilidade: "entenda cada campo, saiba o que falta e confira tudo antes de enviar".' },
  },

  // Disposição: o quanto a pessoa quer o produto.
  disposicao: [
    { chave: 'confianca', rotulo: 'Confiança no assistente (1–5)', titulo: 'Quanto você confiaria num assistente que preenche formulários de vaga com as suas informações, se nada fosse enviado sem a sua confirmação?' },
    { chave: 'interesse', rotulo: 'Interesse (1–5)', titulo: 'Qual o seu interesse em usar um assistente assim?' },
    { chave: 'modelo', rotulo: 'Modelo de uso preferido', titulo: 'Como você preferiria usar uma ferramenta assim?' },
    { chave: 'pagaria', rotulo: 'Pagaria por mês', titulo: 'Quanto você pagaria por mês por uma ferramenta assim?' },
    { chave: 'barreiras', rotulo: 'O que impediria o uso', titulo: 'O que te impediria de usar?' },
  ],
};

const ITENS_TEXTO = {
  relato: 'Conte a última vez que se candidatar a uma vaga foi cansativo ou frustrante. O que aconteceu?',
  maiorDor: 'O que faria você usar o Zyft em vez de se candidatar do jeito que faz hoje?',
  personaFalta: 'O que no perfil escolhido NÃO combina com você, ou o que está faltando?',
  contato: 'Quer testar o Zyft antes do lançamento? Deixe seu e-mail ou WhatsApp (opcional)',
};

// ───────────────────────────── Formulário ─────────────────────────────

/**
 * Vincula este script ao formulário da pesquisa v2 (criado/reescrito por migracao-v2.gs),
 * encontrando cada pergunta pelo título. Rode uma vez antes de gerarPlano().
 */
function vincularFormulario(formId) {
  formId = formId || '17LPUbHowLRCNV3XTBuAHh5NBfvyKbkFNCMUHuNQUoAY';
  const form = FormApp.openById(formId);
  const porTitulo = {};
  form.getItems().forEach((it) => { porTitulo[it.getTitle()] = it.getId(); });

  const ids = {};
  const faltando = [];
  const achar = (chave, titulo) => { if (porTitulo[titulo]) ids[chave] = porTitulo[titulo]; else faltando.push(titulo); };
  CONFIG.persona.concat(CONFIG.disposicao).forEach((p) => achar(p.chave, p.titulo));
  achar('dores', CONFIG.tituloDores);
  achar('persona', CONFIG.tituloPersona);
  achar('identificacao', CONFIG.tituloIdentificacao);
  Object.keys(ITENS_TEXTO).forEach((k) => achar(k, ITENS_TEXTO[k]));

  if (faltando.length) throw new Error('Perguntas não encontradas no formulário: ' + faltando.join(' | '));
  PropertiesService.getScriptProperties().setProperties({ FORM_ID: formId, ITENS: JSON.stringify(ids) });
  Logger.log('Vinculado a ' + form.getTitle() + ' (' + Object.keys(ids).length + ' perguntas).');
}

function abrirFormulario_() {
  const id = PropertiesService.getScriptProperties().getProperty('FORM_ID');
  if (!id) throw new Error('Nenhum formulário vinculado. Rode vincularFormulario() primeiro.');
  return FormApp.openById(id);
}

function idsDosItens_() {
  return JSON.parse(PropertiesService.getScriptProperties().getProperty('ITENS'));
}

function lerRespostas_() {
  const ids = idsDosItens_();
  const chavePorId = {};
  Object.keys(ids).forEach((k) => { chavePorId[ids[k]] = k; });

  return abrirFormulario_().getResponses().map((resposta) => {
    const linha = {};
    resposta.getItemResponses().forEach((ir) => {
      const chave = chavePorId[ir.getItem().getId()];
      if (chave) linha[chave] = ir.getResponse();
    });
    return linha;
  });
}

// ───────────────────────────── Análise ─────────────────────────────

function arquetipoDe_(r) {
  const escolha = r.persona;
  return Object.keys(CONFIG.arquetipos).find((k) => CONFIG.arquetipos[k].opcao === escolha) || null;
}

function notasDeDor_(r) {
  const notas = {};
  const grade = r.dores || [];
  CONFIG.dores.forEach((d, i) => {
    const v = Number(grade[i]);
    if (v) notas[d.chave] = v;
  });
  return notas;
}

/** [[valor, quantidade], ...] do mais comum para o menos comum. */
function contar_(valores) {
  const c = {};
  valores.flat().filter((v) => v !== null && v !== undefined && v !== '').forEach((v) => { c[v] = (c[v] || 0) + 1; });
  return Object.keys(c).map((k) => [k, c[k]]).sort((a, b) => b[1] - a[1]);
}

function perfilDe_(grupo, perguntas) {
  return perguntas.map((p) => {
    const contagem = contar_(grupo.map((r) => r[p.chave]));
    const top = contagem[0] || ['—', 0];
    return { chave: p.chave, rotulo: p.rotulo, valor: top[0], pct: grupo.length ? top[1] / grupo.length : 0, contagem: contagem };
  });
}

function rankingDores_(grupo) {
  const minimo = CONFIG.criterio.minimoRespostas;
  return CONFIG.dores.map((d) => {
    const notas = grupo.map((r) => notasDeDor_(r)[d.chave]).filter(Boolean);
    const media = notas.length ? notas.reduce((s, v) => s + v, 0) / notas.length : 0;
    const top2 = notas.length ? notas.filter((v) => v >= 4).length / notas.length : 0;
    const forte = media >= CONFIG.criterio.media && top2 >= CONFIG.criterio.top2;
    const status = notas.length < minimo ? 'poucos dados' : forte ? 'validada' : 'não validada';
    return { chave: d.chave, texto: d.texto, hipotese: d.hipotese, n: notas.length, media: media, top2: top2, status: status };
  }).sort((a, b) => b.media - a.media);
}

function analisar_(respostas) {
  const comArquetipo = respostas.map((r) => Object.assign({ _arq: arquetipoDe_(r) }, r));

  const arquetipos = Object.keys(CONFIG.arquetipos).map((k) => {
    const grupo = comArquetipo.filter((r) => r._arq === k);
    return {
      chave: k,
      nome: CONFIG.arquetipos[k].nome,
      descricao: CONFIG.arquetipos[k].descricao,
      mensagem: CONFIG.arquetipos[k].mensagem,
      n: grupo.length,
      pct: respostas.length ? grupo.length / respostas.length : 0,
      perfil: perfilDe_(grupo, CONFIG.persona),
      dores: rankingDores_(grupo).slice(0, 2),
    };
  }).sort((a, b) => b.n - a.n);

  const textos = (chave) => respostas.map((r) => String(r[chave] || '').trim()).filter((t) => t.length > 0);

  return {
    n: respostas.length,
    persona: perfilDe_(respostas, CONFIG.persona),
    arquetipos: arquetipos,
    dores: rankingDores_(respostas),
    disposicao: perfilDe_(respostas, CONFIG.disposicao),
    relatos: textos('relato').slice(-8),
    maioresDores: textos('maiorDor').slice(-8),
  };
}

// ───────────────────────────── Plano (Google Docs) ─────────────────────────────

function gerarPlano() {
  const respostas = lerRespostas_();
  if (!respostas.length) throw new Error('O formulário ainda não tem respostas.');
  const a = analisar_(respostas);

  const doc = abrirOuCriarPlano_();
  const b = doc.getBody();
  b.clear();

  const pct = (x) => Math.round(x * 100) + '%';
  const agora = Utilities.formatDate(new Date(), Session.getScriptTimeZone(), 'dd/MM/yyyy HH:mm');
  const principal = a.arquetipos[0];
  const maiorDor = a.dores[0];
  const hipoteses = a.dores.filter((d) => d.hipotese);
  const validadas = hipoteses.filter((d) => d.status === 'validada');
  const poucos = a.n < CONFIG.criterio.minimoRespostas;

  titulo_(b, 'Relatório da pesquisa v2 — ' + CONFIG.produto, DocumentApp.ParagraphHeading.TITLE);
  b.appendParagraph('Gerado em ' + agora + ' com ' + a.n + ' respostas.').editAsText().setItalic(true);
  if (poucos) {
    b.appendParagraph('Atenção: com menos de ' + CONFIG.criterio.minimoRespostas +
      ' respostas, trate tudo abaixo como pista, não como conclusão.').editAsText().setBold(true);
  }

  titulo_(b, 'Resumo', DocumentApp.ParagraphHeading.HEADING1);
  item_(b, 'Arquétipo mais comum: ' + principal.nome + ' (' + pct(principal.pct) + ') — ' + principal.descricao);
  item_(b, 'Persona típica (' + principal.nome + '): ' + principal.perfil.filter((p) => !['portais', 'etapas', 'receios'].includes(p.chave)).map((p) => p.valor).join(' · '));
  item_(b, 'Maior dor: "' + maiorDor.texto + '" (média ' + maiorDor.media.toFixed(1) + ', ' +
    pct(maiorDor.top2) + ' deram nota 4 ou 5).');
  item_(b, 'Hipóteses validadas: ' + validadas.length + ' de ' + hipoteses.length + '.');

  titulo_(b, 'Persona', DocumentApp.ParagraphHeading.HEADING1);
  b.appendParagraph('A resposta mais comum em cada pergunta, entre todas as pessoas.');
  tabela_(b, [['Característica', 'Mais comum', '% das pessoas']]
    .concat(a.persona.map((p) => [p.rotulo, p.valor, pct(p.pct)])));

  titulo_(b, 'Arquétipos', DocumentApp.ParagraphHeading.HEADING1);
  tabela_(b, [['Arquétipo', 'Como age', 'Pessoas', '%']]
    .concat(a.arquetipos.map((x) => [x.nome, x.descricao, String(x.n), pct(x.pct)])));
  a.arquetipos.filter((x) => x.n > 0).forEach((x) => {
    titulo_(b, x.nome + ' — ' + x.n + ' pessoa(s)', DocumentApp.ParagraphHeading.HEADING2);
    item_(b, 'Quem é: ' + x.perfil.filter((p) => !['portais', 'etapas', 'receios'].includes(p.chave)).map((p) => p.valor).join(' · '));
    item_(b, 'O que mais incomoda: ' + x.dores.map((d) => d.texto + ' (' + d.media.toFixed(1) + ')').join('; '));
    item_(b, 'Como falar com esse grupo: ' + x.mensagem);
  });

  titulo_(b, 'Dores', DocumentApp.ParagraphHeading.HEADING1);
  b.appendParagraph('Validada = média ≥ ' + CONFIG.criterio.media + ' e pelo menos ' + pct(CONFIG.criterio.top2) +
    ' das pessoas deram nota 4 ou 5 (mínimo de ' + CONFIG.criterio.minimoRespostas + ' respostas).');
  tabela_(b, [['Dor', 'Média (1–5)', 'Nota 4 ou 5', 'Era hipótese?', 'Situação']]
    .concat(a.dores.map((d) => [d.texto, d.media.toFixed(1), pct(d.top2), d.hipotese ? 'sim' : 'não', d.status])));

  titulo_(b, 'O que as pessoas disseram', DocumentApp.ParagraphHeading.HEADING1);
  titulo_(b, 'Situações frustrantes', DocumentApp.ParagraphHeading.HEADING2);
  citacoes_(b, a.relatos);
  titulo_(b, 'O que faria a pessoa usar o Zyft', DocumentApp.ParagraphHeading.HEADING2);
  citacoes_(b, a.maioresDores);

  titulo_(b, 'Disposição para usar e pagar', DocumentApp.ParagraphHeading.HEADING1);
  a.disposicao.forEach((p) => {
    item_(b, p.rotulo + ': ' + p.contagem.map((c) => c[0] + ' (' + pct(c[1] / a.n) + ')').join(' · '));
  });

  titulo_(b, 'Próximos passos', DocumentApp.ParagraphHeading.HEADING1);
  proximosPassos_(a, poucos).forEach((t) => item_(b, t));

  titulo_(b, 'Dados para revisar com a IA', DocumentApp.ParagraphHeading.HEADING1);
  b.appendParagraph('Copie o bloco abaixo e cole no Claude para discutir e refinar a persona, o arquétipo e a dor. ' +
    'Ele não contém contatos nem textos livres.');
  b.appendParagraph(JSON.stringify(resumoParaIA_(a))).editAsText().setFontFamily('Courier New').setFontSize(8);

  // b.clear() deixa um parágrafo vazio no topo
  if (b.getNumChildren() > 1 && b.getChild(0).asParagraph().getText() === '') b.getChild(0).removeFromParent();

  doc.saveAndClose();
  Logger.log('Plano: ' + doc.getUrl());
  return doc.getUrl();
}

function proximosPassos_(a, poucos) {
  const passos = [];
  if (poucos) {
    passos.push('Coletar pelo menos mais ' + (CONFIG.criterio.minimoRespostas - a.n) +
      ' respostas antes de decidir qualquer coisa.');
  }
  const fortes = a.dores.filter((d) => d.status === 'validada');
  if (fortes.length) {
    passos.push('Centrar a mensagem de divulgação na dor "' + fortes[0].texto + '".');
  } else if (!poucos) {
    passos.push('Nenhuma dor passou no critério: entrevistar 5 pessoas para descobrir o que realmente incomoda.');
  }
  a.dores.filter((d) => d.hipotese && d.status === 'não validada').forEach((d) => {
    passos.push('Rever a hipótese "' + d.texto + '": as respostas não confirmaram.');
  });
  a.dores.filter((d) => !d.hipotese && d.status === 'validada').forEach((d) => {
    passos.push('Dor que não estava no radar e apareceu forte: "' + d.texto + '". Avaliar se o produto deve atacá-la.');
  });
  const principal = a.arquetipos[0];
  if (principal.n) passos.push('Escrever os posts para ' + principal.nome + ' primeiro. ' + principal.mensagem);
  const beta = a.disposicao.find((p) => p.chave === 'beta');
  const sim = beta ? (beta.contagem.find((c) => c[0] === 'Sim') || [0, 0])[1] : 0;
  if (sim) passos.push(sim + ' pessoa(s) querem testar: ver os contatos na planilha e marcar entrevistas curtas.');
  return passos;
}

function resumoParaIA_(a) {
  const r2 = (x) => Math.round(x * 100) / 100;
  return {
    produto: CONFIG.produto,
    respostas: a.n,
    persona: a.persona.map((p) => ({ [p.rotulo]: p.contagem.slice(0, 3) })),
    arquetipos: a.arquetipos.map((x) => ({
      nome: x.nome, pct: r2(x.pct), perfil: x.perfil.map((p) => p.valor), dores: x.dores.map((d) => d.chave),
    })),
    dores: a.dores.map((d) => ({ dor: d.texto, media: r2(d.media), top2: r2(d.top2), hipotese: d.hipotese, status: d.status })),
    disposicao: a.disposicao.map((p) => ({ [p.rotulo]: p.contagem })),
  };
}

function abrirOuCriarPlano_() {
  const props = PropertiesService.getScriptProperties();
  const id = props.getProperty('PLANO_ID');
  if (id) {
    try { return DocumentApp.openById(id); } catch (e) { /* apagado: cria outro */ }
  }
  const doc = DocumentApp.create('Relatório — Pesquisa v2 — ' + CONFIG.produto);
  props.setProperty('PLANO_ID', doc.getId());
  return doc;
}

function titulo_(b, texto, nivel) {
  b.appendParagraph(texto).setHeading(nivel);
}

function item_(b, texto) {
  b.appendListItem(texto).setGlyphType(DocumentApp.GlyphType.BULLET);
}

function tabela_(b, linhas) {
  const t = b.appendTable(linhas);
  t.getRow(0).editAsText().setBold(true);
}

function citacoes_(b, textos) {
  if (!textos.length) {
    b.appendParagraph('Ninguém respondeu ainda.').editAsText().setItalic(true);
    return;
  }
  textos.forEach((t) => b.appendParagraph('“' + t + '”').setIndentStart(24).editAsText().setItalic(true));
}

// ───────────────────────────── Utilidades ─────────────────────────────

function ativarAtualizacaoAutomatica() {
  ScriptApp.getProjectTriggers()
    .filter((t) => t.getHandlerFunction() === 'gerarPlano')
    .forEach((t) => ScriptApp.deleteTrigger(t));
  ScriptApp.newTrigger('gerarPlano').forForm(abrirFormulario_()).onFormSubmit().create();
  Logger.log('O plano será refeito a cada nova resposta.');
}

function limparRespostas() {
  abrirFormulario_().deleteAllResponses();
  Logger.log('Respostas apagadas do formulário. As linhas da planilha não são apagadas: limpe-as à mão.');
}

function resetarConfiguracao() {
  PropertiesService.getScriptProperties().deleteAllProperties();
  Logger.log('Configuração esquecida. Os arquivos no Drive continuam lá.');
}
