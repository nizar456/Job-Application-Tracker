using System.Security.Claims;
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
        group.MapDelete("/{id:guid}", DeleteAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext,
        [AsParameters] JobApplicationQuery query,
        JobApplicationService service)
    {
        var userId = GetUserId(httpContext);
        var result = await service.ListAsync(userId, query);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAsync(
        HttpContext httpContext,
        Guid id,
        JobApplicationService service)
    {
        var userId = GetUserId(httpContext);
        var application = await service.GetAsync(userId, id);
        return application is null ? NotFoundProblem() : Results.Ok(application);
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

        var userId = GetUserId(httpContext);
        var result = await service.CreateAsync(userId, request);
        return result.IsSuccess
            ? Results.Created($"/applications/{result.Value!.Id}", result.Value)
            : ToErrorResult(result);
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

        var userId = GetUserId(httpContext);
        var result = await service.UpdateAsync(userId, id, request);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result);
    }

    private static async Task<IResult> DeleteAsync(
        HttpContext httpContext,
        Guid id,
        JobApplicationService service)
    {
        var userId = GetUserId(httpContext);
        var result = await service.DeleteAsync(userId, id);
        return result.IsSuccess ? Results.NoContent() : NotFoundProblem();
    }

    private static IResult ToErrorResult<T>(CommandResult<T> result) =>
        result.NotFound
            ? NotFoundProblem()
            : Results.ValidationProblem(result.Errors!);

    private static IResult NotFoundProblem() => Results.Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Not Found",
        detail: "The requested job application was not found.");

    private static string GetUserId(HttpContext httpContext) =>
        httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? httpContext.User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("Authenticated user has no identifier claim.");
}
