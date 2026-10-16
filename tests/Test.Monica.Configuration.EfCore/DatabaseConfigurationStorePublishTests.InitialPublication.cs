using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Monica.Configuration.Abstractions;
using Monica.Configuration.EfCore.DbContext;
using Monica.Configuration.EfCore.Entities;
using Monica.Configuration.Models;
using Monica.Configuration.Serialization;
using Xunit;

namespace Test.Monica.Configuration.EfCore;

public sealed partial class DatabaseConfigurationStorePublishTests
{
    [Fact]
    public async Task PublishAsync_WhenInitialPublicationIsReadInFreshScope_ShouldReturnNullPriorSchemaAndExactEvidence()
    {
        var databasePath = await CreateMigratedDatabaseAsync();
        await using var stores = CreateStores(databasePath, 1);
        var definition = CreateDefinition("Test.InitialPublication.FreshRead") with { SchemaVersion = 7 };
        var publisher = CreatePublisher("Test.InitialPublication.Service", "initial:1");
        var expectedSchema = ConfigurationDefinitionSchemaCodec.SerializeSchema(definition);

        await stores.Items[0].PublishAsync(
            CreatePublication(publisher, [definition]),
            TestContext.Current.CancellationToken);

        var persisted = await ReadInitialPublicationHistoryRowsAsync(databasePath, definition.DefinitionKey);
        persisted.Should().ContainSingle();
        var row = persisted[0];
        var overview = await ReadInitialPublicationOverviewInFreshScopeAsync(databasePath, definition.DefinitionKey);
        overview.DefinitionKey.Should().Be(definition.DefinitionKey);
        overview.DefinitionRevision.Should().Be(1);
        overview.SchemaVersion.Should().Be(7);
        overview.PublisherStates.Should().ContainSingle();
        overview.PublisherStates[0].PublisherKey.Should().Be(publisher.PublisherKey);
        overview.RevisionHistories.Should().ContainSingle();
        var history = overview.RevisionHistories[0];
        row.HistoryId.Should().NotBeNullOrWhiteSpace();
        history.HistoryId.Should().Be(row.HistoryId);
        history.DefinitionKey.Should().Be(definition.DefinitionKey);
        history.SectionPath.Should().Be(definition.SectionPath);
        history.DisplayName.Should().Be(definition.DisplayName);
        history.FromProject.Should().Be(definition.FromProject);
        history.Category.Should().Be(definition.Category);
        history.ChangeKind.Should().Be(ConfigurationDefinitionPublishChangeKind.Created);
        history.DefinitionRevision.Should().Be(1);
        history.NewSchemaVersion.Should().Be(definition.SchemaVersion);
        history.NewSchemaHash.Should().Be(definition.SchemaHash);
        history.NewSchemaJson.Should().Be(expectedSchema);
        history.ChangeSummaryJson.Should().Be(row.ChangeSummaryJson);
        history.PublisherId.Should().Be(publisher.InstanceId);
        history.PublisherName.Should().Be(publisher.Name);
        history.PublisherVersion.Should().Be(publisher.Version);
        history.PublishedTime.UtcDateTime.Should().Be(row.PublishedTime);
        history.PreviousSchemaVersion.Should().BeNull();
        history.PreviousSchemaHash.Should().BeNull();
        history.PreviousSchemaJson.Should().BeNull();
    }

