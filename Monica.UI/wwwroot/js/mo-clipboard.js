(function() {
    window.MoClipboard = window.MoClipboard || {};

    window.MoClipboard.copyText = async function(text) {
        if (!text) return false;
        try {
            if (navigator.clipboard && navigator.clipboard.writeText) {
                await navigator.clipboard.writeText(text);
                return true;
            }
            // Fallback for HTTP (non-secure context)
            const textArea = document.createElement("textarea");
            textArea.value = text;
            textArea.style.cssText = "position:fixed;left:-999999px;top:-999999px";
            const previousFocus = document.activeElement;
            try {
                document.body.appendChild(textArea);
                textArea.focus();
                textArea.select();
                return document.execCommand('copy');
            } catch {
                return false;
            } finally {
                // Restore only focus borrowed by this synchronous fallback. A copy-event handler
                // that deliberately moved focus elsewhere keeps its own focus choice.
                const restoreFocus = document.activeElement === textArea;
                textArea.remove();
                if (restoreFocus && previousFocus?.isConnected) {
                    previousFocus.focus({ preventScroll: true });
                }
            }
        } catch {
            return false;
        }
    };
})();
