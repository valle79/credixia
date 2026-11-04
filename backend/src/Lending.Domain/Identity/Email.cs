using System.Text.RegularExpressions;
using Lending.Domain.Common;

namespace Lending.Domain.Identity;

/// <summary>
/// Normalized email address used as the login identifier. The original casing is not
/// preserved because lookups are case-insensitive.
/// </summary>
public sealed partial record Email
{
    public const int MaxLength = 320;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Email Create(string? value)
    {
        var candidate = value?.Trim().ToLowerInvariant() ?? string.Empty;

        if (candidate.Length is < 5 or > MaxLength || !EmailPattern().IsMatch(candidate))
        {
            throw new DomainException("user.email.invalid", "A valid email address is required.");
        }

        return new Email(candidate);
    }

    public override string ToString() => Value;

    [GeneratedRegex(
        "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex EmailPattern();
}
