using Backend.Domain;

namespace Backend.Services;

public class JobApplicationQuery
{
    public int? Page { get; set; }

    public int? PageSize { get; set; }

    public string? SortBy { get; set; }

    public string? SortDirection { get; set; }

    public ApplicationStatus? Status { get; set; }

    public Guid? CompanyId { get; set; }

    public string? Search { get; set; }

    public string? Role { get; set; }

    public string? Location { get; set; }

    public string? Source { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }
}
