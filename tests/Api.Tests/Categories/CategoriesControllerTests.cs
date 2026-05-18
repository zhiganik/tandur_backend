using Api.Controllers;
using Core.DTOs.Categories;
using Core.DTOs.Common;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Categories;

[TestFixture]
public class CategoriesControllerTests
{
    private Mock<ICategoryService> _service    = null!;
    private CategoriesController   _controller = null!;

    private static readonly PaginationQuery DefaultQuery = new() { Page = 1, Limit = 20 };

    [SetUp]
    public void SetUp()
    {
        _service    = new Mock<ICategoryService>();
        _controller = new CategoriesController(_service.Object);
    }

    [Test]
    public async Task GetVisible_ReturnsOkWithPagedResult()
    {
        var restaurantId = Guid.NewGuid();
        var paged        = new PagedResult<CategoryDto> { Data = [MakeDto("Starters"), MakeDto("Mains")], Total = 2, Page = 1, Limit = 20 };
        _service.Setup(s => s.GetVisibleByRestaurantAsync(restaurantId, DefaultQuery)).ReturnsAsync(paged);

        var result = await _controller.GetVisible(restaurantId, DefaultQuery);

        var ok = result as OkObjectResult;
        Assert.That(ok!.Value, Is.SameAs(paged));
    }

    [Test]
    public async Task GetVisible_PassesQueryToService()
    {
        var restaurantId = Guid.NewGuid();
        _service.Setup(s => s.GetVisibleByRestaurantAsync(restaurantId, DefaultQuery))
            .ReturnsAsync(new PagedResult<CategoryDto>());

        await _controller.GetVisible(restaurantId, DefaultQuery);

        _service.Verify(s => s.GetVisibleByRestaurantAsync(restaurantId, DefaultQuery), Times.Once);
    }

    private static CategoryDto MakeDto(string name) =>
        new() { Id = Guid.NewGuid(), RestaurantId = Guid.NewGuid(), Name = name };
}
