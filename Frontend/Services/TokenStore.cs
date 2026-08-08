namespace Frontend.Services;

public sealed class TokenStore
{
    private string? _token;
    private DateTimeOffset _expiresAt;
    private string? _userName;
    private string? _email;

    public bool IsAuthenticated => _token is not null && _expiresAt > DateTimeOffset.UtcNow;

    public bool IsExpired => _token is not null && _expiresAt <= DateTimeOffset.UtcNow;

    public string? Token => _token;

    public string? UserName => _userName;

    public string? Email => _email;

    public void Set(string token, DateTimeOffset expiresAt, string userName, string email)
    {
        _token = token;
        _expiresAt = expiresAt;
        _userName = userName;
        _email = email;
    }

    public void Clear()
    {
        _token = null;
        _expiresAt = default;
        _userName = null;
        _email = null;
    }
}