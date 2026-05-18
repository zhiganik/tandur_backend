using Core.Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests.MenuItems;

[TestFixture]
public class MenuItemRepositoryTests
{
    private AppDbContext       _db   = null!;
    private MenuItemRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db   = new AppDbContext(options);
        _repo = new MenuItemRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetPagedAvailableAsync_ReturnsOnlyActiveAndAvailableOrderedBySortOrder()
    {
        var restaurantId = Guid.NewGuid();
        var categoryId   = Guid.NewGuid();
        _db.MenuItems.AddRange(
            MakeMenuItem("Available",   restaurantId, categoryId, isActive: true,  isAvailable: true,  sortOrder: 2),
            MakeMenuItem("Unavailable", restaurantId, categoryId, isActive: true,  isAvailable: false, sortOrder: 1),
            MakeMenuItem("Deleted",     restaurantId, categoryId, isActive: false, isAvailable: true,  sortOrder: 0),
            MakeMenuItem("First",       restaurantId, categoryId, isActive: true,  isAvailable: true,  sortOrder: 1));
        await _db.SaveChangesAsync();

        var result = await _repo.GetPagedAvailableAsync(restaurantId, 1, 20);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Name, Is.EqualTo("First"));
    }

    [Test]
    public async Task GetPagedAvailableAsync_RespectsPageAndLimit()
    {
        var restaurantId = Guid.NewGuid();
        var categoryId   = Guid.NewGuid();
        for (var i = 1; i <= 5; i++)
            _db.MenuItems.Add(MakeMenuItem($"M{i}", restaurantId, categoryId, sortOrder: i));
        await _db.SaveChangesAsync();

        var result = await _repo.GetPagedAvailableAsync(restaurantId, page: 2, limit: 2);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task CountAvailableAsync_CountsOnlyActiveAndAvailable()
    {
        var restaurantId = Guid.NewGuid();
        var categoryId   = Guid.NewGuid();
        _db.MenuItems.AddRange(
            MakeMenuItem("A1", restaurantId, categoryId, isActive: true,  isAvailable: true),
            MakeMenuItem("A2", restaurantId, categoryId, isActive: true,  isAvailable: false),
            MakeMenuItem("A3", restaurantId, categoryId, isActive: false, isAvailable: true));
        await _db.SaveChangesAsync();

        Assert.That(await _repo.CountAvailableAsync(restaurantId), Is.EqualTo(1));
    }

    [Test]
    public async Task GetPagedAllAsync_ReturnsAllItems()
    {
        var restaurantId = Guid.NewGuid();
        var categoryId   = Guid.NewGuid();
        _db.MenuItems.AddRange(
            MakeMenuItem("Active",   restaurantId, categoryId, isActive: true),
            MakeMenuItem("Inactive", restaurantId, categoryId, isActive: false));
        await _db.SaveChangesAsync();

        var result = await _repo.GetPagedAllAsync(restaurantId, 1, 20);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task CountAllAsync_CountsAll()
    {
        var restaurantId = Guid.NewGuid();
        var categoryId   = Guid.NewGuid();
        _db.MenuItems.AddRange(MakeMenuItem("A", restaurantId, categoryId), MakeMenuItem("B", restaurantId, categoryId, isActive: false));
        await _db.SaveChangesAsync();

        Assert.That(await _repo.CountAllAsync(restaurantId), Is.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsItem()
    {
        var item = MakeMenuItem("M");
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync();

        Assert.That((await _repo.GetByIdAsync(item.Id))!.Id, Is.EqualTo(item.Id));
    }

    [Test]
    public async Task AddAsync_PersistsItem()
    {
        await _repo.AddAsync(MakeMenuItem("New"));

        Assert.That(await _db.MenuItems.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task UpdateAsync_SavesChanges()
    {
        var item = MakeMenuItem("Original");
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync();

        item.Name = "Updated";
        await _repo.UpdateAsync(item);

        Assert.That((await _db.MenuItems.FindAsync(item.Id))!.Name, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task SoftDeleteAsync_SetsIsActiveToFalse()
    {
        var item = MakeMenuItem("M");
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync();

        Assert.That(await _repo.SoftDeleteAsync(item.Id), Is.True);
        Assert.That((await _db.MenuItems.FindAsync(item.Id))!.IsActive, Is.False);
    }

    [Test]
    public async Task SoftDeleteAsync_NonExistingId_ReturnsFalse()
    {
        Assert.That(await _repo.SoftDeleteAsync(Guid.NewGuid()), Is.False);
    }

    private static MenuItem MakeMenuItem(string name, Guid? restaurantId = null, Guid? categoryId = null,
        bool isActive = true, bool isAvailable = true, int sortOrder = 0) =>
        new()
        {
            RestaurantId = restaurantId ?? Guid.NewGuid(),
            CategoryId   = categoryId ?? Guid.NewGuid(),
            Name         = name, Price = 10, Currency = "EUR",
            IsActive = isActive, IsAvailable = isAvailable, SortOrder = sortOrder,
        };
}
