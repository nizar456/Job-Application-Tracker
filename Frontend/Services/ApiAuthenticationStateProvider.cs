using System.Security.Claims;
using Frontend.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend.Services;

public sealed class ApiAuthenticationStateProvider(TokenStore tokenStore) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await tokenStore.EnsureLoadedFromStorageAsync();
        var identity = CreateIdentity();
        var principal = identity is null ? new ClaimsPrincipal() : new ClaimsPrincipal(identity);
        return new AuthenticationState(principal);
    }

    public async Task SignInAsync(AuthResponse auth)
    {
        await tokenStore.PersistAsync(auth);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        await tokenStore.ClearPersistedAsync();
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