// MudMenu v9 owns this custom activator's single Tab stop and Enter/Space behavior.
// Its native focus-restoration method only covers built-in button/icon activators.
export function focusNativeActivator(content) {
    const activator = content?.closest('.mud-menu-activator[role="button"]');
    if (!activator?.isConnected) return false;
    activator.focus({ preventScroll: true });
    return document.activeElement === activator;
}
