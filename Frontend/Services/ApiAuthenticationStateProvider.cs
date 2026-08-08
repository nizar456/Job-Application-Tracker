using System.Security.Claims;
using Frontend.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend.Services;

public sealed class ApiAuthenticationStateProvider(TokenStore tokenStore) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = CreateIdentity();
        var principal = identity is null ? new ClaimsPrincipal() : new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(principal));
    }

    public void SignIn(AuthResponse auth)
    {
        tokenStore.Set(auth.Token, auth.ExpiresAt, auth.UserName, auth.Email);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void SignOut()
    {
        tokenStore.Clear();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private ClaimsIdentity? CreateIdentity()
    {
        if (!tokenStore.IsAuthenticated || tokenStore.UserName is null)
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, tokenStore.UserName),
        };

        if (tokenStore.Email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Email, tokenStore.Email));
        }

        return new ClaimsIdentity(claims, "Bearer");
    }
}