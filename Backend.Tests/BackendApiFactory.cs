using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Backend.Tests;

public class BackendApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "InMemory",
                ["Jwt:Issuer"] = "Backend.Tests",
                ["Jwt:Audience"] = "Backend.Tests",
                ["Jwt:SigningKey"] = "test-signing-key-at-least-32-characters-long",
                ["Jwt:ExpiryMinutes"] = "60",
            });
        });
    }
}
