using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Frontend.Services;

public static class FrontendAuthenticationDefaults
{
    public const string SchemeName = "Frontend";
    public const string AuthCookieName = "job-tracker-auth";
}

public sealed class FrontendAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // The JWT lives in browser localStorage and is validated client-side by
        // ApiAuthenticationStateProvider. This handler only needs to recognise
        // that a session exists so that a page refresh on a protected route is
        // not redirected to the sign-in page before the interactive circuit
        // (which reads the real token) has started.
        if (Request.Cookies.ContainsKey(FrontendAuthenticationDefaults.AuthCookieName))
        {
            var identity = new ClaimsIdentity(Array.Empty<Claim>(), Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, Scheme.Name)));
        }

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
