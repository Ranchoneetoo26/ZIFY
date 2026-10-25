// Crawler e filler do Zyft, injetado sob demanda pelo painel (chrome.scripting.executeScript).
// Regras (ADR-0009, ADR-0010): nunca envia, salva ou avança; nunca clica em botões ou links;
// nunca lê campos de senha; nunca preenche anexos; não faz requisições de rede.
(() => {
  if (window.__zyftContent) return;
  window.__zyftContent = true;

  const ATTR = 'data-zyft-id';
  const clean = (s) => (s || '').replace(/\s+/g, ' ').replace(/\s*\*\s*$/, '').trim();
  const visible = (el) => {
    const r = el.getBoundingClientRect();
    const st = getComputedStyle(el);
    return r.width > 0 && r.height > 0 && st.visibility !== 'hidden' && st.display !== 'none';
  };
  // Se houver um modal aberto (ex.: "Candidatura simplificada"), lê só o mais recente.
  const root = () => [...document.querySelectorAll('[role="dialog"], [aria-modal="true"], dialog[open]')].filter(visible).pop() || document;

  function textWithout(el) {
    const c = el.cloneNode(true);
    c.querySelectorAll('input, select, textarea, button, option, script, style').forEach((x) => x.remove());
    return clean(c.textContent);
  }

  function labelOf(el) {
    if (el.getAttribute('aria-labelledby')) {
      const t = el.getAttribute('aria-labelledby').split(' ').map((id) => document.getElementById(id)?.textContent).join(' ');
      if (clean(t)) return clean(t);
    }
    if (el.id) {
      const l = document.querySelector(`label[for="${CSS.escape(el.id)}"]`);
      if (l && textWithout(l)) return textWithout(l);
    }
    const wrap = el.closest('label');
    if (wrap && textWithout(wrap)) return textWithout(wrap);
    if (el.getAttribute('aria-label')) return clean(el.getAttribute('aria-label'));
    let prev = el.previousElementSibling;
    while (prev && !clean(prev.textContent)) prev = prev.previousElementSibling;
    if (prev && !prev.matches('input,select,textarea')) return clean(prev.textContent).slice(0, 200);
    const parentText = el.parentElement && textWithout(el.parentElement);
    if (parentText && parentText.length < 150) return parentText;
    return '';
  }

  function sectionOf(el, headings) {
    const fs = el.closest('fieldset');
    const legend = fs?.querySelector(':scope > legend');
    if (legend) return clean(legend.textContent);
    let last = '';
    for (const h of headings) {
      if (h.compareDocumentPosition(el) & Node.DOCUMENT_POSITION_FOLLOWING) last = clean(h.textContent);
      else break;
    }
    return last.slice(0, 120);
  }

  let seq = 0;
  function scanFields() {
    const r = root();
    const headings = [...r.querySelectorAll('h1,h2,h3,h4,h5,legend,[role="heading"]')];
    const fields = [];
    const radioGroups = new Set();
    for (const el of r.querySelectorAll('input, select, textarea')) {
      const type = (el.type || el.tagName).toLowerCase();
      // Senha: descartada antes de qualquer leitura (RNF-01).
      if (type === 'password') continue;
      if (['hidden', 'submit', 'button', 'reset', 'image'].includes(type) || el.disabled) continue;
      if (type !== 'radio' && type !== 'checkbox' && !visible(el)) continue;
      if (!el.getAttribute(ATTR)) el.setAttribute(ATTR, `f${++seq}`);
      const fieldId = el.getAttribute(ATTR);

      if (type === 'radio') {
        const key = el.name || fieldId;
        if (radioGroups.has(key)) continue;
        radioGroups.add(key);
        const group = el.name ? [...r.querySelectorAll(`input[type=radio][name="${CSS.escape(el.name)}"]`)] : [el];
        let box = el.parentElement;
        while (box && !group.every((g) => box.contains(g))) box = box.parentElement;
        let q = '';
        if (box) {
          const c = box.cloneNode(true);
          c.querySelectorAll('label, input').forEach((x) => x.remove());
          q = clean(c.textContent);
        }
        fields.push({
          fieldId, tag: 'input', type: 'radio', name: el.name || '', htmlId: el.id || '',
          label: (q || el.name || '').slice(0, 300), placeholder: '', section: sectionOf(el, headings),
          required: group.some((g) => g.required), maxLength: 0,
          currentValue: group.find((g) => g.checked)?.value || '',
          options: group.map((g) => ({ value: g.value, text: clean(g.closest('label')?.textContent || labelOf(g) || g.value) })),
        });
        continue;
      }

      const f = {
        fieldId,
        tag: el.tagName.toLowerCase(),
        type: el.tagName === 'SELECT' ? 'select' : el.tagName === 'TEXTAREA' ? 'textarea' : type,
        name: el.name || '',
        htmlId: el.id || '',
        label: labelOf(el) || clean(el.placeholder) || clean(el.getAttribute('aria-label')),
        placeholder: el.placeholder || '',
        section: sectionOf(el, headings),
        required: el.required || el.getAttribute('aria-required') === 'true',
        maxLength: el.maxLength > 0 ? el.maxLength : 0,
        currentValue: type === 'checkbox' ? (el.checked ? 'true' : '')
          : type === 'file' ? ''
          : el.tagName === 'SELECT' ? (el.value ? clean(el.selectedOptions[0]?.textContent) : '')
          : el.value,
      };
      if (el.tagName === 'SELECT') f.options = [...el.options].map((o) => ({ value: o.value, text: clean(o.textContent) }));
      fields.push(f);
    }
    return fields;
  }

  function readJob() {
    const main = document.querySelector('main, article, [role="main"]') || document.body;
    return {
      url: location.href,
      title: clean(document.querySelector('h1')?.textContent) || document.title,
      text: main.innerText.slice(0, 15000),
    };
  }

  // Setter nativo + eventos: frameworks (React/Vue) percebem a mudança.
  function setNative(el, value) {
    const proto = el.tagName === 'TEXTAREA' ? HTMLTextAreaElement.prototype : el.tagName === 'SELECT' ? HTMLSelectElement.prototype : HTMLInputElement.prototype;
    Object.getOwnPropertyDescriptor(proto, 'value').set.call(el, value);
    el.dispatchEvent(new Event('input', { bubbles: true }));
    el.dispatchEvent(new Event('change', { bubbles: true }));
    el.dispatchEvent(new Event('blur', { bubbles: true }));
  }

  function mark(el, ok) {
    el.style.outline = ok ? '2px solid #1f8a4c' : '2px solid #c03535';
    el.style.outlineOffset = '1px';
    setTimeout(() => { el.style.outline = ''; }, 4000);
  }

  function fill(items) {
    const report = [];
    for (const { fieldId, value } of items || []) {
      const el = document.querySelector(`[${ATTR}="${CSS.escape(String(fieldId))}"]`);
      if (!el) { report.push({ fieldId, ok: false, error: 'campo não encontrado' }); continue; }
      try {
        if (!el.matches('input, select, textarea')) throw new Error('elemento não é campo de formulário');
        if (['file', 'password', 'submit', 'button', 'image', 'reset', 'hidden'].includes(el.type)) throw new Error('campo não é preenchido automaticamente');
        if (el.type === 'radio') {
          const group = el.name ? [...document.querySelectorAll(`input[type=radio][name="${CSS.escape(el.name)}"]`)] : [el];
          const target = group.find((r) => r.value === String(value));
          if (!target) throw new Error('opção não encontrada');
          if (!target.checked) target.click(); // clique só no próprio input de rádio
          mark(target.closest('label') || target, true);
        } else if (el.type === 'checkbox') {
          const want = value === true || value === 'true';
          if (el.checked !== want) el.click(); // clique só no próprio input de checkbox
          mark(el, true);
        } else if (el.tagName === 'SELECT') {
          const opt = [...el.options].find((o) => o.value === String(value)) || [...el.options].find((o) => clean(o.textContent) === clean(String(value)));
          if (!opt) throw new Error('opção não encontrada');
          setNative(el, opt.value);
          mark(el, true);
        } else {
          el.focus();
          setNative(el, String(value));
          mark(el, true);
        }
        report.push({ fieldId, ok: true });
      } catch (e) {
        mark(el, false);
        report.push({ fieldId, ok: false, error: e.message });
      }
    }
    return report;
  }

  function highlight(fieldId) {
    const el = document.querySelector(`[${ATTR}="${CSS.escape(String(fieldId))}"]`);
    if (!el) return;
    el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    el.style.boxShadow = '0 0 0 4px rgba(49,87,213,.45)';
    setTimeout(() => (el.style.boxShadow = ''), 1600);
  }

  // Só observa o envio feito pela pessoa (RF-19). Não chama preventDefault.
  document.addEventListener('submit', () => {
    chrome.runtime.sendMessage({ type: 'submitted', url: location.href, title: document.title }).catch(() => {});
  }, true);

  chrome.runtime.onMessage.addListener((msg, sender, reply) => {
    if (sender.id !== chrome.runtime.id || !msg || typeof msg.cmd !== 'string') return false;
    if (msg.cmd === 'scan') reply({ fields: scanFields(), url: location.href, title: document.title, host: location.hostname });
    else if (msg.cmd === 'job') reply(readJob());
    else if (msg.cmd === 'fill') reply({ report: fill(msg.items) });
    else if (msg.cmd === 'highlight') { highlight(msg.fieldId); reply({}); }
    else if (msg.cmd === 'ping') reply({ ok: true });
    else return false;
    return false;
  });
})();
