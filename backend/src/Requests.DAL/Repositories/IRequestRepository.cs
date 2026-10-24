using Requests.DAL.Entities;

namespace Requests.DAL.Repositories;

public interface IRequestRepository
{
    /// <summary>One page of matching requests plus the total number of matches. Not tracked.</summary>
    Task<(IReadOnlyList<Request> Items, int TotalCount)> SearchAsync(RequestFilter filter, CancellationToken ct);

    /// <summary>The request, or null if it does not exist. Not tracked.</summary>
    Task<Request?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>Counts over all requests, computed in a single SQL query.</summary>
    Task<RequestStats> GetStatsAsync(CancellationToken ct);
}
