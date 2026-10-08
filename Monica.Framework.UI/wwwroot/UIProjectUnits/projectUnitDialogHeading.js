// The heading is mounted by the native dialog's TitleContent. Naming its nearest
// dialog preserves MudBlazor's focus, Escape, backdrop, and result ownership.
export function labelDialog(heading, headingId) {
    if (!heading?.isConnected || heading.id !== headingId) return;
    const dialog = heading.closest('[role="dialog"]');
    if (!dialog) return;
    dialog.setAttribute('aria-labelledby', headingId);
    dialog.setAttribute('aria-modal', 'true');
}
