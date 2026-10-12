using System.Text.RegularExpressions;
using Monica.DevOps.Git.Models;

namespace Monica.DevOps.Git.Support;

/// <summary>Redacts known credential values and common URL secret fields in ordinary encoded UI text.</summary>
/// <remarks>This is bounded presentation hygiene, not an arbitrary secret detector. Callers must still avoid
/// publishing raw credentials/options. Returned strings require normal Razor text encoding, never MarkupString.</remarks>
public sealed class GitPresentationPrivacy
{
    // Non-backtracking matching keeps long ordinary text linear when no URL is present.
    private static readonly Regex _urlUserInfo = new(
        @"([a-z][a-z0-9+.-]*://)[^\s/?#]*@",
        RegexOptions.NonBacktracking | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));
    private static readonly Regex _secretQuery = new(
        @"([?&](?:access_token|token|password|passwd|secret|api_key|apikey|private_token|auth|authorization|signature|sig|credential)=)[^&#\s]*",
        RegexOptions.NonBacktracking | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));
    private readonly string[] _knownSecrets;
    /// <summary>Captures configured token/password values only; does not execute credential resolvers.</summary>
    public GitPresentationPrivacy(IEnumerable<GitCredentialRegistration> registrations)
    {
        _knownSecrets = registrations.SelectMany(x => new[] { x.Token, x.Password })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .SelectMany(x => new[] { x!, Uri.EscapeDataString(x!) })
            .Distinct(StringComparer.Ordinal).OrderByDescending(x => x.Length).ToArray();
    }

    /// <summary>Redacts before bounding to 32,768 UTF-16 code units including a trailing ellipsis when truncated.</summary>
    public string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        var result = text;
        foreach (var secret in _knownSecrets) result = result.Replace(secret, "[redacted]", StringComparison.Ordinal);
        result = _urlUserInfo.Replace(result, "$1[redacted]@");
        result = _secretQuery.Replace(result, "$1[redacted]");
        const int limit = 32768;
        if (result.Length <= limit) return result;
        var end = limit - 1;
        if (char.IsHighSurrogate(result[end - 1])) end--;
        return result[..end] + "…";
    }
}
