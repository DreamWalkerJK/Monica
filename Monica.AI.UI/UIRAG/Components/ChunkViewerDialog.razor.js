export function scrollToMatchedChunk(root) {
    if (!root) {
        return false;
    }

    const scrollRoot = root.querySelector(".chunk-viewer-dialog-body");
    const target = scrollRoot?.querySelector("[data-rag-chunk-target='true']");
    if (!scrollRoot || !target) {
        return false;
    }

    focusElement(target);
    scrollElementIntoView(scrollRoot, target);
    return true;
}

function focusElement(element) {
    if (!element) {
        return;
    }

    element.setAttribute("tabindex", "-1");

    try {
        element.focus({ preventScroll: true });
    }
    catch {
        element.focus();
    }
}

function scrollElementIntoView(scrollRoot, element) {
    const scrollRootRect = scrollRoot.getBoundingClientRect();
    const elementRect = element.getBoundingClientRect();
    const visibleTop = scrollRootRect.top + scrollRoot.clientTop;
    const visibleBottom = visibleTop + scrollRoot.clientHeight;

    // Keep source context in place when any of the matched chunk is already visible.
    // A chunk may be taller than the body, so requiring full visibility would move the first match unnecessarily.
    if (elementRect.bottom > visibleTop && elementRect.top < visibleBottom) {
        return;
    }

    const targetTop =
        scrollRoot.scrollTop + (elementRect.top - visibleTop) - Math.max(48, scrollRoot.clientHeight * 0.18);

    scrollRoot.scrollTo({
        top: Math.max(targetTop, 0),
        behavior: "smooth"
    });
}
