// Perfil Mestre (RF-02, RF-05) no visual do contrato v2: pendentes, "Responda agora", sobre você,
// linha do tempo, competências com sugestões da área, idiomas, formação e cursos, informações da
// área (campos personalizados), dados sensíveis trancados e um card de anúncio na coluna.
import { api, asList } from '../api.js';
import { h, clear, field, readForm, showErrors, clearErrors, toast, announce, confirmDialog, modal, nextId, fmtDate, fmtMoney, safeUrl } from '../dom.js';
import { Button, LinkButton, Icon, Card, Chip, Stamp, SensitiveValue, PageHead, initials } from '../ui.js';
import { adSlot, PLACEMENTS } from '../ads.js';
import { loadAreas, findArea } from '../catalog.js';
import { getProfile, saveBasics, getOnboarding, markAnswered, splitList, basicsOf } from '../store.js';
import { SECTIONS, BASIC_FIELDS, toInput, toApi, labelOf, enumName, WORK_MODELS, EDUCATION_LEVELS, EDUCATION_STATUS, COURSE_TYPES, PROFICIENCY, SKILL_LEVELS, periodOf, toMonthBr, join } from './profile-schema.js';
import { STEP_SHORT } from './onboarding.js';
import * as v from '../validate.js';

const SECTION = Object.fromEntries(SECTIONS.map((s) => [s.key, s]));

// /profile/missing → passo do onboarding e rótulo curto.
const MISSING_MAP = {
  'personal.fullName': ['nome', 'Nome'], 'personal.email': ['contato', 'E-mail'], 'personal.phone': ['contato', 'Telefone'],
  'personal.city': ['endereco', 'Cidade'], 'objective.desiredRoles': ['cargos', 'Cargos'], 'objective.workModel': ['modelo', 'Modelo de trabalho'],
  'preferences.availability': ['disponibilidade', 'Disponibilidade'], 'preferences.salaryExpectation': ['salario', 'Pretensão salarial'],
  'preferences.travel': ['mobilidade', 'Viagens'], 'preferences.relocation': ['mobilidade', 'Mudança de cidade'],
  'preferences.driverLicense': ['mobilidade', 'Categoria da CNH'], experiences: ['experiencia', 'Experiência'], education: ['formacao', 'Formação'],
};

const SENSITIVE_KINDS = [
  ['Gender', 'Gênero'], ['Ethnicity', 'Raça ou etnia'], ['Disability', 'Deficiência'], ['Religion', 'Religião'],
  ['SexualOrientation', 'Orientação sexual'], ['Health', 'Saúde'], ['SocialName', 'Nome social'], ['Other', 'Outro dado sensível'],
];
const kindLabel = (k) => labelOf(SENSITIVE_KINDS, k) || String(k);
const mask = (s) => { const t = String(s || ''); return t ? `${t.slice(0, 1)}${'•'.repeat(Math.min(8, Math.max(3, t.length - 1)))}` : 'Trancado'; };

const PROF_INDEX = { Basic: 1, Intermediate: 2, Advanced: 3, Fluent: 4, Native: 5 };
const SKILL_INDEX = { Basic: 1, Intermediate: 2, Advanced: 3 };

// Checklist dos "carimbos" do passaporte (22 itens).
const STAMPS = [
  (p) => p.fullName, (p) => p.email, (p) => p.phone, (p) => p.birthDate, (p) => p.city, (p) => p.zipCode, (p) => p.workArea,
  (p) => p.desiredRoles, (p) => p.areas, (p) => p.seniority, (p) => p.workModel, (p) => p.locationPreference, (p) => p.contractTypes,
  (p) => p.availability, (p) => p.salaryExpectation != null, (p) => p.hasDriverLicense != null, (p) => p.headline, (p) => p.summary,
  (p) => p.experiences?.length, (p) => p.educations?.length, (p) => p.skills?.length, (p) => p.languages?.length,
];

