using Api.Controllers;
using Core.DTOs.Restaurants;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Restaurants;

[TestFixture]
public class AdminRestaurantsControllerTests
{
    private Mock<IRestaurantService>   _service    = null!;
    private AdminRestaurantsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service    = new Mock<IRestaurantService>();
        _controller = new AdminRestaurantsController(_service.Object);
    }

    [Test]
    public async Task GetAll_ReturnsOk_WithList()
    {
        IReadOnlyList<RestaurantDto> list = [MakeDto("A"), MakeDto("B")];
        _service.Setup(s => s.GetAdminListAsync()).ReturnsAsync(list);

        var result = await _controller.GetAll();

        var ok = result as OkObjectResult;
        Assert.That(ok!.Value, Is.SameAs(list));
    }

    [Test]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(MakeDto("R1", id));

        Assert.That(await _controller.GetById(id), Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_NotFound_Returns404()
    {
        _service.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((RestaurantDto?)null);

        Assert.That(await _controller.GetById(Guid.NewGuid()), Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Create_ValidRequest_Returns201()
    {
        var dto     = MakeDto("New");
        var request = new CreateRestaurantRequest
        {
            Name = "New", Address = "Addr", Latitude = 0, Longitude = 0,
            Currency = "USD", TimeZone = "UTC",
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
        _service.Setup(s => s.UpdateAsync(id, It.IsAny<UpdateRestaurantRequest>())).ReturnsAsync(MakeDto("U", id));

        Assert.That(await _controller.Update(id, new UpdateRestaurantRequest
        {
            Name = "U", Address = "A", Currency = "USD",
        }), Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Update_NotFound_Returns404()
    {
        _service.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateRestaurantRequest>()))
            .ReturnsAsync((RestaurantDto?)null);

        Assert.That(await _controller.Update(Guid.NewGuid(), new UpdateRestaurantRequest
        {
            Name = "X", Address = "X", Currency = "USD",
        }), Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Patch_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.PatchAsync(id, It.IsAny<PatchRestaurantRequest>())).ReturnsAsync(MakeDto("R", id));

        Assert.That(await _controller.Patch(id, new PatchRestaurantRequest { IsActive = false }), Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Delete_ExistingId_ReturnsNoContent()
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

    private static RestaurantDto MakeDto(string name, Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), Name = name, Address = "Addr" };
}
