using System.Text.Json.Serialization;

namespace Requests.BL.Dtos;

public sealed record RequestStatsDto(StatusCountsDto ByStatus, PriorityCountsDto ByPriority, int Total);

// Keys are the enum names as-is (not camelCase), as in docs/api-contract.md.
public sealed record StatusCountsDto(
    [property: JsonPropertyName("New")] int New,
    [property: JsonPropertyName("InProgress")] int InProgress,
    [property: JsonPropertyName("Waiting")] int Waiting,
    [property: JsonPropertyName("Completed")] int Completed);

public sealed record PriorityCountsDto(
    [property: JsonPropertyName("Low")] int Low,
    [property: JsonPropertyName("Medium")] int Medium,
    [property: JsonPropertyName("High")] int High);
