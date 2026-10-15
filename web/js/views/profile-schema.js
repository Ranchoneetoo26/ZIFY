// Definição dos campos do Perfil Mestre, alinhada às entidades da API (camelCase).
import * as v from '../validate.js';

/** "2021-02-01" → "2021-02" (valor de <input type="month">). */
export const toMonth = (d) => (d ? String(d).slice(0, 7) : '');

// Enums da API (Zyft.Domain.Profiles.Enums): [nome, rótulo pt-BR, valor numérico].
export const WORK_MODELS = [['', 'Não informado'], ['OnSite', 'Presencial', 1], ['Hybrid', 'Híbrido', 2], ['Remote', 'Remoto', 3]];
export const EDUCATION_LEVELS = [['', 'Selecione'], ['ElementarySchool', 'Ensino fundamental', 1], ['HighSchool', 'Ensino médio', 2], ['Technical', 'Técnico', 3], ['Undergraduate', 'Graduação', 4], ['Postgraduate', 'Pós-graduação', 5], ['Masters', 'Mestrado', 6], ['Doctorate', 'Doutorado', 7]];
export const EDUCATION_STATUS = [['', 'Selecione'], ['InProgress', 'Cursando', 1], ['Completed', 'Concluído', 2], ['Paused', 'Trancado', 3], ['Incomplete', 'Incompleto', 4]];
export const PROFICIENCY = [['', 'Selecione'], ['Basic', 'Básico', 1], ['Intermediate', 'Intermediário', 2], ['Advanced', 'Avançado', 3], ['Fluent', 'Fluente', 4], ['Native', 'Nativo', 5]];
export const SKILL_LEVELS = [['', 'Não informado'], ['Basic', 'Básico', 1], ['Intermediate', 'Intermediário', 2], ['Advanced', 'Avançado', 3]];
export const COURSE_TYPES = [['Course', 'Curso', 1], ['Certification', 'Certificação', 2]];
export const YES_NO = [['', 'Não informado'], ['true', 'Sim'], ['false', 'Não']];

/** Aceita o enum como nome ("Hybrid"), número (2) ou rótulo ("Híbrido") e devolve o nome. */
export function enumName(options, value) {
  if (value === null || value === undefined || value === '') return '';
  const s = String(value).toLowerCase();
  const hit = options.find(([k, l, n]) => String(k).toLowerCase() === s || String(n) === s || String(l).toLowerCase() === s);
  return hit ? hit[0] : String(value);
}

export const labelOf = (options, value) => { const k = enumName(options, value); if (k === '') return ''; return options.find(([n]) => n === k)?.[1] || k; };

export const BASIC_FIELDS = [
  { name: 'fullName', label: 'Nome completo', required: true, attrs: { autocomplete: 'name', maxlength: 150 } },
  { name: 'email', label: 'E-mail de contato', type: 'email', optional: true, attrs: { autocomplete: 'email' } },
  { name: 'phone', label: 'Telefone', type: 'tel', optional: true, hint: 'Com DDD, ex.: (44) 99999-1111', attrs: { autocomplete: 'tel' } },
  { name: 'birthDate', label: 'Data de nascimento', type: 'date', optional: true, attrs: { autocomplete: 'bday' } },
  { name: 'city', label: 'Cidade', optional: true, attrs: { autocomplete: 'address-level2' } },
  { name: 'state', label: 'Estado (UF)', optional: true, attrs: { autocomplete: 'address-level1', maxlength: 40 } },
  { name: 'country', label: 'País', optional: true, attrs: { autocomplete: 'country-name' } },
  { name: 'zipCode', label: 'CEP', optional: true, attrs: { autocomplete: 'postal-code', inputmode: 'numeric' } },
  { name: 'linkedinUrl', label: 'LinkedIn', type: 'url', optional: true, attrs: { placeholder: 'https://www.linkedin.com/in/…' } },
  { name: 'githubUrl', label: 'GitHub', type: 'url', optional: true },
  { name: 'portfolioUrl', label: 'Portfólio ou site', type: 'url', optional: true },
  { name: 'headline', label: 'Título profissional', optional: true, hint: 'Ex.: Assistente administrativa', wide: true },
  { name: 'summary', label: 'Resumo profissional', type: 'textarea', optional: true, rows: 4, wide: true, hint: 'Escreva com suas palavras. A IA só usa o que estiver aqui e nas seções abaixo.' },
  { name: 'desiredRoles', label: 'Cargos desejados', optional: true, hint: 'Separe por vírgula.', wide: true },
  { name: 'areas', label: 'Áreas de interesse', optional: true, hint: 'Separe por vírgula.' },
  { name: 'seniority', label: 'Senioridade', optional: true, hint: 'Ex.: Júnior, Pleno' },
  { name: 'workModel', label: 'Modelo de trabalho', type: 'select', options: WORK_MODELS, optional: true },
  { name: 'locationPreference', label: 'Preferência de localidade', optional: true },
  { name: 'availability', label: 'Disponibilidade para início', optional: true, hint: 'Ex.: Imediata, 30 dias' },
  { name: 'salaryExpectation', label: 'Pretensão salarial (R$)', type: 'number', optional: true, attrs: { min: 0, step: '0.01', inputmode: 'decimal' } },
  { name: 'contractTypes', label: 'Tipos de contrato aceitos', optional: true, hint: 'Ex.: CLT, PJ, Estágio' },
  { name: 'availableToTravel', label: 'Disponível para viagens', type: 'select', options: YES_NO, optional: true, bool: true },
  { name: 'availableToRelocate', label: 'Disponível para mudar de cidade', type: 'select', options: YES_NO, optional: true, bool: true },
  { name: 'hasDriverLicense', label: 'Possui CNH', type: 'select', options: YES_NO, optional: true, bool: true },
  { name: 'driverLicenseCategory', label: 'Categoria da CNH', optional: true, hint: 'Ex.: B, AB', attrs: { maxlength: 5 } },
];

