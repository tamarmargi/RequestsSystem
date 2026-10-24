using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Requests.DAL.Entities;

namespace Requests.DAL.Configurations;

public class RequestStatusHistoryConfiguration : IEntityTypeConfiguration<RequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<RequestStatusHistory> builder)
    {
        builder.ToTable("RequestStatusHistory");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.PreviousStatus).HasConversion<byte>();
        builder.Property(h => h.NewStatus).HasConversion<byte>();
        builder.Property(h => h.ChangedBy).IsRequired().HasMaxLength(100);

        builder.HasIndex(h => new { h.RequestId, h.ChangedAt });
    }
}
