// Extensão (RF-09, RF-10): pareamento por código, navegadores conectados e sites autorizados.
// Banner web.banner abaixo do título.
import { api, asList } from '../api.js';
import { h, clear, field, readForm, showErrors, clearErrors, toast, announce, confirmDialog, fmtDate, fmtDateTime } from '../dom.js';
import { Button, Icon, PageHead, Card, PairingCode, LinkButton } from '../ui.js';
import { adSlot, PLACEMENTS } from '../ads.js';
import { getBilling } from '../store.js';
import * as v from '../validate.js';

export async function render(main) {
  const [devices, sites, billing] = await Promise.all([
    api('/extension/devices').then(asList),
    api('/authorized-sites').then(asList),
    getBilling().catch(() => null),
  ]);
  const devicesSlot = h('div');
  const refreshDevices = async () => {
    const fresh = await api('/extension/devices').then(asList).catch(() => null);
    if (fresh) clear(devicesSlot, devicesCard(fresh, billing));
  };
  clear(devicesSlot, devicesCard(devices, billing));

  main.append(h('div', { class: 'content content--md', style: { gap: '32px' } },
    PageHead({ title: 'Extensão', lead: 'A extensão lê só o formulário da vaga aberta e propõe o preenchimento. Conecte cada navegador com um código.' }),
    adSlot(PLACEMENTS.banner),
    h('section', { class: 'two-col', 'aria-labelledby': 'connect-title' },
      h('div', { class: 'stack' },
        h('h2', { class: 'card__title', id: 'connect-title' }, 'Conectar um navegador'),
        h('ol', { class: 'steps' },
          h('li', {}, 'Instale a extensão do Zyft no navegador.'),
          h('li', {}, 'Abra o painel lateral do Zyft e escolha "Conectar".'),
          h('li', {}, 'Digite o código ao lado. Ele vale por 10 minutos e só uma vez.')),
        h('details', { class: 'howto' },
          h('summary', {}, 'Instalar em modo de desenvolvedor'),
          h('p', {}, 'Abra brave://extensions ou chrome://extensions, ligue o Modo do desenvolvedor, clique em "Carregar sem compactação" e escolha a pasta extension do projeto.')),
        h('a', { href: 'demo/index.html', target: '_blank', rel: 'noopener' }, 'Testar no portal de demonstração', h('span', { class: 'sr-only' }, ' (abre em nova aba)'))),
      pairing(refreshDevices)),
    devicesSlot,
    sitesCard(sites)));
}

function pairing(onPaired) {
  const slot = h('div');
  let timer = null;
  const start = () => {
    clearInterval(timer);
    const btn = Button({ variant: 'primary', icon: 'plug' }, 'Gerar código de pareamento');
    btn.addEventListener('click', () => generate(btn));
    clear(slot, h('div', { class: 'pair-start' },
      h('div', { class: 'zy-pair__label' }, 'Código de pareamento'),
      h('p', { class: 'zy-pair__hint' }, 'Gere um código e digite no painel lateral da extensão. Ele vale por 10 minutos e só uma vez.'),
      h('div', {}, btn)));
  };
  const generate = async (btn) => {
    if (btn) btn.disabled = true;
    clearInterval(timer);
    try {
      const r = await api('/extension/pairing-codes', { method: 'POST', body: {} });
      const raw = String(r.code || r.userCode || '');
      const code = raw.length === 6 && !raw.includes('-') ? `${raw.slice(0, 3)}-${raw.slice(3)}` : raw;
      const expires = r.expiresAt ? new Date(r.expiresAt) : new Date(Date.now() + 10 * 60_000);
      const card = PairingCode({
        code, hint: 'Digite este código no painel lateral da extensão.',
        onRenew: () => generate(),
        onCopy: async () => {
          try { await navigator.clipboard.writeText(raw); toast('Código copiado'); } catch { toast('Não foi possível copiar. Digite o código no painel.'); }
        },
      });
      const after = h('p', { class: 'small muted' }, 'Depois de digitar o código na extensão, ',
        h('button', { type: 'button', class: 'zy-link', onclick: onPaired }, 'atualize a lista de navegadores'), '.');
      clear(slot, h('div', { class: 'stack' }, card, after));
      const tick = () => {
        const s = Math.max(0, Math.round((expires - Date.now()) / 1000));
        card.setExpires(`${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`);
        if (!s) { clearInterval(timer); card.setExpires('expirado'); }
      };
      tick();
      timer = setInterval(() => { if (!card.isConnected) clearInterval(timer); else tick(); }, 1000);
      announce(`Código gerado: ${raw.split('').join(' ')}. Vale por 10 minutos.`);
      card.querySelector('.zy-btn--secondary')?.focus();
    } catch (err) {
      if (btn) btn.disabled = false;
      if (err.handled) return;
      toast(err.status === 402 || err.status === 409 ? (err.detail || 'Você chegou ao limite de navegadores do seu plano. Desconecte um para conectar outro.') : err.message, 'err');
    }
  };
  start();
  return slot;
}

