using Core.Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests.Restaurants;

[TestFixture]
public class RestaurantRepositoryTests
{
    private AppDbContext        _db   = null!;
    private RestaurantRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db   = new AppDbContext(options);
        _repo = new RestaurantRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetAllActiveAsync_ReturnsOnlyActive()
    {
        _db.Restaurants.AddRange(MakeRestaurant("Active"), MakeRestaurant("Inactive", isActive: false));
        await _db.SaveChangesAsync();

        var result = await _repo.GetAllActiveAsync();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Active"));
    }

    [Test]
    public async Task GetAllAsync_ReturnsAll()
    {
        _db.Restaurants.AddRange(MakeRestaurant("A"), MakeRestaurant("I", isActive: false));
        await _db.SaveChangesAsync();

        Assert.That((await _repo.GetAllAsync()), Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsRestaurant()
    {
        var r = MakeRestaurant("R");
        _db.Restaurants.Add(r);
        await _db.SaveChangesAsync();

        Assert.That((await _repo.GetByIdAsync(r.Id))!.Id, Is.EqualTo(r.Id));
    }

    [Test]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        Assert.That(await _repo.GetByIdAsync(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public async Task AddAsync_PersistsRestaurant()
    {
        await _repo.AddAsync(MakeRestaurant("New"));

        Assert.That(await _db.Restaurants.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task UpdateAsync_SavesChanges()
    {
        var r = MakeRestaurant("Original");
        _db.Restaurants.Add(r);
        await _db.SaveChangesAsync();

        r.Name = "Updated";
        await _repo.UpdateAsync(r);

        Assert.That((await _db.Restaurants.FindAsync(r.Id))!.Name, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task SoftDeleteAsync_SetsIsActiveToFalse()
    {
        var r = MakeRestaurant("R");
        _db.Restaurants.Add(r);
        await _db.SaveChangesAsync();

        Assert.That(await _repo.SoftDeleteAsync(r.Id), Is.True);
        Assert.That((await _db.Restaurants.FindAsync(r.Id))!.IsActive, Is.False);
    }

    [Test]
    public async Task SoftDeleteAsync_NonExistingId_ReturnsFalse()
    {
        Assert.That(await _repo.SoftDeleteAsync(Guid.NewGuid()), Is.False);
    }

    private static Restaurant MakeRestaurant(string name, bool isActive = true) =>
        new()
        {
            Name      = name,
            Address   = "Test Address",
            TimeZone  = "UTC",
            OpenTime  = TimeSpan.FromHours(9),
            CloseTime = TimeSpan.FromHours(22),
            IsActive  = isActive,
        };
}
