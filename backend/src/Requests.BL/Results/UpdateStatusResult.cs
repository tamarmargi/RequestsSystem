using Requests.BL.Dtos;

namespace Requests.BL.Results;

/// <summary>
/// Expected outcomes of a status update. A closed set: only the nested records can derive from it.
/// Invalid input is not a result; it throws <see cref="Exceptions.ValidationException"/>.
/// </summary>
public abstract record UpdateStatusResult
{
    private UpdateStatusResult()
    {
    }

    public sealed record Success(RequestDto Request) : UpdateStatusResult;

    public sealed record NotFound : UpdateStatusResult;

    /// <summary>The client's version is stale; <paramref name="Current"/> is the state in the database.</summary>
    public sealed record Conflict(RequestDto Current) : UpdateStatusResult;

    public sealed record InvalidTransition(string Message) : UpdateStatusResult;
}
