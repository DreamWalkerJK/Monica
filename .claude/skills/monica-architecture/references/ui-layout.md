# UI module placement and page decomposition

Use a standalone `Monica.{Name}.UI` project when UI packaging or ownership is distinct. Keep route pages under `Pages/` and supporting components, dialogs, view models, state, and formatters in root folders for one small UI feature. In a composite UI project, group each feature under `UI{Name}/Components`, `Dialogs`, `Models`, `State`, and `Support`; keep route pages and localization at project level. A lightweight mixed module can put its UI beside infrastructure in the same project, but UI must still reach business behavior through Facades, not `Services/` or `Providers/`.

`State/` owns page/session selection, loading, browser persistence, and polling state. `Support/` owns UI-only resolvers, formatters, and coordinators. Reuse public infrastructure models where possible. A UI module should not add a data-access service wrapper around a Facade.

Keep a page as a composition shell. Extract a section into a component when it has its own visual responsibility; move selection, loading, polling, or concurrency into state; move display transformation into support. A page accumulating many private fields, guards, duplicated loading markup, or cancellation primitives is a useful signal to decompose. Approximate line counts can prompt review but are not correctness limits.

The conventional names are `Module{Name}UI`, `UI{Name}/`, and `UI{Name}[Action]Page`. For async Facade-calling state and cleanup, see [page-state-pattern.md](page-state-pattern.md); for an actual large-page refactor, see [refactoring-examples.md](refactoring-examples.md).
