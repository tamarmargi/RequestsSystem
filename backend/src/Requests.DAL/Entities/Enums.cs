namespace Requests.DAL.Entities;

public enum RequestStatus : byte
{
    New = 0,
    InProgress = 1,
    Waiting = 2,
    Completed = 3
}

public enum RequestPriority : byte
{
    Low = 0,
    Medium = 1,
    High = 2
}
