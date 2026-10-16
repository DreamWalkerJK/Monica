/* Static design prototype: all reads and mutations stay in in-memory fixtures. */
(function () {
  "use strict";
  const $ = function (selector, root) { return (root || document).querySelector(selector); };
  const $$ = function (selector, root) { return Array.from((root || document).querySelectorAll(selector)); };
  const escape = function (value) {
    return String(value == null ? "" : value).replace(/[&<>"']/g, function (c) {
      return {"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c];
    });
  };
  const paths = {
    database:'<ellipse cx="12" cy="5" rx="8" ry="3"/><path d="M4 5v14c0 4 16 4 16 0V5M4 12c0 4 16 4 16 0"/>',
    memory:'<rect x="5" y="5" width="14" height="14" rx="3"/><path d="M9 1v4m6-4v4M9 19v4m6-4v4M1 9h4m-4 6h4m14-6h4m-4 6h4"/><rect x="9" y="9" width="6" height="6" rx="1"/>',
    layers:'<path d="m12 3 10 5-10 5L2 8l10-5Zm-10 9 10 5 10-5M2 16l10 5 10-5"/>',
    search:'<circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 5 5"/>',
    plus:'<path d="M12 5v14M5 12h14"/>',
    arrow:'<path d="M4 12h16m-6-6 6 6-6 6"/>',
    back:'<path d="M20 12H4m6-6-6 6 6 6"/>',
    chevron:'<path d="m9 5 7 7-7 7"/>',
    down:'<path d="m6 9 6 6 6-6"/>',
    refresh:'<path d="M20 10a8 8 0 1 0-2 8M20 4v6h-6"/>',
    list:'<path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01"/>',
    tree:'<rect x="2" y="3" width="6" height="5" rx="1"/><rect x="14" y="10" width="7" height="5" rx="1"/><rect x="14" y="18" width="7" height="4" rx="1"/><path d="M5 8v12h9M5 12h9"/>',
    key:'<circle cx="8" cy="8" r="5"/><path d="m12 12 9 9m-5-5 3-3m0 6 3-3"/>',
    folder:'<path d="M3 19V5a2 2 0 0 1 2-2h5l2 3h7a2 2 0 0 1 2 2v11H3Z"/>',
    check:'<path d="m5 12 4 4L19 6"/>',
    close:'<path d="m6 6 12 12M6 18 18 6"/>',
    minus:'<path d="M5 12h14"/>',
    info:'<circle cx="12" cy="12" r="9"/><path d="M12 11v6m0-10h.01"/>',
    copy:'<rect x="8" y="8" width="12" height="13" rx="2"/><path d="M16 8V5a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h3"/>',
    edit:'<path d="m14 5 5 5M4 20l5-1L21 7a2 2 0 0 0-5-5L4 14v6Z"/>',
    trash:'<path d="M3 6h18M9 6V3h6v3M5 6l1 15h12l1-15M10 10v7m4-7v7"/>',
    expand:'<path d="M8 3H3v5m13-5h5v5M3 16v5h5m13-5v5h-5"/>',
    collapse:'<path d="M3 8h5V3m8 0v5h5M8 21v-5H3m18 0h-5v5"/>',
    sun:'<circle cx="12" cy="12" r="4"/><path d="M12 2v2m0 16v2M2 12h2m16 0h2M5 5l1.5 1.5m11 11L19 19M5 19l1.5-1.5m11-11L19 5"/>',
    moon:'<path d="M20.5 14A8.5 8.5 0 0 1 10 3.5 8.5 8.5 0 1 0 20.5 14Z"/>',
    code:'<path d="m8 7-5 5 5 5m8-10 5 5-5 5m-3-14-2 18"/>',
    book:'<path d="M12 5v16M3 3c4-1 6 0 9 2 3-2 5-3 9-2v16c-4-1-6 0-9 2-3-2-5-3-9-2V3Z"/>'
  };
  function icon(name, extra) {
    return '<svg class="icon '+(extra || "")+'" viewBox="0 0 24 24" aria-hidden="true">'+(paths[name] || paths.database)+'</svg>';
  }
  function iconButton(name, label, action, extra) {
    return '<button class="icon-button '+(extra || "")+'" type="button" data-action="'+action+'" aria-label="'+label+'" title="'+label+'">'+icon(name)+'</button>';
  }
  function symbol(provider) { return '<span class="provider-symbol '+provider.tone+'">'+icon(provider.icon)+'</span>'; }
  function badge(text, tone) { return '<span class="badge '+(tone || "")+'">'+escape(text)+'</span>'; }
  function parseJson(raw) { try { return {valid:true,value:JSON.parse(raw)}; } catch { return {valid:false,value:raw}; } }
  function formatTtl(value) {
    if (value === undefined) return "不支持读取";
    if (value === null) return "永不过期";
    if (value >= 3600) return Math.floor(value/3600)+"h "+Math.floor(value%3600/60)+"m";
    if (value >= 60) return Math.floor(value/60)+"m "+Math.floor(value%60)+"s";
    return value+"s";
  }
  function highlight(raw) {
    const tokens = /"(?:\\.|[^"\\])*"(?:\s*:)?|\b(?:true|false|null)\b|-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b/g;
    let html = "", cursor = 0, match;
    while ((match = tokens.exec(raw)) !== null) {
      html += escape(raw.slice(cursor,match.index));
      let kind = match[0][0] === '"' ? (match[0].trimEnd().endsWith(":") ? "key" : "string") : /true|false|null/.test(match[0]) ? "literal" : "number";
      html += '<span class="code-'+kind+'">'+escape(match[0])+'</span>';
      cursor = match.index + match[0].length;
    }
    return html + escape(raw.slice(cursor));
  }
  const fixtures = window.storeFixtures;
  const state = {
    route:"browser",provider:"redis",providerDetail:"redis",single:false,
    sessions:{},providerFilter:"",expanded:false,menu:false,modalKind:null,
    editor:null,toastTimer:null,deleteKeys:[],split:43
  };
  function providers() { return state.single ? fixtures.providers.filter(function(p){return p.id==="memory";}) : fixtures.providers; }
  function provider() { return providers().find(function(p){return p.id===state.provider;}) || providers()[0]; }
  function session() {
    if (!state.sessions[state.provider]) {
      const p = provider(), items = fixtures.keys[p.id];
      state.sessions[p.id] = {
        draft:p.scan?"*":"",mode:p.scan?"scan":"exact",accepted:p.scan?"*":null,acceptedMode:p.scan?"scan":"exact",
        items:p.scan?items.slice():[],total:p.scan?items.length:0,selected:p.scan?items[0].key:null,
        checked:new Set(),visited:new Set(p.scan?[items[0].key]:[]),
        view:"list",valueView:"formatted",detailOpen:false,time:"示例快照"
      };
    }
    return state.sessions[state.provider];
  }
  function selectedKey() { const s=session(); return s.items.find(function(k){return k.key===s.selected;}); }
  function routeHref(id) { return "#/browser/"+id; }
  function heading(title, subtitle, action) {
    return '<header class="page-heading"><div class="heading-copy"><div class="eyebrow">STATE STORE</div><h1>'+title+'</h1><p>'+subtitle+'</p></div><div class="heading-actions">'+action+'</div></header>'+
      '<nav class="route-nav" aria-label="状态存储页面"><a href="#/providers" '+(state.route==="providers"?'aria-current="page"':"")+'>'+icon("database")+'存储提供者 <span class="nav-count">'+providers().length+'</span></a><a href="'+routeHref(state.provider)+'" '+(state.route==="browser"?'aria-current="page"':"")+'>'+icon("code")+'数据浏览器</a></nav>';
  }
  function render() {
    const app = $("#app");
    app.dataset.page=state.route;
    if (state.route==="providers") renderProviders();
    else renderBrowser();
    document.title = (state.route==="providers"?"存储提供者":"数据浏览器")+" · Monica 交互原型";
    app.scrollTop=0;
  }
  function renderBrowser() {
    const p=provider(),s=session();
    $("#app").innerHTML = heading("数据浏览器","定位一个 Key，查看它的值与生命周期。",
      '<button class="button primary" data-action="create">'+icon("plus")+'新建 Key</button>')+
      '<div class="browser-command"><div class="scope-line"><div class="provider-picker">'+
      (providers().length>1?'<button class="provider-trigger" data-action="provider-menu" aria-expanded="false" aria-haspopup="menu">':'<div class="provider-trigger">')+
      symbol(p)+'<span class="provider-caption"><strong>'+escape(p.name)+'</strong><small>'+p.type+' · '+p.scope+'</small></span>'+
      (providers().length>1?icon("down")+'</button>':'</div>')+
      '<div class="provider-menu" role="menu" aria-label="选择存储提供者" hidden><p class="menu-caption">切换数据上下文</p>'+
      providers().map(function(item){return '<a role="menuitem" href="'+routeHref(item.id)+'" class="'+(item.id===p.id?"selected":"")+'">'+symbol(item)+'<span class="provider-caption"><strong>'+escape(item.name)+'</strong><small>'+item.type+' · '+item.scope+'</small></span>'+(item.id===p.id?icon("check"):"")+'</a>';}).join("")+'</div></div>'+
      '<a class="icon-button provider-info" href="#/providers/'+p.id+'" aria-label="提供者详情" title="提供者详情">'+icon("info")+'</a></div>'+
      '<form class="query-bar" id="query-form"><div class="query-mode" role="group" aria-label="查询方式"><button type="button" data-mode="scan" aria-pressed="'+(s.mode==="scan")+'" '+(!p.scan?'disabled title="此提供者不支持模式扫描"':"")+'>模式扫描</button><button type="button" data-mode="exact" aria-pressed="'+(s.mode==="exact")+'">精确查询</button></div>'+
      '<label class="query-input">'+icon("search")+'<input id="query" aria-label="Key 或匹配模式" spellcheck="false" autocomplete="off" placeholder="'+(p.scan?"搜索 Key 或模式，例如 Flight:*":"输入完整 Key")+'" value="'+escape(s.draft)+'"><span class="shortcut">/</span></label>'+
      '<button type="submit" class="button tonal" id="query-submit">'+icon("search")+'<span>'+(s.mode==="scan"?"扫描":"查找")+'</span></button></form></div>'+
      '<div class="query-meta" id="query-meta"></div>'+
      '<div class="workspace '+(s.detailOpen?"detail-open ":"")+(state.expanded?"is-expanded":"")+'" style="--list-width:'+state.split+'%"><section class="inventory" aria-label="Key 查询结果"></section><div class="resizer" role="separator" aria-label="调整列表宽度" aria-orientation="vertical" aria-valuemin="30" aria-valuemax="65" aria-valuenow="'+state.split+'" tabindex="0"></div><section class="inspector" aria-label="Key 详情"></section></div>'+
      '<footer class="workbench-footnote"><span>'+icon("info")+'当前为示例数据，操作仅作用于本次预览。</span><span>列表与分组均基于本次查询结果</span></footer>';
    renderQueryMeta();
    renderInventory();
    renderInspector();
    setupResizer();
  }
  function renderQueryMeta() {
    const s=session(),p=provider(),dirty=s.accepted!==s.draft || s.acceptedMode!==s.mode;
    $("#query-meta").innerHTML='<span>'+(s.accepted===null
      ? (p.scan?"输入条件后开始查询。":"此提供者支持精确查询，不提供 Key 扫描。")
      : '当前结果：<code>'+escape(s.accepted)+'</code> · '+(s.acceptedMode==="scan"?"模式扫描":"精确查询")+' · <strong>'+s.total+'</strong> 个匹配')+
      (dirty && s.accepted!==null?' <span class="draft-hint">· 条件已修改，执行后更新</span>':"")+'</span><span class="query-time">'+s.time+'</span>';
  }
  function keyRow(k) {
    const s=session(),parts=k.key.split(":"),prefix=parts.length>2?parts.slice(0,2).join(":"):parts.slice(0,-1).join(":"),leaf=parts.length>2?parts.slice(2).join(":"):parts[parts.length-1];
    return '<div class="key-row '+(k.key===s.selected?"selected":"")+'"><input type="checkbox" data-check="'+escape(k.key)+'" aria-label="选择 '+escape(k.key)+'" '+(s.checked.has(k.key)?"checked":"")+'>'+
      '<button class="key-select" data-key="'+escape(k.key)+'" aria-pressed="'+(k.key===s.selected)+'" title="'+escape(k.key)+'"><span class="key-name"><span class="key-prefix">'+escape(prefix || "Key")+'</span><span class="key-leaf">'+escape(leaf)+'</span></span>'+
      (s.visited.has(k.key)?'<span class="visited-mark" title="已读取 Value"></span>':"")+icon("chevron")+'</button></div>';
  }
  function renderInventory() {
    const s=session(),target=$(".inventory");
    if (!target) return;
    const previous=$(".key-list"),scroll=previous?previous.scrollTop:0;
    const expanded=previous?$$(".tree-group[open]").map(function(d){return d.dataset.group;}):[];
    let content="";
    if (s.accepted===null) {
      content='<div class="empty">'+icon("search")+'<h3>从一个 Key 开始</h3><p>输入完整的 Key，查看其值、ETag 与可用的过期信息。</p>'+
        (provider().id==="dapr"?'<button class="button tonal example" data-action="dapr-example">查询 Workflow:Instance:demo-001</button>':"")+'</div>';
    } else if (s.items.length===0) {
      content='<div class="empty">'+icon("search")+'<h3>'+(s.acceptedMode==="exact"?"Key 不存在":"没有匹配的 Key")+'</h3><p>'+escape(s.accepted)+'</p><p>检查拼写，或调整查询条件后重试。</p>'+(provider().scan?'<button class="button" data-action="reset-query">查看全部示例 Key</button>':"")+'</div>';
    } else if(s.view==="tree") {
      const groups={};
      s.items.forEach(function(k){const parts=k.key.split(":");const name=parts.slice(0,Math.min(parts.length-1,2)).join(":")||"未分组";(groups[name]||(groups[name]=[])).push(k);});
      content='<div class="tree-note">以 <code>:</code> 分组 · 数量仅代表当前结果</div>'+Object.entries(groups).map(function(entry,index){
        const name=entry[0],items=entry[1],open=expanded.length?expanded.includes(name):index===0;
        return '<details class="tree-group" data-group="'+escape(name)+'" '+(open?"open":"")+'><summary>'+icon("chevron","chevron")+icon("folder","folder")+'<code>'+escape(name)+'</code><span class="count">'+items.length+'</span></summary>'+items.map(keyRow).join("")+'</details>';
      }).join("");
    } else content=s.items.map(keyRow).join("");
    target.innerHTML='<div class="inventory-toolbar"><strong>Keys</strong><span class="result-count">'+s.items.length+'</span><div class="view-toggle" role="group" aria-label="结果视图"><button data-view="list" aria-label="列表视图" title="列表视图" aria-pressed="'+(s.view==="list")+'">'+icon("list")+'</button><button data-view="tree" aria-label="树形视图" title="树形视图" aria-pressed="'+(s.view==="tree")+'">'+icon("tree")+'</button></div></div>'+
      (s.checked.size?'<div class="bulk-toolbar"><span>已选择 '+s.checked.size+' 个 Key</span><button class="button subtle" data-action="bulk-delete">'+icon("trash")+'删除所选</button></div>':"")+
      (s.items.length?'<div class="list-column-header"><span><input type="checkbox" id="check-all" aria-label="选择当前全部结果" '+(s.checked.size===s.items.length?"checked":"")+'>Key 名称</span><span>'+(s.view==="tree"?"命名空间":"点击查看值")+'</span></div>':"")+
      '<div class="key-list">'+content+'</div><div class="inventory-footer"><span>'+(s.accepted===null?"等待查询":s.items.length+" 个已展示 / "+s.total+" 个匹配")+'</span><span>'+(s.visited.size&&s.items.length?"蓝点 · 已读取":"")+'</span></div>';
    $(".key-list").scrollTop=scroll;
    if ($("#check-all")) $("#check-all").indeterminate=s.checked.size>0&&s.checked.size<s.items.length;
  }
  function renderInspector() {
    const k=selectedKey(),s=session(),target=$(".inspector");
    if(!target)return;
    if(!k){target.innerHTML='<div class="empty">'+icon("key")+'<h3>选择一个 Key</h3><p>在左侧选择查询结果，值与元数据会在这里展开。</p></div>';return;}
    const parsed=parseJson(k.raw),parts=k.key.split(":"),prefix=parts.length>2?parts.slice(0,2).join(":"):parts.slice(0,-1).join(":"),leaf=parts.length>2?parts.slice(2).join(":"):parts[parts.length-1];
    target.innerHTML='<header class="inspector-masthead"><div class="inspector-topline"><button class="mobile-back" data-action="back-list">'+icon("back")+'返回 Keys</button><span class="mobile-provider" title="'+escape(provider().name)+'">'+escape(provider().name)+'</span><span class="desktop-label">KEY INSPECTOR</span>'+badge(parsed.valid?"JSON":"TEXT",parsed.valid?"primary":"info")+
      iconButton("copy","复制完整 Key","copy-key")+iconButton(state.expanded?"collapse":"expand",state.expanded?"收起详情":"展开详情","expand")+
      '</div><div class="inspector-key"><span class="key-glyph">'+icon("key")+'</span><div class="full-key"><p>'+escape(prefix)+'</p><h2 tabindex="-1" id="detail-heading">'+escape(leaf)+'</h2></div></div></header>'+
      '<div class="detail-scroll"><dl class="key-facts"><div><dt>TTL · 读取时快照</dt><dd class="ttl">'+formatTtl(k.ttl)+'</dd></div><div><dt>ETag</dt><dd title="'+escape(k.etag||"未提供")+'">'+escape(k.etag||"未提供")+'</dd></div><div><dt>值的文本大小</dt><dd>'+new TextEncoder().encode(k.raw).length+' B</dd></div></dl>'+
      '<div class="value-toolbar"><strong>Value</strong><div class="value-tools"><div class="value-switch" role="group" aria-label="值的显示方式"><button data-value-view="formatted" aria-pressed="'+(s.valueView==="formatted")+'">格式化</button><button data-value-view="fields" '+(!parsed.valid || typeof parsed.value!=="object" || parsed.value===null?"disabled":"")+' aria-pressed="'+(s.valueView==="fields")+'">字段</button><button data-value-view="raw" aria-pressed="'+(s.valueView==="raw")+'">原文</button></div>'+iconButton("copy","复制 Value","copy-value")+'</div></div><div id="value-content"></div></div>'+
      '<footer class="inspector-footer"><span>已读取 · '+escape(provider().type)+'</span><div><button class="button" data-action="refresh-key">'+icon("refresh")+'刷新</button><button class="button tonal" data-action="edit">'+icon("edit")+'编辑值</button>'+iconButton("trash","删除当前 Key","delete","danger")+'</div></footer>';
    renderValue();
  }
  function renderValue() {
    const k=selectedKey(),s=session(),parsed=parseJson(k.raw);
    if(s.valueView==="fields" && parsed.valid && parsed.value!==null && typeof parsed.value==="object"){
      $("#value-content").innerHTML='<div class="json-fields">'+Object.entries(parsed.value).map(function(entry){
        const v=entry[1],isObject=v!==null&&typeof v==="object";
        return '<div class="json-field"><span>'+escape(entry[0])+'</span><span>'+(isObject?'<details><summary>'+(Array.isArray(v)?"Array":"Object")+" ("+Object.keys(v).length+')</summary><pre>'+highlight(JSON.stringify(v,null,2))+'</pre></details>':highlight(JSON.stringify(v)))+'</span></div>';
      }).join("")+'</div>';
    } else {
      let raw=s.valueView==="raw"||!parsed.valid?k.raw:JSON.stringify(parsed.value,null,2);
      $("#value-content").innerHTML='<div class="code-view" tabindex="0" aria-label="Value 内容">'+raw.split("\n").map(function(line,i){
        return '<div class="code-line"><span class="line-number" aria-hidden="true">'+(i+1)+'</span><code>'+(parsed.valid?highlight(line):escape(line))+'</code></div>';
      }).join("")+'</div>';
    }
  }
  function runQuery() {
    const s=session(),p=provider();s.draft=$("#query").value.trim();
    if(!s.draft){toast("请先输入 Key 或匹配模式");$("#query").focus();return;}
    let matches;
    if(s.mode==="exact")matches=fixtures.keys[p.id].filter(function(k){return k.key===s.draft;});
    else {
      const pattern=s.draft.replace(/[.+^$(){}|[\]\\]/g,"\\$&").replace(/\*/g,".*").replace(/\?/g,".");
      const regex=new RegExp("^"+pattern+"$");
      matches=fixtures.keys[p.id].filter(function(k){return regex.test(k.key);});
    }
    s.accepted=s.draft;s.acceptedMode=s.mode;s.total=matches.length;s.items=matches.slice(0,100);
    s.checked.clear();s.selected=s.items.some(function(k){return k.key===s.selected;})?s.selected:(s.items[0]?s.items[0].key:null);
    if(s.selected)s.visited.add(s.selected);
    s.detailOpen=false;s.time="本次查询已完成";
    renderQueryMeta();renderInventory();renderInspector();
    $(".workspace").classList.remove("detail-open");
  }
  function renderProviders() {
    const list=providers(),selected=list.find(function(p){return p.id===state.providerDetail;})||list[0];
    $("#app").innerHTML=heading("存储提供者","了解状态存在哪里，以及每个提供者能做什么。",
      '<button class="button" data-action="refresh-providers">'+icon("refresh")+'刷新注册信息</button>')+
      '<section class="provider-hero" aria-label="注册概览"><div class="hero-statement"><div class="hero-count">'+String(list.length).padStart(2,"0")+'</div><div><h2>已注册的存储提供者</h2><p>'+list.filter(function(p){return p.scope==="本地内存";}).length+' 个本地 · '+list.filter(function(p){return p.scope==="分布式";}).length+' 个分布式<br>默认解析至 <strong>Memory</strong></p></div></div>'+
      '<div class="hero-facts">'+["Memory","Redis","Dapr"].map(function(type,i){return '<div class="hero-fact"><div class="legend"><span class="dot '+["info","primary","secondary"][i]+'"></span>'+type+'</div><b>'+list.filter(function(p){return p.type===type;}).length+'</b><small>个注册</small></div>';}).join("")+'</div></section>'+
      '<div class="provider-layout"><section class="provider-registry"><div class="section-heading"><h2>注册列表</h2><label class="provider-filter">'+icon("search")+'<input id="provider-filter" placeholder="搜索提供者" aria-label="搜索提供者" value="'+escape(state.providerFilter)+'"></label></div><div id="provider-rows"></div><div class="registry-footnote">'+icon("info")+'<span>注册信息来自应用。这里查看配置与能力；进入数据浏览器后操作 Key。</span></div></section>'+
      '<aside class="provider-detail" id="provider-detail" aria-label="提供者详情">'+providerDetails(selected)+'</aside></div>'+
      '<footer class="workbench-footnote"><span>'+icon("info")+'示例注册，不代表当前宿主实际配置。</span><span>注册信息不代表连接健康状态</span></footer>';
    renderProviderRows();
  }
  function renderProviderRows(){
    const rows=providers().filter(function(p){return (p.name+" "+p.type+" "+p.scope).toLowerCase().includes(state.providerFilter.toLowerCase());});
    $("#provider-rows").innerHTML=rows.length?rows.map(function(p){
      return '<div class="provider-row '+(state.providerDetail===p.id?"is-selected":"")+'">'+symbol(p)+'<button class="provider-row-main" data-provider-detail="'+p.id+'" aria-pressed="'+(state.providerDetail===p.id)+'"><strong>'+escape(p.name)+(p.isDefault?badge("IStateStore","info"):"")+'</strong><p>'+p.type+' · '+p.scope+'</p><div class="row-capabilities">'+badge(p.scan?"扫描 + 精确查询":"仅精确查询",p.tone)+'</div></button><a class="button" href="'+routeHref(p.id)+'">浏览数据 '+icon("arrow")+'</a></div>';
    }).join(""):'<div class="empty">'+icon("search")+'<h3>没有匹配的提供者</h3><p>尝试按名称或类型搜索。</p></div>';
  }
  function providerDetails(p){
    const capabilities=[["精确查询",true],["模式扫描",p.scan],["读取 Value",true],["新建 / 编辑",true],["删除 / 批量删除",true],["读取 TTL",p.ttl]];
    return '<div class="detail-identity"><div class="detail-identity-top">'+symbol(p)+'<div><div class="eyebrow">PROVIDER</div><h2>'+escape(p.name)+'</h2></div></div><p>'+escape(p.description)+'</p></div>'+
      '<dl class="provider-facts"><div><dt>存储类型</dt><dd>'+badge(p.type,p.tone)+'</dd></div><div><dt>作用范围</dt><dd>'+p.scope+'</dd></div><div><dt>Service key</dt><dd><code>'+escape(p.serviceKey||"未命名（默认注册）")+'</code></dd></div><div><dt>实现类型</dt><dd><code>'+escape(p.implementation)+'</code></dd></div></dl>'+
      '<div class="capability-list"><h3>浏览器能力</h3><div class="capability-grid">'+capabilities.map(function(c){return '<div class="'+(c[1]?"":"unavailable")+'">'+icon(c[1]?"check":"minus")+c[0]+'</div>';}).join("")+'</div></div>'+
      '<details class="option-details"><summary>模块选项 · 只读诊断</summary>'+(p.options?'<dl class="provider-facts">'+Object.entries(p.options).map(function(e){return '<div><dt>'+escape(e[0])+'</dt><dd><code>'+escape(e[1])+'</code></dd></div>';}).join("")+'</dl>':'<p class="registry-footnote">此注册未提供独立的选项诊断来源。</p>')+'</details>'+
      '<div class="detail-bottom"><a class="button tonal" href="'+routeHref(p.id)+'">进入数据浏览器 '+icon("arrow")+'</a></div>';
  }
  function openModal(title,subtitle,body,footer,kind){
    state.modalKind=kind;
    const modal=$("#modal");
    modal.innerHTML='<header class="modal-header"><div><h2 id="modal-title">'+title+'</h2><p>'+subtitle+'</p></div>'+iconButton("close","关闭对话框","close-modal")+'</header><div class="modal-body">'+body+'</div><footer class="modal-footer">'+footer+'</footer>';
    modal.showModal();
  }
  function openEditor(create){
    const p=provider(),k=create?null:selectedKey(),parsed=k?parseJson(k.raw):{valid:true};
    state.editor={create:create,json:parsed.valid};
    openModal(create?"新建 Key":"编辑值",p.name+" · "+p.type,
      '<div id="editor-error" class="form-error" hidden></div><div class="form-field"><label for="edit-key">Key</label><input id="edit-key" spellcheck="false" autocomplete="off" '+(!create?"readonly":"")+' value="'+escape(k?k.key:"")+'" placeholder="例如 Flight:Route:CA1831:20261016"></div>'+
      '<div class="form-field"><div class="editor-value-label"><label for="edit-value">Value</label><div class="value-switch" role="group" aria-label="编辑格式"><button data-edit-format="json" aria-pressed="'+parsed.valid+'">JSON</button><button data-edit-format="text" aria-pressed="'+(!parsed.valid)+'">文本</button></div></div><textarea id="edit-value" spellcheck="false" autocapitalize="off">'+escape(k?k.raw:'{\n  "enabled": true\n}')+'</textarea></div>'+
      '<div class="form-split">'+(p.ttl?'<div class="form-field"><label for="edit-ttl">TTL（秒）</label><input id="edit-ttl" type="number" min="1" step="1" value="'+(k&&k.ttl!=null?k.ttl:"")+'" placeholder="留空表示永不过期"></div>':"")+
      '<div class="form-field"><label for="edit-etag">ETag</label><input id="edit-etag" spellcheck="false" value="'+escape(k&&k.etag?k.etag:"")+'" placeholder="未提供" '+(!create?"readonly":"")+'><p class="field-help">用于并发校验</p></div></div>',
      '<span>仅修改示例数据</span><button class="button" data-action="close-modal">取消</button><button class="button primary" data-action="save-key">'+(create?"创建 Key":"保存更改")+'</button>',"editor");
    (create?$("#edit-key"):$("#edit-value")).focus();
  }
  function editorError(message){$("#editor-error").hidden=false;$("#editor-error").textContent=message;}
  function saveKey(){
    const s=session(),p=provider(),name=$("#edit-key").value.trim(),raw=$("#edit-value").value,ttlField=$("#edit-ttl"),ttl=ttlField&&ttlField.value!==""?Number(ttlField.value):null;
    if(!name)return editorError("Key 不能为空。");
    if(state.editor.json&&!parseJson(raw).valid)return editorError("JSON 格式有误，请检查后重试；普通文本可切换为「文本」。");
    if(ttlField&&ttlField.value!==""&&(!Number.isFinite(ttl)||ttl<1||!Number.isInteger(ttl)))return editorError("TTL 必须是大于 0 的整数秒，或留空表示永不过期。");
    if(state.editor.create&&fixtures.keys[p.id].some(function(k){return k.key===name;}))return editorError("这个 Key 已存在，请使用其他名称，或编辑已有值。");
    let k;
    if(state.editor.create){
      k={key:name,raw:raw,etag:$("#edit-etag").value||null,ttl:p.ttl?ttl:undefined};
      fixtures.keys[p.id].unshift(k);s.draft=name;s.mode="exact";
    }else{k=selectedKey();k.raw=raw;if(p.ttl)k.ttl=ttl;}
    $("#modal").close();state.modalKind=null;
    if(state.editor.create){renderBrowser();runQuery();}
    s.selected=k.key;s.visited.add(k.key);s.detailOpen=true;
    renderInventory();renderInspector();$(".workspace").classList.add("detail-open");
    toast(state.editor.create?"示例 Key 已创建":"示例值已保存");
    $("#detail-heading").focus({preventScroll:true});
  }
  function openDelete(keys){
    state.deleteKeys=keys;
    openModal(keys.length===1?"删除这个 Key？":"删除所选 "+keys.length+" 个 Key？",
      provider().name+" · "+provider().type,
      '<p>请核对将要删除的完整 Key。</p><div class="delete-list">'+keys.map(function(k){return '<code>'+escape(k)+'</code>';}).join("")+'</div>',
      '<span>仅删除原型中的示例数据</span><button class="button" data-action="close-modal">取消</button><button class="button danger" data-action="confirm-delete">确认删除</button>',"delete");
  }
  function confirmDelete(){
    const s=session(),names=new Set(state.deleteKeys);
    fixtures.keys[state.provider]=fixtures.keys[state.provider].filter(function(k){return !names.has(k.key);});
    s.items=s.items.filter(function(k){return !names.has(k.key);});s.total=s.items.length;s.checked.clear();
    names.forEach(function(n){s.visited.delete(n);});
    if(names.has(s.selected))s.selected=s.items[0]?s.items[0].key:null;
    if(s.selected)s.visited.add(s.selected);
    else {s.detailOpen=false;$(".workspace").classList.remove("detail-open");}
    $("#modal").close();state.modalKind=null;
    renderQueryMeta();renderInventory();renderInspector();
    toast("已删除 "+names.size+" 个示例 Key");
    if($("#detail-heading"))$("#detail-heading").focus({preventScroll:true});
  }
  function designNotes(){
    openModal("设计说明","State Store / Workbench concept 01",
      '<div class="notes"><h3>先选范围，再操作数据</h3><p><strong>提供者页</strong>回答「有哪些注册、数据放在哪里、支持什么能力」；<strong>数据浏览器</strong>回答「找哪个 Key、内容是什么、要做什么修改」。两个独立页面通过原生链接互通，支持浏览器前进 / 后退。</p>'+
      '<h3>保留参考图的工作效率</h3><p>紧凑查询栏、列表 / 命名空间树、可拖动双栏和持续打开的详情。窄屏在列表和详情之间切换，不把详情堆在长列表底部。单提供者时，选择器收为身份展示。</p>'+
      '<h3>能力边界</h3><p>只呈现已存在的通用 Key/Value、ETag、TTL 与模块选项能力。没有 Redis Hash 字段编辑、连接健康率、吞吐量或内存占用。JSON 字段视图来自值解析，字节数是 UTF-8 文本大小。树形分组只基于本次结果，不是数据库完整目录。</p>'+
      '<p>实际接入保留显式查询与默认展示上限 100；当前扫描接口没有游标，不设计「继续扫描」。列表不预读全部 Value / TTL。原型直接展示查询后的示例状态，全部修改在内存中，刷新页面可重置。</p>'+
      '<h3>视觉与实现交接</h3><dl><dt>主题</dt><dd>Monica Precision Default，Zinc / Indigo 深色与 Violet 浅色。</dd><dt>类别</dt><dd>Redis → Primary；Memory → Info；Dapr → Secondary。</dd><dt>字体</dt><dd>本地 IBM Plex Sans、Oxanium、Geist Mono；中文回退 Microsoft YaHei UI / PingFang SC。</dd><dt>组件</dt><dd>MoWorkbenchNavigation 路由导航；MudSelect 提供者选择；MudDialog 编辑与确认；MoContentState 查询状态。正式接入同步 zh-CN / en-US 资源。</dd></dl>'+
      '<h3>建议路由</h3><div class="mapping">/state-store-dashboard → 数据浏览器<br>/state-store-dashboard/providers → 存储提供者</div>'+
      '<h3>查看不同注册规模</h3><p>多提供者用于验证完整信息架构；单提供者对应当前宿主的页面结构。这里切换的是原型数据。</p><div class="heading-actions"><button class="button '+(!state.single?"tonal":"")+'" data-action="scenario-multi">多提供者示例</button><button class="button '+(state.single?"tonal":"")+'" data-action="scenario-single">单提供者示例</button></div></div>',
      '<button class="button primary" data-action="close-modal">返回原型</button>',"notes");
  }
  function toast(message){
    clearTimeout(state.toastTimer);$("#toast").textContent=message;$("#toast").classList.add("visible");
    state.toastTimer=setTimeout(function(){$("#toast").classList.remove("visible");},2600);
  }
  async function copy(value){
    try{await navigator.clipboard.writeText(value);toast("已复制");}
    catch{openModal("复制内容","可选择下方内容手动复制",'<textarea class="copy-fallback" readonly spellcheck="false">'+escape(value)+'</textarea>','<button class="button" data-action="close-modal">关闭</button>',"copy");$(".copy-fallback").select();}
  }
  function setMenu(open){
    state.menu=open;const menu=$(".provider-menu"),trigger=$(".provider-trigger");
    if(!menu||!trigger)return;
    menu.hidden=!open;trigger.setAttribute("aria-expanded",String(open));
    if(open)($("a.selected",menu)||$("a",menu)).focus();
  }
  function setupResizer(){
    const resizer=$(".resizer"),workspace=$(".workspace");
    if(!resizer)return;
    function set(value){state.split=Math.max(30,Math.min(65,value));workspace.style.setProperty("--list-width",state.split+"%");resizer.setAttribute("aria-valuenow",String(Math.round(state.split)));}
    resizer.addEventListener("pointerdown",function(event){
      event.preventDefault();resizer.setPointerCapture(event.pointerId);
      const move=function(e){const rect=workspace.getBoundingClientRect();set((e.clientX-rect.left)/rect.width*100);};
      const end=function(){resizer.removeEventListener("pointermove",move);resizer.removeEventListener("pointerup",end);resizer.removeEventListener("pointercancel",end);};
      resizer.addEventListener("pointermove",move);resizer.addEventListener("pointerup",end);resizer.addEventListener("pointercancel",end);
    });
    resizer.addEventListener("keydown",function(e){if(e.key==="ArrowLeft"||e.key==="ArrowRight"){e.preventDefault();set(state.split+(e.key==="ArrowLeft"?-2:2));}});
    resizer.addEventListener("dblclick",function(){set(43);});
  }
  function readRoute(){
    const parts=location.hash.replace(/^#\/?/,"").split("/");
    const previousRoute=state.route;
    state.route=parts[0]==="providers"?"providers":"browser";
    if(state.route==="browser"){
      state.provider=providers().some(function(p){return p.id===parts[1];})?parts[1]:(state.single?"memory":"redis");
    }else if(parts[1])state.providerDetail=parts[1];
    else if(previousRoute==="browser")state.providerDetail=state.provider;
    if(!providers().some(function(p){return p.id===state.providerDetail;}))state.providerDetail=providers()[0].id;
    state.menu=false;state.expanded=false;render();
  }
  document.addEventListener("submit",function(e){if(e.target.id==="query-form"){e.preventDefault();runQuery();}});
  document.addEventListener("input",function(e){
    if(e.target.id==="query"){session().draft=e.target.value;renderQueryMeta();}
    if(e.target.id==="provider-filter"){state.providerFilter=e.target.value;renderProviderRows();}
  });
  document.addEventListener("change",function(e){
    if(e.target.matches("[data-check]")){
      const name=e.target.dataset.check,s=session();
      if(e.target.checked)s.checked.add(name);else s.checked.delete(name);
      renderInventory();
      const restored=$$("[data-check]").find(function(c){return c.dataset.check===name;});if(restored)restored.focus({preventScroll:true});
    }else if(e.target.id==="check-all"){
      const s=session();s.checked=e.target.checked?new Set(s.items.map(function(k){return k.key;})):new Set();
      renderInventory();$("#check-all").focus({preventScroll:true});
    }
  });
  document.addEventListener("click",function(e){
    if(state.menu&&!e.target.closest(".provider-picker"))setMenu(false);
    const target=e.target.closest("button,a");if(!target)return;
    if(target.matches(".provider-menu a"))setMenu(false);
    if(target.dataset.mode){
      const s=session();s.mode=target.dataset.mode;
      $$("[data-mode]").forEach(function(b){b.setAttribute("aria-pressed",String(b.dataset.mode===s.mode));});
      $("#query-submit span").textContent=s.mode==="scan"?"扫描":"查找";renderQueryMeta();
    }
    if(target.dataset.view){session().view=target.dataset.view;renderInventory();$('[data-view="'+target.dataset.view+'"]').focus({preventScroll:true});}
    if(target.dataset.key){
      const s=session();s.selected=target.dataset.key;s.visited.add(s.selected);s.detailOpen=true;s.valueView="formatted";
      renderInventory();renderInspector();$(".workspace").classList.add("detail-open");
      if(matchMedia("(max-width:760px)").matches)$("#detail-heading").focus({preventScroll:true});
      else {const row=$$("[data-key]").find(function(b){return b.dataset.key===s.selected;});if(row)row.focus({preventScroll:true});}
    }
    if(target.dataset.valueView){
      session().valueView=target.dataset.valueView;
      $$("[data-value-view]").forEach(function(b){b.setAttribute("aria-pressed",String(b.dataset.valueView===session().valueView));});renderValue();
    }
    if(target.dataset.providerDetail){
      state.providerDetail=target.dataset.providerDetail;
      $("#provider-detail").innerHTML=providerDetails(providers().find(function(p){return p.id===state.providerDetail;}));
      renderProviderRows();
      if(matchMedia("(max-width:760px)").matches)$("#provider-detail").scrollIntoView({block:"start",behavior:"instant"});
      else $('[data-provider-detail="'+state.providerDetail+'"]').focus({preventScroll:true});
    }
    if(target.dataset.editFormat){
      state.editor.json=target.dataset.editFormat==="json";
      $$("[data-edit-format]").forEach(function(b){b.setAttribute("aria-pressed",String((b.dataset.editFormat==="json")===state.editor.json));});
    }
    const action=target.dataset.action;
    if(action==="provider-menu")setMenu(!state.menu);
    if(action==="create")openEditor(true);
    if(action==="edit")openEditor(false);
    if(action==="close-modal"){$("#modal").close();state.modalKind=null;}
    if(action==="save-key")saveKey();
    if(action==="delete")openDelete([session().selected]);
    if(action==="bulk-delete")openDelete(Array.from(session().checked));
    if(action==="confirm-delete")confirmDelete();
    if(action==="copy-key")copy(selectedKey().key);
    if(action==="copy-value")copy(selectedKey().raw);
    if(action==="expand"){state.expanded=!state.expanded;$(".workspace").classList.toggle("is-expanded",state.expanded);renderInspector();$('[data-action="expand"]').focus({preventScroll:true});}
    if(action==="refresh-key"){renderInspector();toast("已重新读取示例值");$('[data-action="refresh-key"]').focus({preventScroll:true});}
    if(action==="back-list"){session().detailOpen=false;$(".workspace").classList.remove("detail-open");const row=$$("[data-key]").find(function(b){return b.dataset.key===session().selected;});if(row)row.focus({preventScroll:true});}
    if(action==="reset-query"){session().mode="scan";session().draft="*";renderBrowser();runQuery();}
    if(action==="dapr-example"){$("#query").value="Workflow:Instance:demo-001";runQuery();}
    if(action==="refresh-providers")toast("示例注册信息已刷新");
    if(action==="scenario-single"||action==="scenario-multi"){
      state.single=action==="scenario-single";state.provider=state.single?"memory":"redis";state.providerDetail=state.provider;state.providerFilter="";
      $("#modal").close();state.modalKind=null;
      const next=routeHref(state.provider);if(location.hash===next)readRoute();else location.hash=next;
    }
  });
  document.addEventListener("keydown",function(e){
    const editing=e.target.matches("input,textarea")||$("#modal").open;
    if(e.key==="/"&&!editing&&state.route==="browser"){e.preventDefault();$("#query").focus();}
    if(state.menu){
      const options=$$(".provider-menu a"),index=options.indexOf(document.activeElement);
      if(e.key==="ArrowDown"||e.key==="ArrowUp"){e.preventDefault();options[(index+(e.key==="ArrowDown"?1:options.length-1))%options.length].focus();}
      if(e.key==="Escape"){e.preventDefault();setMenu(false);$(".provider-trigger").focus();}
    }
  });
  $("#modal").addEventListener("click",function(e){if(e.target===$("#modal")){const r=e.target.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom){e.target.close();state.modalKind=null;}}});
  $("#modal").addEventListener("close",function(){state.modalKind=null;});
  $("#theme-toggle").innerHTML=icon("sun");
  $("#theme-toggle").addEventListener("click",function(){
    const light=document.documentElement.dataset.theme!=="light";document.documentElement.dataset.theme=light?"light":"dark";this.innerHTML=icon(light?"moon":"sun");
    this.setAttribute("aria-label",light?"切换为深色主题":"切换为浅色主题");
  });
  $("#design-notes").innerHTML=icon("book");$("#design-notes").addEventListener("click",designNotes);
  window.addEventListener("hashchange",readRoute);
  readRoute();
})();
