// Onboarding em passos ("passaporte"), contrato v2. Uma pergunta por tela; toda pergunta pode
// ficar para depois. As respostas vão para os endpoints do perfil; /onboarding guarda só o progresso.
// Sem anúncios nesta tela.
import { api, asList, registerFieldLabels } from '../api.js';
import { h, clear, announce, toast, showErrors, clearErrors, go, nextId, enhanceInput, characterCounter } from '../dom.js';
import { Button, LinkButton, Icon, Logo, Chip, ConsentRow, initials } from '../ui.js';
import { loadAreas, areaOrOther, findArea, OTHER } from '../catalog.js';
import { getProfile, saveBasics, getOnboarding, saveOnboarding, splitList, joinList, toUrl, basicsOf } from '../store.js';
import { EDUCATION_LEVELS, EDUCATION_STATUS, COURSE_TYPES, PROFICIENCY, SECTIONS, enumName } from './profile-schema.js';
import { normalizeDate } from '../validate.js';
import * as check from '../validate.js';

const CHAPTERS = ['', 'Quem é você', 'Sua área', 'O que você busca', 'Sua bagagem', 'Sua apresentação', 'Combinados'];
const LEVELS = ['Básico', 'Intermediário', 'Avançado', 'Fluente', 'Nativo'];
const PROF_BY_LABEL = { Básico: 'Basic', Intermediário: 'Intermediate', Avançado: 'Advanced', Fluente: 'Fluent', Nativo: 'Native' };
const MODEL = { Presencial: 'OnSite', Híbrido: 'Hybrid', Remoto: 'Remote' };
const MODEL_BACK = { OnSite: 'Presencial', Hybrid: 'Híbrido', Remote: 'Remoto' };
const CONSENTS = [
  { key: 'AiText', title: 'Sugestões de texto com IA', purpose: 'Rascunhar respostas abertas só com o que está carimbado.', basis: 'Base legal: consentimento (art. 7º, I)' },
  { key: 'JobAnalysis', title: 'Análise de vagas', purpose: 'Mostrar onde cada requisito da vaga aparece no seu passaporte.', basis: 'Base legal: consentimento (art. 7º, I)' },
  { key: 'SensitiveData', title: 'Dados sensíveis', purpose: 'Guardar dados sensíveis trancados, para você destrancar um a um. Eles nunca vão para a IA.', basis: 'Base legal: consentimento específico (art. 11, I)' },
];

/** Rótulo curto de cada passo (usado também no alerta de pendentes do Perfil Mestre). */
export const STEP_SHORT = {
  nome: 'Nome', nascimento: 'Nascimento', contato: 'Contato', endereco: 'Onde mora', area: 'Área', cargos: 'Cargos', setores: 'Setores',
  registro: 'Registro profissional', links: 'Links', nivel: 'Momento da carreira', modelo: 'Modelo de trabalho', local: 'Localidade',
  contrato: 'Contrato', disponibilidade: 'Disponibilidade', salario: 'Pretensão salarial', mobilidade: 'Mobilidade', experiencia: 'Experiência',
  diaadia: 'Dia a dia e resultados', ferramentas: 'Ferramentas', formacao: 'Formação', cursos: 'Cursos e certificados', competencias: 'Competências',
  idiomas: 'Idiomas', titulo: 'Título profissional', resumo: 'Resumo', combinados: 'Combinados',
};

const T = (key, label, ph, opts = {}) => ({
  t: 'input',
  key,
  label,
  ph: ph || '',
  max: opts.max ?? (opts.type === 'email' ? 254 : opts.type === 'url' ? 300 : null),
  ...opts,
});
const S = (key, label, options, opts = {}) => ({ t: 'select', key, label, options, ...opts });
const X = (key, label, ph, opts = {}) => ({ t: 'area', key, label, ph, span: true, max: 4000, ...opts });
const opts = (list) => list.filter(([v]) => v !== '');
// Limite do campo adicionado à mão: o mesmo da coluna da API que recebe a lista.
const CUSTOM_MAX = { roles: 500, sectors: 500, contracts: 200, tools: 1000, skills: 100 };
// A coluna guarda a lista inteira: o limite vale para a soma dos itens. Item com vírgula viraria dois ao recarregar.
const LIST_FIELD = { roles: 'desiredRoles', sectors: 'areas', contracts: 'contractTypes', tools: 'tools' };
registerFieldLabels({ tools: 'Ferramentas', idiomas: 'Idiomas' });
const DEFAULT_LANGS = ['Português', 'Inglês', 'Espanhol'];
const lower = (x) => String(x).toLowerCase();
const sameText = (a, b) => lower(a) === lower(b);

