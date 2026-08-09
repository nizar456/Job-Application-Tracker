using System.Text.Json;
using Frontend.Models;
using Microsoft.JSInterop;

namespace Frontend.Services;

public sealed class TokenStore(IJSRuntime js)
{
    private const string StorageKey = "job-application-tracker-auth";

    private string? _token;
    private DateTimeOffset _expiresAt;
    private string? _userName;
    private string? _email;
    private bool _loadedFromStorage;

    public bool IsAuthenticated => _token is not null && _expiresAt > DateTimeOffset.UtcNow;

    public bool IsExpired => _token is not null && _expiresAt <= DateTimeOffset.UtcNow;

    public string? Token => _token;

    public string? UserName => _userName;

    public string? Email => _email;

    public async Task EnsureLoadedFromStorageAsync()
    {
        if (_loadedFromStorage)
        {
            return;
        }

        _loadedFromStorage = true;
        try
        {
            var stored = await js.InvokeAsync<string?>("appTokenStore.get");
            if (string.IsNullOrEmpty(stored))
            {
                return;
            }

            var auth = JsonSerializer.Deserialize<AuthResponse>(stored);
            if (auth is not null && auth.ExpiresAt.ToUniversalTime() > DateTime.UtcNow)
            {
                _token = auth.Token;
                _expiresAt = auth.ExpiresAt;
                _userName = auth.UserName;
                _email = auth.Email;
            }
        }
        catch
        {
        }
    }

    public async Task PersistAsync(AuthResponse auth)
    {
        Set(auth.Token, auth.ExpiresAt, auth.UserName, auth.Email);
        try
        {
            await js.InvokeVoidAsync("appTokenStore.set", JsonSerializer.Serialize(auth));
        }
        catch
        {
        }
    }

    public async Task ClearPersistedAsync()
    {
        Clear();
        try
        {
            await js.InvokeVoidAsync("appTokenStore.remove");
        }
        catch
        {
        }
    }

    private void Set(string token, DateTimeOffset expiresAt, string userName, string email)
    {
        _token = token;
        _expiresAt = expiresAt;
        _userName = userName;
        _email = email;
    }

    private void Clear()
    {
        _token = null;
        _expiresAt = default;
        _userName = null;
        _email = null;
    }
}