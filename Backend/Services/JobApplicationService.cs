using Backend.Data;
using Backend.Domain;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class JobApplicationService(ApplicationDbContext db)
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<PagedResponse<JobApplicationResponse>> ListAsync(string userId, JobApplicationQuery query)
    {
        var page = Math.Max(1, query.Page ?? 1);
        var pageSize = query.PageSize is > 0 ? Math.Min(query.PageSize.Value, MaxPageSize) : DefaultPageSize;

        var applications = ApplyFilters(
            LoadQuery(userId).AsNoTracking(),
            query);

        var totalCount = await applications.CountAsync();

        var items = await ApplySorting(applications, query.SortBy, query.SortDirection)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<JobApplicationResponse>(
            items.Select(MapToResponse).ToList(),
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<JobApplicationResponse?> GetAsync(string userId, Guid id)
    {
        var application = await LoadQuery(userId).AsNoTracking().FirstOrDefaultAsync(j => j.Id == id);
        return application is null ? null : MapToResponse(application);
    }

    public async Task<CommandResult<JobApplicationResponse>> CreateAsync(string userId, JobApplicationRequest request)
    {
        var errors = await ValidateApplicationAsync(userId, request);
        if (errors.Count > 0)
        {
            return CommandResult<JobApplicationResponse>.Failed(errors);
        }

        var now = DateTime.UtcNow;
        var application = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyId = request.CompanyId,
            ResumeVersionId = request.ResumeVersionId,
            RoleTitle = request.RoleTitle,
            Location = request.Location,
            WorkMode = request.WorkMode,
            JobUrl = request.JobUrl,
            Source = request.Source,
            JobDescription = request.JobDescription,
            DateApplied = request.DateApplied,
            Status = request.Status,
            Notes = request.Notes,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        foreach (var tagId in request.TagIds ?? [])
        {
            application.ApplicationTags.Add(new ApplicationTag { JobApplicationId = application.Id, TagId = tagId });
        }

        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            JobApplicationId = application.Id,
            Status = application.Status,
            ChangedAtUtc = now,
        });

        db.JobApplications.Add(application);
        await db.SaveChangesAsync();

        var saved = await LoadQuery(userId).AsNoTracking().FirstAsync(j => j.Id == application.Id);
        return CommandResult<JobApplicationResponse>.Ok(MapToResponse(saved));
    }

    public async Task<CommandResult<JobApplicationResponse>> UpdateAsync(string userId, Guid id, JobApplicationRequest request)
    {
        var application = await LoadQuery(userId).FirstOrDefaultAsync(j => j.Id == id);
        if (application is null)
        {
            return CommandResult<JobApplicationResponse>.Missing();
        }

        var errors = await ValidateApplicationAsync(userId, request);
        if (errors.Count > 0)
        {
            return CommandResult<JobApplicationResponse>.Failed(errors);
        }

        var statusChanged = application.Status != request.Status;

        application.CompanyId = request.CompanyId;
        application.ResumeVersionId = request.ResumeVersionId;
        application.RoleTitle = request.RoleTitle;
        application.Location = request.Location;
        application.WorkMode = request.WorkMode;
        application.JobUrl = request.JobUrl;
        application.Source = request.Source;
        application.JobDescription = request.JobDescription;
        application.DateApplied = request.DateApplied;
        application.Status = request.Status;
        application.Notes = request.Notes;
        application.UpdatedAtUtc = DateTime.UtcNow;

        application.ApplicationTags.Clear();
        foreach (var tagId in request.TagIds ?? [])
        {
            application.ApplicationTags.Add(new ApplicationTag { JobApplicationId = application.Id, TagId = tagId });
        }

        if (statusChanged)
        {
            application.StatusHistory.Add(new ApplicationStatusHistory
            {
                JobApplicationId = application.Id,
                Status = request.Status,
                ChangedAtUtc = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();

        var saved = await LoadQuery(userId).AsNoTracking().FirstAsync(j => j.Id == id);
        return CommandResult<JobApplicationResponse>.Ok(MapToResponse(saved));
    }

    public async Task<CommandResult> DeleteAsync(string userId, Guid id)
    {
        var application = await db.JobApplications.FirstOrDefaultAsync(j => j.Id == id && j.UserId == userId);
        if (application is null)
        {
            return CommandResult.Missing();
        }

        db.JobApplications.Remove(application);
        await db.SaveChangesAsync();
        return CommandResult.Ok();
    }

    private IQueryable<JobApplication> LoadQuery(string userId) =>
        db.JobApplications
            .Include(j => j.Company)
            .Include(j => j.ResumeVersion)
            .Include(j => j.ApplicationTags)
            .ThenInclude(t => t.Tag)
            .Where(j => j.UserId == userId);

    private static IQueryable<JobApplication> ApplyFilters(IQueryable<JobApplication> query, JobApplicationQuery filter)
    {
        if (filter.Status is not null)
        {
            query = query.Where(j => j.Status == filter.Status.Value);
        }

        if (filter.CompanyId is not null)
        {
            query = query.Where(j => j.CompanyId == filter.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            query = query.Where(j => j.RoleTitle.ToLower().Contains(filter.Role.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(filter.Location))
        {
            query = query.Where(j => (j.Location ?? string.Empty).ToLower().Contains(filter.Location.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(filter.Source))
        {
            query = query.Where(j => (j.Source ?? string.Empty).ToLower().Contains(filter.Source.ToLower()));
        }

        if (filter.FromDate is not null)
        {
            query = query.Where(j => j.DateApplied >= filter.FromDate.Value);
        }

        if (filter.ToDate is not null)
        {
            query = query.Where(j => j.DateApplied <= filter.ToDate.Value);
        }

        return query;
    }

    private static IQueryable<JobApplication> ApplySorting(
        IQueryable<JobApplication> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        return sortBy?.ToLowerInvariant() switch
        {
            "createdat" or "created" => descending
                ? query.OrderByDescending(j => j.CreatedAtUtc)
                : query.OrderBy(j => j.CreatedAtUtc),
            "updatedat" or "updated" => descending
                ? query.OrderByDescending(j => j.UpdatedAtUtc)
                : query.OrderBy(j => j.UpdatedAtUtc),
            "roletitle" or "role" => descending
                ? query.OrderByDescending(j => j.RoleTitle)
                : query.OrderBy(j => j.RoleTitle),
            "company" => descending
                ? query.OrderByDescending(j => j.Company.Name)
                : query.OrderBy(j => j.Company.Name),
            "status" => descending
                ? query.OrderByDescending(j => j.Status)
                : query.OrderBy(j => j.Status),
            _ => descending
                ? query.OrderByDescending(j => j.DateApplied)
                : query.OrderBy(j => j.DateApplied),
        };
    }

    private static JobApplicationResponse MapToResponse(JobApplication application) => new(
        application.Id,
        application.CompanyId,
        application.Company.Name,
        application.ResumeVersionId,
        application.ResumeVersion?.Name,
        application.RoleTitle,
        application.Location,
        application.WorkMode,
        application.JobUrl,
        application.Source,
        application.JobDescription,
        application.DateApplied,
        application.Status,
        application.Notes,
        application.CreatedAtUtc,
        application.UpdatedAtUtc,
        application.ApplicationTags
            .Select(t => new TagResponse(t.Tag.Id, t.Tag.Name))
            .OrderBy(t => t.Name)
            .ToList());

    private async Task<IReadOnlyDictionary<string, string[]>> ValidateApplicationAsync(
        string userId,
        JobApplicationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.CompanyId == Guid.Empty)
        {
            errors[nameof(request.CompanyId)] = ["CompanyId is required."];
        }

        if (request.DateApplied == default)
        {
            errors[nameof(request.DateApplied)] = ["DateApplied is required."];
        }

        if (!Enum.IsDefined(typeof(WorkMode), request.WorkMode))
        {
            errors[nameof(request.WorkMode)] = ["WorkMode is not a valid value."];
        }

        if (!Enum.IsDefined(typeof(ApplicationStatus), request.Status))
        {
            errors[nameof(request.Status)] = ["Status is not a valid value."];
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var companyOwned = await db.Companies.AnyAsync(c => c.Id == request.CompanyId && c.UserId == userId);
        if (!companyOwned)
        {
            errors[nameof(request.CompanyId)] = ["The specified company does not exist."];
        }

        if (request.ResumeVersionId is not null)
        {
            var resumeOwned = await db.ResumeVersions
                .AnyAsync(r => r.Id == request.ResumeVersionId && r.UserId == userId);
            if (!resumeOwned)
            {
                errors[nameof(request.ResumeVersionId)] = ["The specified resume version does not exist."];
            }
        }

        if (request.TagIds is { Length: > 0 })
        {
            var ownedTagIds = await db.Tags
                .Where(t => request.TagIds.Contains(t.Id) && t.UserId == userId)
                .Select(t => t.Id)
                .ToListAsync();

            if (ownedTagIds.Count != request.TagIds.Length)
            {
                errors[nameof(request.TagIds)] = ["One or more tags do not exist."];
            }
        }

        return errors;
    }
}
