using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Requests.BL.Dtos;
using Requests.DAL.Entities;

namespace Requests.Tests.Integration;

[Collection(IntegrationCollection.Name)]
public class BulkUpdateTests
{
    private const string Url = "/api/requests/bulk-status";

    private readonly ApiFixture _fixture;

    public BulkUpdateTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Mixed_ReturnsOutcomePerItemInInputOrder_AndOnlyTheValidItemChanges()
    {
        var valid = await _fixture.CreateRequestAsync(RequestStatus.InProgress);
        var stale = await _fixture.CreateRequestAsync(RequestStatus.InProgress);
        var staleVersion = Version(stale);
        await ChangeStatusAsync(stale, RequestStatus.Waiting); // now the version above is out of date
        var staleBefore = await LoadAsync(stale.Id);
        var invalid = await _fixture.CreateRequestAsync(RequestStatus.New); // New -> Completed is not allowed

        var response = await PostAsync(RequestStatus.Completed, "bulk-user",
            new BulkItem(valid.Id, Version(valid)),
            new BulkItem(999_999, "AAAAAAAAB9E="),
            new BulkItem(stale.Id, staleVersion),
            new BulkItem(invalid.Id, Version(invalid)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<BulkUpdateResultDto>(ApiFixture.Json))!;
        result.Succeeded.Should().Be(1);
        result.Failed.Should().Be(3);
        result.Results.Select(r => (r.Id, r.Outcome)).Should().Equal(
            (valid.Id, BulkOutcome.Updated),
            (999_999, BulkOutcome.NotFound),
            (stale.Id, BulkOutcome.Conflict),
            (invalid.Id, BulkOutcome.InvalidTransition));
        result.Results[3].Message.Should().Contain("New").And.Contain("Completed").And.Contain("InProgress");
        result.Results.Skip(1).Should().OnlyContain(r => r.Message != null && r.Version == null);

        var validAfter = await LoadAsync(valid.Id);
        validAfter.Status.Should().Be(RequestStatus.Completed);
        Convert.ToBase64String(validAfter.RowVersion).Should().Be(result.Results[0].Version).And.NotBe(Version(valid));
        (await HistoryAsync(valid.Id)).Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { PreviousStatus = RequestStatus.InProgress, NewStatus = RequestStatus.Completed, ChangedBy = "bulk-user" });

        (await LoadAsync(stale.Id)).RowVersion.Should().Equal(staleBefore.RowVersion);
        (await HistoryAsync(stale.Id)).Should().ContainSingle("only the earlier single update is recorded");
        (await LoadAsync(invalid.Id)).RowVersion.Should().Equal(invalid.RowVersion);
        (await HistoryAsync(invalid.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task FailureInTheMiddle_DoesNotPreventLaterValidItems()
    {
        var first = await _fixture.CreateRequestAsync(RequestStatus.InProgress);
        var stale = await _fixture.CreateRequestAsync(RequestStatus.InProgress);
        var staleVersion = Version(stale);
        await ChangeStatusAsync(stale, RequestStatus.Waiting);
        var last = await _fixture.CreateRequestAsync(RequestStatus.Waiting);

        var result = await PostOkAsync(RequestStatus.Completed,
            new BulkItem(first.Id, Version(first)),
            new BulkItem(stale.Id, staleVersion),
            new BulkItem(last.Id, Version(last)));

        result.Results.Select(r => r.Outcome).Should().Equal(BulkOutcome.Updated, BulkOutcome.Conflict, BulkOutcome.Updated);
        (await LoadAsync(last.Id)).Status.Should().Be(RequestStatus.Completed);
        (await HistoryAsync(last.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task AllValid_AllUpdated()
    {
        var requests = new[]
        {
            await _fixture.CreateRequestAsync(RequestStatus.InProgress),
            await _fixture.CreateRequestAsync(RequestStatus.Waiting),
            await _fixture.CreateRequestAsync(RequestStatus.InProgress)
        };

        var result = await PostOkAsync(RequestStatus.Completed, requests.Select(r => new BulkItem(r.Id, Version(r))).ToArray());

        result.Succeeded.Should().Be(3);
        result.Failed.Should().Be(0);
        result.Results.Should().OnlyContain(r => r.Outcome == BulkOutcome.Updated && r.Message == null && r.Version != null);
        foreach (var request in requests)
        {
            (await LoadAsync(request.Id)).Status.Should().Be(RequestStatus.Completed);
        }
    }

    public static TheoryData<string> InvalidRequests() => new()
    {
        "tooMany",
        "duplicates",
        "empty"
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task InvalidRequest_Returns400_AndChangesNothing(string kind)
    {
        var request = await _fixture.CreateRequestAsync(RequestStatus.InProgress);
        var valid = new BulkItem(request.Id, Version(request));
        var items = kind switch
        {
            // The valid item is included, so a partial application would be visible.
            "tooMany" => new[] { valid }.Concat(Enumerable.Range(1, 100).Select(i => new BulkItem(-i, "AAAAAAAAB9E="))).ToArray(),
            "duplicates" => new[] { valid, valid },
            _ => Array.Empty<BulkItem>()
        };
        var historyCountBefore = await _fixture.QueryDbAsync(db => db.RequestStatusHistory.CountAsync());

        var response = await PostAsync(RequestStatus.Completed, null, items);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").TryGetProperty("items", out _).Should().BeTrue();
        (await LoadAsync(request.Id)).RowVersion.Should().Equal(request.RowVersion);
        (await _fixture.QueryDbAsync(db => db.RequestStatusHistory.CountAsync())).Should().Be(historyCountBefore);
    }

    [Fact]
    public async Task Stats_AreInvalidated_ByASingleStatusChange()
    {
        var request = await _fixture.CreateRequestAsync(RequestStatus.InProgress);
        var before = await GetStatsAsync();
        (await GetStatsAsync()).Should().BeEquivalentTo(before, "the second read is served from the cache");

        await ChangeStatusAsync(request, RequestStatus.Waiting);
        var after = await GetStatsAsync();

        // Read immediately: with a 60 s expiration only the invalidation explains the new numbers.
        after.ByStatus.InProgress.Should().Be(before.ByStatus.InProgress - 1);
        after.ByStatus.Waiting.Should().Be(before.ByStatus.Waiting + 1);
        after.Total.Should().Be(before.Total);
    }

    [Fact]
    public async Task Stats_AreInvalidated_ByABulkUpdate()
    {
        var first = await _fixture.CreateRequestAsync(RequestStatus.InProgress);
        var second = await _fixture.CreateRequestAsync(RequestStatus.Waiting);
        var before = await GetStatsAsync();

        await PostOkAsync(RequestStatus.Completed, new BulkItem(first.Id, Version(first)), new BulkItem(second.Id, Version(second)));
        var after = await GetStatsAsync();

        after.ByStatus.InProgress.Should().Be(before.ByStatus.InProgress - 1);
        after.ByStatus.Waiting.Should().Be(before.ByStatus.Waiting - 1);
        after.ByStatus.Completed.Should().Be(before.ByStatus.Completed + 2);
    }

    private static string Version(Request request) => Convert.ToBase64String(request.RowVersion);

    private async Task ChangeStatusAsync(Request request, RequestStatus newStatus)
    {
        var response = await _fixture.Client.PatchAsync($"/api/requests/{request.Id}/status",
            JsonContent.Create(new UpdateStatusRequest(newStatus, Version(request)), options: ApiFixture.Json));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private Task<HttpResponseMessage> PostAsync(RequestStatus newStatus, string? userName, params BulkItem[] items)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = JsonContent.Create(new BulkUpdateStatusRequest(newStatus, items), options: ApiFixture.Json)
        };
        if (userName is not null)
        {
            message.Headers.Add("X-User-Name", userName);
        }

        return _fixture.Client.SendAsync(message);
    }

    private async Task<BulkUpdateResultDto> PostOkAsync(RequestStatus newStatus, params BulkItem[] items)
    {
        var response = await PostAsync(newStatus, null, items);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<BulkUpdateResultDto>(ApiFixture.Json))!;
    }

    private async Task<RequestStatsDto> GetStatsAsync() =>
        (await _fixture.Client.GetFromJsonAsync<RequestStatsDto>("/api/requests/stats", ApiFixture.Json))!;

    private Task<Request> LoadAsync(int id) =>
        _fixture.QueryDbAsync(db => db.Requests.AsNoTracking().SingleAsync(r => r.Id == id));

    private Task<List<RequestStatusHistory>> HistoryAsync(int requestId) =>
        _fixture.QueryDbAsync(db => db.RequestStatusHistory.AsNoTracking().Where(h => h.RequestId == requestId).ToListAsync());
}
