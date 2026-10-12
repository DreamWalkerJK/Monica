// Callback-free focus/reveal. The page owns admission, visit/version guards and import drainage.
export function reveal(root, visit, version, kind, key) {
    if (!root?.isConnected || root.dataset.focusVisit !== visit || root.dataset.focusVersion !== String(version) || root.dataset.selectedKey !== key) return;
    const toggle = root.querySelector(".compact-catalog-toggle");
    const compact = toggle && getComputedStyle(toggle).display !== "none";
    if (kind === "catalog" && !compact) return;
    const open = root.dataset.catalogOpen === "true";
    if ((kind === "catalog") !== open) return;
    const shell = root.closest(".mo-body-content");
    if (!shell) return;
    let target;
    if (kind === "catalog") {
        const entries = [...root.querySelectorAll(".entry-button")];
        target = entries.find(entry => entry.dataset.entryKey === key) ?? entries[0] ?? root.querySelector(".catalog-body input");
        const list = target?.closest(".entry-list");
        if (list) {
            const bounds = list.getBoundingClientRect(), row = target.getBoundingClientRect();
            if (row.top < bounds.top || row.bottom > bounds.bottom) list.scrollTop += row.top - bounds.top - 4;
        }
    } else {
        target = [...root.querySelectorAll(".detail-header")].find(header => header.dataset.entryKey === key)?.querySelector("h2");
    }
    if (!target) return;
    // Wide selection retains native row focus; compact selection focuses the revealed target.
    if (compact) target.focus({ preventScroll: true });
    const revealed = kind === "detail" ? target.closest(".detail-header")
        : target.matches("input") ? target.closest(".mud-input-control") ?? target : target;
    const bounds = shell.getBoundingClientRect(), band = revealed.getBoundingClientRect();
    const appBar = document.querySelector(".mud-appbar")?.getBoundingClientRect();
    const top = Math.max(bounds.top, appBar?.bottom ?? bounds.top) + 12;
    const bottom = bounds.bottom - 12;
    // Preserve the shell position when already visible; otherwise move only the necessary distance.
    // An oversized heading can only expose its stable start, not fit the complete band.
    const delta = band.height > bottom - top || band.top < top ? band.top - top
        : band.bottom > bottom ? band.bottom - bottom : 0;
    if (delta !== 0) shell.scrollTo({ top: shell.scrollTop + delta, behavior: "auto" });
}
