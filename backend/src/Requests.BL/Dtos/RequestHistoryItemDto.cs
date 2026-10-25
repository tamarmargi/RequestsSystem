using Requests.DAL.Entities;

namespace Requests.BL.Dtos;

public sealed record RequestHistoryItemDto(
    RequestStatus PreviousStatus,
    RequestStatus NewStatus,
    DateTime ChangedAt,
    string ChangedBy);
