using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Frontend.Services;

public static class FrontendAuthenticationDefaults
{
    public const string SchemeName = "Frontend";
}

public sealed class FrontendAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(AuthenticateResult.NoResult());
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var currentPath = Request.PathBase + Request.Path + Request.QueryString;
        var returnUrl = Uri.EscapeDataString(currentPath);
        Response.StatusCode = StatusCodes.Status302Found;
        Response.Headers.Location = $"/login?returnUrl={returnUrl}";
        return Task.CompletedTask;
    }
}