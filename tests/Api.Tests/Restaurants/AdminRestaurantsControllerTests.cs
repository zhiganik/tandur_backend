using Api.Controllers;
using Core.DTOs.Restaurants;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Restaurants;

[TestFixture]
public class AdminRestaurantsControllerTests
{
    private Mock<IRestaurantService> _service = null!;
    private AdminRestaurantsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IRestaurantService>();
        _controller = new AdminRestaurantsController(_service.Object);
    }

    // GetAll

    [Test]
    public async Task GetAll_ReturnsOk_WithAllRestaurants()
    {
        var list = new List<RestaurantDto> { MakeDto("Active"), MakeDto("Inactive") };
        _service.Setup(s => s.GetAdminListAsync()).ReturnsAsync(list);

        var result = await _controller.GetAll();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(list));
    }

    // GetById

    [Test]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(MakeDto("R1", id));

        var result = await _controller.GetById(id);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_NotFound_Returns404()
    {
        _service.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((RestaurantDto?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // Create

    [Test]
    public async Task Create_ValidRequest_Returns201WithDto()
    {
        var dto = MakeDto("New");
        var request = new CreateRestaurantRequest
        {
            Name = "New", Address = "Addr", Latitude = 0, Longitude = 0,
            TimeZone = "UTC", OpenTime = TimeSpan.FromHours(9), CloseTime = TimeSpan.FromHours(22),
        };
        _service.Setup(s => s.CreateAsync(request)).ReturnsAsync(dto);

        var result = await _controller.Create(request);

        var created = result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.StatusCode, Is.EqualTo(201));
        Assert.That(created.Value, Is.SameAs(dto));
    }

    // Update

    [Test]
    public async Task Update_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = MakeDto("Updated", id);
        var request = new UpdateRestaurantRequest
        {
            Name = "Updated", Address = "Addr", Latitude = 0, Longitude = 0,
            OpenTime = TimeSpan.FromHours(9), CloseTime = TimeSpan.FromHours(22),
        };
        _service.Setup(s => s.UpdateAsync(id, request)).ReturnsAsync(dto);

        var result = await _controller.Update(id, request);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(dto));
    }

    [Test]
    public async Task Update_NotFound_Returns404()
    {
        _service.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateRestaurantRequest>()))
            .ReturnsAsync((RestaurantDto?)null);

        var result = await _controller.Update(Guid.NewGuid(), new UpdateRestaurantRequest
        {
            Name = "X", Address = "X", OpenTime = TimeSpan.Zero, CloseTime = TimeSpan.FromHours(1),
        });

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // Patch

    [Test]
    public async Task Patch_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.PatchAsync(id, It.IsAny<PatchRestaurantRequest>()))
            .ReturnsAsync(MakeDto("R", id));

        var result = await _controller.Patch(id, new PatchRestaurantRequest { IsActive = false });

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Patch_NotFound_Returns404()
    {
        _service.Setup(s => s.PatchAsync(It.IsAny<Guid>(), It.IsAny<PatchRestaurantRequest>()))
            .ReturnsAsync((RestaurantDto?)null);

        var result = await _controller.Patch(Guid.NewGuid(), new PatchRestaurantRequest { IsActive = true });

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // Delete

    [Test]
    public async Task Delete_ExistingId_ReturnsNoContent()
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

    private static RestaurantDto MakeDto(string name, Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), Name = name, Address = "Addr" };
}
