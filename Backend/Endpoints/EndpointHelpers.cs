using System.Security.Claims;
using Backend.Services;

namespace Backend.Endpoints;

public static class EndpointHelpers
{
    public static string GetUserId(HttpContext httpContext) =>
        httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? httpContext.User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("Authenticated user has no identifier claim.");

    public static IResult NotFoundProblem(string detail) => Results.Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Not Found",
        detail: detail);

    public static IResult ToErrorResult<T>(CommandResult<T> result, string notFoundDetail) =>
        result.NotFound
            ? NotFoundProblem(notFoundDetail)
            : Results.ValidationProblem(result.Errors!);

    public static IResult ToErrorResult(CommandResult result, string notFoundDetail) =>
        result.NotFound
            ? NotFoundProblem(notFoundDetail)
            : Results.ValidationProblem(result.Errors!);
}
