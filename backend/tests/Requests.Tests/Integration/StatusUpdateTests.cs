using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Requests.BL.Dtos;
using Requests.DAL.Entities;

namespace Requests.Tests.Integration;

[Collection(IntegrationCollection.Name)]
public class StatusUpdateTests
{
    private readonly ApiFixture _fixture;

    public StatusUpdateTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Patch_Valid_UpdatesStatusVersionAndUpdatedAt_AndWritesOneHistoryRow()
    {
        var request = await _fixture.CreateRequestAsync(RequestStatus.InProgress);
        var oldVersion = Convert.ToBase64String(request.RowVersion);

        var response = await PatchAsync(request.Id, RequestStatus.Completed, oldVersion, userName: "  dana.levi  ");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await response.Content.ReadFromJsonAsync<RequestDto>(ApiFixture.Json))!;
        updated.Status.Should().Be(RequestStatus.Completed);
        updated.Version.Should().NotBe(oldVersion);
        updated.UpdatedAt.Should().BeAfter(request.UpdatedAt);

        var history = await HistoryRowsAsync(request.Id);
        history.Should().ContainSingle();
        history[0].PreviousStatus.Should().Be(RequestStatus.InProgress);
        history[0].NewStatus.Should().Be(RequestStatus.Completed);
        history[0].ChangedBy.Should().Be("dana.levi");
    }

    [Fact]
    public async Task Patch_WithoutUserHeader_ChangedByIsAnonymous()
    {
        var request = await _fixture.CreateRequestAsync(RequestStatus.New);

        var response = await PatchAsync(request.Id, RequestStatus.InProgress, Convert.ToBase64String(request.RowVersion));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await HistoryRowsAsync(request.Id)).Should().ContainSingle().Which.ChangedBy.Should().Be("anonymous");
    }

    [Fact]
    public async Task Patch_StaleVersion_Returns409WithCurrent_AndNoHistoryRow()
    {
        var request = await _fixture.CreateRequestAsync(RequestStatus.InProgress);
        var staleVersion = Convert.ToBase64String(request.RowVersion);
        (await PatchAsync(request.Id, RequestStatus.Waiting, staleVersion)).StatusCode.Should().Be(HttpStatusCode.OK);
        var current = await _fixture.QueryDbAsync(db => db.Requests.AsNoTracking().SingleAsync(r => r.Id == request.Id));

        var response = await PatchAsync(request.Id, RequestStatus.Completed, staleVersion);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("title").GetString().Should().Be("The request was modified by another user");
        var currentDto = problem.GetProperty("current").Deserialize<RequestDto>(ApiFixture.Json)!;
        currentDto.Status.Should().Be(RequestStatus.Waiting);
        currentDto.Version.Should().Be(Convert.ToBase64String(current.RowVersion));
        (await HistoryRowsAsync(request.Id)).Should().ContainSingle("only the first, successful update is recorded");
    }

    [Fact]
    public async Task Patch_ParallelWithSameVersion_ExactlyOneSucceeds()
    {
        var request = await _fixture.CreateRequestAsync(RequestStatus.InProgress);
        var version = Convert.ToBase64String(request.RowVersion);

        var responses = await Task.WhenAll(
            PatchAsync(request.Id, RequestStatus.Waiting, version),
            PatchAsync(request.Id, RequestStatus.Completed, version));

        responses.Select(r => r.StatusCode).Should()
            .BeEquivalentTo(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict });
        var winner = (await responses.Single(r => r.StatusCode == HttpStatusCode.OK)
            .Content.ReadFromJsonAsync<RequestDto>(ApiFixture.Json))!;

        var history = await HistoryRowsAsync(request.Id);
        history.Should().ContainSingle().Which.NewStatus.Should().Be(winner.Status);
        var final = await _fixture.QueryDbAsync(db => db.Requests.AsNoTracking().SingleAsync(r => r.Id == request.Id));
        final.Status.Should().Be(winner.Status);
    }

    [Fact]
    public async Task Patch_InvalidTransition_Returns400_AndStatusUnchanged()
    {
        var request = await _fixture.CreateRequestAsync(RequestStatus.Completed);

        var response = await PatchAsync(request.Id, RequestStatus.New, Convert.ToBase64String(request.RowVersion));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("title").GetString().Should().Be("Invalid status transition");
        problem.GetProperty("detail").GetString().Should().Contain("Completed").And.Contain("New");
        var stored = await _fixture.QueryDbAsync(db => db.Requests.AsNoTracking().SingleAsync(r => r.Id == request.Id));
        stored.Status.Should().Be(RequestStatus.Completed);
        (await HistoryRowsAsync(request.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Patch_InvalidBody_Returns400()
    {
        var request = await _fixture.CreateRequestAsync(RequestStatus.New);

        var malformedVersion = await PatchAsync(request.Id, RequestStatus.InProgress, "not-a-version");
        var unknownStatus = await _fixture.Client.PatchAsync($"/api/requests/{request.Id}/status",
            JsonContent.Create(new { newStatus = "Banana", version = Convert.ToBase64String(request.RowVersion) }));

        malformedVersion.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await malformedVersion.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("errors").TryGetProperty("version", out _).Should().BeTrue();
        unknownStatus.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var unknownStatusBody = await unknownStatus.Content.ReadAsStringAsync();
        JsonSerializer.Deserialize<JsonElement>(unknownStatusBody)
            .GetProperty("errors").TryGetProperty("newStatus", out _).Should().BeTrue();
        unknownStatusBody.Should().NotContain("Requests.BL", "internal type names must not leak");
    }

    [Fact]
    public async Task NonExistingId_Returns404_ForPatchAndHistory()
    {
        var patch = await PatchAsync(999_999, RequestStatus.InProgress, "AAAAAAAAB9E=");
        var history = await _fixture.Client.GetAsync("/api/requests/999999/history");

        patch.StatusCode.Should().Be(HttpStatusCode.NotFound);
        patch.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        history.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task History_IsNewestFirst_AndNewestMatchesLastUpdate()
    {
        var request = await _fixture.CreateRequestAsync(RequestStatus.New);
        var first = await PatchAsync(request.Id, RequestStatus.InProgress, Convert.ToBase64String(request.RowVersion), "user-1");
        var afterFirst = (await first.Content.ReadFromJsonAsync<RequestDto>(ApiFixture.Json))!;
        (await PatchAsync(request.Id, RequestStatus.Waiting, afterFirst.Version, "user-2")).EnsureSuccessStatusCode();

        var history = (await _fixture.Client.GetFromJsonAsync<List<RequestHistoryItemDto>>(
            $"/api/requests/{request.Id}/history", ApiFixture.Json))!;

        history.Should().HaveCount(2);
        history.Select(h => h.ChangedAt).Should().BeInDescendingOrder();
        history[0].Should().BeEquivalentTo(new { PreviousStatus = RequestStatus.InProgress, NewStatus = RequestStatus.Waiting, ChangedBy = "user-2" });
        history[1].Should().BeEquivalentTo(new { PreviousStatus = RequestStatus.New, NewStatus = RequestStatus.InProgress, ChangedBy = "user-1" });
    }

    [Fact]
    public async Task History_ForUnchangedRequest_IsEmpty()
    {
        var request = await _fixture.CreateRequestAsync(RequestStatus.New);

        var history = await _fixture.Client.GetFromJsonAsync<List<RequestHistoryItemDto>>(
            $"/api/requests/{request.Id}/history", ApiFixture.Json);

        history.Should().BeEmpty();
    }

    private Task<HttpResponseMessage> PatchAsync(int id, RequestStatus newStatus, string version, string? userName = null)
    {
        var message = new HttpRequestMessage(HttpMethod.Patch, $"/api/requests/{id}/status")
        {
            Content = JsonContent.Create(new UpdateStatusRequest(newStatus, version), options: ApiFixture.Json)
        };
        if (userName is not null)
        {
            message.Headers.Add("X-User-Name", userName);
        }

        return _fixture.Client.SendAsync(message);
    }

    private Task<List<RequestStatusHistory>> HistoryRowsAsync(int requestId) =>
        _fixture.QueryDbAsync(db => db.RequestStatusHistory.AsNoTracking().Where(h => h.RequestId == requestId).ToListAsync());
}
