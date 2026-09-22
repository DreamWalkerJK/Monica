export function createReadingWidth(root, callback, initialWidth) {
    const handles = [...root.querySelectorAll('[data-reading-resize]')];
    const events = new AbortController();
    let preferredWidth = initialWidth;
    let drag = null;
    let disposed = false;
    let pending = Promise.resolve();
    let limits;

    const bounds = () => {
        const gutter = parseFloat(getComputedStyle(root).getPropertyValue('--mo-chat-reading-gutter')) || 36;
        const max = Math.min(1600, Math.max(0, root.clientWidth - gutter * 2));
        return { min: Math.min(560, max), max };
    };
    const clamp = value => {
        const { min, max } = limits;
        return Math.round(Math.min(max, Math.max(min, value)));
    };
    const apply = value => {
        if (disposed) return;
        const width = clamp(value);
        root.style.setProperty('--mo-chat-reading-width', `${width}px`);
        const { min, max } = limits;
        for (const handle of handles) {
            handle.setAttribute('aria-valuenow', width);
            handle.setAttribute('aria-valuemin', min);
            handle.setAttribute('aria-valuemax', max);
        }
    };
    const commit = value => {
        preferredWidth = Math.max(560, Math.min(1600, Math.round(value)));
        apply(preferredWidth);
        const saved = preferredWidth;
        pending = pending.then(() => disposed ? undefined : callback.invokeMethodAsync('CommitWidthAsync', saved)).catch(() => {});
    };
    const cancelDrag = () => {
        if (!drag) return;
        const active = drag;
        drag = null;
        apply(preferredWidth);
        if (active.handle.hasPointerCapture(active.pointerId)) active.handle.releasePointerCapture(active.pointerId);
    };

    for (const handle of handles) {
        const direction = handle.dataset.readingResize === 'left' ? -1 : 1;
        handle.addEventListener('pointerdown', event => {
            if (disposed || event.button !== 0 || root.clientWidth <= 600) return;
            cancelDrag();
            drag = { handle, pointerId: event.pointerId, x: event.clientX, width: clamp(preferredWidth), next: clamp(preferredWidth), moved: false };
            handle.setPointerCapture(event.pointerId);
            event.preventDefault();
        }, { signal: events.signal });
        handle.addEventListener('pointermove', event => {
            if (!drag || drag.pointerId !== event.pointerId || drag.handle !== handle) return;
            const travel = event.clientX - drag.x;
            drag.moved ||= Math.abs(travel) >= 3;
            drag.next = clamp(drag.width + travel * direction * 2);
            apply(drag.next);
        }, { signal: events.signal });
        handle.addEventListener('pointerup', event => {
            if (!drag || drag.pointerId !== event.pointerId) return;
            const active = drag;
            drag = null;
            const travel = event.clientX - active.x;
            active.moved ||= Math.abs(travel) >= 3;
            active.next = clamp(active.width + travel * direction * 2);
            if (active.moved) commit(active.next); else apply(preferredWidth);
            if (handle.hasPointerCapture(event.pointerId)) handle.releasePointerCapture(event.pointerId);
        }, { signal: events.signal });
        handle.addEventListener('pointercancel', cancelDrag, { signal: events.signal });
        handle.addEventListener('lostpointercapture', cancelDrag, { signal: events.signal });
        handle.addEventListener('dblclick', () => commit(960), { signal: events.signal });
        handle.addEventListener('keydown', event => {
            if (event.key === 'Escape') { cancelDrag(); return; }
            const step = event.shiftKey ? 128 : 32;
            let next;
            if (event.key === 'ArrowLeft') next = clamp(preferredWidth) - step * direction;
            else if (event.key === 'ArrowRight') next = clamp(preferredWidth) + step * direction;
            else if (event.key === 'Home' || event.key === 'Enter') next = 960;
            else if (event.key === 'End') next = limits.max;
            else return;
            event.preventDefault();
            cancelDrag();
            commit(clamp(next));
        }, { signal: events.signal });
    }

    limits = bounds();
    const resize = new ResizeObserver(() => { limits = bounds(); cancelDrag(); apply(preferredWidth); });
    resize.observe(root);
    const removal = new MutationObserver(() => { if (!root.isConnected) shutdown(); });
    removal.observe(document.body, { childList: true, subtree: true });
    function shutdown() {
        if (!disposed) {
            disposed = true;
            drag = null;
            events.abort();
            resize.disconnect();
            removal.disconnect();
        }
        return pending;
    }
    apply(preferredWidth);
    return { shutdown };
}
