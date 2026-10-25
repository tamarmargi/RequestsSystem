using FluentValidation.Results;

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

    public static ValidationException From(ValidationResult result) =>
        new(result.Errors
            .GroupBy(e => ToCamelCase(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
