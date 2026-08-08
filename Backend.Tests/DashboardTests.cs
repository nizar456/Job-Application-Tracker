using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Data;
using Backend.Domain;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests;

public class DashboardTests : IClassFixture<BackendApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly BackendApiFactory _factory;
    private readonly HttpClient _client;

    public DashboardTests(BackendApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithNoApplications_ReturnsEmptyDashboard()
    {
        var (token, _, _) = await RegisterUserAsync();

        var dashboard = await GetDashboardAsync(token);

        Assert.Equal(0, dashboard.TotalCount);
        Assert.Equal(0, dashboard.ActiveCount);
        Assert.Empty(dashboard.ApplicationsByStatus);
        Assert.Empty(dashboard.RecentApplications);
        Assert.Empty(dashboard.ApplicationsPerMonth);
        Assert.Equal(0, dashboard.ResponseRate.RespondedCount);
        Assert.Equal(0, dashboard.ResponseRate.BaseCount);
        Assert.Equal(0, dashboard.ResponseRate.Ratio);
    }

    [Fact]
    public async Task Get_CountsByStatusAndActiveApplications()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");

        await CreateApplicationAsync(token, companyId, status: ApplicationStatus.Applied);
        await CreateApplicationAsync(token, companyId, status: ApplicationStatus.Interviewing);
        await CreateApplicationAsync(token, companyId, status: ApplicationStatus.Saved);
        await CreateApplicationAsync(token, companyId, status: ApplicationStatus.Archived);

        var dashboard = await GetDashboardAsync(token);

        Assert.Equal(4, dashboard.TotalCount);
        Assert.Equal(3, dashboard.ActiveCount);

        Assert.Equal(
            new[] { ApplicationStatus.Saved, ApplicationStatus.Applied, ApplicationStatus.Interviewing, ApplicationStatus.Archived },
            dashboard.ApplicationsByStatus.Select(s => s.Status).ToArray());
        Assert.All(dashboard.ApplicationsByStatus, s => Assert.Equal(1, s.Count));
    }

    [Fact]
    public async Task Get_IsScopedToAuthenticatedUser()
    {
        var (tokenA, userIdA, _) = await RegisterUserAsync();
        var companyA = await SeedCompanyAsync(userIdA, "Acme Corp");
        await CreateApplicationAsync(tokenA, companyA, status: ApplicationStatus.Interviewing);

        var (tokenB, _, _) = await RegisterUserAsync();
        var dashboardB = await GetDashboardAsync(tokenB);

        Assert.Equal(0, dashboardB.TotalCount);
        Assert.Empty(dashboardB.ApplicationsByStatus);
    }

    [Fact]
    public async Task Get_ReturnsFiveMostRecentApplicationsByDateDescending()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");

        for (var i = 1; i <= 7; i++)
        {
            await CreateApplicationAsync(
                token, companyId, roleTitle: $"Role {i}",
                dateApplied: new DateOnly(2026, 2, i));
        }

        var dashboard = await GetDashboardAsync(token);

        Assert.Equal(5, dashboard.RecentApplications.Count);
        Assert.Equal(
            new[] { "Role 7", "Role 6", "Role 5", "Role 4", "Role 3" },
            dashboard.RecentApplications.Select(r => r.RoleTitle).ToArray());
    }

    [Fact]
    public async Task Get_GroupsApplicationsPerMonthAscending()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");

        await CreateApplicationAsync(token, companyId, dateApplied: new DateOnly(2026, 1, 10));
        await CreateApplicationAsync(token, companyId, dateApplied: new DateOnly(2026, 1, 20));
        await CreateApplicationAsync(token, companyId, dateApplied: new DateOnly(2026, 8, 5));

        var dashboard = await GetDashboardAsync(token);

        var months = dashboard.ApplicationsPerMonth;
        Assert.Equal(2, months.Count);
        Assert.Equal(new MonthCount("2026-01", 2), months[0]);
        Assert.Equal(new MonthCount("2026-08", 1), months[1]);
    }

    [Fact]
    public async Task Get_ResponseRate_IsRespondedDividedByActiveNonSaved()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");

        await CreateApplicationAsync(token, companyId, status: ApplicationStatus.Applied);
        await CreateApplicationAsync(token, companyId, status: ApplicationStatus.Interviewing);
        await CreateApplicationAsync(token, companyId, status: ApplicationStatus.Saved);
        await CreateApplicationAsync(token, companyId, status: ApplicationStatus.Archived);

        var dashboard = await GetDashboardAsync(token);

        Assert.Equal(4, dashboard.TotalCount);
        Assert.Equal(1, dashboard.ResponseRate.RespondedCount);
        Assert.Equal(2, dashboard.ResponseRate.BaseCount);
        Assert.Equal(0.5, dashboard.ResponseRate.Ratio);
    }

    [Fact]
    public async Task Get_ResponseRate_WithNoResponseBase_IsZero()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");

        await CreateApplicationAsync(token, companyId, status: ApplicationStatus.Saved);
        await CreateApplicationAsync(token, companyId, status: ApplicationStatus.Archived);

        var dashboard = await GetDashboardAsync(token);

        Assert.Equal(0, dashboard.ResponseRate.RespondedCount);
        Assert.Equal(0, dashboard.ResponseRate.BaseCount);
        Assert.Equal(0, dashboard.ResponseRate.Ratio);
    }

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

    private async Task<JobApplicationResponse> CreateApplicationAsync(
        string token,
        Guid companyId,
        ApplicationStatus status = ApplicationStatus.Saved,
        string roleTitle = "Software Engineer",
        DateOnly? dateApplied = null)
    {
        var request = new JobApplicationRequest(
            CompanyId: companyId,
            ResumeVersionId: null,
            RoleTitle: roleTitle,
            Location: "Berlin",
            WorkMode: WorkMode.Hybrid,
            JobUrl: "https://example.com/jobs/1",
            Source: "LinkedIn",
            JobDescription: "Awesome role",
            DateApplied: dateApplied ?? new DateOnly(2026, 8, 1),
            Status: status,
            Notes: null,
            TagIds: null);

        var response = await _client.PostAsJsonAsync("/applications", request, JsonOptions, token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions))!;
    }

    private async Task<DashboardResponse> GetDashboardAsync(string token)
    {
        var response = await _client.GetAsync("/dashboard", token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DashboardResponse>(JsonOptions))!;
    }
}