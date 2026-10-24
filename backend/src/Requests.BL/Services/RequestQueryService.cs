using System.Diagnostics;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Requests.BL.Dtos;
using Requests.BL.Mapping;
using Requests.DAL.Repositories;
using ValidationException = Requests.BL.Exceptions.ValidationException;

namespace Requests.BL.Services;

public class RequestQueryService : IRequestQueryService
{
    private readonly IRequestRepository _repository;
    private readonly IValidator<RequestQueryParameters> _validator;
    private readonly ILogger<RequestQueryService> _logger;

    public RequestQueryService(
        IRequestRepository repository,
        IValidator<RequestQueryParameters> validator,
        ILogger<RequestQueryService> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<PagedResult<RequestDto>> SearchAsync(RequestQueryParameters parameters, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(parameters, ct);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors
                .GroupBy(e => ToCamelCase(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var filter = ToFilter(parameters);

        // The search text is free user input, so only whether it is present is logged.
        _logger.LogDebug(
            "Searching requests: Page={Page} PageSize={PageSize} HasSearch={HasSearch} Status={Status} Priority={Priority} " +
            "OrganizationName={OrganizationName} AssignedTo={AssignedTo} CreatedFrom={CreatedFrom} " +
            "CreatedToExclusive={CreatedToExclusive} SortBy={SortBy} Descending={Descending}",
            filter.Page, filter.PageSize, filter.Search is not null, filter.Status, filter.Priority,
            filter.OrganizationName, filter.AssignedTo, filter.CreatedFrom, filter.CreatedToExclusive,
            filter.SortBy, filter.Descending);

        var stopwatch = Stopwatch.StartNew();
        var (items, totalCount) = await _repository.SearchAsync(filter, ct);
        _logger.LogInformation(
            "Request search returned {Count} of {TotalCount} in {ElapsedMs} ms",
            items.Count, totalCount, stopwatch.ElapsedMilliseconds);

        return new PagedResult<RequestDto>(items.Select(r => r.ToDto()).ToList(), totalCount, filter.Page, filter.PageSize);
    }

    public async Task<RequestDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var request = await _repository.GetByIdAsync(id, ct);
        return request?.ToDto();
    }

    public async Task<RequestStatsDto> GetStatsAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var stats = await _repository.GetStatsAsync(ct);
        _logger.LogInformation("Request stats computed in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
        return stats.ToDto();
    }

    private static RequestFilter ToFilter(RequestQueryParameters p) => new()
    {
        Page = p.Page,
        PageSize = p.PageSize,
        Search = NullIfBlank(p.Search),
        Status = p.Status,
        Priority = p.Priority,
        OrganizationName = NullIfBlank(p.OrganizationName),
        AssignedTo = NullIfBlank(p.AssignedTo),
        CreatedFrom = p.CreatedFrom?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        // Inclusive day: everything before the start of the next day.
        CreatedToExclusive = p.CreatedTo?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        SortBy = ToSortField(p.SortBy),
        Descending = !string.Equals(
            p.SortDirection?.Trim(), RequestQueryParameters.SortDirectionAsc, StringComparison.OrdinalIgnoreCase)
    };

    // Values are validated before this runs; blank means the default.
    private static SortField ToSortField(string? sortBy) =>
        string.IsNullOrWhiteSpace(sortBy) ? SortField.CreatedAt : Enum.Parse<SortField>(sortBy.Trim(), ignoreCase: true);

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
