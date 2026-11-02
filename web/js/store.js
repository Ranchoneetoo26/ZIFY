// Estado compartilhado entre telas: perfil (dados básicos), progresso do onboarding e uso do plano.
import { api, asList } from './api.js';

// Campos básicos de PUT /profile. O PUT substitui todos, então sempre enviamos o conjunto completo.
export const BASIC_KEYS = [
  'fullName', 'email', 'phone', 'birthDate', 'city', 'state', 'country', 'zipCode',
  'linkedinUrl', 'githubUrl', 'portfolioUrl', 'headline', 'summary', 'desiredRoles', 'areas', 'seniority',
  'workModel', 'locationPreference', 'availability', 'salaryExpectation', 'availableToTravel', 'availableToRelocate',
  'contractTypes', 'hasDriverLicense', 'driverLicenseCategory',
  // v2
  'workArea', 'professionalRegistration', 'salaryType',
];

export const basicsOf = (p) => Object.fromEntries(BASIC_KEYS.map((k) => [k, p?.[k] ?? null]));

export async function getProfile() {
  try {
    return (await api('/profile')) || {};
  } catch (e) {
    if (e.status === 404) return {};
    throw e;
  }
}

const sameValue = (a, b) => (a ?? null) === (b ?? null) || JSON.stringify(a ?? null) === JSON.stringify(b ?? null);

/**
 * Atualiza só os campos em `patch`, preservando o resto (lê o perfil atual antes se `current` não vier).
 * `current` é o retrato que a tela usou para montar o formulário. Se outra sessão salvou o perfil depois dele (409) e a
 * pessoa escolhe "Sobrescrever mesmo assim", o corpo é remontado sobre o perfil mais recente com SÓ os campos que esta
 * tela mudou em relação ao retrato (`changed` diz quais, quando a tela sabe melhor; senão, compara `patch` com o
 * retrato). Os outros campos ficam com o valor salvo na outra sessão.
 */
export async function saveBasics(patch, current, { changed } = {}) {
  const base = current ? basicsOf(current) : basicsOf(await getProfile());
  const body = { ...base, ...patch };
  const mine = (changed ? [...changed] : Object.keys(patch)).filter((k) => BASIC_KEYS.includes(k) && !sameValue(body[k], base[k]));
  const rebase = async () => {
    const fresh = basicsOf(await getProfile());
    return { ...fresh, ...Object.fromEntries(mine.map((k) => [k, body[k]])) };
  };
  const r = await api('/profile', { method: 'PUT', body, rebase });
  return r && typeof r === 'object' ? r : body;
}

/** "a, b, c" ↔ ["a","b","c"] (campos de texto separados por vírgula no perfil). */
export const splitList = (s) => String(s || '').split(/[,;\n]/).map((x) => x.trim()).filter(Boolean);
export const joinList = (xs) => (xs && xs.length ? [...new Set(xs.map((x) => x.trim()).filter(Boolean))].join(', ') : null);

/** "linkedin.com/in/x" → "https://linkedin.com/in/x" (a API exige URL absoluta). */
export function toUrl(v) {
  const s = String(v || '').trim();
  if (!s) return null;
  // Outro protocolo (ftp://) fica como está: quem valida recusa, em vez de virar https://ftp://…
  return /^[a-z][a-z0-9+.-]*:\/\//i.test(s) ? s : `https://${s.replace(/^\/+/, '')}`;
}

// ---------- Onboarding (GET/PUT /onboarding) ----------
const OB_KEY = 'zyft.onboarding';
export const STEP_IDS = ['nome', 'nascimento', 'contato', 'endereco', 'area', 'cargos', 'setores', 'registro', 'links', 'nivel', 'modelo', 'local', 'contrato',
  'disponibilidade', 'salario', 'mobilidade', 'experiencia', 'diaadia', 'ferramentas', 'formacao', 'cursos', 'competencias', 'idiomas', 'titulo', 'resumo', 'combinados'];
const emptyOb = () => ({ answered: [], skipped: [], lastStep: null, completedAt: null });
let obAvailable = true;

export async function getOnboarding() {
  try {
    const r = await api('/onboarding');
    obAvailable = true;
    return { ...emptyOb(), ...(r || {}) };
  } catch (e) {
    if (e.status === 401) throw e;
    obAvailable = false;
    try { return { ...emptyOb(), ...JSON.parse(sessionStorage.getItem(OB_KEY) || '{}') }; } catch { return emptyOb(); }
  }
}

export async function saveOnboarding(state, completed = false, opts = {}) {
  // A API só aceita ids de pergunta; telas de abertura/fim viram null/"combinados".
  const last = state.lastStep === 'fim' ? 'combinados' : STEP_IDS.includes(state.lastStep) ? state.lastStep : null;
  const clean = (xs) => [...new Set(xs)].filter((x) => STEP_IDS.includes(x));
  const body = { answered: clean(state.answered), skipped: clean(state.skipped), lastStep: last, completed: Boolean(completed || state.completedAt) };
  try { sessionStorage.setItem(OB_KEY, JSON.stringify({ ...body, lastStep: state.lastStep, completedAt: completed ? new Date().toISOString() : state.completedAt })); } catch { /* sem armazenamento */ }
  if (!obAvailable) return body;
  try { return await api('/onboarding', { method: 'PUT', body, ...opts }); } catch (e) {
    if (e.status === 404 || e.status === 405) { obAvailable = false; return body; }
    throw e;
  }
}

/** Move um passo de "para depois" para "respondido" (usado no Perfil Mestre). */
export async function markAnswered(stepId) {
  const ob = await getOnboarding();
  if (ob.answered.includes(stepId) && !ob.skipped.includes(stepId)) return ob;
  ob.answered = [...new Set([...ob.answered, stepId])];
  ob.skipped = ob.skipped.filter((s) => s !== stepId);
  await saveOnboarding(ob).catch(() => {});
  return ob;
}

/**
 * Tira de "para depois" o que já tem dado (`answered`) ou deixou de existir para a área (`dropped`).
 * Só grava se algo mudou. Falha aqui não é grave: a pendência já é calculada pelo dado real.
 */
export async function settleSkipped(ob, answered, dropped = []) {
  if (!answered.length && !dropped.length) return ob;
  const gone = new Set([...answered, ...dropped]);
  const next = { ...ob, answered: [...new Set([...ob.answered, ...answered])], skipped: ob.skipped.filter((s) => !gone.has(s)) };
  await saveOnboarding(next).catch(() => {});
  return next;
}

// ---------- Plano e uso (GET /billing) ----------
let billingCache = null;
let billingAt = 0;
export async function getBilling(force = false) {
  if (!force && billingCache && Date.now() - billingAt < 30_000) return billingCache;
  billingCache = null;
  // Sem cache de erro e sem engolir a falha: quem chama decide o que mostrar, e nunca afirma plano sem dado.
  billingCache = await api('/billing');
  billingAt = Date.now();
  return billingCache;
}
export const invalidateBilling = () => { billingCache = null; };

export const listOf = async (path) => asList(await api(path));
