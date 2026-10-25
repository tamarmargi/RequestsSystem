using Requests.DAL.Entities;

namespace Requests.BL.Dtos;

/// <summary>Body of POST /api/requests/bulk-status.</summary>
/// <param name="NewStatus">Nullable so that a missing value is reported as a validation error, not read as New.</param>
public sealed record BulkUpdateStatusRequest(RequestStatus? NewStatus, IReadOnlyList<BulkItem>? Items);

/// <param name="Version">The version (Base64 RowVersion) the client last received for this request.</param>
public sealed record BulkItem(int Id, string? Version);
