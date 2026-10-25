using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Requests.BL.Dtos;
using Requests.BL.Services;

namespace Requests.Tests.Api;

/// <summary>No database needed: the query service is replaced by one that always throws.</summary>
public class GlobalExceptionHandlerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SecretMessage = "secret connection detail";

    private readonly WebApplicationFactory<Program> _factory;

    public GlobalExceptionHandlerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddScoped<IRequestQueryService, ThrowingQueryService>()));
    }

    [Fact]
    public async Task UnhandledException_Returns500ProblemDetails_WithoutExceptionDetails()
    {
        var response = await _factory.CreateClient().GetAsync("/api/requests/stats");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(SecretMessage).And.NotContain(nameof(ThrowingQueryService)).And.NotContain(" at ");

        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("status").GetInt32().Should().Be(500);
        problem.GetProperty("title").GetString().Should().Be("An unexpected error occurred.");
        problem.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
        problem.TryGetProperty("detail", out _).Should().BeFalse();
    }

    private sealed class ThrowingQueryService : IRequestQueryService
    {
        public Task<PagedResult<RequestDto>> SearchAsync(RequestQueryParameters parameters, CancellationToken ct) =>
            throw new InvalidOperationException(SecretMessage);

        public Task<RequestDto?> GetByIdAsync(int id, CancellationToken ct) => throw new InvalidOperationException(SecretMessage);

        public Task<RequestStatsDto> GetStatsAsync(CancellationToken ct) => throw new InvalidOperationException(SecretMessage);

        public Task<IReadOnlyList<RequestHistoryItemDto>?> GetHistoryAsync(int id, CancellationToken ct) =>
            throw new InvalidOperationException(SecretMessage);
    }
}