// "Responda agora": perguntas rápidas que costumam aparecer nos formulários.
const MISSIONS = [
  { id: 'salario', where: 'Pretensão salarial', freq: 'Muito pedida', q: 'Quanto você quer ganhar por mês?', input: { label: 'Valor mensal (R$)', ph: 'Ex.: 4.500,00' },
    pending: (p) => p.salaryExpectation == null, save: (val) => ({ salaryExpectation: parseMoney(val) }), show: (p) => fmtMoney(p.salaryExpectation) },
  { id: 'disponibilidade', where: 'Disponibilidade', freq: 'Muito pedida', q: 'Quando você pode começar?', opts: ['Imediatamente', 'Em 15 dias', 'Em 30 dias', 'A combinar'],
    pending: (p) => !p.availability, save: (val) => ({ availability: val }), show: (p) => p.availability },
  { id: 'cnh', step: 'mobilidade', where: 'Mobilidade', freq: 'Pedida com frequência', q: 'Qual a categoria da sua CNH?', opts: ['A', 'B', 'AB', 'Não tenho'],
    pending: (p) => p.hasDriverLicense == null, save: (val) => (val === 'Não tenho' ? { hasDriverLicense: false, driverLicenseCategory: null } : { hasDriverLicense: true, driverLicenseCategory: val }), show: (p) => (p.hasDriverLicense === false ? 'Não tenho' : p.driverLicenseCategory) },
  { id: 'modelo', where: 'Modelo de trabalho', freq: 'Pedida com frequência', q: 'Como você prefere trabalhar?', opts: ['Presencial', 'Híbrido', 'Remoto'],
    pending: (p) => !p.workModel, save: (val) => ({ workModel: { Presencial: 'OnSite', Híbrido: 'Hybrid', Remoto: 'Remote' }[val] }), show: (p) => labelOf(WORK_MODELS, p.workModel) },
  { id: 'viagem', step: 'mobilidade', where: 'Mobilidade', freq: 'Aparece às vezes', q: 'Topa viajar a trabalho?', opts: ['Sim', 'Não'],
    pending: (p) => p.availableToTravel == null, save: (val) => ({ availableToTravel: val === 'Sim' }), show: (p) => (p.availableToTravel ? 'Sim' : 'Não') },
  { id: 'mudanca', step: 'mobilidade', where: 'Mobilidade', freq: 'Aparece às vezes', q: 'Mudaria de cidade por uma vaga?', opts: ['Sim', 'Não'],
    pending: (p) => p.availableToRelocate == null, save: (val) => ({ availableToRelocate: val === 'Sim' }), show: (p) => (p.availableToRelocate ? 'Sim' : 'Não') },
];

function parseMoney(s) {
  let t = String(s || '').replace(/[^\d.,]/g, '');
  if (t.includes(',')) t = t.replace(/\./g, '').replace(',', '.');
  const n = Number(t);
  return Number.isFinite(n) && t !== '' ? n : null;
}

const yesNo = (b) => (b === true ? 'Sim' : b === false ? 'Não' : null);

