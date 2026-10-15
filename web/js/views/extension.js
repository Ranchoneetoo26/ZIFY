// Extensão (RF-09, RF-10): pareamento por código, dispositivos e sites autorizados.
import { api, asList } from '../api.js';
import { h, clear, field, readForm, showErrors, clearErrors, toast, announce, confirmDialog, fmtDateTime, badge } from '../dom.js';
import * as v from '../validate.js';

export async function render(main) {
  const [devices, sites] = await Promise.all([api('/extension/devices').then(asList), api('/authorized-sites').then(asList)]);
  main.append(
    h('div', { class: 'page-head' }, h('div', {},
      h('h1', {}, 'Extensão do navegador'),
      h('p', { class: 'lead' }, 'A extensão é o copiloto que fica ao lado do portal de vagas: lê o formulário, mostra o que dá para preencher e espera a sua decisão. Ela nunca envia a candidatura.'))),
    h('div', { class: 'grid2' }, pairingCard(() => refreshDevices()), installCard()),
    devicesCard(devices),
    sitesCard(sites));

  async function refreshDevices() {
    const fresh = asList(await api('/extension/devices'));
    main.querySelector('#devices-card').replaceWith(devicesCard(fresh));
  }
}

// ---------- Código de pareamento ----------
function pairingCard(onPaired) {
  const out = h('div', { 'aria-live': 'polite' });
  const btn = h('button', { class: 'primary', type: 'button' }, 'Gerar código de pareamento');
  let timer = null;

  btn.addEventListener('click', async () => {
    btn.disabled = true;
    clearInterval(timer);
    try {
      const r = await api('/extension/pairing-codes', { method: 'POST', body: {} });
      const code = String(r.code || r.userCode || '');
      const expires = r.expiresAt ? new Date(r.expiresAt) : new Date(Date.now() + 10 * 60_000);
      const left = h('span', {});
      const tick = () => {
        const s = Math.max(0, Math.round((expires - Date.now()) / 1000));
        left.textContent = s ? `Vale por mais ${Math.floor(s / 60)} min ${String(s % 60).padStart(2, '0')} s.` : 'Código expirado. Gere outro.';
        if (!s) { clearInterval(timer); codeEl.classList.add('muted'); }
      };
      const codeEl = h('div', { class: 'pair-code', 'aria-label': `Código: ${code.split('').join(' ')}` }, code);
      clear(out,
        h('p', {}, 'Digite este código no painel da extensão:'),
        codeEl,
        h('p', { class: 'small muted' }, left, ' Use uma vez só. Depois de parear, clique em “Atualizar lista”.'),
        h('button', { type: 'button', onclick: onPaired }, 'Atualizar lista de dispositivos'));
      tick();
      timer = setInterval(tick, 1000);
      announce(`Código gerado: ${code.split('').join(' ')}. Vale por 10 minutos.`);
    } catch (err) {
      clear(out, h('p', { class: 'field-error', role: 'alert' }, err.message));
    } finally {
      btn.disabled = false;
    }
  });

  return h('section', { class: 'card', 'aria-labelledby': 'pair-title' },
    h('h2', { id: 'pair-title' }, 'Conectar a extensão'),
    h('ol', { class: 'small' },
      h('li', {}, 'Abra o painel da extensão Zyft (ícone na barra do navegador).'),
      h('li', {}, 'Gere o código aqui e digite os 6 números no painel.'),
      h('li', {}, 'Pronto: a extensão passa a usar o seu Perfil Mestre.')),
    btn, out);
}

function installCard() {
  return h('section', { class: 'card', 'aria-labelledby': 'install-title' },
    h('h2', { id: 'install-title' }, 'Instalar em modo de desenvolvimento'),
    h('ol', { class: 'small' },
      h('li', {}, 'Abra ', h('code', {}, 'chrome://extensions'), ' (ou ', h('code', {}, 'edge://extensions'), ') e ligue o Modo do desenvolvedor.'),
      h('li', {}, 'Clique em “Carregar sem compactação” e escolha a pasta ', h('code', {}, 'extension/'), ' do repositório.'),
      h('li', {}, 'Para testar sem portal real, use o ', h('a', { href: 'demo/index.html', target: '_blank', rel: 'noopener' }, 'portal de demonstração'), '.')),
    h('p', { class: 'small muted' }, 'O Zyft não pede nem guarda a senha dos portais. O login no portal é sempre feito por você.'));
}

