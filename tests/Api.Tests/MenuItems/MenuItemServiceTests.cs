using Core.Domain.Entities;
using Core.DTOs.MenuItems;
using Core.Interfaces.Repositories;
using Core.Services;
using Moq;

namespace Api.Tests.MenuItems;

[TestFixture]
public class MenuItemServiceTests
{
    private Mock<IMenuItemRepository> _menuRepo = null!;
    private Mock<ICategoryRepository> _categoryRepo = null!;
    private MenuItemService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _menuRepo = new Mock<IMenuItemRepository>();
        _categoryRepo = new Mock<ICategoryRepository>();
        _service = new MenuItemService(_menuRepo.Object, _categoryRepo.Object);
    }

    // GetMenuAsync
    [Test]
    public async Task GetMenuAsync_ReturnsOnlyVisibleCategoriesAndAvailableItems()
    {
        var restaurantId = Guid.NewGuid();
        _categoryRepo.Setup(r => r.GetVisibleByRestaurantAsync(restaurantId))
            .ReturnsAsync([MakeCategory("Hot"), MakeCategory("Cold")]);
        _menuRepo.Setup(r => r.GetAvailableByRestaurantAsync(restaurantId))
            .ReturnsAsync([MakeMenuItem("Soup"), MakeMenuItem("Salad")]);

        var result = await _service.GetMenuAsync(restaurantId);

        Assert.That(result.Categories, Has.Count.EqualTo(2));
        Assert.That(result.Items, Has.Count.EqualTo(2));
    }

    // GetAdminMenuAsync
    [Test]
    public async Task GetAdminMenuAsync_ReturnsAllCategoriesAndItems()
    {
        var restaurantId = Guid.NewGuid();
        _categoryRepo.Setup(r => r.GetAllByRestaurantAsync(restaurantId))
            .ReturnsAsync([MakeCategory("Visible"), MakeCategory("Hidden")]);
        _menuRepo.Setup(r => r.GetAllByRestaurantAsync(restaurantId))
            .ReturnsAsync([MakeMenuItem("Available"), MakeMenuItem("Unavailable")]);

        var result = await _service.GetAdminMenuAsync(restaurantId);

        Assert.That(result.Categories, Has.Count.EqualTo(2));
        Assert.That(result.Items, Has.Count.EqualTo(2));
    }

    // GetByIdAsync
    [Test]
    public async Task GetByIdAsync_ActiveItem_ReturnsDto()
    {
        var item = MakeMenuItem("Soup", isActive: true);
        _menuRepo.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);

        var result = await _service.GetByIdAsync(item.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Soup"));
    }

    [Test]
    public async Task GetByIdAsync_SoftDeletedItem_ReturnsNull()
    {
        var item = MakeMenuItem("Ghost", isActive: false);
        _menuRepo.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);

        var result = await _service.GetByIdAsync(item.Id);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _menuRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((MenuItem?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    // GetAdminByIdAsync
    [Test]
    public async Task GetAdminByIdAsync_SoftDeletedItem_ReturnsDto()
    {
        var item = MakeMenuItem("Ghost", isActive: false);
        _menuRepo.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);

        var result = await _service.GetAdminByIdAsync(item.Id);

        Assert.That(result, Is.Not.Null);
    }

    // CreateAsync
    [Test]
    public async Task CreateAsync_BuildsEntityAndCallsRepository()
    {
        _menuRepo.Setup(r => r.AddAsync(It.IsAny<MenuItem>())).ReturnsAsync((MenuItem m) => m);

        var result = await _service.CreateAsync(new CreateMenuItemRequest
        {
            RestaurantId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Name = "Burger",
            Price = 12.50m,
            Currency = "EUR",
        });

        Assert.That(result.Name, Is.EqualTo("Burger"));
        Assert.That(result.Price, Is.EqualTo(12.50m));
        _menuRepo.Verify(r => r.AddAsync(It.Is<MenuItem>(m => m.Name == "Burger")), Times.Once);
    }

    // UpdateAsync
    [Test]
    public async Task UpdateAsync_ExistingId_UpdatesFields()
    {
        var item = MakeMenuItem("Old");
        _menuRepo.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);
        _menuRepo.Setup(r => r.UpdateAsync(item)).Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(item.Id, new UpdateMenuItemRequest
        {
            Name = "New",
            Price = 15,
            Currency = "USD",
            CategoryId = Guid.NewGuid(),
            IsAvailable = false,
        });

        Assert.That(result!.Name, Is.EqualTo("New"));
        Assert.That(result.IsAvailable, Is.False);
        _menuRepo.Verify(r => r.UpdateAsync(item), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        _menuRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((MenuItem?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateMenuItemRequest { Name = "X", Price = 1, Currency = "EUR" });

        Assert.That(result, Is.Null);
    }

    // PatchAsync
    [Test]
    public async Task PatchAsync_TogglesAvailability()
    {
        var item = MakeMenuItem("M", isAvailable: true);
        _menuRepo.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);
        _menuRepo.Setup(r => r.UpdateAsync(item)).Returns(Task.CompletedTask);

        var result = await _service.PatchAsync(item.Id, new PatchMenuItemRequest { IsAvailable = false });

        Assert.That(result!.IsAvailable, Is.False);
    }

    [Test]
    public async Task PatchAsync_NullFields_DoNotChangeValues()
    {
        var item = MakeMenuItem("M", price: 9.99m, isAvailable: true);
        _menuRepo.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);
        _menuRepo.Setup(r => r.UpdateAsync(item)).Returns(Task.CompletedTask);

        var result = await _service.PatchAsync(item.Id, new PatchMenuItemRequest());

        Assert.That(result!.Price, Is.EqualTo(9.99m));
        Assert.That(result.IsAvailable, Is.True);
    }

    // DeleteAsync
    [Test]
    public async Task DeleteAsync_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        _menuRepo.Setup(r => r.SoftDeleteAsync(id)).ReturnsAsync(true);

        var result = await _service.DeleteAsync(id);

        Assert.That(result, Is.True);
        _menuRepo.Verify(r => r.SoftDeleteAsync(id), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        _menuRepo.Setup(r => r.SoftDeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _service.DeleteAsync(Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    private static Category MakeCategory(string name) =>
        new() { RestaurantId = Guid.NewGuid(), Name = name };

    private static MenuItem MakeMenuItem(string name, bool isActive = true, bool isAvailable = true, decimal price = 10) =>
        new()
        {
            RestaurantId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Name = name,
            Price = price,
            Currency = "EUR",
            IsActive = isActive,
            IsAvailable = isAvailable,
        };
}
