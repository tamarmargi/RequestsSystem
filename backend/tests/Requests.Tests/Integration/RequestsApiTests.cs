using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Requests.BL.Dtos;
using Requests.DAL.Entities;

namespace Requests.Tests.Integration;

[Collection(IntegrationCollection.Name)]
public class RequestsApiTests
{
    private readonly ApiFixture _fixture;

    public RequestsApiTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task List_FilterByStatus_ReturnsOnlyThatStatus_AndMatchingTotalCount()
    {
        var result = await GetListAsync("/api/requests?status=Waiting&pageSize=100");

        var expectedCount = await _fixture.QueryDbAsync(db => db.Requests.CountAsync(r => r.Status == RequestStatus.Waiting));
        result.TotalCount.Should().Be(expectedCount).And.BeGreaterThan(0);
        result.Items.Should().NotBeEmpty().And.OnlyContain(r => r.Status == RequestStatus.Waiting);
    }

    [Fact]
    public async Task List_Paging_ReturnsDifferentItemsPerPage()
    {
        var page1 = await GetListAsync("/api/requests?page=1&pageSize=10");
        var page2 = await GetListAsync("/api/requests?page=2&pageSize=10");

        page1.Items.Should().HaveCount(10);
        page2.Items.Should().HaveCount(10);
        page1.Items.Select(r => r.Id).Should().NotIntersectWith(page2.Items.Select(r => r.Id));
        page2.Page.Should().Be(2);
        page2.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task List_SortByPriorityDesc_IsOrderedAndStable()
    {
        const string url = "/api/requests?sortBy=priority&sortDirection=desc&pageSize=100";

        var first = await GetListAsync(url);
        var second = await GetListAsync(url);

        first.Items.Select(r => r.Priority).Should().BeInDescendingOrder();
        second.Items.Select(r => r.Id).Should().Equal(first.Items.Select(r => r.Id));
    }

    [Fact]
    public async Task List_Search_MatchesTitleOrOrganizationName()
    {
        const string term = "עיריית";

        var result = await GetListAsync($"/api/requests?search={Uri.EscapeDataString(term)}&pageSize=100");

        result.Items.Should().NotBeEmpty()
            .And.OnlyContain(r => r.Title.Contains(term) || r.OrganizationName.Contains(term));
    }

    [Fact]
    public async Task List_PageSizeTooLarge_Returns400WithPageSizeError()
    {
        var response = await _fixture.Client.GetAsync("/api/requests?pageSize=500");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(400);
        problem.GetProperty("errors").TryGetProperty("pageSize", out _).Should().BeTrue();
    }

    [Fact]
    public async Task List_UnknownStatus_Returns400WithStatusError()
    {
        var response = await _fixture.Client.GetAsync("/api/requests?status=Banana");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").TryGetProperty("status", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetById_Missing_Returns404()
    {
        var response = await _fixture.Client.GetAsync("/api/requests/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsRequestWithVersion()
    {
        var request = await _fixture.Client.GetFromJsonAsync<RequestDto>("/api/requests/1", ApiFixture.Json);

        request!.Id.Should().Be(1);
        Convert.FromBase64String(request.Version).Should().HaveCount(8);
    }

    [Fact]
    public async Task Stats_TotalEqualsRequestCount_AndStatusCountsSumToTotal()
    {
        var stats = await _fixture.Client.GetFromJsonAsync<RequestStatsDto>("/api/requests/stats", ApiFixture.Json);

        // Other integration tests add their own requests, so compare with the database instead of the seeded count.
        var requestCount = await _fixture.QueryDbAsync(db => db.Requests.CountAsync());
        stats!.Total.Should().Be(requestCount).And.BeGreaterThanOrEqualTo(ApiFixture.SeededRequestCount);
        (stats.ByStatus.New + stats.ByStatus.InProgress + stats.ByStatus.Waiting + stats.ByStatus.Completed)
            .Should().Be(stats.Total);
        (stats.ByPriority.Low + stats.ByPriority.Medium + stats.ByPriority.High).Should().Be(stats.Total);
    }

    private async Task<PagedResult<RequestDto>> GetListAsync(string url)
    {
        var response = await _fixture.Client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<PagedResult<RequestDto>>(ApiFixture.Json))!;
    }
}
