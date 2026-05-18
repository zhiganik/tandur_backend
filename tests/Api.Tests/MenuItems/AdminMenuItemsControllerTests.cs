using Api.Controllers;
using Core.DTOs.MenuItems;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.MenuItems;

[TestFixture]
public class AdminMenuItemsControllerTests
{
    private Mock<IMenuItemService> _service = null!;
    private AdminMenuItemsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IMenuItemService>();
        _controller = new AdminMenuItemsController(_service.Object);
    }

    // GetMenu
    [Test]
    public async Task GetMenu_ReturnsOkWithAdminMenuDto()
    {
        var restaurantId = Guid.NewGuid();
        var menu = new MenuDto { Categories = [], Items = [] };
        _service.Setup(s => s.GetAdminMenuAsync(restaurantId)).ReturnsAsync(menu);

        var result = await _controller.GetMenu(restaurantId);

        var ok = result as OkObjectResult;
        Assert.That(ok!.Value, Is.SameAs(menu));
    }

    // GetById
    [Test]
    public async Task GetById_ExistingItem_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetAdminByIdAsync(id)).ReturnsAsync(MakeDto("M", id));

        var result = await _controller.GetById(id);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_NotFound_Returns404()
    {
        _service.Setup(s => s.GetAdminByIdAsync(It.IsAny<Guid>())).ReturnsAsync((MenuItemDto?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // Create
    [Test]
    public async Task Create_ValidRequest_Returns201()
    {
        var dto = MakeDto("Burger");
        var request = new CreateMenuItemRequest
        {
            RestaurantId = Guid.NewGuid(), CategoryId = Guid.NewGuid(),
            Name = "Burger", Price = 12, Currency = "EUR",
        };
        _service.Setup(s => s.CreateAsync(request)).ReturnsAsync(dto);

        var result = await _controller.Create(request);

        var created = result as CreatedAtActionResult;
        Assert.That(created!.StatusCode, Is.EqualTo(201));
        Assert.That(created.Value, Is.SameAs(dto));
    }

    // Update
    [Test]
    public async Task Update_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.UpdateAsync(id, It.IsAny<UpdateMenuItemRequest>())).ReturnsAsync(MakeDto("Updated", id));

        var result = await _controller.Update(id, new UpdateMenuItemRequest { Name = "Updated", Price = 10, Currency = "EUR", CategoryId = Guid.NewGuid() });

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Update_NotFound_Returns404()
    {
        _service.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateMenuItemRequest>()))
            .ReturnsAsync((MenuItemDto?)null);

        var result = await _controller.Update(Guid.NewGuid(), new UpdateMenuItemRequest { Name = "X", Price = 1, Currency = "EUR", CategoryId = Guid.NewGuid() });

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // Patch
    [Test]
    public async Task Patch_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.PatchAsync(id, It.IsAny<PatchMenuItemRequest>())).ReturnsAsync(MakeDto("M", id));

        var result = await _controller.Patch(id, new PatchMenuItemRequest { IsAvailable = false });

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Patch_NotFound_Returns404()
    {
        _service.Setup(s => s.PatchAsync(It.IsAny<Guid>(), It.IsAny<PatchMenuItemRequest>()))
            .ReturnsAsync((MenuItemDto?)null);

        var result = await _controller.Patch(Guid.NewGuid(), new PatchMenuItemRequest());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // Delete
    [Test]
    public async Task Delete_ExistingItem_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

        var result = await _controller.Delete(id);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_NotFound_Returns404()
    {
        _service.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _controller.Delete(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    private static MenuItemDto MakeDto(string name, Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), RestaurantId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Name = name, Currency = "EUR", Price = 10 };
}
