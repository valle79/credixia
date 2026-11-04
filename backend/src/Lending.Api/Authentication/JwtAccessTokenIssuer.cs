using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lending.Api.Configuration;
using Lending.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lending.Api.Authentication;

public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;
    private readonly SigningCredentials _credentials;

    public JwtAccessTokenIssuer(IOptions<JwtOptions> options, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _clock = clock;
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public IssuedAccessToken Issue(AccessTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, request.Email),
            new("sst", request.SecurityStamp),
            new("sid", request.SessionId.ToString()),
            new("plat", request.IsPlatformAdmin ? "true" : "false"),
        };

        if (request.TenantId.HasValue)
        {
            claims.Add(new Claim("tid", request.TenantId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _credentials);

        return new IssuedAccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}
