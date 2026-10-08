function observeKeys(target, handler, dotNetRef) {
    let disposed = false;
    const pending = new Set();
    const notify = (method, ...args) => {
        const callback = dotNetRef.invokeMethodAsync(method, ...args).catch(() => {
            // Disconnect may reject a callback that was already queued.
        }).finally(() => pending.delete(callback));
        pending.add(callback);
    };
    const onKeyDown = event => {
        if (!disposed) {
            handler(event, notify);
        }
    };
    target.addEventListener("keydown", onKeyDown);
    return {
        async dispose() {
            disposed = true;
            target.removeEventListener("keydown", onKeyDown);
            await Promise.allSettled(Array.from(pending));
        }
    };
}

export function observeShortcut(dotNetRef) {
    return observeKeys(window, (event, notify) => {
        if (event.isComposing || event.keyCode === 229 || event.repeat || event.altKey || event.shiftKey ||
            !(event.ctrlKey || event.metaKey) || event.key.toLowerCase() !== "k" ||
            document.querySelector(".mud-dialog-container")) {
            return;
        }
        event.preventDefault();
        notify("RequestSearchAsync");
    }, dotNetRef);
}

export function observeDialogKeys(root, dotNetRef) {
    return observeKeys(root, (event, notify) => {
        if (event.isComposing || event.keyCode === 229 || event.key === "Process" || event.ctrlKey || event.metaKey || event.altKey ||
            !(event.target instanceof HTMLInputElement) ||
            !["ArrowDown", "ArrowUp", "Enter"].includes(event.key)) {
            return;
        }
        event.preventDefault();
        notify("HandleSearchKeyAsync", event.key);
    }, dotNetRef);
}

export function scrollSelection(root, selectedIndex) {
    if (root.isConnected) {
        root.querySelector(`[data-search-result-index="${selectedIndex}"]`)?.scrollIntoView({ block: "nearest" });
    }
}
