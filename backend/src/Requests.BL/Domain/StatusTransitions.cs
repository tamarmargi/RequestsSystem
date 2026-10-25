using Requests.DAL.Entities;

namespace Requests.BL.Domain;

/// <summary>
/// The request workflow. Completed is final, and a status cannot be changed to itself.
/// Consistent with the history paths of the seeded test data.
/// </summary>
public static class StatusTransitions
{
    private static readonly IReadOnlyDictionary<RequestStatus, RequestStatus[]> Allowed =
        new Dictionary<RequestStatus, RequestStatus[]>
        {
            [RequestStatus.New] = new[] { RequestStatus.InProgress },
            [RequestStatus.InProgress] = new[] { RequestStatus.Waiting, RequestStatus.Completed },
            [RequestStatus.Waiting] = new[] { RequestStatus.InProgress, RequestStatus.Completed },
            [RequestStatus.Completed] = Array.Empty<RequestStatus>()
        };

    public static bool IsAllowed(RequestStatus from, RequestStatus to) => GetAllowedTargets(from).Contains(to);

    public static IReadOnlyList<RequestStatus> GetAllowedTargets(RequestStatus from) =>
        Allowed.TryGetValue(from, out var targets) ? targets : Array.Empty<RequestStatus>();
}
