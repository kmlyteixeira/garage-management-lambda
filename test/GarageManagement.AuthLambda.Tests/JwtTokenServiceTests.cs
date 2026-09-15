using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using GarageManagement.AuthLambda;
using Xunit;

namespace GarageManagement.AuthLambda.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_includes_configured_key_id()
    {
        using var rsa = RSA.Create(2048);
        var service = new JwtTokenService(new Dictionary<string, string?>
        {
            ["JWT_ISSUER"] = "https://auth.example.test",
            ["JWT_AUDIENCE"] = "garage-management-api",
            ["JWT_KEY_ID"] = "key-2026",
            ["JWT_PRIVATE_KEY_PEM"] = rsa.ExportRSAPrivateKeyPem()
        });

        var response = service.CreateToken(new CustomerAuthenticationRecord(Guid.NewGuid(), "52998224725", true));
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);

        Assert.Equal("key-2026", token.Header.Kid);
        Assert.Equal("https://auth.example.test", token.Issuer);
        Assert.Equal("garage-management-api", token.Audiences.Single());
    }

    [Fact]
    public void GetPublicJwks_does_not_expose_private_key_material()
    {
        using var rsa = RSA.Create(2048);
        var service = new JwtTokenService(new Dictionary<string, string?>
        {
            ["JWT_ISSUER"] = "https://auth.example.test",
            ["JWT_AUDIENCE"] = "garage-management-api",
            ["JWT_PRIVATE_KEY_PEM"] = rsa.ExportRSAPrivateKeyPem()
        });

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(service.GetPublicJwks()));
        var key = document.RootElement.GetProperty("keys")[0];

        Assert.Equal("RSA", key.GetProperty("kty").GetString());
        Assert.Equal("garage-management-auth", key.GetProperty("kid").GetString());
        Assert.True(key.TryGetProperty("n", out _));
        Assert.True(key.TryGetProperty("e", out _));
        Assert.False(key.TryGetProperty("d", out _));
    }
}
