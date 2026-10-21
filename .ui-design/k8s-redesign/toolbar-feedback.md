# K8S inventory toolbar feedback

The resource-kind selectors and filter predicates now preserve the prototype's distinct visual hierarchy. Kind selectors use 36px controls, 13.5px labels, 8px square role markers, 9px corners and inset 12px monospace count badges. Lower filters use 28px pills, 12.5px labels, 8px circular markers and plain 11.5px counts. Selected controls retain their semantic soft fill, including neutral filters. The changes are local to K8S; shared filter controls remain unchanged. Phone owners scroll each control rail without widening the document.

Clear filters is available beside the filter pills and in the no-matching-results state. It clears search, exposure, attention, unbound, restart/start and native DataGrid column predicates, then returns to the first page. It preserves namespace, resource kind, selected resources, sort order and the Dapr sidecar folding preference. Clearing uses the current captured data; the controlled SSH ledger stayed at 80 commands before and after a search/clear sequence. Browser search preferences are updated. The toolbar action retains keyboard focus; empty-state recovery moves focus to the native search input before its button disappears.

Verification completed:

- Final `dotnet build Monica.slnx -m -c Release`: zero warnings and errors; the standard runner passed all 30 default test projects. No UI rendering tests were added or run.
- Theme-token, strict localization, knowledge-impact, skill validation/projection and all four skill-owned test suites passed.
- Browser checks covered combined quick/search/time/column predicates, native-column-only and search-only recovery, first-page reset, exact retained selection/sort, both sidecar fold preferences, all four resource kinds, persisted cleared search and an empty namespace with four visible zero count badges.
- Default/Grove light and dark plus Material Design 3 light passed at desktop/intermediate/400px owners. Actual upper/lower geometry matches the sizes above; selected hover preserves fill. The minimum sampled small-text contrast was 5.10:1. At 400px the document remained 400px wide and native Tab navigation scrolled Clear filters fully into view. Fresh functional browser checks reported zero warnings or errors.

The isolated preview at `http://127.0.0.1:5576/ui/k8s` runs the actual Monica UI, facade, parser and SSH provider against a finite SSH/kubectl protocol fixture with 30 Services and 25 workloads. It does not establish Kubernetes cluster behavior. The user's service on port 5248 and the earlier preview on 5575 were not restarted or replaced.

Screenshots are in [toolbar-captures](toolbar-captures/). Raw browser evidence and source/runtime hashes remain in `.tmp/monica-ui-bridge-debug/k8s-toolbar/`; the preview run is `runs/fb887947-69dc-455a-90d4-425969d24f26`.
