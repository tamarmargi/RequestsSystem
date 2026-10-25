using Requests.BL.Dtos;

namespace Requests.BL.Services;

public interface IRequestQueryService
{
    /// <exception cref="Exceptions.ValidationException">The parameters are invalid.</exception>
    Task<PagedResult<RequestDto>> SearchAsync(RequestQueryParameters parameters, CancellationToken ct);

    /// <summary>The request, or null if it does not exist.</summary>
    Task<RequestDto?> GetByIdAsync(int id, CancellationToken ct);

    Task<RequestStatsDto> GetStatsAsync(CancellationToken ct);

    /// <summary>Status changes newest first, or null if the request does not exist.</summary>
    Task<IReadOnlyList<RequestHistoryItemDto>?> GetHistoryAsync(int id, CancellationToken ct);
}
