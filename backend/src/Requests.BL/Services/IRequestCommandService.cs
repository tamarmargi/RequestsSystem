using Requests.BL.Dtos;
using Requests.BL.Results;

namespace Requests.BL.Services;

public interface IRequestCommandService
{
    /// <param name="changedBy">Stored in the history row.</param>
    /// <exception cref="Exceptions.ValidationException">The body is invalid.</exception>
    Task<UpdateStatusResult> UpdateStatusAsync(int id, UpdateStatusRequest request, string changedBy, CancellationToken ct);
}
