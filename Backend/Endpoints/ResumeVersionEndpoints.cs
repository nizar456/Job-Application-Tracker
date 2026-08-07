using Backend.Models;
using Backend.Services;

namespace Backend.Endpoints;

public static class ResumeVersionEndpoints
{
    public static IEndpointRouteBuilder MapResumeVersionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/resume-versions").WithTags("ResumeVersions");

        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(HttpContext httpContext, ResumeVersionService service)
    {
        var userId = EndpointHelpers.GetUserId(httpContext);
        return Results.Ok(await service.ListAsync(userId));
    }

    private static async Task<IResult> GetAsync(HttpContext httpContext, Guid id, ResumeVersionService service)
    {
        var userId = EndpointHelpers.GetUserId(httpContext);
        var resumeVersion = await service.GetAsync(userId, id);
        return resumeVersion is null
            ? EndpointHelpers.NotFoundProblem("The requested resume version was not found.")
            : Results.Ok(resumeVersion);
    }

    private static async Task<IResult> CreateAsync(HttpContext httpContext, ResumeVersionRequest request, ResumeVersionService service)
    {
        var validation = ValidationHelper.Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var userId = EndpointHelpers.GetUserId(httpContext);
        var result = await service.CreateAsync(userId, request);
        return result.IsSuccess
            ? Results.Created($"/resume-versions/{result.Value!.Id}", result.Value)
            : EndpointHelpers.ToErrorResult(result, "The requested resume version was not found.");
    }

    private static async Task<IResult> UpdateAsync(HttpContext httpContext, Guid id, ResumeVersionRequest request, ResumeVersionService service)
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
            : EndpointHelpers.ToErrorResult(result, "The requested resume version was not found.");
    }

    private static async Task<IResult> DeleteAsync(HttpContext httpContext, Guid id, ResumeVersionService service)
    {
        var userId = EndpointHelpers.GetUserId(httpContext);
        var result = await service.DeleteAsync(userId, id);
        return result.IsSuccess
            ? Results.NoContent()
            : EndpointHelpers.ToErrorResult(result, "The requested resume version was not found.");
    }
}
