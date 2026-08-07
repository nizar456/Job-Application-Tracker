namespace Backend.Domain;

public class ApplicationStatusHistory
{
    public Guid Id { get; set; }

    public Guid JobApplicationId { get; set; }

    public ApplicationStatus Status { get; set; }

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

    public JobApplication JobApplication { get; set; } = null!;
}
