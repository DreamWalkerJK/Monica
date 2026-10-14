// The Files visit owns this callback target, all users, and observer retirement.
const workspaceFocus = new WeakMap();
export function focusPolicyOpener(root, visit, version) {
    if (!root?.isConnected || root.dataset.visit !== visit || root.dataset.focusVersion !== String(version)) return;
    if ([...root.ownerDocument.querySelectorAll(".mud-dialog, .mud-popover-open")].some(value => value.getClientRects().length)) return;
    const opener = root.querySelector("button[data-policy-open='true']:not(:disabled)");
    if (opener?.getClientRects().length) opener.focus({ preventScroll: true });
}
export function createWorkspaceSizer(root, visit, callback) {
    let disposed = false, frame = 0, last = "";
    const pending = new Set(), failures = [];
    const shell = root?.closest(".mo-body-content");
    const connected = () => !disposed && root?.isConnected && root.dataset.visit === visit;
    const focus = { visit, path: null, version: null, element: null };
    workspaceFocus.set(root, focus);
    const sizes = new ResizeObserver(schedule);
    const mutations = new MutationObserver(schedule);

    function rememberRow(element) {
        focus.path = element.dataset.entryPath;
        focus.version = root.dataset.focusVersion;
        focus.element = element;
    }
    function forgetRow() { focus.path = null; focus.version = null; focus.element = null; }
    function onFocus(event) {
        if (!connected()) return;
        const row = event.target?.closest?.(".entry-open");
        if (row && root.contains(row)) rememberRow(row);
        else if (![root.ownerDocument.body, root.ownerDocument.documentElement].includes(event.target)) forgetRow();
        schedule();
    }
    function onPointer(event) {
        if (!connected()) return;
        const row = event.target?.closest?.(".entry-open");
        if (!row || !root.contains(row)) forgetRow();
    }

    function schedule() {
        if (!connected()) return;
        if (!frame) frame = requestAnimationFrame(() => { frame = 0; measure(); });
    }
    function measure() {
        if (!connected() || !shell) return;
        // Mud's nullable value supplies the mixed SVG, but native accessibility uses this IDL property.
        for (const input of root.querySelectorAll("input[type='checkbox'][data-select-all-state]")) {
            input.indeterminate = input.dataset.selectAllState === "mixed";
        }
        const work = root.querySelector(".work-area");
        const inventory = root.querySelector(".inventory-owner");
        if (!work || !inventory) return;
        sizes.observe(work);
        sizes.observe(inventory);
        const shellBounds = shell.getBoundingClientRect(), workBounds = work.getBoundingClientRect();
        // Measure the stable opening after the actual preceding page controls; scrolling does not change mode.
        const offset = Math.max(0, workBounds.top + shell.scrollTop - shellBounds.top);
        const height = Math.max(0, shellBounds.height - offset);
        const width = workBounds.width, inventoryWidth = inventory.getBoundingClientRect().width;
        root.dataset.workWidth = String(width);
        root.dataset.usableHeight = String(height);
        root.dataset.inventoryWidth = String(inventoryWidth);
        const list = inventory.querySelector(".inventory-list");
        // Directory commands are above work. Subtract the pane's actual disclosure,
        // count/sort header, selection/cap notices and visible footer from its opening.
        // An inspector-free inventory uses the available opening rather than the split-list cap.
        const chrome = list ? Math.max(0, list.getBoundingClientRect().top - workBounds.top) : 0;
        const footer = inventory.querySelector(".inventory-footer")?.getBoundingClientRect().height ?? 0;
        // The page's container query supplies its gutter on this reachable workspace root.
        // Preserve any additional inset supplied by the containing page.
        const page = root.closest(".file-ops-page");
        const pageBottom = (parseFloat(getComputedStyle(root).paddingBottom) || 0) +
            (page ? parseFloat(getComputedStyle(page).paddingBottom) || 0 : 0);
        const splitInspector = root.dataset.split === "true" && root.dataset.hasInspector === "true";
        const cap = Math.max(132, Math.min(splitInspector ? 512 : Infinity, height - chrome - footer - pageBottom - 1));
        const current = root.style.getPropertyValue("--file-inventory-height");
        if (current !== cap + "px") root.style.setProperty("--file-inventory-height", cap + "px");
        const active = root.ownerDocument.activeElement;
        const activeRow = active?.closest?.(".entry-open");
        if (activeRow && root.contains(activeRow) && activeRow.getClientRects().length) rememberRow(activeRow);
        const bodyFocus = [root.ownerDocument.body, root.ownerDocument.documentElement].includes(active);
        const matching = focus.path === root.dataset.selectedPath && focus.version === root.dataset.focusVersion;
        const lost = matching && bodyFocus && (!focus.element?.isConnected || !focus.element.getClientRects().length);
        const focusedPath = matching && (lost || activeRow === focus.element) ? focus.path : null;
        const focusedVersion = Number(root.dataset.focusVersion);
        const signature = [width, height, inventoryWidth].map(value => value.toFixed(3)).join("|") + "|" + focusedPath + "|" + focusedVersion + "|" + lost;
        if (signature === last) return;
        last = signature;
        const task = callback.invokeMethodAsync("OwnerSizeChanged", width, height, inventoryWidth, focusedPath, focusedVersion, lost);
        pending.add(task);
        task.then(() => {
            if (!connected()) return;
            schedule();
        }, error => { failures.push(error); console.error("FileOps owner measurement failed", error); })
            .finally(() => pending.delete(task));
    }
    async function dispose() {
        if (disposed) return;
        disposed = true;
        if (frame) { cancelAnimationFrame(frame); frame = 0; }
        sizes.disconnect();
        mutations.disconnect();
        window.removeEventListener("resize", schedule);
        root.ownerDocument.removeEventListener("focusin", onFocus, true);
        root.ownerDocument.removeEventListener("pointerdown", onPointer, true);
        forgetRow();
        workspaceFocus.delete(root);
        // Stop callbacks before their .NET target is released. Do not mutate retired DOM.
        await Promise.allSettled([...pending]);
        if (failures.length) throw new AggregateError(failures, "FileOps owner measurement callbacks failed");
    }
    if (connected() && shell) {
        sizes.observe(root);
        sizes.observe(shell);
        mutations.observe(root, { childList: true, subtree: true, attributes: true, attributeFilter: ["data-select-all-state"] });
        window.addEventListener("resize", schedule, { passive: true });
        root.ownerDocument.addEventListener("focusin", onFocus, true);
        root.ownerDocument.addEventListener("pointerdown", onPointer, true);
        measure();
    }
    return { dispose };
}

