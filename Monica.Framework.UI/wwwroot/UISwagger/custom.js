/**
 * Adapts the native Swagger UI workbench to Monica's host-owned palette and preferences.
 * Swagger retains its document, request, authorization, disclosure and clipboard state.
 */
(function () {
    'use strict';

    const configurationElement = document.getElementById('mo-swagger-config');
    if (!configurationElement) return;

    let config;
    try {
        config = JSON.parse(configurationElement.textContent);
        if (!Array.isArray(config.themes) || config.themes.length === 0 || !config.defaults) return;
    } catch (error) {
        console.warn('MoSwagger: invalid workbench configuration.', error);
        return;
    }

    const namespace = window.MoSwagger = window.MoSwagger || {};
    namespace.workbench?.dispose();
    const root = document.documentElement;
    const labels = config.labels || {};
    const defaultTheme = config.themes.find(theme => theme.themeKind === config.defaults.themeKind) || config.themes[0];
    const destinations = (config.buttons || []).slice().sort((left, right) => left.order - right.order);
    let selectedTheme = defaultTheme;
    let requestedMode = null;
    let preference = null;
    let storageModule = null;
    let storagePromise = null;
    let active = false;
    let disposed = false;
    let hydrated = false;
    let epoch = 0;
    let hydrationRevision = 0;
    let userModeRevision = 0;
    let frame = 0;
    let navigation = null;
    let nativeButton = null;
    let agreement = null;
    let agreementStartedAt = 0;
    let syncClickPending = false;
    let adapterClick = false;
    let persistAfterSync = false;
    let appliedTheme = null;
    let modal = null;
    let modalOpener = null;
    let pendingOpener = null;
    let modalSequence = 0;

    function attribute(element, name, value) {
        if (value != null && element.getAttribute(name) !== String(value))
            element.setAttribute(name, String(value));
    }

    function schedule() {
        if (active && !frame) frame = requestAnimationFrame(reconcile);
    }

    const contentObserver = new MutationObserver(schedule);
    const modeObserver = new MutationObserver(schedule);

    function getStorageModule() {
        if (!storagePromise) {
            storagePromise = import(config.storage.moduleUrl).then(module => {
                storageModule = module;
                return module;
            }).catch(error => {
                console.warn('MoSwagger: browser storage module unavailable; using host defaults.', error);
                return null;
            });
        }
        return storagePromise;
    }

    async function hydrate() {
        const admittedEpoch = epoch;
        const admittedRevision = ++hydrationRevision;
        const admittedUserRevision = userModeRevision;
        const module = await getStorageModule();
        if (!active || epoch !== admittedEpoch || hydrationRevision !== admittedRevision) return;
        let stored = null;
        try {
            const value = module?.getItem('local', config.storage.key);
            const parsed = value ? JSON.parse(value) : null;
            if (parsed && typeof parsed === 'object' && !Array.isArray(parsed) &&
                Number.isInteger(parsed.themeKind) && typeof parsed.isDarkMode === 'boolean') stored = parsed;
        } catch (error) {
            console.warn('MoSwagger: unreadable theme preference; using host defaults.', error);
        }
        preference = stored;
        selectedTheme = stored
            ? config.themes.find(theme => theme.themeKind === stored.themeKind) || config.themes[0]
            : defaultTheme;
        // A late module import must not undo an already admitted native user toggle.
        if (userModeRevision === admittedUserRevision) {
            requestedMode = typeof stored?.isDarkMode === 'boolean'
                ? stored.isDarkMode : config.defaults.isDarkMode === true;
        }
        persistAfterSync = stored === null;
        hydrated = true;
        agreement = null;
        syncClickPending = false;
        agreementStartedAt = performance.now();
        attribute(root, 'data-mo-swagger-storage-status', module ? 'available' : 'unavailable');
        schedule();
    }

    function persist(mode) {
        persistAfterSync = false;
        if (!storageModule) return;
        preference = { ...preference, themeKind: selectedTheme.themeKind, isDarkMode: mode };
        const failure = storageModule.trySetItem('local', config.storage.key, JSON.stringify(preference));
        attribute(root, 'data-mo-swagger-storage-status', failure || 'saved');
    }

    function syncNavigation() {
        const toolbar = document.querySelector('.swagger-ui .topbar-wrapper');
        if (!toolbar || destinations.length === 0) return;
        if (navigation?.isConnected && navigation.parentElement === toolbar) return;
        navigation?.remove();
        navigation = document.createElement('nav');
        navigation.className = 'mo-swagger-nav-buttons';
        navigation.setAttribute('data-mo-swagger-navigation', '');
        attribute(navigation, 'aria-label', labels.navigation);
        attribute(navigation, 'lang', config.culture);
        for (const destination of destinations) {
            const link = document.createElement('a');
            link.className = 'mo-swagger-nav-btn';
            link.href = destination.href;
            link.textContent = destination.name;
            attribute(link, 'title', destination.description);
            navigation.appendChild(link);
        }
        toolbar.appendChild(navigation);
    }

    function nativeMode(button) {
        const path = button.querySelector('svg path')?.getAttribute('d') || '';
        // Swagger UI 5.31.0 renders different bulb paths for its own React mode state.
        const iconMode = path.startsWith('M12 2a7') ? true :
            path.startsWith('M12 2C9.76') ? false : null;
        return iconMode !== null && iconMode === root.classList.contains('dark-mode') ? iconMode : null;
    }

    function applyTheme(mode, acknowledged = true) {
        const identity = selectedTheme.token + '-' + (mode ? 'dark' : 'light');
        if (appliedTheme !== identity) {
            const properties = mode ? selectedTheme.dark : selectedTheme.light;
            for (const [name, value] of Object.entries(properties)) {
                if (name.startsWith('--mo-swagger-') && root.style.getPropertyValue(name) !== value)
                    root.style.setProperty(name, value);
            }
            appliedTheme = identity;
            attribute(root, 'data-mo-swagger-theme', identity);
            root.classList.add('mo-swagger-workbench');
        }
        attribute(root, 'data-mo-swagger-mode', mode ? 'dark' : 'light');
        if (!acknowledged) {
            root.removeAttribute('data-mo-swagger-native-mode');
            attribute(root, 'data-mo-swagger-mode-status', 'no-native-control');
            return;
        }
        attribute(root, 'data-mo-swagger-native-mode', mode ? 'dark' : 'light');
        attribute(root, 'data-mo-swagger-mode-status', 'ready');
        attribute(nativeButton, 'aria-pressed', mode);
        if (!nativeButton.hasAttribute('aria-label') || nativeButton.hasAttribute('data-mo-swagger-mode-label')) {
            attribute(nativeButton, 'aria-label', mode ? labels.switchToLight : labels.switchToDark);
            attribute(nativeButton, 'title', mode ? labels.switchToLight : labels.switchToDark);
            attribute(nativeButton, 'lang', config.culture);
            attribute(nativeButton, 'data-mo-swagger-mode-label', '');
        }
    }

    function syncMode() {
        if (!hydrated) return;
        const button = document.querySelector('.swagger-ui .dark-mode-toggle > button');
        if (!button?.isConnected) {
            // Native BaseLayout intentionally has no standalone toolbar or mode control.
            if (window.ui?.getConfigs?.().layout === 'BaseLayout' && document.querySelector('.swagger-ui')) {
                requestedMode ??= config.defaults.isDarkMode === true;
                applyTheme(requestedMode, false);
                if (persistAfterSync) persist(requestedMode);
            }
            return;
        }
        if (nativeButton !== button) {
            nativeButton = button;
            agreement = null;
            syncClickPending = false;
            agreementStartedAt = performance.now();
        }
        const mode = nativeMode(button);
        if (mode === null || agreement?.mode !== mode || agreement.button !== button) {
            agreement = mode === null ? null : { button, mode };
            attribute(root, 'data-mo-swagger-mode-status', 'waiting-native-acknowledgement');
            if (performance.now() - agreementStartedAt < 5000) schedule();
            else attribute(root, 'data-mo-swagger-mode-status', 'unacknowledged');
            return;
        }
        if (requestedMode !== null && mode !== requestedMode) {
            if (!syncClickPending) {
                syncClickPending = true;
                agreement = null;
                adapterClick = true;
                try { button.click(); } finally { adapterClick = false; }
            }
            if (performance.now() - agreementStartedAt < 5000) schedule();
            else attribute(root, 'data-mo-swagger-mode-status', 'unacknowledged');
            return;
        }
        // Both native signals agree across frames, including after the native button's own handler.
        const userChange = requestedMode === null;
        requestedMode = mode;
        syncClickPending = false;
        applyTheme(mode);
        if (userChange || persistAfterSync) persist(mode);
    }

    function nameNativeIcons() {
        if (labels.copy) {
            for (const button of document.querySelectorAll('.swagger-ui .copy-to-clipboard > button'))
                if (!button.hasAttribute('aria-label')) attribute(button, 'aria-label', labels.copy);
        }
        for (const button of document.querySelectorAll('.swagger-ui .dialog-ux .close-modal'))
            if (!button.hasAttribute('aria-label')) attribute(button, 'aria-label', labels.close);
    }

    function syncModal() {
        const next = document.querySelector('.swagger-ui .dialog-ux .modal-ux-inner');
        if (next === modal) return;
        if (!next && modal) {
            const focused = document.activeElement;
            if (modalOpener?.isConnected && !modalOpener.disabled &&
                document.visibilityState === 'visible' &&
                (focused === document.body || !focused || modal.contains(focused)))
                modalOpener.focus({ preventScroll: true });
            modalOpener = null;
        }
        modal = next;
        if (!modal) return;
        modalOpener = pendingOpener?.isConnected ? pendingOpener : modalOpener;
        pendingOpener = null;
        const heading = modal.querySelector('.modal-ux-header h3');
        if (heading) {
            if (!heading.id) heading.id = 'mo-swagger-authorization-title-' + ++modalSequence;
            if (!modal.hasAttribute('aria-label') && !modal.hasAttribute('aria-labelledby'))
                attribute(modal, 'aria-labelledby', heading.id);
        }
        if (!modal.hasAttribute('role')) attribute(modal, 'role', 'dialog');
        attribute(modal, 'aria-modal', 'true');
        // The native credential input's autofocus remains the initial focus owner.
    }

    function onClick(event) {
        const target = event.target instanceof Element ? event.target : null;
        const button = target?.closest('button');
        if (!button?.closest('.swagger-ui')) return;
        if (!button.closest('.dialog-ux') && button.matches('.authorize, .authorization__btn'))
            pendingOpener = button;
        if (button.matches('.dark-mode-toggle > button') && !adapterClick) {
            ++userModeRevision;
            requestedMode = null;
            agreement = null;
            syncClickPending = false;
            agreementStartedAt = performance.now();
            schedule();
        }
    }

    function onKeyDown(event) {
        if (!modal?.isConnected || event.isComposing || !modal.contains(document.activeElement)) return;
        if (event.key === 'Escape') {
            const close = modal.querySelector('.close-modal');
            if (close && !close.disabled) {
                event.preventDefault();
                event.stopPropagation();
                close.click();
            }
        } else if (event.key === 'Tab') {
            const targets = [...modal.querySelectorAll('button, input, select, textarea, a[href], [tabindex]')]
                .filter(element => !element.disabled && element.tabIndex >= 0 &&
                    element.getClientRects().length > 0 && getComputedStyle(element).visibility !== 'hidden');
            const first = targets[0];
            const last = targets[targets.length - 1];
            if (event.shiftKey && document.activeElement === first && last) {
                event.preventDefault();
                last.focus();
            } else if (!event.shiftKey && document.activeElement === last && first) {
                event.preventDefault();
                first.focus();
            }
        }
    }

    function reconcile() {
        frame = 0;
        if (!active) return;
        syncNavigation();
        nameNativeIcons();
        syncModal();
        syncMode();
    }

    function onStorage(event) {
        if (active && (event.key === config.storage.key || event.key === null)) void hydrate();
    }

    function start() {
        if (active || disposed || !document.body) return;
        active = true;
        ++epoch;
        hydrated = false;
        nativeButton = null;
        agreement = null;
        requestedMode = null;
        contentObserver.observe(document.body, { childList: true, subtree: true, attributes: true, attributeFilter: ['d'] });
        modeObserver.observe(root, { attributes: true, attributeFilter: ['class'] });
        document.body.addEventListener('click', onClick, true);
        document.addEventListener('keydown', onKeyDown);
        window.addEventListener('storage', onStorage);
        void hydrate();
        schedule();
    }

    function stop() {
        active = false;
        ++epoch;
        contentObserver.disconnect();
        modeObserver.disconnect();
        if (frame) cancelAnimationFrame(frame);
        frame = 0;
        modal = null;
        modalOpener = null;
        pendingOpener = null;
        document.body?.removeEventListener('click', onClick, true);
        document.removeEventListener('keydown', onKeyDown);
        window.removeEventListener('storage', onStorage);
    }

    function dispose() {
        disposed = true;
        stop();
        document.removeEventListener('DOMContentLoaded', start);
        window.removeEventListener('pagehide', stop);
        window.removeEventListener('pageshow', start);
        navigation?.remove();
    }

    namespace.workbench = { dispose };
    window.addEventListener('pagehide', stop);
    window.addEventListener('pageshow', start);
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, { once: true });
    else start();
})();
