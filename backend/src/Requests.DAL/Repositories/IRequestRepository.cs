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

    Task<bool> ExistsAsync(int id, CancellationToken ct);

    /// <summary>The request as a tracked entity, for <see cref="SaveStatusChangeAsync"/>; null if it does not exist.</summary>
    Task<Request?> GetForUpdateAsync(int id, CancellationToken ct);

    /// <summary>
    /// Changes the status of a tracked request and adds its history row, in a single transaction. The UPDATE only
    /// succeeds if the row still has <paramref name="expectedVersion"/>.
    /// </summary>
    /// <returns>false if the request was changed or deleted by someone else in the meantime (nothing is saved).</returns>
    Task<bool> SaveStatusChangeAsync(
        Request request, byte[] expectedVersion, RequestStatus newStatus, string changedBy, CancellationToken ct);

    /// <summary>Status changes of one request, newest first. Not tracked.</summary>
    Task<IReadOnlyList<RequestStatusHistory>> GetHistoryAsync(int requestId, CancellationToken ct);
}
