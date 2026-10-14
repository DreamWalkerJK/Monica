/* Grove B, applied to the Flight host's captured information architecture. */
(() => {
  'use strict';
  const data = window.GROVE_DATA;
  const main = document.getElementById('main');
  const workspace = document.querySelector('.workspace');
  const modal = document.getElementById('detail-dialog');
  let dialogTrigger;
  let dialogTriggerSelector;
  let lastActionSelector;
  let toastTimer;
  let current;
  let shell;
  const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const date = value => value ? new Date(value).toLocaleString('en-GB', {hour12:false}) : 'Not recorded';
  const time = value => value ? new Date(value).toLocaleTimeString('en-GB', {hour12:false}) : 'Not recorded';
  const num = value => Number(value).toLocaleString('en-US',{maximumFractionDigits:2});
  const iconPaths = {
    refresh:'<path d="M20 7v5h-5M4 17v-5h5M19 12a7 7 0 0 0-12-5L4 10m1 2a7 7 0 0 0 12 5l3-3"/>',
    arrow:'<path d="M7 17 17 7M7 7h10v10"/>',
    pulse:'<path d="M3 12h4l3-8 4 16 3-8h4"/>',
    clock:'<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
    server:'<rect x="3" y="4" width="18" height="7" rx="2"/><rect x="3" y="15" width="18" height="5" rx="2"/><path d="M7 8h.01m0 10h.01"/>',
    code:'<path d="m7 6-6 6 6 6M17 6l6 6-6 6m-4-15-2 18"/>',
    module:'<path d="m12 3 9 5-9 5-9-5ZM3 8v9l9 5 9-5V8M12 13v9"/>',
    info:'<circle cx="12" cy="12" r="9"/><path d="M12 11v6m0-10h.01"/>',
    copy:'<rect x="8" y="8" width="13" height="13" rx="2"/><path d="M16 8V3H3v13h5"/>',
    check:'<path d="m5 12 4 4L19 6"/>',
    restart:'<path d="M12 3v9M6 5a9 9 0 1 0 12 0"/>',
    book:'<path d="M12 5v15M3 4c4-1 6 0 9 2 3-2 5-3 9-2v14c-4-1-6 0-9 2-3-2-5-3-9-2Z"/>'
  };
  const icon = name => `<svg class="sn-icon" viewBox="0 0 24 24" aria-hidden="true">${iconPaths[name] || iconPaths.server}</svg>`;
  function toast(message) {
    const node = document.querySelector('.toast');
    clearTimeout(toastTimer);node.textContent=message;node.hidden=false;
    toastTimer=setTimeout(()=>node.hidden=true,4000);
  }
  function openDialog(title, html) {
    if (!modal.open) {
      dialogTrigger=document.activeElement;
      dialogTriggerSelector=lastActionSelector;
    }
    document.getElementById('dialog-title').textContent=title;
    modal.querySelector('.modal-body').innerHTML=html;
    if (!modal.open) modal.showModal();
  }
  modal.addEventListener('close',()=>{
    const replacement=dialogTriggerSelector && main.querySelector(dialogTriggerSelector);
    const target=dialogTrigger?.isConnected && dialogTrigger!==document.body ? dialogTrigger : replacement || main.querySelector('h1');
    target?.focus();
  });
  function rememberAction(event) {
    const button=event.target.closest('button,a');
    if(!button || !main.contains(button))return;
    const attribute=['data-config-action','data-module-detail','data-workbench-action','data-app-action','data-inspect-endpoint'].find(name=>button.hasAttribute(name));
    lastActionSelector=attribute ? `[${attribute}="${CSS.escape(button.getAttribute(attribute))}"]` : null;
  }
  document.addEventListener('pointerdown',rememberAction,true);
  document.addEventListener('focusin',rememberAction,true);
  modal.addEventListener('click', event => {
    if(event.target!==modal)return;
    const rect=modal.getBoundingClientRect();
    if(event.clientX<rect.left||event.clientX>rect.right||event.clientY<rect.top||event.clientY>rect.bottom)modal.close();
  });
  const hostLink = (path,label='Open running host') => `<a class="btn" href="${esc(data.hostUrl+path)}" target="_blank" rel="noopener">${esc(label)} ${icon('arrow')}</a>`;
  function footer() {
    return `<footer class="page-footer"><span>Grove B · FlightService.API · <button class="inline-link" data-app-action="about">Captured host prototype ${icon('info')}</button></span><span>Snapshot ${esc(time(data.capturedAt))} · ${hostLink(current.path || current.id,'Original page')}</span></footer>`;
  }
  function metric(label,value,note,svg) {
    return `<article class="metric"><span class="metric-label">${icon(svg)}${label}</span><strong class="metric-value">${value}</strong><span class="metric-foot">${note}</span></article>`;
  }
  function rows(values) {
    return `<dl class="definition-list">${values.map(([label,value,mono=false])=>`<div><dt>${esc(label)}</dt><dd ${mono?'class="mono"':''}>${esc(value ?? 'Not recorded')}</dd></div>`).join('')}</dl>`;
  }
  function heading(eyebrow,title,description,actions='') {
    return `<header class="page-heading"><div><p class="eyebrow">${eyebrow}</p><h1>${title}</h1><p>${description}</p></div><div class="page-actions">${actions}</div></header>`;
  }
  function system() {
    const s=data.system,a=s.application,p=s.process,h=s.host,art=s.artifact;
    const capture=s.capturedAtUtc || data.capturedAt;
    const uptimeSeconds=Math.max(0,Math.floor((new Date(capture)-new Date(p.startedAtUtc))/1000));
    const uptime=Number.isFinite(uptimeSeconds)?`${Math.floor(uptimeSeconds/3600).toString().padStart(2,'0')}:${Math.floor(uptimeSeconds%3600/60).toString().padStart(2,'0')}:${(uptimeSeconds%60).toString().padStart(2,'0')}`:'Not recorded';
    const mem=p.workingSetBytes?num(p.workingSetBytes/1048576):'Not recorded';
    const links = s.links || [
      {label:'接口管理',description:'API Management',path:'/swagger',icon:'code',external:true},
      {label:'服务发现',description:'Service Discovery',path:'/service-discovery-manage',icon:'server'},
      {label:'项目单元',description:'Project Units',path:'/project-units',icon:'module'},
      {label:'Observable Instance',description:'Instance state and exceptions',path:'/observable-instance-monitor',icon:'pulse'},
      {label:'EventBus',description:'Subscriptions and state',path:'/event-bus-monitor',icon:'server'},
      {label:'Execution Timing',description:'API performance evidence',path:'/execution-timing',icon:'clock'},
      {label:'Job Scheduler',description:'Scheduler overview',path:'/job-scheduler',icon:'clock'}
    ];
    const sections=[['runtime','Runtime pulse'],['environment','Environment fingerprint'],['endpoints','Listening endpoints'],['artifact','Artifact evidence']];
    return heading('MONICA / LIVE SERVICE PROFILE','System dossier','Identity, readiness, and runtime evidence for this host instance.',`<button class="btn" data-app-action="restart">${icon('restart')}Restart service</button><button class="btn btn-primary" data-app-action="refresh">${icon('refresh')}Refresh evidence</button>`)+
      `<section class="system-hero" aria-label="${esc(a.name)}"><div class="system-identity"><p class="eyebrow">APPLICATION / FLIGHT SERVICE</p><h2 class="service-name">${esc(a.name)}</h2><div class="identity-meta"><span class="mono">${esc(a.id)}</span><span class="badge">${esc(a.environmentName)}</span><span>${esc(a.version)}</span></div><p class="hero-detail">Product ${esc(art.productVersion)}</p></div><div class="readiness-block"><span class="badge">${icon('check')}Ready</span><p>Application startup</p><strong class="startup-value">${num(s.startup?.durationMs)} <small>ms</small></strong><p>Host ready at ${esc(time(s.startup?.readyAtUtc))}</p></div></section>`+
      `<section class="pulse-grid" aria-label="Runtime evidence summary">${metric('Process uptime',uptime,'At capture · running','clock')}${metric('Working set',`${mem} <small>MiB</small>`,'Point-in-time measurement','pulse')}${metric('Process ID',num(p.processId),'FlightService.API','server')}${metric('Evidence captured',time(capture),date(capture).split(',')[0],'clock')}</section>`+
      `<div class="section-title"><div><h2>Configured links</h2><p class="muted">Host-provided operational destinations.</p></div><span class="badge">Monica · ${links.length}</span></div><nav class="quick-links" aria-label="Configured operational links">${links.map(link=>`<a class="quick-link" href="${link.external?esc(data.hostUrl+link.path):'#'+esc(link.path)}" ${link.external?'target="_blank" rel="noopener"':''}>${icon(link.icon)}<span><strong>${esc(link.label)}</strong><small>${esc(link.description)}</small></span>${icon('arrow')}</a>`).join('')}</nav>`+
      `<div class="dossier-layout"><nav class="dossier-index" aria-label="Dossier index"><p class="eyebrow">DOSSIER INDEX</p>${sections.map(([id,label],i)=>`<button data-dossier-section="${id}" ${i===0?'class="active" aria-current="location"':''}><span class="mono">0${i+1}</span> &nbsp;${label}</button>`).join('')}<small class="muted">Captured ${esc(time(capture))}</small></nav><div class="dossier-content">`+
      `<section id="runtime" class="dossier-section"><header class="section-title"><div><p class="eyebrow">01 / PROCESS EVIDENCE</p><h2>Runtime pulse</h2></div>${icon('pulse')}</header>${rows([['.NET runtime',p.runtimeVersion],['Process architecture',p.architecture || '64-bit process'],['Privilege',p.isPrivileged?'Privileged':'Not privileged'],['System page size',num(p.pageSizeBytes)+' B'],['User interactive',p.isInteractive?'Interactive':'Not interactive'],['Working set',num(p.workingSetBytes)+' B',true],['Process started',date(p.startedAtUtc)],['Process ID',num(p.processId),true]])}</section>`+
      `<section id="environment" class="dossier-section"><header class="section-title"><div><p class="eyebrow">02 / EXECUTION CONTEXT</p><h2>Environment fingerprint</h2></div>${icon('server')}</header>${rows([['Machine',h.machineName,true],['Process user',`${h.userDomainName || ''}${h.userDomainName?'\\':''}${h.userName || ''}`,true],['Operating system',h.operatingSystem],['Time zone',typeof h.timeZone==='string'?h.timeZone:h.timeZone?.displayName || h.timeZone?.id],['Working directory',h.currentDirectory,true],['Process path',p.processPath,true]])}</section>`+
      `<section id="endpoints" class="dossier-section"><header class="section-title"><div><p class="eyebrow">03 / BOUND ADDRESSES</p><h2>Listening endpoints</h2></div><span class="badge">${s.endpoints.length} bound</span></header><div class="panel-body">${s.endpoints.map(endpoint=>`<div class="endpoint-row"><span class="badge">${esc(endpoint.scheme?.toUpperCase() || 'HTTP')}</span><strong class="mono">${esc(endpoint.address)}</strong><span class="badge success">Bound</span><button class="btn btn-small" data-inspect-endpoint="${esc(endpoint.address)}">Inspect ${icon('arrow')}</button></div>`).join('')}</div></section>`+
      `<section id="artifact" class="dossier-section"><header class="section-title"><div><p class="eyebrow">04 / PRODUCT METADATA</p><h2>Artifact evidence</h2></div>${icon('code')}</header>${rows([['Product',art.productName],['Product version',art.productVersion,true],['File version',art.fileVersion],['Artifact timestamp',date(art.artifactTimestampUtc)],['Company',art.companyName],['Legal',art.legalCopyright || '—']])}</section></div></div>`;
  }
  const evidenceByRoute={
    '/system-info':'system-original.png',
    '/module-system-dashboard/overview':'overview-original.png',
    '/module-system-dashboard/modules':'modules-original.png',
    '/module-system-dashboard/dependencies':'dependencies-original.png',
    '/configuration/state':'configuration-original.png'
  };
  function sourcePage(page) {
    return heading(`MONICA / ${esc(page.categoryLabel.toUpperCase())}`,esc(page.label),'A registered destination in the captured Flight host.',hostLink(page.path || page.id))+
      `<section class="panel"><div class="panel-heading"><div><h2>Current host destination</h2><p>This screen is available in the running application.</p></div><span class="badge">Registered</span></div>${rows([['Navigation category',page.categoryLabel],['Route',page.path || page.id,true],['Application',data.system.application.name],['Host',data.hostUrl,true]])}<div class="panel-body"><p class="muted">The detailed Grove studies cover System dossier, Module workbench, and Configuration State. This destination keeps its real navigation label and route; open the current host to inspect its existing screen.</p><div class="page-actions">${hostLink(page.path || page.id)}<a class="btn" href="#/system-info">System dossier</a><a class="btn" href="#/module-system-dashboard/modules">Module catalog</a></div></div></section>`;
  }
  function render(page) {
    current=page;
    const route=page.path || page.id;
    let html;
    if(route==='/system-info')html=system();
    else if(route.startsWith('/module-system-dashboard'))html=window.GroveModules.render(route.split('/')[2] || 'overview');
    else if(route.startsWith('/configuration/'))html=window.GroveConfiguration.render(route.split('/')[2]);
    else html=sourcePage(page);
    main.innerHTML=html+footer();
    main.querySelector('h1')?.setAttribute('tabindex','-1');
    if(route.startsWith('/module-system-dashboard'))window.GroveModules.bind(main);
    if(route.startsWith('/configuration/'))window.GroveConfiguration.bind(main);
    document.title=`${route==='/system-info'?'System dossier':page.label} · Grove · Flight host`;
    workspace.scrollTop=0;
  }
  function about() {
    const route=current.path || current.id;
    openDialog('Grove, grounded in the running host',`<p>Direction B is selected. This prototype applies its warm stone, forest green, and restrained typography to Monica’s actual screens.</p>${rows([['Source host','FIPS2022 / FlightService.API'],['Navigation',`${data.navigation.length} categories · registered host destinations`],['Module capture',`${data.modules.length} modules · revision #${data.moduleMeta.revision}`],['Configuration catalog',`${data.configuration.definitions.length} actual definitions`]])}<p style="margin-top:16px">The workbench’s five existing sections are projected into a nested menu. Layout and color mode live together in Appearance. Local filters, comparisons, and configuration drafts make the design interactive.</p><p>Measurements are captured evidence. Configuration edits remain in this preview. Host restart is represented by an explanatory dialog.</p><div class="page-actions">${hostLink(route)}${evidenceByRoute[route]?`<a class="btn" href="evidence/${evidenceByRoute[route]}" target="_blank" rel="noopener">Original screenshot ${icon('arrow')}</a>`:''}<a class="btn" href="design.md" target="_blank">Design notes</a></div>`);
  }
  function refreshSnapshot() {
    openDialog('Refresh captured evidence',`<p>This design uses the host capture from ${esc(date(data.capturedAt))}. It stays stable while you compare layouts.</p><p>Open the running host for new measurements, or reload the saved evidence in this preview.</p><div class="page-actions">${hostLink(current.path || current.id)}<button class="btn btn-primary" data-app-action="reload">Reload saved capture</button></div>`);
  }
  document.addEventListener('click',async event=>{
    const section=event.target.closest('[data-dossier-section]');
    if(section){document.getElementById(section.dataset.dossierSection)?.scrollIntoView({block:'start'});main.querySelectorAll('[data-dossier-section]').forEach(button=>{const active=button===section;button.classList.toggle('active',active);active?button.setAttribute('aria-current','location'):button.removeAttribute('aria-current');});return;}
    const endpoint=event.target.closest('[data-inspect-endpoint]');
    if(endpoint){openDialog('Listening endpoint',`${rows([['Address',endpoint.dataset.inspectEndpoint,true],['Transport','HTTP'],['State','Bound'],['Observed in',data.system.application.id,true]])}<div class="page-actions"><button class="btn" data-copy-value="${esc(endpoint.dataset.inspectEndpoint)}">${icon('copy')}Copy address</button>${hostLink('/system-info')}</div>`);return;}
    const copy=event.target.closest('[data-copy-value]');
    if(copy){try{await navigator.clipboard.writeText(copy.dataset.copyValue);toast('Copied to clipboard.');}catch{toast('Clipboard is unavailable in this browser.');}return;}
    const action=event.target.closest('[data-app-action]')?.dataset.appAction;
    if(action==='close-dialog')modal.close();
    if(action==='about')about();
    if(action==='refresh')refreshSnapshot();
    if(action==='reload')location.reload();
    if(action==='restart')openDialog('Restart service',`<p>The current Flight host exposes a graceful restart action. Its availability is retained in this design.</p><p>This prototype does not send the restart request. A production confirmation should explain that an external supervisor must start the process again.</p>${hostLink('/system-info','Inspect original control')}`);
    if(action==='language')openDialog('Language',`<p>The current shell supports English and Simplified Chinese. This design preview uses English interface labels and preserves application-owned names from the Flight host.</p>${rows([['Preview language','English'],['Original application name','航班服务'],['Host cultures','en-US · zh-CN']])}<div class="page-actions">${hostLink('/system-info','Open host language settings')}</div>`);
  });
  window.GroveApp={openDialog,toast,refreshSnapshot};
  shell=window.MonicaShellNavigation.mount({id:'flight-host',layout:'side',theme:'light',onNavigate:render});
  window.GroveApp.navigation=shell;
})();