export async function render(main) {
  const [areas, first, missingRaw, ob] = await Promise.all([
    loadAreas(), getProfile(), api('/profile/missing').then(asList).catch(() => []), getOnboarding().catch(() => ({ answered: [], skipped: [] })),
  ]);
  const st = { p: first, missing: missingRaw, ob, customs: null, customsAvailable: true, sensitive: null, sensitiveBlocked: false, done: {}, newSkills: [] };

  const loadCustoms = async () => {
    try { st.customs = asList(await api('/profile/custom-fields')); st.customsAvailable = true; } catch (e) {
      st.customs = asList(st.p.customFields);
      st.customsAvailable = !(e.status === 404 || e.status === 405);
    }
  };
  const loadSensitive = async () => {
    // Sem o consentimento específico a API responde 403: consulta o consentimento antes para não gerar erro à toa.
    try {
      const consents = asList(await api('/consents'));
      const c = consents.find((x) => String(x.purpose).toLowerCase() === 'sensitivedata');
      if (c && !c.granted) { st.sensitive = []; st.sensitiveBlocked = true; return; }
    } catch { /* segue e deixa a API decidir */ }
    try { st.sensitive = asList(await api('/sensitive-data')); st.sensitiveBlocked = false; } catch (e) {
      st.sensitive = []; st.sensitiveBlocked = e.status === 403;
    }
  };
  await Promise.all([loadCustoms(), loadSensitive()]);

  const reload = async (focusSel) => {
    const [p, missing] = await Promise.all([getProfile(), api('/profile/missing').then(asList).catch(() => st.missing)]);
    st.p = p; st.missing = missing;
    paint(focusSel);
  };

  // ---------- Estrutura ----------
  const pendingSlot = h('div');
  const missionsSlot = h('div');
  const mainCol = h('div', { class: 'profile-main' });
  const meSlot = h('div');
  const ad = adSlot(PLACEMENTS.profileRail, { variant: 'native' });
  main.append(h('div', { class: 'content content--wide' },
    PageHead({ overline: 'Seu passaporte', title: 'Perfil Mestre', wide: true, actions: LinkButton('#/importar', { variant: 'quiet', icon: 'upload' }, 'Atualizar com um currículo') }),
    pendingSlot, missionsSlot,
    h('div', { class: 'profile-cols' }, mainCol, h('aside', { class: 'profile-aside', 'aria-label': 'Resumo do passaporte' }, meSlot, ad))));

  function pendingList() {
    const out = new Map();
    for (const id of st.ob.skipped || []) out.set(id, STEP_SHORT[id] || id);
    for (const mi of st.missing) {
      const [step, label] = MISSING_MAP[mi.key] || [null, mi.question || mi.key];
      const k = step ? `${step}:${label}` : mi.key;
      if (step && out.has(step)) continue;
      out.set(k, label);
    }
    return [...out.entries()].map(([k, label]) => ({ step: k.split(':')[0], label }));
  }

  function paintPending() {
    const list = pendingList();
    if (!list.length) { clear(pendingSlot); return; }
    const firstStep = list.find((x) => STEP_SHORT[x.step])?.step;
    clear(pendingSlot, h('section', { class: 'pending', 'aria-labelledby': 'pending-title' },
      h('div', { class: 'pending__count', 'aria-hidden': 'true' }, String(list.length)),
      h('div', { class: 'pending__text' },
        h('h2', { id: 'pending-title', class: 'pending__title' }, list.length === 1 ? '1 informação ficou para depois no seu passaporte' : `${list.length} informações ficaram para depois no seu passaporte`),
        h('ul', { class: 'chips', style: { listStyle: 'none', margin: 0, padding: 0 } }, list.map((x) => h('li', { class: 'tag' }, x.label))),
        h('span', { class: 'small muted' }, 'Enquanto faltarem, a extensão vai marcar esses campos como Precisa de você nos formulários.')),
      LinkButton(firstStep ? `#/boas-vindas/${firstStep}` : '#/boas-vindas', { variant: 'primary' }, 'Continuar de onde parei')));
  }

  function paintMissions() {
    const p = st.p;
    const list = MISSIONS.filter((m) => st.done[m.id] || m.pending(p)).slice(0, 3);
    if (!list.length) { clear(missionsSlot); return; }
    clear(missionsSlot, h('section', { class: 'stack', 'aria-labelledby': 'prox' },
      h('div', { class: 'section-head' },
        h('h2', { id: 'prox', class: 'section-title' }, 'Responda agora, em 10 segundos'),
        h('span', { class: 'small muted' }, 'Pendentes que costumam aparecer nos formulários')),
      h('div', { class: 'missions' }, list.map((m) => missionCard(m)))));
  }

  function missionCard(m) {
    const done = st.done[m.id];
    const card = h('div', { class: `mission ${done ? 'is-done' : ''}` },
      h('div', { class: 'mission__meta' }, h('b', {}, m.where), h('span', {}, m.freq)),
      h('div', { class: 'mission__q', id: `mq-${m.id}` }, m.q));
    if (done) {
      card.append(h('div', { class: 'mission__foot' }, h('span', { style: { fontSize: '15px', fontWeight: '600' } }, m.show(st.p) || done), Stamp({ size: 'sm', at: 'agora' })));
      return card;
    }
    let value = '';
    const stampBtn = Button({ variant: 'primary', size: 'sm', disabled: true }, 'Carimbar');
    if (m.opts) {
      const group = h('div', { class: 'row', role: 'radiogroup', 'aria-labelledby': `mq-${m.id}` });
      for (const o of m.opts) {
        const b = h('button', { type: 'button', role: 'radio', class: 'pill', 'aria-checked': 'false' }, o);
        b.addEventListener('click', () => {
          value = o;
          group.querySelectorAll('.pill').forEach((x) => x.setAttribute('aria-checked', String(x === b)));
          stampBtn.disabled = false;
        });
        group.append(b);
      }
      card.append(group);
    } else {
      const id = nextId('mission');
      const input = h('input', { id, class: 'zy-tf__input', inputmode: 'decimal', placeholder: m.input.ph });
      input.addEventListener('input', () => { value = input.value; stampBtn.disabled = parseMoney(value) === null; });
      card.append(h('div', { class: 'zy-tf' }, h('label', { class: 'zy-tf__label', for: id }, m.input.label), input));
    }
    stampBtn.addEventListener('click', async () => {
      stampBtn.disabled = true;
      try {
        st.p = { ...st.p, ...(await saveBasics(m.save(value), st.p)) };
        Object.assign(st.p, m.save(value));
        st.done[m.id] = value;
        const step = m.step || m.id;
        if ((st.ob.skipped || []).includes(step)) st.ob = await markAnswered(step);
        st.missing = await api('/profile/missing').then(asList).catch(() => st.missing);
        paintPending(); paintMissions(); paintMe(); paintMainCol();
        announce(`${m.where}: carimbado.`);
        document.querySelector('#prox')?.focus?.();
      } catch (err) { stampBtn.disabled = false; if (!err.handled) toast(err.message, 'err'); }
    });
    card.append(h('div', { class: 'mission__foot' }, stampBtn));
    return card;
  }

  function paintMe() {
    const p = st.p;
    const count = STAMPS.filter((f) => f(p)).length;
    const pct = Math.round((count / STAMPS.length) * 100);
    const sub = [p.workArea, splitList(p.desiredRoles)[0], p.seniority, labelOf(WORK_MODELS, p.workModel)].filter(Boolean).join(' · ');
    clear(meSlot, h('section', { class: 'me-card', 'aria-label': 'Seu passaporte' },
      h('div', { class: 'me-card__who' },
        h('div', { class: 'me-card__avatar', 'aria-hidden': 'true' }, initials(p.fullName)),
        h('div', {}, h('span', { class: 'me-card__name' }, p.fullName || 'Seu nome'), h('span', { class: 'me-card__sub' }, sub || 'Conte sua área no perfil'))),
      h('div', { class: 'passport__meter' },
        h('div', { class: 'passport__meter-row' }, h('span', {}, `${count} de ${STAMPS.length} carimbos`), h('span', { style: { opacity: '0.75' } }, `${pct}%`)),
        h('div', { class: 'passport__track', role: 'progressbar', 'aria-label': 'Carimbos do passaporte', 'aria-valuenow': count, 'aria-valuemin': 0, 'aria-valuemax': STAMPS.length },
          h('div', { style: { width: `${pct}%` } }))),
      h('p', { class: 'me-card__note' }, 'Com o passaporte completo, a extensão costuma deixar quase todo campo Pronto.')));
  }

  // ---------- Coluna principal ----------
  function paintMainCol(focusSel) {
    clear(mainCol, aboutCard(), experiencesCard(),
      h('div', { class: 'profile-grid' }, skillsCard(), languagesCard(), educationCard(), customCard(), sensitiveCard()));
    if (focusSel) mainCol.querySelector(focusSel)?.focus();
  }

  function paint(focusSel) { paintPending(); paintMissions(); paintMe(); paintMainCol(focusSel); }

  const headingFocus = (id) => `#${id}`;
  const focusable = (el) => { el.setAttribute('tabindex', '-1'); return el; };

  // Sobre você
  function aboutCard() {
    const p = st.p;
    const city = [p.city, p.state].filter(Boolean).join(', ') + (p.country ? ` · ${p.country}` : '');
    const rows = [
      ['Nome completo', p.fullName], ['E-mail', p.email], ['Telefone', p.phone], ['Nascimento', p.birthDate ? fmtDate(`${String(p.birthDate).slice(0, 10)}T12:00:00`) : null],
      ['Cidade', city.trim() || null], ['CEP', p.zipCode], ['Área', p.workArea], ['Registro profissional', p.professionalRegistration],
      ['Título profissional', p.headline], ['Busca', p.desiredRoles], ['Setores', p.areas], ['Momento da carreira', p.seniority],
      ['Modelo de trabalho', labelOf(WORK_MODELS, p.workModel) || null], ['Localidade', p.locationPreference], ['Contrato', p.contractTypes],
      ['Disponibilidade', p.availability], ['Pretensão salarial', p.salaryExpectation != null ? `${fmtMoney(p.salaryExpectation)}${p.salaryType ? ` · ${p.salaryType}` : ''}` : null],
      ['Viagens', yesNo(p.availableToTravel)], ['Mudança de cidade', yesNo(p.availableToRelocate)],
      ['CNH', p.hasDriverLicense === false ? 'Não tenho' : p.driverLicenseCategory || (p.hasDriverLicense ? 'Sim' : null)],
      ['LinkedIn', p.linkedinUrl], ['GitHub', p.githubUrl], ['Portfólio', p.portfolioUrl],
    ];
    const edit = Button({ variant: 'ghost', size: 'sm', icon: 'edit', 'aria-label': 'Editar sobre você' }, 'Editar');
    edit.addEventListener('click', () => basicsModal());
    return Card({ title: 'Sobre você', titleId: 'about-title', action: edit },
      h('dl', { class: 'dl-grid' }, rows.map(([k, val]) => h('div', {}, h('dt', {}, k),
        h('dd', { class: val ? '' : 'is-empty' }, val && /^https?:\/\//.test(val) ? h('a', { href: safeUrl(val), target: '_blank', rel: 'noopener noreferrer' }, val.replace(/^https?:\/\/(www\.)?/, '')) : (val || 'Não informado'))))),
      p.summary ? h('div', { class: 'stack', style: { gap: '4px' } }, h('span', { class: 'caption' }, 'Resumo'), h('p', { class: 'timeline__text' }, p.summary)) : null);
  }

  function basicsModal() {
    const p = st.p;
    const listId = nextId('areas');
    const form = h('form', { novalidate: true, class: 'form-grid', id: nextId('basics') },
      h('datalist', { id: listId }, areas.map((a) => h('option', { value: a.name }))),
      BASIC_FIELDS.map((f) => field({ ...f, value: toInput(f, p[f.name]), attrs: { ...(f.attrs || {}), ...(f.name === 'workArea' ? { list: listId } : {}) } })));
    const save = Button({ variant: 'primary', type: 'submit', form: form.id }, 'Salvar e carimbar');
    const cancel = Button({ variant: 'quiet' }, 'Cancelar');
    const m = modal({ title: 'Sobre você', body: form, actions: [save, cancel], wide: true });
    cancel.addEventListener('click', () => m.close());
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      clearErrors(form);
      const data = toApi(BASIC_FIELDS, readForm(form));
      for (const k of ['linkedinUrl', 'githubUrl', 'portfolioUrl']) if (data[k] && !/^https?:\/\//i.test(data[k])) data[k] = `https://${data[k]}`;
      const errors = v.collect({
        email: data.email ? v.email(data.email) : null,
        linkedinUrl: v.url(data.linkedinUrl), githubUrl: v.url(data.githubUrl), portfolioUrl: v.url(data.portfolioUrl),
        summary: v.maxLen(data.summary, 4000),
        salaryExpectation: data.salaryExpectation !== null && !(data.salaryExpectation >= 0) ? 'Informe um valor numérico, sem pontos de milhar.' : null,
        birthDate: v.notFuture(data.birthDate, 'A data de nascimento não pode estar no futuro.'),
      });
      if (v.hasErrors(errors)) return showErrors(form, errors);
      save.disabled = true;
      try {
        await saveBasics({ ...basicsOf(p), ...data }, p);
        m.close();
        toast('Sobre você: carimbado', 'ok');
        await reload(headingFocus('about-title'));
      } catch (err) { if (!err.handled) showErrors(form, err); } finally { save.disabled = false; }
    });
  }

  /** Modal genérico para experiências, formação, cursos, competências e idiomas. */
  function itemModal(key, item, focusId) {
    const s = SECTION[key];
    const isNew = !item;
    const form = h('form', { novalidate: true, class: 'form-grid', id: nextId(`form-${key}`) },
      s.fields.map((f) => field({ ...f, wide: f.wide || f.type === 'checkbox', value: toInput(f, item?.[f.name]) })));
    const cur = form.elements.isCurrent;
    const end = form.elements.endDate;
    if (cur && end) { const sync = () => { end.disabled = cur.checked; if (cur.checked) end.value = ''; }; cur.addEventListener('change', sync); sync(); }
    const save = Button({ variant: 'primary', type: 'submit', form: form.id }, isNew ? 'Adicionar e carimbar' : 'Salvar e carimbar');
    const cancel = Button({ variant: 'quiet' }, 'Cancelar');
    const del = isNew ? null : Button({ variant: 'danger' }, 'Excluir');
    const m = modal({ title: isNew ? `Adicionar ${s.singular}` : `Editar ${s.singular}`, body: form, actions: [save, cancel, del].filter(Boolean), wide: key === 'experiences' });
    cancel.addEventListener('click', () => m.close());
    del?.addEventListener('click', async () => {
      const title = s.summary(item).title;
      if (!(await confirmDialog({ title: `Excluir ${s.singular}?`, message: `“${title}” sai do seu Perfil Mestre.`, confirmText: 'Excluir', danger: true }))) return;
      try {
        await api(`${s.endpoint}/${encodeURIComponent(item.id)}`, { method: 'DELETE' });
        m.close();
        toast('Item excluído do perfil.');
        await reload(`#${focusId}`);
      } catch (err) { if (!err.handled) toast(err.message, 'err'); }
    });
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      clearErrors(form);
      const data = toApi(s.fields, readForm(form));
      if (cur && cur.checked) data.endDate = null;
      const errors = v.collect(s.validate(data));
      if (v.hasErrors(errors)) return showErrors(form, errors);
      save.disabled = true;
      try {
        if (isNew) await api(s.endpoint, { method: 'POST', body: data });
        else await api(`${s.endpoint}/${encodeURIComponent(item.id)}`, { method: 'PUT', body: data });
        m.close();
        toast(isNew ? 'Adicionado e carimbado' : 'Alterações carimbadas', 'ok');
        await reload(`#${focusId}`);
      } catch (err) { if (!err.handled) showErrors(form, err); } finally { save.disabled = false; }
    });
  }

  // Experiências (linha do tempo)
  function experiencesCard() {
    const list = st.p.experiences || [];
    const add = Button({ variant: 'quiet', size: 'sm', icon: 'plus' }, 'Adicionar');
    add.addEventListener('click', () => itemModal('experiences', null, 'exp-title'));
    return Card({ title: 'Experiências', titleId: 'exp-title', action: add },
      list.length ? h('ol', { class: 'timeline' }, list.map((e) => {
        const edit = Button({ variant: 'ghost', size: 'sm', icon: 'edit', 'aria-label': `Editar experiência ${e.role} em ${e.company}` }, 'Editar');
        edit.addEventListener('click', () => itemModal('experiences', e, 'exp-title'));
        return h('li', { class: 'timeline__item' },
          h('div', { class: 'timeline__rail', 'aria-hidden': 'true' }, h('span', { class: `timeline__dot ${e.isCurrent ? 'is-current' : ''}` }), h('span', { class: 'timeline__line' })),
          h('div', { class: 'timeline__body' },
            h('div', { class: 'timeline__top' },
              h('div', {}, h('span', { class: 'timeline__role' }, e.role), h('span', { class: 'timeline__meta' }, join(e.company, periodOf(e)))),
              edit),
            e.results ? h('p', { class: 'timeline__text' }, e.results) : null,
            e.activities ? h('p', { class: 'timeline__text muted' }, e.activities) : null,
            e.tools ? h('div', { class: 'chips' }, splitList(e.tools).map((t) => h('span', { class: 'tag--sq' }, t))) : null));
      })) : h('p', { class: 'muted' }, 'Nenhuma experiência ainda. Vale estágio, freela, trabalho voluntário ou negócio próprio.'));
  }

  // Competências com sugestões da área + cadastro livre
  function skillsCard() {
    const skills = st.p.skills || [];
    const area = findArea(areas, st.p.workArea);
    const have = new Set(skills.map((s) => s.name.toLowerCase()));
    const sugs = (area?.skills || []).filter((s) => !have.has(s.toLowerCase())).slice(0, 6);
    const inputId = nextId('skill');
    const input = h('input', { id: inputId, class: 'mini-input', placeholder: 'Ex.: Mediação de conflitos', maxlength: 100 });
    const addBtn = Button({ variant: 'secondary', size: 'sm', disabled: true }, 'Adicionar');
    input.addEventListener('input', () => { addBtn.disabled = !input.value.trim(); });
    const addSkill = async (name, origin) => {
      if (have.has(name.toLowerCase())) { toast('Essa competência já está no seu perfil.'); return; }
      try {
        await api('/profile/skills', { method: 'POST', body: { name, level: null } });
        st.newSkills.push(name.toLowerCase());
        announce(`${name} adicionada às competências.`);
        await reload(origin === 'input' ? `#${inputId}` : '#skills-title');
      } catch (err) { if (!err.handled) toast(err.message, 'err'); }
    };
    addBtn.addEventListener('click', () => addSkill(input.value.trim(), 'input'));
    input.addEventListener('keydown', (e) => { if (e.key === 'Enter') { e.preventDefault(); if (input.value.trim()) addSkill(input.value.trim(), 'input'); } });
    const headAdd = Button({ variant: 'ghost', size: 'sm', icon: 'plus' }, 'Adicionar');
    headAdd.addEventListener('click', () => itemModal('skills', null, 'skills-title'));
    return Card({ title: 'Competências', titleId: 'skills-title', action: headAdd },
      skills.length ? h('div', { class: 'chips' }, skills.map((s) => {
        const n = SKILL_INDEX[enumName(SKILL_LEVELS, s.level)] || 0;
        const lv = labelOf(SKILL_LEVELS, s.level);
        return h('button', { type: 'button', class: `skill ${st.newSkills.includes(s.name.toLowerCase()) ? 'is-new' : ''}`, 'aria-label': `${s.name}${lv ? `, nível ${lv}` : ''}. Editar`, onclick: () => itemModal('skills', s, 'skills-title') },
          s.name, n ? h('span', { class: 'dots', 'aria-hidden': 'true' }, [1, 2, 3].map((i) => h('span', { class: i <= n ? 'on' : '' }))) : null);
      })) : h('p', { class: 'muted' }, 'Nenhuma competência ainda.'),
      h('div', { class: 'suggest' },
        sugs.length ? [h('span', { class: 'suggest__label' }, `Sugestões para ${area.name} · toque para adicionar`),
          h('div', { class: 'chips' }, sugs.map((sname) => Chip({ label: `+ ${sname}`, size: 'sm', dashed: true, onClick: () => addSkill(sname, 'sug') })))] : null,
        h('div', { class: 'row-end' },
          h('label', { class: 'mini-label grow', for: inputId }, 'Cadastrar do seu jeito', input), addBtn)));
  }

  // Idiomas com barra de nível
  function languagesCard() {
    const langs = st.p.languages || [];
    const add = Button({ variant: 'ghost', size: 'sm', icon: 'plus' }, 'Adicionar');
    add.addEventListener('click', () => itemModal('languages', null, 'langs-title'));
    return Card({ title: 'Idiomas', titleId: 'langs-title', action: add },
      langs.length ? langs.map((l) => {
        const n = PROF_INDEX[enumName(PROFICIENCY, l.proficiency)] || 0;
        const edit = Button({ variant: 'ghost', size: 'sm', icon: 'edit', 'aria-label': `Editar ${l.name}` }, 'Editar');
        edit.addEventListener('click', () => itemModal('languages', l, 'langs-title'));
        return h('div', { class: 'lang' },
          h('div', { class: 'lang__head' }, h('b', {}, l.name), h('span', { class: 'row' }, h('span', { class: 'muted' }, labelOf(PROFICIENCY, l.proficiency)), edit)),
          h('div', { class: 'lang__bars', 'aria-hidden': 'true' }, [1, 2, 3, 4, 5].map((i) => h('span', { class: i <= n ? 'on' : '' }))));
      }) : h('p', { class: 'muted' }, 'Nenhum idioma ainda. Libras também conta.'));
  }

  // Formação e cursos
  function educationCard() {
    const edus = st.p.educations || [];
    const courses = st.p.courses || [];
    const addEdu = Button({ variant: 'ghost', size: 'sm', icon: 'plus' }, 'Formação');
    const addCourse = Button({ variant: 'ghost', size: 'sm', icon: 'plus' }, 'Curso');
    addEdu.addEventListener('click', () => itemModal('educations', null, 'edu-title'));
    addCourse.addEventListener('click', () => itemModal('courses', null, 'edu-title'));
    const row = (title, meta, onEdit, label) => {
      const b = Button({ variant: 'ghost', size: 'sm', icon: 'edit', 'aria-label': `Editar ${label}` }, 'Editar');
      b.addEventListener('click', onEdit);
      return h('div', { class: 'line-item' }, h('div', { class: 'line-item__main' }, h('span', { class: 'line-item__title' }, title), h('span', { class: 'line-item__meta' }, meta)), b);
    };
    return Card({ title: 'Formação e cursos', titleId: 'edu-title', action: h('div', { class: 'row' }, addEdu, addCourse) },
      !edus.length && !courses.length ? h('p', { class: 'muted' }, 'Nenhuma formação ou curso ainda.') : null,
      edus.map((e) => row(e.course, join(e.institution, labelOf(EDUCATION_LEVELS, e.level), labelOf(EDUCATION_STATUS, e.status), periodOf(e)), () => itemModal('educations', e, 'edu-title'), e.course)),
      courses.map((c) => row(c.name, join(labelOf(COURSE_TYPES, c.type || 'Course'), c.institution, c.hours ? `${c.hours} h` : '', c.completedAt ? toMonthBr(c.completedAt) : ''), () => itemModal('courses', c, 'edu-title'), c.name)));
  }

  // Informações da sua área (campos personalizados)
  function customCard() {
    const list = st.customs || [];
    const msg = h('div', { 'aria-live': 'polite' });
    const kId = nextId('cf-k');
    const vId = nextId('cf-v');
    const k = h('input', { id: kId, class: 'mini-input', name: 'label', maxlength: 80, placeholder: 'Ex.: Disponível para plantão noturno' });
    const val = h('input', { id: vId, class: 'mini-input', name: 'value', maxlength: 500, placeholder: 'Ex.: Sim, até 2 por semana' });
    const add = Button({ variant: 'secondary', size: 'sm', disabled: true }, 'Adicionar informação');
    const sync = () => { add.disabled = !k.value.trim() || !val.value.trim(); };
    k.addEventListener('input', sync); val.addEventListener('input', sync);

    const sensitiveMsg = (err) => h('div', { class: 'notice notice--care', role: 'alert' },
      Icon('shield', { size: 16 }),
      h('p', {}, err.detail || 'Esse tipo de informação é sensível e fica melhor guardado à parte: use o bloco Dados sensíveis, logo abaixo. Lá ele fica trancado e nunca vai para a IA.'),
      Button({ variant: 'quiet', size: 'sm', onClick: () => document.querySelector('#sens-title')?.focus() }, 'Ir para Dados sensíveis'));

    const submit = async (label, value, id) => {
      clear(msg);
      try {
        if (id) await api(`/profile/custom-fields/${encodeURIComponent(id)}`, { method: 'PUT', body: { label, value } });
        else await api('/profile/custom-fields', { method: 'POST', body: { label, value } });
        await loadCustoms();
        toast(id ? 'Informação atualizada' : 'Informação adicionada e carimbada', 'ok');
        paintMainCol(id ? '#custom-title' : `#${kId}`);
        return true;
      } catch (err) {
        if (err.handled) return false;
        if (err.code === 'profile.custom_field_sensitive' || err.status === 422) clear(msg, sensitiveMsg(err));
        else clear(msg, h('p', { class: 'notice notice--error', role: 'alert' }, err.message));
        return false;
      }
    };
    add.addEventListener('click', () => submit(k.value.trim(), val.value.trim()));

    const rowFor = (x) => {
      const r = h('div', { class: 'extra' });
      const view = () => {
        const edit = Button({ variant: 'ghost', size: 'sm', icon: 'edit', 'aria-label': `Editar ${x.label}` }, 'Editar');
        const del = Button({ variant: 'ghost', size: 'sm', 'aria-label': `Excluir ${x.label}` }, 'Excluir');
        edit.addEventListener('click', editMode);
        del.addEventListener('click', async () => {
          if (!(await confirmDialog({ title: 'Excluir esta informação?', message: `“${x.label}” deixa de ser usada pela extensão.`, confirmText: 'Excluir', danger: true }))) return;
          try { await api(`/profile/custom-fields/${encodeURIComponent(x.id)}`, { method: 'DELETE' }); await loadCustoms(); paintMainCol('#custom-title'); announce('Informação excluída.'); } catch (err) { if (!err.handled) toast(err.message, 'err'); }
        });
        clear(r, h('span', { class: 'extra__k' }, x.label), h('span', { class: 'extra__v' }, x.value), h('span', { class: 'extra__actions' }, edit, del));
      };
      const editMode = () => {
        const ek = h('input', { class: 'mini-input', value: x.label, maxlength: 80, 'aria-label': 'Nome da informação' });
        const ev = h('input', { class: 'mini-input', value: x.value, maxlength: 500, 'aria-label': 'Sua resposta' });
        const ok = Button({ variant: 'primary', size: 'sm', icon: 'check' }, 'Salvar');
        const no = Button({ variant: 'ghost', size: 'sm' }, 'Cancelar');
        ok.addEventListener('click', () => { if (ek.value.trim() && ev.value.trim()) submit(ek.value.trim(), ev.value.trim(), x.id); });
        no.addEventListener('click', () => { view(); r.querySelector('button')?.focus(); });
        clear(r, h('span', { class: 'extra__k' }, ek), h('span', { class: 'extra__v' }, ev), h('span', { class: 'extra__actions' }, ok, no));
        ek.focus();
      };
      view();
      return r;
    };

    return Card({ title: 'Informações da sua área', titleId: 'custom-title', className: 'span-all' },
      h('p', { class: 'card__sub' }, 'Crie os campos que a sua profissão pede e o Zyft não conhece: registro em conselho, disponibilidade para plantão, ferramentas próprias. A extensão usa quando o formulário perguntar algo parecido.'),
      !st.customsAvailable ? h('p', { class: 'notice notice--care' }, Icon('alert', { size: 14 }), 'Este recurso ainda não está disponível neste servidor. Tente de novo mais tarde.') : null,
      h('div', {}, list.map(rowFor)),
      st.customsAvailable ? h('div', { class: 'row-end' },
        h('label', { class: 'mini-label grow', for: kId }, 'Nome da informação', k),
        h('label', { class: 'mini-label grow', for: vId }, 'Sua resposta', val),
        add) : null,
      msg);
  }

  // Dados sensíveis (trancados)
  function sensitiveCard() {
    const title = h('h2', { class: 'card__title', id: 'sens-title', tabindex: '-1' }, Icon('lock'), 'Dados sensíveis');
    const card = h('section', { class: 'card', 'aria-labelledby': 'sens-title' }, h('div', { class: 'card__head' }, title));
    if (st.sensitiveBlocked) {
      card.append(
        h('p', { class: 'small muted' }, 'Para guardar gênero, raça ou etnia, deficiência e outros dados sensíveis, ligue antes o consentimento específico. Eles ficam trancados e nunca vão para a IA.'),
        LinkButton('#/conta', { variant: 'quiet', size: 'sm', icon: 'shield' }, 'Ligar em Conta e privacidade'));
      return card;
    }
    const list = st.sensitive || [];
    const used = new Set(list.map((x) => x.kind));
    const free = SENSITIVE_KINDS.filter(([k]) => !used.has(k));
    card.append(h('p', { class: 'small muted' }, 'Trancados. Você destranca um a um, e no formulário cada um é aprovado separadamente.'));
    for (const x of list) {
      const edit = Button({ variant: 'ghost', size: 'sm', icon: 'edit', 'aria-label': `Editar ${kindLabel(x.kind)}` }, 'Editar');
      edit.addEventListener('click', () => sensitiveModal(x, free));
      card.append(h('div', { class: 'sens-row' }, h('span', { class: 'sens-row__label' }, kindLabel(x.kind)),
        h('span', { class: 'row' }, SensitiveValue({ value: x.value, masked: mask(x.value) }), edit)));
    }
    if (!list.length) card.append(h('p', { class: 'muted small' }, 'Nenhum dado sensível guardado. Só cadastre se quiser.'));
    if (free.length) {
      const add = Button({ variant: 'quiet', size: 'sm', icon: 'plus' }, 'Adicionar dado sensível');
      add.addEventListener('click', () => sensitiveModal(null, free));
      card.append(h('div', {}, add));
    }
    return card;
  }

  function sensitiveModal(item, free) {
    const isNew = !item;
    const form = h('form', { novalidate: true, class: 'stack', id: nextId('sens') },
      isNew ? field({ name: 'kind', label: 'Tipo de dado', type: 'select', options: free, value: free[0]?.[0] }) : h('p', {}, h('b', {}, kindLabel(item.kind))),
      field({ name: 'value', label: 'Valor', value: item?.value || '', attrs: { maxlength: 500, autocomplete: 'off' }, hint: 'Fica cifrado e trancado. Nunca vai para a IA nem entra em aprovação em lote.' }));
    const save = Button({ variant: 'primary', type: 'submit', form: form.id }, 'Guardar trancado');
    const cancel = Button({ variant: 'quiet' }, 'Cancelar');
    const del = isNew ? null : Button({ variant: 'danger' }, 'Apagar');
    const m = modal({ title: isNew ? 'Adicionar dado sensível' : 'Editar dado sensível', body: form, actions: [save, cancel, del].filter(Boolean) });
    cancel.addEventListener('click', () => m.close());
    del?.addEventListener('click', async () => {
      if (!(await confirmDialog({ title: 'Apagar este dado?', message: `${kindLabel(item.kind)} será apagado de vez.`, confirmText: 'Apagar', danger: true }))) return;
      try { await api(`/sensitive-data/${item.id}`, { method: 'DELETE' }); m.close(); await loadSensitive(); paintMainCol('#sens-title'); toast('Dado sensível apagado.'); } catch (err) { if (!err.handled) toast(err.message, 'err'); }
    });
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      clearErrors(form);
      const data = readForm(form);
      if (!data.value) return showErrors(form, { value: ['Escreva o valor ou cancele.'] });
      save.disabled = true;
      try {
        if (isNew) await api('/sensitive-data', { method: 'POST', body: { kind: data.kind, value: data.value } });
        else await api(`/sensitive-data/${item.id}`, { method: 'PUT', body: { kind: item.kind, value: data.value } });
        m.close();
        await loadSensitive();
        paintMainCol('#sens-title');
        toast('Guardado trancado.', 'ok');
      } catch (err) { if (!err.handled) showErrors(form, err); } finally { save.disabled = false; }
    });
  }

  paint();
  void focusable;
}