    [Fact]
    public async Task PublishAsync_WhenInitialPublicationSchemaChangesAndRepeats_ShouldRetainFullSchemasAndStableHistory()
    {
        var databasePath = await CreateMigratedDatabaseAsync();
        await using var stores = CreateStores(databasePath, 1);
        const string definitionKey = "Test.InitialPublication.SchemaTransition";
        var original = CreateDefinition(definitionKey) with { SchemaVersion = 7 };
        var changed = CreateStringChildDefinition(definitionKey) with { SchemaVersion = 7 };
        var publisher = CreatePublisher("Test.InitialPublication.SchemaService", "schema:1");
        var originalSchema = ConfigurationDefinitionSchemaCodec.SerializeSchema(original);
        var changedSchema = ConfigurationDefinitionSchemaCodec.SerializeSchema(changed);
        changedSchema.Should().NotBe(originalSchema);
        changed.SchemaHash.Should().NotBe(original.SchemaHash);

        await stores.Items[0].PublishAsync(
            CreatePublication(publisher, [original]),
            TestContext.Current.CancellationToken);
        var initial = await ReadInitialPublicationOverviewInFreshScopeAsync(databasePath, definitionKey);
        initial.RevisionHistories.Should().ContainSingle();
        var initialHistory = initial.RevisionHistories[0];
        initialHistory.NewSchemaJson.Should().Be(originalSchema);
        initialHistory.NewSchemaHash.Should().Be(original.SchemaHash);
        initialHistory.NewSchemaVersion.Should().Be(7);

        await stores.Items[0].PublishAsync(
            CreatePublication(publisher, [changed]),
            TestContext.Current.CancellationToken);
        var updated = await ReadInitialPublicationOverviewInFreshScopeAsync(databasePath, definitionKey);
        updated.DefinitionRevision.Should().Be(2);
        updated.SchemaVersion.Should().Be(8);
        updated.RevisionHistories.Select(static history => history.DefinitionRevision).Should().Equal(2, 1);
        updated.RevisionHistories[1].Should().BeEquivalentTo(initialHistory);
        var schemaChange = updated.RevisionHistories[0];
        schemaChange.ChangeKind.Should().Be(ConfigurationDefinitionPublishChangeKind.SchemaChanged);
        schemaChange.HistoryId.Should().NotBeNullOrWhiteSpace().And.NotBe(initialHistory.HistoryId);
        schemaChange.PreviousSchemaVersion.Should().Be(7);
        schemaChange.PreviousSchemaHash.Should().Be(original.SchemaHash);
        schemaChange.PreviousSchemaJson.Should().Be(originalSchema);
        schemaChange.NewSchemaVersion.Should().Be(8);
        schemaChange.NewSchemaHash.Should().Be(changed.SchemaHash);
        schemaChange.NewSchemaJson.Should().Be(changedSchema);
        schemaChange.PublisherId.Should().Be(publisher.InstanceId);
        schemaChange.PublisherName.Should().Be(publisher.Name);
        schemaChange.PublisherVersion.Should().Be(publisher.Version);
        var persistedBeforeRepeat = await ReadInitialPublicationHistoryRowsAsync(databasePath, definitionKey);

        await stores.Items[0].PublishAsync(
            CreatePublication(publisher, [changed]),
            TestContext.Current.CancellationToken);
        var repeated = await ReadInitialPublicationOverviewInFreshScopeAsync(databasePath, definitionKey);
        var persistedAfterRepeat = await ReadInitialPublicationHistoryRowsAsync(databasePath, definitionKey);
        repeated.Should().BeEquivalentTo(updated);
        persistedAfterRepeat.Should().BeEquivalentTo(persistedBeforeRepeat);
    }

    [Theory]
    [InlineData("", "", null, null)]
    [InlineData(null, null, null, null)]
    [InlineData("", null, null, null)]
    [InlineData(null, "", null, null)]
    [InlineData("", "\t\r\n", null, "\t\r\n")]
    [InlineData("  ", "", "  ", null)]
    public async Task GetDefinitionPublicationOverviewAsync_WhenInitialPublicationHasLegacyAbsenceValues_ShouldCanonicalizeWithoutWriting(
        string? storedHash,
        string? storedJson,
        string? expectedHash,
        string? expectedJson)
    {
        var databasePath = await CreateMigratedDatabaseAsync();
        await using var stores = CreateStores(databasePath, 1);
        var definition = CreateDefinition("Test.InitialPublication.LegacyAbsence");
        await stores.Items[0].PublishAsync(
            CreatePublication([definition]),
            TestContext.Current.CancellationToken);
        var created = await ReadInitialPublicationHistoryRowsAsync(databasePath, definition.DefinitionKey);
        created.Should().ContainSingle();
        await SetInitialPublicationPriorEvidenceAsync(
            databasePath,
            created[0].HistoryId,
            ConfigurationDefinitionPublishChangeKind.Created,
            null,
            storedHash,
            storedJson);
        var persistedBeforeRead = await ReadInitialPublicationHistoryRowsAsync(databasePath, definition.DefinitionKey);
        persistedBeforeRead[0].PreviousSchemaHash.Should().Be(storedHash);
        persistedBeforeRead[0].PreviousSchemaJson.Should().Be(storedJson);

        var overview = await ReadInitialPublicationOverviewInFreshScopeAsync(databasePath, definition.DefinitionKey);
        var repeated = await ReadInitialPublicationOverviewInFreshScopeAsync(databasePath, definition.DefinitionKey);
        var persistedAfterRead = await ReadInitialPublicationHistoryRowsAsync(databasePath, definition.DefinitionKey);

        // Canonical DTO reads must not rewrite the retained SQL sentinel or any other audit evidence.
        persistedAfterRead.Should().BeEquivalentTo(persistedBeforeRead);
        repeated.Should().BeEquivalentTo(overview);
        overview.DefinitionRevision.Should().Be(1);
        overview.RevisionHistories.Should().ContainSingle();
        var history = overview.RevisionHistories[0];
        history.HistoryId.Should().Be(created[0].HistoryId);
        history.ChangeKind.Should().Be(ConfigurationDefinitionPublishChangeKind.Created);
        history.PreviousSchemaVersion.Should().BeNull();
        history.PreviousSchemaHash.Should().Be(expectedHash);
        history.PreviousSchemaJson.Should().Be(expectedJson);
        history.NewSchemaJson.Should().Be(ConfigurationDefinitionSchemaCodec.SerializeSchema(definition));
        history.NewSchemaHash.Should().Be(definition.SchemaHash);
    }

