using Api.Controllers;
using Core.DTOs.Categories;
using Core.DTOs.MenuItems;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.MenuItems;

[TestFixture]
public class MenuItemsControllerTests
{
    private Mock<IMenuItemService> _service = null!;
    private MenuItemsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IMenuItemService>();
        _controller = new MenuItemsController(_service.Object);
    }

    [Test]
    public async Task GetMenu_ReturnsOkWithMenuDto()
    {
        var restaurantId = Guid.NewGuid();
        var menu = new MenuDto { Categories = [], Items = [] };
        _service.Setup(s => s.GetMenuAsync(restaurantId)).ReturnsAsync(menu);

        var result = await _controller.GetMenu(restaurantId);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(menu));
    }

    [Test]
    public async Task GetById_ExistingItem_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(MakeDto("Soup", id));

        var result = await _controller.GetById(id);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_NotFound_Returns404()
    {
        _service.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((MenuItemDto?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    private static MenuItemDto MakeDto(string name, Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), RestaurantId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Name = name, Currency = "EUR", Price = 10 };
}
