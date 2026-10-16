// Explicit compact comparison reveal uses the shell's real scroll owner.
export function revealAccepted(page, headingId, revision, visit) {
    if (!page?.isConnected || page.dataset.visit !== visit || page.dataset.acceptedRevision !== revision || page.dataset.draftRevision !== revision) return;
    const drafts = page.querySelector('.diff-draft-surface');
    if (!drafts || contentWidth(drafts) >= 900) return;
    const heading = page.querySelector(`#${headingId}`);
    const owner = page.closest('.mo-body-content');
    if (!heading || !owner) return;
    const ownerBounds = owner.getBoundingClientRect();
    const headingBounds = heading.getBoundingClientRect();
    const clearance = 12;
    owner.scrollTop += headingBounds.top - ownerBounds.top - clearance;
    heading.focus({ preventScroll: true });
}

function contentWidth(element) {
    const style = getComputedStyle(element);
    const width = parseFloat(style.width);
    if (style.boxSizing !== 'border-box') return width;
    return width - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight)
        - parseFloat(style.borderLeftWidth) - parseFloat(style.borderRightWidth);
}

// A page-local return action restores a real editor without changing any evidence.
export function revealDrafts(page, inputId, visit) {
    if (!page?.isConnected || page.dataset.visit !== visit) return;
    const input = page.querySelector(`#${inputId}`);
    const editor = input?.closest('.diff-draft-editor');
    const owner = page.closest('.mo-body-content');
    if (!input || !editor || !owner) return;
    owner.scrollTop += editor.getBoundingClientRect().top - owner.getBoundingClientRect().top - 12;
    input.focus({ preventScroll: true });
}
