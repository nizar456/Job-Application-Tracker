namespace Backend.Domain;

public class ResumeVersion
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<JobApplication> JobApplications { get; set; } = [];
}
