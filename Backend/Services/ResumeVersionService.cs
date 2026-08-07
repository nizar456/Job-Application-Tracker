using Backend.Data;
using Backend.Domain;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class ResumeVersionService(ApplicationDbContext db)
{
    public async Task<List<ResumeVersionResponse>> ListAsync(string userId) =>
        await db.ResumeVersions
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.Name)
            .Select(r => new ResumeVersionResponse(r.Id, r.Name, r.CreatedAtUtc))
            .ToListAsync();

    public async Task<ResumeVersionResponse?> GetAsync(string userId, Guid id) =>
        await db.ResumeVersions
            .AsNoTracking()
            .Where(r => r.Id == id && r.UserId == userId)
            .Select(r => new ResumeVersionResponse(r.Id, r.Name, r.CreatedAtUtc))
            .FirstOrDefaultAsync();

    public async Task<CommandResult<ResumeVersionResponse>> CreateAsync(string userId, ResumeVersionRequest request)
    {
        if (await NameExistsAsync(userId, request.Name, null))
        {
            return CommandResult<ResumeVersionResponse>.Failed(
                FieldErrors.For(nameof(request.Name), "A resume version with this name already exists."));
        }

        var resumeVersion = new ResumeVersion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
        };

        db.ResumeVersions.Add(resumeVersion);
        await db.SaveChangesAsync();

        return CommandResult<ResumeVersionResponse>.Ok(
            new ResumeVersionResponse(resumeVersion.Id, resumeVersion.Name, resumeVersion.CreatedAtUtc));
    }

    public async Task<CommandResult<ResumeVersionResponse>> UpdateAsync(string userId, Guid id, ResumeVersionRequest request)
    {
        var resumeVersion = await db.ResumeVersions.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (resumeVersion is null)
        {
            return CommandResult<ResumeVersionResponse>.Missing();
        }

        if (await NameExistsAsync(userId, request.Name, id))
        {
            return CommandResult<ResumeVersionResponse>.Failed(
                FieldErrors.For(nameof(request.Name), "A resume version with this name already exists."));
        }

        resumeVersion.Name = request.Name;
        await db.SaveChangesAsync();

        return CommandResult<ResumeVersionResponse>.Ok(
            new ResumeVersionResponse(resumeVersion.Id, resumeVersion.Name, resumeVersion.CreatedAtUtc));
    }

    public async Task<CommandResult> DeleteAsync(string userId, Guid id)
    {
        var resumeVersion = await db.ResumeVersions.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (resumeVersion is null)
        {
            return CommandResult.Missing();
        }

        db.ResumeVersions.Remove(resumeVersion);
        await db.SaveChangesAsync();
        return CommandResult.Ok();
    }

    private Task<bool> NameExistsAsync(string userId, string name, Guid? excludeId) =>
        db.ResumeVersions.AnyAsync(r => r.UserId == userId && r.Name == name && r.Id != excludeId);
}