function buildSteps(A) {
  const list = [
    { id: 'intro', kind: 'intro' },
    { id: 'inicio', kind: 'start' },
    { id: 'nome', ch: 1, kind: 'text', q: 'Como você se chama?', hint: 'É assim que o seu nome vai aparecer nos formulários.', fields: [T('fullName', 'Nome completo', 'Nome e sobrenome', { span: true, auto: 'name', max: 160 })] },
    { id: 'nascimento', ch: 1, kind: 'text', q: 'Quando você nasceu?', hint: 'Muitos formulários pedem. Fica só no seu perfil.', fields: [T('birthDate', 'Data de nascimento', '', { type: 'date', auto: 'bday' })] },
    { id: 'contato', ch: 1, kind: 'text', q: 'Como as empresas falam com você?', hint: 'Use o telefone e o e-mail que você confere todo dia.', fields: [T('phone', 'Telefone com DDD', '(00) 00000-0000', { type: 'tel', auto: 'tel', mask: 'phone', max: 15 }), T('email', 'E-mail de contato', 'voce@email.com', { type: 'email', auto: 'email', max: 254 })] },
    { id: 'endereco', ch: 1, kind: 'text', q: 'Onde você mora?', hint: 'Só cidade e CEP. Dados que você considera sensíveis ficam trancados em Dados sensíveis, no perfil.', fields: [T('city', 'Cidade', 'Ex.: Belo Horizonte', { auto: 'address-level2', max: 120 }), T('state', 'Estado (UF)', 'Ex.: MG', { auto: 'address-level1', mask: 'uf', max: 2 }), T('country', 'País', '', { auto: 'country-name', max: 80 }), T('zipCode', 'CEP', '00000-000', { auto: 'postal-code', mode: 'numeric', mask: 'cep', max: 9 })] },
    { id: 'area', ch: 2, kind: 'single', key: 'area', q: 'Em que área você trabalha ou quer trabalhar?', hint: 'As próximas perguntas e sugestões se adaptam à sua área.' },
    { id: 'cargos', ch: 2, kind: 'multi', key: 'roles', q: 'Que vagas você está buscando?', hint: 'Escolha quantas quiser ou escreva o nome do cargo do seu jeito.', options: A.roles, customLabel: 'Outro cargo', customPh: 'Ex.: Cuidador(a) de idosos' },
    { id: 'setores', ch: 2, kind: 'multi', key: 'sectors', q: 'Em que tipo de empresa ou setor?', hint: 'Ajuda a extensão quando o formulário pergunta por área de interesse.', options: A.sectors, customLabel: 'Outro setor', customPh: 'Ex.: Hotelaria' },
  ];
  if (A.registration) list.push({ id: 'registro', ch: 2, kind: 'text', q: 'Você tem registro ou certificação obrigatória?', hint: 'Na sua área, os formulários costumam pedir.', fields: [T('professionalRegistration', A.registration.label, A.registration.placeholder, { span: true, max: 120 })] });
  const links = [T('linkedinUrl', 'LinkedIn', 'linkedin.com/in/…', { type: 'url' })];
  if (A.linkLabel) links.push(T('link2', A.linkLabel, '', { type: 'url', max: A.linkLabel === 'GitHub' ? 300 : 500 }));
  links.push(T('portfolioUrl', 'Portfólio ou site', 'Opcional', { span: true, type: 'url' }));
  list.push(
    { id: 'links', ch: 2, kind: 'text', q: 'Onde as empresas podem ver seu trabalho?', hint: 'Coloque só os que você mantém atualizados.', fields: links },
    { id: 'nivel', ch: 3, kind: 'single', key: 'level', q: 'Em que momento da carreira você está?', hint: 'Vale para qualquer área.', options: [['Primeiro emprego', 'Ainda sem experiência formal'], ['Estágio ou aprendiz', 'Estudando e trabalhando'], ['Júnior ou assistente', 'Até 2 anos'], ['Pleno ou analista', 'De 2 a 5 anos'], ['Sênior', 'Mais de 5 anos'], ['Liderança', 'Coordena pessoas ou equipes']] },
    { id: 'modelo', ch: 3, kind: 'single', key: 'mode', q: 'Como você prefere trabalhar?', hint: 'Escolha o que mais combina.', options: [['Presencial', 'No local todos os dias'], ['Híbrido', 'Alguns dias no local'], ['Remoto', 'De casa'], ['Tanto faz', 'Depende da vaga']] },
    { id: 'local', ch: 3, kind: 'text', q: 'Onde você aceita trabalhar?', hint: 'Cidades, regiões ou "qualquer lugar".', fields: [T('locationPreference', 'Localidades', 'Ex.: Belo Horizonte e região', { span: true, max: 200 })] },
    { id: 'contrato', ch: 3, kind: 'multi', key: 'contracts', q: 'Que tipo de contrato você aceita?', hint: 'Marque todos que servem.', options: ['CLT', 'PJ', 'Estágio', 'Jovem aprendiz', 'Temporário', 'Freelancer', 'Plantão / escala'], customLabel: 'Outro tipo', customPh: 'Ex.: Cooperado', noSuggestTitle: true },
    { id: 'disponibilidade', ch: 3, kind: 'single', key: 'avail', q: 'Quando você pode começar?', hint: '', options: [['Imediatamente', ''], ['Em 15 dias', ''], ['Em 30 dias', ''], ['A combinar', '']] },
    { id: 'salario', ch: 3, kind: 'text', q: 'Quanto você quer ganhar?', hint: 'Fica só no seu perfil e só vai para um formulário se você aprovar. Pode deixar para depois.', fields: [T('salaryExpectation', 'Valor mensal', 'R$ 0,00', { mode: 'decimal', max: 17 }), S('salaryType', 'Tipo', [['CLT (bruto)', 'CLT (bruto)'], ['PJ', 'PJ'], ['Por hora', 'Por hora'], ['Por plantão', 'Por plantão']])] },
    { id: 'mobilidade', ch: 3, kind: 'rows', q: 'E sobre deslocamento?', hint: 'Três respostas rápidas.', rows: [['travel', 'Topa viajar a trabalho?', ['Sim', 'Às vezes', 'Não']], ['move', 'Mudaria de cidade?', ['Sim', 'Talvez', 'Não']], ['cnh', 'Tem CNH?', ['Não tenho', 'A', 'B', 'AB', 'C', 'D', 'E']]] },
    { id: 'experiencia', ch: 4, kind: 'text', exp: true, addMore: 'Adicionar outra experiência', q: 'Onde você trabalha ou trabalhou por último?', hint: 'Vale estágio, freela, trabalho voluntário ou negócio próprio.', fields: [T('company', 'Empresa ou lugar', 'Ex.: Hospital Santa Clara', { max: 200 }), T('role', 'Cargo ou função', A.roles[0] ? `Ex.: ${A.roles[0]}` : '', { max: 200 }), T('startDate', 'Início', 'aaaa-mm', { type: 'month' }), T('endDate', 'Fim', 'aaaa-mm', { type: 'month' })] },
    { id: 'diaadia', ch: 4, kind: 'text', q: 'O que você fazia lá, e o que deu certo?', hint: 'Números ajudam: pessoas atendidas, metas, prazos. Escreva como você falaria.', fields: [X('activities', 'No dia a dia', 'Ex.: Atendia 30 pacientes por plantão…'), X('results', 'Um resultado de que você se orgulha', 'Ex.: Reduzi o tempo de triagem…')] },
    { id: 'ferramentas', ch: 4, kind: 'multi', key: 'tools', q: 'Que ferramentas, sistemas ou equipamentos você usa?', hint: 'Pode ser software, máquina, instrumento ou método.', options: A.tools, customLabel: 'Outra ferramenta', customPh: 'Ex.: Glicosímetro, Empilhadeira elétrica' },
    { id: 'formacao', ch: 4, kind: 'text', addMore: 'Adicionar outra formação', q: 'Qual a sua formação principal?', hint: 'A mais importante para as vagas que você busca.', fields: [T('institution', 'Instituição', 'Ex.: Senac, UFMG, escola', { max: 200 }), T('course', 'Curso', 'Ex.: Técnico em Enfermagem', { max: 200 }), S('level', 'Nível', opts(EDUCATION_LEVELS)), S('status', 'Situação', opts(EDUCATION_STATUS)), T('eduStart', 'Início', 'aaaa-mm', { type: 'month' }), T('eduEnd', 'Conclusão ou previsão', 'aaaa-mm', { type: 'month' })] },
    { id: 'cursos', ch: 4, kind: 'text', addMore: 'Adicionar outro curso', q: 'Fez algum curso ou certificação que vale citar?', hint: 'Cursos livres contam.', fields: [T('courseName', 'Nome do curso', 'Ex.: Suporte Básico de Vida', { max: 200 }), S('courseType', 'Tipo', COURSE_TYPES), T('issuer', 'Quem emitiu', '', { max: 200 }), T('hours', 'Carga horária (horas)', 'Ex.: 16', { mode: 'numeric', max: 5 })] },
    { id: 'competencias', ch: 4, kind: 'multi', key: 'skills', q: 'O que você sabe fazer bem?', hint: 'Toque nas sugestões ou cadastre as suas. O nível você ajusta depois no perfil.', options: A.skills, customLabel: 'Outra competência', customPh: 'Escreva do seu jeito' },
    { id: 'idiomas', ch: 4, kind: 'rows', langs: true, q: 'Que idiomas você fala?', hint: 'Marque o nível de cada um. Libras também conta.' },
    { id: 'titulo', ch: 5, kind: 'text', q: 'Como você se apresenta em uma linha?', hint: 'Aparece no topo dos formulários que pedem "título" ou "cargo atual".', fields: [T('headline', 'Título', 'Ex.: Técnica de enfermagem com foco em UTI', { span: true, max: 200 })] },
    { id: 'resumo', ch: 5, kind: 'text', q: 'Resuma sua trajetória em poucas frases.', hint: 'Três ou quatro frases. Você pode pedir um rascunho à IA depois, no perfil.', fields: [X('summary', 'Resumo', '')] },
    { id: 'combinados', ch: 6, kind: 'consent', q: 'Por último: o que o Zyft pode fazer por você?', hint: 'Você muda isso quando quiser em Conta e privacidade.' },
    { id: 'fim', kind: 'done' },
  );
  return list;
}

const filled = (...xs) => xs.some((x) => x !== null && x !== undefined && String(x).trim() !== '');
const HAS_DATA = {
  nome: (p) => filled(p.fullName), nascimento: (p) => filled(p.birthDate), contato: (p) => filled(p.phone, p.email),
  endereco: (p) => filled(p.city, p.state, p.zipCode), area: (p) => filled(p.workArea), cargos: (p) => filled(p.desiredRoles), setores: (p) => filled(p.areas),
  registro: (p) => filled(p.professionalRegistration), nivel: (p) => filled(p.seniority), local: (p) => filled(p.locationPreference),
  contrato: (p) => filled(p.contractTypes), disponibilidade: (p) => filled(p.availability), salario: (p) => filled(p.salaryExpectation),
  experiencia: (p) => Boolean(p.experiences?.length), diaadia: (p) => (p.experiences || []).some((e) => filled(e.activities, e.results)),
  ferramentas: (p) => (p.experiences || []).some((e) => filled(e.tools)) || Boolean(p.skills?.length), formacao: (p) => Boolean(p.educations?.length),
  cursos: (p) => Boolean(p.courses?.length), competencias: (p) => Boolean(p.skills?.length), idiomas: (p) => Boolean(p.languages?.length),
  titulo: (p) => filled(p.headline), resumo: (p) => filled(p.summary),
};

/**
 * O passo tem dado de verdade no perfil? "Tanto faz", "Às vezes" e "Talvez" gravam null e são respostas
 * válidas: nesses passos, ter sido respondido conta como dado (a API não tem valor para essas opções).
 */
export function stepHasData(id, p, { customs = [], linkLabel = null, answered = [] } = {}) {
  if (id === 'modelo') return filled(p.workModel) || answered.includes(id);
  if (id === 'mobilidade') return filled(p.availableToTravel, p.availableToRelocate, p.hasDriverLicense) || answered.includes(id);
  if (id === 'links') return filled(p.linkedinUrl, p.githubUrl, p.portfolioUrl) || customs.some((c) => c.label === linkLabel && filled(c.value));
  return Boolean(HAS_DATA[id]?.(p));
}

/** Setas movem a escolha no radiogroup, como nos grupos de rádio nativos. */
export function radioKeys(group) {
  group.addEventListener('keydown', (e) => {
    const items = [...group.querySelectorAll('[role="radio"]')];
    const i = items.indexOf(document.activeElement);
    const d = e.key === 'ArrowDown' || e.key === 'ArrowRight' ? 1 : e.key === 'ArrowUp' || e.key === 'ArrowLeft' ? -1 : 0;
    if (i < 0 || !d) return;
    e.preventDefault();
    const target = items[(i + d + items.length) % items.length];
    target.click();
    if (target.isConnected) target.focus(); // quem repinta o grupo devolve o foco ao botão novo
  });
}

const boolTo = (b, yes, no) => (b === true ? yes : b === false ? no : '');
const month = (d) => (d ? String(d).slice(0, 7) : '');

