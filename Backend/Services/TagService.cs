using Backend.Data;
using Backend.Domain;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class TagService(ApplicationDbContext db)
{
    public async Task<List<TagResponse>> ListAsync(string userId) =>
        await db.Tags
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .Select(t => new TagResponse(t.Id, t.Name))
            .ToListAsync();

    public async Task<TagResponse?> GetAsync(string userId, Guid id) =>
        await db.Tags
            .AsNoTracking()
            .Where(t => t.Id == id && t.UserId == userId)
            .Select(t => new TagResponse(t.Id, t.Name))
            .FirstOrDefaultAsync();

    public async Task<CommandResult<TagResponse>> CreateAsync(string userId, TagRequest request)
    {
        if (await NameExistsAsync(userId, request.Name, null))
        {
            return CommandResult<TagResponse>.Failed(
                FieldErrors.For(nameof(request.Name), "A tag with this name already exists."));
        }

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
        };

        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        return CommandResult<TagResponse>.Ok(new TagResponse(tag.Id, tag.Name));
    }

    public async Task<CommandResult<TagResponse>> UpdateAsync(string userId, Guid id, TagRequest request)
    {
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (tag is null)
        {
            return CommandResult<TagResponse>.Missing();
        }

        if (await NameExistsAsync(userId, request.Name, id))
        {
            return CommandResult<TagResponse>.Failed(
                FieldErrors.For(nameof(request.Name), "A tag with this name already exists."));
        }

        tag.Name = request.Name;
        await db.SaveChangesAsync();

        return CommandResult<TagResponse>.Ok(new TagResponse(tag.Id, tag.Name));
    }

    public async Task<CommandResult> DeleteAsync(string userId, Guid id)
    {
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (tag is null)
        {
            return CommandResult.Missing();
        }

        db.Tags.Remove(tag);
        await db.SaveChangesAsync();
        return CommandResult.Ok();
    }

    private Task<bool> NameExistsAsync(string userId, string name, Guid? excludeId) =>
        db.Tags.AnyAsync(t => t.UserId == userId && t.Name == name && t.Id != excludeId);
}
