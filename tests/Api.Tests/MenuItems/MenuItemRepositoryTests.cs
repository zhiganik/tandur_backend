using Core.Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests.MenuItems;

[TestFixture]
public class MenuItemRepositoryTests
{
    private AppDbContext _db = null!;
    private MenuItemRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repo = new MenuItemRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetAvailableByRestaurantAsync_ReturnsOnlyActiveAndAvailableOrderedBySortOrder()
    {
        var restaurantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        _db.MenuItems.AddRange(
            MakeMenuItem("Available",  restaurantId, categoryId, isActive: true,  isAvailable: true,  sortOrder: 2),
            MakeMenuItem("Unavailable",restaurantId, categoryId, isActive: true,  isAvailable: false, sortOrder: 1),
            MakeMenuItem("Deleted",    restaurantId, categoryId, isActive: false, isAvailable: true,  sortOrder: 0),
            MakeMenuItem("First",      restaurantId, categoryId, isActive: true,  isAvailable: true,  sortOrder: 1));
        await _db.SaveChangesAsync();

        var result = await _repo.GetAvailableByRestaurantAsync(restaurantId);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Name, Is.EqualTo("First"));
        Assert.That(result[1].Name, Is.EqualTo("Available"));
    }

    [Test]
    public async Task GetAvailableByRestaurantAsync_DoesNotReturnOtherRestaurantItems()
    {
        _db.MenuItems.Add(MakeMenuItem("Other", Guid.NewGuid(), Guid.NewGuid()));
        await _db.SaveChangesAsync();

        var result = await _repo.GetAvailableByRestaurantAsync(Guid.NewGuid());

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAllByRestaurantAsync_ReturnsAllItemsIncludingUnavailable()
    {
        var restaurantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        _db.MenuItems.AddRange(
            MakeMenuItem("Active",   restaurantId, categoryId, isActive: true,  isAvailable: true),
            MakeMenuItem("Inactive", restaurantId, categoryId, isActive: false, isAvailable: false));
        await _db.SaveChangesAsync();

        var result = await _repo.GetAllByRestaurantAsync(restaurantId);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsItem()
    {
        var item = MakeMenuItem("M");
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(item.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(item.Id));
    }

    [Test]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AddAsync_PersistsItem()
    {
        var item = MakeMenuItem("New");

        await _repo.AddAsync(item);

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
        var item = MakeMenuItem("M", isActive: true);
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync();

        var result = await _repo.SoftDeleteAsync(item.Id);

        Assert.That(result, Is.True);
        Assert.That((await _db.MenuItems.FindAsync(item.Id))!.IsActive, Is.False);
    }

    [Test]
    public async Task SoftDeleteAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _repo.SoftDeleteAsync(Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    private static MenuItem MakeMenuItem(string name, Guid? restaurantId = null, Guid? categoryId = null,
        bool isActive = true, bool isAvailable = true, int sortOrder = 0) =>
        new()
        {
            RestaurantId = restaurantId ?? Guid.NewGuid(),
            CategoryId = categoryId ?? Guid.NewGuid(),
            Name = name,
            Price = 10,
            Currency = "EUR",
            IsActive = isActive,
            IsAvailable = isAvailable,
            SortOrder = sortOrder,
        };
}
