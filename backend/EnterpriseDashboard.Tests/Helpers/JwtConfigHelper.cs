using Microsoft.Extensions.Configuration;

namespace EnterpriseDashboard.Tests.Helpers;

/// <summary>Builds an IConfiguration with valid JWT settings for AuthService tests.</summary>
public static class JwtConfigHelper
{
    public static IConfiguration Build() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]            = "SuperSecretTestKey_AtLeast32CharsLong!",
                ["Jwt:Issuer"]         = "test-issuer",
                ["Jwt:Audience"]       = "test-audience",
                ["Jwt:ExpiryMinutes"]  = "60",
            })
            .Build();
}
