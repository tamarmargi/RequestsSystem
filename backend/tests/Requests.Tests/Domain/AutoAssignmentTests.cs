using FluentAssertions;
using Requests.BL.Domain;
using Requests.DAL.Entities;

namespace Requests.Tests.Domain;

public class AutoAssignmentTests
{
    [Fact]
    public void NewToInProgress_WithoutHandler_AssignsTheUser()
    {
        AutoAssignment.HandlerAfterChange(RequestStatus.New, RequestStatus.InProgress, null, "דנה לוי")
            .Should().Be("דנה לוי");
    }

    [Fact]
    public void NewToInProgress_ByAnonymous_StaysUnassigned()
    {
        AutoAssignment.HandlerAfterChange(RequestStatus.New, RequestStatus.InProgress, null, AutoAssignment.AnonymousUser)
            .Should().BeNull();
    }

    [Fact]
    public void NewToInProgress_WithHandler_KeepsTheHandler()
    {
        AutoAssignment.HandlerAfterChange(RequestStatus.New, RequestStatus.InProgress, "יוסי כהן", "דנה לוי")
            .Should().Be("יוסי כהן");
    }

    [Theory]
    [InlineData(RequestStatus.Waiting, RequestStatus.InProgress)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Waiting)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Completed)]
    [InlineData(RequestStatus.Waiting, RequestStatus.Completed)]
    public void OtherChanges_NeverAssign(RequestStatus from, RequestStatus to)
    {
        AutoAssignment.HandlerAfterChange(from, to, null, "דנה לוי").Should().BeNull();
    }
}
