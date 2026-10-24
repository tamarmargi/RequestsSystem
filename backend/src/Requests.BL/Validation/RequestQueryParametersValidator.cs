using FluentValidation;
using Requests.BL.Dtos;

namespace Requests.BL.Validation;

public class RequestQueryParametersValidator : AbstractValidator<RequestQueryParameters>
{
    public const int MaxPageSize = 100;
    public const int MaxSearchLength = 100;

    // Match the column lengths in the DAL configuration.
    public const int MaxOrganizationNameLength = 200;
    public const int MaxAssignedToLength = 100;

    public static readonly IReadOnlyList<string> SortFields =
        new[] { "createdAt", "updatedAt", "priority", "title", "organizationName" };

    public static readonly IReadOnlyList<string> SortDirections =
        new[] { RequestQueryParameters.SortDirectionAsc, RequestQueryParameters.SortDirectionDesc };

    public RequestQueryParametersValidator()
    {
        RuleFor(p => p.Page)
            .GreaterThanOrEqualTo(1).WithMessage("page must be 1 or greater.");

        RuleFor(p => p.PageSize)
            .InclusiveBetween(1, MaxPageSize).WithMessage($"pageSize must be between 1 and {MaxPageSize}.");

        RuleFor(p => p.Search)
            .MaximumLength(MaxSearchLength).WithMessage($"search must be at most {MaxSearchLength} characters.");

        RuleFor(p => p.OrganizationName)
            .MaximumLength(MaxOrganizationNameLength)
            .WithMessage($"organizationName must be at most {MaxOrganizationNameLength} characters.");

        RuleFor(p => p.AssignedTo)
            .MaximumLength(MaxAssignedToLength).WithMessage($"assignedTo must be at most {MaxAssignedToLength} characters.");

        // Model binding also accepts numbers for enums, so out-of-range values are rejected here.
        RuleFor(p => p.Status)
            .IsInEnum().WithMessage("status must be one of: New, InProgress, Waiting, Completed.");

        RuleFor(p => p.Priority)
            .IsInEnum().WithMessage("priority must be one of: Low, Medium, High.");

        RuleFor(p => p.CreatedTo)
            .GreaterThanOrEqualTo(p => p.CreatedFrom)
            .When(p => p.CreatedFrom is not null && p.CreatedTo is not null)
            .WithMessage("createdTo must be on or after createdFrom.");

        RuleFor(p => p.SortBy)
            .Must(value => IsOneOf(value, SortFields))
            .When(p => !string.IsNullOrWhiteSpace(p.SortBy))
            .WithMessage($"sortBy must be one of: {string.Join(", ", SortFields)}.");

        RuleFor(p => p.SortDirection)
            .Must(value => IsOneOf(value, SortDirections))
            .When(p => !string.IsNullOrWhiteSpace(p.SortDirection))
            .WithMessage($"sortDirection must be one of: {string.Join(", ", SortDirections)}.");
    }

    private static bool IsOneOf(string? value, IReadOnlyList<string> allowed) =>
        allowed.Contains(value!.Trim(), StringComparer.OrdinalIgnoreCase);
}
