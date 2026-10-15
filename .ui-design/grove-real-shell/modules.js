/* Grove module workbench: presentation of the captured host's diagnostics only. */
(() => {
  'use strict';

  const sections = ['overview', 'performance', 'modules', 'dependencies', 'discovery'];
  const labels = { overview: 'Overview', performance: 'Performance', modules: 'Modules', dependencies: 'Dependencies', discovery: 'Discovery' };
  const state = { section: 'overview', search: '', filter: 'all', assembly: '', page: 1, pageSize: 25, focus: '', graphScope: 'neighborhood', baseline: null };
  const bound = new WeakSet();
  let currentContainer;
  let dialogEventsBound = false;
  const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const data = () => window.GROVE_DATA || {};
  const modules = () => Array.isArray(data().modules) ? data().modules : [];
  const meta = () => data().moduleMeta || {};
  const recorded = value => value !== null && value !== undefined && value !== '';
  const number = value => recorded(value) && Number.isFinite(Number(value)) ? Number(value) : null;
  const ms = value => number(value) === null ? 'Not recorded' : `${Number(value).toLocaleString('en-US', { maximumFractionDigits: 2 })} ms`;
  const text = value => recorded(value) ? esc(value) : '<span class="muted">Not recorded</span>';
  const array = value => Array.isArray(value) ? value : [];
  const idOf = item => String(item?.id || item?.name || '');
  const titleOf = item => item?.name || item?.typeName || item?.id || 'Unnamed module';
  const resolve = value => {
    const key = typeof value === 'object' ? value.id || value.moduleId || value.name || value.moduleKey?.id : value;
    return modules().find(item => [item.id, item.name, item.fullTypeName].includes(key));
  };
  const relationName = value => titleOf(resolve(value)) === 'Unnamed module' ? String(typeof value === 'object' ? value.name || value.id || value.moduleId || 'Not recorded' : value) : titleOf(resolve(value));
  const app = () => window.GroveApp || {};
  const dialog = (title, html) => app().openDialog?.(title, html);
  const toast = message => app().toast?.(message);
  const moduleState = item => item.state || (typeof item.isActive === 'boolean' ? item.isActive ? 'Active' : 'Disabled' : 'Not recorded');
  const isActive = item => String(moduleState(item)).toLowerCase() === 'active';
  const link = (item, label) => `<button type="button" class="inline-link" data-module-detail="${esc(idOf(item))}">${esc(label || titleOf(item))}</button>`;
  const relationList = values => array(values).length ? `<div class="detail-links">${array(values).map(value => { const item = resolve(value); return item ? link(item) : `<span class="mono">${esc(relationName(value))}</span>`; }).join('')}</div>` : '<p class="muted">None in this snapshot.</p>';
  const panel = (title, body, note = '') => `<section class="panel"><div class="panel-heading"><div><h2>${esc(title)}</h2>${note ? `<p class="muted">${esc(note)}</p>` : ''}</div></div>${body}</section>`;
  const metric = (label, value, note = '') => `<article class="metric"><span class="metric-title">${esc(label)}</span><strong class="metric-value mono">${value}</strong>${note ? `<span class="metric-foot muted">${note}</span>` : ''}</article>`;
  const empty = (heading, body) => `<div class="empty-state"><strong>${esc(heading)}</strong>${body ? `<p class="muted">${esc(body)}</p>` : ''}</div>`;
  const findModule = key => resolve(key);

  function header() {
    const m = meta();
    return `<header class="page-heading"><div><p class="eyebrow">MONICA / COMPOSITION FLIGHT RECORDER</p><h1>Module workbench</h1><p class="muted">Explain startup health, timing causality, discovery, and topology from one immutable revision.</p><div class="module-identity"><span class="badge">${text(m.outcome)}</span><span class="muted">Revision ${recorded(m.revision) ? `#${esc(m.revision)}` : 'not recorded'}</span><span class="mono muted" title="${esc(m.compositionId || '')}">${text(m.compositionId)}</span></div></div><div class="page-actions"><button type="button" class="btn" data-workbench-action="compare">Compare</button><button type="button" class="btn" data-workbench-action="export">Export</button><button type="button" class="btn btn-primary" data-workbench-action="refresh">Refresh</button><button type="button" class="btn" data-workbench-action="help" aria-label="Open diagnostics help">Help</button></div></header>${state.baseline ? `<div class="panel module-baseline"><p>Baseline active · ${esc(state.baseline.label)} · total delta ${baselineDelta()}</p><button type="button" class="btn" data-workbench-action="remove-baseline">Remove baseline</button></div>` : ''}<nav class="tab-strip" aria-label="Module workbench sections">${sections.map(section => `<a href="#/module-system-dashboard/${section}" ${state.section === section ? 'class="active" aria-current="page"' : ''}>${labels[section]}</a>`).join('')}</nav>`;
  }

  function baselineDelta() {
    const current = number(meta().totalMs);
    const baseline = number(state.baseline?.totalMs);
    if (current === null || baseline === null) return 'Not recorded';
    const delta = current - baseline;
    return `${delta > 0 ? '+' : ''}${ms(delta)}`;
  }

  function overview() {
    const m = meta();
    const all = modules();
    const findings = array(m.findings);
    const critical = array(m.criticalPath);
    const edgeCount = all.reduce((sum, item) => sum + array(item.requires).length, 0);
    const top = [...all].filter(item => number(item.costMs) !== null).sort((a, b) => b.costMs - a.costMs).slice(0, 5);
    const startup = m.applicationStartupMs ?? m.applicationStartupDurationMs;
    const stats = m.discovery?.statistics || m.discovery || {};
    return `<div class="metric-strip module-kpis">${metric(recorded(startup) ? 'Application startup' : 'Module composition', ms(recorded(startup) ? startup : m.totalMs), recorded(startup) ? `Module composition ${ms(m.totalMs)}` : 'Recorded composition interval')}${metric('Active modules', all.length ? all.filter(isActive).length : 'Not recorded', `${all.length} declared · ${edgeCount} direct edges`)}${metric('Service registration', ms(m.serviceRegistrationMs ?? m.serviceRegistrationDurationMs), 'Final composition evidence')}${metric('Type discovery', ms(m.typeDiscoveryMs ?? m.typeDiscoveryDurationMs), recorded(stats.distinctQueryCount) ? `${esc(stats.distinctQueryCount)} distinct queries` : 'Duration not available in captured evidence')}${metric('Findings', recorded(m.findings) ? findings.length : 'Not recorded', recorded(m.findings) ? `${findings.filter(item => String(item.severity).toLowerCase() === 'error').length} errors · ${findings.filter(item => String(item.severity).toLowerCase() === 'warning').length} warnings` : 'Structured evidence')}</div>${panel('Critical-path ribbon', critical.length ? `<ol class="evidence-list">${critical.map(item => `<li><strong>${text(item.name || item.phase || item.kind)}</strong><span class="mono">${ms(item.durationMs ?? item.duration)}</span>${item.moduleId && resolve(item.moduleId) ? link(resolve(item.moduleId)) : ''}</li>`).join('')}</ol>` : empty(recorded(m.criticalPath) ? 'No causal blocking path was recorded.' : 'Critical path not recorded', 'The prototype presents only captured intervals.'), 'Causal startup chain')}<div class="content-grid">${panel('Findings', findings.length ? `<ul class="evidence-list">${findings.map(finding => `<li><span class="badge">${text(finding.severity)}</span><div><strong>${text(finding.message || finding.description || finding.code)}</strong>${finding.code ? `<small class="mono muted">${esc(finding.code)}</small>` : ''}</div></li>`).join('')}</ul>` : empty(recorded(m.findings) ? 'No structural or configured-budget findings.' : 'Findings not recorded'))}${panel('Module hotspots', top.length ? `<div class="evidence-list">${top.map(item => `<div>${link(item)}<span class="mono">${ms(item.costMs)}</span><small class="muted">${text(item.assembly)}</small></div>`).join('')}</div>` : empty('Recorded module costs unavailable'), 'Aggregate serial callbacks and startup work')}</div>`;
  }

  function filteredModules() {
    const query = state.search.trim().toLowerCase();
    return modules().filter(item => (!query || [item.name, item.fullTypeName, item.assembly].some(value => String(value || '').toLowerCase().includes(query))) && (state.filter === 'all' || String(moduleState(item)).toLowerCase() === state.filter) && (!state.assembly || item.assembly === state.assembly)).sort((a, b) => (number(b.costMs) ?? -1) - (number(a.costMs) ?? -1));
  }

  function catalogRows() {
    const filtered = filteredModules();
    const pages = Math.max(1, Math.ceil(filtered.length / state.pageSize));
    state.page = Math.min(state.page, pages);
    const maximum = Math.max(0, ...modules().map(item => number(item.costMs) || 0));
    const rows = filtered.slice((state.page - 1) * state.pageSize, state.page * state.pageSize);
    return rows.length ? rows.map(item => `<tr><td><div class="module-identity">${link(item)}<small class="mono muted">${text(item.fullTypeName)}</small><span>${item.isUI ? '<span class="badge">UI</span> ' : ''}${item.isWeb ? '<span class="badge">Web</span>' : ''}</span></div></td><td><span class="badge ${isActive(item) ? 'success' : 'neutral'}">${esc(moduleState(item))}</span></td><td>${text(item.assembly)}</td><td class="mono">${text(item.depth)}</td><td class="module-relations">${array(item.requires).length} requires<br><span class="muted">${array(item.dependents).length} dependents</span></td><td><div class="module-cost"><span class="mono">${ms(item.costMs)}</span>${number(item.costMs) !== null ? `<div class="cost-track" aria-hidden="true"><span class="cost-fill" style="width:${maximum ? Math.max(0, Number(item.costMs)) / maximum * 100 : 0}%"></span></div>` : ''}</div></td><td><button type="button" class="btn" data-module-detail="${esc(idOf(item))}" aria-label="Inspect module ${esc(titleOf(item))}">Inspect</button></td></tr>`).join('') : `<tr><td colspan="7">${empty('No module matches the current filters.', 'Reset the filters or try a different module, namespace, or assembly.')}</td></tr>`;
  }

  function catalog() {
    const all = modules();
    const assemblies = [...new Set(all.map(item => item.assembly).filter(Boolean))].sort();
    return `<div class="metric-strip module-summary">${metric('Active', all.filter(isActive).length)}${metric('UI modules', recorded(meta().uiCount) ? meta().uiCount : all.filter(item => item.isUI).length)}${metric('Web modules', recorded(meta().webCount) ? meta().webCount : all.filter(item => item.isWeb).length)}${metric('Maximum depth', recorded(meta().maxDepth) ? meta().maxDepth : all.length ? Math.max(...all.map(item => number(item.depth) || 0)) : 'Not recorded')}</div>${panel('Module catalog', `<div class="filter-bar"><label class="input-wrap"><span>Search modules</span><input data-module-filter="search" type="search" value="${esc(state.search)}" placeholder="Type, namespace, or assembly"></label><label class="input-wrap"><span>State</span><select data-module-filter="state"><option value="all">All states</option><option value="active" ${state.filter === 'active' ? 'selected' : ''}>Active</option><option value="disabled" ${state.filter === 'disabled' ? 'selected' : ''}>Disabled</option></select></label><label class="input-wrap"><span>Assembly</span><select data-module-filter="assembly"><option value="">All assemblies</option>${assemblies.map(value => `<option ${state.assembly === value ? 'selected' : ''}>${esc(value)}</option>`).join('')}</select></label><button type="button" class="btn" data-workbench-action="reset">Reset</button></div><div class="table-wrap"><table class="data-table"><thead><tr><th scope="col">Module</th><th scope="col">State</th><th scope="col">Assembly</th><th scope="col">Depth</th><th scope="col">Direct relationships</th><th scope="col">Recorded cost</th><th scope="col">Actions</th></tr></thead><tbody data-module-rows>${catalogRows()}</tbody></table></div><div class="table-footer"><span class="muted" data-module-result>${resultText()}</span><div class="page-actions"><button type="button" class="btn" data-workbench-action="previous-page" ${state.page === 1 ? 'disabled' : ''}>Previous</button><span data-module-page>${pageText()}</span><button type="button" class="btn" data-workbench-action="next-page" ${state.page >= Math.ceil(filteredModules().length / state.pageSize) ? 'disabled' : ''}>Next</button><label>Rows <select data-module-filter="page-size">${[25, 50, 100].map(size => `<option ${state.pageSize === size ? 'selected' : ''}>${size}</option>`).join('')}</select></label></div></div>`, 'Declared module inventory')}`;
  }

  function resultText() { return `${filteredModules().length} of ${modules().length} modules`; }
  function pageText() { return `${state.page} / ${Math.max(1, Math.ceil(filteredModules().length / state.pageSize))}`; }
  function updateCatalog() {
    if (!currentContainer) return;
    const rows = currentContainer.querySelector('[data-module-rows]');
    if (!rows) return;
    rows.innerHTML = catalogRows();
    currentContainer.querySelector('[data-module-result]').textContent = resultText();
    currentContainer.querySelector('[data-module-page]').textContent = pageText();
    currentContainer.querySelector('[data-workbench-action="previous-page"]').disabled = state.page === 1;
    currentContainer.querySelector('[data-workbench-action="next-page"]').disabled = state.page >= Math.ceil(filteredModules().length / state.pageSize);
  }

  function phaseRows(item) {
    return array(item.phases).map(phase => typeof phase === 'object' ? { name: phase.name || phase.phase || phase.kind || 'Recorded phase', duration: phase.durationMs ?? phase.duration ?? phase.costMs, start: phase.startedAtMs ?? phase.startMs } : { name: phase, duration: null });
  }

  function performance() {
    const all = modules();
    const rows = all.flatMap(item => phaseRows(item).map(phase => ({ ...phase, module: item })));
    const contributors = [...all].filter(item => number(item.costMs) !== null).sort((a, b) => b.costMs - a.costMs);
    return `<div class="metric-strip">${metric('Module composition', ms(meta().totalMs), 'Captured host composition')}${metric('Service registration', ms(meta().serviceRegistrationMs ?? meta().serviceRegistrationDurationMs))}${metric('Type discovery', ms(meta().typeDiscoveryMs ?? meta().typeDiscoveryDurationMs))}${metric('Recorded module costs', contributors.length, `${all.length} declared modules`)}</div>${panel('Recorded composition intervals', rows.length ? `<div class="table-wrap"><table class="data-table"><thead><tr><th>Module</th><th>Phase</th><th>Start</th><th>Duration</th></tr></thead><tbody>${rows.map(row => `<tr><td>${link(row.module)}</td><td>${esc(row.name)}</td><td class="mono">${ms(row.start)}</td><td class="mono">${ms(row.duration)}</td></tr>`).join('')}</tbody></table></div>` : empty('Composition intervals not recorded', 'Individual timeline spans are unavailable in this capture. Aggregate costs remain available below.'), 'Exact phase evidence, when included in the host capture')}${panel('Top contributors', contributors.length ? `<div class="table-wrap"><table class="data-table"><thead><tr><th>Module</th><th>Assembly</th><th>Recorded cost</th></tr></thead><tbody>${contributors.map(item => `<tr><td>${link(item)}</td><td>${text(item.assembly)}</td><td class="mono">${ms(item.costMs)}</td></tr>`).join('')}</tbody></table></div>` : empty('Module cost evidence not recorded'), 'Aggregate callbacks and startup work; no timing values are simulated')}`;
  }

  function edges() {
    return modules().flatMap(source => array(source.requires).map(value => ({ source, target: resolve(value), targetName: relationName(value) })));
  }

  function selectedFocus() {
    return findModule(state.focus) || modules().find(item => item.name === 'ModuleShellUI') || modules()[0];
  }

  function graphMarkup() {
    const all = modules();
    const focus = selectedFocus();
    if (!focus) return empty('No module topology was captured.');
    const direct = edges();
    const neighbors = new Set([idOf(focus), ...direct.filter(edge => edge.source === focus || edge.target === focus).flatMap(edge => [idOf(edge.source), idOf(edge.target)])]);
    const visible = state.graphScope === 'host' ? all : all.filter(item => neighbors.has(idOf(item)));
    const positions = new Map();
    const columns = new Map();
    for (const item of visible) {
      const column = state.graphScope === 'host' ? number(item.depth) || 0 : item === focus ? 1 : direct.some(edge => edge.source === focus && edge.target === item) ? 2 : 0;
      if (!columns.has(column)) columns.set(column, []);
      columns.get(column).push(item);
    }
    const columnNumbers = [...columns.keys()].sort((a, b) => a - b);
    const graphWidth = Math.max(660, columnNumbers.length * 250 + 60);
    const graphHeight = Math.max(240, ...[...columns.values()].map(items => items.length * 74 + 70));
    columnNumbers.forEach((column, index) => columns.get(column).forEach((item, row) => positions.set(idOf(item), { x: 28 + index * 250, y: 46 + row * 74 })));
    const visibleEdges = direct.filter(edge => positions.has(idOf(edge.source)) && edge.target && positions.has(idOf(edge.target)));
    const wires = visibleEdges.map(edge => {
      const source = positions.get(idOf(edge.source));
      const target = positions.get(idOf(edge.target));
      const forward = target.x >= source.x;
      const x1 = source.x + (forward ? 208 : 0), y1 = source.y + 22;
      const x2 = target.x + (forward ? 0 : 208), y2 = target.y + 22;
      const bend = (x1 + x2) / 2;
      return `<path class="graph-edge" d="M ${x1} ${y1} C ${bend} ${y1}, ${bend} ${y2}, ${x2} ${y2}" fill="none" stroke="var(--border)" stroke-width="1.5" marker-end="url(#grove-edge-arrow)"/>`;
    }).join('');
    const nodes = visible.map(item => {
      const position = positions.get(idOf(item));
      const label = titleOf(item);
      return `<g class="graph-node ${item === focus ? 'is-selected' : ''}" transform="translate(${position.x},${position.y})"><rect width="208" height="46" rx="8" fill="${item === focus ? 'var(--primary-soft)' : 'var(--surface)'}" stroke="${item === focus ? 'var(--primary)' : 'var(--border)'}"/><text x="12" y="20" fill="var(--text)" font-size="11">${esc(label.length > 25 ? label.slice(0, 23) + '…' : label)}</text><text x="12" y="35" fill="var(--muted)" font-size="9">Depth ${esc(item.depth ?? '—')} · ${esc(moduleState(item))}</text><title>${esc(label)} · ${array(item.requires).length} direct dependencies</title></g>`;
    }).join('');
    return `<div class="graph-surface"><svg class="module-graph" viewBox="0 0 ${graphWidth} ${graphHeight}" width="100%" role="img" aria-label="${visible.length} modules and ${visibleEdges.length} direct dependency edges"><defs><marker id="grove-edge-arrow" markerWidth="6" markerHeight="6" refX="5" refY="3" orient="auto"><path d="M0,0 L6,3 L0,6 Z" fill="var(--muted)"/></marker></defs>${wires}${nodes}</svg></div><p class="muted">${visible.length} modules · ${visibleEdges.length} direct edges · arrows point from the requiring module to its dependency.</p>`;
  }

  function dependencies() {
    const all = modules();
    const focus = selectedFocus();
    const direct = edges();
    return `<div class="metric-strip">${metric('Modules', all.length)}${metric('Direct edges', direct.length)}${metric('Maximum depth', text(meta().maxDepth ?? (all.length ? Math.max(...all.map(item => number(item.depth) || 0)) : null)))}</div>${panel('Module topology', `<div class="filter-bar graph-controls"><label class="input-wrap"><span>Graph scope</span><select data-module-graph="scope"><option value="neighborhood" ${state.graphScope === 'neighborhood' ? 'selected' : ''}>Selected neighborhood</option><option value="host" ${state.graphScope === 'host' ? 'selected' : ''}>Full host</option></select></label><label class="input-wrap"><span>Focus module</span><select data-module-graph="focus">${all.map(item => `<option value="${esc(idOf(item))}" ${item === focus ? 'selected' : ''}>${esc(titleOf(item))}</option>`).join('')}</select></label>${focus ? `<button type="button" class="btn" data-module-detail="${esc(idOf(focus))}">Inspect selected module</button>` : ''}</div><div data-module-graph-canvas>${graphMarkup()}</div>`, 'Direct compiled topology')}<div class="content-grid">${panel('Selected module', focus ? `<div class="module-detail-grid"><h3>${esc(titleOf(focus))}</h3><p class="mono muted">${text(focus.assembly)}</p><h4>Direct dependencies</h4>${relationList(focus.requires)}<h4>Direct dependents</h4>${relationList(focus.dependents)}</div>` : empty('Select a module.'))}${panel('Direct dependency edges', direct.length ? `<div class="table-wrap"><table class="data-table"><thead><tr><th>Requires module</th><th>Required module</th></tr></thead><tbody>${direct.map(edge => `<tr><td>${link(edge.source)}</td><td>${edge.target ? link(edge.target) : esc(edge.targetName)}</td></tr>`).join('')}</tbody></table></div>` : empty('No direct dependency edges recorded.'))}</div>`;
  }

  function discovery() {
    const d = meta().discovery || {};
    const stats = d.statistics || d;
    const stages = array(d.stages);
    const queries = array(d.queries);
    const counters = [['Assemblies', stats.assemblyCount ?? stats.assembliesScanned], ['Types enumerated', stats.enumeratedTypeCount ?? stats.typeCount ?? stats.typesEnumerated], ['Non-empty plans', stats.nonEmptyPlanCount ?? stats.planCount], ['Distinct queries', stats.distinctQueryCount ?? stats.queryCount], ['Matches', stats.matchCount]];
    return `<div class="metric-strip">${counters.map(([label, value]) => metric(label, text(value))).join('')}</div>${panel('Discovery evidence', `<p class="muted">One plan declaration pass, one centralized compilation, and one registration commit boundary.</p>${stages.length ? `<div class="table-wrap"><table class="data-table"><thead><tr><th>Factual stage</th><th>Duration</th></tr></thead><tbody>${stages.map(item => `<tr><td>${text(item.name || item.stage || item.kind)}</td><td class="mono">${ms(item.durationMs ?? item.elapsedMs)}</td></tr>`).join('')}</tbody></table></div>` : empty('Typed discovery stages not recorded', 'The capture does not include stage-by-stage discovery evidence.')}`)}${panel('Query contributions', queries.length ? `<div class="table-wrap"><table class="data-table"><thead><tr><th>Query ID</th><th>Consumers</th><th>Matches</th></tr></thead><tbody>${queries.map(item => `<tr><td class="mono">${text(item.queryId || item.id)}</td><td>${array(item.consumerModuleIds || item.consumers).map(relationName).map(esc).join(', ') || 'Not recorded'}</td><td class="mono">${text(item.matchCount)}</td></tr>`).join('')}</tbody></table></div>` : empty(recorded(d.queries) ? 'No non-empty discovery query was compiled.' : 'Query contributions not recorded'), 'Identical queries are evaluated once and shared by consuming modules')}`;
  }

  function detail(item, tab = 'summary') {
    const tabs = ['summary', 'dependencies', 'performance'];
    const content = tab === 'dependencies' ? `<h3>Direct dependencies</h3>${relationList(item.requires)}<h3>Direct dependents</h3>${relationList(item.dependents)}<a class="btn" href="#/module-system-dashboard/dependencies" data-module-focus-link="${esc(idOf(item))}">Open neighborhood explorer</a>` : tab === 'performance' ? `<div class="metric-strip">${metric('Recorded cost', ms(item.costMs))}</div>${phaseRows(item).length ? `<div class="table-wrap"><table class="data-table"><thead><tr><th>Phase</th><th>Duration</th></tr></thead><tbody>${phaseRows(item).map(phase => `<tr><td>${esc(phase.name)}</td><td class="mono">${ms(phase.duration)}</td></tr>`).join('')}</tbody></table></div>` : empty('No module-owned spans recorded', 'Aggregate recorded cost is shown when available.')}` : `<dl class="module-detail-grid"><div><dt>State</dt><dd>${esc(moduleState(item))}</dd></div><div><dt>Last phase</dt><dd>${text(item.phase || phaseRows(item).at(-1)?.name)}</dd></div><div><dt>Registration order</dt><dd class="mono">${text(item.registrationOrder)}</dd></div><div><dt>Dependency depth</dt><dd class="mono">${text(item.depth)}</dd></div><div><dt>Assembly</dt><dd class="mono">${text(item.assembly)}</dd></div><div><dt>Capabilities</dt><dd>${item.isUI ? '<span class="badge">UI</span> ' : ''}${item.isWeb ? '<span class="badge">Web</span>' : ''}${!item.isUI && !item.isWeb ? 'Core' : ''}</dd></div></dl><p class="mono muted">${text(item.fullTypeName)}</p><p class="muted">${array(item.requires).length} direct dependencies · ${array(item.dependents).length} direct dependents</p>`;
    dialog(titleOf(item), `<p class="eyebrow">SELECTED MODULE / CAPTURED HOST EVIDENCE</p><nav class="tab-strip module-dialog-tabs" aria-label="Module details">${tabs.map(value => `<button type="button" class="${tab === value ? 'active' : ''}" data-module-detail-tab="${value}" data-module-id="${esc(idOf(item))}" aria-pressed="${tab === value}">${value[0].toUpperCase() + value.slice(1)}</button>`).join('')}</nav><div class="module-dialog-content">${content}</div>`);
  }

  function exportSnapshot() {
    const snapshot = { schemaVersion: 'grove-host-capture-v1', capturedAt: data().capturedAt || meta().captureTime || null, moduleMeta: meta(), modules: modules() };
    const url = URL.createObjectURL(new Blob([JSON.stringify(snapshot, null, 2)], { type: 'application/json' }));
    const anchor = document.createElement('a');
    anchor.href = url; anchor.download = `grove-module-capture-${meta().revision ?? 'snapshot'}.json`; anchor.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
    toast('Captured module diagnostics exported.');
  }

  function action(name) {
    if (name === 'export') return exportSnapshot();
    if (name === 'compare') return dialog('Compare a captured baseline', `<p class="muted">Select a Monica diagnostics export or another Grove host capture to compare its recorded composition duration and module inventory.</p><label class="input-wrap"><span>Baseline JSON · maximum 10 MB</span><input type="file" accept="application/json,.json" data-module-baseline></label><p class="muted">This comparison reads a local file. It does not change the running service.</p><div data-module-baseline-error role="alert"></div>`);
    if (name === 'refresh') {
      if (typeof app().refreshSnapshot === 'function') return app().refreshSnapshot();
      return dialog('Diagnostics capture', `<p>This prototype displays an immutable host capture.</p><dl class="module-detail-grid"><div><dt>Captured</dt><dd>${text(meta().captureTime || data().capturedAt)}</dd></div><div><dt>Revision</dt><dd>${text(meta().revision)}</dd></div></dl><p class="muted">Reloading this page rereads the saved capture. Measurements advance only when a new host capture is supplied.</p><button type="button" class="btn btn-primary" data-workbench-action="reload-capture">Reload saved capture</button>`);
    }
    if (name === 'help') return dialog('Read the module workbench', `<div class="evidence-list"><p><strong>Overview</strong> explains the captured composition outcome, findings, and recorded module costs.</p><p><strong>Performance</strong> shows exact captured phases and aggregate contributors. Missing timings remain marked Not recorded.</p><p><strong>Modules</strong> filters the declared module inventory. Open any module to inspect its summary, cost, and relationships.</p><p><strong>Dependencies</strong> follows direct compiled relationships. An arrow points from a requiring module to its required module.</p><p><strong>Discovery</strong> presents type-discovery compiler evidence where the capture includes it.</p><p class="muted">Compare reads a local JSON baseline; Export downloads this capture. All measurements belong to the captured host instance.</p></div>`);
    if (name === 'reload-capture') return window.location.reload();
    if (name === 'remove-baseline') { state.baseline = null; rerender(); return; }
    if (name === 'reset') { state.search = ''; state.filter = 'all'; state.assembly = ''; state.page = 1; rerender(); return; }
    if (name === 'previous-page') { state.page = Math.max(1, state.page - 1); updateCatalog(); return; }
    if (name === 'next-page') { state.page += 1; updateCatalog(); }
  }

  function rerender() {
    if (!currentContainer) return;
    currentContainer.innerHTML = render(state.section);
  }

  function click(event) {
    const detailButton = event.target.closest('[data-module-detail]');
    if (detailButton) { const item = findModule(detailButton.dataset.moduleDetail); if (item) detail(item); return; }
    const tab = event.target.closest('[data-module-detail-tab]');
    if (tab) { const item = findModule(tab.dataset.moduleId); if (item) detail(item, tab.dataset.moduleDetailTab); return; }
    const focusLink = event.target.closest('[data-module-focus-link]');
    if (focusLink) { state.focus = focusLink.dataset.moduleFocusLink; const dialogElement = focusLink.closest('dialog'); if (dialogElement?.open) dialogElement.close(); return; }
    const actionButton = event.target.closest('[data-workbench-action]');
    if (actionButton) action(actionButton.dataset.workbenchAction);
  }

  async function importBaseline(input) {
    const file = input.files?.[0];
    if (!file) return;
    const errorNode = input.closest('dialog')?.querySelector('[data-module-baseline-error]') || document.querySelector('[data-module-baseline-error]');
    try {
      if (file.size > 10 * 1024 * 1024) throw new Error('The selected baseline exceeds the 10 MB import limit.');
      const baseline = JSON.parse(await file.text());
      const sourceModules = baseline.modules ?? baseline.Modules;
      const sourceSummary = baseline.summary ?? baseline.Summary ?? baseline.moduleMeta;
      if (!Array.isArray(sourceModules) || !sourceSummary) throw new Error('This file does not contain a module diagnostics capture.');
      const moduleNames = sourceModules.map(item => item.name || item.typeName || item.TypeName || item.moduleId || item.ModuleId);
      const currentNames = modules().map(titleOf);
      state.baseline = { label: file.name, totalMs: sourceSummary.totalMs ?? sourceSummary.totalCompositionDurationMs ?? sourceSummary.TotalCompositionDurationMs, added: currentNames.filter(name => !moduleNames.includes(name)), removed: moduleNames.filter(name => !currentNames.includes(name)) };
      rerender();
      dialog('Baseline comparison', `<p class="eyebrow">PORTABLE BASELINE</p><p>${esc(file.name)}</p><div class="metric-strip">${metric('Composition delta', baselineDelta())}${metric('Modules added', state.baseline.added.length)}${metric('Modules removed', state.baseline.removed.length)}</div><h3>Added modules</h3><p class="mono muted">${state.baseline.added.map(esc).join(', ') || 'None'}</p><h3>Removed modules</h3><p class="mono muted">${state.baseline.removed.map(esc).join(', ') || 'None'}</p>`);
    } catch (error) {
      if (errorNode) errorNode.textContent = error.message;
      else toast(error.message);
    }
  }

  function render(section) {
    state.section = sections.includes(String(section || '').toLowerCase()) ? String(section).toLowerCase() : 'overview';
    const views = { overview, modules: catalog, performance, dependencies, discovery };
    return header() + views[state.section]();
  }

  function bind(container) {
    currentContainer = container;
    if (!bound.has(container)) {
      bound.add(container);
      container.addEventListener('click', click);
      container.addEventListener('input', event => {
        if (event.target.dataset.moduleFilter === 'search') { state.search = event.target.value; state.page = 1; updateCatalog(); }
      });
      container.addEventListener('change', event => {
        const filter = event.target.dataset.moduleFilter;
        if (filter === 'state') state.filter = event.target.value;
        if (filter === 'assembly') state.assembly = event.target.value;
        if (filter === 'page-size') state.pageSize = Number(event.target.value);
        if (filter) { state.page = 1; updateCatalog(); }
        const graph = event.target.dataset.moduleGraph;
        if (graph === 'scope') state.graphScope = event.target.value;
        if (graph === 'focus') state.focus = event.target.value;
        if (graph) rerender();
      });
    }
    if (!dialogEventsBound) {
      dialogEventsBound = true;
      document.addEventListener('click', event => { if (!currentContainer?.contains(event.target) && event.target.closest('dialog')) click(event); });
      document.addEventListener('change', event => { if (event.target.matches('[data-module-baseline]')) importBaseline(event.target); });
    }
  }

  window.GroveModules = { render, bind };
})();
