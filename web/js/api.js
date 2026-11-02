// Cliente HTTP único da plataforma web.
// - Prefixo /api/v1, JSON por padrão (FormData passa direto).
// - Injeta "Authorization: Bearer <access token>".
// - Ao receber 401, tenta renovar UMA vez com o refresh token (rotação) e repete a chamada.
// - Erros viram ApiError com os campos de ProblemDetails (title, detail, errors por campo).
// - Só 401/403 na renovação encerram a sessão: erro passageiro vira mensagem e o refresh token fica.
// - Toda chamada tem tempo limite, com mensagem própria.
//
// Armazenamento: o access token (15 min) fica só em memória. O refresh token fica em
// sessionStorage (some ao fechar a aba). Nenhum token vai para localStorage; lá fica só o rascunho do
// onboarding (views/onboarding.js), apagado ao concluir, ao sair da conta e depois de 7 dias.

export const API_BASE = '/api/v1';
const RT_KEY = 'zyft.refreshToken';

let accessToken = null;
let refreshing = null;
let leaving = false; // saída pedida pela pessoa: um 401 no caminho não é "sessão expirada"
let sessionNotice = '';
let returnRoute = '';
const listeners = new Set();

const TIMEOUT_MS = 20_000;
const UPLOAD_TIMEOUT_MS = 120_000; // a leitura do currículo responde depois do envio do arquivo
const TIMED_OUT = { timedOut: true, ok: false, status: 0 };

const SESSION_EXPIRED = 'Sua sessão expirou. Entre de novo para continuar de onde parou.';
const SESSION_REUSED = 'Por segurança, encerramos esta sessão porque ela foi usada em outra aba. Entre de novo para continuar.';
const SESSION_GONE = 'Esta conta não existe mais. A sessão foi encerrada.';

export class ApiError extends Error {
  constructor(status, problem = {}) {
    const errors = normalizeErrors(problem.errors);
    const lines = describeErrors(errors);
    // 400 com errors por campo: a mensagem diz QUAL campo (com nome legível), não só "campos inválidos".
    super(lines.length ? lines.join(' ') : problem.detail || problem.title || defaultMessage(status));
    this.status = status;
    this.title = problem.title || '';
    this.detail = problem.detail || '';
    this.type = problem.type || '';
    this.code = problem.code || problem.extensions?.code || '';
    this.errors = errors;
    this.problem = problem;
  }
}

// ---------- Nomes legíveis dos campos de erro (ValidationProblemDetails) ----------
// A API devolve chaves técnicas ("educations[0].status", "type"). A interface mostra o nome que a
// pessoa vê na tela ("Formação 1 · Situação"). As telas podem registrar mais rótulos (profile-schema.js).
const FIELD_LABELS = {
  body: 'Requisição', name: 'Nome', email: 'E-mail', password: 'Senha', currentPassword: 'Senha atual', newPassword: 'Nova senha',
  fullName: 'Nome completo', phone: 'Telefone', type: 'Tipo', status: 'Situação', level: 'Nível', proficiency: 'Nível',
  institution: 'Instituição', course: 'Curso', company: 'Empresa', role: 'Cargo', startDate: 'Início', endDate: 'Fim',
  completedAt: 'Concluído em', hours: 'Carga horária', isCurrent: 'Trabalho atual', question: 'Pergunta', text: 'Texto',
  value: 'Valor', label: 'Nome do campo', period: 'Período', scope: 'Opção', origin: 'Endereço do site', code: 'Código',
  file: 'Arquivo', content: 'Arquivo', fileName: 'Arquivo', granted: 'Consentimento', kind: 'Tipo de dado', maxLength: 'Limite de caracteres',
  acceptTerms: 'Termos de uso', termsAccepted: 'Termos de uso', termsVersion: 'Termos de uso', token: 'Link',
};
const SECTION_LABELS = {
  experiences: 'Experiência', educations: 'Formação', courses: 'Curso ou certificação', skills: 'Competência',
  languages: 'Idioma', fields: 'Campo', profile: 'Dados básicos', customFields: 'Informação da área',
};

