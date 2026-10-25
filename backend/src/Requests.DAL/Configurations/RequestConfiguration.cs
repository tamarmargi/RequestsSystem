using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requests.DAL.Entities;

namespace Requests.DAL.Configurations;

public class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.ToTable("Requests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).UseIdentityColumn();

        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);
        builder.Property(r => r.OrganizationName).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Status).HasConversion<byte>();
        builder.Property(r => r.Priority).HasConversion<byte>();
        builder.Property(r => r.AssignedTo).HasMaxLength(100);
        builder.Property(r => r.CreatedAt);
        builder.Property(r => r.UpdatedAt);
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasMany(r => r.History)
            .WithOne(h => h.Request)
            .HasForeignKey(h => h.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes chosen by measured queries (docs/performance/README.md). Key order and directions match the
        // ORDER BY of the list queries (CreatedAt DESC, Id DESC as the stable tie-breaker), so the first page is
        // read in order from the index with no Sort operator.

        // Default list sort without filters (and the narrowest choice for nothing else).
        builder.HasIndex(r => new { r.CreatedAt, r.Id })
            .IsDescending(true, true)
            .HasDatabaseName("IX_Requests_CreatedAt");

        // Status filter with the default sort.
        builder.HasIndex(r => new { r.Status, r.CreatedAt, r.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("IX_Requests_Status_CreatedAt");

        // Handler filter with the default sort.
        builder.HasIndex(r => new { r.AssignedTo, r.CreatedAt, r.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("IX_Requests_AssignedTo");

        // Organization filter with the default sort.
        builder.HasIndex(r => new { r.OrganizationName, r.CreatedAt, r.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("IX_Requests_OrganizationName");

        // Narrow index for the stats aggregate (and Status + Priority filters).
        builder.HasIndex(r => new { r.Status, r.Priority })
            .HasDatabaseName("IX_Requests_Status_Priority");
    }
}
