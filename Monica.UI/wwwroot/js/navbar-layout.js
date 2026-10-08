function getGapWidth(element) {
    const styles = window.getComputedStyle(element);
    const gapValue = styles.columnGap !== "normal" ? styles.columnGap : styles.gap;
    const parsedGap = Number.parseFloat(gapValue);
    return Number.isFinite(parsedGap) ? parsedGap : 0;
}

function measureCategoryWidths(measurementRail) {
    const categoryWidths = Array.from(
        measurementRail.querySelectorAll("[data-nav-measure='category']"),
        (element) => element.getBoundingClientRect().width);

    const moreButton = measurementRail.querySelector("[data-nav-measure='more']");
    const moreWidth = moreButton?.getBoundingClientRect().width ?? 0;

    return { categoryWidths, moreWidth };
}

function calculateVisibleCategoryCount(desktopNav, measurementRail, maxVisibleCategories, safetyMarginPx) {
    const availableWidth = desktopNav.getBoundingClientRect().width - safetyMarginPx;

    if (availableWidth <= 0) {
        return 0;
    }

    const { categoryWidths, moreWidth } = measureCategoryWidths(measurementRail);
    const totalCategories = categoryWidths.length;
    const maxVisibleCount = Math.min(maxVisibleCategories, totalCategories);
    const gapWidth = getGapWidth(desktopNav);

    for (let visibleCount = maxVisibleCount; visibleCount >= 0; visibleCount -= 1) {
        const needsMoreButton = totalCategories > visibleCount;
        const visibleCategoryWidth = categoryWidths
            .slice(0, visibleCount)
            .reduce((sum, width) => sum + width, 0);

        const renderedItemCount = visibleCount + (needsMoreButton ? 1 : 0);
        const renderedGapWidth = Math.max(0, renderedItemCount - 1) * gapWidth;
        const requiredWidth = visibleCategoryWidth
            + (needsMoreButton ? moreWidth : 0)
            + renderedGapWidth;

        if (requiredWidth <= availableWidth) {
            return visibleCount;
        }
    }

    return 0;
}

export async function focusContextNavigation(tree) {
    const previousFocus = document.activeElement;
    // Inherited visibility can remain hidden until the drawer's opening transition resolves.
    while (tree.isConnected && document.activeElement === previousFocus &&
        tree.closest(".mo-context-navigation.mud-drawer--open") &&
        !window.matchMedia("(max-width: 929px)").matches) {
        const target = window.getTabbableElements(tree)[0];
        if (!target) {
            return;
        }

        if (window.getComputedStyle(target).visibility === "visible" && target.getClientRects().length > 0) {
            target.focus({ preventScroll: true });
            return;
        }

        await new Promise(resolve => window.requestAnimationFrame(resolve));
    }
}

export function createNavBarLayoutObserver(
    desktopNav,
    measurementRail,
    maxVisibleCategories,
    safetyMarginPx,
    dotNetRef) {
    let disposed = false;
    let frameId = 0;
    let lastVisibleCount = -1;
    let lastMobile = null;
    const mobileViewport = window.matchMedia("(max-width: 929px)");
    const pendingNotifications = new Set();

    const notify = (method, ...args) => {
        const notification = dotNetRef.invokeMethodAsync(method, ...args).catch(() => {
            // A disconnected circuit can reject a callback already queued before observer disposal.
        }).finally(() => {
            pendingNotifications.delete(notification);
        });
        pendingNotifications.add(notification);
    };

    const notifyVisibleCount = () => {
        if (disposed) {
            return;
        }

        const visibleCount = calculateVisibleCategoryCount(
            desktopNav,
            measurementRail,
            maxVisibleCategories,
            safetyMarginPx);

        const isMobile = mobileViewport.matches;
        if (visibleCount === lastVisibleCount && isMobile === lastMobile) {
            return;
        }

        lastVisibleCount = visibleCount;
        lastMobile = isMobile;
        notify("UpdateVisibleCategoryCountAsync", visibleCount, isMobile);
    };

    const handleFocusIn = (event) => {
        if (disposed || mobileViewport.matches || !(event.target instanceof Element)) {
            return;
        }

        const layout = desktopNav.closest(".mo-layout");
        const context = layout?.querySelector(".mo-context-navigation.mud-drawer--open");
        if (!context || context.getClientRects().length === 0) {
            return;
        }

        const styles = window.getComputedStyle(context);
        if (styles.display === "none" || styles.visibility === "hidden") {
            return;
        }

        const sidebar = layout.querySelector(".mo-sidebar");
        if (!context.contains(event.target) && !sidebar?.contains(event.target)) {
            notify("CloseContextNavigationOnFocusExitAsync");
        }
    };

    const scheduleMeasurement = () => {
        if (disposed) {
            return;
        }

        if (frameId !== 0) {
            window.cancelAnimationFrame(frameId);
        }

        frameId = window.requestAnimationFrame(() => {
            frameId = 0;
            notifyVisibleCount();
        });
    };

    const resizeObserver = new ResizeObserver(() => {
        scheduleMeasurement();
    });

    resizeObserver.observe(desktopNav);
    resizeObserver.observe(measurementRail);
    window.addEventListener("resize", scheduleMeasurement);
    window.addEventListener("focusin", handleFocusIn);

    scheduleMeasurement();

    return {
        async dispose() {
            if (disposed) {
                return;
            }

            disposed = true;

            if (frameId !== 0) {
                window.cancelAnimationFrame(frameId);
                frameId = 0;
            }

            resizeObserver.disconnect();
            window.removeEventListener("resize", scheduleMeasurement);
            window.removeEventListener("focusin", handleFocusIn);
            await Promise.allSettled(Array.from(pendingNotifications));
        }
    };
}