// ---------- Dispositivos ----------
function devicesCard(devices) {
  const body = h('div', { class: 'table-wrap' });
  const paint = () => clear(body, devices.length
    ? h('table', {},
      h('caption', { class: 'sr-only' }, 'Dispositivos pareados'),
      h('thead', {}, h('tr', {}, ['Dispositivo', 'Pareado em', 'Último uso', 'Situação', 'Ações'].map((t) => h('th', { scope: 'col' }, t)))),
      h('tbody', {}, devices.map((d) => {
        const revoked = Boolean(d.revokedAt);
        const btn = revoked ? null : h('button', { type: 'button', class: 'link danger' }, 'Desconectar', h('span', { class: 'sr-only' }, ` ${d.name}`));
        btn?.addEventListener('click', async () => {
          if (!(await confirmDialog({ title: 'Desconectar dispositivo?', message: `A extensão em “${d.name}” perde o acesso na hora e precisará de um novo código para voltar.`, confirmText: 'Desconectar', danger: true }))) return btn.focus();
          try {
            await api(`/extension/devices/${encodeURIComponent(d.id)}`, { method: 'DELETE' });
            d.revokedAt = new Date().toISOString();
            paint();
            announce(`${d.name} desconectado.`);
          } catch (err) { toast(err.message, 'err'); }
        });
        return h('tr', {},
          h('td', {}, d.name || 'Navegador'),
          h('td', {}, fmtDateTime(d.pairedAt)),
          h('td', {}, d.lastSeenAt ? fmtDateTime(d.lastSeenAt) : '—'),
          h('td', {}, revoked ? badge('Desconectado', 'revoked', '○') : badge('Ativo', 'on', '●')),
          h('td', {}, btn || '—'));
      })))
    : h('p', { class: 'empty' }, 'Nenhuma extensão pareada ainda.'));
  paint();
  return h('section', { class: 'card', id: 'devices-card', 'aria-labelledby': 'dev-title' }, h('h2', { id: 'dev-title' }, 'Dispositivos pareados'), body);
}

// ---------- Sites autorizados ----------
function sitesCard(sites) {
  const list = h('ul', { class: 'items' });
  const originOf = (s) => (typeof s === 'string' ? s : s.origin);
  const paint = () => clear(list, sites.length ? sites.map((s) => {
    const btn = h('button', { type: 'button', class: 'link danger' }, 'Remover', h('span', { class: 'sr-only' }, ` ${originOf(s)}`));
    btn.addEventListener('click', async () => {
      try {
        await api(`/authorized-sites/${encodeURIComponent(s.id ?? originOf(s))}`, { method: 'DELETE' });
        sites = sites.filter((x) => x !== s);
        paint();
        announce(`${originOf(s)} removido.`);
        form.querySelector('input').focus();
      } catch (err) { toast(err.message, 'err'); }
    });
    return h('li', { class: 'item' }, h('div', { class: 'item-head' }, h('code', {}, originOf(s)), btn));
  }) : h('li', { class: 'empty' }, 'Nenhum site autorizado além do portal de demonstração.'));

  const form = h('form', { novalidate: true, class: 'row', 'aria-label': 'Autorizar site' },
    field({ name: 'origin', label: 'Origem do site', type: 'url', required: true, hint: 'Ex.: https://vagas.exemplo.com', attrs: { placeholder: 'https://' } }),
    h('button', { class: 'primary', type: 'submit' }, 'Autorizar site'));
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    clearErrors(form);
    let { origin } = readForm(form);
    const err = v.origin(origin);
    if (err) return showErrors(form, { origin: [err] });
    origin = new URL(origin).origin;
    try {
      const r = await api('/authorized-sites', { method: 'POST', body: { origin } });
      sites.push(r && typeof r === 'object' ? r : { origin });
      paint();
      form.reset();
      toast('Site autorizado', 'ok');
    } catch (error) { showErrors(form, error); }
  });
  paint();
  return h('section', { class: 'card', 'aria-labelledby': 'sites-title' },
    h('h2', { id: 'sites-title' }, 'Sites autorizados'),
    h('p', { class: 'small muted' }, 'A extensão só lê páginas destes sites (e do portal de demonstração). Sites fora da lista não são suportados oficialmente: a leitura pode errar, e o navegador também pede a sua permissão.'),
    list, form);
}
