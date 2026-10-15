const $ = (s, root = document) => root.querySelector(s);
const $$ = (s, root = document) => [...root.querySelectorAll(s)];
const paths = {
  search:'<circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 4 4"/>',
  overview:'<rect x="3" y="3" width="7" height="7" rx="1.4"/><rect x="14" y="3" width="7" height="7" rx="1.4"/><rect x="3" y="14" width="7" height="7" rx="1.4"/><rect x="14" y="14" width="7" height="7" rx="1.4"/>',
  ai:'<path d="m12 3 2.5 6.5L21 12l-6.5 2.5L12 21l-2.5-6.5L3 12l6.5-2.5Z"/>',
  knowledge:'<path d="M12 5v15M3 4c4-1 6 0 9 2 3-2 5-3 9-2v14c-4-1-6 0-9 2-3-2-5-3-9-2Z"/>',
  modules:'<path d="m12 3 9 5-9 5-9-5Z M3 8v9l9 5 9-5V8 M12 13v9 M7.5 5.5l9 5"/>',
  config:'<path d="M4 7h16M4 17h16"/><circle cx="8" cy="7" r="3" fill="var(--surface)"/><circle cx="16" cy="17" r="3" fill="var(--surface)"/>',
  monitor:'<path d="M3 12h4l3-8 4 16 3-8h4"/>',
  jobs:'<circle cx="12" cy="12" r="9"/><path d="M12 7v6l4 2"/>',
  bell:'<path d="M5 17h14l-2-3V9a5 5 0 0 0-10 0v5ZM10 21h4"/>',
  menu:'<path d="M4 6h16M4 12h16M4 18h16"/>',
  arrow:'<path d="M4 12h16m-5-5 5 5-5 5"/>',
  check:'<path d="m5 12 4 4L19 6"/>',
  refresh:'<path d="M20 10a8 8 0 1 0-1 8M20 4v6h-6"/>',
  moon:'<path d="M20 14A8 8 0 0 1 10 4 8 8 0 1 0 20 14Z"/>',
  sun:'<circle cx="12" cy="12" r="4"/><path d="M12 2v2m0 16v2M2 12h2m16 0h2M5 5l1.5 1.5m11 11L19 19M5 19l1.5-1.5m11-11L19 5"/>'
};
const icon = name => `<svg class="icon" aria-hidden="true" viewBox="0 0 24 24">${paths[name] || paths.modules}</svg>`;
function hydrateIcons() { $$('[data-icon]').forEach(e => e.innerHTML = icon(e.dataset.icon)); }
const pages = [
  {id:'overview',label:'Overview',icon:'overview'}, {id:'ai',label:'AI workspace',icon:'ai'},
  {id:'knowledge',label:'Knowledge',icon:'knowledge'}, {id:'modules',label:'Modules',icon:'modules',count:'24'},
  {id:'configuration',label:'Configuration',icon:'config'}, {id:'observability',label:'Observability',icon:'monitor'},
  {id:'jobs',label:'Jobs',icon:'jobs'}
];
const modules = [
  {name:'Event bus',namespace:'Monica.EventBus',kind:'Messaging',provider:'RabbitMQ',status:'Healthy',icon:'modules',detail:'Event delivery and durable subscriptions across your application.',metric:'12 active subscriptions'},
  {name:'AI services',namespace:'Monica.AI',kind:'Intelligence',provider:'OpenAI',status:'Healthy',icon:'ai',detail:'Provider composition for chat, generation and model discovery.',metric:'8 registered providers'},
  {name:'Job scheduler',namespace:'Monica.JobScheduler',kind:'Background',provider:'PostgreSQL',status:'Healthy',icon:'jobs',detail:'Recurring and triggered jobs with durable execution history.',metric:'1,284 completed today'},
  {name:'Configuration',namespace:'Monica.Configuration',kind:'Infrastructure',provider:'EF Core',status:'Review',icon:'config',detail:'Managed configuration with runtime validation and reload.',metric:'1 pending configuration review'},
  {name:'Observability',namespace:'Monica.OpenTelemetry',kind:'Monitoring',provider:'OpenTelemetry',status:'Healthy',icon:'monitor',detail:'Distributed tracing and metrics for the entire workspace.',metric:'248k requests today'},
  {name:'Knowledge',namespace:'Monica.AI.RAG',kind:'Intelligence',provider:'Local store',status:'Healthy',icon:'knowledge',detail:'Knowledge collections, retrieval and indexing pipelines.',metric:'6 indexed collections'}
];
let currentPage = 'overview';
let moduleFilter = 'All';
let moduleSearch = '';
const previewPreferences = {name:'Monica workspace',environment:'Development',showStatus:true};
function heading(title,subtitle,actions='') { return `<div class="page-heading"><div><div class="eyebrow">MONICA / WORKSPACE</div><h1>${title}</h1><p>${subtitle}</p></div><div class="heading-actions">${actions}</div></div>`; }
function tableRows(items) { return items.map(m => `<tr><td><div class="module-name"><span class="module-icon">${icon(m.icon)}</span><span>${m.name}<small>${m.namespace}</small></span></div></td><td class="mono">${m.provider}</td><td><span class="status ${m.status==='Review'?'warn':''}"><i></i>${m.status==='Review'?'Needs review':m.status}</span></td><td><button class="row-action" data-module="${m.name}" aria-label="View ${m.name} details">${icon('arrow')}</button></td></tr>`).join(''); }
function moduleTable(items) { return `<div class="table-scroll"><table><thead><tr><th>MODULE</th><th>PROVIDER</th><th>STATUS</th><th><span aria-label="Actions"></span></th></tr></thead><tbody>${items.length?tableRows(items):'<tr><td colspan="4" class="empty">No modules match your search.</td></tr>'}</tbody></table></div>`; }
const topology = `<svg class="topology" viewBox="0 0 440 225" aria-label="Monica host connects AI, messaging, jobs and observability" role="img"><defs><pattern id="dots" x="0" y="0" width="18" height="18" patternUnits="userSpaceOnUse"><circle cx="1" cy="1" r=".7" fill="currentColor" opacity=".12"/></pattern></defs><rect width="440" height="225" fill="url(#dots)"/><g class="trace"><path d="M215 104V53H112M215 104V53H328M215 126v51H111M215 126v51h113"/><path d="M111 65v92M330 65v92" stroke-dasharray="3 4"/></g><rect class="node" x="173" y="79" width="84" height="62" rx="10"/><rect x="199" y="89" width="31" height="27" rx="7" fill="currentColor"/><text x="215" y="109" text-anchor="middle" style="fill:var(--surface);font-size:23px;font-weight:500">m</text><text x="215" y="131" text-anchor="middle">Monica host</text><g><rect class="node" x="64" y="33" width="93" height="38" rx="7"/><text x="111" y="50" text-anchor="middle">AI services</text><text class="node-label" x="111" y="62" text-anchor="middle">8 providers</text><rect class="node" x="283" y="33" width="93" height="38" rx="7"/><text x="329" y="50" text-anchor="middle">Event bus</text><text class="node-label" x="329" y="62" text-anchor="middle">12 subscriptions</text><rect class="node" x="64" y="159" width="93" height="38" rx="7"/><text x="111" y="176" text-anchor="middle">Job scheduler</text><text class="node-label" x="111" y="188" text-anchor="middle">Ready</text><rect class="node" x="283" y="159" width="93" height="38" rx="7"/><text x="329" y="176" text-anchor="middle">Observability</text><text class="node-label" x="329" y="188" text-anchor="middle">Connected</text></g><g fill="var(--success)"><circle cx="157" cy="34" r="3"/><circle cx="376" cy="34" r="3"/><circle cx="157" cy="159" r="3"/><circle cx="376" cy="159" r="3"/></g></svg>`;
function chart(range='24h') {
  const points = range==='24h'?'40,103 60,108 80,92 100,99 120,68 140,84 160,75 180,89 200,60 220,64 240,39 260,51 280,34 300,41 320,22 340,46 360,31 380,42 400,26 420,30 440,17 460,23 480,13':'40,101 60,96 80,104 100,89 120,99 140,67 160,72 180,53 200,68 220,40 240,58 260,31 280,52 300,37 320,46 340,26 360,31 380,20 400,35 420,24 440,16 460,25 480,20';
  return `<svg class="chart" viewBox="0 0 500 155" role="img" aria-label="Request volume rises through the selected ${range==='24h'?'24 hours':'7 days'}; errors remain low.">${[25,60,95,130].map((y,i)=>`<line class="gridline" x1="40" y1="${y}" x2="485" y2="${y}"/><text x="2" y="${y+3}">${['12k','8k','4k','0'][i]}</text>`).join('')}<polyline class="line" points="${points}"/><polyline class="line secondary" points="40,128 60,126 80,129 100,127 120,125 140,128 160,124 180,126 200,129 220,125 240,122 260,126 280,125 300,128 320,124 340,125 360,119 380,126 400,121 420,122 440,120 460,123 480,120"/>${['00:00','04:00','08:00','12:00','16:00','20:00'].map((t,i)=>`<text x="${40+i*83}" y="151">${range==='24h'?t:['Mon','Tue','Wed','Thu','Fri','Sat'][i]}</text>`).join('')}</svg>`;
}
function overview() {
  return heading('Your workspace, in sync.','A clear view of the infrastructure behind your application.',`<button class="button" id="refresh">${icon('refresh')}Refresh</button><a class="button primary" href="#modules">Explore modules ${icon('arrow')}</a>`)+
  `<section class="readiness"><div class="readiness-copy"><div class="eyebrow"><span></span>CONNECTED & READY</div><h2>Many moving parts.<br>One composed system.</h2><p>Your modules are connected, your providers are ready, and everything has room to grow.</p><a href="#modules">View your infrastructure <span>↗</span></a></div>${topology}</section>`+
  `<section class="metric-strip" aria-label="Workspace metrics">${[['Active modules','24',' / 24','All modules initialized','modules'],['AI providers','8','','Across 3 connections','ai'],['Jobs completed','1,284','','↑ 12.8% from yesterday','jobs'],['Success rate','99.98','%','↑ 0.02% this week','monitor']].map(([label,value,unit,note,i])=>`<div class="metric"><div class="metric-label">${label}${icon(i)}</div><div class="metric-value">${value}<small>${unit}</small></div><div class="metric-note">${note.startsWith('↑')?'<span class="positive">'+note.slice(0,note.indexOf(' ',2))+'</span>'+note.slice(note.indexOf(' ',2)):note}</div></div>`).join('')}</section>`+
  `<div class="content-grid"><section class="panel"><div class="panel-head"><div><h2>Request activity</h2><div class="subtitle">Steady traffic. Healthy responses.</div></div><select id="chart-range" aria-label="Activity time range"><option value="24h">Last 24 hours</option><option value="7d">Last 7 days</option></select></div><div class="chart-key"><span><i></i>Requests</span><span><i></i>Errors</span></div><div class="chart-wrap">${chart()}</div></section><section class="panel"><div class="panel-head"><h2>Latest activity</h2><a href="#observability">View all ↗</a></div><div class="activity-list">${[['check','Job completed','Daily report · 240 ms','2m ago',''],['config','Configuration synced','3 values updated · Development','14m ago','blue'],['modules','Provider connected','RabbitMQ · Event bus','32m ago','']].map(([i,title,sub,t,c])=>`<div class="activity-row"><span class="activity-dot ${c}">${icon(i)}</span><div class="activity-text">${title}<p>${sub}</p></div><time>${t}</time></div>`).join('')}</div></section></div>`+
  `<section class="panel"><div class="panel-head"><div><h2>Infrastructure at a glance</h2><div class="subtitle">Core modules serving this workspace</div></div><a href="#modules">All modules ${icon('arrow')}</a></div><div class="module-list">${moduleTable(modules.slice(0,3))}</div></section>`;
}
function moduleView() { return heading('Built to work together.','Discover, inspect and understand your application’s building blocks.', '<span class="status"><i></i>24 modules initialized</span>')+`<section class="panel"><div class="filter-row"><input id="module-search" placeholder="Find a module…" aria-label="Find a module"><button class="filter-chip ${moduleFilter==='All'?'active':''}" data-filter="All">All modules</button><button class="filter-chip ${moduleFilter==='Healthy'?'active':''}" data-filter="Healthy">Healthy</button><button class="filter-chip ${moduleFilter==='Review'?'active':''}" data-filter="Review">Needs review</button><span class="filter-count" id="filter-count"></span></div><div id="module-results"></div></section>`; }
function updateModules() {
  const visible = modules.filter(m => (moduleFilter==='All'||m.status===moduleFilter) && (m.name+' '+m.namespace+' '+m.provider).toLowerCase().includes(moduleSearch.toLowerCase()));
  $('#module-results').innerHTML = moduleTable(visible);
  $('#filter-count').textContent = `${visible.length} of ${modules.length} sample modules`;
  $$('[data-filter]').forEach(b=>b.classList.toggle('active',b.dataset.filter===moduleFilter));
}
function configView() { return heading('A place for every setting.','A consistent language for forms, choices and configuration.')+`<section class="panel"><div class="panel-head"><div><h2>Workspace preferences</h2><div class="subtitle">Changes here apply only to this local design preview.</div></div></div><form id="preferences" style="padding:25px;display:grid;gap:22px;max-width:580px"><label>Workspace name<input name="workspaceName" value="Monica workspace" required style="display:block;width:100%;margin-top:8px;padding:10px 12px;border:1px solid var(--border);background:var(--surface-soft);border-radius:6px;color:var(--text)"></label><label>Environment<select name="environment" style="display:block;margin-top:8px;width:100%;font-size:12px;padding:10px"><option>Development</option><option>Staging</option><option>Production</option></select></label><label style="display:flex;align-items:center;gap:9px;font-size:12px"><input type="checkbox" checked>Show connection status in navigation</label><div style="display:flex;gap:8px"><button class="button primary" type="submit">Save preferences</button><button class="button" type="reset">Reset</button></div></form></section>`; }
function secondaryView(id) {
 const data = {ai:['AI workspace','Your models and connections, composed in one place.',modules.filter(m=>m.kind==='Intelligence')],knowledge:['Knowledge collections','Retrieval infrastructure for context that stays connected.',modules.filter(m=>m.name==='Knowledge')],observability:['A clear signal.','Inspect the runtime services behind your application.',modules.filter(m=>m.name==='Observability'||m.name==='Event bus')],jobs:['Work, on schedule.','Background execution with visible, dependable outcomes.',modules.filter(m=>m.name==='Job scheduler')]}[id];
 return heading(data[0],data[1])+`<section class="panel"><div class="panel-head"><h2>Connected services</h2><span class="status"><i></i>Ready</span></div><div class="module-list">${moduleTable(data[2])}</div></section>`;
}
function renderPage(id) {
 currentPage = pages.some(p=>p.id===id)?id:'overview';
 $('#content').innerHTML=currentPage==='overview'?overview():currentPage==='modules'?moduleView():currentPage==='configuration'?configView():secondaryView(currentPage);
 if(currentPage==='modules') { $('#module-search').value=moduleSearch; updateModules(); $('#module-search').oninput=e=>{moduleSearch=e.target.value;updateModules();}; }
 if($('#chart-range')) $('#chart-range').onchange=e=>$('.chart-wrap').innerHTML=chart(e.target.value);
 if($('#refresh')) $('#refresh').onclick=()=>toast('Workspace refreshed · All connections are healthy.');
 if($('#preferences')) {
  const formElement=$('#preferences');
  function restorePreferences(){formElement.elements.workspaceName.value=previewPreferences.name;formElement.elements.environment.value=previewPreferences.environment;$('input[type=checkbox]',formElement).checked=previewPreferences.showStatus;}
  restorePreferences();
  formElement.onreset=()=>setTimeout(restorePreferences,0);
  formElement.onsubmit=e=>{e.preventDefault();const form=new FormData(e.target);previewPreferences.name=form.get('workspaceName');previewPreferences.environment=form.get('environment');previewPreferences.showStatus=$('input[type=checkbox]',e.target).checked;$('.env').innerHTML='<i></i>';$('.env').append(document.createTextNode(previewPreferences.environment));$('.sn-readiness').hidden=!previewPreferences.showStatus;toast('Preview preferences saved.');};
 }
 $('.stage').scrollTop=0;
}
let toastTimer;
function toast(message){$('.toast').textContent=message;$('.toast').hidden=false;clearTimeout(toastTimer);toastTimer=setTimeout(()=>$('.toast').hidden=true,3500);}
function showModule(name){const m=modules.find(m=>m.name===name);if(!m)return;$('#detail-content').innerHTML=`<span class="module-icon">${icon(m.icon)}</span><h2>${m.name}</h2><p>${m.detail}</p><dl><dt>Package</dt><dd>${m.namespace}</dd><dt>Provider</dt><dd>${m.provider}</dd><dt>Readiness</dt><dd>${m.status==='Review'?'Needs review':'Healthy'}</dd><dt>Runtime</dt><dd>${m.metric}</dd></dl>`;$('#detail-dialog').showModal();}
function searchResults(query=''){const registered=window.shellNavigation?.getPages()||pages;const found=registered.filter(p=>(p.label+' '+(p.categoryLabel||'')+' '+(p.groupLabel||'')).toLowerCase().includes(query.toLowerCase()));$('.command-results').innerHTML=found.length?found.map(p=>`<button data-command="${p.id}">${icon(p.icon||'modules')}<span>${p.label}</span><span>${p.categoryLabel||'Workspace'}</span></button>`).join(''):'<div class="empty">No matching pages.</div>';}
function openSearch(){$('#command-input').value='';searchResults();$('#search-dialog').showModal();$('#command-input').focus();}
$$('.search-launch').forEach(b=>b.onclick=openSearch);
$('#command-input').oninput=e=>searchResults(e.target.value);
$('#command-input').onkeydown=e=>{if(e.key==='Enter')$('.command-results button')?.click();};
$('#notifications').onclick=()=>{location.hash='observability';};
$('#account').onclick=()=>{ $('#detail-content').innerHTML='<div class="avatar">MO</div><h2>Monica developer</h2><p>Your local development workspace.</p><dl><dt>Environment</dt><dd>Development</dd><dt>Access</dt><dd>Administrator</dd></dl>';$('#detail-dialog').showModal();};
document.addEventListener('click',e=>{const target=e.target.closest('button');if(!target)return;if(target.matches('[data-module]'))showModule(target.dataset.module);if(target.matches('[data-filter]')){moduleFilter=target.dataset.filter;updateModules();}if(target.matches('[data-close]'))target.closest('dialog').close();if(target.matches('[data-command]')){$('#search-dialog').close();location.hash=target.dataset.command;}});
document.addEventListener('keydown',e=>{if((e.ctrlKey||e.metaKey)&&e.key.toLowerCase()==='k'){e.preventDefault();if(!$$('dialog[open]').length)openSearch();}});
$$('dialog').forEach(d=>d.addEventListener('click',e=>{if(e.target===d){const r=d.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)d.close();}}));
hydrateIcons();
