using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public record CompanyRequest(
    [property: Required, StringLength(200)] string Name,
    [property: StringLength(500)] string? Website);

public record CompanyResponse(Guid Id, string Name, string? Website, DateTime CreatedAtUtc);
