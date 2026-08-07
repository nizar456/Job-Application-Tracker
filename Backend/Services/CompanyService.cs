using Backend.Data;
using Backend.Domain;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class CompanyService(ApplicationDbContext db)
{
    public async Task<List<CompanyResponse>> ListAsync(string userId) =>
        await db.Companies
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new CompanyResponse(c.Id, c.Name, c.Website, c.CreatedAtUtc))
            .ToListAsync();

    public async Task<CompanyResponse?> GetAsync(string userId, Guid id) =>
        await db.Companies
            .AsNoTracking()
            .Where(c => c.Id == id && c.UserId == userId)
            .Select(c => new CompanyResponse(c.Id, c.Name, c.Website, c.CreatedAtUtc))
            .FirstOrDefaultAsync();

    public async Task<CommandResult<CompanyResponse>> CreateAsync(string userId, CompanyRequest request)
    {
        if (await NameExistsAsync(userId, request.Name, null))
        {
            return CommandResult<CompanyResponse>.Failed(
                FieldErrors.For(nameof(request.Name), "A company with this name already exists."));
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Website = request.Website,
        };

        db.Companies.Add(company);
        await db.SaveChangesAsync();

        return CommandResult<CompanyResponse>.Ok(
            new CompanyResponse(company.Id, company.Name, company.Website, company.CreatedAtUtc));
    }

    public async Task<CommandResult<CompanyResponse>> UpdateAsync(string userId, Guid id, CompanyRequest request)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (company is null)
        {
            return CommandResult<CompanyResponse>.Missing();
        }

        if (await NameExistsAsync(userId, request.Name, id))
        {
            return CommandResult<CompanyResponse>.Failed(
                FieldErrors.For(nameof(request.Name), "A company with this name already exists."));
        }

        company.Name = request.Name;
        company.Website = request.Website;
        await db.SaveChangesAsync();

        return CommandResult<CompanyResponse>.Ok(
            new CompanyResponse(company.Id, company.Name, company.Website, company.CreatedAtUtc));
    }

    public async Task<CommandResult> DeleteAsync(string userId, Guid id)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (company is null)
        {
            return CommandResult.Missing();
        }

        if (await db.JobApplications.AnyAsync(j => j.CompanyId == id && j.UserId == userId))
        {
            return CommandResult.Failed(
                FieldErrors.For(nameof(Company.Id), "This company has job applications and cannot be deleted."));
        }

        db.Companies.Remove(company);
        await db.SaveChangesAsync();
        return CommandResult.Ok();
    }

    private Task<bool> NameExistsAsync(string userId, string name, Guid? excludeId) =>
        db.Companies.AnyAsync(c => c.UserId == userId && c.Name == name && c.Id != excludeId);
}
