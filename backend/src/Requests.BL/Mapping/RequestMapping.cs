using Requests.BL.Dtos;
using Requests.DAL.Entities;
using Requests.DAL.Repositories;

namespace Requests.BL.Mapping;

public static class RequestMapping
{
    public static RequestDto ToDto(this Request request) => new(
        request.Id,
        request.Title,
        request.OrganizationName,
        request.Status,
        request.Priority,
        request.AssignedTo,
        AsUtc(request.CreatedAt),
        AsUtc(request.UpdatedAt),
        Convert.ToBase64String(request.RowVersion));

    public static RequestHistoryItemDto ToDto(this RequestStatusHistory history) => new(
        history.PreviousStatus,
        history.NewStatus,
        AsUtc(history.ChangedAt),
        history.ChangedBy);

    public static RequestStatsDto ToDto(this RequestStats stats) => new(
        new StatusCountsDto(stats.New, stats.InProgress, stats.Waiting, stats.Completed),
        new PriorityCountsDto(stats.Low, stats.Medium, stats.High),
        stats.Total);

    // datetime2 has no kind; all dates are stored in UTC, so mark them as such (serialized with a "Z").
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
