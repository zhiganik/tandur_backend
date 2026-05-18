using Core.Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests.Categories;

[TestFixture]
public class CategoryRepositoryTests
{
    private AppDbContext _db = null!;
    private CategoryRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repo = new CategoryRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetVisibleByRestaurantAsync_ReturnsOnlyVisibleOrderedBySortOrder()
    {
        var restaurantId = Guid.NewGuid();
        _db.Categories.AddRange(
            MakeCategory("Hidden", restaurantId, isVisible: false, sortOrder: 0),
            MakeCategory("Last",   restaurantId, isVisible: true,  sortOrder: 2),
            MakeCategory("First",  restaurantId, isVisible: true,  sortOrder: 1));
        await _db.SaveChangesAsync();

        var result = await _repo.GetVisibleByRestaurantAsync(restaurantId);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Name, Is.EqualTo("First"));
        Assert.That(result[1].Name, Is.EqualTo("Last"));
    }

    [Test]
    public async Task GetVisibleByRestaurantAsync_DoesNotReturnOtherRestaurantCategories()
    {
        var restaurantId = Guid.NewGuid();
        _db.Categories.Add(MakeCategory("Other", Guid.NewGuid()));
        await _db.SaveChangesAsync();

        var result = await _repo.GetVisibleByRestaurantAsync(restaurantId);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAllByRestaurantAsync_ReturnsAllIncludingHidden()
    {
        var restaurantId = Guid.NewGuid();
        _db.Categories.AddRange(
            MakeCategory("Visible", restaurantId, isVisible: true),
            MakeCategory("Hidden",  restaurantId, isVisible: false));
        await _db.SaveChangesAsync();

        var result = await _repo.GetAllByRestaurantAsync(restaurantId);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsCategory()
    {
        var category = MakeCategory("C");
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(category.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(category.Id));
    }

    [Test]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AddAsync_PersistsCategory()
    {
        var category = MakeCategory("New");

        await _repo.AddAsync(category);

        Assert.That(await _db.Categories.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task UpdateAsync_SavesChanges()
    {
        var category = MakeCategory("Original");
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        category.Name = "Updated";
        await _repo.UpdateAsync(category);

        Assert.That((await _db.Categories.FindAsync(category.Id))!.Name, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task DeleteAsync_RemovesCategory()
    {
        var category = MakeCategory("C");
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var result = await _repo.DeleteAsync(category.Id);

        Assert.That(result, Is.True);
        Assert.That(await _db.Categories.AnyAsync(c => c.Id == category.Id), Is.False);
    }

    [Test]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _repo.DeleteAsync(Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task HasActiveItemsAsync_WhenItemsExist_ReturnsTrue()
    {
        var restaurantId = Guid.NewGuid();
        var category = MakeCategory("C", restaurantId);
        _db.Categories.Add(category);
        _db.MenuItems.Add(MakeMenuItem(category.Id, restaurantId, isActive: true));
        await _db.SaveChangesAsync();

        var result = await _repo.HasActiveItemsAsync(category.Id);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task HasActiveItemsAsync_WhenOnlySoftDeletedItems_ReturnsFalse()
    {
        var restaurantId = Guid.NewGuid();
        var category = MakeCategory("C", restaurantId);
        _db.Categories.Add(category);
        _db.MenuItems.Add(MakeMenuItem(category.Id, restaurantId, isActive: false));
        await _db.SaveChangesAsync();

        var result = await _repo.HasActiveItemsAsync(category.Id);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task HasActiveItemsAsync_WhenNoItems_ReturnsFalse()
    {
        var category = MakeCategory("C");
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var result = await _repo.HasActiveItemsAsync(category.Id);

        Assert.That(result, Is.False);
    }

    private static Category MakeCategory(string name, Guid? restaurantId = null, bool isVisible = true, int sortOrder = 0) =>
        new()
        {
            RestaurantId = restaurantId ?? Guid.NewGuid(),
            Name = name,
            SortOrder = sortOrder,
            IsVisible = isVisible,
        };

    private static MenuItem MakeMenuItem(Guid categoryId, Guid restaurantId, bool isActive = true) =>
        new()
        {
            CategoryId = categoryId,
            RestaurantId = restaurantId,
            Name = "Item",
            Price = 10,
            Currency = "EUR",
            IsActive = isActive,
        };
}
