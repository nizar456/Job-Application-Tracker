using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public record ResumeVersionRequest(
    [property: Required, StringLength(100)] string Name);

public record ResumeVersionResponse(Guid Id, string Name, DateTime CreatedAtUtc);
