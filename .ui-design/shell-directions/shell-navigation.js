(function () {
  'use strict';
  const symbols = {
    home:'<rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>',
    ai:'<path d="m12 3 2.5 6.5L21 12l-6.5 2.5L12 21l-2.5-6.5L3 12l6.5-2.5Z"/>',
    modules:'<path d="m12 3 9 5-9 5-9-5ZM3 8v9l9 5 9-5V8M12 13v9"/>',
    pulse:'<path d="M3 12h4l3-8 4 16 3-8h4"/>',
    book:'<path d="M12 5v15M3 4c4-1 6 0 9 2 3-2 5-3 9-2v14c-4-1-6 0-9 2-3-2-5-3-9-2Z"/>',
    settings:'<path d="M4 7h16M4 17h16"/><circle cx="8" cy="7" r="3"/><circle cx="16" cy="17" r="3"/>',
    chevron:'<path d="m9 5 7 7-7 7"/>',
    close:'<path d="m6 6 12 12M18 6 6 18"/>',
    side:'<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M9 4v16"/>',
    top:'<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M3 9h18"/>',
    collapse:'<path d="M9 4v16M20 4H4v16h16ZM16 8l-4 4 4 4"/>',
    expand:'<path d="M9 4v16M20 4H4v16h16ZM12 8l4 4-4 4"/>',
    sun:'<circle cx="12" cy="12" r="4"/><path d="M12 2v2m0 16v2M2 12h2m16 0h2M5 5l1.5 1.5m11 11L19 19M5 19l1.5-1.5m11-11L19 5"/>',
    moon:'<path d="M20 14A8 8 0 0 1 10 4 8 8 0 1 0 20 14Z"/>'
  };
  const icon = name => `<svg class="sn-icon" viewBox="0 0 24 24" aria-hidden="true">${symbols[name] || symbols.modules}</svg>`;
  const leaf = (id, base, label, description) => ({id,base,label,description});
  const navigation = [
    {id:'workspace',label:'Workspace',icon:'home',items:[leaf('workspace-overview','overview','Workspace overview','A clear view of your composed application.')]},
    {id:'intelligence',label:'Intelligence',icon:'ai',items:[
      {id:'ai-workspace',label:'AI workspace',children:[leaf('ai-chat','ai','Chat workspace','Explore the conversations and models behind your application.'),leaf('ai-providers','ai','Providers & models','Inspect provider connections and model capabilities.')]},
      {id:'knowledge',label:'Knowledge & retrieval',children:[leaf('knowledge-collections','knowledge','Knowledge collections','Documents and collections available to your workspace.'),leaf('knowledge-index','knowledge','Index status','Follow indexing progress and retrieval readiness.')]}
    ]},
    {id:'platform',label:'Platform',icon:'modules',items:[
      {id:'module-system',label:'Module System',children:[leaf('modules-overview','overview','System overview','The modules, providers and runtime services working together.'),leaf('modules-registry','modules','Module registry','Discover and inspect your application’s registered modules.'),leaf('modules-dependencies','modules','Dependencies','Understand the connections between your framework modules.')]},
      {id:'configuration',label:'Configuration',children:[leaf('configuration-managed','configuration','Managed settings','Inspect managed options and workspace configuration.'),leaf('configuration-validation','configuration','Runtime validation','Review validation and configuration readiness.')]}
    ]},
    {id:'operations',label:'Operations',icon:'pulse',items:[
      {id:'observability',label:'Observability',children:[leaf('observability-health','observability','System health','Keep runtime availability and service readiness in view.'),leaf('observability-traces','observability','Traces & logs','Follow requests across your application’s services.')]},
      {id:'jobs',label:'Background jobs',children:[leaf('jobs-scheduled','jobs','Scheduled jobs','Inspect recurring jobs and upcoming execution.'),leaf('jobs-history','jobs','Execution history','Review completed work, timing and outcomes.')]}
    ]},
    {id:'resources',label:'Resources',icon:'book',items:[
      {id:'framework-guides',label:'Framework guidance',children:[leaf('resources-guides','knowledge','Guides & examples','Explore the guidance behind your infrastructure.'),leaf('resources-api','modules','Module reference','Inspect module contracts and their composed providers.')]}
    ]}
  ];
  const leaves = navigation.flatMap(category => category.items.flatMap(item => (item.children || [item]).map(page => ({...page,categoryId:category.id,categoryLabel:category.label,groupId:item.children?item.id:'',groupLabel:item.children?item.label:''}))));
  const baseDefaults = {overview:'workspace-overview',ai:'ai-providers',knowledge:'knowledge-collections',modules:'modules-registry',configuration:'configuration-managed',observability:'observability-health',jobs:'jobs-scheduled'};

  function mount(options) {
    const root = document.documentElement;
    const host = document.querySelector('[data-shell-navigation]');
    const topHost = document.querySelector('[data-shell-top]');
    if (!host || !topHost) throw new Error('Shell navigation requires side and top mounts.');
    const storageKey = `monica-shell-directions-v2-${options.id}`;
    let stored = {};
    try { stored = JSON.parse(localStorage.getItem(storageKey) || '{}'); } catch {}
    const query = new URLSearchParams(location.search);
    const state = {
      layout:['side','top'].includes(query.get('layout'))?query.get('layout'):stored.layout || options.layout || 'side',
      theme:['light','dark'].includes(query.get('theme'))?query.get('theme'):stored.theme || options.theme || 'light',
      collapsed:Boolean(stored.collapsed), expanded:new Set(stored.expanded || ['module-system','configuration']),
      current:leaves.find(l=>l.id===location.hash.slice(1)) || leaves.find(l=>l.id===baseDefaults[location.hash.slice(1)]) || leaves.find(l=>l.id==='modules-overview'),
      category:'platform', flyout:false, mobile:false, dropdown:null, trigger:null
    };
    state.category=state.current.categoryId;
    if(state.current.groupId) state.expanded.add(state.current.groupId);
    const chrome=document.querySelector('.study-bar,.prototype');
    function measureChrome(){root.style.setProperty('--sn-chrome-height',`${chrome?chrome.getBoundingClientRect().height:0}px`);}measureChrome();
    host.className='sn-shell-nav';
    host.innerHTML=`<div class="sn-global-rail"><a class="sn-mark" href="#workspace-overview" aria-label="Monica home">m<span></span></a><nav class="sn-category-rail" aria-label="Navigation categories"></nav><div class="sn-rail-bottom"><button class="sn-rail-control" data-sn-collapse aria-label="Collapse page navigation" title="Collapse page navigation"></button><button class="sn-rail-control" data-shell-settings aria-label="Appearance settings" title="Appearance settings">${icon('settings')}</button><span class="sn-avatar" title="Workspace developer">MO</span></div></div><section class="sn-context" aria-label="Category pages"><div class="sn-identity"><span class="sn-wordmark">monica<span>.</span></span><small>Modular infrastructure</small><button class="sn-close-nav" aria-label="Close navigation">${icon('close')}</button></div><div class="sn-context-heading"><span class="sn-overline">EXPLORE</span><h2></h2></div><nav class="sn-context-pages" aria-label="Pages in category"></nav><div class="sn-context-bottom"><span class="sn-readiness"><i></i>All systems operational</span><button class="sn-collapse-link" data-sn-collapse>${icon('collapse')}<span>Collapse pages</span></button></div></section>`;
    topHost.classList.add('sn-top-nav');
    topHost.setAttribute('aria-label','Navigation categories');
    const scrim=document.createElement('button');scrim.className='sn-scrim';scrim.hidden=true;scrim.setAttribute('aria-label','Close navigation');document.body.append(scrim);
    const popover=document.createElement('section');popover.className='sn-top-popover';popover.hidden=true;popover.setAttribute('aria-label','Category pages');document.body.append(popover);
    const settings=document.createElement('dialog');settings.className='sn-settings';settings.setAttribute('aria-labelledby','sn-settings-title');document.body.append(settings);
    let pendingAppearance=null;
    let hiddenCategories=[];
    let syncingHash=false;
    let inertNodes=[];
    let lastNavigationFocus=null;
    const isNavigationTarget=element=>host.contains(element)||topHost.contains(element)||popover.contains(element);
    const mobileQuery=matchMedia('(max-width:760px)');
    function persist(){try{localStorage.setItem(storageKey,JSON.stringify({layout:state.layout,theme:state.theme,collapsed:state.collapsed,expanded:[...state.expanded]}));}catch{}}
    function categoryButton(category,top=false){return `<button class="${top?'sn-top-category':'sn-category'} ${state.current.categoryId===category.id?'active':''}" data-sn-category="${category.id}" aria-label="${category.label}" ${top?'aria-haspopup="true" aria-expanded="false"':'title="'+category.label+'" aria-pressed="'+(state.category===category.id)+'"'}>${icon(category.icon)}<span>${category.label}</span>${top?icon('chevron'):''}</button>`;}
    function tree(category,scope){return category.items.map(item=>{
      if(!item.children)return pageLink(leaves.find(l=>l.id===item.id));
      const open=state.expanded.has(item.id);
      return `<div class="sn-branch"><button class="sn-disclosure" data-sn-branch="${item.id}" aria-expanded="${open}" aria-controls="${scope}-${item.id}"><span>${item.label}</span><span class="sn-branch-count">${item.children.length}</span>${icon('chevron')}</button><div class="sn-children" id="${scope}-${item.id}" ${open?'':'hidden'}>${item.children.map(p=>pageLink(leaves.find(l=>l.id===p.id))).join('')}</div></div>`;
    }).join('');}
    function pageLink(page){return `<a class="sn-page ${state.current.id===page.id?'active':''}" data-sn-leaf="${page.id}" href="#${page.id}" ${state.current.id===page.id?'aria-current="page"':''}><span class="sn-page-dot"></span><span>${page.label}</span>${state.current.id===page.id?'<span class="sn-current-mark"></span>':''}</a>`;}
    function renderContext(){const category=navigation.find(c=>c.id===state.category);host.querySelector('.sn-context-heading h2').textContent=category.label;host.querySelector('.sn-context-pages').innerHTML=tree(category,'sn-side');const rail=host.querySelector('.sn-category-rail');if(!rail.children.length)rail.innerHTML=navigation.map(c=>categoryButton(c)).join('');rail.querySelectorAll('[data-sn-category]').forEach(button=>{button.classList.toggle('active',button.dataset.snCategory===state.current.categoryId);button.setAttribute('aria-pressed',String(button.dataset.snCategory===state.category));});}
    function renderBreadcrumb(){document.querySelectorAll('[data-shell-breadcrumb]').forEach(e=>{e.innerHTML=`<span>${state.current.categoryLabel}</span>${state.current.groupLabel?'<span class="sn-breadcrumb-divider">/</span><span>'+state.current.groupLabel+'</span>':''}<span class="sn-breadcrumb-divider">/</span><b>${state.current.label}</b>`;});}
    function renderTop(){topHost.innerHTML=navigation.map(c=>categoryButton(c,true)).join('')+`<button class="sn-top-more" data-sn-more aria-label="More categories" aria-haspopup="true" aria-expanded="false" hidden>More ${icon('chevron')}</button>`;measureTop();}
    function measureTop(){
      if(state.layout!=='top'||mobileQuery.matches)return;
      const more=topHost.querySelector('[data-sn-more]');
      if(!more)return;
      const previousHidden=hiddenCategories.map(category=>category.id).join(',');
      const focusedBeforeMeasure=document.activeElement;
      const buttons=[...topHost.querySelectorAll('[data-sn-category]')];
      buttons.forEach(button=>button.hidden=false);
      hiddenCategories=[];
      const available=topHost.clientWidth;
      const gap=parseFloat(getComputedStyle(topHost).columnGap)||0;
      const widths=buttons.map(button=>button.getBoundingClientRect().width);
      let used=widths.reduce((total,width)=>total+width,0)+Math.max(0,buttons.length-1)*gap;
      if(used>available){
        more.hidden=false;
        const reserved=more.getBoundingClientRect().width+gap;
        for(let index=buttons.length-1;index>=0&&used+reserved>available;index--){
          buttons[index].hidden=true;
          used-=widths[index]+gap;
          hiddenCategories.unshift(navigation[index]);
        }
      }else more.hidden=true;
      more.classList.toggle('active',hiddenCategories.some(category=>category.id===state.current.categoryId));
      const membershipChanged=previousHidden!==hiddenCategories.map(category=>category.id).join(',');
      if(state.dropdown&&(state.trigger?.hidden||(state.dropdown==='more'&&membershipChanged))){
        const restore=popover.contains(focusedBeforeMeasure)||focusedBeforeMeasure===state.trigger;
        const current=buttons.find(button=>button.dataset.snCategory===state.current.categoryId&&!button.hidden);
        const target=!more.hidden?more:current||buttons.find(button=>!button.hidden);
        closeDropdown();
        if(restore)target?.focus();
      }
      positionPopover();
    }
    function closeDropdown(restore=false){if(!state.dropdown)return;const trigger=state.trigger;state.dropdown=null;popover.hidden=true;topHost.querySelectorAll('[aria-expanded]').forEach(b=>b.setAttribute('aria-expanded','false'));if(restore)trigger?.focus();}
    function positionPopover(){if(!state.trigger||popover.hidden)return;const rect=state.trigger.getBoundingClientRect();const width=Math.min(380,innerWidth-24);popover.style.width=`${width}px`;popover.style.left=`${Math.min(Math.max(12,rect.left),innerWidth-width-12)}px`;popover.style.top=`${rect.bottom+8}px`;popover.style.maxHeight=`${Math.max(180,innerHeight-rect.bottom-25)}px`;}
    function renderDropdown(){if(!state.dropdown)return;if(state.dropdown==='more'){popover.innerHTML=`<div class="sn-popover-heading"><span class="sn-overline">MORE CATEGORIES</span><h2>Your workspace</h2></div><div class="sn-more-tree">${hiddenCategories.map(c=>`<section><h3>${icon(c.icon)}${c.label}</h3>${tree(c,'sn-more')}</section>`).join('')}</div>`;}else{const category=navigation.find(c=>c.id===state.dropdown);popover.innerHTML=`<div class="sn-popover-heading"><span class="sn-overline">EXPLORE</span><h2>${category.label}</h2><p>${category.items.filter(item=>item.children).map(item=>item.label).join(' · ')||'Your application at a glance'}</p></div><nav class="sn-popover-pages" aria-label="${category.label} pages">${tree(category,'sn-top')}</nav>`;}positionPopover();}
    function openDropdown(id,trigger){if(state.dropdown===id){closeDropdown();return;}closeDropdown();state.dropdown=id;state.trigger=trigger;trigger.setAttribute('aria-expanded','true');popover.hidden=false;renderDropdown();}
    function setMainInert(active){if(active){inertNodes=[...document.querySelectorAll('.stage,.workspace,.workarea,.study-bar,.prototype')].filter(e=>!host.contains(e));inertNodes.forEach(e=>e.inert=true);}else{inertNodes.forEach(e=>e.inert=false);inertNodes=[];}}
    function closeSide(restore=false){const wasOpen=state.flyout||state.mobile;state.flyout=false;state.mobile=false;host.classList.remove('sn-flyout-open','sn-mobile-open');scrim.hidden=true;document.querySelectorAll('[data-shell-mobile]').forEach(b=>b.setAttribute('aria-expanded','false'));setMainInert(false);if(wasOpen&&restore)state.trigger?.focus();}
    function openSide(trigger){state.trigger=trigger;state.category=state.current.categoryId;renderContext();if(mobileQuery.matches){state.mobile=true;host.classList.add('sn-mobile-open');scrim.hidden=false;setMainInert(true);document.querySelectorAll('[data-shell-mobile]').forEach(b=>b.setAttribute('aria-expanded','true'));host.querySelector('.sn-close-nav').focus();}else{state.flyout=true;host.classList.add('sn-flyout-open');}}
    function applyAppearance(){root.dataset.layout=state.layout;root.dataset.theme=state.theme;root.dataset.contextCollapsed=String(state.collapsed);root.style.setProperty('--sn-sidebar-width',mobileQuery.matches||state.layout==='top'?'0px':state.collapsed?'64px':'290px');host.querySelector('[data-sn-collapse]').innerHTML=icon(state.collapsed?'expand':'collapse');host.querySelector('.sn-collapse-link span').textContent=state.collapsed?'Keep pages open':'Collapse pages';host.querySelector('.sn-collapse-link .sn-icon').outerHTML=icon(state.collapsed?'expand':'collapse');host.querySelectorAll('[data-sn-collapse]').forEach(b=>{b.setAttribute('aria-label',state.collapsed?'Expand page navigation':'Collapse page navigation');b.title=state.collapsed?'Expand page navigation':'Collapse page navigation';});persist();options.onAppearance?.({layout:state.layout,theme:state.theme,collapsed:state.collapsed});requestAnimationFrame(measureTop);}
    function collapse(){state.collapsed=!state.collapsed;closeSide();applyAppearance();host.querySelector('.sn-rail-bottom [data-sn-collapse]').focus();}
    function navigate(id){const page=leaves.find(p=>p.id===id);if(!page)return;const restoreMobile=state.mobile,restoreFlyout=state.flyout,restoreTop=state.dropdown;closeDropdown();closeSide();state.current=page;state.category=page.categoryId;if(page.groupId)state.expanded.add(page.groupId);renderContext();renderBreadcrumb();renderTop();persist();options.onNavigate(page);if(restoreMobile)document.querySelector('[data-shell-mobile]')?.focus();else if(restoreFlyout)host.querySelector(`[data-sn-category="${page.categoryId}"]`)?.focus();else if(restoreTop){const active=topHost.querySelector(`[data-sn-category="${page.categoryId}"]`);(active&&!active.hidden?active:topHost.querySelector('[data-sn-more]'))?.focus();}if(location.hash!==`#${id}`){syncingHash=true;history.replaceState(null,'',location.pathname+location.search+'#'+id);syncingHash=false;}}
    function navigateBase(base){navigate(baseDefaults[base]||base);}
    function renderSettings(){settings.innerHTML=`<div class="sn-settings-header"><span class="sn-overline">MAKE YOURSELF AT HOME</span><h2 id="sn-settings-title">Appearance</h2><p>Choose how your workspace looks and feels.</p><button class="sn-icon-button" data-sn-close-settings aria-label="Close appearance settings">${icon('close')}</button></div><div class="sn-settings-body"><fieldset><legend>Navigation layout</legend><p>The same categories and pages, in a different place.</p><div class="sn-layout-choices">${['side','top'].map(layout=>`<label class="sn-layout-choice"><input type="radio" name="sn-layout" value="${layout}" ${pendingAppearance.layout===layout?'checked':''}><span class="sn-layout-preview sn-layout-preview-${layout}"><i></i><b></b><em></em></span><strong>${layout==='side'?'Sidebar':'Top navigation'}</strong><small>${layout==='side'?'A category rail and contextual pages':'Grouped menus above your workspace'}</small></label>`).join('')}</div></fieldset><fieldset><legend>Color mode</legend><div class="sn-color-choices">${['light','dark'].map(theme=>`<label><input type="radio" name="sn-theme" value="${theme}" ${pendingAppearance.theme===theme?'checked':''}>${icon(theme==='light'?'sun':'moon')}<span>${theme==='light'?'Light':'Dark'}</span></label>`).join('')}</div></fieldset><div class="sn-settings-hint">Sidebar pages can collapse to give your content more room. Your current page and filters stay in place when changing layouts.</div></div><div class="sn-settings-footer"><button class="sn-settings-cancel" data-sn-close-settings>Cancel</button><button class="sn-settings-apply" data-sn-apply>Apply preferences</button></div>`;}
    function openSettings(trigger){closeDropdown();closeSide();pendingAppearance={layout:state.layout,theme:state.theme};state.trigger=trigger;renderSettings();settings.showModal();}
    function onClick(e){const action=e.target.closest('[data-sn-category],[data-sn-leaf],[data-sn-branch],[data-sn-collapse],[data-sn-more],[data-shell-settings],[data-shell-mobile],[data-sn-close-settings],[data-sn-apply],.sn-close-nav');if(!action){if(state.dropdown&&!popover.contains(e.target)&&!topHost.contains(e.target))closeDropdown();if(state.flyout&&!host.contains(e.target))closeSide();return;}
      if(action.matches('[data-sn-category]')){const id=action.dataset.snCategory;if(topHost.contains(action)){openDropdown(id,action);}else{state.category=id;renderContext();const fresh=host.querySelector(`[data-sn-category="${id}"]`);fresh.focus();if(state.collapsed&&!mobileQuery.matches){state.flyout=true;state.trigger=fresh;host.classList.add('sn-flyout-open');}}}
      else if(action.matches('[data-sn-leaf]')){e.preventDefault();navigate(action.dataset.snLeaf);}
      else if(action.matches('[data-sn-branch]')){const branch=action.dataset.snBranch;state.expanded.has(branch)?state.expanded.delete(branch):state.expanded.add(branch);const keepId=branch;renderContext();renderDropdown();persist();(popover.hidden?host:popover).querySelector(`[data-sn-branch="${keepId}"]`)?.focus();}
      else if(action.matches('[data-sn-collapse]'))collapse();
      else if(action.matches('[data-sn-more]'))openDropdown('more',action);
      else if(action.matches('[data-shell-settings]'))openSettings(action);
      else if(action.matches('[data-shell-mobile]'))state.mobile?closeSide(true):openSide(action);
      else if(action.matches('.sn-close-nav'))closeSide(true);
      else if(action.matches('[data-sn-close-settings]'))settings.close();
      else if(action.matches('[data-sn-apply]')){state.layout=pendingAppearance.layout;state.theme=pendingAppearance.theme;closeDropdown();closeSide();const address=new URL(location.href);address.searchParams.delete('layout');address.searchParams.delete('theme');history.replaceState(null,'',address.pathname+address.search+address.hash);applyAppearance();renderTop();settings.close();}
    }
    document.addEventListener('click',onClick);
    document.addEventListener('focusin',event=>{lastNavigationFocus=isNavigationTarget(event.target)?event.target:null;});
    document.addEventListener('pointerdown',event=>{if(!isNavigationTarget(event.target))lastNavigationFocus=null;});
    settings.addEventListener('change',e=>{if(e.target.name==='sn-layout')pendingAppearance.layout=e.target.value;if(e.target.name==='sn-theme')pendingAppearance.theme=e.target.value;});
    settings.addEventListener('click',e=>{if(e.target===settings){const r=settings.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)settings.close();}});
    settings.addEventListener('close',()=>{const target=state.trigger?.isConnected&&state.trigger.getClientRects().length?state.trigger:document.querySelector('.appbar [data-shell-settings],.topbar [data-shell-settings],.shell-header [data-shell-settings]');target?.focus();});
    scrim.onclick=()=>closeSide(true);
    document.addEventListener('keydown',e=>{
      if(e.key==='Escape'){if(state.dropdown){e.preventDefault();closeDropdown(true);}if(state.mobile||state.flyout){e.preventDefault();closeSide(true);}}
      const category=e.target.closest('.sn-top-category,.sn-top-more');if(category&&e.key==='ArrowDown'){e.preventDefault();const id=category.dataset.snCategory||'more';if(state.dropdown!==id)openDropdown(id,category);popover.querySelector('a,button')?.focus();}
      if(e.key==='Tab'&&state.mobile){const focusable=[...host.querySelectorAll('a,button')].filter(el=>el.getClientRects().length&&getComputedStyle(el).visibility!=='hidden');const first=focusable[0],last=focusable.at(-1);if(e.shiftKey&&document.activeElement===first){e.preventDefault();last.focus();}else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first.focus();}}
    });
    window.addEventListener('hashchange',()=>{if(syncingHash)return;const id=location.hash.slice(1);if(leaves.some(l=>l.id===id)){if(state.current.id!==id)navigate(id);}else if(baseDefaults[id])navigateBase(id);});
    mobileQuery.addEventListener('change',()=>{
      const focusedNavigation=isNavigationTarget(document.activeElement)||(document.activeElement===document.body&&lastNavigationFocus);
      closeSide();closeDropdown();applyAppearance();
      if(focusedNavigation)requestAnimationFrame(()=>{
        if(mobileQuery.matches)document.querySelector('[data-shell-mobile]')?.focus();
        else if(state.layout==='top'){
          const current=topHost.querySelector(`[data-sn-category="${state.current.categoryId}"]`);
          (current&&!current.hidden?current:topHost.querySelector('[data-sn-more]'))?.focus();
        }else (state.collapsed?host.querySelector(`[data-sn-category="${state.current.categoryId}"]`):host.querySelector('[aria-current="page"]'))?.focus();
      });
    });
    window.addEventListener('resize',()=>{measureChrome();measureTop();positionPopover();});
    const observer=new ResizeObserver(measureTop);observer.observe(topHost);document.fonts.ready.then(measureTop);
    host.querySelector('.sn-mark').addEventListener('click',e=>{e.preventDefault();navigateBase('overview');});
    renderContext();applyAppearance();renderTop();renderBreadcrumb();navigate(state.current.id);
    const controller={navigateBase,navigate,openSettings:()=>openSettings(document.querySelector('[data-shell-settings]')),getState:()=>({layout:state.layout,theme:state.theme,collapsed:state.collapsed,current:state.current.id}),getPages:()=>leaves.map(l=>({...l}))};
    return controller;
  }
  window.MonicaShellNavigation={mount};
})();
