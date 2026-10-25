using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Requests.BL.Dtos;
using Requests.DAL.Entities;

namespace Requests.Tests.Integration;

/// <summary>New -> InProgress assigns the user as handler, through both the single and the bulk update.</summary>
[Collection(IntegrationCollection.Name)]
public class AutoAssignmentTests
{
    private readonly ApiFixture _fixture;

    public AutoAssignmentTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Patch_NewToInProgress_WithUser_AssignsTheUser()
    {
        var request = await _fixture.CreateRequestAsync(RequestStatus.New);

        var response = await PatchAsync(request, RequestStatus.InProgress, Uri.EscapeDataString("דנה לוי"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<RequestDto>(ApiFixture.Json))!.AssignedTo.Should().Be("דנה לוי");
        (await LoadAsync(request.Id)).AssignedTo.Should().Be("דנה לוי");
    }

    [Fact]
    public async Task Patch_NewToInProgress_Anonymous_StaysUnassigned()
    {
        var request = await _fixture.CreateRequestAsync(RequestStatus.New);

        var response = await PatchAsync(request, RequestStatus.InProgress, userName: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LoadAsync(request.Id)).AssignedTo.Should().BeNull();
    }

    [Fact]
    public async Task Patch_NewToInProgress_ExistingHandler_IsKept()
    {
        var request = await _fixture.CreateRequestAsync(RequestStatus.New, assignedTo: "יוסי כהן");

        var response = await PatchAsync(request, RequestStatus.InProgress, "dana.levi");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LoadAsync(request.Id)).AssignedTo.Should().Be("יוסי כהן");
    }

    [Fact]
    public async Task Patch_Conflict_DoesNotAssign()
    {
        var request = await _fixture.CreateRequestAsync(RequestStatus.New);

        var response = await PatchAsync(request, RequestStatus.InProgress, "dana.levi", version: "AAAAAAAAAAE=");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await LoadAsync(request.Id)).AssignedTo.Should().BeNull();
    }

    [Fact]
    public async Task Bulk_AppliesTheSameRule()
    {
        var unassigned = await _fixture.CreateRequestAsync(RequestStatus.New);
        var assigned = await _fixture.CreateRequestAsync(RequestStatus.New, assignedTo: "יוסי כהן");

        var message = new HttpRequestMessage(HttpMethod.Post, "/api/requests/bulk-status")
        {
            Content = JsonContent.Create(new BulkUpdateStatusRequest(RequestStatus.InProgress, new[]
            {
                new BulkItem(unassigned.Id, Convert.ToBase64String(unassigned.RowVersion)),
                new BulkItem(assigned.Id, Convert.ToBase64String(assigned.RowVersion))
            }), options: ApiFixture.Json)
        };
        message.Headers.Add("X-User-Name", Uri.EscapeDataString("דנה לוי"));

        var response = await _fixture.Client.SendAsync(message);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<BulkUpdateResultDto>(ApiFixture.Json))!.Succeeded.Should().Be(2);
        (await LoadAsync(unassigned.Id)).AssignedTo.Should().Be("דנה לוי");
        (await LoadAsync(assigned.Id)).AssignedTo.Should().Be("יוסי כהן");
    }

    private Task<HttpResponseMessage> PatchAsync(Request request, RequestStatus newStatus, string? userName, string? version = null)
    {
        var message = new HttpRequestMessage(HttpMethod.Patch, $"/api/requests/{request.Id}/status")
        {
            Content = JsonContent.Create(
                new UpdateStatusRequest(newStatus, version ?? Convert.ToBase64String(request.RowVersion)), options: ApiFixture.Json)
        };
        if (userName is not null)
        {
            message.Headers.Add("X-User-Name", userName);
        }

        return _fixture.Client.SendAsync(message);
    }

    private Task<Request> LoadAsync(int id) =>
        _fixture.QueryDbAsync(db => db.Requests.AsNoTracking().SingleAsync(r => r.Id == id));
}
