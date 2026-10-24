using Requests.BL;
using Requests.DAL;
using Requests.DAL.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDal(builder.Configuration);
builder.Services.AddBl();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// "--seed [--count N]": fill the database with test data and exit without starting the web server.
if (args.Contains("--seed"))
{
    const int defaultSeedCount = 100_000;
    const int maxSeedCount = 1_000_000;

    var seedCount = defaultSeedCount;
    var countIndex = Array.IndexOf(args, "--count");
    if (countIndex >= 0)
    {
        if (countIndex + 1 >= args.Length
            || !int.TryParse(args[countIndex + 1], out seedCount)
            || seedCount < 1 || seedCount > maxSeedCount)
        {
            Console.Error.WriteLine($"--count must be a number between 1 and {maxSeedCount:N0}.");
            Environment.ExitCode = 1;
            return;
        }
    }

    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(seedCount, CancellationToken.None);
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
