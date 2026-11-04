namespace Lending.Domain.Common;

/// <summary>
/// Small argument guards for domain invariants. Technical argument errors are thrown as
/// <see cref="ArgumentException"/>; business rule violations must use
/// <see cref="DomainException"/> instead.
/// </summary>
public static class Guard
{
    public static string NotNullOrWhiteSpace(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return trimmed;
    }

    public static decimal NotNegative(decimal value, string parameterName)
    {
        if (value < 0m)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
        }

        return value;
    }

    public static T NotNull<T>(T? value, string parameterName)
        where T : class
    {
        return value ?? throw new ArgumentNullException(parameterName);
    }
}
