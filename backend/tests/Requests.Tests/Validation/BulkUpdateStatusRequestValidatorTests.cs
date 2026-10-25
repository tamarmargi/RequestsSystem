using FluentAssertions;
using Requests.BL.Dtos;
using Requests.BL.Validation;
using Requests.DAL.Entities;

namespace Requests.Tests.Validation;

public class BulkUpdateStatusRequestValidatorTests
{
    private const string ValidVersion = "AAAAAAAAB9E=";

    private readonly BulkUpdateStatusRequestValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void ValidItems_Pass(int count)
    {
        _validator.Validate(Request(Items(count))).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyList_Fails()
    {
        var result = _validator.Validate(Request(Array.Empty<BulkItem>()));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BulkUpdateStatusRequest.Items));
    }

    [Fact]
    public void MissingList_Fails()
    {
        var result = _validator.Validate(new BulkUpdateStatusRequest(RequestStatus.Completed, null));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BulkUpdateStatusRequest.Items));
    }

    [Fact]
    public void MoreThan100Items_Fails()
    {
        var result = _validator.Validate(Request(Items(101)));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BulkUpdateStatusRequest.Items))
            .Which.ErrorMessage.Should().Contain("100");
    }

    [Fact]
    public void DuplicateIds_Fails()
    {
        var result = _validator.Validate(Request(new[] { new BulkItem(5, ValidVersion), new BulkItem(5, ValidVersion) }));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BulkUpdateStatusRequest.Items))
            .Which.ErrorMessage.Should().Contain("duplicate").And.Contain("5");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not base64!")]
    [InlineData("AAAAAAAAAAAA")] // 9 bytes
    public void InvalidVersion_Fails(string? version)
    {
        var result = _validator.Validate(Request(new[] { new BulkItem(1, ValidVersion), new BulkItem(2, version) }));

        result.Errors.Should().ContainSingle(e => e.PropertyName == "Items[1].Version");
    }

    [Fact]
    public void InvalidStatus_Fails()
    {
        var result = _validator.Validate(new BulkUpdateStatusRequest((RequestStatus)99, Items(1)));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BulkUpdateStatusRequest.NewStatus));
    }

    [Fact]
    public void MissingStatus_Fails()
    {
        var result = _validator.Validate(new BulkUpdateStatusRequest(null, Items(1)));

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(BulkUpdateStatusRequest.NewStatus));
    }

    private static BulkUpdateStatusRequest Request(IReadOnlyList<BulkItem> items) => new(RequestStatus.Completed, items);

    private static BulkItem[] Items(int count) =>
        Enumerable.Range(1, count).Select(id => new BulkItem(id, ValidVersion)).ToArray();
}
