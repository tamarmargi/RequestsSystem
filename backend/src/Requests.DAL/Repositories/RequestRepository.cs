using Microsoft.EntityFrameworkCore;
using Requests.DAL.Entities;

namespace Requests.DAL.Repositories;

public class RequestRepository : IRequestRepository
{
    private readonly AppDbContext _db;

    public RequestRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<Request> Items, int TotalCount)> SearchAsync(RequestFilter filter, CancellationToken ct)
    {
        var query = ApplyFilter(_db.Requests.AsNoTracking(), filter);

        var totalCount = await query.CountAsync(ct);

        var items = await ApplySort(query, filter.SortBy, filter.Descending)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<Request?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Requests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct) =>
        _db.Requests.AnyAsync(r => r.Id == id, ct);

    public Task<Request?> GetForUpdateAsync(int id, CancellationToken ct) =>
        _db.Requests.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyDictionary<int, Request>> GetManyForUpdateAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        await _db.Requests
            .Where(r => ids.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, ct);

    public async Task<bool> SaveStatusChangeAsync(
        Request request, byte[] expectedVersion, RequestStatus newStatus, string changedBy, CancellationToken ct)
    {
        var requestEntry = _db.Entry(request);

        // EF puts the original RowVersion into the WHERE clause of the UPDATE: the database itself rejects a stale version.
        requestEntry.Property(r => r.RowVersion).OriginalValue = expectedVersion;

        var history = new RequestStatusHistory
        {
            RequestId = request.Id,
            PreviousStatus = request.Status,
            NewStatus = newStatus,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = changedBy
        };
        request.Status = newStatus;
        var historyEntry = _db.RequestStatusHistory.Add(history);

        try
        {
            // One SaveChanges = one transaction for the UPDATE and the history INSERT.
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Leave the context clean: nothing from the failed attempt may be saved by a later SaveChanges.
            historyEntry.State = EntityState.Detached;
            requestEntry.State = EntityState.Detached;
            return false;
        }
    }

    public async Task<IReadOnlyList<RequestStatusHistory>> GetHistoryAsync(int requestId, CancellationToken ct) =>
        await _db.RequestStatusHistory
            .AsNoTracking()
            .Where(h => h.RequestId == requestId)
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)
            .ToListAsync(ct);

    public async Task<RequestStats> GetStatsAsync(CancellationToken ct)
    {
        // Grouping by a constant gives one group over the whole table: a single SELECT with conditional COUNTs.
        var rows = await _db.Requests
            .GroupBy(_ => 1)
            .Select(g => new RequestStats(
                g.Count(),
                g.Count(r => r.Status == RequestStatus.New),
                g.Count(r => r.Status == RequestStatus.InProgress),
                g.Count(r => r.Status == RequestStatus.Waiting),
                g.Count(r => r.Status == RequestStatus.Completed),
                g.Count(r => r.Priority == RequestPriority.Low),
                g.Count(r => r.Priority == RequestPriority.Medium),
                g.Count(r => r.Priority == RequestPriority.High)))
            .ToListAsync(ct);

        // At most one row; an empty table has no group at all.
        return rows.SingleOrDefault() ?? RequestStats.Empty;
    }

    private static IQueryable<Request> ApplyFilter(IQueryable<Request> query, RequestFilter filter)
    {
        if (filter.Search is not null)
        {
            query = filter.SearchId is null
                ? query.Where(r => r.Title.Contains(filter.Search) || r.OrganizationName.Contains(filter.Search))
                : WhereTextOrId(query, filter.Search, filter.SearchId.Value);
        }

        if (filter.Status is not null)
        {
            query = query.Where(r => r.Status == filter.Status);
        }

        if (filter.Priority is not null)
        {
            query = query.Where(r => r.Priority == filter.Priority);
        }

        if (filter.OrganizationName is not null)
        {
            query = query.Where(r => r.OrganizationName == filter.OrganizationName);
        }

        if (filter.AssignedTo is not null)
        {
            query = query.Where(r => r.AssignedTo == filter.AssignedTo);
        }

        if (filter.CreatedFrom is not null)
        {
            query = query.Where(r => r.CreatedAt >= filter.CreatedFrom);
        }

        if (filter.CreatedToExclusive is not null)
        {
            query = query.Where(r => r.CreatedAt < filter.CreatedToExclusive);
        }

        return query;
    }

    // A numeric search also matches the request number: one more OR in the same WHERE, which costs nothing
    // extra because the LIKE already scans. A separate method keeps the text-only query (and its parameter
    // names) exactly as before.
    private static IQueryable<Request> WhereTextOrId(IQueryable<Request> query, string search, int id) =>
        query.Where(r => r.Title.Contains(search) || r.OrganizationName.Contains(search) || r.Id == id);

    // Id is always the secondary sort, in the same direction, so rows with equal values keep a stable order across pages.
    private static IOrderedQueryable<Request> ApplySort(IQueryable<Request> query, SortField sortBy, bool descending) =>
        sortBy switch
        {
            SortField.CreatedAt => OrderWithTieBreaker(query, r => r.CreatedAt, descending),
            SortField.UpdatedAt => OrderWithTieBreaker(query, r => r.UpdatedAt, descending),
            // Stored as tinyint, so this sorts by the numeric value: Low < Medium < High.
            SortField.Priority => OrderWithTieBreaker(query, r => r.Priority, descending),
            SortField.Title => OrderWithTieBreaker(query, r => r.Title, descending),
            SortField.OrganizationName => OrderWithTieBreaker(query, r => r.OrganizationName, descending),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        };

    private static IOrderedQueryable<Request> OrderWithTieBreaker<TKey>(
        IQueryable<Request> query, System.Linq.Expressions.Expression<Func<Request, TKey>> key, bool descending) =>
        descending
            ? query.OrderByDescending(key).ThenByDescending(r => r.Id)
            : query.OrderBy(key).ThenBy(r => r.Id);
}
