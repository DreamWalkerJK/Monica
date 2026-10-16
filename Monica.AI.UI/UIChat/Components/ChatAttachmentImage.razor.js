export function observeThumbnail(root, callback) {
    let stopped = false;
    let pending = Promise.resolve();
    const visible = new IntersectionObserver(entries => {
        if (stopped || !entries.some(entry => entry.isIntersecting)) return;
        visible.disconnect();
        pending = callback.invokeMethodAsync('LoadPreviewAsync').catch(() => {});
    }, { rootMargin: '160px' });
    const removal = new MutationObserver(() => { if (!root.isConnected) shutdown(); });
    visible.observe(root);
    removal.observe(document.body, { childList: true, subtree: true });
    function shutdown() {
        stopped = true;
        visible.disconnect();
        removal.disconnect();
        return pending;
    }
    return { shutdown };
}
