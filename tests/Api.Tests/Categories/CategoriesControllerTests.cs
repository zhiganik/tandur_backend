using Api.Controllers;
using Core.DTOs.Categories;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Categories;

[TestFixture]
public class CategoriesControllerTests
{
    private Mock<ICategoryService> _service = null!;
    private CategoriesController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<ICategoryService>();
        _controller = new CategoriesController(_service.Object);
    }

    [Test]
    public async Task GetVisible_ReturnsOkWithList()
    {
        var restaurantId = Guid.NewGuid();
        var list = new List<CategoryDto> { MakeDto("Starters"), MakeDto("Mains") };
        _service.Setup(s => s.GetVisibleByRestaurantAsync(restaurantId)).ReturnsAsync(list);

        var result = await _controller.GetVisible(restaurantId);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(list));
    }

    [Test]
    public async Task GetVisible_PassesRestaurantIdToService()
    {
        var restaurantId = Guid.NewGuid();
        _service.Setup(s => s.GetVisibleByRestaurantAsync(restaurantId)).ReturnsAsync([]);

        await _controller.GetVisible(restaurantId);

        _service.Verify(s => s.GetVisibleByRestaurantAsync(restaurantId), Times.Once);
    }

    private static CategoryDto MakeDto(string name) =>
        new() { Id = Guid.NewGuid(), RestaurantId = Guid.NewGuid(), Name = name };
}
