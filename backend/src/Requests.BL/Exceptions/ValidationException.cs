namespace Requests.BL.Exceptions;

/// <summary>Invalid input. The API maps it to 400 ProblemDetails with <see cref="Errors"/>.</summary>
public class ValidationException : Exception
{
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    /// <summary>Field name (camelCase, as in the API) -> error messages.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
