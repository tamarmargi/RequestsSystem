using Requests.DAL.Entities;

namespace Requests.BL.Dtos;

/// <summary>Body of PATCH /api/requests/{id}/status.</summary>
/// <param name="NewStatus">Nullable so that a missing value is reported as a validation error, not read as New.</param>
/// <param name="Version">The version (Base64 RowVersion) the client last received for this request.</param>
public sealed record UpdateStatusRequest(RequestStatus? NewStatus, string? Version);
