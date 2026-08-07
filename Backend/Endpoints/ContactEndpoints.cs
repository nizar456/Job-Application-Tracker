using Backend.Models;
using Backend.Services;

namespace Backend.Endpoints;

public static class ContactEndpoints
{
    public static IEndpointRouteBuilder MapContactEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/applications/{applicationId:guid}/contacts").WithTags("Contacts");

        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(HttpContext httpContext, Guid applicationId, ContactService service)
    {
        var userId = EndpointHelpers.GetUserId(httpContext);
        var contacts = await service.ListAsync(userId, applicationId);
        return contacts is null
            ? EndpointHelpers.NotFoundProblem("The requested job application was not found.")
            : Results.Ok(contacts);
    }

    private static async Task<IResult> GetAsync(HttpContext httpContext, Guid applicationId, Guid id, ContactService service)
    {
        var userId = EndpointHelpers.GetUserId(httpContext);
        var contact = await service.GetAsync(userId, applicationId, id);
        return contact is null
            ? EndpointHelpers.NotFoundProblem("The requested contact was not found.")
            : Results.Ok(contact);
    }

    private static async Task<IResult> CreateAsync(
        HttpContext httpContext, Guid applicationId, ContactRequest request, ContactService service)
    {
        var validation = ValidationHelper.Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var userId = EndpointHelpers.GetUserId(httpContext);
        var result = await service.CreateAsync(userId, applicationId, request);
        return result.IsSuccess
            ? Results.Created($"/applications/{applicationId}/contacts/{result.Value!.Id}", result.Value)
            : EndpointHelpers.ToErrorResult(result, "The requested job application was not found.");
    }

    private static async Task<IResult> UpdateAsync(
        HttpContext httpContext, Guid applicationId, Guid id, ContactRequest request, ContactService service)
    {
        var validation = ValidationHelper.Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var userId = EndpointHelpers.GetUserId(httpContext);
        var result = await service.UpdateAsync(userId, applicationId, id, request);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : EndpointHelpers.ToErrorResult(result, "The requested contact was not found.");
    }

    private static async Task<IResult> DeleteAsync(
        HttpContext httpContext, Guid applicationId, Guid id, ContactService service)
    {
        var userId = EndpointHelpers.GetUserId(httpContext);
        var result = await service.DeleteAsync(userId, applicationId, id);
        return result.IsSuccess
            ? Results.NoContent()
            : EndpointHelpers.ToErrorResult(result, "The requested contact was not found.");
    }
}
