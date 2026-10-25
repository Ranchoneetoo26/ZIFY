// Onboarding em passos ("passaporte"), contrato v2. Uma pergunta por tela; toda pergunta pode
// ficar para depois. As respostas vão para os endpoints do perfil; /onboarding guarda só o progresso.
// Sem anúncios nesta tela.
import { api, asList } from '../api.js';
import { h, clear, announce, toast, showErrors, clearErrors, go, nextId } from '../dom.js';
import { Button, LinkButton, Icon, Logo, Chip, ConsentRow, initials } from '../ui.js';
import { loadAreas, areaOrOther, findArea, OTHER } from '../catalog.js';
import { getProfile, saveBasics, getOnboarding, saveOnboarding, splitList, joinList, toUrl } from '../store.js';
import { EDUCATION_LEVELS, EDUCATION_STATUS, COURSE_TYPES, PROFICIENCY, enumName } from './profile-schema.js';
import { normalizeDate } from '../validate.js';

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

const T = (key, label, ph, opts = {}) => ({ t: 'input', key, label, ph: ph || '', ...opts });
const S = (key, label, options, opts = {}) => ({ t: 'select', key, label, options, ...opts });
const X = (key, label, ph) => ({ t: 'area', key, label, ph, span: true });
const opts = (list) => list.filter(([v]) => v !== '');

