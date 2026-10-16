// A denied visit may focus its own heading only while the browser has no
// persistent control owning focus. The keyed element cannot target a newer visit.
export function focusDeniedHeading(heading) {
    if (!(heading instanceof HTMLElement) || !heading.isConnected) {
        return false;
    }

    const active = document.activeElement;
    if (document.querySelector('[role="dialog"][aria-modal="true"]') ||
        (active && active !== document.body && active !== document.documentElement && active !== heading)) {
        return false;
    }

    heading.focus();
    return document.activeElement === heading;
}
