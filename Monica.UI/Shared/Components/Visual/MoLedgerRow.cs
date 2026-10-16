namespace Monica.UI.Shared.Components.Visual;

/// <summary>A capability badge rendered beside ranked-ledger provenance metadata.</summary>
public sealed record MoLedgerBadge(string Text, MoVisualRole Role = MoVisualRole.Neutral);

/// <summary>One ranked ledger row: identity, provenance, an optional two-role meter against a shared scale, and a right-aligned total.</summary>
public sealed record MoLedgerRow(
    string Name,
    string? Metadata = null,
    IReadOnlyList<MoLedgerBadge>? Badges = null,
    double PrimaryValue = 0,
    double SecondaryValue = 0,
    double MeterScale = 0,
    string? MeterPrimaryCaption = null,
    string? MeterSecondaryCaption = null,
    string? Total = null,
    bool TotalWarn = false,
    bool Disabled = false);

/// <summary>Localized column labels for the ranked ledger header row.</summary>
public sealed record MoLedgerHeaderLabels(string Rank, string Name, string Meter, string Total);
