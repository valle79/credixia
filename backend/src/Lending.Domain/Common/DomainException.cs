namespace Lending.Domain.Common;

/// <summary>
/// Represents a violated business rule. The <see cref="Code"/> is stable and safe to
/// expose to API clients, unlike exception messages from infrastructure.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string code, string message)
        : base(message)
    {
        Code = string.IsNullOrWhiteSpace(code)
            ? throw new ArgumentException("A domain error code is required.", nameof(code))
            : code;
    }

    public string Code { get; }
}
