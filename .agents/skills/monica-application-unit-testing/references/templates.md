# Sociable Application Test Templates

These templates use `UserService.API` as a neutral example. Keep the real runnable project name equal to `Test.` plus the exact production project stem.

## Project Factory

```csharp
public sealed class UserServiceTestApplicationFactory
    : MonicaTestApplicationFactory<CommandHandlerUserLogin>
{
    protected override void ConfigureHost(WebApplicationBuilder builder)
    {
        builder.Environment.EnvironmentName = Environments.Development;
    }

    protected override void ConfigureMonica(IMonicaBuilder monica)
    {
        monica.AddUserService(options => options.EnableExternalNotifications = false);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.UseTestDatabase<UserDbContext>(DatabaseIsolation.PerScopeDatabase);
        services.RemoveAll<IExternalUserDirectory>();
        services.AddSingleton<IExternalUserDirectory, StubExternalUserDirectory>();
    }
}
```

The factory is a stateless recipe. `CreateAsync(...)` builds a new host; do not cache an application or provider on the factory.

## Command Handler Scenario

```csharp
public sealed class CommandHandlerUserLoginTests(
    UserServiceTestApplicationFactory factory)
    : IClassFixture<UserServiceTestApplicationFactory>
{
    private readonly UserServiceTestApplicationFactory _factory = factory;

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ShouldIssueTokenWithBusinessClaims()
    {
        var jwt = Substitute.For<IJwtAuthManager>();
        var expectedToken = CreateJwtAuthResult("exam01");
        Claim[] issuedClaims = [];

        jwt.GenerateTokens("exam01", Arg.Do<Claim[]>(claims => issuedClaims = claims), Arg.Any<DateTime?>())
            .Returns(expectedToken);

        await using var application = await _factory.CreateAsync(
            scenario => scenario.With<IJwtAuthManager>(jwt),
            TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        await SeedLoginUserAsync(scope);

        var handler = scope.Resolve<CommandHandlerUserLogin>();
        var result = await handler.Handle(
            new CommandUserLogin
            {
                Username = "exam01",
                Password = "pass123",
                GrantType = EGrantType.PasswordPlain
            },
            scope.CancellationToken);

        var data = result.ShouldSucceed();
        data!.AccessToken.Should().Be(expectedToken.AccessToken);
        issuedClaims.Should().Contain(claim =>
            claim.Type == AuthorityClaimTypes.Username && claim.Value == "exam01");
        jwt.Received(1).GenerateTokens("exam01", Arg.Any<Claim[]>(), Arg.Any<DateTime?>());
    }
}
```

The replacement callback changes the service collection before `Build()`. `CreateScope()` only creates a child scope.

This shape resolves the handler directly, so the auto-controller execution pipeline (including its request-level unit of work) does not run. It is fine for handlers that only read. When the handler writes and the operation must commit as one business transaction, resolve it inside `scope.InvokeAsync` instead:

```csharp
var result = await scope.InvokeAsync(async s =>
{
    var handler = s.Resolve<CommandHandlerCreateUser>();
    return await handler.Handle(command, s.CancellationToken);
});
```

## Repository Read Scenario

```csharp
public sealed class RepositoryUserTests(
    UserServiceTestApplicationFactory factory)
    : IClassFixture<UserServiceTestApplicationFactory>
{
    private readonly UserServiceTestApplicationFactory _factory = factory;

    [Fact]
    public async Task GetUserInfo_WhenUserExists_ShouldReturnUserWithOrganUnit()
    {
        await using var application = await _factory.CreateAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        await scope.SeedAsync(
            new OrganUnit { Id = 20, OrganName = "Test Tower", Code = "ZBAA-TWR" },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "exam01",
                Nickname = "Exam User",
                OrganUnitId = 20
            });

        var repository = scope.Resolve<IRepositoryUser>();
        var user = await repository.GetUserInfo("exam01");

        user.Should().NotBeNull();
        user!.OrganUnit.Should().NotBeNull();
    }
}
```

## Repository Write Scenario

Wrap write actions in `scope.InvokeAsync` so they run with request-shaped unit-of-work semantics: staged repository writes are saved when the action succeeds and committed by the unit of work. Persistence concepts (soft delete, audit stamping) apply without any manual context work.

```csharp
public sealed class RepositoryPermissionTests(
    UserServiceTestApplicationFactory factory)
    : IClassFixture<UserServiceTestApplicationFactory>
{
    private readonly UserServiceTestApplicationFactory _factory = factory;

    [Fact]
    public async Task DeleteAsync_WhenPermissionExists_ShouldSoftDelete()
    {
        await using var application = await _factory.CreateAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = application.CreateScope(TestContext.Current.CancellationToken);
        await scope.SeedAsync(new Permission { Id = 201, Name = "user:view" });

        await scope.InvokeAsync(async s =>
        {
            var repository = s.Resolve<IRepositoryPermission>();
            var loaded = await repository.FindAsync(p => p.Name == "user:view", s.CancellationToken);
            await repository.DeleteAsync(loaded!, s.CancellationToken);
        });

        var context = await scope.GetDbContextAsync<UserDbContext>();
        var stored = await context.Permissions.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(p => p.Id == 201, TestContext.Current.CancellationToken);
        stored.IsDeleted.Should().BeTrue();
        stored.DeletionTime.Should().NotBeNull();
    }
}
```

Use the generic overload (`await scope.InvokeAsync(async s => { ...; return result; })`) when the test asserts on the action's return value.

## Module Composition Scenario

```csharp
public sealed class UserServiceModuleTests(
    UserServiceTestApplicationFactory factory)
    : IClassFixture<UserServiceTestApplicationFactory>
{
    private readonly UserServiceTestApplicationFactory _factory = factory;

    [Fact]
    public async Task Module_WhenHostStarts_ShouldExposeExpectedComposition()
    {
        await using var application = await _factory.CreateAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        application.Application.Should().BeSameAs(
            application.Services.GetRequiredService<MonicaApplication>());
        application.ModuleSnapshots.Should().Contain(snapshot =>
            snapshot.ModuleType == typeof(ModuleUserService));
        application.Services.GetService<IRepositoryUser>().Should().NotBeNull();
    }
}
```

## Raw ProjectUnit Fast Path

```csharp
public sealed class QueryHandlerUserCheckTests
{
    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldReturnBadRequest()
    {
        await using var fixture = ProjectUnitFixture<QueryHandlerUserCheck>
            .Builder()
            .WithSubstitute<IRepositoryUser>(out var repository)
            .Build();

        repository.GetUserInfo("missing").Returns(Task.FromResult<User?>(null));

        var result = await fixture.Unit.Handle(
            new QueryUserCheck { Username = "missing" },
            CancellationToken.None);

        result.ShouldFail(ResStatus.BadRequest, "user does not exist");
        result.Data.Should().BeNull();
    }
}
```

This is raw Microsoft DI activation. Use it only when module registration, options, proxies, interceptors, hosted lifecycle, and host isolation are outside the assertion.

## Entity Invariant

```csharp
public sealed class UserTests
{
    [Fact]
    public void Create_WhenRequiredValuesAreValid_ShouldPreserveIdentity()
    {
        var user = User.Create("exam01", "Exam User");

        user.Username.Should().Be("exam01");
        user.Nickname.Should().Be("Exam User");
    }
}
```