function buildSteps(A) {
  const list = [
    { id: 'intro', kind: 'intro' },
    { id: 'inicio', kind: 'start' },
    { id: 'nome', ch: 1, kind: 'text', q: 'Como você se chama?', hint: 'É assim que o seu nome vai aparecer nos formulários.', fields: [T('fullName', 'Nome completo', 'Nome e sobrenome', { span: true, auto: 'name' })] },
    { id: 'nascimento', ch: 1, kind: 'text', q: 'Quando você nasceu?', hint: 'Muitos formulários pedem. Fica só no seu perfil.', fields: [T('birthDate', 'Data de nascimento', '', { type: 'date', auto: 'bday' })] },
    { id: 'contato', ch: 1, kind: 'text', q: 'Como as empresas falam com você?', hint: 'Use o telefone e o e-mail que você confere todo dia.', fields: [T('phone', 'Telefone com DDD', '(00) 00000-0000', { type: 'tel', auto: 'tel' }), T('email', 'E-mail de contato', 'voce@email.com', { type: 'email', auto: 'email' })] },
    { id: 'endereco', ch: 1, kind: 'text', q: 'Onde você mora?', hint: 'Só cidade e CEP. Dados que você considera sensíveis ficam trancados em Dados sensíveis, no perfil.', fields: [T('city', 'Cidade', 'Ex.: Belo Horizonte', { auto: 'address-level2' }), T('state', 'Estado (UF)', 'Ex.: MG', { auto: 'address-level1' }), T('country', 'País', '', { auto: 'country-name' }), T('zipCode', 'CEP', '00000-000', { auto: 'postal-code', mode: 'numeric' })] },
    { id: 'area', ch: 2, kind: 'single', key: 'area', q: 'Em que área você trabalha ou quer trabalhar?', hint: 'As próximas perguntas e sugestões se adaptam à sua área.' },
    { id: 'cargos', ch: 2, kind: 'multi', key: 'roles', q: 'Que vagas você está buscando?', hint: 'Escolha quantas quiser ou escreva o nome do cargo do seu jeito.', options: A.roles, customLabel: 'Outro cargo', customPh: 'Ex.: Cuidador(a) de idosos' },
    { id: 'setores', ch: 2, kind: 'multi', key: 'sectors', q: 'Em que tipo de empresa ou setor?', hint: 'Ajuda a extensão quando o formulário pergunta por área de interesse.', options: A.sectors, customLabel: 'Outro setor', customPh: 'Ex.: Hotelaria' },
  ];
  if (A.registration) list.push({ id: 'registro', ch: 2, kind: 'text', q: 'Você tem registro ou certificação obrigatória?', hint: 'Na sua área, os formulários costumam pedir.', fields: [T('professionalRegistration', A.registration.label, A.registration.placeholder, { span: true })] });
  const links = [T('linkedinUrl', 'LinkedIn', 'linkedin.com/in/…', { type: 'url' })];
  if (A.linkLabel) links.push(T('link2', A.linkLabel, '', { type: 'url' }));
  links.push(T('portfolioUrl', 'Portfólio ou site', 'Opcional', { span: true, type: 'url' }));
  list.push(
    { id: 'links', ch: 2, kind: 'text', q: 'Onde as empresas podem ver seu trabalho?', hint: 'Coloque só os que você mantém atualizados.', fields: links },
    { id: 'nivel', ch: 3, kind: 'single', key: 'level', q: 'Em que momento da carreira você está?', hint: 'Vale para qualquer área.', options: [['Primeiro emprego', 'Ainda sem experiência formal'], ['Estágio ou aprendiz', 'Estudando e trabalhando'], ['Júnior ou assistente', 'Até 2 anos'], ['Pleno ou analista', 'De 2 a 5 anos'], ['Sênior', 'Mais de 5 anos'], ['Liderança', 'Coordena pessoas ou equipes']] },
    { id: 'modelo', ch: 3, kind: 'single', key: 'mode', q: 'Como você prefere trabalhar?', hint: 'Escolha o que mais combina.', options: [['Presencial', 'No local todos os dias'], ['Híbrido', 'Alguns dias no local'], ['Remoto', 'De casa'], ['Tanto faz', 'Depende da vaga']] },
    { id: 'local', ch: 3, kind: 'text', q: 'Onde você aceita trabalhar?', hint: 'Cidades, regiões ou "qualquer lugar".', fields: [T('locationPreference', 'Localidades', 'Ex.: Belo Horizonte e região', { span: true })] },
    { id: 'contrato', ch: 3, kind: 'multi', key: 'contracts', q: 'Que tipo de contrato você aceita?', hint: 'Marque todos que servem.', options: ['CLT', 'PJ', 'Estágio', 'Jovem aprendiz', 'Temporário', 'Freelancer', 'Plantão / escala'], customLabel: 'Outro tipo', customPh: 'Ex.: Cooperado', noSuggestTitle: true },
    { id: 'disponibilidade', ch: 3, kind: 'single', key: 'avail', q: 'Quando você pode começar?', hint: '', options: [['Imediatamente', ''], ['Em 15 dias', ''], ['Em 30 dias', ''], ['A combinar', '']] },
    { id: 'salario', ch: 3, kind: 'text', q: 'Quanto você quer ganhar?', hint: 'Fica só no seu perfil e só vai para um formulário se você aprovar. Pode deixar para depois.', fields: [T('salaryExpectation', 'Valor mensal', 'R$ 0,00', { mode: 'decimal' }), S('salaryType', 'Tipo', [['CLT (bruto)', 'CLT (bruto)'], ['PJ', 'PJ'], ['Por hora', 'Por hora'], ['Por plantão', 'Por plantão']])] },
    { id: 'mobilidade', ch: 3, kind: 'rows', q: 'E sobre deslocamento?', hint: 'Três respostas rápidas.', rows: [['travel', 'Topa viajar a trabalho?', ['Sim', 'Às vezes', 'Não']], ['move', 'Mudaria de cidade?', ['Sim', 'Talvez', 'Não']], ['cnh', 'Tem CNH?', ['Não tenho', 'A', 'B', 'AB', 'C', 'D', 'E']]] },
    { id: 'experiencia', ch: 4, kind: 'text', exp: true, addMore: 'Adicionar outra experiência', q: 'Onde você trabalha ou trabalhou por último?', hint: 'Vale estágio, freela, trabalho voluntário ou negócio próprio.', fields: [T('company', 'Empresa ou lugar', 'Ex.: Hospital Santa Clara'), T('role', 'Cargo ou função', A.roles[0] ? `Ex.: ${A.roles[0]}` : ''), T('startDate', 'Início', 'aaaa-mm', { type: 'month' }), T('endDate', 'Fim', 'aaaa-mm', { type: 'month' })] },
    { id: 'diaadia', ch: 4, kind: 'text', q: 'O que você fazia lá, e o que deu certo?', hint: 'Números ajudam: pessoas atendidas, metas, prazos. Escreva como você falaria.', fields: [X('activities', 'No dia a dia', 'Ex.: Atendia 30 pacientes por plantão…'), X('results', 'Um resultado de que você se orgulha', 'Ex.: Reduzi o tempo de triagem…')] },
    { id: 'ferramentas', ch: 4, kind: 'multi', key: 'tools', q: 'Que ferramentas, sistemas ou equipamentos você usa?', hint: 'Pode ser software, máquina, instrumento ou método.', options: A.tools, customLabel: 'Outra ferramenta', customPh: 'Ex.: Glicosímetro, Empilhadeira elétrica' },
    { id: 'formacao', ch: 4, kind: 'text', addMore: 'Adicionar outra formação', q: 'Qual a sua formação principal?', hint: 'A mais importante para as vagas que você busca.', fields: [T('institution', 'Instituição', 'Ex.: Senac, UFMG, escola'), T('course', 'Curso', 'Ex.: Técnico em Enfermagem'), S('level', 'Nível', opts(EDUCATION_LEVELS)), S('status', 'Situação', opts(EDUCATION_STATUS)), T('eduStart', 'Início', 'aaaa-mm', { type: 'month' }), T('eduEnd', 'Conclusão ou previsão', 'aaaa-mm', { type: 'month' })] },
    { id: 'cursos', ch: 4, kind: 'text', addMore: 'Adicionar outro curso', q: 'Fez algum curso ou certificação que vale citar?', hint: 'Cursos livres contam.', fields: [T('courseName', 'Nome do curso', 'Ex.: Suporte Básico de Vida'), S('courseType', 'Tipo', COURSE_TYPES), T('issuer', 'Quem emitiu', ''), T('hours', 'Carga horária (horas)', 'Ex.: 16', { mode: 'numeric' })] },
    { id: 'competencias', ch: 4, kind: 'multi', key: 'skills', q: 'O que você sabe fazer bem?', hint: 'Toque nas sugestões ou cadastre as suas. O nível você ajusta depois no perfil.', options: A.skills, customLabel: 'Outra competência', customPh: 'Escreva do seu jeito' },
    { id: 'idiomas', ch: 4, kind: 'rows', langs: true, q: 'Que idiomas você fala?', hint: 'Marque o nível de cada um. Libras também conta.' },
    { id: 'titulo', ch: 5, kind: 'text', q: 'Como você se apresenta em uma linha?', hint: 'Aparece no topo dos formulários que pedem "título" ou "cargo atual".', fields: [T('headline', 'Título', 'Ex.: Técnica de enfermagem com foco em UTI', { span: true })] },
    { id: 'resumo', ch: 5, kind: 'text', q: 'Resuma sua trajetória em poucas frases.', hint: 'Três ou quatro frases. Você pode pedir um rascunho à IA depois, no perfil.', fields: [X('summary', 'Resumo', '')] },
    { id: 'combinados', ch: 6, kind: 'consent', q: 'Por último: o que o Zyft pode fazer por você?', hint: 'Você muda isso quando quiser em Conta e privacidade.' },
    { id: 'fim', kind: 'done' },
  );
  return list;
}

