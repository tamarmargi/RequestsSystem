using FluentValidation;
using Requests.BL.Dtos;

namespace Requests.BL.Validation;

public class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    /// <summary>SQL Server rowversion is 8 bytes.</summary>
    public const int VersionLength = 8;

    public UpdateStatusRequestValidator()
    {
        RuleFor(r => r.NewStatus)
            .NotNull().WithMessage("newStatus is required.")
            .IsInEnum().WithMessage("newStatus must be one of: New, InProgress, Waiting, Completed.");

        RuleFor(r => r.Version)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("version is required.")
            .Must(BeValidVersion).WithMessage("version must be the Base64 value received from the API.");
    }

    public static bool BeValidVersion(string? version) => TryDecodeVersion(version, out _);

    public static bool TryDecodeVersion(string? version, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        Span<byte> buffer = stackalloc byte[VersionLength + 3];
        if (version is null || !Convert.TryFromBase64String(version, buffer, out var written) || written != VersionLength)
        {
            return false;
        }

        bytes = buffer[..written].ToArray();
        return true;
    }
}
