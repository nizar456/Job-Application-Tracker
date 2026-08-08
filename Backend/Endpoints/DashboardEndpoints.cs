using Backend.Services;

namespace Backend.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/dashboard").WithTags("Dashboard");

        group.MapGet("/", GetAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(HttpContext httpContext, DashboardService service)
    {
        var userId = EndpointHelpers.GetUserId(httpContext);
        return Results.Ok(await service.GetAsync(userId));
    }
}