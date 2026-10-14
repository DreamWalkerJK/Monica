const navigationMenus = new Set();
const POINTER_LEAVE_GRACE_MS = 180;

function isVisible(element) {
    if (!element || element.getClientRects().length === 0) {
        return false;
    }

    const styles = window.getComputedStyle(element);
    return styles.display !== "none" && styles.visibility !== "hidden";
}

/**
 * Coordinates pointer travel between one trigger and its portaled MudPopover.
 * Native links and Tab behavior remain owned by MudBlazor and the browser.
 */
export function createNavigationPopoverObserver(activator, popupId, dotNetRef) {
    let disposed = false;
    let enabled = false;
    let open = false;
    let openedByHover = false;
    let suppressHover = false;
    let closeTimer = 0;
    let focusFrame = 0;
    let pointerVersion = 0;
    const hoverPointer = window.matchMedia("(hover: hover) and (pointer: fine)");
    const pendingNotifications = new Set();
    const navigationRow = activator.closest(".navbar-nav") ?? activator.parentElement;
    const popup = () => document.getElementById(popupId);
    const contains = (target) => target instanceof Node && (activator.contains(target) || popup()?.contains(target));

    const notify = (requestedOpen) => {
        const version = ++pointerVersion;
        const notification = dotNetRef.invokeMethodAsync("UpdatePointerNavigationAsync", requestedOpen, version).catch(() => {
            // A callback already queued can be rejected when its server circuit disconnects.
        }).finally(() => {
            pendingNotifications.delete(notification);
        });
        pendingNotifications.add(notification);
    };

    const cancelClose = () => {
        if (closeTimer !== 0) {
            window.clearTimeout(closeTimer);
            closeTimer = 0;
        }
    };

    const cancelFocus = () => {
        if (focusFrame !== 0) {
            window.cancelAnimationFrame(focusFrame);
            focusFrame = 0;
        }
    };

    const requestClose = () => {
        cancelClose();
        cancelFocus();
        if (disposed || !open) {
            return;
        }

        open = false;
        openedByHover = false;
        notify(false);
    };

    const closeNeighbors = () => {
        for (const menu of navigationMenus) {
            if (menu !== controller && menu.navigationRow === navigationRow) {
                menu.close();
            }
        }
    };

    const requestOpen = () => {
        cancelClose();
        if (disposed || !enabled || !isVisible(activator)) {
            return;
        }

        closeNeighbors();
        if (!open) {
            open = true;
            openedByHover = true;
            notify(true);
        }
    };

    const scheduleClose = () => {
        cancelClose();
        if (!open || !openedByHover) {
            return;
        }

        closeTimer = window.setTimeout(() => {
            closeTimer = 0;
            // The popup can appear beneath a stationary pointer after a server render.
            if (!activator.matches(":hover") && !popup()?.matches(":hover") && !popup()?.contains(document.activeElement)) {
                requestClose();
            }
        }, POINTER_LEAVE_GRACE_MS);
    };

    const handlePointerOver = (event) => {
        if (disposed || !enabled || event.pointerType !== "mouse" || !hoverPointer.matches) {
            return;
        }

        if (activator.contains(event.target) && !suppressHover) {
            requestOpen();
        }
        else if (popup()?.contains(event.target)) {
            cancelClose();
        }
    };

    const handlePointerOut = (event) => {
        if (activator.contains(event.target) && !activator.contains(event.relatedTarget)) {
            suppressHover = false;
        }

        if (disposed || !enabled || event.pointerType !== "mouse" || !contains(event.target) || contains(event.relatedTarget)) {
            return;
        }

        scheduleClose();
    };

    const handlePointerDown = (event) => {
        if (enabled && open && !contains(event.target)) {
            requestClose();
        }
    };

    const handleFocusIn = (event) => {
        if (enabled && open && !contains(event.target)) {
            requestClose();
        }
    };

    const handleKeyDown = (event) => {
        if (enabled && event.key === "ArrowDown" && activator.contains(event.target)) {
            event.preventDefault();
        }

        if (enabled && open && event.key === "Escape") {
            suppressHover = activator.matches(":hover");
        }

        if (enabled && open && event.key === "Escape" && !contains(event.target)) {
            // A hover opening can be dismissed while focus remains elsewhere on the page.
            requestClose();
        }
    };

    const handleResize = () => {
        if (enabled && open && !isVisible(activator)) {
            requestClose();
        }
    };

    const focusFirstWhenVisible = () => {
        cancelFocus();
        let remainingFrames = 120;
        const focus = () => {
            focusFrame = 0;
            if (disposed || !enabled || !open || !activator.isConnected) {
                return;
            }

            const surface = popup();
            const first = surface && Array.from(window.getTabbableElements(surface)).find(isVisible);
            if (isVisible(surface) && first) {
                first.focus({ preventScroll: true });
                return;
            }

            // MudPopover's provider may render the portaled surface after its trigger owner.
            if (--remainingFrames > 0) {
                focusFrame = window.requestAnimationFrame(focus);
            }
        };
        focusFrame = window.requestAnimationFrame(focus);
    };

    const controller = {
        navigationRow,
        close: requestClose,
        synchronize(isEnabled, renderedOpen, hoverOpening, focusFirst, restoreFocus, acknowledgedVersion) {
            if (disposed) {
                return;
            }

            enabled = isEnabled;
            // A render started before a neighbor's close request must not reopen this menu.
            if (acknowledgedVersion >= pointerVersion || !renderedOpen || focusFirst || restoreFocus) {
                open = enabled && renderedOpen;
                openedByHover = hoverOpening;
            }

            if (!open) {
                cancelClose();
                cancelFocus();
            }
            else {
                closeNeighbors();
                if (!openedByHover) {
                    cancelClose();
                }
                if (!isVisible(activator)) {
                    requestClose();
                }
                else if (openedByHover && !activator.matches(":hover") && !popup()?.matches(":hover")) {
                    scheduleClose();
                }
                if (focusFirst) {
                    focusFirstWhenVisible();
                }
            }

            if (enabled && restoreFocus && isVisible(activator)) {
                suppressHover = activator.matches(":hover");
                activator.querySelector("button")?.focus({ preventScroll: true });
            }
        },
        async dispose() {
            if (disposed) {
                return;
            }

            disposed = true;
            cancelClose();
            cancelFocus();
            navigationMenus.delete(controller);
            document.removeEventListener("pointerover", handlePointerOver);
            document.removeEventListener("pointerout", handlePointerOut);
            document.removeEventListener("pointerdown", handlePointerDown, true);
            document.removeEventListener("focusin", handleFocusIn);
            document.removeEventListener("keydown", handleKeyDown);
            window.removeEventListener("resize", handleResize);
            await Promise.allSettled(Array.from(pendingNotifications));
        }
    };

    navigationMenus.add(controller);
    document.addEventListener("pointerover", handlePointerOver);
    document.addEventListener("pointerout", handlePointerOut);
    document.addEventListener("pointerdown", handlePointerDown, true);
    document.addEventListener("focusin", handleFocusIn);
    document.addEventListener("keydown", handleKeyDown);
    window.addEventListener("resize", handleResize);
    return controller;
}
