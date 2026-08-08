using Backend.Domain;

namespace Backend.Models;

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