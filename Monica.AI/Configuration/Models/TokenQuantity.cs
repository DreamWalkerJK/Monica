using System.Globalization;

namespace Monica.AI.Configuration.Models;

/// <summary>Parses and formats token limits without rounding away the configured capacity.</summary>
public static class TokenQuantity
{
    private const int KILO = 1024;
    private const int MEGA = KILO * KILO;

    /// <summary>
    /// Accepts a positive whole token count or a decimal quantity suffixed with K or M (case-insensitive).
    /// K means 1,024 and M means 1,048,576 tokens. Empty input succeeds with null; invalid, fractional-token,
    /// or overflowing quantities fail. Decimal notation uses a period and does not accept grouping separators.
    /// </summary>
    public static bool TryParse(string? text, out int? tokens)
    {
        tokens = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        var value = text.Trim();
        var multiplier = char.ToUpperInvariant(value[^1]) switch { 'K' => KILO, 'M' => MEGA, _ => 1 };
        if (multiplier != 1) value = value[..^1].TrimEnd();
        if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var quantity)
            || quantity <= 0 || quantity > (decimal)int.MaxValue / multiplier) return false;
        var total = quantity * multiplier;
        if (decimal.Truncate(total) != total) return false;
        tokens = (int)total;
        return true;
    }

    /// <summary>Uses an exact integral M or K suffix when possible; otherwise returns the full token count.</summary>
    public static string Format(int? tokens) => tokens switch
    {
        null => string.Empty,
        > 0 when tokens % MEGA == 0 => (tokens.Value / MEGA).ToString(CultureInfo.InvariantCulture) + "M",
        > 0 when tokens % KILO == 0 => (tokens.Value / KILO).ToString(CultureInfo.InvariantCulture) + "K",
        _ => tokens.Value.ToString(CultureInfo.InvariantCulture)
    };
}
