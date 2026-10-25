using FluentAssertions;
using Requests.BL.Domain;
using Requests.DAL.Entities;

namespace Requests.Tests.Domain;

public class StatusTransitionsTests
{
    private static readonly HashSet<(RequestStatus From, RequestStatus To)> Allowed = new()
    {
        (RequestStatus.New, RequestStatus.InProgress),
        (RequestStatus.InProgress, RequestStatus.Waiting),
        (RequestStatus.InProgress, RequestStatus.Completed),
        (RequestStatus.Waiting, RequestStatus.InProgress),
        (RequestStatus.Waiting, RequestStatus.Completed)
    };

    /// <summary>All 16 (from, to) pairs with the expected answer.</summary>
    public static TheoryData<RequestStatus, RequestStatus, bool> AllPairs()
    {
        var data = new TheoryData<RequestStatus, RequestStatus, bool>();
        foreach (var from in Enum.GetValues<RequestStatus>())
        {
            foreach (var to in Enum.GetValues<RequestStatus>())
            {
                data.Add(from, to, Allowed.Contains((from, to)));
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void IsAllowed_MatchesWorkflow(RequestStatus from, RequestStatus to, bool expected)
    {
        StatusTransitions.IsAllowed(from, to).Should().Be(expected);
    }

    [Fact]
    public void GetAllowedTargets_Completed_IsEmpty()
    {
        StatusTransitions.GetAllowedTargets(RequestStatus.Completed).Should().BeEmpty();
    }

    [Fact]
    public void GetAllowedTargets_InProgress_IsWaitingAndCompleted()
    {
        StatusTransitions.GetAllowedTargets(RequestStatus.InProgress)
            .Should().BeEquivalentTo(new[] { RequestStatus.Waiting, RequestStatus.Completed });
    }
}
