namespace Requests.DAL.Entities;

public class Request
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public RequestPriority Priority { get; set; }
    public string? AssignedTo { get; set; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>SQL Server rowversion, used as the optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<RequestStatusHistory> History { get; set; } = new List<RequestStatusHistory>();
}
