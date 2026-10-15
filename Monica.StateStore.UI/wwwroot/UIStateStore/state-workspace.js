const visits = new WeakMap();
function live(root, visit) { return root?.isConnected && root.dataset.visit === visit && visits.get(root)?.visit === visit; }
function contentWidth(root) {
    const style = getComputedStyle(root);
    const width = parseFloat(style.width);
    return style.boxSizing === "border-box" ? width - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight) - parseFloat(style.borderLeftWidth) - parseFloat(style.borderRightWidth) : width;
}
function measure(root, visit) {
    if (!live(root, visit)) return;
    const shell = root.closest(".mo-body-content");
    if (!shell) return;
    const commands = root.querySelector(".workspace-commands")?.getBoundingClientRect().height ?? 0;
    const identity = root.querySelector(".workspace-identity")?.getBoundingClientRect().height ?? 0;
    const receipt = root.querySelector(".accepted-context")?.getBoundingClientRect().height ?? 0;
    const usable = shell.clientHeight - commands - identity - receipt - 96;
    const compact = contentWidth(root) < 960 || usable < 360;
    root.dataset.compact = String(compact);
    root.style.setProperty("--state-inventory-height", `${Math.max(160, Math.min(usable, shell.clientHeight * (compact ? .55 : .75)))}px`);
    root.style.setProperty("--state-value-height", `${Math.max(180, Math.min(544, shell.clientHeight * .6))}px`);
}
export function observe(root, visit) {
    if (visits.has(root)) return;
    const observer = new ResizeObserver(() => measure(root, visit));
    const resize = () => measure(root, visit);
    visits.set(root, { visit, observer, resize });
    observer.observe(root);
    const shell = root.closest(".mo-body-content");
    if (shell) observer.observe(shell);
    window.addEventListener("resize", resize);
    measure(root, visit);
}
function focusReveal(root, target, revealed = target) {
    const shell = root.closest(".mo-body-content");
    if (!shell || !target) return;
    target.focus({ preventScroll: true });
    const bounds = shell.getBoundingClientRect(), band = revealed.getBoundingClientRect();
    const clearance = 12;
    const appBar = document.querySelector(".mud-appbar")?.getBoundingClientRect();
    const usableTop = Math.max(bounds.top, appBar?.bottom ?? bounds.top) + clearance;
    if (band.top < usableTop || band.bottom > bounds.bottom - clearance)
        shell.scrollTo({ top: shell.scrollTop + band.top - usableTop, behavior: "auto" });
}
export function reveal(root, visit, provider, result, selection, focus, key) {
    if (!live(root, visit)) return;
    measure(root, visit);
    if (root.dataset.compact !== "true" || root.dataset.pickerOpen !== "false" || root.dataset.focusVersion !== String(focus) || root.dataset.providerGeneration !== String(provider) || root.dataset.resultGeneration !== String(result) || root.dataset.selectionGeneration !== String(selection) || root.dataset.selectedKey !== key) return;
    const heading = root.querySelector(".inspector-heading");
    focusReveal(root, heading?.querySelector("h2"), heading);
}
export function back(root, visit, provider, result, selection, focus, key) {
    if (!live(root, visit)) return;
    measure(root, visit);
    if (root.dataset.compact !== "true" || root.dataset.pickerOpen !== "true" || root.dataset.focusVersion !== String(focus) || root.dataset.providerGeneration !== String(provider) || root.dataset.resultGeneration !== String(result) || root.dataset.selectionGeneration !== String(selection) || root.dataset.selectedKey !== key) return;
    const selected = [...root.querySelectorAll(".result-select")].find(item => item.dataset.key === root.dataset.selectedKey);
    if (selected) {
        const list = selected.closest(".results-list"), bounds = list?.getBoundingClientRect(), row = selected.getBoundingClientRect();
        if (list && (row.top < bounds.top || row.bottom > bounds.bottom)) list.scrollTop += row.top - bounds.top;
    }
    focusReveal(root, selected ?? root.querySelector(".inventory-heading"));
}
export function stop(root, visit) {
    const state = visits.get(root);
    if (!state || state.visit !== visit) return;
    state.observer.disconnect();
    window.removeEventListener("resize", state.resize);
    visits.delete(root);
}
