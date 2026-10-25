using Requests.BL.Dtos;
using Requests.BL.Results;

namespace Requests.BL.Services;

public interface IRequestCommandService
{
    /// <param name="changedBy">Stored in the history row.</param>
    /// <exception cref="Exceptions.ValidationException">The body is invalid.</exception>
    Task<UpdateStatusResult> UpdateStatusAsync(int id, UpdateStatusRequest request, string changedBy, CancellationToken ct);

    /// <summary>
    /// Partial success: each item is checked like a single update and saved in its own transaction; a failed item
    /// never rolls back the others. Results are in the order of the input items.
    /// </summary>
    /// <exception cref="Exceptions.ValidationException">The request itself is invalid; nothing was changed.</exception>
    Task<BulkUpdateResultDto> BulkUpdateStatusAsync(BulkUpdateStatusRequest request, string changedBy, CancellationToken ct);
}
