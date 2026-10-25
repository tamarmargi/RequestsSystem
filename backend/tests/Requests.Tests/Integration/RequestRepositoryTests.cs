using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Requests.DAL;
using Requests.DAL.Entities;
using Requests.DAL.Repositories;

namespace Requests.Tests.Integration;

[Collection(IntegrationCollection.Name)]
public class RequestRepositoryTests
{
    private readonly ApiFixture _fixture;

    public RequestRepositoryTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetManyForUpdate_ReturnsTrackedExistingRequestsOnly()
    {
        var a = await _fixture.CreateRequestAsync(RequestStatus.New);
        var b = await _fixture.CreateRequestAsync(RequestStatus.New);
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IRequestRepository>();

        var loaded = await repository.GetManyForUpdateAsync(new[] { a.Id, b.Id, 999_999 }, CancellationToken.None);

        loaded.Keys.Should().BeEquivalentTo(new[] { a.Id, b.Id });
        loaded.Values.Should().OnlyContain(r => db.Entry(r).State == EntityState.Unchanged);
    }

    [Fact]
    public async Task FailedSave_LeavesContextClean_SoTheNextSaveInTheSameContextOnlyWritesItsOwnChange()
    {
        var failing = await _fixture.CreateRequestAsync(RequestStatus.InProgress);
        var succeeding = await _fixture.CreateRequestAsync(RequestStatus.InProgress);
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IRequestRepository>();
        var loaded = await repository.GetManyForUpdateAsync(new[] { failing.Id, succeeding.Id }, CancellationToken.None);

        // A version the row does not have: the database UPDATE matches no row (a real DbUpdateConcurrencyException).
        var wrongVersion = new byte[8];
        var failed = await repository.SaveStatusChangeAsync(
            loaded[failing.Id], wrongVersion, RequestStatus.Waiting, "test", CancellationToken.None);

        failed.Should().BeFalse();
        db.ChangeTracker.Entries().Should().OnlyContain(e => e.State == EntityState.Unchanged,
            "the failed request and its pending history row must not be saved by a later SaveChanges");

        var saved = await repository.SaveStatusChangeAsync(
            loaded[succeeding.Id], succeeding.RowVersion, RequestStatus.Completed, "test", CancellationToken.None);

        saved.Should().BeTrue();
        var stored = await _fixture.QueryDbAsync(async check => new
        {
            Failing = await check.Requests.AsNoTracking().SingleAsync(r => r.Id == failing.Id),
            FailingHistory = await check.RequestStatusHistory.CountAsync(h => h.RequestId == failing.Id),
            Succeeding = await check.Requests.AsNoTracking().SingleAsync(r => r.Id == succeeding.Id),
            SucceedingHistory = await check.RequestStatusHistory.CountAsync(h => h.RequestId == succeeding.Id)
        });
        stored.Failing.Status.Should().Be(RequestStatus.InProgress);
        stored.Failing.RowVersion.Should().Equal(failing.RowVersion);
        stored.FailingHistory.Should().Be(0);
        stored.Succeeding.Status.Should().Be(RequestStatus.Completed);
        stored.SucceedingHistory.Should().Be(1);
    }
}
