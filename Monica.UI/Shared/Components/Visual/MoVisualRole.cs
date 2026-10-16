namespace Monica.UI.Shared.Components.Visual;

/// <summary>
/// Palette roles a visual-language element can carry. Each role resolves through
/// MudBlazor palette variables and shared color derivations; no element paints a literal color.
/// </summary>
public enum MoVisualRole
{
    /// <summary>Context not owned by the subject; host and baseline elements.</summary>
    Neutral,

    /// <summary>The page's main category, signature emphasis, and selected state.</summary>
    Primary,

    /// <summary>A distinct contributing category, such as a second meter segment.</summary>
    Info,

    /// <summary>A third category when Info is already used and the theme supplies a solid secondary.</summary>
    Secondary,

    /// <summary>An actual success state.</summary>
    Success,

    /// <summary>An actual warning state, such as an exceeded budget.</summary>
    Warning,

    /// <summary>An actual failure state.</summary>
    Error,
}

/// <summary>
/// Maps visual roles to the shared CSS modifier classes defined by
/// <c>wwwroot/css/components/mo-visual-language.css</c>.
/// </summary>
public static class MoVisualRoleExtensions
{
    /// <summary>Gets the shared CSS modifier class that assigns the role's palette color.</summary>
    public static string ToCssClass(this MoVisualRole role) => role switch
    {
        MoVisualRole.Neutral => "mo-vl-role--neutral",
        MoVisualRole.Primary => "mo-vl-role--primary",
        MoVisualRole.Info => "mo-vl-role--info",
        MoVisualRole.Secondary => "mo-vl-role--secondary",
        MoVisualRole.Success => "mo-vl-role--success",
        MoVisualRole.Warning => "mo-vl-role--warning",
        MoVisualRole.Error => "mo-vl-role--error",
        _ => "mo-vl-role--primary",
    };
}
