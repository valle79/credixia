namespace Lending.Application.Abstractions;

/// <summary>
/// Abstraction over time so business rules and tests are deterministic.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateOnly Today(string timeZoneId);
}
