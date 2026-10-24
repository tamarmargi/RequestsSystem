using FluentAssertions;
using Requests.BL.Dtos;
using Requests.BL.Validation;

namespace Requests.Tests.Validation;

public class RequestQueryParametersValidatorTests
{
    private readonly RequestQueryParametersValidator _validator = new();

    [Fact]
    public void Defaults_AreValid()
    {
        _validator.Validate(new RequestQueryParameters()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSize_OutOfRange_Fails(int pageSize)
    {
        var result = _validator.Validate(new RequestQueryParameters { PageSize = pageSize });

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(RequestQueryParameters.PageSize));
    }

    [Fact]
    public void Page_Zero_Fails()
    {
        var result = _validator.Validate(new RequestQueryParameters { Page = 0 });

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(RequestQueryParameters.Page));
    }

    [Fact]
    public void CreatedFrom_AfterCreatedTo_Fails()
    {
        var result = _validator.Validate(new RequestQueryParameters
        {
            CreatedFrom = new DateOnly(2026, 5, 2),
            CreatedTo = new DateOnly(2026, 5, 1)
        });

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(RequestQueryParameters.CreatedTo));
    }

    [Fact]
    public void CreatedFrom_SameDayAsCreatedTo_IsValid()
    {
        var day = new DateOnly(2026, 5, 1);

        _validator.Validate(new RequestQueryParameters { CreatedFrom = day, CreatedTo = day }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void SortBy_Unknown_Fails()
    {
        var result = _validator.Validate(new RequestQueryParameters { SortBy = "id; DROP TABLE Requests" });

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(RequestQueryParameters.SortBy));
    }
}
