using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Monica.Repository.Persistence.Models;

/// <summary>The storage operation captured before audit and soft-delete rewriting.</summary>
public enum PersistenceChangeKind
{
    /// <summary>A newly inserted row.</summary>
    Created,
    /// <summary>An existing row changed.</summary>
    Updated,
    /// <summary>A soft or physical deletion.</summary>
    Deleted
}

/// <summary>A changed entry with its original operation kind, available to explicit save policies.</summary>
public sealed record PersistenceChange(EntityEntry Entry, PersistenceChangeKind Kind);
