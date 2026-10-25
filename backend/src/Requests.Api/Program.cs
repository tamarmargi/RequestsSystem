using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Requests.Api.ErrorHandling;
using Requests.Api.Http;
using Requests.BL;
using Requests.BL.Caching;
using Requests.DAL;
using Requests.DAL.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDal(builder.Configuration);
builder.Services.AddBl();
builder.Services.AddOptions<CacheOptions>()
    .Bind(builder.Configuration.GetSection(CacheOptions.SectionName))
    .Validate(options => options.StatsSeconds >= 0, "Cache:StatsSeconds must be 0 (disabled) or greater.")
    .ValidateOnStart();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Generic "The input was not valid." instead of serializer messages that name internal types.
        options.AllowInputFormatterExceptionMessages = false;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Binding errors (e.g. status=Banana in the query or the body) use the same camelCase field names as the
        // BL validation errors: "$.newStatus" -> "newStatus", "Status" -> "status".
        var defaultFactory = options.InvalidModelStateResponseFactory;
        options.InvalidModelStateResponseFactory = context =>
        {
            var result = (ObjectResult)defaultFactory(context);
            if (result.Value is ValidationProblemDetails problem)
            {
                // An unreadable body also adds "The body field is required." under the parameter name; it only
                // repeats the real error, so it is dropped when there is one.
                var bodyParameters = context.ActionDescriptor.Parameters
                    .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
                    .Select(p => p.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var errors = problem.Errors
                    .Where(e => !(bodyParameters.Contains(e.Key) && problem.Errors.Count > 1))
                    .ToDictionary(e => JsonNamingPolicy.CamelCase.ConvertName(e.Key.TrimStart('$', '.')), e => e.Value);
                problem.Errors.Clear();
                foreach (var (key, messages) in errors)
                {
                    problem.Errors[key] = messages;
                }
            }

            return result;
        };
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
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

// GlobalExceptionHandler decides how each exception is logged; the middleware's own Error log for every
// exception (including validation and cancellation) is turned off in appsettings.json.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
