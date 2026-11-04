namespace Lending.Application.Abstractions;

/// <summary>
/// One-way password hashing. The implementation must use a memory-hard or high-iteration
/// algorithm and never store the clear password.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
