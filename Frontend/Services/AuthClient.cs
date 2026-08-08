using System.Net;
using System.Net.Http.Json;
using Frontend.Models;

namespace Frontend.Services;

public sealed class AuthClient(HttpClient httpClient)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("auth/register", request, cancellationToken);
        return await ReadAuthResponseAsync(response, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("auth/login", request, cancellationToken);
        return await ReadAuthResponseAsync(response, cancellationToken);
    }

    private static async Task<AuthResponse> ReadAuthResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken)
                ?? throw new ApiException("The API returned an empty response.", (int)response.StatusCode);
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ApiException("The email or password is incorrect.", (int)HttpStatusCode.Unauthorized, "Unauthorized");
        }

        throw await ApiException.FromResponseAsync(response, cancellationToken);
    }
}