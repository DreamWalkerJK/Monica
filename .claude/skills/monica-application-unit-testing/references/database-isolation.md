# Database Isolation

One MonicaTestApplication owns one scenario database for each registered test context type. Arrange, act and assert deliberately cross scope boundaries.

## Default SQLite Scenario

Register the production context/module first, then call services.UseTestDatabase<TContext>() in the factory's ConfigureServices. This replaces provider options while retaining the production context, provider, repository and transaction registration.

A host-owned keeper connection preserves a uniquely named in-memory SQLite database. Each operation resolves a fresh context and connection. The factory creates schema before host startup. Disposing the scenario releases the database; another scenario never shares it.

Use SeedAsync<TContext,TResult> for explicit setup/save and returned keys, ExecuteAsync for production-pipeline writes, and VerifyAsync<TContext> for fresh-scope assertions. There is no rollback-on-dispose test wrapper around every scope. The real operation boundary must establish rollback.

Supply required EF options/interceptors using UseTestDatabase's configureOptions callback. Do not replace IDbContextProvider with a semantically different test implementation.

## Real Provider

Use UseRealTestDatabase<TContext>((services, options) => ...) when provider-specific SQL, transaction isolation or sharding is part of the assertion. This helper replaces options only; it does not create or clean schema.

Use unique database/schema names, keep credentials outside source control, and dispose resources with the scenario. If isolation is impossible, serialize only tests sharing that named resource. SQLite success does not validate ShardingCore or production-dialect behavior.

## Multiple Contexts

Seed and verify by explicit context type. Select the business operation's participant with UnitOfWorkScopeOptions when multiple contexts are registered for writes. Several contexts can share one local transaction only through the same DbConnection instance and provider. The ordinary SQLite scenario helper gives contexts separate connections and does not imply multi-context atomicity.

Independent stores and shards require a separately designed consistency workflow, not sequential commits.
