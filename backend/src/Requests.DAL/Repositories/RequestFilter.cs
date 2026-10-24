using Requests.DAL.Entities;

namespace Requests.DAL.Repositories;

public enum SortField
{
    CreatedAt,
    UpdatedAt,
    Priority,
    Title,
    OrganizationName
}

/// <summary>Already validated and normalized filter for <see cref="IRequestRepository.SearchAsync"/>.</summary>
public sealed record RequestFilter
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    /// <summary>Contained in Title or OrganizationName.</summary>
    public string? Search { get; init; }

    public RequestStatus? Status { get; init; }
    public RequestPriority? Priority { get; init; }

    /// <summary>Exact match.</summary>
    public string? OrganizationName { get; init; }

    /// <summary>Exact match.</summary>
    public string? AssignedTo { get; init; }

    /// <summary>Inclusive lower bound on CreatedAt (UTC).</summary>
    public DateTime? CreatedFrom { get; init; }

    /// <summary>Exclusive upper bound on CreatedAt (UTC).</summary>
    public DateTime? CreatedToExclusive { get; init; }

    public SortField SortBy { get; init; } = SortField.CreatedAt;
    public bool Descending { get; init; } = true;
}
