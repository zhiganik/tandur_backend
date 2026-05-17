using Api.Controllers;
using Core.DTOs.Restaurants;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Restaurants;

[TestFixture]
public class RestaurantsControllerTests
{
    private Mock<IRestaurantService> _service = null!;
    private RestaurantsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IRestaurantService>();
        _controller = new RestaurantsController(_service.Object);
    }

    [Test]
    public async Task GetAll_ReturnsOk_WithRestaurantList()
    {
        var list = new List<RestaurantDto> { MakeDto("R1"), MakeDto("R2") };
        _service.Setup(s => s.GetAllAsync(null, null)).ReturnsAsync(list);

        var result = await _controller.GetAll(null, null);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(list));
    }

    [Test]
    public async Task GetAll_PassesCoordinatesToService()
    {
        _service.Setup(s => s.GetAllAsync(43.25, 76.95)).ReturnsAsync([]);

        await _controller.GetAll(43.25, 76.95);

        _service.Verify(s => s.GetAllAsync(43.25, 76.95), Times.Once);
    }

    [Test]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = MakeDto("R1", id);
        _service.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(dto);

        var result = await _controller.GetById(id);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(dto));
    }

    [Test]
    public async Task GetById_NotFound_Returns404()
    {
        _service.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((RestaurantDto?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    private static RestaurantDto MakeDto(string name, Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), Name = name, Address = "Addr" };
}
