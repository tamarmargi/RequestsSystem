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

        // No additional indexes yet: they will be added after performance measurement.
    }
}
