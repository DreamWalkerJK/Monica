// Hover crosshair for the module workbench startup timeline.
// Positions the cursor line and formats the exact offset readout; lane
// selection and captions stay in Blazor. Loaded as a page-level module.
export function attachTimeline(timeline) {
    if (!timeline || timeline.dataset.crosshairAttached === "true") {
        return;
    }

    timeline.dataset.crosshairAttached = "true";
    const domainMs = Number.parseFloat(timeline.dataset.domain || "0");
    const overlay = timeline.querySelector(".tl-overlay");
    const cursor = overlay?.querySelector(".tl-cursor span");
    if (!overlay || !cursor || !(domainMs > 0)) {
        return;
    }

    const format = (ms) => ms >= 1000
        ? `${(ms / 1000).toFixed(2)} s`
        : `${ms.toFixed(1)} ms`;

    timeline.addEventListener("mousemove", (event) => {
        const bounds = overlay.getBoundingClientRect();
        const x = event.clientX - bounds.left;
        if (x < 0 || x > bounds.width) {
            timeline.classList.remove("is-hovering");
            return;
        }

        timeline.classList.add("is-hovering");
        cursor.parentElement.style.left = `${x}px`;
        cursor.textContent = format((x / bounds.width) * domainMs);
    });

    timeline.addEventListener("mouseleave", () => timeline.classList.remove("is-hovering"));
}
