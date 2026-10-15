export function createLayoutObserver(root, main, callback) {
    let stopped = false;
    let frame = 0;
    let last = "";
    let pending = Promise.resolve();

    function report() {
        frame = 0;
        if (stopped || !root.isConnected || !main.isConnected) return;
        const values = [Math.round(root.clientWidth), Math.round(main.clientWidth), Math.round(root.clientHeight)];
        const identity = values.join(":");
        if (identity === last || values.some(value => value <= 0)) return;
        last = identity;
        pending = pending.then(() => {
            if (!stopped && root.isConnected) return callback.invokeMethodAsync("ReportLayoutAsync", ...values);
        }).catch(error => {
            // Keep later observations usable while retaining a visible diagnostic for a failed callback.
            if (!stopped) console.error("Chat layout observation failed.", error);
        });
    }

    function schedule() {
        if (!stopped && !frame) frame = requestAnimationFrame(report);
    }

    const observer = new ResizeObserver(schedule);
    observer.observe(root);
    observer.observe(main);
    schedule();
    return {
        async shutdown() {
            stopped = true;
            observer.disconnect();
            cancelAnimationFrame(frame);
            frame = 0;
            await pending;
        }
    };
}