    [Theory]
    [InlineData(ConfigurationDefinitionPublishChangeKind.Created, null, " ", "\t\r\n")]
    [InlineData(ConfigurationDefinitionPublishChangeKind.Created, null, "sha256:legacy-initial", "\n{\"legacy\":true}\n")]
    [InlineData(ConfigurationDefinitionPublishChangeKind.Created, 7, "", "")]
    [InlineData(ConfigurationDefinitionPublishChangeKind.SchemaChanged, null, "", "")]
    [InlineData(ConfigurationDefinitionPublishChangeKind.MetadataChanged, null, "", "")]
    [InlineData(ConfigurationDefinitionPublishChangeKind.SchemaChanged, 7, " \t ", "\r\n")]
    [InlineData(ConfigurationDefinitionPublishChangeKind.MetadataChanged, 7, "sha256:legacy-prior", "\n{\"legacy\":false}\n")]
    public async Task GetDefinitionPublicationOverviewAsync_WhenPriorEvidenceIsNotAnInitialPublicationSentinel_ShouldPreserveItVerbatim(
        ConfigurationDefinitionPublishChangeKind changeKind,
        int? previousVersion,
        string storedHash,
        string storedJson)
    {
        var databasePath = await CreateMigratedDatabaseAsync();
        await using var stores = CreateStores(databasePath, 1);
        var definition = CreateDefinition("Test.InitialPublication.RetainedEvidence");
        await stores.Items[0].PublishAsync(
            CreatePublication([definition]),
            TestContext.Current.CancellationToken);
        var created = await ReadInitialPublicationHistoryRowsAsync(databasePath, definition.DefinitionKey);
        created.Should().ContainSingle();

        // Arrange retained, potentially inconsistent evidence; reading must not repair or conceal it.
        await SetInitialPublicationPriorEvidenceAsync(
            databasePath,
            created[0].HistoryId,
            changeKind,
            previousVersion,
            storedHash,
            storedJson);
        var persistedBeforeRead = await ReadInitialPublicationHistoryRowsAsync(databasePath, definition.DefinitionKey);

        var overview = await ReadInitialPublicationOverviewInFreshScopeAsync(databasePath, definition.DefinitionKey);
        var persistedAfterRead = await ReadInitialPublicationHistoryRowsAsync(databasePath, definition.DefinitionKey);

        persistedAfterRead.Should().BeEquivalentTo(persistedBeforeRead);
        overview.RevisionHistories.Should().ContainSingle();
        var history = overview.RevisionHistories[0];
        history.HistoryId.Should().Be(created[0].HistoryId);
        history.ChangeKind.Should().Be(changeKind);
        history.PreviousSchemaVersion.Should().Be(previousVersion);
        history.PreviousSchemaHash.Should().Be(storedHash);
        history.PreviousSchemaJson.Should().Be(storedJson);
        history.NewSchemaJson.Should().Be(persistedBeforeRead[0].NewSchemaJson);
        history.NewSchemaHash.Should().Be(persistedBeforeRead[0].NewSchemaHash);
    }

    private static async Task<ConfigurationDefinitionPublicationOverview> ReadInitialPublicationOverviewInFreshScopeAsync(
        string databasePath,
        string definitionKey)
    {
        await using var provider = CreateProvider(databasePath);
        await using var scope = provider.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IConfigurationMetadataStore>();
        return await store.GetDefinitionPublicationOverviewAsync(
            definitionKey,
            200,
            TestContext.Current.CancellationToken);
    }

    private static async Task<ConfigurationDefinitionPublishHistoryEntity[]> ReadInitialPublicationHistoryRowsAsync(
        string databasePath,
        string definitionKey)
    {
        await using var provider = CreateProvider(databasePath);
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        return await dbContext.ConfigurationDefinitionPublishHistories
            .AsNoTracking()
            .Where(history => history.DefinitionKey == definitionKey)
            .OrderByDescending(static history => history.DefinitionRevision)
            .ThenByDescending(static history => history.HistoryId)
            .ToArrayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task SetInitialPublicationPriorEvidenceAsync(
        string databasePath,
        string historyId,
        ConfigurationDefinitionPublishChangeKind changeKind,
        int? previousVersion,
        string? previousHash,
        string? previousJson)
    {
        await using var provider = CreateProvider(databasePath);
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var affected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE "ConfigurationDefinitionPublishHistories"
             SET "ChangeKind" = {changeKind.ToString()},
                 "PreviousSchemaVersion" = {previousVersion},
                 "PreviousSchemaHash" = {previousHash},
                 "PreviousSchemaJson" = {previousJson}
             WHERE "HistoryId" = {historyId}
             """,
            TestContext.Current.CancellationToken);
        affected.Should().Be(1);
    }
}
