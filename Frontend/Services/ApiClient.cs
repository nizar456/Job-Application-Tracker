using System.Net.Http.Json;

namespace Frontend.Services;

public sealed class ApiClient(HttpClient httpClient)
{
    public async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new ApiException("The API returned an empty response.", (int)response.StatusCode);
    }

    public async Task<T> PostAsync<T>(string uri, object content, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(uri, content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new ApiException("The API returned an empty response.", (int)response.StatusCode);
    }

    public async Task PostAsync(string uri, object content, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(uri, content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task PutAsync(string uri, object content, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(uri, content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<T> PatchAsync<T>(string uri, object content, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PatchAsJsonAsync(uri, content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new ApiException("The API returned an empty response.", (int)response.StatusCode);
    }

    public async Task DeleteAsync(string uri, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(uri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiException.FromResponseAsync(response, cancellationToken);
        }
    }
}