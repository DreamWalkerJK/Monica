/* Local configuration preview. Captured values are never sent to the running host. */
(() => {
  'use strict';

  const sections = ['state', 'history', 'versions', 'debug', 'storage'];
  const titles = { state: 'Configuration State', history: 'Configuration History', versions: 'Configuration Versions', debug: 'Configuration Debug', storage: 'Configuration Storage' };
  const subtitles = {
    state: 'Inspect managed definitions, stage edits, and save them as one audited group.',
    history: 'Review audited changes by mutation or by group.',
    versions: 'Review the current unified configuration version and compare captured history.',
    debug: 'Inspect the runtime configuration provider view.',
    storage: 'Inspect the active configuration store bundle and its runtime state.'
  };
  const state = { section: 'state', search: '', selected: '', tab: 'values', drafts: new Map(), errors: new Map(), saved: false };
  const bound = new WeakSet();
  const storageKey = 'monica:grove-real:configuration:preview-draft';
  let container;
  let dialogBound = false;
  const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const data = () => window.GROVE_DATA || {};
  const config = () => data().configuration || {};
  const definitions = () => Array.isArray(config().definitions) ? config().definitions : [];
  const fields = definition => Array.isArray(definition?.fields) ? definition.fields : [];
  const app = () => window.GroveApp || {};
  const dialog = (title, html) => app().openDialog?.(title, html);
  const toast = message => app().toast?.(message);
  const keyOf = definition => String(definition.key || definition.name || '');
  const fieldKey = (definition, field) => `${keyOf(definition)}::${field.path}`;
  const present = value => value !== null && value !== undefined && value !== '';
  const text = value => present(value) ? esc(value) : '<span class="muted">Not recorded</span>';
  const fieldName = field => {
    const name = String(field.path || field.label || '').split(/[.:]/).filter(Boolean).at(-1) || 'Value';
    const words = name.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/([A-Z])([A-Z][a-z])/g, '$1 $2');
    return words[0].toUpperCase() + words.slice(1).toLowerCase();
  };
  const captureDate = () => {
    const value = config().captureTime || data().capturedAt;
    if (!value) return 'Not recorded';
    const date = new Date(value);
    return Number.isNaN(date.valueOf()) ? 'Not recorded' : date.toLocaleString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit', timeZoneName: 'short' });
  };
  const typeOf = field => String(field.type || (Array.isArray(field.value) ? 'array' : typeof field.value)).toLowerCase();
  const isArray = field => Array.isArray(field.value) || /array|\[\]|list|collection/.test(typeOf(field));
  const isBoolean = field => typeof field.value === 'boolean' || /boolean|bool/.test(typeOf(field));
  const isNumber = field => typeof field.value === 'number' || /^(number|int|long|double|float|decimal)/.test(typeOf(field));
  const rawOriginal = field => isArray(field) || typeof field.value === 'object' ? JSON.stringify(field.value, null, 2) : String(field.value ?? '');
  const rawValue = (definition, field) => state.drafts.has(fieldKey(definition, field)) ? state.drafts.get(fieldKey(definition, field)) : rawOriginal(field);
  const selected = () => definitions().find(definition => keyOf(definition) === state.selected) || definitions().find(definition => keyOf(definition) === 'FlightService.Domain.Configurations.TowerDepArrOptions') || definitions().find(definition => fields(definition).length) || definitions()[0];

  function liveLink(route, label = 'Open running host') {
    const base = config().hostUrl || data().hostUrl || data().sourceUrl || data().liveUrl;
    if (!base) return '<p class="muted">The running host address is not included in this capture.</p>';
    try {
      const address = new URL(route, base);
      if (!['http:', 'https:'].includes(address.protocol)) return '';
      return `<a class="btn" href="${esc(address.href)}" target="_blank" rel="noopener">${esc(label)} ↗</a>`;
    } catch { return '<p class="muted">The running host address is unavailable.</p>'; }
  }

  function header() {
    return `<header class="page-heading"><div><p class="eyebrow">MONICA / MANAGED CONFIGURATION</p><h1>${titles[state.section]}</h1><p class="muted">${subtitles[state.section]}</p></div><div class="page-actions">${state.section === 'state' ? '<button type="button" class="btn" data-config-action="import">Import preview draft</button><button type="button" class="btn" data-config-action="export">Export capture</button>' : ''}<button type="button" class="btn btn-primary" data-config-action="refresh">Refresh</button></div></header><p class="provenance-note">Captured host evidence · ${esc(captureDate())} · edits below are local preview drafts.</p>`;
  }

  function navigatorItems() {
    const query = state.search.trim().toLowerCase();
    const matches = definitions().filter(definition => !query || [definition.label, definition.name, definition.key, definition.project, definition.category].some(value => String(value || '').toLowerCase().includes(query)) || fields(definition).some(field => [field.path, field.label, field.description].some(value => String(value || '').toLowerCase().includes(query))));
    const projects = [...new Set(matches.map(definition => definition.project || 'Project not recorded'))].sort();
    const current = selected();
    return matches.length ? projects.map(project => `<div class="definition-project"><p class="eyebrow">${esc(project)}</p>${matches.filter(definition => (definition.project || 'Project not recorded') === project).map(definition => `<button type="button" class="config-definition ${current === definition ? 'active' : ''}" data-definition="${esc(keyOf(definition))}" aria-current="${current === definition ? 'true' : 'false'}"><span>${esc(definition.label || definition.name || keyOf(definition))}</span><small class="mono">${esc(definition.name || keyOf(definition).split('.').at(-1))}</small>${fields(definition).length ? '<small>Values captured</small>' : '<small>Catalog metadata</small>'}${definitionChanges(definition).length ? `<small class="badge">${definitionChanges(definition).length} staged</small>` : ''}</button>`).join('')}</div>`).join('') : '<div class="empty-state"><strong>No definitions match the current search.</strong><button type="button" class="btn" data-config-action="reset-search">Clear search</button></div>';
  }

  function definitionNavigator() {
    const projects = [...new Set(definitions().map(definition => definition.project || 'Project not recorded'))].sort();
    const current = selected();
    const picker = `<label class="mobile-definition-picker input-wrap"><span>Definition</span><select data-config-definition-picker aria-label="Choose configuration definition">${projects.map(project => `<optgroup label="${esc(project)}">${definitions().filter(definition => (definition.project || 'Project not recorded') === project).map(definition => `<option value="${esc(keyOf(definition))}" title="${esc(definition.label || '')}" ${current === definition ? 'selected' : ''}>${esc(definition.name || keyOf(definition))}</option>`).join('')}</optgroup>`).join('')}</select></label>`;
    return `<aside class="config-definitions" aria-label="Configuration definitions"><header><h2>Definitions <span class="definition-count">${definitions().length}</span></h2><p class="muted">Grouped by publishing project</p></header>${picker}<label class="input-wrap"><input type="search" aria-label="Search definitions or options" placeholder="Search definitions or options" value="${esc(state.search)}" data-config-search></label><div class="definition-items" data-config-items>${navigatorItems()}</div></aside>`;
  }

  function fieldEditor(definition, field, index) {
    const key = fieldKey(definition, field);
    const raw = rawValue(definition, field);
    const inputId = `config-field-${index}`;
    let editor;
    const attributes = `id="${inputId}" data-config-field="${esc(field.path)}" data-config-key="${esc(keyOf(definition))}" aria-describedby="${inputId}-hint${state.errors.has(key) ? ` ${inputId}-error` : ''}"${state.errors.has(key) ? ' aria-invalid="true"' : ''}`;
    if (isBoolean(field)) editor = `<label class="editor-switch"><input type="checkbox" ${attributes} ${raw === 'true' ? 'checked' : ''}><span>${raw === 'true' ? 'Enabled' : 'Disabled'}</span></label>`;
    else if (isArray(field) || typeof field.value === 'object') editor = `<textarea class="editor-input mono" rows="4" ${attributes}>${esc(raw)}</textarea>`;
    else editor = `<input class="editor-input mono" type="${isNumber(field) ? 'number' : 'text'}" ${attributes} value="${esc(raw)}"${isNumber(field) ? ' step="any"' : ''}>`;
    return `<div class="config-value-row"><div><label for="${inputId}">${esc(fieldName(field))}</label><small class="mono">${esc(field.path)}</small>${field.label && field.label !== fieldName(field) ? `<small>${esc(field.label)}</small>` : ''}${field.description ? `<small>${esc(field.description)}</small>` : ''}${field.restart || /restart/i.test(definition.reload || '') ? '<span class="badge warning">Restart required</span>' : ''}${state.drafts.has(key) ? ' <span class="badge">Staged preview</span>' : ''}</div><div>${editor}<p class="editor-hint" id="${inputId}-hint">${isArray(field) ? 'JSON array · local draft only' : `${esc(field.type || typeof field.value)} · captured value`}</p>${state.errors.has(key) ? `<p class="editor-hint" id="${inputId}-error" role="alert">${esc(state.errors.get(key))}</p>` : ''}<div class="page-actions"><button type="button" class="inline-link" data-config-source="${esc(field.path)}">Source chain</button><button type="button" class="inline-link" data-config-history="${esc(field.path)}">History</button>${state.drafts.has(key) ? `<button type="button" class="inline-link" data-config-undo="${esc(field.path)}">Undo</button>` : ''}</div></div></div>`;
  }

  function metadata(definition) {
    const rows = [['Definition key', definition.key], ['CLR type', definition.name], ['Publishing project', definition.project], ['Category', definition.category], ['Schema version', definition.version], ['Definition revision', definition.revision], ['Origin', definition.origin], ['Reload behavior', definition.reload]];
    return `<dl class="definition-list">${rows.map(([label, value]) => `<div><dt>${label}</dt><dd class="mono">${text(value)}</dd></div>`).join('')}</dl>`;
  }

  function jsonValues(definition) {
    const values = {};
    for (const field of fields(definition)) {
      const result = parseField(definition, field);
      values[field.path] = result.error ? rawValue(definition, field) : result.value;
    }
    return JSON.stringify(values, null, 2);
  }

  function detail() {
    const definition = selected();
    if (!definition) return '<section class="config-detail panel"><div class="empty-state"><strong>No configuration definitions were found.</strong></div></section>';
    const valueFields = fields(definition);
    const key = keyOf(definition);
    const route = definition.key ? `/configuration/state?definitionKey=${encodeURIComponent(definition.key)}` : '/configuration/state';
    const body = state.tab === 'metadata' ? metadata(definition) : state.tab === 'json' ? valueFields.length ? `<div class="panel-body"><p class="muted">Current local preview values by captured logical path.</p><pre class="editor-input mono">${esc(jsonValues(definition))}</pre><button type="button" class="btn" data-config-action="copy-json">Copy JSON</button></div>` : '<div class="empty-state"><strong>Value JSON was not included in the capture.</strong><p class="muted">Catalog metadata remains available.</p></div>' : valueFields.length ? valueFields.map((field, index) => fieldEditor(definition, field, index)).join('') : `<div class="empty-state"><strong>Values were not included in the capture.</strong><p class="muted">This definition is present in the actual host catalog. Its values were not copied into this prototype.</p>${liveLink(route)}</div>`;
    return `<div class="config-detail"><section class="panel"><header class="config-detail-heading"><div><p class="eyebrow">SELECTED DEFINITION</p><h2>${esc(definition.label || definition.name)}</h2><p class="mono">${esc(definition.name || key)}</p><p class="mono">Definition key · ${text(definition.key)}</p></div><div class="page-actions"><span class="badge">Schema ${present(definition.version) ? `v${esc(definition.version)}` : 'not recorded'} · ${present(definition.revision) ? `r${esc(definition.revision)}` : 'revision not recorded'}</span><span class="badge warning">${text(definition.reload)}</span></div></header><nav class="config-section-nav" aria-label="Definition views">${['values', 'json', 'metadata'].map(tab => `<button type="button" class="${state.tab === tab ? 'active' : ''}" data-config-tab="${tab}" aria-pressed="${state.tab === tab}">${tab === 'json' ? 'JSON' : tab[0].toUpperCase() + tab.slice(1)}</button>`).join('')}</nav>${body}</section><p class="config-notice">Runtime validation, effective source chains, and reload delivery were not included in this capture. Local type checks cannot establish runtime configuration validity.</p>${draftFooter()}</div>`;
  }

  function draftFooter() {
    const count = changes().length;
    return `<footer class="draft-footer"><div><strong data-config-count>${count} staged ${count === 1 ? 'change' : 'changes'}${state.saved ? ' · preview draft saved' : ''}</strong><small>Local preview · host values unchanged</small></div><div class="page-actions"><button type="button" class="btn" data-config-action="undo-all" ${count ? '' : 'disabled'}>Undo all</button><button type="button" class="btn" data-config-action="validate" ${count ? '' : 'disabled'}>Validate draft</button><button type="button" class="btn btn-primary" data-config-action="review" ${count ? '' : 'disabled'}>Review draft</button></div></footer>`;
  }

  function parseField(definition, field) {
    const raw = rawValue(definition, field);
    try {
      if (isArray(field)) {
        const value = JSON.parse(raw);
        if (!Array.isArray(value)) return { error: 'Enter a valid JSON array.' };
        if (Array.isArray(field.value) && field.value.length && value.some(item => typeof item !== typeof field.value[0])) return { error: `Every array item must be a ${typeof field.value[0]}.` };
        return { value };
      }
      if (isBoolean(field)) return { value: raw === 'true' };
      if (isNumber(field)) {
        if (!raw.trim() || !Number.isFinite(Number(raw))) return { error: 'Enter a finite number.' };
        if (/^(int|long)/.test(typeOf(field)) && !Number.isInteger(Number(raw))) return { error: 'Enter a whole number.' };
        return { value: Number(raw) };
      }
      if (/timespan/.test(typeOf(field)) && !/^-?(?:\d+\.)?(?:[01]?\d|2[0-3]):[0-5]\d(?::[0-5]\d(?:\.\d{1,7})?)?$/.test(raw.trim())) {
        return { error: 'Use a duration such as 02:00 or 2.00:00:00.' };
      }
      if (typeof field.value === 'object' && field.value !== null) return { value: JSON.parse(raw) };
      return { value: raw };
    } catch { return { error: 'Enter valid JSON.' }; }
  }

  function changes() {
    return definitions().flatMap(definition => fields(definition).filter(field => state.drafts.has(fieldKey(definition, field))).map(field => ({ definition, field, ...parseField(definition, field) }))).filter(change => change.error || JSON.stringify(change.value) !== JSON.stringify(change.field.value));
  }

  function definitionChanges(definition) { return changes().filter(change => change.definition === definition); }

  function validate() {
    state.errors.clear();
    for (const change of changes()) if (change.error) state.errors.set(fieldKey(change.definition, change.field), change.error);
    return state.errors.size === 0;
  }

  function review() {
    const valid = validate();
    const staged = changes();
    updateDetail();
    dialog('Review preview draft', `<p class="eyebrow">LOCAL PREVIEW / NO HOST WRITES</p><p>${staged.length} staged changes across ${new Set(staged.map(change => keyOf(change.definition))).size} definitions.</p><div class="table-wrap"><table class="data-table"><thead><tr><th>Definition / path</th><th>Captured</th><th>Draft</th></tr></thead><tbody>${staged.map(change => `<tr><td><strong>${esc(change.definition.name || change.definition.label)}</strong><small class="mono muted">${esc(change.field.path)}</small></td><td class="mono">${esc(JSON.stringify(change.field.value))}</td><td class="mono">${change.error ? `<span role="alert">${esc(change.error)}</span>` : esc(JSON.stringify(change.value))}</td></tr>`).join('')}</tbody></table></div><p class="muted">Saving stores this preview draft in this browser. The running host and its configuration store receive no request.</p><button type="button" class="btn btn-primary" data-config-action="save-preview" ${valid && staged.length ? '' : 'disabled'}>Save preview draft</button>`);
  }

  function refresh() {
    dialog('Configuration capture', `<p>The definition catalog and selected values belong to a saved host observation.</p><dl class="definition-list"><div><dt>Captured</dt><dd>${text(config().captureTime || data().capturedAt)}</dd></div><div><dt>Definitions</dt><dd>${definitions().length}</dd></div></dl><p class="muted">Use the running host to inspect current runtime values. Local draft edits stay within this prototype.</p>${liveLink('/configuration/state')}`);
  }

  function uncaptured(section) {
    const route = `/configuration/${section}`;
    const heading = section === 'history' ? 'Audit history was not included in the capture.' : section === 'versions' ? 'Unified version snapshots were not included in the capture.' : section === 'debug' ? 'Runtime debug values were not included in the capture.' : 'Store diagnostics were not included in the capture.';
    const structure = section === 'history' ? `<nav class="tab-strip" aria-label="History views"><button type="button" class="active" data-config-history-tab="mutations">By mutation</button><button type="button" data-config-history-tab="groups">By group</button></nav><div class="table-wrap"><table class="data-table"><thead data-config-history-heading><tr><th>Time</th><th>Definition</th><th>Path</th><th>Target</th><th>Kind</th><th>Group label</th></tr></thead></table></div>` : section === 'versions' ? '<div class="panel-heading"><h2>Current Version</h2></div><div class="panel-body"><p class="muted">Current unified version: Not recorded</p></div><div class="panel-heading"><h2>History Versions</h2></div>' : section === 'storage' ? '<div class="panel-heading"><h2>Runtime configuration sources</h2></div><div class="panel-body"><dl class="definition-list"><div><dt>Effective values</dt><dd>Not recorded</dd></div><div><dt>History store</dt><dd>Not recorded</dd></div><div><dt>Metadata store</dt><dd>Not recorded</dd></div><div><dt>Change notifications</dt><dd>Not recorded</dd></div></dl></div>' : '<div class="panel-heading"><h2>Provider debug view</h2></div>';
    return `<section class="panel">${structure}<div class="empty-state"><strong>${heading}</strong><p class="muted">This is a capture boundary, not evidence that the host has no ${section === 'history' ? 'audit history' : section === 'versions' ? 'versions' : section === 'storage' ? 'registered stores' : 'runtime configuration'}.</p>${liveLink(route)}</div></section>`;
  }

  function updateDetail() {
    if (!container || state.section !== 'state') return;
    const detailElement = container.querySelector('.config-detail');
    if (detailElement) detailElement.outerHTML = detail();
    const items = container.querySelector('[data-config-items]');
    if (items) items.innerHTML = navigatorItems();
    const picker = container.querySelector('[data-config-definition-picker]');
    if (picker && selected()) picker.value = keyOf(selected());
  }

  function updateDraftFooter() {
    const footer = container?.querySelector('.draft-footer');
    if (footer) footer.outerHTML = draftFooter();
    const items = container?.querySelector('[data-config-items]');
    if (items) items.innerHTML = navigatorItems();
  }

  function stage(input) {
    const definition = definitions().find(item => keyOf(item) === input.dataset.configKey);
    const field = fields(definition).find(item => String(item.path) === input.dataset.configField);
    if (!field) return;
    const key = fieldKey(definition, field);
    const raw = isBoolean(field) ? String(input.checked) : input.value;
    if (isBoolean(field)) {
      const switchLabel = input.closest('.editor-switch')?.querySelector('span');
      if (switchLabel) switchLabel.textContent = input.checked ? 'Enabled' : 'Disabled';
    }
    if (raw === rawOriginal(field)) state.drafts.delete(key);
    else state.drafts.set(key, raw);
    state.errors.delete(key);
    state.saved = false;
    input.removeAttribute('aria-invalid');
    updateDraftFooter();
  }

  function download(payload, name) {
    const url = URL.createObjectURL(new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' }));
    const anchor = document.createElement('a'); anchor.href = url; anchor.download = name; anchor.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  function action(name) {
    if (name === 'refresh') return refresh();
    if (name === 'reset-search') { state.search = ''; container.querySelector('[data-config-search]').value = ''; container.querySelector('[data-config-items]').innerHTML = navigatorItems(); return; }
    if (name === 'undo-all') {
      state.drafts.clear(); state.errors.clear(); state.saved = false;
      let cleared = true;
      try { localStorage.removeItem(storageKey); } catch { cleared = false; }
      updateDetail();
      toast(cleared ? 'All local staged changes and the saved preview draft were cleared.' : 'Local edits cleared. The saved browser draft could not be removed.');
      return;
    }
    if (name === 'validate') { const valid = validate(); updateDetail(); toast(valid ? 'Preview types are valid. Runtime rules were not captured.' : `${state.errors.size} draft values need attention.`); return; }
    if (name === 'review') return review();
    if (name === 'save-preview') {
      if (!validate()) return review();
      try {
        const saved = { schema: 'grove-configuration-preview-draft-v1', captureTime: config().captureTime || data().capturedAt, changes: changes().map(change => ({ key: keyOf(change.definition), path: change.field.path, value: change.value })) };
        localStorage.setItem(storageKey, JSON.stringify(saved));
        state.saved = true;
        updateDraftFooter();
        dialog('Preview draft saved', '<p>The local preview draft is saved in this browser.</p><p class="muted">Host values unchanged. No configuration mutation, runtime reload, or restart was requested.</p>');
        toast('Preview draft saved locally.');
      } catch { toast('Browser storage is unavailable. The staged draft remains in this page.'); }
      return;
    }
    if (name === 'export') {
      download({ schema: 'grove-configuration-capture-v1', captureTime: config().captureTime || data().capturedAt, definitions: definitions() }, 'grove-configuration-capture.json');
      toast('Captured configuration catalog exported.'); return;
    }
    if (name === 'import') return dialog('Import preview draft', '<p class="muted">Import a Grove capture or saved preview draft. Only values for definitions and paths already captured here can be staged.</p><label class="input-wrap"><span>Local JSON file · maximum 10 MB</span><input type="file" accept="application/json,.json" data-config-import></label><p class="muted">Unknown definitions and uncaptured paths are skipped. Imported values remain local.</p><div data-config-import-error role="alert"></div>');
    if (name === 'copy-json') {
      const definition = selected();
      if (definition && window.navigator.clipboard) window.navigator.clipboard.writeText(jsonValues(definition)).then(() => toast('Local preview JSON copied.'), () => toast('Clipboard access is unavailable.'));
      else toast('Clipboard access is unavailable.');
    }
  }

  function click(event) {
    const definitionButton = event.target.closest('[data-definition]');
    if (definitionButton) { state.selected = definitionButton.dataset.definition; state.tab = 'values'; updateDetail(); return; }
    const tab = event.target.closest('[data-config-tab]');
    if (tab) { state.tab = tab.dataset.configTab; updateDetail(); return; }
    const undo = event.target.closest('[data-config-undo]');
    if (undo && selected()) { state.drafts.delete(`${keyOf(selected())}::${undo.dataset.configUndo}`); state.errors.clear(); state.saved = false; updateDetail(); return; }
    const source = event.target.closest('[data-config-source]');
    if (source) return dialog('Configuration source chain', `<p class="mono">${esc(source.dataset.configSource)}</p><p>Effective source-chain evidence was not included in the capture.</p><p class="muted">The prototype cannot identify which provider currently supplies this value.</p>${liveLink(`/configuration/state?definitionKey=${encodeURIComponent(keyOf(selected()))}&path=${encodeURIComponent(source.dataset.configSource)}`)}`);
    const history = event.target.closest('[data-config-history]');
    if (history) return dialog('Configuration value history', `<p class="mono">${esc(history.dataset.configHistory)}</p><p>Audit records for this value were not included in the capture.</p>${liveLink(`/configuration/history?definitionKey=${encodeURIComponent(keyOf(selected()))}&path=${encodeURIComponent(history.dataset.configHistory)}`)}`);
    const historyTab = event.target.closest('[data-config-history-tab]');
    if (historyTab) {
      container.querySelectorAll('[data-config-history-tab]').forEach(button => { button.classList.toggle('active', button === historyTab); button.setAttribute('aria-pressed', String(button === historyTab)); });
      container.querySelector('[data-config-history-heading]').innerHTML = historyTab.dataset.configHistoryTab === 'groups' ? '<tr><th>Group label</th><th>Time</th><th>Mutations</th><th>Definitions</th><th>Status</th></tr>' : '<tr><th>Time</th><th>Definition</th><th>Path</th><th>Target</th><th>Kind</th><th>Group label</th></tr>';
      return;
    }
    const button = event.target.closest('[data-config-action]');
    if (button) action(button.dataset.configAction);
  }

  function applyImportedChanges(entries) {
    let staged = 0, skipped = 0;
    for (const entry of entries) {
      const definition = definitions().find(item => keyOf(item) === entry.key);
      const field = fields(definition).find(item => String(item.path) === String(entry.path));
      if (!field || !Object.prototype.hasOwnProperty.call(entry, 'value')) { skipped += 1; continue; }
      const raw = isArray(field) || typeof field.value === 'object' ? JSON.stringify(entry.value, null, 2) : String(entry.value ?? '');
      if (raw === rawOriginal(field)) state.drafts.delete(fieldKey(definition, field));
      else { state.drafts.set(fieldKey(definition, field), raw); staged += 1; }
    }
    state.saved = false;
    return { staged, skipped };
  }

  async function importDraft(input) {
    const file = input.files?.[0];
    if (!file) return;
    try {
      if (file.size > 10 * 1024 * 1024) throw new Error('The selected file exceeds the 10 MB import limit.');
      const document = JSON.parse(await file.text());
      const importedDefinitions = document.definitions || document.configuration?.definitions;
      const entries = Array.isArray(document.changes) ? document.changes : Array.isArray(importedDefinitions) ? importedDefinitions.flatMap(definition => fields(definition).map(field => ({ key: keyOf(definition), path: field.path, value: field.value }))) : null;
      if (!entries) throw new Error('This file does not contain a Grove configuration capture or preview draft.');
      const outcome = applyImportedChanges(entries);
      validate(); updateDetail();
      dialog('Preview draft imported', `<p>${outcome.staged} values staged · ${outcome.skipped} unknown or uncaptured paths skipped.</p><p class="muted">${state.errors.size ? `${state.errors.size} imported values need type corrections.` : 'Local value types checked.'} Host values unchanged.</p>`);
    } catch (error) {
      const node = input.closest('dialog')?.querySelector('[data-config-import-error]');
      if (node) node.textContent = error.message;
      else toast(error.message);
    }
  }

  function restoreSavedDraft() {
    try {
      const saved = JSON.parse(localStorage.getItem(storageKey) || 'null');
      if (saved?.schema === 'grove-configuration-preview-draft-v1' && Array.isArray(saved.changes)) { applyImportedChanges(saved.changes); state.saved = true; }
    } catch { /* Storage is optional for this local prototype. */ }
  }

  function render(section) {
    state.section = sections.includes(String(section).toLowerCase()) ? String(section).toLowerCase() : 'state';
    if (!state.selected) { const definition = selected(); if (definition) { state.selected = keyOf(definition); restoreSavedDraft(); } }
    return header() + (state.section === 'state' ? `<div class="config-layout">${definitionNavigator()}${detail()}</div>` : uncaptured(state.section));
  }

  function bind(element) {
    container = element;
    if (!bound.has(element)) {
      bound.add(element);
      element.addEventListener('click', click);
      element.addEventListener('input', event => {
        if (event.target.matches('[data-config-search]')) { state.search = event.target.value; element.querySelector('[data-config-items]').innerHTML = navigatorItems(); }
        if (event.target.matches('[data-config-field]')) stage(event.target);
      });
      element.addEventListener('change', event => {
        if (event.target.matches('[data-config-field][type="checkbox"]')) stage(event.target);
        if (event.target.matches('[data-config-definition-picker]')) { state.selected = event.target.value; state.tab = 'values'; updateDetail(); }
      });
    }
    if (!dialogBound) {
      dialogBound = true;
      document.addEventListener('click', event => { if (!container?.contains(event.target) && event.target.closest('dialog')) click(event); });
      document.addEventListener('change', event => { if (event.target.matches('[data-config-import]')) importDraft(event.target); });
    }
  }

  window.GroveConfiguration = { render, bind };
})();
