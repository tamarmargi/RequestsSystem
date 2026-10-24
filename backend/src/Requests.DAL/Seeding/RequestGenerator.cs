using Requests.DAL.Entities;

namespace Requests.DAL.Seeding;

public sealed record GeneratedRequest(
    int Id,
    string Title,
    string OrganizationName,
    RequestStatus Status,
    RequestPriority Priority,
    string? AssignedTo,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record GeneratedStatusChange(
    long Id,
    int RequestId,
    RequestStatus PreviousStatus,
    RequestStatus NewStatus,
    DateTime ChangedAt,
    string ChangedBy);

public sealed record GeneratedData(
    IReadOnlyList<GeneratedRequest> Requests,
    IReadOnlyList<GeneratedStatusChange> History);

/// <summary>
/// Generates test requests and their status history in memory. Deterministic: the same seed and count
/// always produce the same data, independent of the current time or machine.
/// </summary>
public sealed class RequestGenerator
{
    public const int DefaultSeed = 42;
    public const string SystemUser = "system";

    /// <summary>All generated dates lie before this fixed point in time.</summary>
    public static readonly DateTime ReferenceDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    // About 20% of organizations ("big" ones) receive about 60% of requests.
    private const double BigOrganizationShare = 0.2;
    private const double BigOrganizationRequestShare = 0.6;

    private const int MinAgeSeconds = 24 * 60 * 60;
    private const int MaxAgeSeconds = 2 * 365 * 24 * 60 * 60;
    private const int MinStepSeconds = 10 * 60;
    private const int MaxStepSeconds = 14 * 24 * 60 * 60;

    private static readonly RequestStatus[] InProgressPath = { RequestStatus.New, RequestStatus.InProgress };
    private static readonly RequestStatus[] WaitingPath = { RequestStatus.New, RequestStatus.InProgress, RequestStatus.Waiting };
    private static readonly RequestStatus[] CompletedPath = { RequestStatus.New, RequestStatus.InProgress, RequestStatus.Completed };
    private static readonly RequestStatus[] CompletedViaWaitingPath =
    {
        RequestStatus.New, RequestStatus.InProgress, RequestStatus.Waiting, RequestStatus.InProgress, RequestStatus.Completed
    };

    private readonly Random _random;
    private readonly string[] _organizations;
    private readonly int _bigOrganizationCount;

    public RequestGenerator(int seed = DefaultSeed)
    {
        _random = new Random(seed);

        // Shuffle so the "big" organizations are a mix of all organization types.
        _organizations = SeedData.Organizations.ToArray();
        _random.Shuffle(_organizations);
        _bigOrganizationCount = (int)(_organizations.Length * BigOrganizationShare);
    }

    public GeneratedData Generate(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var requests = new List<GeneratedRequest>(count);
        var history = new List<GeneratedStatusChange>(count * 3);

        for (var id = 1; id <= count; id++)
        {
            var status = NextStatus();
            var priority = NextPriority();
            var organization = NextOrganization();
            var title = NextTitle();
            var assignedTo = status == RequestStatus.New ? null : Pick(SeedData.Handlers);
            var createdAt = ReferenceDate.AddSeconds(-_random.Next(MinAgeSeconds, MaxAgeSeconds + 1));
            var updatedAt = createdAt;

            var path = PathTo(status);
            for (var step = 1; step < path.Length; step++)
            {
                var stepsLeft = path.Length - step;
                var remainingSeconds = (ReferenceDate - updatedAt).TotalSeconds;
                var maxGap = (int)Math.Min(MaxStepSeconds, remainingSeconds / (stepsLeft + 1));
                var gap = _random.Next(Math.Min(MinStepSeconds, maxGap), maxGap + 1);
                updatedAt = updatedAt.AddSeconds(gap);

                history.Add(new GeneratedStatusChange(
                    Id: history.Count + 1,
                    RequestId: id,
                    PreviousStatus: path[step - 1],
                    NewStatus: path[step],
                    ChangedAt: updatedAt,
                    ChangedBy: step == 1 ? SystemUser : assignedTo!));
            }

            requests.Add(new GeneratedRequest(id, title, organization, status, priority, assignedTo, createdAt, updatedAt));
        }

        return new GeneratedData(requests, history);
    }

    private RequestStatus NextStatus()
    {
        var roll = _random.NextDouble();
        return roll switch
        {
            < 0.50 => RequestStatus.Completed,
            < 0.75 => RequestStatus.InProgress,
            < 0.90 => RequestStatus.New,
            _ => RequestStatus.Waiting
        };
    }

    private RequestPriority NextPriority()
    {
        var roll = _random.NextDouble();
        return roll switch
        {
            < 0.50 => RequestPriority.Low,
            < 0.85 => RequestPriority.Medium,
            _ => RequestPriority.High
        };
    }

    private string NextOrganization()
    {
        var index = _random.NextDouble() < BigOrganizationRequestShare
            ? _random.Next(_bigOrganizationCount)
            : _random.Next(_bigOrganizationCount, _organizations.Length);
        return _organizations[index];
    }

    private string NextTitle()
    {
        var template = Pick(SeedData.TitleTemplates);
        return _random.Next(2) == 0 ? template : $"{template} - {Pick(SeedData.TitleSuffixes)}";
    }

    private RequestStatus[] PathTo(RequestStatus status) => status switch
    {
        RequestStatus.New => new[] { RequestStatus.New },
        RequestStatus.InProgress => InProgressPath,
        RequestStatus.Waiting => WaitingPath,
        RequestStatus.Completed => _random.Next(3) == 0 ? CompletedViaWaitingPath : CompletedPath,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private string Pick(IReadOnlyList<string> items) => items[_random.Next(items.Count)];
}
