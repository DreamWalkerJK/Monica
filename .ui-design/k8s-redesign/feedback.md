# K8S prototype feedback

The outlined buttons and Pod badges in the initial implementation were local styling choices. Neither the Monica UI skills nor MudBlazor requires transparent controls. The existing visual language calls for soft semantic surfaces; its owning reference now explicitly preserves accepted prototype fills during contrast repairs and treats outlined/text paint as action-hierarchy choices.

## Changes

- Restart/start groups now use full-row buttons with hover and persistent pressed feedback. Small groups retain the prototype's warning ring; groups of at least five workloads use a filled warning node with a soft halo. The hover uses a palette-derived surface rather than an undefined theme variable.
- Right-hand comparison bars use the largest group in the complete capture, including collapsed groups, as a common denominator. Segments show actual workload kinds. Expanding older groups preserves the scale; selection follows the parent interval filter, survives refresh, and clears with the filter or a kind change.
- The native inspector tab bar has the prototype's inset and label padding. Native slider geometry remains exact by avoiding an extra wrapper gap that MudTabs excludes from its width calculation.
- Inspector operation buttons use soft warning, error and primary surfaces. Running/phase and readiness badges retain their distinct semantic roles with soft fills. Pod status dots reuse the shared semantic dot and halo. Controls remain native MudBlazor components, including native disabled behavior and focus ownership.

## Verification

- Final Release solution build: zero warnings and errors. The standard runner passed all 30 default test projects; the final follow-up edits changed only CSS. No UI rendering tests were added or run.
- Theme-token, strict localization, knowledge-impact, skill validation/projection and all four skill-owned test suites passed.
- Browser smoke checks covered Default/Grove light and dark, Material Design 3 light, both shell layouts, English/Chinese, and desktop/intermediate/400px phone widths. The document and wave rows remain contained. Final native underline position and width agree with the tabs within 0.016px. Inspector badge/button text contrast was 7.46–10.40:1 in the checked Default/Grove modes. Fresh final browser sessions reported zero console warnings or errors.
- Wave checks used four groups of 1, 2, 3 and 6 workloads. The hidden largest group establishes the initial 1/6, 2/6 and 3/6 lengths; expanding reveals all three workload-kind segments. Whole-row hover, persistent selection, refresh retention, filter clearing and kind switching passed.
- Native inspector Escape and review-Cancel paths return focus to their exact openers. DaemonSet replica actions retain native disabled treatment.

The isolated preview at `http://127.0.0.1:5575/ui/k8s` runs the actual Monica UI, parsing, facade and SSH provider against a finite SSH/kubectl protocol fixture. These checks do not establish behavior of a Kubernetes cluster. The user's running service on port 5248 was not restarted or replaced.

Final screenshots are in [feedback-captures](feedback-captures/). Raw browser measurements and source/runtime provenance remain in `.tmp/monica-ui-bridge-debug/k8s-feedback/`; the final preview run is `runs/a7df5e93-a15a-47bf-85c4-07bc8dcaa58f`.