const parseMoney = (s) => {
  if (s === null || s === undefined || String(s).trim() === '') return null;
  let t = String(s).replace(/[^\d.,]/g, '');
  if (t.includes(',')) t = t.replace(/\./g, '').replace(',', '.');
  const n = Number(t);
  return Number.isFinite(n) ? n : NaN;
};
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
    consents: {}, draft: '', expId: null, eduId: null, courseId: null, current: true,
    savedExtra: { experiencia: [], formacao: [], cursos: [] },
  };
  for (const c of CONSENTS) {
    const found = consentsRaw.find((x) => String(x.purpose).toLowerCase() === c.key.toLowerCase());
    st.consents[c.key] = Boolean(found?.granted);
  }

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
      travel: boolTo(p.availableToTravel, 'Sim', 'Não'), move: boolTo(p.availableToRelocate, 'Sim', 'Não'),
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
    const exp = (p.experiences || [])[0];
    if (exp) {
      st.expId = exp.id;
      Object.assign(a, { company: exp.company, role: exp.role, startDate: month(exp.startDate), endDate: month(exp.endDate), activities: exp.activities || '', results: exp.results || '' });
      st.current = Boolean(exp.isCurrent);
      st.m.tools = splitList(exp.tools);
    }
    const edu = (p.educations || [])[0];
    if (edu) { st.eduId = edu.id; Object.assign(a, { institution: edu.institution, course: edu.course, level: a.level, eduLevel: enumName(EDUCATION_LEVELS, edu.level), eduStatus: enumName(EDUCATION_STATUS, edu.status), eduStart: month(edu.startDate), eduEnd: month(edu.endDate) }); }
    const course = (p.courses || [])[0];
    if (course) { st.courseId = course.id; Object.assign(a, { courseName: course.name, courseType: enumName(COURSE_TYPES, course.type), issuer: course.institution || '', hours: course.hours ? String(course.hours) : '' }); }
    const langs = (p.languages || []).map((l) => ({ id: l.id, name: l.name, level: LEVELS[['Basic', 'Intermediate', 'Advanced', 'Fluent', 'Native'].indexOf(enumName(PROFICIENCY, l.proficiency))] || '' }));
    for (const def of ['Português', 'Inglês', 'Espanhol']) if (!langs.some((l) => l.name.toLowerCase() === def.toLowerCase())) langs.push({ name: def, level: '' });
    st.langs = langs;
    const A = curArea();
    if (A.linkLabel === 'GitHub') a.link2 = p.githubUrl || '';
    else if (A.linkLabel) a.link2 = customs.find((c) => c.label === A.linkLabel)?.value || '';
  };
  // Campos de formação/curso usam chaves próprias para não colidir com "nível" da carreira.
  const curArea = () => (st.a.area && st.a.area !== OTHER ? areaOrOther(areas, st.a.area) : areaOrOther(areas, OTHER));
  prefill();
  st.a.eduLevel ||= 'Technical';
  st.a.eduStatus ||= 'Completed';
  st.a.courseType ||= 'Course';

  let steps = buildSteps(curArea());
  const idxOf = (id) => Math.max(0, steps.findIndex((s) => s.id === id));
  let idx = startAt ? idxOf(startAt) : ob.lastStep ? idxOf(ob.lastStep) : 0;
  if (ob.completedAt && !startAt) idx = steps.length - 1;
  if (startAt && !steps.some((s) => s.id === startAt)) idx = 0;

  const questions = () => steps.filter((s) => s.ch && s.kind !== 'consent');
  const fieldKey = (f, s) => (s.id === 'formacao' && f.key === 'level' ? 'eduLevel' : s.id === 'formacao' && f.key === 'status' ? 'eduStatus' : f.key);

  // ---------- Layout fixo ----------
  const top = h('header', { class: 'ob__top' });
  const body = h('section', { class: 'ob__main', 'aria-live': 'off' });
  const aside = h('aside', { class: 'passport', 'aria-label': 'Seu passaporte' });
  main.append(h('div', { class: 'ob' }, top, h('div', { class: 'ob__body' }, body, aside)));

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

  const SAVE = {
    nome: () => basics({ fullName: st.a.fullName?.trim() || null }),
    nascimento: () => basics({ birthDate: st.a.birthDate || null }),
    contato: () => basics({ phone: st.a.phone?.trim() || null, email: st.a.email?.trim() || null }),
    endereco: () => basics({ city: st.a.city?.trim() || null, state: st.a.state?.trim() || null, country: st.a.country?.trim() || null, zipCode: st.a.zipCode?.trim() || null }),
    area: () => basics({ workArea: st.a.area === OTHER ? (st.a.otherArea?.trim() || null) : st.a.area }),
    cargos: () => basics({ desiredRoles: joinList(st.m.roles) }),
    setores: () => basics({ areas: joinList(st.m.sectors) }),
    registro: () => basics({ professionalRegistration: st.a.professionalRegistration?.trim() || null }),
    links: async () => {
      const A = curArea();
      const patch = { linkedinUrl: toUrl(st.a.linkedinUrl), portfolioUrl: toUrl(st.a.portfolioUrl) };
      if (A.linkLabel === 'GitHub') patch.githubUrl = toUrl(st.a.link2);
      await basics(patch);
      if (A.linkLabel && A.linkLabel !== 'GitHub' && st.a.link2?.trim()) await upsertCustom(A.linkLabel, toUrl(st.a.link2));
    },
    nivel: () => basics({ seniority: st.a.level || null }),
    modelo: () => basics({ workModel: MODEL[st.a.mode] || null }),
    local: () => basics({ locationPreference: st.a.locationPreference?.trim() || null }),
    contrato: () => basics({ contractTypes: joinList(st.m.contracts) }),
    disponibilidade: () => basics({ availability: st.a.avail || null }),
    salario: () => {
      const n = parseMoney(st.a.salaryExpectation);
      if (Number.isNaN(n)) throw Object.assign(new Error('Informe só o valor, por exemplo 4.500,00.'), { errors: { salaryExpectation: ['Informe só o valor, por exemplo 4.500,00.'] } });
      return basics({ salaryExpectation: n, salaryType: st.a.salaryType || null });
    },
    mobilidade: () => {
      const a = st.a;
      const patch = {
        availableToTravel: a.travel ? a.travel !== 'Não' : p.availableToTravel ?? null,
        availableToRelocate: a.move === 'Sim' ? true : a.move === 'Não' ? false : (a.move === 'Talvez' ? null : p.availableToRelocate ?? null),
      };
      if (a.cnh) { patch.hasDriverLicense = a.cnh !== 'Não tenho'; patch.driverLicenseCategory = a.cnh === 'Não tenho' ? null : a.cnh; }
      return basics(patch);
    },
    experiencia: saveExperience,
    diaadia: () => patchExperience({ activities: st.a.activities?.trim() || null, results: st.a.results?.trim() || null }),
    ferramentas: async () => {
      if (await patchExperience({ tools: joinList(st.m.tools) })) return;
      const have = new Set((p.skills || []).map((s) => s.name.toLowerCase()));
      for (const t of st.m.tools) if (!have.has(t.toLowerCase())) await api('/profile/skills', { method: 'POST', body: { name: t, level: null } });
      await refreshLists();
    },
    formacao: saveEducation,
    cursos: saveCourse,
    competencias: async () => {
      const existing = p.skills || [];
      const sel = new Set(st.m.skills.map((s) => s.toLowerCase()));
      const have = new Set(existing.map((s) => s.name.toLowerCase()));
      for (const s of st.m.skills) if (!have.has(s.toLowerCase())) await api('/profile/skills', { method: 'POST', body: { name: s, level: null } });
      for (const s of existing) if (!sel.has(s.name.toLowerCase())) await api(`/profile/skills/${s.id}`, { method: 'DELETE' });
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
    titulo: () => basics({ headline: st.a.headline?.trim() || null }),
    resumo: () => basics({ summary: st.a.summary?.trim() || null }),
  };

  const persist = async (lastStep, completed = false) => {
    try {
      await saveOnboarding({ answered: [...st.answered], skipped: [...st.skipped], lastStep, completedAt: st.completedAt }, completed);
      if (completed) st.completedAt ||= new Date().toISOString();
    } catch { /* progresso é conveniência: não trava a pessoa */ }
  };

  // ---------- Renderização ----------
  let formEl = null;
  let cta = null;

  const paintTop = () => {
    const cur = steps[idx];
    const qs = questions();
    const qNum = qs.indexOf(cur) + 1;
    clear(top,
      h('a', { href: '#/perfil', 'aria-label': 'Zyft, ir para o Perfil Mestre' }, Logo()),
      cur.q ? h('div', { class: 'ob__progress' },
        h('div', { class: 'ob__progress-meta' }, h('span', {}, cur.ch ? `Capítulo ${cur.ch} · ${CHAPTERS[cur.ch]}` : ''), h('span', {}, `Pergunta ${qNum || qs.length} de ${qs.length}`)),
        h('div', { class: 'ob__segs', 'aria-hidden': 'true' }, [1, 2, 3, 4, 5, 6].map((c) => {
          const inCh = steps.filter((s) => s.ch === c);
          const seen = inCh.filter((s) => steps.indexOf(s) < idx).length;
          return h('span', { class: `ob__seg ${c === cur.ch ? 'is-current' : ''}` }, h('span', { style: { width: `${Math.round((seen / Math.max(1, inCh.length)) * 100)}%` } }));
        }))) : null,
      h('a', { href: '#/perfil', class: 'ob__exit', onclick: () => { persist(steps[idx].id); } }, 'Sair e continuar depois'));
  };

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

  const syncCta = () => { if (cta) cta.disabled = !isAnswered(steps[idx]); };

  const goTo = (n, { focus = true } = {}) => {
    idx = Math.max(0, Math.min(steps.length - 1, n));
    st.draft = '';
    const id = steps[idx].id;
    try { history.replaceState(null, '', `#/boas-vindas/${id}`); } catch { /* ok */ }
    paint();
    if (focus) {
      const h1 = body.querySelector('h1');
      if (h1) { h1.setAttribute('tabindex', '-1'); h1.focus({ preventScroll: true }); }
      window.scrollTo(0, 0);
      announce(steps[idx].q || h1?.textContent || '');
    }
  };

  const next = async () => {
    const cur = steps[idx];
    clearErrors(formEl || body);
    if (cur.ch && cur.kind !== 'consent') {
      cta.disabled = true;
      try {
        await SAVE[cur.id]?.();
      } catch (err) {
        cta.disabled = false;
        if (err.handled) return;
        showErrors(formEl, err);
        return;
      }
      st.answered.add(cur.id);
      st.skipped.delete(cur.id);
    }
    if (cur.id === 'area') steps = buildSteps(curArea());
    const nextIdx = steps.findIndex((s) => s.id === cur.id) + 1;
    const completing = steps[nextIdx]?.kind === 'done';
    await persist(steps[nextIdx]?.id || cur.id, completing);
    goTo(nextIdx);
  };

  const skip = async () => {
    const cur = steps[idx];
    if (cur.ch) { st.skipped.add(cur.id); st.answered.delete(cur.id); }
    const completing = steps[idx + 1]?.kind === 'done';
    await persist(steps[idx + 1]?.id || cur.id, completing);
    goTo(idx + 1);
  };

  const inputFor = (s, f) => {
    const key = fieldKey(f, s);
    const id = nextId(`ob-${key}`);
    let control;
    if (f.t === 'select') {
      control = h('select', { id, name: key, class: 'zy-tf__input ob__big', 'aria-describedby': `${id}-err` }, f.options.map(([v, t]) => h('option', { value: v }, t)));
      control.value = st.a[key] || f.options[0][0];
      st.a[key] = control.value;
    } else if (f.t === 'area') {
      control = h('textarea', { id, name: key, rows: 4, class: 'zy-tf__input', placeholder: f.ph, 'aria-describedby': `${id}-err`, maxlength: 4000 });
      control.value = st.a[key] || '';
    } else {
      control = h('input', { id, name: key, type: f.type || 'text', class: 'zy-tf__input', placeholder: f.ph, value: st.a[key] || '', autocomplete: f.auto || null, inputmode: f.mode || null, 'aria-describedby': `${id}-err` });
      if (key === 'endDate') control.disabled = st.current;
    }
    control.addEventListener(f.t === 'select' ? 'change' : 'input', () => {
      st.a[key] = control.value;
      syncCta();
      if (key === 'fullName') paintAside();
    });
    return h('div', { class: `zy-tf ${f.span ? 'span2' : ''}` },
      h('label', { class: 'zy-tf__label', for: id }, f.label), control, h('p', { class: 'zy-tf__error', id: `${id}-err`, hidden: true }));
  };

  const customAdder = (s, onAdd, label, ph) => {
    const id = nextId('ob-custom');
    const input = h('input', { id, class: 'zy-tf__input', placeholder: ph, value: st.draft, maxlength: 100 });
    const add = Button({ variant: 'secondary', disabled: !st.draft.trim() }, 'Adicionar');
    input.addEventListener('input', () => { st.draft = input.value; add.disabled = !input.value.trim(); });
    const doAdd = () => { const val = input.value.trim(); if (!val) return; st.draft = ''; onAdd(val); };
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
      if (s.addMore && st.savedExtra[s.id].length) {
        formEl.append(h('div', { class: 'ob__saved' }, st.savedExtra[s.id].map((t) => h('div', { class: 'ob__saved-item' }, h('span', {}, t), h('span', { class: 'zy-stamp zy-stamp--sm' }, h('span', { class: 'zy-stamp__tick', 'aria-hidden': 'true' }, Icon('check', { size: 10 })), h('span', {}, 'Guardado'))))));
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
        const more = Button({ variant: 'quiet', size: 'sm', icon: 'plus' }, s.addMore);
        more.addEventListener('click', async () => {
          if (!isAnswered(s)) { toast('Preencha esta primeiro. A próxima você cadastra em seguida.'); return; }
          more.disabled = true;
          try {
            await SAVE[s.id]();
            const label = s.id === 'experiencia' ? `${st.a.role} · ${st.a.company}` : s.id === 'formacao' ? `${st.a.course} · ${st.a.institution}` : st.a.courseName;
            st.savedExtra[s.id].push(label);
            st.answered.add(s.id); st.skipped.delete(s.id);
            if (s.id === 'experiencia') { st.expId = null; Object.assign(st.a, { company: '', role: '', startDate: '', endDate: '' }); st.current = false; }
            if (s.id === 'formacao') { st.eduId = null; Object.assign(st.a, { institution: '', course: '', eduStart: '', eduEnd: '' }); }
            if (s.id === 'cursos') { st.courseId = null; Object.assign(st.a, { courseName: '', issuer: '', hours: '' }); }
            toast('Guardado no seu perfil. Preencha o próximo ou continue.', 'ok');
            paint();
            refocus('input');
          } catch (err) {
            more.disabled = false;
            if (!err.handled) showErrors(formEl, err);
          }
        });
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
      group.addEventListener('keydown', (e) => {
        const items = [...group.querySelectorAll('[role="radio"]')];
        const i = items.indexOf(document.activeElement);
        if (i < 0) return;
        const d = e.key === 'ArrowDown' || e.key === 'ArrowRight' ? 1 : e.key === 'ArrowUp' || e.key === 'ArrowLeft' ? -1 : 0;
        if (!d) return;
        e.preventDefault();
        items[(i + d + items.length) % items.length].click();
      });
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
      const all = [...new Set([...(s.options || []), ...sel, ...customs])];
      const areaLabel = st.a.area === OTHER ? (st.a.otherArea || 'a sua área') : st.a.area;
      if (!s.noSuggestTitle && (s.options || []).length) formEl.append(h('div', { class: 'suggest__label' }, `Sugestões para ${areaLabel}`));
      formEl.append(h('div', { class: 'chips', role: 'group', 'aria-label': s.q }, all.map((o) => {
        const on = sel.includes(o);
        const b = Chip({ label: o, pressed: on, size: 'lg', mine: !(s.options || []).includes(o), onClick: () => {
          st.m[s.key] = on ? sel.filter((x) => x !== o) : [...sel, o];
          paint();
          refocus(`.chip[data-v="${CSS.escape(o)}"]`);
        } });
        b.dataset.v = o;
        return b;
      })));
      formEl.append(customAdder(s, (val) => {
        st.custom[s.key] = [...customs, val];
        if (!sel.some((x) => x.toLowerCase() === val.toLowerCase())) st.m[s.key] = [...sel, val];
        paint();
        refocus('.row-end input');
        announce(`${val} adicionado.`);
      }, s.customLabel, s.customPh));
    }

    if (s.kind === 'rows') {
      const rows = s.langs
        ? st.langs.map((l, i) => ({ label: l.name, value: l.level, pills: LEVELS, pick: (v) => { st.langs[i].level = v; } }))
        : s.rows.map(([key, label, pills]) => ({ label, value: st.a[key], pills, pick: (v) => { st.a[key] = v; } }));
      formEl.append(h('div', { class: 'ob__rows' }, rows.map((r, ri) => h('div', { class: 'ob__row' },
        h('span', { class: 'ob__row-label', id: `ob-row-${ri}` }, r.label),
        h('div', { class: 'row', role: 'radiogroup', 'aria-labelledby': `ob-row-${ri}` }, r.pills.map((pv) => {
          const b = h('button', { type: 'button', role: 'radio', class: 'pill', 'aria-checked': String(r.value === pv), dataset: { k: `${ri}-${pv}` } }, pv);
          b.addEventListener('click', () => { r.pick(pv); paint(); refocus(`.pill[data-k="${CSS.escape(`${ri}-${pv}`)}"]`); });
          return b;
        }))))));
      if (s.langs) {
        formEl.append(customAdder(s, (val) => {
          if (!st.langs.some((l) => l.name.toLowerCase() === val.toLowerCase())) st.langs.push({ name: val, level: '' });
          paint();
          refocus('.row-end input');
        }, 'Outro idioma', 'Ex.: Libras, Italiano'));
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
          } catch (err) { if (!err.handled) toast(err.message, 'err'); } finally { sw.disabled = false; }
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

  // Se a pessoa entrou direto num passo pulado, o capítulo da área muda os passos: recalcula.
  if (startAt) idx = idxOf(startAt);
  paint();
}
