using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Models;

namespace Backend.Tests;

public class AuthTests : IClassFixture<BackendApiFactory>
{
    private readonly HttpClient _client;

    public AuthTests(BackendApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidCredentials_ReturnsToken()
    {
        var request = ValidRegisterRequest();

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
        Assert.Equal(request.UserName, body.UserName);
        Assert.Equal(request.Email, body.Email);
        Assert.True(body.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var request = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/auth/register", request);

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest(request.Email, request.Password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var request = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/auth/register", request);

        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(request.Email, "Wrong-Password-123"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("nobody@example.com", "Some-Password-123"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/weatherforecast");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithToken_ReturnsOk()
    {
        var request = ValidRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/auth/register", request);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/weatherforecast");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var response = await _client.SendAsync(requestMessage);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_WithoutToken_IsAccessible()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsValidationProblem()
    {
        var request = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/auth/register", request);

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static RegisterRequest ValidRegisterRequest()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new RegisterRequest(
            UserName: $"user{suffix}",
            Email: $"user{suffix}@example.com",
            Password: "Str0ng-Password!");
    }
}
