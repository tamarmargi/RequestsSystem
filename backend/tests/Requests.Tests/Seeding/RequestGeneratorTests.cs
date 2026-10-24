using FluentAssertions;
using Requests.DAL.Entities;
using Requests.DAL.Seeding;

namespace Requests.Tests.Seeding;

public class RequestGeneratorTests
{
    private const int Count = 10_000;

    private static readonly GeneratedData Data = new RequestGenerator().Generate(Count);

    [Fact]
    public void Generate_SameSeed_ProducesIdenticalData()
    {
        var second = new RequestGenerator().Generate(Count);

        second.Requests.Should().Equal(Data.Requests);
        second.History.Should().Equal(Data.History);
    }

    [Theory]
    [InlineData(RequestStatus.Completed, 0.50)]
    [InlineData(RequestStatus.InProgress, 0.25)]
    [InlineData(RequestStatus.New, 0.15)]
    [InlineData(RequestStatus.Waiting, 0.10)]
    public void Generate_StatusDistribution_IsWithinTwoPercentOfTarget(RequestStatus status, double target)
    {
        var share = Data.Requests.Count(r => r.Status == status) / (double)Count;

        share.Should().BeApproximately(target, 0.02);
    }

    [Fact]
    public void Generate_NonNewRequests_HaveConsistentHistory()
    {
        var historyByRequest = Data.History.ToLookup(h => h.RequestId);

        foreach (var request in Data.Requests.Where(r => r.Status != RequestStatus.New))
        {
            var history = historyByRequest[request.Id].ToList();

            history.Should().NotBeEmpty();
            history[0].PreviousStatus.Should().Be(RequestStatus.New);
            history[^1].NewStatus.Should().Be(request.Status);
            history.Select(h => h.ChangedAt).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
            history[0].ChangedAt.Should().BeAfter(request.CreatedAt);
            history[^1].ChangedAt.Should().BeBefore(RequestGenerator.ReferenceDate);
            request.UpdatedAt.Should().Be(history[^1].ChangedAt);
        }
    }

    [Fact]
    public void Generate_NewRequests_HaveNoHistoryAndNoHandler()
    {
        var requestIdsWithHistory = Data.History.Select(h => h.RequestId).ToHashSet();

        foreach (var request in Data.Requests.Where(r => r.Status == RequestStatus.New))
        {
            requestIdsWithHistory.Should().NotContain(request.Id);
            request.AssignedTo.Should().BeNull();
            request.UpdatedAt.Should().Be(request.CreatedAt);
        }
    }
}
