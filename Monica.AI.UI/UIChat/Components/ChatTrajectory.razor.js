export function initialize(root, ledger, timeline) {
    let disposed = false;
    let follow = false;
    let frame = null;
    const schedule = () => {
        if (disposed || !follow || frame !== null) return;
        frame = requestAnimationFrame(() => {
            frame = null;
            if (!disposed && follow) ledger.scrollTop = ledger.scrollHeight;
        });
    };
    const onScroll = () => {
        follow = ledger.scrollHeight - ledger.scrollTop - ledger.clientHeight <= 40;
    };
    const changes = new MutationObserver(schedule);
    const size = new ResizeObserver(schedule);
    changes.observe(ledger, { childList: true, subtree: true, characterData: true });
    size.observe(ledger);
    ledger.addEventListener('scroll', onScroll, { passive: true });
    const removal = new MutationObserver(() => { if (!root.isConnected) shutdown(); });
    removal.observe(root.ownerDocument.body, { childList: true, subtree: true });
    function shutdown() {
        if (disposed) return;
        disposed = true;
        if (frame !== null) cancelAnimationFrame(frame);
        frame = null;
        changes.disconnect(); size.disconnect(); removal.disconnect();
        ledger.removeEventListener('scroll', onScroll);
    }
    return {
        follow() { if (!disposed) { follow = true; schedule(); } },
        reveal(index) {
            if (disposed) return;
            follow = false;
            // The virtualized ledger uses an intentionally constant 42px row height.
            ledger.scrollTop = Math.max(0, index * 42 - ledger.clientHeight / 3);
        },
        interval(from, to) {
            if (disposed) return null;
            const bounds = timeline.getBoundingClientRect();
            if (bounds.width <= 0) return null;
            const fraction = value => Math.max(0, Math.min(1, (value - bounds.left) / bounds.width));
            return [fraction(Math.min(from, to)), fraction(Math.max(from, to))];
        },
        shutdown
    };
}
