using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Frontend.Services;

public sealed class ApiClient(HttpClient httpClient, TokenStore tokenStore, ILogger<ApiClient> logger, IJSRuntime jsRuntime)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, uri);
        using var response = await SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new ApiException("The API returned an empty response.", (int)response.StatusCode);
    }

    public async Task<T> PostAsync<T>(string uri, object content, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, uri, content);
        using var response = await SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new ApiException("The API returned an empty response.", (int)response.StatusCode);
    }

    public async Task PostAsync(string uri, object content, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, uri, content);
        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task PutAsync(string uri, object content, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Put, uri, content);
        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<T> PatchAsync<T>(string uri, object content, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Patch, uri, content);
        using var response = await SendAsync(request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new ApiException("The API returned an empty response.", (int)response.StatusCode);
    }

    public async Task DeleteAsync(string uri, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Delete, uri);
        using var response = await SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string uri, object? content = null)
    {
        var request = new HttpRequestMessage(method, uri);
        if (tokenStore.Token is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (content is not null)
        {
            request.Content = JsonContent.Create(content);
        }

        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await httpClient.SendAsync(request, cancellationToken);
        if ((int)response.StatusCode >= 400)
        {
            var message =
                $"[api] {request.Method} {request.RequestUri} -> {(int)response.StatusCode} {response.ReasonPhrase} | Authorization: {(tokenStore.Token is null ? "MISSING" : "attached")}";
            logger.LogInformation(message);
            try
            {
                await jsRuntime.InvokeVoidAsync("console.log", message);
            }
            catch
            {
            }
        }

        return response;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiException.FromResponseAsync(response, cancellationToken);
        }
    }
}