export function reveal(root, visit, version, kind, path, directoryPath = null) {
    if (!root?.isConnected || root.dataset.visit !== visit || root.dataset.focusVersion !== String(version) || root.dataset.selectedPath !== path) return;
    const shell = root.closest(".mo-body-content");
    const receipt = workspaceFocus.get(root);
    if (!shell || receipt?.visit !== visit) return;
    const compact = root.dataset.split !== "true";
    const originalKind = kind;
    const active = root.ownerDocument.activeElement;
    const bodyFocus = [root.ownerDocument.body, root.ownerDocument.documentElement].includes(active);
    const overlayOpen = [...root.ownerDocument.querySelectorAll(".mud-dialog, .mud-popover-open")].some(value => value.getClientRects().length);
    if (overlayOpen) return;
    if (kind === "selection-clear") {
        if (!bodyFocus || !directoryPath || root.dataset.directoryPath !== directoryPath || root.querySelector(".selection-band")) return;
        const target = [...root.querySelectorAll(".select-all input:not(:disabled), .entry-open, .inventory-list")]
            .find(value => value.getClientRects().length);
        if (!target) return;
        target.focus({ preventScroll: true });
        if (target.closest(".inventory-list")) revealInventoryRow(target);
        revealInShell(shell, root.ownerDocument, target);
        return;
    }
    if (kind === "inventory-return") {
        // A completed close returns to an owner which survives table/list child replacement.
        if (path !== "" || root.querySelector(".file-inspector") || !bodyFocus) return;
        const target = root.querySelector(".inventory-list") ?? root.querySelector(".empty-state") ?? root.querySelector(".inventory-toolbar button");
        if (!target?.getClientRects().length) return;
        target.focus({ preventScroll: true });
        revealInShell(shell, root.ownerDocument, target);
        return;
    }
    const activeRow = active?.closest?.(".entry-open");
    const rowOrigin = receipt.path === path && (bodyFocus || activeRow?.dataset.entryPath === path);
    // An admitted selection or mode transition renews only the same actually focused row receipt.
    if (rowOrigin) receipt.version = String(version);
    if (kind === "row") {
        if (!rowOrigin) return;
        const row = [...root.querySelectorAll(".entry-open")].find(value => value.dataset.entryPath === path && value.getClientRects().length);
        if (row) { revealInventoryRow(row); row.focus({ preventScroll: true }); }
        return;
    }
    if (kind === "inspector-transition") {
        if (!rowOrigin) return;
        kind = "inspector";
    }
    if (kind === "inventory" && (!compact || root.dataset.catalogOpen !== "true")) return;
    if (kind === "inspector" && compact && root.dataset.catalogOpen === "true") return;
    let target, band;
    if (kind === "inventory") {
        const entries = [...root.querySelectorAll(".entry-open")].filter(value => value.getClientRects().length);
        target = entries.find(value => value.dataset.entryPath === path) ?? entries[0] ?? root.querySelector(".empty-state");
        if (!target) return;
        revealInventoryRow(target);
        band = target.closest("li, tr") ?? target;
    } else {
        band = [...root.querySelectorAll(".inspector-heading")].find(value => value.dataset.entryPath === path);
        target = band?.querySelector("h2");
    }
    if (!target || !band) return;
    if (compact) {
        if (kind === "inventory" || rowOrigin || bodyFocus) target.focus({ preventScroll: true });
        else return;
    }
    else if (kind === "inspector" && bodyFocus && rowOrigin) {
        // Full-width table to split-list reflow can replace the native name control; restore its same identity.
        const row = [...root.querySelectorAll(".entry-open")].find(value => value.dataset.entryPath === path && value.getClientRects().length);
        if (row) { revealInventoryRow(row); row.focus({ preventScroll: true }); }
    }
    if (compact && originalKind === "inspector") {
        const disclosure = root.querySelector(".inventory-toggle");
        if (disclosure?.getClientRects().length) {
            const bounds = shell.getBoundingClientRect(), navbar = root.ownerDocument.querySelector(".mo-navbar, .mud-appbar")?.getBoundingClientRect();
            const top = Math.max(bounds.top, navbar?.bottom ?? bounds.top) + 12;
            shell.scrollTo({ top: shell.scrollTop + disclosure.getBoundingClientRect().top - top, behavior: "auto" });
            return;
        }
    }
    revealInShell(shell, root.ownerDocument, band);
}

