using System.ComponentModel.DataAnnotations;

namespace Frontend.Models;

public sealed class ApplicationFormModel
{
    [Required(ErrorMessage = "Please select a company.")]
    public Guid? CompanyId { get; set; }

    public Guid? ResumeVersionId { get; set; }

    [Required(ErrorMessage = "Role title is required.")]
    [StringLength(200, ErrorMessage = "Role title cannot be longer than 200 characters.")]
    public string RoleTitle { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Location cannot be longer than 200 characters.")]
    public string? Location { get; set; }

    public WorkMode WorkMode { get; set; } = WorkMode.Hybrid;

    [Url(ErrorMessage = "The job URL is not valid.")]
    [StringLength(500, ErrorMessage = "The job URL cannot be longer than 500 characters.")]
    public string? JobUrl { get; set; }

    [StringLength(200, ErrorMessage = "Source cannot be longer than 200 characters.")]
    public string? Source { get; set; }

    public string? JobDescription { get; set; }

    public DateOnly DateApplied { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Saved;

    [StringLength(1000, ErrorMessage = "Notes cannot be longer than 1000 characters.")]
    public string? Notes { get; set; }

    public HashSet<Guid> SelectedTagIds { get; set; } = [];

    public JobApplicationRequest ToRequest() => new(
        CompanyId: CompanyId ?? Guid.Empty,
        ResumeVersionId: ResumeVersionId,
        RoleTitle: RoleTitle,
        Location: Location,
        WorkMode: WorkMode,
        JobUrl: JobUrl,
        Source: Source,
        JobDescription: JobDescription,
        DateApplied: DateApplied,
        Status: Status,
        Notes: Notes,
        TagIds: SelectedTagIds.Count == 0 ? null : SelectedTagIds.ToArray());
}

public enum ApplicationStatus
{
    Saved = 1,
    Applied = 2,
    RecruiterContacted = 3,
    Interviewing = 4,
    Offer = 5,
    Rejected = 6,
    Withdrawn = 7,
    Archived = 8,
}

public enum WorkMode
{
    Remote = 1,
    Hybrid = 2,
    OnSite = 3,
}

public record TagResponse(Guid Id, string Name);

public record JobApplicationRequest(
    Guid CompanyId,
    Guid? ResumeVersionId,
    string RoleTitle,
    string? Location,
    WorkMode WorkMode,
    string? JobUrl,
    string? Source,
    string? JobDescription,
    DateOnly DateApplied,
    ApplicationStatus Status,
    string? Notes,
    Guid[]? TagIds);

public record ChangeStatusRequest(ApplicationStatus Status, string? Note);

public record JobApplicationResponse(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    Guid? ResumeVersionId,
    string? ResumeVersionName,
    string RoleTitle,
    string? Location,
    WorkMode WorkMode,
    string? JobUrl,
    string? Source,
    string? JobDescription,
    DateOnly DateApplied,
    ApplicationStatus Status,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<TagResponse> Tags);

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public record CompanyResponse(Guid Id, string Name, string? Website, DateTime CreatedAtUtc);

public record ResumeVersionResponse(Guid Id, string Name, DateTime CreatedAtUtc);

public record DashboardResponse(
    int TotalCount,
    int ActiveCount,
    IReadOnlyList<StatusCount> ApplicationsByStatus,
    IReadOnlyList<RecentApplication> RecentApplications,
    IReadOnlyList<MonthCount> ApplicationsPerMonth,
    ResponseRate ResponseRate);

public record StatusCount(ApplicationStatus Status, int Count);

public record RecentApplication(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string RoleTitle,
    ApplicationStatus Status,
    DateOnly DateApplied);

public record MonthCount(string YearMonth, int Count);

public record ResponseRate(int RespondedCount, int BaseCount, double Ratio);

public static class ApplicationStatusDisplay
{
    public static string DisplayName(this ApplicationStatus status) => status switch
    {
        ApplicationStatus.Saved => "Saved",
        ApplicationStatus.Applied => "Applied",
        ApplicationStatus.RecruiterContacted => "Recruiter contacted",
        ApplicationStatus.Interviewing => "Interviewing",
        ApplicationStatus.Offer => "Offer",
        ApplicationStatus.Rejected => "Rejected",
        ApplicationStatus.Withdrawn => "Withdrawn",
        ApplicationStatus.Archived => "Archived",
        _ => status.ToString(),
    };

    public static string BadgeClass(this ApplicationStatus status) => status switch
    {
        ApplicationStatus.Saved => "bg-secondary",
        ApplicationStatus.Applied => "bg-primary",
        ApplicationStatus.RecruiterContacted => "bg-info",
        ApplicationStatus.Interviewing => "bg-warning text-dark",
        ApplicationStatus.Offer => "bg-success",
        ApplicationStatus.Rejected => "bg-danger",
        ApplicationStatus.Withdrawn => "bg-dark",
        ApplicationStatus.Archived => "bg-secondary",
        _ => "bg-secondary",
    };

    public static string DisplayName(this WorkMode workMode) => workMode switch
    {
        WorkMode.Remote => "Remote",
        WorkMode.Hybrid => "Hybrid",
        WorkMode.OnSite => "On-site",
        _ => workMode.ToString(),
    };
}
