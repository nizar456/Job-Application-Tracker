using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public record ContactRequest(
    [property: StringLength(200)] string? Name,
    [property: StringLength(256)] string? Email,
    [property: StringLength(50)] string? Phone,
    [property: StringLength(200)] string? Role,
    string? Notes);

public record ContactResponse(
    Guid Id,
    Guid JobApplicationId,
    string? Name,
    string? Email,
    string? Phone,
    string? Role,
    string? Notes);
