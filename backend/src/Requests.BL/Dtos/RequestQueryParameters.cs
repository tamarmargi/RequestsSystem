using Requests.DAL.Entities;

namespace Requests.BL.Dtos;

/// <summary>Query parameters of GET /api/requests (see docs/api-contract.md).</summary>
public sealed class RequestQueryParameters
{
    public const string SortDirectionAsc = "asc";
    public const string SortDirectionDesc = "desc";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>Contained in Title or OrganizationName.</summary>
    public string? Search { get; set; }

    public RequestStatus? Status { get; set; }
    public RequestPriority? Priority { get; set; }

    /// <summary>Exact match.</summary>
    public string? OrganizationName { get; set; }

    /// <summary>Exact match.</summary>
    public string? AssignedTo { get; set; }

    /// <summary>Inclusive (UTC day).</summary>
    public DateOnly? CreatedFrom { get; set; }

    /// <summary>Inclusive (UTC day).</summary>
    public DateOnly? CreatedTo { get; set; }

    /// <summary>createdAt | updatedAt | priority | title | organizationName.</summary>
    public string? SortBy { get; set; } = "createdAt";

    /// <summary>asc | desc.</summary>
    public string? SortDirection { get; set; } = SortDirectionDesc;
}