/** Acrescenta rótulos legíveis ({ chave: 'Rótulo' }); usado pelas telas com campos próprios. */
export function registerFieldLabels(map) { Object.assign(FIELD_LABELS, map || {}); }

/** "educations[0].status" → "Formação 1 · Situação"; "type" → "Tipo"; chave desconhecida vira texto legível. */
export function fieldLabel(key) {
  const parts = String(key || '').split('.').filter(Boolean);
  const out = [];
  for (const part of parts) {
    const m = part.match(/^([^[]+)(?:\[(\d+)\])?$/);
    const base = m ? m[1] : part;
    const idx = m && m[2] !== undefined ? Number(m[2]) + 1 : null;
    if (SECTION_LABELS[base] && (idx || parts.length > 1)) out.push(idx ? `${SECTION_LABELS[base]} ${idx}` : SECTION_LABELS[base]);
    else out.push(FIELD_LABELS[base] || humanize(base));
  }
  return out.join(' · ') || 'Campo';
}
const humanize = (k) => {
  const s = String(k).replace(/([a-z])([A-Z])/g, '$1 $2').replace(/[_-]+/g, ' ').toLowerCase().trim();
  return s ? s.charAt(0).toUpperCase() + s.slice(1) : 'Campo';
};

/** { campo: [mensagens] } → ["Formação 1 · Situação: Informe a situação."]. */
export function describeErrors(errors) {
  const out = [];
  for (const [k, msgs] of Object.entries(errors || {})) {
    const text = [].concat(msgs).filter(Boolean).join(' ');
    if (!text) continue;
    const label = fieldLabel(k);
    // A API às vezes já nomeia o item ("Formação “ADM – UEM”: escolha a situação…"): não repete o rótulo.
    const head = label.split(' · ')[0].replace(/ \d+$/, '');
    out.push(text.startsWith(head) ? text : `${label}: ${text}`);
  }
  return out;
}

// 402 plan.limit_reached: qualquer tela recebe o mesmo diálogo amigável (billing.js registra).
let limitHandler = null;
export const onLimitReached = (fn) => { limitHandler = fn; };
export const isLimitError = (e) => e instanceof ApiError && e.status === 402;

function defaultMessage(status) {
  if (status === 0) return 'Não foi possível falar com o servidor. Verifique sua conexão e tente de novo.';
  if (status === 401) return 'Sua sessão expirou. Entre de novo.';
  if (status === 403) return 'Você não tem permissão para esta ação.';
  if (status === 404) return 'Não encontramos o que você procurou.';
  if (status === 402) return 'Você chegou ao limite do seu plano.';
  if (status === 409) return 'Esta ação entra em conflito com dados existentes.';
  if (status === 413) return 'O arquivo é grande demais.';
  if (status === 429) return 'Muitas tentativas em pouco tempo. Aguarde um minuto e tente de novo.';
  if (status >= 500) return 'O servidor teve um problema. Tente de novo em instantes.';
  return 'Não foi possível concluir a ação.';
}

// ProblemDetails de validação: { errors: { "Email": ["..."], "Experiences[0].StartDate": ["..."] } }.
// Normalizamos as chaves para camelCase em minúsculas na primeira letra de cada segmento.
function normalizeErrors(errors) {
  const out = {};
  if (!errors || typeof errors !== 'object') return out;
  for (const [key, val] of Object.entries(errors)) {
    const k = key.replace(/^\$\./, '').split('.').map((s) => s.charAt(0).toLowerCase() + s.slice(1)).join('.');
    out[k] = [].concat(val).map(String);
  }
  return out;
}

/** `reason` diz por que a sessão acabou sem a pessoa pedir: 'expired' ou 'gone' (conta excluída). */
export function setTokens({ accessToken: at, refreshToken: rt } = {}, reason) {
  accessToken = at || null;
  try {
    if (rt) sessionStorage.setItem(RT_KEY, rt);
    else sessionStorage.removeItem(RT_KEY);
  } catch { /* armazenamento bloqueado: a sessão vale só até recarregar */ }
  listeners.forEach((fn) => fn(isLoggedIn(), reason));
}

function endSession(notice, reason = 'expired') {
  if (!isLoggedIn()) return;
  if (!leaving) sessionNotice = notice;
  setTokens({}, reason);
}

/** Aviso para #/entrar depois de a sessão acabar sozinha. */
export const getSessionNotice = () => sessionNotice;
/** Guarda a rota para voltar depois de entrar. */
export const rememberRoute = (hash) => { returnRoute = hash || ''; };
/** Entrou: devolve a rota guardada e limpa o aviso. */
export function takeReturnRoute() {
  const hash = returnRoute;
  returnRoute = '';
  sessionNotice = '';
  return hash;
}

function getRefreshToken() {
  try { return sessionStorage.getItem(RT_KEY); } catch { return null; }
}

export const isLoggedIn = () => Boolean(accessToken || getRefreshToken());
export const onAuthChange = (fn) => listeners.add(fn);

async function rawFetch(path, { method = 'GET', body, headers = {}, auth = true, timeout, keepalive } = {}) {
  const isForm = body instanceof FormData;
  const h = { Accept: 'application/json, application/problem+json', ...headers };
  if (body !== undefined && !isForm) h['Content-Type'] = 'application/json';
  if (auth && accessToken) h.Authorization = `Bearer ${accessToken}`;
  const abort = new AbortController();
  const timer = setTimeout(() => abort.abort(), timeout ?? (isForm ? UPLOAD_TIMEOUT_MS : TIMEOUT_MS));
  try {
    return await fetch(API_BASE + path, {
      method,
      headers: h,
      body: body === undefined ? undefined : isForm ? body : JSON.stringify(body),
      signal: abort.signal,
      keepalive, // envio ao fechar a aba (rascunho do onboarding)
    });
  } catch {
    return abort.signal.aborted ? TIMED_OUT : null; // null: erro de rede
  } finally {
    clearTimeout(timer);
  }
}

async function parse(res) {
  if (!res) throw new ApiError(0);
  if (res.timedOut) throw new ApiError(0, { detail: 'O servidor demorou demais para responder. Verifique sua conexão e tente de novo.', code: 'timeout' });
  if (res.status === 204) return null;
  const ct = res.headers.get('content-type') || '';
  const data = ct.includes('json') ? await res.json().catch(() => null) : await res.text().catch(() => null);
  if (!res.ok) {
    const err = new ApiError(res.status, data && typeof data === 'object' ? data : {});
    // O token de acesso sobrevive à exclusão da conta em outra aba: a API responde 404 para a própria conta.
    if (res.status === 404 && err.detail === 'Conta não encontrada.') endSession(SESSION_GONE, 'gone');
    if (res.status === 402 && limitHandler) {
      err.handled = true;
      try { limitHandler(err); } catch { /* o diálogo nunca derruba a tela */ }
    }
    throw err;
  }
  return data;
}

// Renovação: uma chamada por vez, compartilhada por quem receber 401 ao mesmo tempo.
// Resolve null se renovou, ou o ApiError que a chamada original deve lançar.
export function refresh() {
  const rt = getRefreshToken();
  if (!rt) return Promise.resolve(new ApiError(401));
  if (!refreshing) {
    refreshing = (async () => {
      const res = await rawFetch('/auth/refresh', { method: 'POST', body: { refreshToken: rt }, auth: false });
      if (res && res.ok) {
        const tokens = await res.json().catch(() => null);
        if (!tokens?.accessToken) return new ApiError(502);
        setTokens(tokens);
        return null;
      }
      if (res && (res.status === 401 || res.status === 403)) { // refresh inválido ou reutilizado
        const problem = await res.json().catch(() => ({}));
        endSession(/reutilizado/i.test(problem?.detail || '') ? SESSION_REUSED : SESSION_EXPIRED);
        return new ApiError(401);
      }
      // Passageiro (5xx, rede, tempo limite): o refresh token continua valendo para a próxima tentativa.
      return parse(res).then(() => new ApiError(0), (err) => err);
    })().finally(() => { refreshing = null; });
  }
  return refreshing;
}

async function renew() {
  const failure = await refresh();
  if (failure) throw failure;
}

/**
 * Chamada à API. Ex.: api('/profile'), api('/answers', { method: 'POST', body: {...} }).
 * Lança ApiError em caso de erro.
 */
export async function api(path, opts = {}) {
  const authed = opts.auth !== false;
  if (authed && !accessToken && getRefreshToken()) await renew();
  let res = await rawFetch(path, opts);
  if (res && res.status === 401 && authed) {
    if (getRefreshToken()) {
      await renew();
      res = await rawFetch(path, opts);
    }
    if (res && res.status === 401) endSession(SESSION_EXPIRED);
  }
  return parse(res);
}

/** Baixa um recurso autenticado como arquivo (ex.: exportação JSON). */
export async function download(path, filename) {
  const data = await api(path);
  const blob = new Blob([typeof data === 'string' ? data : JSON.stringify(data, null, 2)], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = Object.assign(document.createElement('a'), { href: url, download: filename });
  document.body.append(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

// Atalhos usados pelas telas.
export const get = (p) => api(p);
export const post = (p, body) => api(p, { method: 'POST', body: body ?? {} });
export const put = (p, body) => api(p, { method: 'PUT', body: body ?? {} });
export const patch = (p, body) => api(p, { method: 'PATCH', body: body ?? {} });
export const del = (p) => api(p, { method: 'DELETE' });

/** Aceita lista pura ou envelopes comuns ({ items }, { data }, { value }). */
export const asList = (r) => (Array.isArray(r) ? r : r?.items || r?.data || r?.value || []);

export async function login(email, password) {
  const r = await api('/auth/login', { method: 'POST', body: { email, password }, auth: false });
  setTokens(r);
  return r;
}

/** Versão vigente dos termos de uso e da política de privacidade (GET /auth/terms, público). */
export async function getTermsVersion() {
  const r = await api('/auth/terms', { auth: false });
  if (!r?.version) throw new ApiError(502, { detail: 'Não foi possível carregar a versão dos termos. Tente de novo.' });
  return r.version;
}

/** Cadastro com aceite explícito dos termos (`acceptTerms` + `termsVersion` vigente). */
export async function register(name, email, password, { acceptTerms = false, termsVersion = null } = {}) {
  const r = await api('/auth/register', { method: 'POST', body: { name, email, password, acceptTerms, termsVersion }, auth: false });
  if (r && r.accessToken) setTokens(r);
  else await login(email, password); // caso o cadastro não devolva tokens
  return r;
}

export async function logout() {
  const rt = getRefreshToken();
  leaving = true;
  // Sair nunca espera a rede: sem resposta em alguns segundos, encerra aqui mesmo.
  if (rt) await Promise.race([api('/auth/logout', { method: 'POST', body: { refreshToken: rt } }).catch(() => {}), new Promise((done) => { setTimeout(done, 6000); })]);
  leaving = false;
  takeReturnRoute();
  setTokens({});
}

/** Confirma o e-mail com o token do link (público: funciona com ou sem login). */
export const verifyEmail = (token) => api('/auth/verify', { method: 'POST', body: { token }, auth: false });
/** Pede um novo link de confirmação para o e-mail da conta (202). */
export const resendVerification = () => api('/auth/verify/resend', { method: 'POST', body: {} });