export async function render(main, { step: startAt, me }) {
  const [areas, profile, ob, consentsRaw, customs] = await Promise.all([
    loadAreas(),
    getProfile(),
    getOnboarding(),
    api('/consents').then(asList).catch(() => []),
    api('/profile/custom-fields').then(asList).catch(() => []),
  ]);
  let p = profile || {};
  const st = {
    answered: new Set(ob.answered || []),
    skipped: new Set(ob.skipped || []),
    completedAt: ob.completedAt,
    a: {}, m: { roles: [], sectors: [], contracts: [], tools: [], skills: [] }, custom: {}, langs: [],
    consents: {}, draft: '', expId: null, eduId: null, courseId: null, current: true, lastSaved: {}, shownSkills: [],
  };
  for (const c of CONSENTS) {
    const found = consentsRaw.find((x) => String(x.purpose).toLowerCase() === c.key.toLowerCase());
    st.consents[c.key] = Boolean(found?.granted);
  }

  const loadExperience = (exp) => {
    st.expId = exp.id;
    Object.assign(st.a, { company: exp.company, role: exp.role, startDate: month(exp.startDate), endDate: month(exp.endDate), activities: exp.activities || '', results: exp.results || '' });
    st.current = Boolean(exp.isCurrent);
    st.m.tools = splitList(exp.tools);
  };
  const loadEducation = (edu) => {
    st.eduId = edu.id;
    Object.assign(st.a, { institution: edu.institution, course: edu.course, eduLevel: enumName(EDUCATION_LEVELS, edu.level), eduStatus: enumName(EDUCATION_STATUS, edu.status), eduStart: month(edu.startDate), eduEnd: month(edu.endDate) });
  };
  const loadCourse = (course) => {
    st.courseId = course.id;
    Object.assign(st.a, { courseName: course.name, courseType: enumName(COURSE_TYPES, course.type), issuer: course.institution || '', hours: course.hours ? String(course.hours) : '' });
  };

  // ---------- Pré-preenche com o que já está no perfil ----------
  const prefill = () => {
    const a = st.a;
    Object.assign(a, {
      fullName: p.fullName || me?.name || '', birthDate: p.birthDate ? String(p.birthDate).slice(0, 10) : '',
      phone: p.phone || '', email: p.email || me?.email || '', city: p.city || '', state: p.state || '', country: p.country || 'Brasil', zipCode: p.zipCode || '',
      professionalRegistration: p.professionalRegistration || '', linkedinUrl: p.linkedinUrl || '', portfolioUrl: p.portfolioUrl || '',
      level: p.seniority || '', mode: MODEL_BACK[p.workModel] || (st.answered.has('modelo') ? 'Tanto faz' : ''),
      locationPreference: p.locationPreference || '', avail: p.availability || '',
      salaryExpectation: p.salaryExpectation != null ? String(p.salaryExpectation).replace('.', ',') : '', salaryType: p.salaryType || 'CLT (bruto)',
      travel: boolTo(p.availableToTravel, 'Sim', 'Não') || (st.answered.has('mobilidade') ? 'Às vezes' : ''),
      move: boolTo(p.availableToRelocate, 'Sim', 'Não') || (st.answered.has('mobilidade') ? 'Talvez' : ''),
      cnh: p.hasDriverLicense === false ? 'Não tenho' : (p.driverLicenseCategory || ''),
      headline: p.headline || '', summary: p.summary || '',
    });
    const wa = p.workArea || '';
    const known = findArea(areas, wa);
    a.area = known ? known.name : (wa ? OTHER : '');
    a.otherArea = known ? '' : wa;
    st.m.roles = splitList(p.desiredRoles);
    st.m.sectors = splitList(p.areas);
    st.m.contracts = splitList(p.contractTypes);
    st.m.skills = (p.skills || []).map((s) => s.name);
    if (p.experiences?.[0]) loadExperience(p.experiences[0]);
    if (p.educations?.[0]) loadEducation(p.educations[0]);
    if (p.courses?.[0]) loadCourse(p.courses[0]);
    const langs = (p.languages || []).map((l) => ({ id: l.id, name: l.name, level: LEVELS[['Basic', 'Intermediate', 'Advanced', 'Fluent', 'Native'].indexOf(enumName(PROFICIENCY, l.proficiency))] || '' }));
    for (const def of DEFAULT_LANGS) if (!langs.some((l) => l.name.toLowerCase() === def.toLowerCase())) langs.push({ name: def, level: '' });
    st.langs = langs;
    const A = curArea();
    if (A.linkLabel === 'GitHub') a.link2 = p.githubUrl || '';
    else if (A.linkLabel) a.link2 = customs.find((c) => c.label === A.linkLabel)?.value || '';
  };
  // Campos de formação/curso usam chaves próprias para não colidir com "nível" da carreira.
  const curArea = () => (st.a.area && st.a.area !== OTHER ? areaOrOther(areas, st.a.area) : areaOrOther(areas, OTHER));
  const selectDefaults = () => {
    st.a.eduLevel ||= 'Technical';
    st.a.eduStatus ||= 'Completed';
    st.a.courseType ||= 'Course';
  };
  prefill();
  selectDefaults();

  let steps = buildSteps(curArea());
  const idxOf = (id) => Math.max(0, steps.findIndex((s) => s.id === id));
  let idx = ob.completedAt ? steps.length - 1 : ob.lastStep ? idxOf(ob.lastStep) : 0;
  if (startAt) {
    // "fim" só existe depois de concluir; passo desconhecido volta ao início. A URL acompanha.
    const known = steps.some((s) => s.id === startAt) && (startAt !== 'fim' || ob.completedAt);
    if (known) idx = idxOf(startAt);
    else {
      if (startAt !== 'fim') idx = 0;
      try { history.replaceState(history.state, '', `#/boas-vindas/${steps[idx].id}`); } catch { /* ok */ }
    }
  }

  const questions = () => steps.filter((s) => s.ch && s.kind !== 'consent');
  const fieldKey = (f, s) => (s.id === 'formacao' && f.key === 'level' ? 'eduLevel' : s.id === 'formacao' && f.key === 'status' ? 'eduStatus' : f.key);

  // ---------- Layout fixo ----------
  const top = h('header', { class: 'ob__top' });
  const body = h('section', { class: 'ob__main', 'aria-live': 'off' });
  const aside = h('aside', { class: 'passport', 'aria-label': 'Seu passaporte' });
  main.append(h('div', { class: 'ob' }, top, h('div', { class: 'ob__body' }, body, aside)));

  const ctx = () => ({ customs, linkLabel: curArea().linkLabel, answered: [...st.answered] });
  const hasSaved = (s) => stepHasData(s.id, p, ctx());
  // Formulário vazio em passo repetível: nada a gravar, vale o que já foi guardado (A3).
  const isBlank = (s) => Boolean(s.addMore) && s.fields.every((f) => f.t === 'select' || !String(st.a[fieldKey(f, s)] || '').trim());

  const isAnswered = (s) => {
    const a = st.a;
    if (s.kind === 'single') return s.key === 'area' ? Boolean(a.area && (a.area !== OTHER || a.otherArea?.trim())) : Boolean(a[s.key]);
    if (s.kind === 'multi') return (st.m[s.key] || []).length > 0;
    if (s.kind === 'text') {
      if (s.id === 'experiencia') return Boolean(a.company?.trim() && a.role?.trim() && a.startDate);
      if (s.id === 'formacao') return Boolean(a.institution?.trim() && a.course?.trim() && a.eduStart);
      if (s.id === 'cursos') return Boolean(a.courseName?.trim());
      if (s.id === 'diaadia') return Boolean((a.activities || '').trim() || (a.results || '').trim()) && Boolean(st.expId);
      return s.fields.some((f) => f.t !== 'select' && String(a[fieldKey(f, s)] || '').trim());
    }
    if (s.kind === 'rows' && s.langs) return st.langs.some((l) => l.level);
    if (s.kind === 'rows') return s.rows.some((r) => a[r[0]]);
    return true;
  };

  // ---------- Gravação de cada passo ----------
  const basics = async (patch) => {
    const r = await saveBasics(patch, p);
    const next = { ...p, ...r };
    for (const k of Object.keys(patch)) if (r?.[k] === undefined) next[k] = patch[k];
    p = next;
  };
  const refreshLists = async () => { const fresh = await getProfile(); p = { ...p, ...fresh }; };

  const saveExperience = async () => {
    const a = st.a;
    const prev = (p.experiences || []).find((e) => e.id === st.expId);
    const body = {
      company: a.company?.trim(), role: a.role?.trim(), startDate: normalizeDate(a.startDate), endDate: st.current ? null : normalizeDate(a.endDate) || null,
      isCurrent: st.current, activities: prev?.activities ?? null, tools: prev?.tools ?? null, results: prev?.results ?? null,
    };
    const r = st.expId ? await api(`/profile/experiences/${st.expId}`, { method: 'PUT', body }) : await api('/profile/experiences', { method: 'POST', body });
    if (r?.id) st.expId = r.id;
    await refreshLists();
  };
  const patchExperience = async (patch) => {
    const e = (p.experiences || []).find((x) => x.id === st.expId);
    if (!e) return false;
    await api(`/profile/experiences/${e.id}`, { method: 'PUT', body: { company: e.company, role: e.role, startDate: e.startDate, endDate: e.endDate, isCurrent: e.isCurrent, activities: e.activities, tools: e.tools, results: e.results, ...patch } });
    await refreshLists();
    return true;
  };
  const saveEducation = async () => {
    const a = st.a;
    const body = { institution: a.institution?.trim(), course: a.course?.trim(), level: a.eduLevel, status: a.eduStatus, startDate: normalizeDate(a.eduStart), endDate: normalizeDate(a.eduEnd) || null };
    const r = st.eduId ? await api(`/profile/educations/${st.eduId}`, { method: 'PUT', body }) : await api('/profile/educations', { method: 'POST', body });
    if (r?.id) st.eduId = r.id;
    await refreshLists();
  };
  const saveCourse = async () => {
    const a = st.a;
    const hours = a.hours ? parseInt(String(a.hours).replace(/\D/g, ''), 10) : null;
    const body = { name: a.courseName?.trim(), type: a.courseType || 'Course', institution: a.issuer?.trim() || null, hours: Number.isFinite(hours) && hours > 0 ? hours : null, completedAt: null };
    const r = st.courseId ? await api(`/profile/courses/${st.courseId}`, { method: 'PUT', body }) : await api('/profile/courses', { method: 'POST', body });
    if (r?.id) st.courseId = r.id;
    await refreshLists();
  };
  const upsertCustom = async (label, value) => {
    const found = customs.find((c) => c.label === label);
    if (found) await api(`/profile/custom-fields/${found.id}`, { method: 'PUT', body: { label, value } });
    else { const r = await api('/profile/custom-fields', { method: 'POST', body: { label, value } }); if (r?.id) customs.push(r); }
  };

  const removeCustom = async (label) => {
    const i = customs.findIndex((c) => c.label === label);
    if (i < 0) return;
    await api(`/profile/custom-fields/${customs[i].id}`, { method: 'DELETE' });
    customs.splice(i, 1);
  };

  // Passos gravados só em PUT /profile: cabem numa requisição única, a que sai ao fechar a aba.
  const PATCH = {
    nome: () => ({ fullName: st.a.fullName?.trim() || null }),
    nascimento: () => ({ birthDate: st.a.birthDate || null }),
    contato: () => ({ phone: st.a.phone?.trim() || null, email: st.a.email?.trim() || null }),
    endereco: () => ({ city: st.a.city?.trim() || null, state: st.a.state?.trim() || null, country: st.a.country?.trim() || null, zipCode: st.a.zipCode?.trim() || null }),
    area: () => ({ workArea: st.a.area === OTHER ? (st.a.otherArea?.trim() || null) : st.a.area }),
    cargos: () => ({ desiredRoles: joinList(st.m.roles) }),
    setores: () => ({ areas: joinList(st.m.sectors) }),
    registro: () => ({ professionalRegistration: st.a.professionalRegistration?.trim() || null }),
    links: () => {
      const patch = { linkedinUrl: toUrl(st.a.linkedinUrl), portfolioUrl: toUrl(st.a.portfolioUrl) };
      if (curArea().linkLabel === 'GitHub') patch.githubUrl = toUrl(st.a.link2);
      return patch;
    },
    nivel: () => ({ seniority: st.a.level || null }),
    modelo: () => ({ workModel: MODEL[st.a.mode] || null }),
    local: () => ({ locationPreference: st.a.locationPreference?.trim() || null }),
    contrato: () => ({ contractTypes: joinList(st.m.contracts) }),
    disponibilidade: () => ({ availability: st.a.avail || null }),
    salario: () => ({ salaryExpectation: check.parseMoney(st.a.salaryExpectation).value, salaryType: st.a.salaryType || null }),
    mobilidade: () => {
      const a = st.a;
      // "Às vezes" e "Talvez" não são sim nem não: ficam null, e a extensão pede a resposta à pessoa no formulário.
      const answer = (val, saved) => (val === 'Sim' ? true : val === 'Não' ? false : val ? null : saved ?? null);
      const patch = { availableToTravel: answer(a.travel, p.availableToTravel), availableToRelocate: answer(a.move, p.availableToRelocate) };
      if (a.cnh) { patch.hasDriverLicense = a.cnh !== 'Não tenho'; patch.driverLicenseCategory = a.cnh === 'Não tenho' ? null : a.cnh; }
      return patch;
    },
    titulo: () => ({ headline: st.a.headline?.trim() || null }),
    resumo: () => ({ summary: st.a.summary?.trim() || null }),
  };

  const SAVE = {
    ...Object.fromEntries(Object.entries(PATCH).map(([id, patch]) => [id, () => basics(patch())])),
    links: async () => {
      await basics(PATCH.links());
      const { linkLabel } = curArea();
      if (!linkLabel || linkLabel === 'GitHub') return;
      if (st.a.link2?.trim()) await upsertCustom(linkLabel, toUrl(st.a.link2));
      else await removeCustom(linkLabel);
    },
    experiencia: saveExperience,
    diaadia: () => patchExperience({ activities: st.a.activities?.trim() || null, results: st.a.results?.trim() || null }),
    ferramentas: async () => {
      if (await patchExperience({ tools: joinList(st.m.tools) })) return;
      const have = new Set((p.skills || []).map((s) => lower(s.name)));
      for (const t of st.m.tools) if (!have.has(lower(t))) await api('/profile/skills', { method: 'POST', body: { name: t, level: null } });
      await refreshLists();
      st.m.skills = [...st.m.skills, ...st.m.tools.filter((t) => !st.m.skills.some((s) => sameText(s, t)))];
    },
    formacao: saveEducation,
    cursos: saveCourse,
    competencias: async () => {
      await refreshLists();
      const sel = new Set(st.m.skills.map(lower));
      const have = new Set((p.skills || []).map((s) => lower(s.name)));
      for (const s of st.m.skills) if (!have.has(lower(s))) await api('/profile/skills', { method: 'POST', body: { name: s, level: null } });
      // Só sai o que a pessoa viu marcado aqui e desmarcou. O resto do perfil (ferramentas, outra sessão) fica.
      const unmarked = new Set(st.shownSkills.map(lower).filter((s) => !sel.has(s)));
      for (const s of p.skills || []) if (unmarked.has(lower(s.name))) await api(`/profile/skills/${s.id}`, { method: 'DELETE' });
      await refreshLists();
    },
    idiomas: async () => {
      const existing = p.languages || [];
      for (const l of st.langs) {
        if (!l.level) continue;
        const prof = PROF_BY_LABEL[l.level];
        const cur = existing.find((x) => x.id === l.id);
        if (cur) { if (enumName(PROFICIENCY, cur.proficiency) !== prof) await api(`/profile/languages/${cur.id}`, { method: 'PUT', body: { name: l.name, proficiency: prof } }); }
        else { const r = await api('/profile/languages', { method: 'POST', body: { name: l.name, proficiency: prof } }); if (r?.id) l.id = r.id; }
      }
      await refreshLists();
    },
  };

  // ---------- Validação de cada passo (as mesmas regras do Perfil Mestre) ----------
  // Valor que já estava salvo não barra a pessoa: só valida o que ela mudou.
  const changed = (key, saved) => String(st.a[key] ?? '').trim() !== String(saved ?? '').trim();
  const listFits = (key, list) => {
    const limit = key === 'tools' && !st.expId ? null : CUSTOM_MAX[key];
    return !LIST_FIELD[key] || !limit || (joinList(list) || '').length <= limit;
  };
  const listTooLong = (key) => `Sua seleção passou do limite de ${CUSTOM_MAX[key]} caracteres. Tire ou encurte algum item para continuar.`;

  const stepErrors = (s) => {
    const a = st.a;
    if (s.addMore && isBlank(s)) return {};
    const section = (key) => SECTIONS.find((x) => x.key === key).validate;
    switch (s.id) {
      case 'nome': return check.collect({ fullName: check.required(a.fullName, 'Informe seu nome completo.') });
      case 'nascimento': return check.collect({ birthDate: check.notFuture(a.birthDate, 'A data de nascimento não pode estar no futuro.') });
      case 'contato': return check.collect({
        phone: changed('phone', p.phone) ? check.phone(a.phone) : null,
        email: a.email?.trim() && changed('email', p.email) ? check.email(a.email.trim()) : null,
      });
      case 'endereco': return check.collect({
        state: changed('state', p.state) ? check.uf(a.state?.trim(), a.country) : null,
        zipCode: changed('zipCode', p.zipCode) ? check.cep(a.zipCode?.trim(), a.country) : null,
      });
      case 'links': return check.collect(Object.fromEntries(s.fields.map((f) => [f.key, changed(f.key, p[f.key]) ? check.webUrl(toUrl(a[f.key]), f.max) : null])));
      case 'salario': return check.collect({ salaryExpectation: check.parseMoney(a.salaryExpectation).error });
      case 'experiencia': return check.collect(section('experiences')({ company: a.company, role: a.role, startDate: normalizeDate(a.startDate), endDate: st.current ? null : normalizeDate(a.endDate), isCurrent: st.current }));
      case 'formacao': {
        const e = section('educations')({ institution: a.institution, course: a.course, level: a.eduLevel, status: a.eduStatus, startDate: normalizeDate(a.eduStart), endDate: normalizeDate(a.eduEnd) });
        return check.collect({ institution: e.institution, course: e.course, eduLevel: e.level, eduStatus: e.status, eduStart: e.startDate, eduEnd: e.endDate });
      }
      case 'cursos': return check.collect({ courseName: check.required(a.courseName, 'Informe o nome do curso.'), hours: check.parseHours(a.hours).error });
      case 'idiomas': {
        const open = st.langs.find((l) => !l.level && !l.id && !DEFAULT_LANGS.includes(l.name));
        return check.collect({ idiomas: open ? `Escolha o nível de ${open.name} ou remova o idioma.` : null });
      }
      default:
        return s.kind === 'multi' && !listFits(s.key, st.m[s.key]) ? check.collect({ [LIST_FIELD[s.key]]: listTooLong(s.key) }) : {};
    }
  };

  // Formulário vazio depois de "Adicionar outra": os passos seguintes voltam a falar do último item guardado.
  const reloadSaved = (s) => {
    const pick = (list, id) => list?.find((x) => x.id === (id ?? st.lastSaved[s.id])) || list?.[0];
    if (s.id === 'experiencia') { const e = pick(p.experiences, st.expId); if (e) loadExperience(e); }
    if (s.id === 'formacao') { const e = pick(p.educations, st.eduId); if (e) loadEducation(e); }
    if (s.id === 'cursos') { const c = pick(p.courses, st.courseId); if (c) loadCourse(c); }
  };
  const saveStep = async (s) => {
    if (s.addMore && isBlank(s)) { reloadSaved(s); return; }
    await SAVE[s.id]?.();
  };
  const markStep = (s) => {
    if (isAnswered(s) || hasSaved(s)) { st.answered.add(s.id); st.skipped.delete(s.id); } else { st.answered.delete(s.id); st.skipped.add(s.id); }
  };

  /** Grava o progresso. Quem chama decide o que fazer com a falha: avançar como se tivesse salvo, nunca. */
  const persist = async (lastStep, completed = false, opts) => {
    await saveOnboarding({ answered: [...st.answered], skipped: [...st.skipped], lastStep, completedAt: st.completedAt }, completed, opts);
    if (completed) st.completedAt ||= new Date().toISOString();
  };

  // ---------- Rascunho (localStorage, por pessoa) ----------
  // O que foi digitado e ainda não está no perfil fica em localStorage (zyft.onboarding.draft.<id>), para
  // sobreviver a fechar a aba: experiência, formação, cursos, competências e idiomas não cabem no envio do
  // pagehide. Some ao concluir, ao sair da conta (app.js) e depois de DRAFT_TTL. Cada valor guarda também o
  // que o servidor tinha quando foi digitado: se o perfil mudou depois disso, vale o perfil (nunca o rascunho).
  const takeState = () => JSON.stringify({ a: st.a, m: st.m, custom: st.custom, langs: st.langs, current: st.current });
  let entry = takeState();
  let entryIds = { expId: st.expId, eduId: st.eduId, courseId: st.courseId }; // item que o passo editava ao abrir
  const enterStep = () => { entry = takeState(); entryIds = { expId: st.expId, eduId: st.eduId, courseId: st.courseId }; st.shownSkills = [...st.m.skills]; };
  const isDirty = () => takeState() !== entry;
  const DRAFT_KEY = `zyft.onboarding.draft.${me?.id || 'anon'}`;
  const DRAFT_TTL = 7 * 24 * 60 * 60 * 1000;
  const GROUPS = {
    expId: { list: 'experiences', load: (x) => loadExperience(x), blank: () => { Object.assign(st.a, { company: '', role: '', startDate: '', endDate: '', activities: '', results: '' }); st.m.tools = []; st.current = false; }, keys: ['company', 'role', 'startDate', 'endDate', 'activities', 'results'], m: ['tools'], current: true },
    eduId: { list: 'educations', load: (x) => loadEducation(x), blank: () => Object.assign(st.a, { institution: '', course: '', eduStart: '', eduEnd: '' }), keys: ['institution', 'course', 'eduLevel', 'eduStatus', 'eduStart', 'eduEnd'], m: [] },
    courseId: { list: 'courses', load: (x) => loadCourse(x), blank: () => Object.assign(st.a, { courseName: '', issuer: '', hours: '' }), keys: ['courseName', 'courseType', 'issuer', 'hours'], m: [] },
  };
  const groupOf = (kind, key) => Object.keys(GROUPS).find((g) => (kind === 'current' ? GROUPS[g].current : (GROUPS[g][kind === 'a' ? 'keys' : 'm'] || []).includes(key)));
  const norm = (x) => (x === undefined || x === null || x === '' ? null : x);
  const same = (x, y) => JSON.stringify(norm(x)) === JSON.stringify(norm(y));
  const clone = (x) => JSON.parse(JSON.stringify(x));
  const ids = () => ({ expId: st.expId, eduId: st.eduId, courseId: st.courseId });

  /** Como a tela ficaria só com o que está salvo em `prof`, nos itens `want` (null = item novo, em branco). */
  const viewOf = (prof, want) => {
    const keep = { a: st.a, m: st.m, langs: st.langs, current: st.current, ...ids() };
    const keepP = p;
    try {
      p = prof;
      Object.assign(st, { a: {}, m: { roles: [], sectors: [], contracts: [], tools: [], skills: [] }, langs: [], current: true, expId: null, eduId: null, courseId: null });
      prefill();
      selectDefaults();
      const missing = [];
      for (const [g, def] of Object.entries(GROUPS)) {
        if (want[g] === undefined || want[g] === st[g]) continue;
        if (want[g] === null) { def.blank(); st[g] = null; continue; }
        const item = (p[def.list] || []).find((x) => x.id === want[g]);
        if (item) def.load(item); else missing.push(g);
      }
      return { a: { ...st.a }, m: clone(st.m), langs: clone(st.langs), current: st.current, ids: ids(), missing };
    } finally {
      Object.assign(st, keep);
      p = keepP;
    }
  };

  const removeDraft = () => { try { localStorage.removeItem(DRAFT_KEY); } catch { /* sem armazenamento */ } };
  const readDraft = () => {
    let d = null;
    try { d = JSON.parse(localStorage.getItem(DRAFT_KEY) || 'null'); } catch { /* corrompido ou bloqueado */ }
    if (d && d.v === 2 && Date.now() - d.at < DRAFT_TTL) return d;
    if (d) removeDraft();
    return null;
  };
  /** Grava só o que difere do perfil salvo (p), com o valor do servidor de quando foi digitado. */
  const writeDraft = () => {
    if (steps[idx]?.kind === 'done') { removeDraft(); return; }
    const server = viewOf(p, ids());
    const out = { v: 2, at: Date.now(), step: steps[idx].id, ids: ids(), a: {}, m: {}, custom: st.custom, base: { a: {}, m: {} } };
    for (const [k, val] of Object.entries(st.a)) if (!same(val, server.a[k])) { out.a[k] = val; out.base.a[k] = server.a[k] ?? null; }
    for (const [k, val] of Object.entries(st.m)) if (!same(val, server.m[k])) { out.m[k] = val; out.base.m[k] = server.m[k] ?? []; }
    if (!same(st.langs, server.langs)) { out.langs = st.langs; out.base.langs = server.langs; }
    if (st.current !== server.current) { out.current = st.current; out.base.current = server.current; }
    if (!Object.keys(out.a).length && !Object.keys(out.m).length && !out.langs && out.current === undefined) { removeDraft(); return; }
    try { localStorage.setItem(DRAFT_KEY, JSON.stringify(out)); } catch { /* cheio ou bloqueado: vale o que está na tela */ }
  };

  /**
   * Na abertura: aplica o rascunho por cima do perfil recém-lido. Por valor: servidor igual ao de quando foi
   * digitado → vale o rascunho; servidor igual ao rascunho → já foi salvo; servidor diferente dos dois → o
   * perfil mudou depois (outra aba, outro aparelho): vale o servidor e a pessoa fica sabendo.
   */
  const restoreDraft = () => {
    const d = readDraft();
    if (!d) return;
    const now = viewOf(p, d.ids || {});
    const lost = new Set(now.missing); // item apagado em outro lugar
    let applied = 0;
    let conflicts = 0;
    const merge = (mine, base, server, set) => {
      if (same(server, mine)) return;
      if (same(server, base)) { set(mine); applied += 1; } else conflicts += 1;
    };
    // Item do rascunho (ex.: segunda experiência, ou uma nova ainda sem id) aparece com o que o servidor tem dele.
    for (const [g, def] of Object.entries(GROUPS)) {
      const touched = def.keys.some((k) => k in (d.a || {})) || def.m.some((k) => k in (d.m || {})) || (def.current && 'current' in d);
      if (!touched) continue;
      if (lost.has(g)) { conflicts += 1; continue; }
      if (now.ids[g] === st[g]) continue;
      for (const k of def.keys) st.a[k] = now.a[k];
      for (const k of def.m) st.m[k] = now.m[k];
      if (def.current) st.current = now.current;
      st[g] = now.ids[g];
    }
    const skip = (kind, k) => lost.has(groupOf(kind, k));
    for (const [k, val] of Object.entries(d.a || {})) if (!skip('a', k)) merge(val, d.base?.a?.[k], now.a[k], (v) => { st.a[k] = v; });
    for (const [k, val] of Object.entries(d.m || {})) if (!skip('m', k)) merge(val, d.base?.m?.[k], now.m[k], (v) => { st.m[k] = v; });
    if (d.langs) merge(d.langs, d.base?.langs, now.langs, (v) => { st.langs = v; });
    if ('current' in d && !skip('current')) merge(d.current, d.base?.current, now.current, (v) => { st.current = v; });
    Object.assign(st.custom, d.custom || {});
    if (!startAt && !ob.completedAt && steps.some((s) => s.id === d.step && s.ch)) idx = idxOf(d.step);
    if (conflicts) toast('Seu perfil mudou depois do rascunho guardado neste navegador. Mostramos o que está salvo no perfil; confira antes de continuar.');
    else if (applied) toast('Recuperamos o que você tinha preenchido e ainda não estava salvo.');
    writeDraft(); // já sem o que foi salvo ou descartado
  };

  const onPageHide = () => {
    if (!body.isConnected) { window.removeEventListener('pagehide', onPageHide); return; }
    writeDraft();
    const cur = steps[idx];
    if (busy || !PATCH[cur.id] || !isDirty() || !isAnswered(cur) || check.hasErrors(stepErrors(cur))) return;
    api('/profile', { method: 'PUT', body: { ...basicsOf(p), ...PATCH[cur.id]() }, keepalive: true }).catch(() => {});
    markStep(cur);
    persist(cur.id, false, { keepalive: true }).catch(() => {});
  };
  window.addEventListener('pagehide', onPageHide);

  // Uma ação por vez: clique duplo, Enter repetido e Voltar durante o envio não saltam passos.
  let busy = false;
  const locked = async (fn) => {
    if (busy) return;
    busy = true;
    const primary = cta;
    const label = primary?.textContent;
    const others = () => body.querySelectorAll('.ob__actions button, button[data-lock]');
    others().forEach((b) => { b.disabled = true; });
    if (primary) primary.textContent = 'Salvando…';
    try { await fn(); } finally {
      busy = false;
      if (primary?.isConnected) primary.textContent = label;
      others().forEach((b) => { b.disabled = false; });
      syncCta();
    }
  };

  // ---------- Renderização ----------
  let formEl = null;
  let cta = null;

  const paintTop = () => {
    const cur = steps[idx];
    const qs = questions();
    const qNum = qs.indexOf(cur) + 1;
    clear(top,
      h('a', { href: '#/perfil', 'aria-label': 'Zyft, ir para o Perfil Mestre', onclick: cur.kind === 'done' ? null : exit }, Logo()),
      cur.q ? h('div', { class: 'ob__progress' },
        h('div', { class: 'ob__progress-meta' }, h('span', {}, cur.ch ? `Capítulo ${cur.ch} · ${CHAPTERS[cur.ch]}` : ''), h('span', {}, qNum ? `Pergunta ${qNum} de ${qs.length}` : 'Última etapa')),
        h('div', { class: 'ob__segs', 'aria-hidden': 'true' }, [1, 2, 3, 4, 5, 6].map((c) => {
          const inCh = steps.filter((s) => s.ch === c);
          const seen = inCh.filter((s) => steps.indexOf(s) < idx).length;
          return h('span', { class: `ob__seg ${c === cur.ch ? 'is-current' : ''}` }, h('span', { style: { width: `${Math.round((seen / Math.max(1, inCh.length)) * 100)}%` } }));
        }))) : null,
      cur.kind === 'done' ? null : h('a', { href: '#/perfil', class: 'ob__exit', onclick: exit }, 'Sair e continuar depois'));
  };

  // Sair guarda o passo aberto. Se não der, a pessoa fica sabendo; um segundo clique sai mesmo assim.
  let exitArmed = false;
  const exitFailed = (msg) => { exitArmed = true; toast(msg, 'err'); };
  function exit(e) {
    e.preventDefault();
    locked(async () => {
      const cur = steps[idx];
      if (exitArmed) { writeDraft(); go('#/perfil'); return; }
      if (cur.ch && cur.kind !== 'consent' && isDirty()) {
        if (!isAnswered(cur) && !hasSaved(cur)) {
          writeDraft();
          toast('Esta resposta está incompleta: ela fica guardada neste navegador até você terminar.');
          go('#/perfil');
          return;
        }
        const errors = stepErrors(cur);
        if (check.hasErrors(errors)) {
          showErrors(formEl, errors);
          exitFailed('Esta resposta não foi salva. Corrija o campo ou clique em Sair de novo: o rascunho fica guardado neste navegador.');
          return;
        }
        try {
          await saveStep(cur);
          markStep(cur);
        } catch (err) {
          writeDraft();
          if (!err.handled) showErrors(formEl, err);
          exitFailed('Não conseguimos salvar esta resposta. Tente de novo ou clique em Sair de novo: o rascunho fica guardado neste navegador.');
          return;
        }
        enterStep();
      }
      try { await persist(cur.id); } catch {
        exitFailed('Não conseguimos registrar seu progresso. Verifique sua conexão ou clique em Sair de novo para sair assim mesmo.');
        return;
      }
      writeDraft(); // o que ficou sem salvar em outros passos continua guardado
      go('#/perfil');
    });
  }

  const paintAside = () => {
    const qs = questions();
    const areaName = st.a.area === OTHER ? st.a.otherArea : st.a.area;
    const sub = [areaName, st.m.roles[0]].filter(Boolean).join(' · ') || 'Sua área aparece aqui';
    const numero = String(me?.id || '000000').replace(/\D/g, '').padEnd(6, '0').slice(0, 6);
    const answered = qs.filter((s) => st.answered.has(s.id)).length;
    const skipped = qs.filter((s) => st.skipped.has(s.id)).length;
    clear(aside,
      h('div', { class: 'passport__head' },
        h('img', { src: 'assets/zyft-mark.svg', alt: '' }),
        h('div', {}, h('div', { class: 'passport__kicker' }, 'Passaporte profissional'), h('div', { class: 'passport__no' }, `Zyft · Nº ${numero}`))),
      h('div', {}, h('div', { class: 'passport__name' }, st.a.fullName || 'Seu nome aqui'), h('div', { class: 'passport__sub' }, sub)),
      h('ul', { class: 'passport__list' }, [1, 2, 3, 4, 5].map((c, i) => {
        const inCh = steps.filter((s) => s.ch === c);
        const ans = inCh.filter((s) => st.answered.has(s.id)).length;
        const sk = inCh.filter((s) => st.skipped.has(s.id)).length;
        return h('li', { class: 'passport__item' },
          h('div', {}, h('span', { class: 'passport__label' }, CHAPTERS[c]), h('span', { class: 'passport__value' }, `${ans} de ${inCh.length} respondidas`)),
          ans === inCh.length && inCh.length ? h('span', { class: `passport__stamp ${i % 2 ? 'passport__stamp--alt' : ''}` }, 'Carimbado') : null,
          sk ? h('span', { class: 'passport__later' }, `${sk} para depois`) : null);
      })),
      h('div', { class: 'passport__meter' },
        h('div', { class: 'passport__meter-row' }, h('span', {}, `${answered} de ${qs.length} respondidas`), h('span', { style: { opacity: '0.75' } }, `${skipped} para depois`)),
        h('div', { class: 'passport__track', role: 'progressbar', 'aria-label': 'Perguntas respondidas', 'aria-valuenow': answered, 'aria-valuemin': 0, 'aria-valuemax': qs.length },
          h('div', { style: { width: `${Math.round((answered / qs.length) * 100)}%` } }))));
  };

  // Com dado salvo, o botão continua ativo mesmo com o campo vazio: é assim que se apaga uma resposta.
  function syncCta() {
    if (!cta) return;
    cta.disabled = !isAnswered(steps[idx]) && !hasSaved(steps[idx]);
    writeDraft();
  }

  const goTo = (n, { focus = true } = {}) => {
    idx = Math.max(0, Math.min(steps.length - 1, n));
    st.draft = '';
    exitArmed = false;
    const id = steps[idx].id;
    try { history.replaceState(history.state, '', `#/boas-vindas/${id}`); } catch { /* ok */ }
    enterStep();
    paint();
    if (focus) {
      const h1 = body.querySelector('h1');
      if (h1) { h1.setAttribute('tabindex', '-1'); h1.focus({ preventScroll: true }); }
      window.scrollTo(0, 0);
      announce(steps[idx].q || h1?.textContent || '');
    }
  };

  const next = () => locked(async () => {
    const cur = steps[idx];
    const form = formEl;
    clearErrors(form || body);
    if (cur.ch && cur.kind !== 'consent') {
      const errors = stepErrors(cur);
      if (check.hasErrors(errors)) { showErrors(form, errors); return; }
      try {
        await saveStep(cur);
      } catch (err) {
        if (!err.handled) showErrors(form, err);
        return;
      }
      markStep(cur);
    }
    if (cur.id === 'area') steps = buildSteps(curArea());
    const nextIdx = steps.findIndex((s) => s.id === cur.id) + 1;
    const completing = steps[nextIdx]?.kind === 'done';
    try {
      await persist(steps[nextIdx]?.id || cur.id, completing);
    } catch {
      enterStep(); // a resposta já está no perfil: tentar de novo só refaz o progresso
      showErrors(form, null, 'Sua resposta foi salva, mas não conseguimos registrar o progresso. Verifique sua conexão e tente de novo.');
      return;
    }
    if (completing) removeDraft();
    goTo(nextIdx);
  });

  // Pular descarta o que foi digitado e não salvo (nada de rascunho no passaporte ou nas sugestões). Se o
  // passo já tem dado salvo, ele continua respondido: a pendência falaria de algo que existe.
  const skip = () => locked(async () => {
    const at = idx;
    const cur = steps[at];
    const before = { draft: takeState(), ids: ids(), answered: [...st.answered], skipped: [...st.skipped] };
    Object.assign(st, JSON.parse(entry), entryIds); // inclusive o item (rascunho de item novo volta ao salvo)
    if (cur.ch) {
      // Pré-preenchido não conta (e-mail da conta, país): só o que está salvo no perfil tira da pendência.
      if (hasSaved(cur)) { st.answered.add(cur.id); st.skipped.delete(cur.id); } else { st.skipped.add(cur.id); st.answered.delete(cur.id); }
    }
    const completing = steps[at + 1]?.kind === 'done';
    try {
      await persist(steps[at + 1]?.id || cur.id, completing);
    } catch {
      Object.assign(st, JSON.parse(before.draft), before.ids, { answered: new Set(before.answered), skipped: new Set(before.skipped) });
      showErrors(formEl, null, 'Não conseguimos registrar seu progresso. Verifique sua conexão e tente de novo.');
      return;
    }
    if (completing) removeDraft();
    goTo(at + 1);
  });

  const inputFor = (s, f) => {
    const key = fieldKey(f, s);
    const id = nextId(`ob-${key}`);
    let control;
    if (f.t === 'select') {
      control = h('select', { id, name: key, class: 'zy-tf__input ob__big', 'aria-describedby': `${id}-err` }, f.options.map(([v, t]) => h('option', { value: v }, t)));
      control.value = st.a[key] || f.options[0][0];
      st.a[key] = control.value;
    } else if (f.t === 'area') {
      control = h('textarea', { id, name: key, rows: 4, class: 'zy-tf__input', placeholder: f.ph, 'aria-describedby': `${id}-err`, maxlength: f.max || 4000 });
      control.value = st.a[key] || '';
    } else {
      control = h('input', { id, name: key, type: f.type || 'text', class: 'zy-tf__input', placeholder: f.ph, value: st.a[key] || '', autocomplete: f.auto || null, inputmode: f.mode || null, maxlength: f.max || null, 'aria-describedby': `${id}-err` });
      enhanceInput(control, { name: key, mask: f.mask });
      if (key === 'endDate') control.disabled = st.current;
    }
    control.addEventListener(f.t === 'select' ? 'change' : 'input', () => {
      st.a[key] = control.value;
      syncCta();
      if (key === 'fullName') paintAside();
    });
    return h('div', { class: `zy-tf ${f.span ? 'span2' : ''}` },
      h('label', { class: 'zy-tf__label', for: id }, f.label), control, characterCounter(control), h('p', { class: 'zy-tf__error', id: `${id}-err`, hidden: true }));
  };

  const customAdder = (s, onAdd, label, ph, max) => {
    const id = nextId('ob-custom');
    const input = h('input', { id, class: 'zy-tf__input', placeholder: ph, value: st.draft, maxlength: max });
    const add = Button({ variant: 'secondary', disabled: !st.draft.trim() }, 'Adicionar');
    input.addEventListener('input', () => { st.draft = input.value; add.disabled = !input.value.trim(); });
    const doAdd = () => {
      const val = input.value.trim();
      if (!val) return;
      const typed = st.draft;
      st.draft = '';
      if (onAdd(val) === false) st.draft = typed;
    };
    input.addEventListener('keydown', (e) => { if (e.key === 'Enter') { e.preventDefault(); doAdd(); } });
    add.addEventListener('click', doAdd);
    return h('div', { class: 'row-end', style: { maxWidth: '760px' } },
      h('div', { class: 'zy-tf grow' }, h('label', { class: 'zy-tf__label', for: id }, label), input), add);
  };

  const refocus = (sel) => { const el = body.querySelector(sel); if (el) el.focus(); };

  function paint() {
    paintTop();
    paintAside();
    const s = steps[idx];
    const A = curArea();
    cta = null;
    formEl = null;
    clear(body);

    if (s.kind === 'intro') {
      body.append(h('div', { class: 'ob__intro' },
        h('div', { class: 'overline' }, 'Bem-vindo ao Zyft'),
        h('h1', { class: 'ob__hero' }, 'Seu passaporte profissional começa aqui.'),
        h('p', { class: 'lead' }, 'Uma pergunta por vez, sobre você e a sua área, seja ela qual for. Cada resposta vira um carimbo. Não sabe agora? Deixe para depois: o Zyft guarda o lembrete no seu perfil.'),
        h('div', { class: 'row' }, Button({ variant: 'primary', onClick: () => goTo(idx + 1) }, 'Começar'), h('span', { class: 'muted' }, 'Cerca de 10 minutos · pare quando quiser'))));
      return;
    }
    if (s.kind === 'start') {
      body.append(h('div', { class: 'stack-lg' },
        h('h1', { class: 'page-title' }, 'Como você quer começar?'),
        h('div', { class: 'ob__start' },
          h('a', { href: '#/importar', class: 'ob__start-card', onclick: () => persist('inicio') },
            Icon('upload', { size: 24 }), h('b', {}, 'Tenho um currículo'),
            h('span', {}, 'O Zyft lê o arquivo e você confere cada item antes de entrar no perfil. Depois, volte aqui para carimbar o resto.')),
          h('button', { type: 'button', class: 'ob__start-card', onclick: () => goTo(idx + 1) },
            Icon('edit', { size: 24 }), h('b', {}, 'Responder aqui'),
            h('span', {}, 'Uma pergunta por vez. Bom para quem não tem currículo pronto.'))),
        h('div', {}, Button({ variant: 'ghost', onClick: () => goTo(idx - 1) }, 'Voltar'))));
      return;
    }
    if (s.kind === 'done') {
      const skippedIds = steps.filter((x) => x.ch && st.skipped.has(x.id)).map((x) => x.id);
      body.append(h('div', { class: 'stack-lg', style: { maxWidth: '640px', paddingTop: '24px' } },
        h('div', { class: 'ob__done-kicker' }, 'Passaporte carimbado'),
        h('h1', { class: 'ob__hero' }, `Pronto, ${(st.a.fullName || me?.name || '').split(' ')[0] || 'tudo certo'}. Agora é com a vaga.`),
        skippedIds.length ? h('div', { class: 'ob__later' },
          h('b', {}, `${skippedIds.length} ${skippedIds.length === 1 ? 'pergunta ficou' : 'perguntas ficaram'} para depois`),
          h('div', { class: 'chips' }, skippedIds.map((id) => h('a', { class: 'tag', href: `#/boas-vindas/${id}` }, STEP_SHORT[id]))),
          h('span', { class: 'small' }, 'Elas ficam como lembrete no topo do Perfil Mestre. Responda quando quiser.')) : null,
        h('div', { class: 'row' },
          LinkButton('#/extensao', { variant: 'primary', className: 'zy-btn--lg' }, 'Conectar a extensão'),
          LinkButton('#/perfil', { variant: 'quiet', className: 'zy-btn--lg' }, 'Ver meu Perfil Mestre'))));
      return;
    }

    // Pergunta
    body.append(h('div', { class: 'ob__q' }, h('h1', {}, s.q), s.hint ? h('p', { class: 'lead' }, s.hint) : null));
    formEl = h('form', { novalidate: true, class: 'stack-lg', 'aria-label': s.q, onsubmit: (e) => { e.preventDefault(); if (!cta.disabled) next(); } });

    if (s.kind === 'text') {
      const saved = !s.addMore ? []
        : s.id === 'experiencia' ? (p.experiences || []).filter((e) => e.id !== st.expId).map((e) => `${e.role} · ${e.company}`)
          : s.id === 'formacao' ? (p.educations || []).filter((e) => e.id !== st.eduId).map((e) => `${e.course} · ${e.institution}`)
            : (p.courses || []).filter((c) => c.id !== st.courseId).map((c) => c.name);
      if (saved.length) {
        formEl.append(h('div', { class: 'ob__saved' }, saved.map((t) => h('div', { class: 'ob__saved-item' }, h('span', {}, t), h('span', { class: 'zy-stamp zy-stamp--sm' }, h('span', { class: 'zy-stamp__tick', 'aria-hidden': 'true' }, Icon('check', { size: 10 })), h('span', {}, 'Guardado'))))));
      }
      if (s.id === 'diaadia' && !st.expId) {
        formEl.append(h('p', { class: 'notice notice--care' }, Icon('alert', { size: 14 }), 'Para guardar este texto, cadastre uma experiência na pergunta anterior. Se preferir, deixe para depois.'));
      }
      formEl.append(h('div', { class: 'ob__fields' }, s.fields.map((f) => inputFor(s, f))));
      if (s.exp) {
        const cb = h('input', { type: 'checkbox', id: 'ob-current', checked: st.current });
        cb.addEventListener('change', () => {
          st.current = cb.checked;
          const end = formEl.querySelector('[name="endDate"]');
          if (end) { end.disabled = cb.checked; if (cb.checked) { end.value = ''; st.a.endDate = ''; } }
        });
        formEl.append(h('label', { class: 'check row', for: 'ob-current' }, cb, 'Trabalho aqui atualmente'));
      }
      if (s.addMore) {
        const more = Button({ variant: 'quiet', size: 'sm', icon: 'plus', dataset: { lock: '1' } }, s.addMore);
        more.addEventListener('click', () => locked(async () => {
          if (!isAnswered(s)) { toast('Preencha esta primeiro. A próxima você cadastra em seguida.'); return; }
          const errors = stepErrors(s);
          if (check.hasErrors(errors)) { showErrors(formEl, errors); return; }
          try {
            await SAVE[s.id]();
          } catch (err) {
            if (!err.handled) showErrors(formEl, err);
            return;
          }
          st.answered.add(s.id); st.skipped.delete(s.id);
          // O item novo começa do zero: nada do anterior passa para ele.
          if (s.id === 'experiencia') { st.lastSaved[s.id] = st.expId; st.expId = null; Object.assign(st.a, { company: '', role: '', startDate: '', endDate: '', activities: '', results: '' }); st.m.tools = []; st.current = false; }
          if (s.id === 'formacao') { st.lastSaved[s.id] = st.eduId; st.eduId = null; Object.assign(st.a, { institution: '', course: '', eduStart: '', eduEnd: '' }); }
          if (s.id === 'cursos') { st.lastSaved[s.id] = st.courseId; st.courseId = null; Object.assign(st.a, { courseName: '', issuer: '', hours: '' }); }
          toast('Guardado no seu perfil. Preencha o próximo ou continue.', 'ok');
          enterStep();
          paint();
          refocus('input');
        }));
        formEl.append(h('div', { class: 'row' }, more, h('span', { class: 'small muted' }, 'Os outros você também pode cadastrar depois no Perfil Mestre.')));
      }
    }

    if (s.kind === 'single') {
      const options = s.key === 'area' ? areas.map((x) => [x.name, '']) : s.options;
      const group = h('div', { class: 'ob__options', role: 'radiogroup', 'aria-label': s.q });
      options.forEach(([label, sub], i) => {
        const on = st.a[s.key] === label;
        const btn = h('button', { type: 'button', role: 'radio', class: 'ob__option', 'aria-checked': String(on), tabindex: on || (!st.a[s.key] && i === 0) ? '0' : '-1', dataset: { v: label } },
          h('span', { class: 'ob__radio', 'aria-hidden': 'true' }, h('span', {})),
          h('span', {}, h('span', { class: 'ob__opt-label' }, label), sub ? h('span', { class: 'ob__opt-sub' }, sub) : null));
        btn.addEventListener('click', () => {
          st.a[s.key] = label;
          paint();
          refocus(`.ob__option[data-v="${CSS.escape(label)}"]`);
        });
        group.append(btn);
      });
      radioKeys(group);
      formEl.append(group);
      if (s.key === 'area' && st.a.area === OTHER) {
        const id = nextId('ob-other');
        const input = h('input', { id, class: 'zy-tf__input ob__big', placeholder: 'Ex.: Gastronomia, Agronomia, Estética', value: st.a.otherArea || '', maxlength: 80 });
        input.addEventListener('input', () => { st.a.otherArea = input.value; syncCta(); paintAside(); });
        formEl.append(h('div', { class: 'zy-tf', style: { maxWidth: '420px' } }, h('label', { class: 'zy-tf__label', for: id }, 'Qual é a sua área?'), input));
      }
    }

    if (s.kind === 'multi') {
      const sel = st.m[s.key] || [];
      const customs = st.custom[s.key] || [];
      const all = [...(s.options || []), ...sel, ...customs].filter((o, i, list) => list.findIndex((x) => sameText(x, o)) === i);
      const areaLabel = st.a.area === OTHER ? (st.a.otherArea || 'a sua área') : (st.a.area || 'a sua área');
      if (!s.noSuggestTitle && (s.options || []).length) formEl.append(h('div', { class: 'suggest__label' }, `Sugestões para ${areaLabel}`));
      formEl.append(h('div', { class: 'chips', role: 'group', 'aria-label': s.q }, all.map((o) => {
        const on = sel.some((x) => sameText(x, o));
        const b = Chip({ label: o, pressed: on, size: 'lg', mine: !(s.options || []).includes(o), onClick: () => {
          st.m[s.key] = on ? sel.filter((x) => !sameText(x, o)) : [...sel, o];
          paint();
          refocus(`.chip[data-v="${CSS.escape(o)}"]`);
        } });
        b.dataset.v = o;
        return b;
      })));
      formEl.append(customAdder(s, (val) => {
        // Listas guardadas como "a, b": cada parte vira um chip, para a tela mostrar o que volta ao recarregar.
        const parts = (LIST_FIELD[s.key] ? splitList(val) : [val]).map((x) => (s.options || []).find((o) => sameText(o, x)) || x);
        const nextSel = [...sel];
        for (const x of parts) if (!nextSel.some((y) => sameText(y, x))) nextSel.push(x);
        if (!listFits(s.key, nextSel)) { showErrors(formEl, null, listTooLong(s.key)); return false; }
        st.custom[s.key] = [...customs, ...parts];
        st.m[s.key] = nextSel;
        paint();
        refocus('.row-end input');
        announce(`${parts.join(', ')} adicionado.`);
        return true;
        // Sem experiência cadastrada, as ferramentas viram competências (limite de 100).
      }, s.customLabel, s.customPh, s.key === 'tools' && !st.expId ? CUSTOM_MAX.skills : CUSTOM_MAX[s.key]));
    }

    if (s.kind === 'rows') {
      const rows = s.langs
        ? st.langs.map((l, i) => ({ label: l.name, value: l.level, pills: LEVELS, pick: (v) => { st.langs[i].level = v; }, remove: !l.id && !DEFAULT_LANGS.includes(l.name) ? () => { st.langs.splice(i, 1); paint(); refocus('.row-end input'); } : null }))
        : s.rows.map(([key, label, pills]) => ({ label, value: st.a[key], pills, pick: (v) => { st.a[key] = v; } }));
      formEl.append(h('div', { class: 'ob__rows' }, rows.map((r, ri) => {
        const group = h('div', { class: 'row', role: 'radiogroup', 'aria-labelledby': `ob-row-${ri}` }, r.pills.map((pv, pi) => {
          const b = h('button', { type: 'button', role: 'radio', class: 'pill', 'aria-checked': String(r.value === pv), tabindex: r.value === pv || (!r.value && pi === 0) ? '0' : '-1', dataset: { k: `${ri}-${pv}` } }, pv);
          b.addEventListener('click', () => { r.pick(pv); paint(); refocus(`.pill[data-k="${CSS.escape(`${ri}-${pv}`)}"]`); });
          return b;
        }));
        radioKeys(group);
        return h('div', { class: 'ob__row' },
          h('span', { class: 'ob__row-label', id: `ob-row-${ri}` }, r.label),
          group,
          r.remove ? Button({ variant: 'ghost', size: 'sm', 'aria-label': `Remover ${r.label}`, onClick: r.remove }, 'Remover') : null);
      })));
      if (s.langs) {
        formEl.append(customAdder(s, (val) => {
          if (!st.langs.some((l) => l.name.toLowerCase() === val.toLowerCase())) st.langs.push({ name: val, level: '' });
          paint();
          refocus('.row-end input');
        }, 'Outro idioma', 'Ex.: Libras, Italiano', 60));
      }
    }

    if (s.kind === 'consent') {
      formEl.append(h('div', { class: 'ob__consents' }, CONSENTS.map((c) => ConsentRow({
        title: c.title, purpose: c.purpose, basis: c.basis, checked: st.consents[c.key],
        onChange: async (nextVal, sw) => {
          sw.disabled = true;
          try {
            await api(`/consents/${c.key}`, { method: 'PUT', body: { granted: nextVal } });
            st.consents[c.key] = nextVal;
            sw.setChecked(nextVal);
            announce(`${c.title}: ${nextVal ? 'ligado' : 'desligado'}.`);
          } catch (err) { if (!err.handled) toast(err.message, 'err'); } finally { sw.disabled = false; sw.focus?.(); }
        },
      }))));
    }

    cta = Button({ variant: 'primary', type: 'submit' }, s.kind === 'consent' ? 'Carimbar e concluir' : 'Carimbar e continuar');
    formEl.append(h('div', { class: 'ob__actions' },
      cta,
      Button({ variant: 'quiet', onClick: skip }, 'Preencher mais tarde'),
      Button({ variant: 'ghost', onClick: () => goTo(idx - 1) }, 'Voltar')));
    body.append(formEl);
    syncCta();
  }

  enterStep();
  restoreDraft();
  paint();
}
