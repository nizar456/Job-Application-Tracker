using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public record TagRequest(
    [property: Required, StringLength(50)] string Name);
