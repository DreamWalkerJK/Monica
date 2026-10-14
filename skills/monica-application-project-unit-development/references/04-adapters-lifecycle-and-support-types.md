# Adapters, Lifecycle Units, and Support Types

These choices supplement the [unit catalog](00-project-unit-overview.md) within the existing architecture profiles. The project specification records their concrete placement and adopted conventions. Native discovery support does not authorize using an adapter to bypass domain invariants or inventing a new role.

## Bounded CRUD

`CrudApplicationService<...>` implements `ICrudApplicationService` and is classified as `CrudApplicationService`, separately from request-owned mediator handlers. Use it only for a persistence-facing CRUD surface whose generic mapping and actions match the use case. Commands with meaningful state transitions use handlers and entity methods; they must not gain a generic update path that bypasses those invariants.

This adapter may consume the same concrete repository's `IEfEntityStore` view because the native CRUD contract requires EF access. The [repository rules](13-repository-and-persistence-template.md) bound this exception: ordinary handlers, jobs, and domain services still inject `IRepository{Aggregate}`. Do not widen the business interface or add a forwarding repository layer.

Use the project's adopted CRUD naming convention. `CrudControllerOption.CrudControllerPostfix` defaults to `CrudService` and is stripped to derive the route name; it is a route aid, not a native ProjectUnit naming requirement or a registration gate. Registration follows `ICrudApplicationService` and host type discovery. Use [Web endpoint integration](../../monica-infra-web/references/endpoints-and-execution.md) for setup and the persistence transaction contract for execution.

## Startup Seeder

Use `ISeeder`, normally `SeederBase`, for finite, idempotent startup preparation with dependency/readiness ordering. It is a discovered `Seeder`; the framework imposes no prefix convention. Place domain preparation with its domain and solution-wide preparation in the existing solution infrastructure boundary. Keep domain writes through their owning repositories and the established execution scope.

Use [startup seeding](../../monica-infra-jobs/references/startup-seeding.md) for registration, dependencies, policy, retries, and readiness. A repeating task is a recurring job; a perpetual connection or listener is a hosted service. Do not build a second startup coordinator.

## Hosted Service

Use a hosted service when work owns host start/stop or continuous infrastructure activity. `IHostedService` is discoverable as `HostedService`; Monica's `MoHostedService` and `MoBackgroundService` additionally supply observed lifecycle and finite work-item execution points. Use [hosted-service integration](../../monica-infra-hosting/references/hosted-services.md) for registration and scope/lifecycle semantics.

Register one host-owned instance. Do not retain scoped repositories in a long-lived worker; finite work runs in the native work-item scope. Business behavior stays in its owning entity/service. Choose a scheduler job when the requirement is independent scheduled/triggered execution with history and operator control.

## Controller and Internal Mediator Handler

Request-owned `ApplicationService` endpoints are the normal HTTP use-case shape. A handwritten `ControllerBase` is a discovered `HttpApi` transport adapter when HTTP-specific behavior warrants it. Keep it thin and in the delivery/application boundary, with domain rules elsewhere. Generated mediated controllers remain generator-owned; do not duplicate them or attach MVC attributes to their handlers.

For an internal operation, implement `Monica.Core.Mediator.IRequestHandler<TRequest, TResult>` with the normal transient dependency marker. It has no HTTP endpoint and is not classified as `ApplicationService` merely because its name contains `Handler`. An `IResultRequest` input remains a discoverable `RequestDto`; a plain mediator `IRequest` does not. Follow the [application-service rules](10-application-service-template.md) and [execution guidance](../../monica-infra-web/references/endpoints-and-execution.md) for the actual invocation boundary. Do not omit `[ApiEndpoint]` from an `ApplicationService` as an HTTP opt-out or assume a direct method call invokes the pipeline.

## Facade and Other Support Types

A `*Facade` is a public UI/API entry point owned by a domain, returning `Res` or `Res<T>` where appropriate. UI calls the Facade; it does not gain access to a domain's persistence internals. Preserve the public result boundary while internal services use ordinary .NET returns and exceptions. A Facade alone has no discovered role; its actual inheritance/contracts determine discovery.

Value objects, DTOs, mappers, validators, pure helpers, and composition modules are allowed support types when they have a concrete responsibility. Keep them at that responsibility's owner. Composition-only hosts may contain modules and registration adapters; this permits wiring, not domain behavior. Do not introduce a support-type base family, service toolbox, scanner, or alternate provider/transaction framework without the shared specification's evolution decision.
