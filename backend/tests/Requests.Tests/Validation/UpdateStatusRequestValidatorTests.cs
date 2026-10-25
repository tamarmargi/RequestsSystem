using FluentAssertions;
using Requests.BL.Dtos;
using Requests.BL.Validation;
using Requests.DAL.Entities;

namespace Requests.Tests.Validation;

public class UpdateStatusRequestValidatorTests
{
    private const string ValidVersion = "AAAAAAAAB9E="; // 8 bytes

    private readonly UpdateStatusRequestValidator _validator = new();

    [Fact]
    public void Valid_Passes()
    {
        _validator.Validate(new UpdateStatusRequest(RequestStatus.Completed, ValidVersion)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Version_Missing_Fails(string? version)
    {
        var result = _validator.Validate(new UpdateStatusRequest(RequestStatus.Completed, version));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateStatusRequest.Version));
    }

    [Fact]
    public void Version_NotBase64_Fails()
    {
        var result = _validator.Validate(new UpdateStatusRequest(RequestStatus.Completed, "not base64!"));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateStatusRequest.Version));
    }

    [Theory]
    [InlineData("AAAAAAAAAA==")]     // 7 bytes
    [InlineData("AAAAAAAAAAAA")]     // 9 bytes
    [InlineData("AAAAAAAAAAAAAAAA")] // 12 bytes
    public void Version_WrongLength_Fails(string version)
    {
        var result = _validator.Validate(new UpdateStatusRequest(RequestStatus.Completed, version));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateStatusRequest.Version));
    }

    [Fact]
    public void NewStatus_Undefined_Fails()
    {
        var result = _validator.Validate(new UpdateStatusRequest((RequestStatus)99, ValidVersion));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateStatusRequest.NewStatus));
    }

    [Fact]
    public void NewStatus_Missing_Fails()
    {
        var result = _validator.Validate(new UpdateStatusRequest(null, ValidVersion));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(UpdateStatusRequest.NewStatus));
    }
}
