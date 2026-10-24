namespace Requests.DAL.Repositories;

public sealed record RequestStats(
    int Total,
    int New,
    int InProgress,
    int Waiting,
    int Completed,
    int Low,
    int Medium,
    int High)
{
    public static readonly RequestStats Empty = new(0, 0, 0, 0, 0, 0, 0, 0);
}
