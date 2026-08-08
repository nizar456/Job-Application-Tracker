using System.ComponentModel.DataAnnotations;

namespace Frontend.Models;

public record RegisterRequest(
    [property: Required, StringLength(50, MinimumLength = 3)]
    string UserName,
    [property: Required, EmailAddress]
    string Email,
    [property: Required, StringLength(100, MinimumLength = 8)]
    string Password);

public record LoginRequest(
    [property: Required, EmailAddress]
    string Email,
    [property: Required]
    string Password);

public record AuthResponse(string Token, DateTime ExpiresAt, string UserName, string Email);