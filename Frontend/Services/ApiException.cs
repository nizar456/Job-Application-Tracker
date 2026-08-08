using System.Net.Http.Json;

namespace Frontend.Services;

public class ApiException : Exception
{
    public ApiException(string message, int statusCode, string? title = null, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Title = title;
        Errors = errors;
    }

    public int StatusCode { get; }

    public string? Title { get; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public static async Task<ApiException> FromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        string? title = null;
        IReadOnlyDictionary<string, string[]>? errors = null;

        if (response.Content.Headers.ContentType?.MediaType is "application/problem+json")
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(cancellationToken: cancellationToken);
            title = problem?.Title;
            errors = problem?.Errors;
        }

        var message = errors is { Count: > 0 }
            ? string.Join(" ", errors.SelectMany(pair => pair.Value))
            : title ?? $"The request failed with status code {statusCode}.";

        return new ApiException(message, statusCode, title, errors);
    }

    private sealed record ProblemPayload(string? Title, Dictionary<string, string[]>? Errors);
}