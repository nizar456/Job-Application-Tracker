using System.ComponentModel.DataAnnotations;
using Backend.Domain;

namespace Backend.Models;

public record JobApplicationRequest(
    Guid CompanyId,
    Guid? ResumeVersionId,
    [property: Required, StringLength(200)] string RoleTitle,
    [property: StringLength(200)] string? Location,
    [property: EnumDataType(typeof(WorkMode))] WorkMode WorkMode,
    [property: StringLength(500)] string? JobUrl,
    [property: StringLength(200)] string? Source,
    string? JobDescription,
    DateOnly DateApplied,
    [property: EnumDataType(typeof(ApplicationStatus))] ApplicationStatus Status,
    string? Notes,
    Guid[]? TagIds);

public record TagResponse(Guid Id, string Name);

public record ChangeStatusRequest(
    [property: EnumDataType(typeof(ApplicationStatus))] ApplicationStatus Status,
    [property: StringLength(1000)] string? Note);

public record JobApplicationResponse(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    Guid? ResumeVersionId,
    string? ResumeVersionName,
    string RoleTitle,
    string? Location,
    WorkMode WorkMode,
    string? JobUrl,
    string? Source,
    string? JobDescription,
    DateOnly DateApplied,
    ApplicationStatus Status,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<TagResponse> Tags);

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
