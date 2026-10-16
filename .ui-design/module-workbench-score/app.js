(() => {
  "use strict";
  const data = window.WORKBENCH_FIXTURE;
  const $ = (sel, root = document) => root.querySelector(sel);
  const pct = (ms) => (ms / data.domainMs) * 100;
  const esc = (s) => String(s).replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
  const fmt = (ms) => ms >= 1000 ? `${(ms / 1000).toFixed(2)} s` : `${ms.toFixed(1)} ms`;
  const icon = (id, cls = "i") => `<svg class="${cls}"><use href="#${id}"/></svg>`;

  // Derived relationships: reverse edges for "required by".
  const byName = new Map(data.modules.map((m) => [m.name, m]));
  const requiredBy = new Map();
  for (const m of data.modules) {
    for (const r of m.requires) {
      if (!requiredBy.has(r)) requiredBy.set(r, []);
      requiredBy.get(r).push(m.name);
    }
  }
  const total = (m) => m.cb + m.sw;

  // ---------- Toast ----------
  const toast = $("#toast");
  let toastTimer;
  function notify(text) {
    toast.textContent = text;
    toast.classList.add("show");
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => toast.classList.remove("show"), 1800);
  }

  // ---------- Timeline ----------
  const timeline = $("#timeline");
  const caption = $("#scoreCaption");
  const kindColor = { host: "var(--neutral-bar)", compose: "var(--primary)", work: "var(--info)" };
  let activeLane = "work";

  function renderTimeline() {
    const ticks = [];
    for (let s = 0; s * 1000 <= data.domainMs; s++) {
      ticks.push(`<span class="tl-tick" style="left:${pct(s * 1000)}%">${s}s</span>`);
    }
    const grid = [];
    for (let s = 1; s * 1000 < data.domainMs; s++) {
      grid.push(`<i class="tl-grid" style="left:${pct(s * 1000)}%"></i>`);
    }
    const brackets = data.brackets.map((b) =>
      `<div class="bracket ${b.kind === "work" ? "info" : ""}" style="left:${pct(b.start)}%;width:${pct(b.end - b.start)}%"><span>${esc(b.label)}</span></div>`
    ).join("");

    const lanes = data.lanes.map((lane) => {
      let bars;
      if (lane.segments) {
        let cursor = lane.start;
        bars = lane.segments.map((seg, i) => {
          const left = pct(cursor), width = pct(seg.ms);
          cursor += seg.ms;
          const cls = ["bar", "k-work", "seg", i === 0 ? "first" : "", i === lane.segments.length - 1 ? "last" : "", seg.warn ? "warn" : ""].join(" ");
          const label = width > 6 ? `<span class="seg-label" style="left:${left}%;max-width:${width}%">${esc(seg.label)}</span>` : "";
          return `<i class="${cls}" data-alt="${i % 2}" data-module="${seg.module ?? ""}" title="${esc(seg.label)} · ${fmt(seg.ms)}" style="left:${left}%;width:${width}%"></i>${label}`;
        }).join("");
      } else {
        bars = `<i class="bar k-${lane.kind}" style="left:${pct(lane.start)}%;width:${pct(lane.end - lane.start)}%"></i>`;
      }
      return `
        <div class="tl-lane${lane.id === activeLane ? " is-active" : ""}" data-lane="${lane.id}" role="button" tabindex="0"
             aria-label="${esc(lane.name)} ${fmt(lane.end - lane.start)}">
          <div class="tl-label"><span class="tl-name">${esc(lane.name)}</span><span class="tl-dur">${fmt(lane.end - lane.start)}</span></div>
          <div class="tl-track">${bars}</div>
        </div>`;
    }).join("");

    timeline.innerHTML = `
      <div class="tl-axis" aria-hidden="true"><span></span><div class="tl-track">${ticks.join("")}</div></div>
      <div class="tl-body">
        <div class="tl-bracket" aria-hidden="true"><span></span><div class="tl-track">${brackets}</div></div>
        ${lanes}
        <div class="tl-overlay" aria-hidden="true">
          ${grid.join("")}
          <div class="ready" style="left:${pct(data.readyMs)}%"><span class="ready-flag">就绪 <span class="mono">${fmt(data.readyMs)}</span></span></div>
          <div class="cursor"><span></span></div>
        </div>
      </div>`;
    renderCaption();
  }

  function renderCaption() {
    const lane = data.lanes.find((l) => l.id === activeLane);
    const dur = lane.end - lane.start;
    const share = ((dur / data.readyMs) * 100).toFixed(1);
    caption.innerHTML = `
      <span class="cap-dot" style="background:${kindColor[lane.kind]}"></span>
      <strong>${esc(lane.name)}</strong>
      <span class="mono">${fmt(lane.start)} → ${fmt(lane.end)}</span>
      <span>占就绪时刻 ${share}%</span>
      <span aria-hidden="true">·</span>
      <span>${esc(lane.note)}</span>`;
  }

  // Overlay must align with the track column; position it from the first track.
  function placeOverlay() {
    const overlay = $(".tl-overlay", timeline);
    const track = $(".tl-lane .tl-track", timeline);
    const body = $(".tl-body", timeline);
    if (!overlay || !track) return;
    const b = body.getBoundingClientRect(), t = track.getBoundingClientRect();
    Object.assign(overlay.style, {
      position: "absolute", top: "0", bottom: "0", pointerEvents: "none",
      left: `${t.left - b.left}px`, width: `${t.width}px`
    });
  }

  timeline.addEventListener("mousemove", (e) => {
    const overlay = $(".tl-overlay", timeline);
    if (!overlay) return;
    const r = overlay.getBoundingClientRect();
    const x = e.clientX - r.left;
    if (x < 0 || x > r.width) { timeline.classList.remove("hovering"); return; }
    timeline.classList.add("hovering");
    const cursor = $(".cursor", overlay);
    cursor.style.left = `${x}px`;
    $("span", cursor).textContent = fmt((x / r.width) * data.domainMs);
  });
  timeline.addEventListener("mouseleave", () => timeline.classList.remove("hovering"));

  timeline.addEventListener("click", (e) => {
    const seg = e.target.closest(".bar.seg[data-module]");
    if (seg && seg.dataset.module) { openInspector(seg.dataset.module, seg); return; }
    const lane = e.target.closest(".tl-lane");
    if (lane) selectLane(lane.dataset.lane);
  });
  timeline.addEventListener("keydown", (e) => {
    const lane = e.target.closest(".tl-lane");
    if (!lane) return;
    if (e.key === "Enter" || e.key === " ") { e.preventDefault(); selectLane(lane.dataset.lane); }
    if (e.key === "ArrowDown" || e.key === "ArrowUp") {
      e.preventDefault();
      const sib = e.key === "ArrowDown" ? lane.nextElementSibling : lane.previousElementSibling;
      if (sib?.classList.contains("tl-lane")) sib.focus();
    }
  });
  function selectLane(id) {
    activeLane = id;
    timeline.querySelectorAll(".tl-lane").forEach((el) => el.classList.toggle("is-active", el.dataset.lane === id));
    renderCaption();
  }

  // ---------- Ledger ----------
  const rowsEl = $("#ledgerRows");
  const searchEl = $("#ledgerSearch");
  let filter = "all";
  const maxTotal = Math.max(...data.modules.map(total));

  function highlight(text, q) {
    if (!q) return esc(text);
    const i = text.toLowerCase().indexOf(q.toLowerCase());
    if (i < 0) return esc(text);
    return esc(text.slice(0, i)) + `<mark class="q">${esc(text.slice(i, i + q.length))}</mark>` + esc(text.slice(i + q.length));
  }

  function renderLedger() {
    const q = searchEl.value.trim();
    const ql = q.toLowerCase();
    const sorted = [...data.modules].sort((a, b) => total(b) - total(a));
    const rows = sorted.filter((m) => {
      if (ql && !m.name.toLowerCase().includes(ql) && !m.asm.toLowerCase().includes(ql)) return false;
      switch (filter) {
        case "startup": return m.sw > 0;
        case "web": return m.caps.some((c) => c.startsWith("Web"));
        case "ui": return m.caps.includes("UI");
        case "disabled": return !!m.disabled;
        default: return true;
      }
    });

    const visible = expanded || q || filter !== "all" ? rows : rows.slice(0, 10);
    $("#ledgerCount").textContent = `显示 ${visible.length} / ${data.declared}`;
    if (!rows.length) {
      rowsEl.innerHTML = `<li class="ledger-empty">没有匹配“${esc(q)}”的模块</li>`;
    } else {
      rowsEl.innerHTML = visible.map((m) => {
        const rank = sorted.indexOf(m) + 1;
        const t = total(m);
        const over = m.budget && m.sw > m.budget;
        const caps = (m.disabled ? `<span class="cap off">已禁用</span>` : "") + m.caps.map((c) => `<span class="cap">${esc(c)}</span>`).join("");
        return `
          <li class="lrow${m.disabled ? " is-disabled" : ""}" tabindex="0" role="button" data-module="${esc(m.name)}" aria-label="${esc(m.name)}，合计 ${fmt(t)}">
            <span class="rank">${String(rank).padStart(2, "0")}</span>
            <div class="lid">
              <span class="lname">${highlight(m.name, q)}</span>
              <div class="lmeta"><span class="asm">${highlight(m.asm, q)}</span>${caps}</div>
            </div>
            <div class="lbar">
              <div class="meter"><i class="m-cb" style="width:${(m.cb / maxTotal) * 100}%"></i><i class="m-sw" style="width:${(m.sw / maxTotal) * 100}%"></i></div>
              <div class="meter-cap"><span>回调 ${m.cb ? fmt(m.cb) : "—"}</span>${m.sw ? `<span>任务 ${fmt(m.sw)}</span>` : ""}</div>
            </div>
            <span class="total${over ? " warn" : ""}">${t ? fmt(t) : "—"}</span>
            <span class="chev" aria-hidden="true">${icon("i-chev")}</span>
          </li>`;
      }).join("");
    }
    const hidden = rows.length - visible.length;
    $("#ledgerFoot").innerHTML = hidden > 0
      ? `<button class="more" type="button" id="ledgerMore">再显示 ${hidden} 个模块 ${icon("i-chev", "i i-sm")}</button><span>其余 ${data.remainder.count} 个合计 ${fmt(data.remainder.ms)}</span>`
      : `<span>其余 ${data.remainder.count} 个模块合计 ${fmt(data.remainder.ms)}，完整注册表在“模块”分区。</span>`;
  }
  let expanded = false;
  $("#ledgerFoot").addEventListener("click", (e) => {
    if (e.target.closest("#ledgerMore")) { expanded = true; renderLedger(); }
  });

  searchEl.addEventListener("input", renderLedger);
  $("#ledgerChips").addEventListener("click", (e) => {
    const btn = e.target.closest("button[data-filter]");
    if (!btn) return;
    filter = btn.dataset.filter;
    $("#ledgerChips").querySelectorAll("button").forEach((b) => b.setAttribute("aria-pressed", String(b === btn)));
    renderLedger();
  });
  rowsEl.addEventListener("click", (e) => {
    const row = e.target.closest(".lrow");
    if (row) openInspector(row.dataset.module, row);
  });
  rowsEl.addEventListener("keydown", (e) => {
    const row = e.target.closest(".lrow");
    if (row && (e.key === "Enter" || e.key === " ")) { e.preventDefault(); openInspector(row.dataset.module, row); }
  });

  // ---------- Side panels ----------
  function renderPath() {
    $("#criticalPath").innerHTML = data.criticalPath.map((p) => {
      const name = p.module
        ? `<button class="link" type="button" data-module="${esc(p.module)}">${esc(p.name)}</button>`
        : esc(p.name);
      return `<li>
        <span class="node k-${p.kind}${p.warn ? " warn" : ""}" aria-hidden="true"></span>
        <span class="p-name">${name}<small>${esc(p.sub)}</small></span>
        <span class="p-dur">${fmt(p.ms)}</span>
      </li>`;
    }).join("");
  }

  function renderDepth() {
    const last = data.depth.length - 1;
    $("#depthChart").innerHTML = data.depth.map((n, d) => {
      const o = (0.28 + (d / last) * 0.72).toFixed(2);
      const dots = Array.from({ length: n }, () => `<i style="--o:${o}"></i>`).join("");
      return `<div class="d-row"><span class="d-key">L${d}</span><span class="d-dots" aria-hidden="true">${dots}</span><span class="d-n">${n}</span></div>`;
    }).join("");
  }

  // Any [data-module] link outside the ledger/timeline opens the inspector.
  document.addEventListener("click", (e) => {
    const link = e.target.closest("button.link[data-module]");
    if (link) openInspector(link.dataset.module, link);
  });

  // ---------- Inspector ----------
  const inspector = $("#inspector");
  const scrim = $("#scrim");
  let returnFocus = null;

  function depButtons(names) {
    if (!names?.length) return `<span class="dep-none">无</span>`;
    return `<div class="dep-list">${names.map((n) =>
      `<button type="button" data-goto="${esc(n)}" ${byName.has(n) ? "" : `title="未包含在示意数据中"`}>${esc(n)}</button>`).join("")}</div>`;
  }

  function openInspector(name, origin) {
    const m = byName.get(name);
    if (!m) { notify(`${name} 未包含在示意数据中`); return; }
    if (inspector.hidden) returnFocus = origin ?? document.activeElement;
    const t = total(m);
    const over = m.budget && m.sw > m.budget;
    $("#inspTitle").textContent = m.name;
    $("#inspBody").innerHTML = `
      <section>
        <div class="insp-total"><b>${t ? (t >= 1000 ? (t / 1000).toFixed(2) : t.toFixed(1)) : "—"}</b><span>${t >= 1000 ? "s" : t ? "ms" : ""} 记录耗时</span></div>
        <div class="insp-split">
          <div class="meter"><i class="m-cb" style="width:${t ? (m.cb / t) * 100 : 0}%"></i><i class="m-sw" style="width:${t ? (m.sw / t) * 100 : 0}%"></i></div>
          <dl class="split-legend">
            <div><dt><i class="sw sw-primary"></i>注册回调</dt><dd>${fmt(m.cb)}</dd></div>
            <div><dt><i class="sw sw-info"></i>启动任务</dt><dd>${fmt(m.sw)}${over ? `<small class="over">超出预算 ${fmt(m.budget)}</small>` : ""}</dd></div>
          </dl>
        </div>
      </section>
      <section class="insp-sec">
        <h3>身份</h3>
        <dl class="kv">
          <dt>状态</dt><dd>${m.disabled ? "已禁用" : "启用"}</dd>
          <dt>程序集</dt><dd class="mono">${esc(m.asm)}</dd>
          <dt>完整类型</dt><dd class="mono">${esc(m.asm)}.Modules.${esc(m.name)}</dd>
          <dt>能力</dt><dd>${m.caps.length ? m.caps.map(esc).join(" · ") : "—"}</dd>
          <dt>依赖深度</dt><dd>L${m.depth}</dd>
        </dl>
      </section>
      <section class="insp-sec"><h3>依赖于 · ${m.requires.length}</h3>${depButtons(m.requires)}</section>
      <section class="insp-sec"><h3>被依赖 · ${(requiredBy.get(m.name) ?? []).length}</h3>${depButtons(requiredBy.get(m.name))}</section>`;
    scrim.hidden = false;
    inspector.hidden = false;
    $("#inspClose").focus();
  }

  function closeInspector() {
    inspector.hidden = true;
    scrim.hidden = true;
    returnFocus?.focus?.();
  }

  $("#inspClose").addEventListener("click", closeInspector);
  scrim.addEventListener("click", closeInspector);
  document.addEventListener("keydown", (e) => { if (e.key === "Escape" && !inspector.hidden) closeInspector(); });
  $("#inspBody").addEventListener("click", (e) => {
    const b = e.target.closest("button[data-goto]");
    if (b) openInspector(b.dataset.goto);
  });

  // ---------- Masthead actions ----------
  $(".id-chip").addEventListener("click", async (e) => {
    const id = e.currentTarget.dataset.copy;
    try { await navigator.clipboard.writeText(id); notify("已复制完整组合 ID"); }
    catch { notify(id); }
  });
  $("#refreshBtn").addEventListener("click", (e) => {
    const btn = e.currentTarget;
    btn.classList.add("spinning");
    btn.disabled = true;
    setTimeout(() => { btn.classList.remove("spinning"); btn.disabled = false; notify("快照已是最终状态（示意）"); }, 900);
  });

  const themeBtn = $("#themeToggle");
  function applyTheme(t) {
    document.documentElement.dataset.theme = t;
    $("use", themeBtn).setAttribute("href", t === "dark" ? "#i-sun" : "#i-moon");
  }
  themeBtn.addEventListener("click", () => applyTheme(document.documentElement.dataset.theme === "dark" ? "light" : "dark"));
  const qsTheme = new URLSearchParams(location.search).get("theme");
  if (qsTheme === "dark") applyTheme("dark");

  // ---------- Boot ----------
  renderTimeline();
  renderLedger();
  renderPath();
  renderDepth();
  requestAnimationFrame(placeOverlay);
  new ResizeObserver(placeOverlay).observe(timeline);
})();
