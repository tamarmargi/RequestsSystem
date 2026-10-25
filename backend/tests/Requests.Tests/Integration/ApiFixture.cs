using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Requests.BL.Caching;
using Requests.DAL;
using Requests.DAL.Entities;
using Requests.DAL.Seeding;
using Testcontainers.MsSql;

namespace Requests.Tests.Integration;

/// <summary>
/// Starts SQL Server in Docker once for all integration tests, applies the migrations and seeds a small
/// deterministic dataset, then hosts the API in memory against it.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public const int SeededRequestCount = 500;

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public RequestsApiFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();

        var connectionString = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "RequestsTests"
        }.ConnectionString;
        Factory = new RequestsApiFactory(connectionString);

        using (var scope = Factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(SeededRequestCount, CancellationToken.None);
        }

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await _sqlServer.DisposeAsync();
    }

    /// <summary>
    /// Inserts a fresh request, so a test that changes data works on its own row and does not affect other tests.
    /// </summary>
    /// <remarks>New requests get no handler, others get "test-handler" (as in the seeded data).</remarks>
    public Task<Request> CreateRequestAsync(RequestStatus status) =>
        CreateRequestAsync(status, status == RequestStatus.New ? null : "test-handler");

    public async Task<Request> CreateRequestAsync(RequestStatus status, string? assignedTo)
    {
        var request = await InsertRequestAsync(status, assignedTo);

        // A direct database write bypasses the API, so the cached stats are dropped here, as the API does after its writes.
        Factory.Services.GetRequiredService<IStatsCache>().Invalidate();
        return request;
    }

    private Task<Request> InsertRequestAsync(RequestStatus status, string? assignedTo) => QueryDbAsync(async db =>
    {
        var created = DateTime.UtcNow.AddDays(-1);
        var request = new Request
        {
            Title = "Integration test request",
            OrganizationName = "Integration Tests Ltd",
            Status = status,
            Priority = RequestPriority.Medium,
            AssignedTo = assignedTo,
            CreatedAt = created,
            UpdatedAt = created
        };
        db.Requests.Add(request);
        await db.SaveChangesAsync();
        return request;
    });

    /// <summary>Runs <paramref name="query"/> directly against the test database.</summary>
    public async Task<T> QueryDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        using var scope = Factory.Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}

public sealed class RequestsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public RequestsApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
    }
}

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Integration";
}
