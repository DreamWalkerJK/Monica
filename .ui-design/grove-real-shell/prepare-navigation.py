"""Adapt the reviewed shell interactions to the captured host's navigation."""
from pathlib import Path

here = Path(__file__).parent
source = here.parent / 'shell-directions'
code = (source / 'shell-navigation.js').read_text(encoding='utf-8')
start = code.index('  const leaf = ')
end = code.index('\n  function mount(options)', start)
code = code[:start] + '''  const navigation = window.GROVE_DATA.navigation;
  const leaves = navigation.flatMap(category => category.items.flatMap(item => (item.children || [item]).map(page => ({...page,categoryId:category.id,categoryLabel:category.label,groupId:item.children?item.id:'',groupLabel:item.children?item.label:''}))));
  const baseDefaults = {overview:'/system-info',modules:'/module-system-dashboard/modules',configuration:'/configuration/state','/module-system-dashboard':'/module-system-dashboard/overview'};
''' + code[end:]
code = code.replace('monica-shell-directions-v2-', 'monica-grove-real-v1-')
code = code.replace("['module-system','configuration']", "['module-workbench']")
code = code.replace("l.id==='modules-overview'", "l.id==='/system-info'")
code = code.replace("category:'platform'", "category:'monitor'")
code = code.replace('href="#workspace-overview"', 'href="#/system-info"')
code = code.replace('aria-label="Monica home"', 'aria-label="Flight service home"')
code = code.replace('title="Workspace developer"', 'title="Mo User"')
code = code.replace('<span class="sn-wordmark">monica<span>.</span></span><small>Modular infrastructure</small>', '<span class="sn-wordmark">航班服务</span><small class="mono">dev-service-flight-api</small><small>v1.0.3 · Development</small>')
code = code.replace('All systems operational', 'Host ready at capture')
code = code.replace("'Your application at a glance'", "'Registered pages in this host'")
code = code.replace('<h2>Your workspace</h2>', '<h2>More destinations</h2>')
code = code.replace('MAKE YOURSELF AT HOME', 'GROVE / PREFERENCES')
code = code.replace('Grouped menus above your workspace', 'One compact row with nested menus')
code = code.replace("    host.querySelector('.sn-mark').addEventListener", """    function positionRailTooltip(event){const button=event.target.closest('.sn-category');if(!button)return;const rect=button.getBoundingClientRect();const label=button.querySelector('span');label.style.left=`${rect.right+12}px`;label.style.top=`${rect.top+5}px`;}
    host.addEventListener('pointerover',positionRailTooltip);
    host.addEventListener('focusin',positionRailTooltip);
    host.querySelector('.sn-mark').addEventListener""")
code = code.replace("    book:", "    debug:'<path d=\"M8 8h8v10H8ZM5 11h3m8 0h3M5 16h3m8 0h3M9 4l3 4 3-4M12 8v10\"/>',\n    task:'<rect x=\"4\" y=\"5\" width=\"16\" height=\"16\" rx=\"2\"/><path d=\"M8 3v4m8-4v4M4 11h16m-11 5 2 2 4-4\"/>',\n    infrastructure:'<rect x=\"3\" y=\"3\" width=\"18\" height=\"7\" rx=\"2\"/><rect x=\"3\" y=\"14\" width=\"18\" height=\"7\" rx=\"2\"/><path d=\"M7 7h.01M7 18h.01M12 10v4\"/>',\n    book:")
(here / 'navigation.js').write_text(code, encoding='utf-8')
(here / 'navigation.css').write_text((source / 'shell-navigation.css').read_text(encoding='utf-8') + '''
/* Nine real categories remain reachable in shorter viewports. */
.sn-category-rail{overflow-y:auto;overflow-x:visible;min-height:0;gap:7px;margin-top:28px;padding-bottom:15px}
.sn-category{flex-shrink:0}
.sn-rail-bottom{flex-shrink:0}
.sn-identity .sn-wordmark{font-size:22px;letter-spacing:0}
.sn-identity{padding-bottom:20px}
.sn-settings-apply{color:var(--on-primary)!important}
.sn-category>span{position:fixed}
@media(max-width:760px){.sn-category-rail{gap:4px;margin-top:24px}}
''', encoding='utf-8')
