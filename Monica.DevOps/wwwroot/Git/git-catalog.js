// Callback-free sizing and selection. The page owns visit/version admission and resource drainage.
export function createCatalogSizer(root, visit) {
    let disposed = false, frame = 0, lastHeight = -1;
    const catalog = root?.querySelector(".git-catalog");
    const body = catalog?.querySelector(".catalog-body");
    const list = body?.querySelector(".repository-list");
    const header = root?.querySelector(".workspace-frame");
    const shell = root?.closest(".mo-body-content");
    const connected = () => !disposed && root?.isConnected && root.dataset.focusVisit === visit;
    const resizeObserver = new ResizeObserver(() => schedule());
    const mutationObserver = new MutationObserver(() => {
        if (!connected()) dispose();
        else schedule();
    });

    function measure() {
        if (!connected()) { dispose(); return; }
        const toggle = root.querySelector(".catalog-toggle");
        // Compact inventory height remains owned by its CSS; only wide sizing follows the scrollport.
        if (!list || !shell || (toggle && getComputedStyle(toggle).display !== "none")) return;
        const shellBounds = shell.getBoundingClientRect();
        const listBounds = list.getBoundingClientRect();
        const bodyPadding = Number.parseFloat(getComputedStyle(body).paddingBottom) || 0;
        const catalogBorder = Number.parseFloat(getComputedStyle(catalog).borderBottomWidth) || 0;
        const bottom = Math.min(shellBounds.bottom, window.innerHeight);
        const height = Math.max(160, Math.min(512, Math.floor(bottom - listBounds.top - bodyPadding - catalogBorder - 24)));
        if (height === lastHeight) return;
        lastHeight = height;
        root.style.setProperty("--git-catalog-list-height", `${height}px`);
    }
    function schedule() {
        if (!connected()) { dispose(); return; }
        if (frame !== 0) return;
        frame = window.requestAnimationFrame(() => { frame = 0; measure(); });
    }
    function dispose() {
        if (disposed) return;
        disposed = true;
        if (frame !== 0) { window.cancelAnimationFrame(frame); frame = 0; }
        resizeObserver.disconnect();
        mutationObserver.disconnect();
        window.removeEventListener("resize", schedule);
        shell?.removeEventListener("scroll", schedule);
        // Cleanup releases producers without changing a retired component's DOM.
    }
    if (connected() && body && list && shell) {
        for (const element of [root, body, header, shell].filter(Boolean)) resizeObserver.observe(element);
        mutationObserver.observe(root.ownerDocument.documentElement, { childList: true, subtree: true });
        window.addEventListener("resize", schedule, { passive: true });
        shell.addEventListener("scroll", schedule, { passive: true });
        measure();
    } else dispose();
    return { dispose };
}

export function reveal(root, visit, version, kind, key) {
    if (!root?.isConnected || root.dataset.focusVisit !== visit || root.dataset.focusVersion !== String(version) || root.dataset.selectedKey !== key) return;
    const toggle = root.querySelector(".catalog-toggle");
    const compact = toggle && getComputedStyle(toggle).display !== "none";
    if (kind === "catalog" && !compact) return;
    if ((kind === "catalog") !== (root.dataset.catalogOpen === "true")) return;
    const shell = root.closest(".mo-body-content");
    if (!shell) return;
    let target;
    if (kind === "catalog") {
        const entries = [...root.querySelectorAll(".repository-row")];
        target = entries.find(entry => entry.dataset.entryKey === key) ?? entries[0] ?? root.querySelector(".catalog-body input");
        const list = target?.closest(".repository-list");
        if (list) {
            const bounds = list.getBoundingClientRect(), row = target.getBoundingClientRect();
            if (row.top < bounds.top || row.bottom > bounds.bottom) list.scrollTop += row.top - bounds.top - 4;
        }
    } else {
        target = [...root.querySelectorAll(".worksheet-header")].find(header => header.dataset.entryKey === key)?.querySelector("h2");
    }
    if (!target) return;
    if (compact) target.focus({ preventScroll: true });
    const revealed = kind === "detail" ? target.closest(".worksheet-header")
        : target.matches("input") ? target.closest(".mud-input-control") ?? target : target;
    const bounds = shell.getBoundingClientRect(), band = revealed.getBoundingClientRect();
    const appBar = document.querySelector(".mud-appbar")?.getBoundingClientRect();
    const top = Math.max(bounds.top, appBar?.bottom ?? bounds.top) + 12;
    const bottom = bounds.bottom - 12;
    const delta = band.height > bottom - top || band.top < top ? band.top - top
        : band.bottom > bottom ? band.bottom - bottom : 0;
    if (delta !== 0) shell.scrollTo({ top: shell.scrollTop + delta, behavior: "auto" });
}
