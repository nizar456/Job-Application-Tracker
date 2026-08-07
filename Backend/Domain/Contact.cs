namespace Backend.Domain;

public class Contact
{
    public Guid Id { get; set; }

    public Guid JobApplicationId { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Role { get; set; }

    public string? Notes { get; set; }

    public JobApplication JobApplication { get; set; } = null!;
}
