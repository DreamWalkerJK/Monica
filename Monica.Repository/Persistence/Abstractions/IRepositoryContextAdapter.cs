using Microsoft.EntityFrameworkCore;

namespace Monica.Repository.Persistence.Abstractions;

/// <summary>
/// Opt-in contract for providers whose logical context delegates EF saves to physical RepositoryDbContexts.
/// Every physical context must run the fixed persistence policies exactly once and share its owner's
/// connection and transaction. Independent database transactions are not a supported atomic operation.
/// </summary>
public interface IRepositoryContextAdapter
{
    /// <summary>True only on a logical coordinator whose base EF save invokes the physical contexts' saves.</summary>
    bool CoordinatesSave { get; }

    /// <summary>The logical transaction owner of a physical context; null for coordinators and ordinary contexts.</summary>
    DbContext? TransactionOwner { get; }
}
