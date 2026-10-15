// A session observes one routed workspace. It never reads or changes mapping evidence.
export function observe(root, callback) {
    const scrollport = root.closest('.mo-body-content');
    if (!scrollport) throw new Error('Mapper workspace requires the shell scroll owner.');
    let retired = false, frame = 0, publishing = null, pending = null, last = '', retirement = null;
    const uses = new Set();
    const failures = [];
    const remember = promise => {
        uses.add(promise);
        promise.then(() => uses.delete(promise), error => {
            uses.delete(promise);
            failures.push(error);
            if (!retired) console.error('Mapper workspace operation failed.', error);
        });
        return promise;
    };

    async function publish() {
        while (!retired && pending) {
            const measurement = pending;
            pending = null;
            await callback.invokeMethodAsync('OnWorkbenchMeasured', measurement.width, measurement.openingHeight);
        }
    }

    function measure() {
        frame = 0;
        if (retired || !root.isConnected) return;
        const workspace = root.querySelector('.mapper-workspace');
        if (!workspace) return;
        const width = Math.round(workspace.getBoundingClientRect().width * 100) / 100;
        // Relative geometry is stable when the shell scrolls through a long expression.
        const beforeWorkspace = workspace.getBoundingClientRect().top - root.getBoundingClientRect().top;
        const openingHeight = Math.round(Math.max(0, scrollport.clientHeight - beforeWorkspace) * 100) / 100;
        const catalog = root.querySelector('.mapper-catalog');
        const controls = root.querySelector('.mapper-catalog__controls');
        const header = root.querySelector('.mapper-catalog__header');
        const controlsHeight = (controls?.getBoundingClientRect().height || 0) + (header?.getBoundingClientRect().height || 0);
        if (catalog) catalog.style.setProperty('--mapper-entry-height', Math.min(600, Math.max(96, openingHeight - controlsHeight - 24)) + 'px');
        const key = width + '/' + openingHeight;
        if (key === last) return;
        last = key;
        pending = { width, openingHeight };
        if (!publishing) {
            publishing = remember(publish());
            publishing.finally(() => { publishing = null; }).catch(() => {});
        }
    }

    function schedule() {
        if (!retired && !frame) frame = requestAnimationFrame(measure);
    }
    const observer = new ResizeObserver(schedule);
    observer.observe(root);
    observer.observe(scrollport);
    const header = root.querySelector('.mapper-page__header');
    if (header) observer.observe(header);
    // Browser-side removal also retires callbacks when a disconnected circuit cannot invoke disposal.
    const removalObserver = new MutationObserver(() => {
        if (!root.isConnected) retire().catch(error => console.error('Mapper workspace retirement failed.', error));
    });
    removalObserver.observe(scrollport, { childList: true, subtree: true });
    schedule();

    function retire() {
        if (retirement) return retirement;
        retired = true;
        observer.disconnect();
        removalObserver.disconnect();
        if (frame) cancelAnimationFrame(frame);
        pending = null;
        retirement = (async () => {
            await Promise.allSettled([...uses]);
            if (failures.length) throw new AggregateError(failures, 'Mapper workspace operations failed.');
        })();
        return retirement;
    }

    function matches(read, ordinal, version) {
        return !retired && root.isConnected && root.dataset.readId === String(read)
            && root.dataset.selectedOrdinal === String(ordinal) && root.dataset.selectionVersion === String(version)
            && root.querySelector('.mapper-workspace')?.dataset.mode === 'compact'
            && root.querySelector('.mapper-workspace')?.dataset.pickerOpen === 'false';
    }

    return {
        resetEntries(read) {
            if (!retired && root.isConnected && root.dataset.readId === String(read)) {
                const entries = root.querySelector('.mapper-catalog__entries');
                if (entries) entries.scrollTop = 0;
            }
            schedule();
        },
        reveal(read, ordinal, version) {
            return remember((async () => {
                await new Promise(resolve => requestAnimationFrame(resolve));
                if (!matches(read, ordinal, version)) return;
                const heading = root.querySelector('#mapper-expression-heading');
                const reader = heading?.closest('.mapper-reader');
                if (!reader) return;
                heading.focus({ preventScroll: true });
                // Reveal the whole heading band with its owned margin, including the focus ring.
                reader.scrollIntoView({ block: 'start', inline: 'nearest', behavior: 'instant' });
            })());
        },
        focusPicker(read, version) {
            if (!retired && root.isConnected && root.dataset.readId === String(read)
                && root.dataset.selectionVersion === String(version)
                && root.querySelector('.mapper-workspace')?.dataset.mode === 'compact')
                root.querySelector('.mapper-picker-toggle')?.focus({ preventScroll: true });
        },
        dispose: retire
    };
}
