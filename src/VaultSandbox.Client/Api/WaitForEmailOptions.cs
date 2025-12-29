using System.Text.RegularExpressions;

namespace VaultSandbox.Client.Api;

/// <summary>
/// Options for filtering emails when waiting.
/// </summary>
public sealed class WaitForEmailOptions
{
    /// <summary>
    /// Match emails with this exact subject, or use regex pattern.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Match emails from this sender address, or use regex pattern.
    /// </summary>
    public string? From { get; set; }

    /// <summary>
    /// Custom predicate function for advanced filtering.
    /// </summary>
    public Func<Email, bool>? Predicate { get; set; }

    /// <summary>
    /// Maximum time to wait for a matching email.
    /// Default: 30 seconds.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Polling interval when using polling strategy.
    /// Default: 2 seconds.
    /// </summary>
    public TimeSpan? PollInterval { get; set; }

    /// <summary>
    /// Whether subject/from filters should use regex matching.
    /// Default: false (exact match).
    /// </summary>
    public bool UseRegex { get; set; }

    internal bool Matches(Email email)
    {
        if (Subject is not null)
        {
            var matches = UseRegex
                ? Regex.IsMatch(email.Subject, Subject, RegexOptions.IgnoreCase)
                : email.Subject.Equals(Subject, StringComparison.OrdinalIgnoreCase);

            if (!matches) return false;
        }

        if (From is not null)
        {
            var matches = UseRegex
                ? Regex.IsMatch(email.From, From, RegexOptions.IgnoreCase)
                : email.From.Contains(From, StringComparison.OrdinalIgnoreCase);

            if (!matches) return false;
        }

        if (Predicate is not null && !Predicate(email))
        {
            return false;
        }

        return true;
    }
}
