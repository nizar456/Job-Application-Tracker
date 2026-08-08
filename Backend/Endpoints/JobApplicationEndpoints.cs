using Backend.Models;
using Backend.Services;

namespace Backend.Endpoints;

public static class JobApplicationEndpoints
{
    public static IEndpointRouteBuilder MapJobApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/applications").WithTags("Applications");

        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapPatch("/{id:guid}/status", ChangeStatusAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext,
        [AsParameters] JobApplicationQuery query,
        JobApplicationService service)
    {
        var userId = EndpointHelpers.GetUserId(httpContext);
        var result = await service.ListAsync(userId, query);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAsync(
        HttpContext httpContext,
        Guid id,
        JobApplicationService service)
    {
        var userId = EndpointHelpers.GetUserId(httpContext);
        var application = await service.GetAsync(userId, id);
        return application is null
            ? EndpointHelpers.NotFoundProblem("The requested job application was not found.")
            : Results.Ok(application);
    }

    private static async Task<IResult> CreateAsync(
        HttpContext httpContext,
        JobApplicationRequest request,
        JobApplicationService service)
    {
        var validation = ValidationHelper.Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var userId = EndpointHelpers.GetUserId(httpContext);
        var result = await service.CreateAsync(userId, request);
        return result.IsSuccess
            ? Results.Created($"/applications/{result.Value!.Id}", result.Value)
            : EndpointHelpers.ToErrorResult(result, "The requested job application was not found.");
    }

    private static async Task<IResult> UpdateAsync(
        HttpContext httpContext,
        Guid id,
        JobApplicationRequest request,
        JobApplicationService service)
    {
        var validation = ValidationHelper.Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var userId = EndpointHelpers.GetUserId(httpContext);
        var result = await service.UpdateAsync(userId, id, request);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : EndpointHelpers.ToErrorResult(result, "The requested job application was not found.");
    }

    private static async Task<IResult> ChangeStatusAsync(
        HttpContext httpContext,
        Guid id,
        ChangeStatusRequest request,
        JobApplicationService service)
    {
        var validation = ValidationHelper.Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var userId = EndpointHelpers.GetUserId(httpContext);
        var result = await service.ChangeStatusAsync(userId, id, request);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : EndpointHelpers.ToErrorResult(result, "The requested job application was not found.");
    }

    private static async Task<IResult> DeleteAsync(
        HttpContext httpContext,
        Guid id,
        JobApplicationService service)
    {
        var userId = EndpointHelpers.GetUserId(httpContext);
        var result = await service.DeleteAsync(userId, id);
        return result.IsSuccess
            ? Results.NoContent()
            : EndpointHelpers.NotFoundProblem("The requested job application was not found.");
    }
}
