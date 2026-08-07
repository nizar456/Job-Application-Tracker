namespace Backend.Domain;

public class JobApplication
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public Guid CompanyId { get; set; }

    public Guid? ResumeVersionId { get; set; }

    public string RoleTitle { get; set; } = string.Empty;

    public string? Location { get; set; }

    public WorkMode WorkMode { get; set; }

    public string? JobUrl { get; set; }

    public string? Source { get; set; }

    public string? JobDescription { get; set; }

    public DateOnly DateApplied { get; set; }

    public ApplicationStatus Status { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;

    public ResumeVersion? ResumeVersion { get; set; }

    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = [];

    public ICollection<Contact> Contacts { get; set; } = [];

    public ICollection<ApplicationTag> ApplicationTags { get; set; } = [];
}