function devicesCard(devices, billing) {
  const active = devices.filter((d) => d.active ?? !d.revokedAt);
  const limit = billing?.devicesLimit;
  const body = devices.length ? h('div', { class: 'table-wrap' }, h('table', { class: 'table' },
    h('caption', { class: 'sr-only' }, 'Navegadores conectados'),
    h('thead', {}, h('tr', {}, ['Dispositivo', 'Conectado em', 'Último uso', 'Situação'].map((t) => h('th', { scope: 'col' }, t)), h('th', { scope: 'col' }, h('span', { class: 'sr-only' }, 'Ações')))),
    h('tbody', {}, devices.map((d) => {
      const on = d.active ?? !d.revokedAt;
      const btn = on ? Button({ variant: 'quiet', size: 'sm', 'aria-label': `Desconectar ${d.name}` }, 'Desconectar') : null;
      btn?.addEventListener('click', async () => {
        if (!(await confirmDialog({ title: 'Desconectar este navegador?', message: `A extensão em “${d.name}” perde o acesso na hora e vai precisar de um novo código para voltar.`, confirmText: 'Desconectar', danger: true }))) return;
        try {
          await api(`/extension/devices/${encodeURIComponent(d.id)}`, { method: 'DELETE' });
          d.revokedAt = new Date().toISOString(); d.active = false;
          const card = btn.closest('section');
          card.replaceWith(devicesCard(devices, billing));
          announce(`${d.name} desconectado.`);
        } catch (err) { if (!err.handled) toast(err.message, 'err'); }
      });
      return h('tr', {},
        h('td', { style: { fontWeight: '600' } }, d.name || 'Navegador'),
        h('td', {}, fmtDate(d.pairedAt)),
        h('td', {}, d.lastSeenAt ? fmtDateTime(d.lastSeenAt) : '—'),
        h('td', {}, on ? 'Conectado' : 'Desconectado'),
        h('td', { style: { textAlign: 'right' } }, btn || ''));
    })))) : h('p', { class: 'empty' }, 'Nenhum navegador conectado ainda.');
  return Card({ title: 'Navegadores conectados', titleId: 'dev-title', className: 'card--flush', action: limit != null ? h('span', { class: 'small muted' }, `${active.length} de ${limit} no seu plano`) : null }, body,
    limit != null && active.length >= limit ? h('p', { class: 'small muted', style: { padding: '0 24px 16px' } }, 'Para conectar outro navegador, desconecte um ou ', h('a', { href: '#/planos' }, 'conheça o Plus'), ', que permite até 3.') : null);
}

function sitesCard(sites) {
  const list = h('div');
  const originOf = (s) => (typeof s === 'string' ? s : s.origin);
  const paint = () => clear(list, sites.length ? sites.map((s) => {
    const btn = Button({ variant: 'ghost', size: 'sm', 'aria-label': `Remover ${originOf(s)}` }, 'Remover');
    btn.addEventListener('click', async () => {
      try {
        await api(`/authorized-sites/${encodeURIComponent(s.id ?? originOf(s))}`, { method: 'DELETE' });
        sites = sites.filter((x) => x !== s);
        paint();
        announce(`${originOf(s)} removido.`);
        form.querySelector('input').focus();
      } catch (err) { if (!err.handled) toast(err.message, 'err'); }
    });
    return h('div', { class: 'site' }, Icon('globe'), h('span', { class: 'site__origin' }, originOf(s)),
      h('span', { class: 'caption' }, s.createdAt ? `Desde ${fmtDate(s.createdAt)}` : ''), btn);
  }) : h('p', { class: 'muted small' }, 'Nenhum site autorizado além do portal de demonstração.'));

  const form = h('form', { novalidate: true, class: 'row-end', 'aria-label': 'Autorizar site' },
    h('div', { class: 'grow', style: { flexBasis: '320px' } }, field({ name: 'origin', label: 'Endereço do site', type: 'url', placeholder: 'https://vagas.exemplo.com' })),
    Button({ variant: 'secondary', type: 'submit' }, 'Autorizar site'));
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    clearErrors(form);
    let { origin } = readForm(form);
    if (origin && !/^https?:\/\//i.test(origin)) origin = `https://${origin}`;
    const err = v.url(origin) ? 'Informe o endereço do site, como https://vagas.exemplo.com.' : null;
    if (!origin || err) return showErrors(form, { origin: [err || 'Informe o endereço do site.'] });
    origin = new URL(origin).origin;
    try {
      const r = await api('/authorized-sites', { method: 'POST', body: { origin } });
      sites.push(r && typeof r === 'object' ? r : { origin });
      paint();
      form.reset();
      toast('Site autorizado', 'ok');
    } catch (error) { if (!error.handled) showErrors(form, error); }
  });
  paint();
  return Card({ title: 'Sites autorizados', titleId: 'sites-title' },
    h('p', { class: 'card__sub' }, 'A extensão só lê formulários nestes endereços. Em outro site, ela pede a sua permissão antes.'),
    form, list);
}

void LinkButton;