const range = (d) => {
  const a = d.startDate ? toMonthBr(d.startDate) : '';
  const b = d.isCurrent ? 'atual' : d.endDate ? toMonthBr(d.endDate) : '';
  return a || b ? `${a || '?'} a ${b || '?'}` : '';
};
const toMonthBr = (d) => { const m = String(d).match(/^(\d{4})-(\d{2})/); return m ? `${m[2]}/${m[1]}` : String(d); };
const join = (...xs) => xs.filter(Boolean).join(' · ');

export const SECTIONS = [
  {
    key: 'experiences', endpoint: '/profile/experiences', title: 'Experiências', singular: 'experiência',
    empty: 'Nenhuma experiência ainda. Adicione a mais recente primeiro.',
    help: 'Resultados reais ajudam a IA a escrever respostas sem inventar nada.',
    fields: [
      { name: 'company', label: 'Empresa', required: true },
      { name: 'role', label: 'Cargo', required: true },
      { name: 'startDate', label: 'Início', type: 'month', required: true, hint: 'Mês e ano' },
      { name: 'endDate', label: 'Fim', type: 'month', optional: true, hint: 'Mês e ano' },
      { name: 'isCurrent', label: 'Trabalho aqui atualmente', type: 'checkbox' },
      { name: 'tools', label: 'Ferramentas utilizadas', optional: true, wide: true },
      { name: 'activities', label: 'Atividades', type: 'textarea', optional: true, wide: true },
      { name: 'results', label: 'Resultados reais', type: 'textarea', optional: true, wide: true },
    ],
    summary: (x) => ({ title: join(x.role, x.company) || 'Experiência', meta: range(x) }),
    validate: (d) => ({
      company: v.required(d.company, 'Informe a empresa.'),
      role: v.required(d.role, 'Informe o cargo.'),
      startDate: v.required(d.startDate, 'Informe o mês de início.') || v.notFuture(d.startDate, 'O início não pode estar no futuro.'),
      endDate: d.isCurrent ? null : v.dateRange(d.startDate, d.endDate),
    }),
  },
  {
    key: 'educations', endpoint: '/profile/educations', title: 'Formação', singular: 'formação',
    empty: 'Nenhuma formação ainda.',
    fields: [
      { name: 'institution', label: 'Instituição', required: true },
      { name: 'course', label: 'Curso', required: true },
      { name: 'level', label: 'Nível', type: 'select', options: EDUCATION_LEVELS, required: true },
      { name: 'status', label: 'Situação', type: 'select', options: EDUCATION_STATUS, required: true },
      { name: 'startDate', label: 'Início', type: 'month', required: true, hint: 'Mês e ano' },
      { name: 'endDate', label: 'Conclusão (ou previsão)', type: 'month', optional: true },
    ],
    summary: (x) => ({ title: join(x.course, x.institution) || 'Formação', meta: join(labelOf(EDUCATION_LEVELS, x.level), labelOf(EDUCATION_STATUS, x.status), range(x)) }),
    validate: (d) => ({
      institution: v.required(d.institution, 'Informe a instituição.'),
      course: v.required(d.course, 'Informe o curso.'),
      level: v.required(d.level, 'Escolha o nível.'),
      status: v.required(d.status, 'Escolha a situação.'),
      startDate: v.required(d.startDate, 'Informe o mês de início.'),
      endDate: v.dateRange(d.startDate, d.endDate),
    }),
  },
  {
    key: 'courses', endpoint: '/profile/courses', title: 'Cursos e certificações', singular: 'curso ou certificação',
    empty: 'Nenhum curso ou certificação ainda.',
    fields: [
      { name: 'name', label: 'Nome', required: true },
      { name: 'type', label: 'Tipo', type: 'select', options: COURSE_TYPES },
      { name: 'institution', label: 'Instituição ou emissor', optional: true },
      { name: 'hours', label: 'Carga horária (horas)', type: 'number', optional: true, attrs: { min: 1, max: 10000, inputmode: 'numeric' } },
      { name: 'completedAt', label: 'Concluído em', type: 'month', optional: true },
    ],
    summary: (x) => ({ title: x.name || 'Curso', meta: join(labelOf(COURSE_TYPES, x.type || 'Course'), x.institution, x.hours ? `${x.hours} h` : '', x.completedAt ? toMonthBr(x.completedAt) : '') }),
    validate: (d) => ({
      name: v.required(d.name, 'Informe o nome do curso.'),
      hours: d.hours !== null && (!Number.isFinite(d.hours) || d.hours <= 0) ? 'Informe um número de horas maior que zero.' : null,
      completedAt: v.notFuture(d.completedAt, 'A conclusão não pode estar no futuro.'),
    }),
  },
  {
    key: 'skills', endpoint: '/profile/skills', title: 'Competências', singular: 'competência',
    empty: 'Nenhuma competência ainda.',
    fields: [
      { name: 'name', label: 'Competência', required: true, hint: 'Ex.: Excel, Atendimento ao cliente' },
      { name: 'level', label: 'Nível', type: 'select', options: SKILL_LEVELS, optional: true },
    ],
    summary: (x) => ({ title: x.name || 'Competência', meta: labelOf(SKILL_LEVELS, x.level) }),
    validate: (d) => ({ name: v.required(d.name, 'Informe a competência.') || v.maxLen(d.name, 100) }),
  },
  {
    key: 'languages', endpoint: '/profile/languages', title: 'Idiomas', singular: 'idioma',
    empty: 'Nenhum idioma ainda.',
    fields: [
      { name: 'name', label: 'Idioma', required: true },
      { name: 'proficiency', label: 'Nível', type: 'select', options: PROFICIENCY, required: true },
    ],
    summary: (x) => ({ title: x.name || 'Idioma', meta: labelOf(PROFICIENCY, x.proficiency) }),
    validate: (d) => ({
      name: v.required(d.name, 'Informe o idioma.'),
      proficiency: v.required(d.proficiency, 'Escolha o nível.'),
    }),
  },
];

/** Valor vindo da API → valor do controle de formulário. */
export function toInput(f, val) {
  if (Array.isArray(val)) return val.join(', ');
  if (f.type === 'month') return toMonth(val);
  if (f.type === 'date') return val ? String(val).slice(0, 10) : '';
  if (f.type === 'checkbox') return Boolean(val);
  if (f.bool) return val === true ? 'true' : val === false ? 'false' : '';
  if (f.type === 'select') return enumName(f.options, val);
  return val ?? '';
}

/** Valores lidos do formulário → tipos esperados pela API (in-place). */
export function toApi(fields, data) {
  for (const f of fields) {
    const val = data[f.name];
    if (f.bool) data[f.name] = val === 'true' ? true : val === 'false' ? false : null;
    else if (f.type === 'number') data[f.name] = val === null || val === undefined || val === '' ? null : Number(val);
    else if (f.type === 'month') data[f.name] = v.normalizeDate(val);
    else if (f.type === 'select' && val === '') data[f.name] = null;
  }
  return data;
}
