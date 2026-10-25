using System.Text.Json.Serialization;

namespace Requests.BL.Dtos;

public enum BulkOutcome
{
    Updated,
    NotFound,
    Conflict,
    InvalidTransition
}

/// <param name="Results">One entry per input item, in the order of the request.</param>
public sealed record BulkUpdateResultDto(int Succeeded, int Failed, IReadOnlyList<BulkItemResultDto> Results);

/// <param name="Message">Present on failures.</param>
/// <param name="Version">The new version, present on <see cref="BulkOutcome.Updated"/>.</param>
public sealed record BulkItemResultDto(
    int Id,
    BulkOutcome Outcome,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Message = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Version = null);
