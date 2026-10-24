using Requests.DAL.Entities;

namespace Requests.BL.Dtos;

/// <param name="Version">RowVersion as Base64; the client returns it unchanged on updates.</param>
public sealed record RequestDto(
    int Id,
    string Title,
    string OrganizationName,
    RequestStatus Status,
    RequestPriority Priority,
    string? AssignedTo,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string Version);
