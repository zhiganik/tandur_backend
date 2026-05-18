using Api.Controllers;
using Core.DTOs.Common;
using Core.DTOs.MenuItems;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.MenuItems;

[TestFixture]
public class AdminMenuItemsControllerTests
{
    private Mock<IMenuItemService>      _service    = null!;
    private AdminMenuItemsController    _controller = null!;

    private static readonly PaginationQuery DefaultQuery = new() { Page = 1, Limit = 20 };

    [SetUp]
    public void SetUp()
    {
        _service    = new Mock<IMenuItemService>();
        _controller = new AdminMenuItemsController(_service.Object);
    }

    [Test]
    public async Task GetMenu_ReturnsOkWithAdminMenuDto()
    {
        var restaurantId = Guid.NewGuid();
        var menu         = new MenuDto();
        _service.Setup(s => s.GetAdminMenuAsync(restaurantId, DefaultQuery)).ReturnsAsync(menu);

        var result = await _controller.GetMenu(restaurantId, DefaultQuery);

        var ok = result as OkObjectResult;
        Assert.That(ok!.Value, Is.SameAs(menu));
    }

    [Test]
    public async Task GetById_ExistingItem_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetAdminByIdAsync(id)).ReturnsAsync(MakeDto("M", id));

        Assert.That(await _controller.GetById(id), Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_NotFound_Returns404()
    {
        _service.Setup(s => s.GetAdminByIdAsync(It.IsAny<Guid>())).ReturnsAsync((MenuItemDto?)null);

        Assert.That(await _controller.GetById(Guid.NewGuid()), Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Create_ValidRequest_Returns201()
    {
        var dto     = MakeDto("Burger");
        var request = new CreateMenuItemRequest
        {
            RestaurantId = Guid.NewGuid(), CategoryId = Guid.NewGuid(),
            Name = "Burger", Price = 12, Currency = "EUR",
        };
        _service.Setup(s => s.CreateAsync(request)).ReturnsAsync(dto);

        var result  = await _controller.Create(request);
        var created = result as CreatedAtActionResult;

        Assert.That(created!.StatusCode, Is.EqualTo(201));
        Assert.That(created.Value, Is.SameAs(dto));
    }

    [Test]
    public async Task Update_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.UpdateAsync(id, It.IsAny<UpdateMenuItemRequest>())).ReturnsAsync(MakeDto("U", id));

        Assert.That(await _controller.Update(id, new UpdateMenuItemRequest
        {
            Name = "U", Price = 10, Currency = "EUR", CategoryId = Guid.NewGuid(),
        }), Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Update_NotFound_Returns404()
    {
        _service.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateMenuItemRequest>()))
            .ReturnsAsync((MenuItemDto?)null);

        Assert.That(await _controller.Update(Guid.NewGuid(), new UpdateMenuItemRequest
        {
            Name = "X", Price = 1, Currency = "EUR", CategoryId = Guid.NewGuid(),
        }), Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Patch_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.PatchAsync(id, It.IsAny<PatchMenuItemRequest>())).ReturnsAsync(MakeDto("M", id));

        Assert.That(await _controller.Patch(id, new PatchMenuItemRequest { IsAvailable = false }), Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Delete_ExistingItem_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

        Assert.That(await _controller.Delete(id), Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_NotFound_Returns404()
    {
        _service.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        Assert.That(await _controller.Delete(Guid.NewGuid()), Is.InstanceOf<NotFoundResult>());
    }

    private static MenuItemDto MakeDto(string name, Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), RestaurantId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Name = name, Currency = "EUR", Price = 10 };
}
