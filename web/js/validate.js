// Validações do frontend que espelham as do backend (FluentValidation).
// O backend continua sendo a fonte de verdade: estas regras só evitam ida e volta à toa.

export const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
export const MAX_UPLOAD_BYTES = 10 * 1024 * 1024;
export const ALLOWED_EXT = ['.pdf', '.docx'];
export const ALLOWED_MIME = [
  'application/pdf',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
];

export function email(v) {
  if (!v) return 'Informe seu e-mail.';
  if (v.length > 254 || !EMAIL_RE.test(v)) return 'Informe um e-mail válido, como nome@exemplo.com.';
  return null;
}

export function password(v) {
  if (!v) return 'Crie uma senha.';
  if (v.length < 8) return 'A senha precisa ter pelo menos 8 caracteres.';
  if (!/[A-Za-zÀ-ÿ]/.test(v) || !/\d/.test(v)) return 'A senha precisa ter pelo menos uma letra e um número.';
  return null;
}

export function required(v, msg = 'Campo obrigatório.') {
  return v === null || v === undefined || String(v).trim() === '' ? msg : null;
}

export function maxLen(v, n) {
  return v && String(v).length > n ? `Use no máximo ${n} caracteres.` : null;
}

/** Datas como "AAAA-MM" ou "AAAA-MM-DD". Fim precisa ser igual ou depois do início. */
export function dateRange(start, end) {
  if (!start || !end) return null;
  return normalizeDate(end) < normalizeDate(start) ? 'A data de fim deve ser igual ou posterior à de início.' : null;
}

export function notFuture(v, msg = 'A data não pode estar no futuro.') {
  if (!v) return null;
  const today = new Date().toISOString().slice(0, 10);
  return normalizeDate(v) > today ? msg : null;
}

/** "2021-02" → "2021-02-01"; "2021-02-15" fica igual. */
export function normalizeDate(v) {
  if (!v) return null;
  const s = String(v);
  return /^\d{4}-\d{2}$/.test(s) ? `${s}-01` : s.slice(0, 10);
}

export function url(v) {
  if (!v) return null;
  try {
    const u = new URL(v);
    return u.protocol === 'http:' || u.protocol === 'https:' ? null : 'Use um endereço que comece com http:// ou https://.';
  } catch {
    return 'Informe um endereço completo, como https://www.linkedin.com/in/seu-nome.';
  }
}

export function origin(v) {
  const e = url(v);
  if (e) return 'Informe a origem do site, como https://vagas.exemplo.com.';
  const u = new URL(v);
  return u.pathname !== '/' || u.search || u.hash ? 'Informe só a origem (protocolo e domínio), sem caminho.' : null;
}

export function resumeFile(file) {
  if (!file) return 'Escolha um arquivo.';
  const name = file.name.toLowerCase();
  const okExt = ALLOWED_EXT.some((e) => name.endsWith(e));
  const okMime = !file.type || ALLOWED_MIME.includes(file.type);
  if (!okExt || !okMime) return 'Envie um arquivo PDF ou DOCX.';
  if (file.size === 0) return 'O arquivo está vazio.';
  if (file.size > MAX_UPLOAD_BYTES) return 'O arquivo passa de 10 MB. Envie uma versão menor.';
  return null;
}

/** Junta resultados { campo: mensagem|null } num mapa de erros { campo: [mensagem] }. */
export function collect(checks) {
  const out = {};
  for (const [k, msg] of Object.entries(checks)) if (msg) out[k] = [msg];
  return out;
}

export const hasErrors = (m) => Object.keys(m).length > 0;
