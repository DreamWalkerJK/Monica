# K8S redesign implementation review

The implementation follows this prototype's namespace identity band, readiness signature, restart/start groups, exposure distribution, compact inventory and right-edge inspector. The production module retains its registered inventory/runtime-configuration routes, host access policies, guarded operation previews, localized copy, native grid sorting/filtering/selection/paging, and explicit visit retirement.

## Implemented behavior

- One complete namespace read supplies Services, Deployments, StatefulSets, DaemonSets and their resolved workload/Pod relationships. Switching kind reuses that evidence. A failed constituent read retains the preceding complete capture and selected identities.
- Workload squares open real inspections. Partial readiness uses Warning, zero ready replicas uses Error, and stopped/empty evidence uses Neutral. Service attention checks each backing workload rather than allowing aggregate replica counts to mask a deficit.
- Restart/start groups span at most two minutes and filter the inventory; Service filtering checks every captured backing workload. Exposure marks filter the Service list. Dapr-only port Services are folded by default, with an explicit toggle. Unknown Service types remain counted under Other.
- The inspector uses a native temporary drawer with Summary, Pods and raw Describe tabs, relation navigation, nested Pod inspection, and an operation footer. It fills the viewport, contains keyboard focus and preserves the exact opener. Closing cancels/drains owned Pod reads; route and namespace changes retire it.
- Review dialogs expose requested/eligible/ignored counts, ignored reasons, workload identities, replica transitions, recorded replica counts and exact command arguments. Execution and subsequent reads retain distinct outcomes. A completed command does not establish Pod readiness. Successful mutations refresh an open inspector.
- Runtime configuration retains the existing draft, save/discard, masked password, connection and lifetime rules.

## Native verification

The preview is a real Monica host using the production facade, services, JSON parser and SSH.NET provider, connected only to an isolated external SSH/kubectl protocol fixture. It is **not a Kubernetes cluster**. Fixtures prove UI/provider behavior and emitted commands; they do not prove controllers, rollout convergence, RBAC or real-cluster connectivity. No existing cluster or credentials were used.

- `dotnet build Monica.slnx -m -c Release`: zero warnings and errors.
- `pwsh -File scripts/run-tests.ps1 -NoBuild`: all 30 default test projects passed. No UI rendering tests were added or run.
- Strict localization: zero missing/unused keys, JSON errors or language mismatches. Theme-token, skill, projection, skill-owned behavior and knowledge-impact validators passed.
- Native search, empty results, sort, kind switching, paging, selection retention, hidden selection, whole-row restart filtering, empty/missing namespaces and injected read failure were exercised.
- Native inspector focus trapping, first Escape/backdrop dismissal, exact opener restoration, relation navigation, nested Pod Describe and preview Cancel were exercised. In the final runtime, confirming restart from the inspector updated its restart evidence from the captured October 1 value to the new command time while readiness remained 2/3.
- The protocol fixture recorded only the intended target's restart/annotation/scale mutations during the operation round trip; desired replicas returned from 3 to 0 to 3 while independently observed ready replicas remained 2. Runtime configuration edits were discarded, with no save and no password reveal.
- Default and Grove light/dark, Material Design 3 light, both shell layouts, English/Chinese and phone/intermediate/desktop owners were checked. At 599/600px the native table stacks; at 601px it keeps a 49px header and one intentional horizontal table scroller. The document itself stays contained. Final sampled working-text contrast was at least 4.70:1 across the checked modes.
- The drawer was checked at 400x860 and 1440x1000; confirmation dialogs at desktop and 390px. Native fresh-session consoles reported zero errors/warnings.

## Evidence

Selected final native captures are in `implementation-captures/`: desktop, dark, phone, inspector, review and pager. Full raw browser receipts, controlled command/state evidence and runtime hashes remain in `.tmp/monica-ui-bridge-debug/k8s-redesign/`. Final preview run: `runs/14559766-ecb2-4e18-80d9-c8f7a996f06f`.

The existing host-wide MudBlazor default still owns built-in sort/filter/select-all translations; K8S content, pager and row selection labels use module localization. NodePort values come from captured resources; the redesign does not invent external endpoint URLs from the SSH execution-node address.
