using Requests.DAL.Entities;

namespace Requests.BL.Domain;

/// <summary>
/// When a request is taken into work (New -> InProgress) and has no handler, the user who made the change
/// becomes its handler, so an in-progress request always has one. An existing handler is never overwritten,
/// and an anonymous change assigns nobody.
/// </summary>
public static class AutoAssignment
{
    /// <summary>The user name used when the client does not identify itself.</summary>
    public const string AnonymousUser = "anonymous";

    /// <returns>The handler the request should have after the change.</returns>
    public static string? HandlerAfterChange(RequestStatus from, RequestStatus to, string? currentHandler, string changedBy)
    {
        var takenIntoWork = from == RequestStatus.New && to == RequestStatus.InProgress;
        var canAssign = !string.IsNullOrWhiteSpace(changedBy) && changedBy != AnonymousUser;

        return takenIntoWork && string.IsNullOrWhiteSpace(currentHandler) && canAssign ? changedBy : currentHandler;
    }
}
