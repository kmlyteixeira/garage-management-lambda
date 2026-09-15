using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace GarageManagement.AuthLambda;

public sealed class JwtTokenService
{
    private readonly string issuer;
    private readonly string audience;
    private readonly string keyId;
    private readonly int lifetimeMinutes;
    private readonly RSA signingKey;

    public JwtTokenService(IReadOnlyDictionary<string, string?> settings)
    {
        issuer = Required(settings, "JWT_ISSUER");
        audience = Required(settings, "JWT_AUDIENCE");
        keyId = settings.TryGetValue("JWT_KEY_ID", out var configuredKeyId) && !string.IsNullOrWhiteSpace(configuredKeyId)
            ? configuredKeyId
            : "garage-management-auth";
        lifetimeMinutes = int.Parse(settings.TryGetValue("JWT_LIFETIME_MINUTES", out var lifetime)
            ? lifetime ?? "15"
            : "15");
        signingKey = RSA.Create();
        signingKey.ImportFromPem(Required(settings, "JWT_PRIVATE_KEY_PEM"));
    }

    public AuthenticationResponse CreateToken(CustomerAuthenticationRecord customer)
    {
        var expiresIn = TimeSpan.FromMinutes(lifetimeMinutes);
        var signingSecurityKey = new RsaSecurityKey(signingKey) { KeyId = keyId };
        var credentials = new SigningCredentials(signingSecurityKey, SecurityAlgorithms.RsaSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
            new Claim("customer_id", customer.Id.ToString()),
            new Claim("scope", "garage-management")
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.Add(expiresIn),
            signingCredentials: credentials);

        return new AuthenticationResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            (int)expiresIn.TotalSeconds);
    }

    public object GetPublicJwks()
    {
        var parameters = signingKey.ExportParameters(includePrivateParameters: false);
        return new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    alg = SecurityAlgorithms.RsaSha256,
                    kid = keyId,
                    n = Base64UrlEncoder.Encode(parameters.Modulus),
                    e = Base64UrlEncoder.Encode(parameters.Exponent)
                }
            }
        };
    }

    private static string Required(IReadOnlyDictionary<string, string?> settings, string key)
    {
        return settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Missing required setting: {key}");
    }
}
