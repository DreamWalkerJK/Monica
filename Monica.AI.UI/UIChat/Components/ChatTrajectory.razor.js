export function initialize(root, ledger, timeline) {
    const rowHeight = Number(root.dataset.rowHeight);
    let focusRecordId = null;
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
    const restoreFocus = () => {
        if (!focusRecordId || disposed) return;
        const row = Array.from(ledger.querySelectorAll('[data-record-id]')).find(element => element.dataset.recordId === focusRecordId);
        if (row) { row.focus({ preventScroll: true }); focusRecordId = null; }
    };
    const changes = new MutationObserver(() => { schedule(); restoreFocus(); });
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
        reveal(index, recordId) {
            if (disposed) return;
            follow = false;
            focusRecordId = recordId;
            ledger.scrollTop = Math.max(0, index * rowHeight - ledger.clientHeight / 3);
            restoreFocus();
        },
        interval(from, to) {
            if (disposed) return null;
            const plot = root.querySelector('.timeline-plot');
            if (!plot) return null;
            const bounds = plot.getBoundingClientRect();
            if (bounds.width <= 0) return null;
            const fraction = value => Math.max(0, Math.min(1, (value - bounds.left) / bounds.width));
            return [fraction(Math.min(from, to)), fraction(Math.max(from, to))];
        },
        shutdown
    };
}
