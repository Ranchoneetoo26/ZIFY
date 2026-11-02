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
  if (v.length > 128) return 'A senha deve ter no máximo 128 caracteres.';
  if (!/\p{L}/u.test(v) || !/\d/.test(v)) return 'A senha precisa ter pelo menos uma letra e um número.';
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

// ---------- Números digitados à mão ----------
export const MONEY_MAX = 999999999.99;
export const HOURS_MAX = 10000;
const MONEY_HINT = 'Informe só o valor, por exemplo 4.500,00.';

/**
 * Valor em reais como a pessoa escreve: "4.500" = 4500, "4,50" = 4,5, "4.500,00" = 4500.
 * Devolve { value, error }. Vazio apaga (value null, sem erro); nada inválido vira null em silêncio.
 */
export function parseMoney(input) {
  const t = String(input ?? '').trim().replace(/^R\$/i, '').replace(/\s/g, '');
  if (t === '') return { value: null, error: null };
  if (/^-/.test(t)) return { value: null, error: 'O valor não pode ser negativo.' };
  let normal = null;
  if (/^\d+$/.test(t)) normal = t;
  else if (/^\d{1,3}(\.\d{3})+(,\d+)?$/.test(t)) normal = t.replace(/\./g, '').replace(',', '.');
  else if (/^\d+,\d+$/.test(t)) normal = t.replace(',', '.');
  else if (/^\d+\.\d+$/.test(t)) normal = t;
  if (normal === null) return { value: null, error: MONEY_HINT };
  if ((normal.split('.')[1] || '').length > 2) return { value: null, error: 'Use no máximo 2 casas decimais.' };
  const value = Number(normal);
  if (!(value <= MONEY_MAX)) return { value: null, error: 'O valor máximo é R$ 999.999.999,99.' };
  return { value, error: null };
}

/** Carga horária: inteiro de 1 a 10000. Vazio apaga. Devolve { value, error }. */
export function parseHours(input) {
  const t = String(input ?? '').trim();
  if (t === '') return { value: null, error: null };
  const value = /^\d+$/.test(t) ? Number(t) : NaN;
  if (!(value >= 1 && value <= HOURS_MAX)) return { value: null, error: 'Informe um número inteiro de horas, de 1 a 10000.' };
  return { value, error: null };
}

// ---------- Contato e endereço ----------
const plain = (s) => String(s ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().replace(/\s+/g, ' ').trim();
const UF_NAMES = {
  AC: 'Acre', AL: 'Alagoas', AP: 'Amapá', AM: 'Amazonas', BA: 'Bahia', CE: 'Ceará', DF: 'Distrito Federal', ES: 'Espírito Santo', GO: 'Goiás',
  MA: 'Maranhão', MT: 'Mato Grosso', MS: 'Mato Grosso do Sul', MG: 'Minas Gerais', PA: 'Pará', PB: 'Paraíba', PR: 'Paraná', PE: 'Pernambuco',
  PI: 'Piauí', RJ: 'Rio de Janeiro', RN: 'Rio Grande do Norte', RS: 'Rio Grande do Sul', RO: 'Rondônia', RR: 'Roraima', SC: 'Santa Catarina',
  SP: 'São Paulo', SE: 'Sergipe', TO: 'Tocantins',
};
const UF_BY_NAME = Object.fromEntries(Object.entries(UF_NAMES).map(([uf, name]) => [plain(name), uf]));
/** "São Paulo" → "SP"; nome desconhecido → null. */
export const ufFromName = (s) => UF_BY_NAME[plain(s)] || null;
export const CNH_CATEGORIES = ['A', 'B', 'C', 'D', 'E', 'AB', 'AC', 'AD', 'AE', 'ACC'];
const isBrazil = (country) => !plain(country) || ['brasil', 'brazil', 'br'].includes(plain(country));

export function phone(v) {
  if (!v) return null;
  const s = String(v).trim();
  if (s.length > 40) return 'Use no máximo 40 caracteres.';
  if (!/^[0-9()+\-\s.]*$/.test(s)) return 'O telefone só pode ter números, espaços, parênteses, + e -.';
  const digits = s.replace(/\D/g, '').length;
  if (s.startsWith('+') || digits > 11) return digits >= 8 ? null : 'Informe o telefone completo, com o código do país.';
  return digits >= 10 ? null : 'Informe o telefone com DDD, como (44) 99999-1111.';
}

/** CEP e UF só seguem o formato brasileiro quando o país é o Brasil (ou não foi informado). */
export function cep(v, country) {
  if (!v || !isBrazil(country)) return null;
  return /^\d{5}-?\d{3}$/.test(String(v).trim()) ? null : 'Informe o CEP com 8 números, como 87000-000.';
}

export function uf(v, country) {
  if (!v || !isBrazil(country)) return null;
  return UF_NAMES[String(v).trim().toUpperCase()] ? null : 'Informe a sigla de um dos 27 estados, como MG ou SP.';
}

export function cnh(v) {
  if (!v) return null;
  return CNH_CATEGORIES.includes(String(v).trim().toUpperCase()) ? null : `Use uma categoria válida: ${CNH_CATEGORIES.join(', ')}.`;
}

/** Endereço de perfil: além de http(s), o domínio precisa existir de fato ("x" não serve). */
export function webUrl(v, max = 300) {
  if (!v) return null;
  if (String(v).length > max) return `Use no máximo ${max} caracteres.`;
  const e = url(v);
  if (e) return e;
  return new URL(v).hostname.includes('.') ? null : 'Informe um endereço completo, como https://www.linkedin.com/in/seu-nome.';
}

/** Junta resultados { campo: mensagem|null } num mapa de erros { campo: [mensagem] }. */
export function collect(checks) {
  const out = {};
  for (const [k, msg] of Object.entries(checks)) if (msg) out[k] = [msg];
  return out;
}

export const hasErrors = (m) => Object.keys(m).length > 0;
