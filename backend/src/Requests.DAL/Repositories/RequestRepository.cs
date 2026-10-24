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
            query = query.Where(r => r.Title.Contains(filter.Search) || r.OrganizationName.Contains(filter.Search));
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
