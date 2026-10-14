// The standalone Guide shell owns these DOM preferences; the engine owns setup state.
(function () {
    const storageKey = "mw-setup-theme";
    let stored = null;
    try { stored = localStorage.getItem(storageKey); } catch { }
    document.documentElement.dataset.theme = stored === "dark" ? "dark" : "light";
    window.monicaGuideShell = {
        currentTheme: () => document.documentElement.dataset.theme,
        toggleTheme: () => {
            const next = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
            document.documentElement.dataset.theme = next;
            try { localStorage.setItem(storageKey, next); } catch { }
            return next;
        },
        setLanguage: language => { document.documentElement.lang = language; },
        focusMain: expectedLocation => {
            const expected = new URL(expectedLocation, document.baseURI);
            if (expected.origin !== location.origin || expected.pathname !== location.pathname || expected.search !== location.search) return;
            const main = document.getElementById("guide-main");
            if (!main?.isConnected) return;
            main.focus({ preventScroll: true });
            main.scrollIntoView({ block: "start", inline: "nearest", behavior: "auto" });
        }
    };
})();
