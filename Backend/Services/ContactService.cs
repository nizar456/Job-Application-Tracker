using Backend.Data;
using Backend.Domain;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class ContactService(ApplicationDbContext db)
{
    public async Task<List<ContactResponse>?> ListAsync(string userId, Guid applicationId)
    {
        if (!await OwnsApplicationAsync(userId, applicationId))
        {
            return null;
        }

        return await db.Contacts
            .AsNoTracking()
            .Where(c => c.JobApplicationId == applicationId)
            .OrderBy(c => c.Name)
            .Select(c => new ContactResponse(c.Id, c.JobApplicationId, c.Name, c.Email, c.Phone, c.Role, c.Notes))
            .ToListAsync();
    }

    public async Task<ContactResponse?> GetAsync(string userId, Guid applicationId, Guid contactId)
    {
        if (!await OwnsApplicationAsync(userId, applicationId))
        {
            return null;
        }

        return await db.Contacts
            .AsNoTracking()
            .Where(c => c.Id == contactId && c.JobApplicationId == applicationId)
            .Select(c => new ContactResponse(c.Id, c.JobApplicationId, c.Name, c.Email, c.Phone, c.Role, c.Notes))
            .FirstOrDefaultAsync();
    }

    public async Task<CommandResult<ContactResponse>> CreateAsync(string userId, Guid applicationId, ContactRequest request)
    {
        if (!await OwnsApplicationAsync(userId, applicationId))
        {
            return CommandResult<ContactResponse>.Missing();
        }

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            JobApplicationId = applicationId,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Role = request.Role,
            Notes = request.Notes,
        };

        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        return CommandResult<ContactResponse>.Ok(MapToResponse(contact));
    }

    public async Task<CommandResult<ContactResponse>> UpdateAsync(
        string userId, Guid applicationId, Guid contactId, ContactRequest request)
    {
        var contact = await db.Contacts.FirstOrDefaultAsync(c => c.Id == contactId && c.JobApplicationId == applicationId);
        if (contact is null)
        {
            return CommandResult<ContactResponse>.Missing();
        }

        if (!await OwnsApplicationAsync(userId, applicationId))
        {
            return CommandResult<ContactResponse>.Missing();
        }

        contact.Name = request.Name;
        contact.Email = request.Email;
        contact.Phone = request.Phone;
        contact.Role = request.Role;
        contact.Notes = request.Notes;
        await db.SaveChangesAsync();

        return CommandResult<ContactResponse>.Ok(MapToResponse(contact));
    }

    public async Task<CommandResult> DeleteAsync(string userId, Guid applicationId, Guid contactId)
    {
        var contact = await db.Contacts.FirstOrDefaultAsync(c => c.Id == contactId && c.JobApplicationId == applicationId);
        if (contact is null)
        {
            return CommandResult.Missing();
        }

        if (!await OwnsApplicationAsync(userId, applicationId))
        {
            return CommandResult.Missing();
        }

        db.Contacts.Remove(contact);
        await db.SaveChangesAsync();
        return CommandResult.Ok();
    }

    private Task<bool> OwnsApplicationAsync(string userId, Guid applicationId) =>
        db.JobApplications.AnyAsync(j => j.Id == applicationId && j.UserId == userId);

    private static ContactResponse MapToResponse(Contact contact) => new(
        contact.Id,
        contact.JobApplicationId,
        contact.Name,
        contact.Email,
        contact.Phone,
        contact.Role,
        contact.Notes);
}
