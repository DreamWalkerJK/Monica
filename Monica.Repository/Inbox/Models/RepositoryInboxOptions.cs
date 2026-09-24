namespace Monica.Repository.Inbox.Models;

/// <summary>Controls Inbox receipts for one repository context.</summary>
public sealed class RepositoryInboxOptions
{
    /// <summary>
    /// Maps the Inbox table for runtime use without owning it in migrations. Set this on contexts that share
    /// a physical database with another context whose migrations create the shared table; leave the default
    /// (owner) on exactly one context per database.
    /// </summary>
    public bool ExcludeFromMigrations { get; set; }
}
