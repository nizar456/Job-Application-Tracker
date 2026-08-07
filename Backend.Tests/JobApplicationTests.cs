using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Data;
using Backend.Domain;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests;

public class JobApplicationTests : IClassFixture<BackendApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly BackendApiFactory _factory;
    private readonly HttpClient _client;

    public JobApplicationTests(BackendApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedApplication()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");

        var response = await _client.PostAsJsonAsync(
            "/applications",
            ValidRequest(companyId) with { RoleTitle = "Backend Engineer" },
            JsonOptions,
            token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Backend Engineer", body.RoleTitle);
        Assert.Equal(companyId, body.CompanyId);
        Assert.Equal("Acme Corp", body.CompanyName);
        Assert.Equal(ApplicationStatus.Saved, body.Status);
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/applications", ValidRequest(Guid.NewGuid()), JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithAnotherUsersCompany_ReturnsValidationProblem()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var (otherToken, otherUserId, _) = await RegisterUserAsync();
        var otherCompanyId = await SeedCompanyAsync(otherUserId, "Other Inc");

        var response = await _client.PostAsJsonAsync(
            "/applications",
            ValidRequest(otherCompanyId),
            JsonOptions,
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_WithMissingRoleTitle_ReturnsValidationProblem()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");

        var response = await _client.PostAsJsonAsync(
            "/applications",
            ValidRequest(companyId) with { RoleTitle = "" },
            JsonOptions,
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_OwnedApplication_ReturnsIt()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");
        var created = await CreateApplicationAsync(token, companyId, status: ApplicationStatus.Applied);

        var response = await _client.GetAsync($"/applications/{created.Id}", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal(ApplicationStatus.Applied, body.Status);
    }

    [Fact]
    public async Task Get_OtherUsersApplication_ReturnsNotFound()
    {
        var (tokenA, userIdA, _) = await RegisterUserAsync();
        var companyA = await SeedCompanyAsync(userIdA, "Acme Corp");
        var created = await CreateApplicationAsync(tokenA, companyA);

        var (tokenB, _, _) = await RegisterUserAsync();
        var response = await _client.GetAsync($"/applications/{created.Id}", tokenB);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_OwnedApplication_UpdatesFieldsAndRecordsHistory()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");
        var created = await CreateApplicationAsync(token, companyId, status: ApplicationStatus.Saved);

        var updated = ValidRequest(companyId) with
        {
            RoleTitle = "Senior Backend Engineer",
            Status = ApplicationStatus.Interviewing,
            Notes = "Second round",
        };

        var response = await _client.PutAsJsonAsync($"/applications/{created.Id}", updated, JsonOptions, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Senior Backend Engineer", body.RoleTitle);
        Assert.Equal(ApplicationStatus.Interviewing, body.Status);
        Assert.Equal("Second round", body.Notes);

        var history = await ReadHistoryAsync(created.Id);
        Assert.Equal(2, history.Count);
        Assert.Contains(history, h => h.Status == ApplicationStatus.Saved);
        Assert.Contains(history, h => h.Status == ApplicationStatus.Interviewing);
    }

    [Fact]
    public async Task Update_OtherUsersApplication_ReturnsNotFound()
    {
        var (tokenA, userIdA, _) = await RegisterUserAsync();
        var companyA = await SeedCompanyAsync(userIdA, "Acme Corp");
        var created = await CreateApplicationAsync(tokenA, companyA);

        var (tokenB, userIdB, _) = await RegisterUserAsync();
        var companyB = await SeedCompanyAsync(userIdB, "Other Inc");

        var response = await _client.PutAsJsonAsync(
            $"/applications/{created.Id}",
            ValidRequest(companyB),
            JsonOptions,
            tokenB);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_OwnedApplication_DeletesIt()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");
        var created = await CreateApplicationAsync(token, companyId);

        var deleteResponse = await _client.DeleteAsync($"/applications/{created.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/applications/{created.Id}", token);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_OtherUsersApplication_ReturnsNotFound()
    {
        var (tokenA, userIdA, _) = await RegisterUserAsync();
        var companyA = await SeedCompanyAsync(userIdA, "Acme Corp");
        var created = await CreateApplicationAsync(tokenA, companyA);

        var (tokenB, _, _) = await RegisterUserAsync();
        var response = await _client.DeleteAsync($"/applications/{created.Id}", tokenB);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_OnlyReturnsOwnApplications()
    {
        var (tokenA, userIdA, _) = await RegisterUserAsync();
        var companyA = await SeedCompanyAsync(userIdA, "Acme Corp");
        await CreateApplicationAsync(tokenA, companyA);

        var (tokenB, _, _) = await RegisterUserAsync();
        var response = await _client.GetAsync("/applications", tokenB);

        var page = await response.Content.ReadFromJsonAsync<PagedResponse<JobApplicationResponse>>(JsonOptions);
        Assert.NotNull(page);
        Assert.Equal(0, page.TotalCount);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task List_FiltersByStatus_Company_Role_Location_Source()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var company1 = await SeedCompanyAsync(userId, "Acme Corp");
        var company2 = await SeedCompanyAsync(userId, "Globex");

        await CreateApplicationAsync(
            token, company1, status: ApplicationStatus.Applied,
            roleTitle: "Backend Engineer", location: "Berlin", source: "LinkedIn");
        await CreateApplicationAsync(
            token, company2, status: ApplicationStatus.Interviewing,
            roleTitle: "Frontend Engineer", location: "London", source: "Company Website");

        var byStatus = await ListAsync(token, "status=Applied");
        Assert.Single(byStatus.Items);
        Assert.Equal(ApplicationStatus.Applied, byStatus.Items[0].Status);

        var byCompany = await ListAsync(token, $"companyId={company1}");
        Assert.Single(byCompany.Items);
        Assert.Equal("Backend Engineer", byCompany.Items[0].RoleTitle);

        var byRole = await ListAsync(token, "role=backend");
        Assert.Single(byRole.Items);

        var byLocation = await ListAsync(token, "location=Berlin");
        Assert.Single(byLocation.Items);

        var bySource = await ListAsync(token, "source=LinkedIn");
        Assert.Single(bySource.Items);
    }

    [Fact]
    public async Task List_FiltersByDateRange()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");

        await CreateApplicationAsync(token, companyId, dateApplied: new DateOnly(2026, 1, 10));
        await CreateApplicationAsync(token, companyId, dateApplied: new DateOnly(2026, 6, 10));
        await CreateApplicationAsync(token, companyId, dateApplied: new DateOnly(2026, 8, 10));

        var page = await ListAsync(token, "fromDate=2026-03-01&toDate=2026-07-31");
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task List_Paginates()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");

        for (var i = 0; i < 25; i++)
        {
            await CreateApplicationAsync(token, companyId, roleTitle: $"Role {i}");
        }

        var page = await ListAsync(token, "page=2&pageSize=10");

        Assert.Equal(10, page.Items.Count);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task List_SortsByDateAppliedAscending()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");

        await CreateApplicationAsync(token, companyId, dateApplied: new DateOnly(2026, 8, 10), roleTitle: "Role 3");
        await CreateApplicationAsync(token, companyId, dateApplied: new DateOnly(2026, 1, 10), roleTitle: "Role 1");
        await CreateApplicationAsync(token, companyId, dateApplied: new DateOnly(2026, 5, 10), roleTitle: "Role 2");

        var page = await ListAsync(token, "sortBy=dateApplied&sortDirection=asc");

        Assert.Equal(new[] { "Role 1", "Role 2", "Role 3" }, page.Items.Select(i => i.RoleTitle).ToArray());
    }

    [Fact]
    public async Task Create_WithTags_ReturnsTagsOnApplication()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");
        var tagId = await SeedTagAsync(userId, "backend");

        var response = await _client.PostAsJsonAsync(
            "/applications",
            ValidRequest(companyId) with { TagIds = [tagId] },
            JsonOptions,
            token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Single(body.Tags);
        Assert.Equal(tagId, body.Tags[0].Id);
        Assert.Equal("backend", body.Tags[0].Name);
    }

    private static JobApplicationRequest ValidRequest(Guid companyId) => new(
        CompanyId: companyId,
        ResumeVersionId: null,
        RoleTitle: "Software Engineer",
        Location: "Berlin",
        WorkMode: WorkMode.Hybrid,
        JobUrl: "https://example.com/jobs/1",
        Source: "LinkedIn",
        JobDescription: "Awesome role",
        DateApplied: new DateOnly(2026, 8, 1),
        Status: ApplicationStatus.Saved,
        Notes: null,
        TagIds: null);

    private async Task<(string Token, string UserId, string Email)> RegisterUserAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"user{suffix}@example.com";
        var response = await _client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest($"user{suffix}", email, "Str0ng-Password!"));
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        var userId = new JwtSecurityTokenHandler().ReadJwtToken(auth!.Token).Subject;
        return (auth.Token, userId, email);
    }

    private async Task<Guid> SeedCompanyAsync(string userId, string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var company = new Company { Id = Guid.NewGuid(), UserId = userId, Name = name };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company.Id;
    }

    private async Task<Guid> SeedTagAsync(string userId, string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tag = new Tag { Id = Guid.NewGuid(), UserId = userId, Name = name };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();
        return tag.Id;
    }

    private async Task<List<ApplicationStatusHistory>> ReadHistoryAsync(Guid applicationId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.ApplicationStatusHistory
            .Where(h => h.JobApplicationId == applicationId)
            .OrderBy(h => h.ChangedAtUtc)
            .ToListAsync();
    }

    private async Task<JobApplicationResponse> CreateApplicationAsync(
        string token,
        Guid companyId,
        ApplicationStatus status = ApplicationStatus.Saved,
        string roleTitle = "Software Engineer",
        string? location = "Berlin",
        string? source = "LinkedIn",
        DateOnly? dateApplied = null)
    {
        var request = ValidRequest(companyId) with
        {
            Status = status,
            RoleTitle = roleTitle,
            Location = location,
            Source = source,
            DateApplied = dateApplied ?? new DateOnly(2026, 8, 1),
        };

        var response = await _client.PostAsJsonAsync("/applications", request, JsonOptions, token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions))!;
    }

    private async Task<PagedResponse<JobApplicationResponse>> ListAsync(string token, string query)
    {
        var response = await _client.GetAsync($"/applications?{query}", token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResponse<JobApplicationResponse>>(JsonOptions))!;
    }
}

internal static class HttpClientExtensions
{
    public static HttpRequestMessage WithAuth(this HttpRequestMessage request, string token)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public static Task<HttpResponseMessage> GetAsync(this HttpClient client, string url, string token) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url).WithAuth(token));

    public static Task<HttpResponseMessage> DeleteAsync(this HttpClient client, string url, string token) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, url).WithAuth(token));

    public static Task<HttpResponseMessage> PostAsJsonAsync<T>(
        this HttpClient client, string url, T value, JsonSerializerOptions options, string token) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(value, options: options) }.WithAuth(token));

    public static Task<HttpResponseMessage> PutAsJsonAsync<T>(
        this HttpClient client, string url, T value, JsonSerializerOptions options, string token) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Put, url) { Content = JsonContent.Create(value, options: options) }.WithAuth(token));
}
