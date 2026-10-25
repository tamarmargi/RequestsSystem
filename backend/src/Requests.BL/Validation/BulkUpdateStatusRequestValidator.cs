using FluentValidation;
using Requests.BL.Dtos;

namespace Requests.BL.Validation;

/// <summary>Request-level rules: if any fails, the whole call is rejected with 400 and nothing is touched.</summary>
public class BulkUpdateStatusRequestValidator : AbstractValidator<BulkUpdateStatusRequest>
{
    public const int MaxItems = 100;

    public BulkUpdateStatusRequestValidator()
    {
        RuleFor(r => r.NewStatus)
            .NotNull().WithMessage("newStatus is required.")
            .IsInEnum().WithMessage("newStatus must be one of: New, InProgress, Waiting, Completed.");

        RuleFor(r => r.Items)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("items must contain at least one item.")
            .Must(items => items!.Count <= MaxItems).WithMessage($"items must contain at most {MaxItems} items.")
            .Must(HaveUniqueIds).WithMessage(r => $"items must not contain duplicate ids: {string.Join(", ", DuplicateIds(r.Items!))}.");

        RuleForEach(r => r.Items).ChildRules(item =>
            item.RuleFor(i => i.Version)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("version is required.")
                .Must(UpdateStatusRequestValidator.BeValidVersion)
                .WithMessage("version must be the Base64 value received from the API."))
            .When(r => r.Items is { Count: > 0 and <= MaxItems });
    }

    private static bool HaveUniqueIds(IReadOnlyList<BulkItem>? items) => !DuplicateIds(items!).Any();

    private static IEnumerable<int> DuplicateIds(IReadOnlyList<BulkItem> items) =>
        items.GroupBy(i => i.Id).Where(g => g.Count() > 1).Select(g => g.Key);
}
