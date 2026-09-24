# Web host integration

## Bundle and endpoint lifecycle

```csharp
using Monica.Core.Modularity.Extensions;
using Monica.Modules;

var builder = WebApplication.CreateBuilder(args);
builder.AddMonica(monica =>
{
    monica.AddWebApi();
    monica.AddAuthentication(options =>
    {
        options.Secret = builder.Configuration["Auth:Secret"]
            ?? throw new InvalidOperationException("Auth:Secret is required.");
        options.Issuer = "my-app";
        options.Audience = "my-app";
    });
    monica.AddEventBus().UseNoOpDistributedEventBus(); // Local example only.
    monica.AddProjectUnits();
});
var app = builder.Build();
app.UseMonica();
app.MapMonica();
app.Run();
```

`AddWebApi()` declares the standard infrastructure graph: AutoControllers, AutoModel, conventional DI, Swagger, JWT authentication, mediator, object mapping, repository, and exception handling. It is a bundle, so add individual modules instead when the host does not want that full graph. `AddProjectUnits()` is separate and connects application request units to execution and protocol layers; it requires the EventBus module. The example uses a no-op distributed transport for a local host. Select a real provider if publishing beyond the process matters. Configure type discovery to include the domain and protocol assemblies that contain your controllers and ProjectUnits. `UseMonica()` configures middleware around routing, including module authentication/authorization; `MapMonica()` maps endpoints after it. Repeated or reversed calls fail. A Web-required module fails in a generic host.

## Generated MVC controllers and AutoModel

`AddAutoControllers(configure?, configureCrud?)` discovers `ControllerBase` types and `ICrudApplicationService` implementations from the configured type-discovery scope, validates CRUD options, registers generated MVC conventions, and maps controllers. It depends on AutoModel, the core controller module, and exception handling. If generated controllers are absent, inspect type-discovery assembly selection and CRUD naming/suffix diagnostics before manually adding routes. Invalid CRUD options fail service composition with `OptionsValidationException`.

`AddAutoModel()` registers model snapshots and expression operations. Its optional Web status route is `/auto-model/status` when the common minimal-API switch permits it. `EnableActiveMode` limits AutoModel fields to those marked `AutoField`; most other AutoModel switches control naming, ignored attributes, or diagnostics. Avoid turning on `EnableTitleAsActivateName`: the property is marked obsolete and unimplemented.

ProjectUnit HTTP endpoints follow request-owned metadata and execute through the shared typed execution pipeline. The pipeline is used by other modules too: authorization and execution timing install filtered behaviors for business operations. Do not bypass it with a second ad hoc controller path for the same request contract. `AddExecutionPipeline()` can also be registered directly on a host that needs typed execution without the Web API bundle; `.AddBehavior<TBehavior>(order, descriptorFilter, lifetime)` adds one implementation to the host-owned catalog. The same behavior type registered twice fails composition, and behavior service registrations are checked for exclusivity. Use `ExecutionPipelineCatalogFacade` to inspect the effective behavior plan. For request naming, controller placement, and endpoint ownership, read `$monica-application-project-unit-development`.

## Authentication, authorization, and CORS

`AddAuthentication(options => { ... })` configures JWT bearer validation with `Issuer`, `Audience`, and `Secret`; the option defaults are placeholder values, so supply application-owned settings before accepting tokens. Bearer tokens from query strings are ignored unless `AllowQueryStringAccessTokens(pathPrefixes)` explicitly enables narrowly scoped transport paths.

`AddAuthorization<TEnum>(claimTypeDefinition)` registers the primary permission-bit contract and the authorization execution behavior. It brings in authentication, localization, exception mapping, and the execution pipeline. A Web host is required. Additional enums use `AddPermissionBit<TEnum>(claimTypeDefinition)`. `ConfigAsAlwaysAllow()` replaces permission and authorization checks, so reserve it for explicitly trusted hosts. `AddCors().ConfigureDefaultPolicy(...)` or `.AllowAll()` configures CORS before authentication in Monica's module order. Choose `AllowAll()` only when that origin policy is intentional.

`AddSwagger()` can be configured independently; `AddWebApi()` already depends on it. The shared module-system options include `EnableMinimalApiByDefault` and API-group defaults. A module deriving from `MinimalApiModuleOptions<T>` follows that common switch unless explicitly overridden, so a missing diagnostic route may be intentionally disabled even while the module's services are present.

## Source checks

Verify bundle membership and exact APIs in `Monica.WebApi/Modules/ModuleWebApi.cs`, `ModuleAutoControllers.cs`, `ModuleSwagger.cs`, `Monica.AutoModel/Modules/ModuleAutoModel.cs`, `Monica.Authority/Modules/ModuleAuthentication.cs`, `ModuleAuthorization.cs`, `ModuleCors.cs`, and `Monica.ProjectUnits/Modules/ModuleProjectUnits.cs`. `examples/Monica.ReferenceApplication/src/AppHost/Monica.Reference.Api/Program.cs` shows a Web host that includes the bundle and ProjectUnits.
