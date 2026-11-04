using System.Security.Cryptography;
using System.Text;

namespace Lending.Infrastructure.Security;

/// <summary>
/// Pseudonymizes client IP addresses before they are written to audit records.
/// </summary>
public static class IpHasher
{
    public static string? Hash(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ipAddress.Trim())))[..32];
    }
}
