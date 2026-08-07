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

public class SupportingResourceTests : IClassFixture<BackendApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly BackendApiFactory _factory;
    private readonly HttpClient _client;

    public SupportingResourceTests(BackendApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Company_Create_ReturnsCreated()
    {
        var (token, _, _) = await RegisterUserAsync();

        var response = await _client.PostAsJsonAsync(
            "/companies", new CompanyRequest("Acme Corp", "https://acme.example"), JsonOptions, token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CompanyResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Acme Corp", body.Name);
        Assert.Equal("https://acme.example", body.Website);
    }

    [Fact]
    public async Task Company_Create_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/companies", new CompanyRequest("Acme Corp", null), JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Company_Create_DuplicateName_ReturnsValidationProblem()
    {
        var (token, _, _) = await RegisterUserAsync();
        await _client.PostAsJsonAsync("/companies", new CompanyRequest("Acme Corp", null), JsonOptions, token);

        var response = await _client.PostAsJsonAsync(
            "/companies", new CompanyRequest("Acme Corp", null), JsonOptions, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Company_Get_OtherUsers_ReturnsNotFound()
    {
        var (tokenA, userIdA, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userIdA, "Acme Corp");
        var (tokenB, _, _) = await RegisterUserAsync();

        var response = await _client.GetAsync($"/companies/{companyId}", tokenB);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Company_Update_OtherUsers_ReturnsNotFound()
    {
        var (tokenA, userIdA, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userIdA, "Acme Corp");
        var (tokenB, _, _) = await RegisterUserAsync();

        var response = await _client.PutAsJsonAsync(
            $"/companies/{companyId}", new CompanyRequest("Renamed", null), JsonOptions, tokenB);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Company_Delete_Own_ReturnsNoContent()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");

        var response = await _client.DeleteAsync($"/companies/{companyId}", token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Company_Delete_OtherUsers_ReturnsNotFound()
    {
        var (tokenA, userIdA, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userIdA, "Acme Corp");
        var (tokenB, _, _) = await RegisterUserAsync();

        var response = await _client.DeleteAsync($"/companies/{companyId}", tokenB);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Company_Delete_WithApplications_ReturnsValidationProblem()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");
        await CreateApplicationAsync(token, companyId);

        var response = await _client.DeleteAsync($"/companies/{companyId}", token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Company_List_OnlyReturnsOwn()
    {
        var (tokenA, userIdA, _) = await RegisterUserAsync();
        await SeedCompanyAsync(userIdA, "Acme Corp");
        var (tokenB, _, _) = await RegisterUserAsync();

        var response = await _client.GetAsync("/companies", tokenB);
        var companies = await response.Content.ReadFromJsonAsync<List<CompanyResponse>>(JsonOptions);

        Assert.NotNull(companies);
        Assert.Empty(companies);
    }

    [Fact]
    public async Task ResumeVersion_Create_ReturnsCreated()
    {
        var (token, _, _) = await RegisterUserAsync();

        var response = await _client.PostAsJsonAsync(
            "/resume-versions", new ResumeVersionRequest("Backend CV v3"), JsonOptions, token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ResumeVersionResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Backend CV v3", body.Name);
    }

    [Fact]
    public async Task ResumeVersion_Create_DuplicateName_ReturnsValidationProblem()
    {
        var (token, _, _) = await RegisterUserAsync();
        await _client.PostAsJsonAsync("/resume-versions", new ResumeVersionRequest("Backend CV v3"), JsonOptions, token);

        var response = await _client.PostAsJsonAsync(
            "/resume-versions", new ResumeVersionRequest("Backend CV v3"), JsonOptions, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResumeVersion_Get_OtherUsers_ReturnsNotFound()
    {
        var (tokenA, userIdA, _) = await RegisterUserAsync();
        var resumeId = await SeedResumeVersionAsync(userIdA, "Backend CV v3");
        var (tokenB, _, _) = await RegisterUserAsync();

        var response = await _client.GetAsync($"/resume-versions/{resumeId}", tokenB);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ResumeVersion_Delete_Own_ReturnsNoContent()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var resumeId = await SeedResumeVersionAsync(userId, "Backend CV v3");

        var response = await _client.DeleteAsync($"/resume-versions/{resumeId}", token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Tag_Create_ReturnsCreated()
    {
        var (token, _, _) = await RegisterUserAsync();

        var response = await _client.PostAsJsonAsync(
            "/tags", new TagRequest("backend"), JsonOptions, token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TagResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("backend", body.Name);
    }

    [Fact]
    public async Task Tag_Create_DuplicateName_ReturnsValidationProblem()
    {
        var (token, _, _) = await RegisterUserAsync();
        await _client.PostAsJsonAsync("/tags", new TagRequest("backend"), JsonOptions, token);

        var response = await _client.PostAsJsonAsync(
            "/tags", new TagRequest("backend"), JsonOptions, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Tag_Get_OtherUsers_ReturnsNotFound()
    {
        var (tokenA, userIdA, _) = await RegisterUserAsync();
        var tagId = await SeedTagAsync(userIdA, "backend");
        var (tokenB, _, _) = await RegisterUserAsync();

        var response = await _client.GetAsync($"/tags/{tagId}", tokenB);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Tag_CanBeAssignedToApplication()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");
        var tagId = await SeedTagAsync(userId, "backend");

        var response = await _client.PostAsJsonAsync(
            "/applications",
            ValidApplicationRequest(companyId) with { TagIds = [tagId] },
            JsonOptions,
            token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Single(body.Tags);
        Assert.Equal(tagId, body.Tags[0].Id);
    }

    [Fact]
    public async Task Contact_Create_OwnApplication_ReturnsCreated()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");
        var application = await CreateApplicationAsync(token, companyId);

        var response = await _client.PostAsJsonAsync(
            $"/applications/{application.Id}/contacts",
            new ContactRequest("Jane Doe", "jane@example.com", "123", "Recruiter", null),
            JsonOptions,
            token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ContactResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(application.Id, body.JobApplicationId);
        Assert.Equal("Jane Doe", body.Name);
    }

    [Fact]
    public async Task Contact_Create_OtherUsersApplication_ReturnsNotFound()
    {
        var (tokenA, userIdA, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userIdA, "Acme Corp");
        var application = await CreateApplicationAsync(tokenA, companyId);
        var (tokenB, _, _) = await RegisterUserAsync();

        var response = await _client.PostAsJsonAsync(
            $"/applications/{application.Id}/contacts",
            new ContactRequest("Jane Doe", null, null, null, null),
            JsonOptions,
            tokenB);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Contact_List_OwnApplication_ReturnsContacts()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");
        var application = await CreateApplicationAsync(token, companyId);
        await _client.PostAsJsonAsync(
            $"/applications/{application.Id}/contacts",
            new ContactRequest("Jane Doe", null, null, null, null),
            JsonOptions,
            token);

        var response = await _client.GetAsync($"/applications/{application.Id}/contacts", token);
        var contacts = await response.Content.ReadFromJsonAsync<List<ContactResponse>>(JsonOptions);

        Assert.NotNull(contacts);
        Assert.Single(contacts);
    }

    [Fact]
    public async Task Contact_List_OtherUsersApplication_ReturnsNotFound()
    {
        var (tokenA, userIdA, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userIdA, "Acme Corp");
        var application = await CreateApplicationAsync(tokenA, companyId);
        var (tokenB, _, _) = await RegisterUserAsync();

        var response = await _client.GetAsync($"/applications/{application.Id}/contacts", tokenB);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Contact_Get_OtherUsersApplication_ReturnsNotFound()
    {
        var (tokenA, userIdA, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userIdA, "Acme Corp");
        var application = await CreateApplicationAsync(tokenA, companyId);
        var contact = await CreateContactAsync(tokenA, application.Id, "Jane Doe");
        var (tokenB, _, _) = await RegisterUserAsync();

        var response = await _client.GetAsync(
            $"/applications/{application.Id}/contacts/{contact.Id}", tokenB);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Contact_Update_Own_ReturnsOk()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");
        var application = await CreateApplicationAsync(token, companyId);
        var contact = await CreateContactAsync(token, application.Id, "Jane Doe");

        var response = await _client.PutAsJsonAsync(
            $"/applications/{application.Id}/contacts/{contact.Id}",
            new ContactRequest("Jane Smith", "jane.smith@example.com", null, "Hiring Manager", null),
            JsonOptions,
            token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ContactResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Jane Smith", body.Name);
        Assert.Equal("Hiring Manager", body.Role);
    }

    [Fact]
    public async Task Contact_Delete_Own_ReturnsNoContent()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");
        var application = await CreateApplicationAsync(token, companyId);
        var contact = await CreateContactAsync(token, application.Id, "Jane Doe");

        var response = await _client.DeleteAsync(
            $"/applications/{application.Id}/contacts/{contact.Id}", token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Contact_Create_OversizedEmail_ReturnsValidationProblem()
    {
        var (token, userId, _) = await RegisterUserAsync();
        var companyId = await SeedCompanyAsync(userId, "Acme Corp");
        var application = await CreateApplicationAsync(token, companyId);
        var longEmail = new string('a', 300) + "@example.com";

        var response = await _client.PostAsJsonAsync(
            $"/applications/{application.Id}/contacts",
            new ContactRequest("Jane Doe", longEmail, null, null, null),
            JsonOptions,
            token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static JobApplicationRequest ValidApplicationRequest(Guid companyId) => new(
        CompanyId: companyId,
        ResumeVersionId: null,
        RoleTitle: "Software Engineer",
        Location: "Berlin",
        WorkMode: WorkMode.Hybrid,
        JobUrl: null,
        Source: null,
        JobDescription: null,
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

    private async Task<Guid> SeedResumeVersionAsync(string userId, string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var resume = new ResumeVersion { Id = Guid.NewGuid(), UserId = userId, Name = name };
        db.ResumeVersions.Add(resume);
        await db.SaveChangesAsync();
        return resume.Id;
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

    private async Task<JobApplicationResponse> CreateApplicationAsync(string token, Guid companyId)
    {
        var response = await _client.PostAsJsonAsync(
            "/applications", ValidApplicationRequest(companyId), JsonOptions, token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JobApplicationResponse>(JsonOptions))!;
    }

    private async Task<ContactResponse> CreateContactAsync(string token, Guid applicationId, string name)
    {
        var response = await _client.PostAsJsonAsync(
            $"/applications/{applicationId}/contacts",
            new ContactRequest(name, null, null, null, null),
            JsonOptions,
            token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ContactResponse>(JsonOptions))!;
    }
}
