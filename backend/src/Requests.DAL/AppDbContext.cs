using Microsoft.EntityFrameworkCore;
using Requests.DAL.Entities;

namespace Requests.DAL;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Request> Requests => Set<Request>();
    public DbSet<RequestStatusHistory> RequestStatusHistory => Set<RequestStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    // The parameterless overloads call these two, so every save goes through SetUpdatedAt.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetUpdatedAt();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SetUpdatedAt();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Sets UpdatedAt centrally, so no code path that modifies a request can forget it.</summary>
    private void SetUpdatedAt()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Request>().Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.UpdatedAt = now;
        }
    }
}
