using Microsoft.EntityFrameworkCore;

namespace Monica.Repository.UnitOfWork.Annotations;

/// <summary>
/// Selects the write contexts for an automatically transactional execution boundary.
/// The first context owns the physical transaction and any Outbox or Inbox rows. Additional
/// contexts must share its connection and transaction. An explicit execution feature overrides this default.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class UnitOfWorkContextAttribute : Attribute
{
    /// <summary>Creates a selection with the primary context first, followed by any shared participants.</summary>
    public UnitOfWorkContextAttribute(Type primaryContext, params Type[] participantContexts)
    {
        ArgumentNullException.ThrowIfNull(primaryContext);
        ArgumentNullException.ThrowIfNull(participantContexts);
        var types = new[] { primaryContext }.Concat(participantContexts).ToArray();
        if (types.Any(type => type is null || !typeof(DbContext).IsAssignableFrom(type)) || types.Distinct().Count() != types.Length)
            throw new ArgumentException("UnitOfWork context types must be distinct DbContext types.", nameof(participantContexts));
        DbContextTypes = Array.AsReadOnly(types);
    }

    /// <summary>Ordered context selection; the first entry owns the transaction.</summary>
    public IReadOnlyList<Type> DbContextTypes { get; }
}
