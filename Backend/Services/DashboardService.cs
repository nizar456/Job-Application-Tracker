using Backend.Data;
using Backend.Domain;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class DashboardService(ApplicationDbContext db)
{
    private const int RecentApplicationsLimit = 5;

    private static readonly ApplicationStatus[] RespondedStatuses =
    [
        ApplicationStatus.RecruiterContacted,
        ApplicationStatus.Interviewing,
        ApplicationStatus.Offer,
        ApplicationStatus.Rejected,
        ApplicationStatus.Withdrawn,
    ];

    public async Task<DashboardResponse> GetAsync(string userId)
    {
        var applications = await db.JobApplications
            .AsNoTracking()
            .Where(j => j.UserId == userId)
            .Select(j => new ApplicationRow(
                j.Id,
                j.CompanyId,
                j.Company.Name,
                j.RoleTitle,
                j.Status,
                j.DateApplied,
                j.CreatedAtUtc))
            .ToListAsync();

        var total = applications.Count;
        var archived = applications.Count(a => a.Status == ApplicationStatus.Archived);
        var saved = applications.Count(a => a.Status == ApplicationStatus.Saved);
        var active = total - archived;

        var byStatus = applications
            .GroupBy(a => a.Status)
            .OrderBy(g => g.Key)
            .Select(g => new StatusCount(g.Key, g.Count()))
            .ToList();

        var recent = applications
            .OrderByDescending(a => a.DateApplied)
            .ThenByDescending(a => a.CreatedAtUtc)
            .Take(RecentApplicationsLimit)
            .Select(a => new RecentApplication(a.Id, a.CompanyId, a.CompanyName, a.RoleTitle, a.Status, a.DateApplied))
            .ToList();

        var perMonth = applications
            .GroupBy(a => a.DateApplied.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .Select(g => new MonthCount(g.Key, g.Count()))
            .ToList();

        var responded = applications.Count(a => RespondedStatuses.Contains(a.Status));
        var responseBase = active - saved;
        var ratio = responseBase == 0 ? 0 : (double)responded / responseBase;

        return new DashboardResponse(
            total,
            active,
            byStatus,
            recent,
            perMonth,
            new ResponseRate(responded, responseBase, ratio));
    }

    private sealed record ApplicationRow(
        Guid Id,
        Guid CompanyId,
        string CompanyName,
        string RoleTitle,
        ApplicationStatus Status,
        DateOnly DateApplied,
        DateTime CreatedAtUtc);
}