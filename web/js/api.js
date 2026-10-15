// Cliente HTTP único da plataforma web.
// - Prefixo /api/v1, JSON por padrão (FormData passa direto).
// - Injeta "Authorization: Bearer <access token>".
// - Ao receber 401, tenta renovar UMA vez com o refresh token (rotação) e repete a chamada.
// - Erros viram ApiError com os campos de ProblemDetails (title, detail, errors por campo).
//
// Armazenamento: o access token (15 min) fica só em memória. O refresh token fica em
// sessionStorage (some ao fechar a aba). Nada vai para localStorage.

export const API_BASE = '/api/v1';
const RT_KEY = 'zyft.refreshToken';

let accessToken = null;
let refreshing = null;
const listeners = new Set();

export class ApiError extends Error {
  constructor(status, problem = {}) {
    super(problem.detail || problem.title || defaultMessage(status));
    this.status = status;
    this.title = problem.title || '';
    this.detail = problem.detail || '';
    this.type = problem.type || '';
    this.errors = normalizeErrors(problem.errors);
    this.problem = problem;
  }
}

function defaultMessage(status) {
  if (status === 0) return 'Não foi possível falar com o servidor. Verifique sua conexão e tente de novo.';
  if (status === 401) return 'Sua sessão expirou. Entre de novo.';
  if (status === 403) return 'Você não tem permissão para esta ação.';
  if (status === 404) return 'Não encontramos o que você procurou.';
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

export function setTokens({ accessToken: at, refreshToken: rt } = {}) {
  accessToken = at || null;
  try {
    if (rt) sessionStorage.setItem(RT_KEY, rt);
    else sessionStorage.removeItem(RT_KEY);
  } catch { /* armazenamento bloqueado: a sessão vale só até recarregar */ }
  listeners.forEach((fn) => fn(isLoggedIn()));
}

function getRefreshToken() {
  try { return sessionStorage.getItem(RT_KEY); } catch { return null; }
}

export const isLoggedIn = () => Boolean(accessToken || getRefreshToken());
export const onAuthChange = (fn) => listeners.add(fn);

async function rawFetch(path, { method = 'GET', body, headers = {}, auth = true } = {}) {
  const isForm = body instanceof FormData;
  const h = { Accept: 'application/json, application/problem+json', ...headers };
  if (body !== undefined && !isForm) h['Content-Type'] = 'application/json';
  if (auth && accessToken) h.Authorization = `Bearer ${accessToken}`;
  try {
    return await fetch(API_BASE + path, {
      method,
      headers: h,
      body: body === undefined ? undefined : isForm ? body : JSON.stringify(body),
    });
  } catch {
    return null; // erro de rede
  }
}

async function parse(res) {
  if (!res) throw new ApiError(0);
  if (res.status === 204) return null;
  const ct = res.headers.get('content-type') || '';
  const data = ct.includes('json') ? await res.json().catch(() => null) : await res.text().catch(() => null);
  if (!res.ok) throw new ApiError(res.status, data && typeof data === 'object' ? data : {});
  return data;
}

// Renovação: uma chamada por vez, compartilhada por quem receber 401 ao mesmo tempo.
export function refresh() {
  const rt = getRefreshToken();
  if (!rt) return Promise.resolve(false);
  if (!refreshing) {
    refreshing = (async () => {
      const res = await rawFetch('/auth/refresh', { method: 'POST', body: { refreshToken: rt }, auth: false });
      if (res && res.ok) {
        setTokens(await res.json());
        return true;
      }
      if (res && (res.status === 400 || res.status === 401)) setTokens({}); // refresh inválido ou reutilizado
      return false;
    })().finally(() => { refreshing = null; });
  }
  return refreshing;
}

/**
 * Chamada à API. Ex.: api('/profile'), api('/answers', { method: 'POST', body: {...} }).
 * Lança ApiError em caso de erro.
 */
export async function api(path, opts = {}) {
  if (opts.auth !== false && !accessToken && getRefreshToken()) await refresh();
  let res = await rawFetch(path, opts);
  if (res && res.status === 401 && opts.auth !== false && getRefreshToken()) {
    if (await refresh()) res = await rawFetch(path, opts);
  }
  if (res && res.status === 401 && opts.auth !== false) setTokens({});
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

export async function register(name, email, password) {
  const r = await api('/auth/register', { method: 'POST', body: { name, email, password }, auth: false });
  if (r && r.accessToken) setTokens(r);
  else await login(email, password); // caso o cadastro não devolva tokens
  return r;
}

export async function logout() {
  const rt = getRefreshToken();
  try { if (rt) await api('/auth/logout', { method: 'POST', body: { refreshToken: rt } }); } catch { /* sai mesmo assim */ }
  setTokens({});
}
