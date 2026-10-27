using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Requests.DAL;
using Requests.DAL.Seeding;

namespace Requests.Api.Startup;

/// <summary>
/// Optional database preparation when the API starts, for the one-command Docker run. Both options are OFF by
/// default (appsettings.json); normal environments apply migrations explicitly (dotnet ef database update).
/// </summary>
public static class DatabaseStartup
{
    /// <summary>Apply pending EF Core migrations.</summary>
    public const string MigrateOnStartupKey = "Database:MigrateOnStartup";

    /// <summary>Run the seeder (100,000 requests) only when the Requests table is empty, so restarts keep the data.</summary>
    public const string SeedOnStartupIfEmptyKey = "Seed:OnStartupIfEmpty";

    public static async Task PrepareDatabaseAsync(this WebApplication app, CancellationToken ct = default)
    {
        var migrate = app.Configuration.GetValue<bool>(MigrateOnStartupKey);
        var seed = app.Configuration.GetValue<bool>(SeedOnStartupIfEmptyKey);
        if (!migrate && !seed)
        {
            return;
        }

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseStartup));
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (migrate)
        {
            logger.LogInformation("{Option} is on: applying migrations", MigrateOnStartupKey);
            var stopwatch = Stopwatch.StartNew();
            await db.Database.MigrateAsync(ct);
            logger.LogInformation("Migrations applied in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
        }

        if (seed)
        {
            if (await db.Requests.AnyAsync(ct))
            {
                logger.LogInformation("{Option} is on: Requests table is not empty, seeding skipped", SeedOnStartupIfEmptyKey);
                return;
            }

            logger.LogInformation(
                "{Option} is on: Requests table is empty, seeding {Count} requests", SeedOnStartupIfEmptyKey, DatabaseSeeder.DefaultCount);
            var stopwatch = Stopwatch.StartNew();
            await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(DatabaseSeeder.DefaultCount, ct);
            logger.LogInformation("Seeding on startup finished in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
        }
    }
}