function revealInventoryRow(target) {
    const list = target.closest(".inventory-list");
    if (!list) return;
    const bounds = list.getBoundingClientRect(), row = target.closest("li, tr")?.getBoundingClientRect() ?? target.getBoundingClientRect();
    const delta = row.height > bounds.height || row.top < bounds.top ? row.top - bounds.top : row.bottom > bounds.bottom ? row.bottom - bounds.bottom : 0;
    if (delta) list.scrollTop += delta;
}

function revealInShell(shell, ownerDocument, band) {
    const bounds = shell.getBoundingClientRect(), rect = band.getBoundingClientRect();
    const appBar = ownerDocument.querySelector(".mo-navbar, .mud-appbar")?.getBoundingClientRect();
    const top = Math.max(bounds.top, appBar?.bottom ?? bounds.top) + 12, bottom = bounds.bottom - 12;
    const delta = rect.height > bottom - top || rect.top < top ? rect.top - top : rect.bottom > bottom ? rect.bottom - bottom : 0;
    if (delta) shell.scrollTo({ top: shell.scrollTop + delta, behavior: "auto" });
}

export function restoreTrackedFocus(root, visit, version, path) {
    const receipt = workspaceFocus.get(root);
    if (receipt?.visit !== visit || receipt.path !== path || receipt.version !== String(version)) return;
    reveal(root, visit, version, root.dataset.split === "true" || root.dataset.catalogOpen === "true" ? "row" : "inspector-transition", path);
}

export async function downloadStream(fileName, contentType, streamReference) {
    if (!streamReference) return false;
    const arrayBuffer = await streamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: contentType || "application/octet-stream" });
    return triggerDownload(fileName || "download", blob);
}

function triggerDownload(fileName, blob) {
    const objectUrl = URL.createObjectURL(blob);
    let anchor;
    try {
        anchor = document.createElement("a");
        anchor.href = objectUrl;
        anchor.download = fileName || "download";
        anchor.rel = "noopener";
        anchor.style.display = "none";
        document.body.appendChild(anchor);
        anchor.click();
        return true;
    } finally {
        try { anchor?.remove(); }
        finally { URL.revokeObjectURL(objectUrl); }
    }
}
