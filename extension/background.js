// Service worker do Zyft: guarda os tokens e faz TODAS as chamadas à API.
// O content script nunca fala com a API e não tem acesso aos tokens.
const DEFAULT_API = 'http://localhost:5080';
const VERSION = chrome.runtime.getManifest().version;

chrome.sidePanel.setPanelBehavior({ openPanelOnActionClick: true }).catch(() => {});
// Garante que content scripts não leiam chrome.storage.session (tokens).
chrome.storage.session.setAccessLevel?.({ accessLevel: 'TRUSTED_CONTEXTS' }).catch(() => {});

async function apiBase() {
  const { apiBase: url } = await chrome.storage.local.get('apiBase');
  return (url || DEFAULT_API).replace(/\/+$/, '');
}
async function getAccess() {
  const { accessToken } = await chrome.storage.session.get('accessToken');
  return accessToken || null;
}
async function getRefresh() {
  const { refreshToken } = await chrome.storage.local.get('refreshToken');
  return refreshToken || null;
}
async function saveTokens(t) {
  const access = t.accessToken ?? t.access_token;
  const refresh = t.refreshToken ?? t.refresh_token;
  if (access) await chrome.storage.session.set({ accessToken: access });
  if (refresh) await chrome.storage.local.set({ refreshToken: refresh });
}
async function clearTokens() {
  await chrome.storage.session.remove(['accessToken']);
  await chrome.storage.local.remove(['refreshToken', 'deviceId']);
}

async function rawFetch(method, path, body, token) {
  const base = await apiBase();
  const headers = { 'X-Zyft-Extension-Version': VERSION, Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (token) headers.Authorization = `Bearer ${token}`;
  let res;
  try {
    res = await fetch(base + path, { method, headers, body: body !== undefined ? JSON.stringify(body) : undefined });
  } catch {
    return { ok: false, status: 0, problem: { title: 'Sem conexão', detail: `Não foi possível falar com a API em ${base}.` } };
  }
  const text = await res.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  if (res.ok) return { ok: true, status: res.status, data };
  const problem = data && typeof data === 'object'
    ? data
    : { title: `Erro ${res.status}`, detail: res.status === 429 ? 'Muitas tentativas. Aguarde um pouco.' : 'A API respondeu com erro.' };
  return { ok: false, status: res.status, problem };
}

// A conta desconectou esta extensão (senha alterada ou redefinida, navegador desconectado na plataforma, conta
// excluída ou conexão expirada). O motivo fica guardado para o painel explicar mesmo se for aberto depois.
const DISCONNECTED = {
  title: 'Extensão desconectada',
  detail: 'A sua conta desconectou esta extensão. Isso acontece quando a senha é alterada ou redefinida, quando este navegador é desconectado na plataforma ou quando a conexão expira. Para continuar, gere um código em "Conectar um navegador", na tela Extensão da plataforma, e conecte de novo.',
};

let refreshing = null;
/**
 * Renova o par de tokens. Resolve { ok: true } ou { ok: false, revoked, status, problem }.
 * Só 401/403 (sessão revogada ou expirada) contam como desconexão; erro passageiro (rede, 5xx, 429) mantém os tokens.
 */
async function refresh() {
  // Uma renovação por vez (o refresh token é rotativo: reuso revoga a cadeia).
  if (!refreshing) {
    refreshing = (async () => {
      const rt = await getRefresh();
      if (!rt) return { ok: false, revoked: true, status: 401 };
      const r = await rawFetch('POST', '/api/v1/auth/refresh', { refreshToken: rt });
      if (r.ok && r.data) { await saveTokens(r.data); return { ok: true }; }
      return { ok: false, revoked: r.status === 401 || r.status === 403, status: r.status, problem: r.problem };
    })().finally(() => { refreshing = null; });
  }
  return refreshing;
}

async function disconnected() {
  await clearTokens();
  await chrome.storage.local.set({ disconnected: { at: Date.now() } });
  return { ok: false, status: 401, paired: false, revoked: true, problem: { ...DISCONNECTED } };
}

/** Falha passageira na renovação: a extensão continua conectada e o painel oferece "Tentar de novo". */
const transient = (rf) => ({ ok: false, status: rf.status || 0, problem: rf.problem || { title: 'Sem conexão', detail: 'Não foi possível renovar o acesso agora. Tente de novo em instantes.' } });

async function callApi(method, path, body) {
  let token = await getAccess();
  if (!token && (await getRefresh())) {
    const rf = await refresh();
    if (!rf.ok) return rf.revoked ? disconnected() : transient(rf);
    token = await getAccess();
  }
  let r = await rawFetch(method, path, body, token);
  if (r.status === 401) {
    const rf = await refresh();
    if (!rf.ok) return rf.revoked ? disconnected() : transient(rf);
    r = await rawFetch(method, path, body, await getAccess());
    if (r.status === 401) return disconnected();
  }
  return r;
}

async function pair(code, deviceName) {
  const r = await rawFetch('POST', '/api/v1/extension/pair', { code, deviceName });
  if (!r.ok) return r;
  // A API responde { device: {id,…}, tokens: {accessToken, refreshToken} }; aceita também o formato plano antigo.
  await saveTokens(r.data?.tokens || r.data || {});
  const deviceId = r.data?.device?.id ?? r.data?.deviceId;
  if (deviceId) await chrome.storage.local.set({ deviceId });
  await chrome.storage.local.remove('disconnected');
  return { ok: true, status: r.status, data: { paired: true } };
}

async function status() {
  const paired = !!(await getRefresh()) || !!(await getAccess());
  const { disconnected: d } = await chrome.storage.local.get('disconnected');
  return { ok: true, data: { paired, apiBase: await apiBase(), disconnected: !paired && d ? { ...DISCONNECTED, at: d.at } : null } };
}

async function unpair() {
  const rt = await getRefresh();
  // Revoga o próprio dispositivo no servidor (libera a vaga de navegador do plano); falha aqui não impede desconectar.
  try { await rawFetch('DELETE', '/api/v1/extension/devices/current', undefined, await getAccess()); } catch { /* segue */ }
  if (rt) await rawFetch('POST', '/api/v1/auth/logout', { refreshToken: rt }, await getAccess());
  await clearTokens();
  await chrome.storage.local.remove('disconnected'); // saída pedida pela pessoa: sem aviso de desconexão
  return { ok: true, data: { paired: false } };
}

chrome.runtime.onMessage.addListener((msg, sender, reply) => {
  // Só aceita mensagens das páginas da própria extensão (painel), nunca de content scripts:
  // content scripts têm sender.url da página do portal; páginas da extensão têm chrome-extension://<id>/
  // (vale também para o painel aberto numa aba com ?tabId=N, usado nos testes).
  const fromExtensionPage = sender.id === chrome.runtime.id && String(sender.url || '').startsWith(chrome.runtime.getURL(''));
  if (!fromExtensionPage) return false;
  const handlers = {
    api: () => callApi(msg.method || 'GET', msg.path, msg.body),
    pair: () => pair(msg.code, msg.deviceName),
    unpair,
    status,
    setApiBase: async () => {
      await chrome.storage.local.set({ apiBase: String(msg.url || DEFAULT_API).replace(/\/+$/, '') });
      return status();
    },
  };
  const h = handlers[msg?.type];
  if (!h) return false;
  h().then(reply, (e) => reply({ ok: false, status: 0, problem: { title: 'Erro interno', detail: String(e?.message || e) } }));
  return true;
});
