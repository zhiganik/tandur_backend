using Core.Domain.Entities;
using Core.DTOs.Restaurants;
using Core.Interfaces.Repositories;
using Core.Services;
using Moq;

namespace Api.Tests.Restaurants;

[TestFixture]
public class RestaurantServiceTests
{
    private Mock<IRestaurantRepository> _repo = null!;
    private RestaurantService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IRestaurantRepository>();
        _service = new RestaurantService(_repo.Object);
    }

    // GetAllAsync

    [Test]
    public async Task GetAllAsync_ReturnsOnlyActiveRestaurants()
    {
        _repo.Setup(r => r.GetActiveAsync())
            .ReturnsAsync([MakeRestaurant("Active 1"), MakeRestaurant("Active 2")]);

        var result = await _service.GetAllAsync(null, null);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(r => r.Name), Is.EquivalentTo(new[] { "Active 1", "Active 2" }));
    }

    [Test]
    public async Task GetAllAsync_WithCoordinates_SortsByDistanceAscending()
    {
        // Almaty ~(43.25, 76.95). Near restaurant should sort first.
        _repo.Setup(r => r.GetActiveAsync()).ReturnsAsync(
        [
            MakeRestaurant("Far",  lat: 51.51, lng: -0.13),   // London
            MakeRestaurant("Near", lat: 43.26, lng: 76.96),   // ~1 km from query point
        ]);

        var result = await _service.GetAllAsync(43.25, 76.95);

        Assert.That(result[0].Name, Is.EqualTo("Near"));
        Assert.That(result[0].DistanceKm!.Value, Is.LessThan(result[1].DistanceKm!.Value));
    }

    [Test]
    public async Task GetAllAsync_WithoutCoordinates_DistanceKmIsNull()
    {
        _repo.Setup(r => r.GetActiveAsync()).ReturnsAsync([MakeRestaurant("R1")]);

        var result = await _service.GetAllAsync(null, null);

        Assert.That(result[0].DistanceKm, Is.Null);
    }

    // GetAdminListAsync

    [Test]
    public async Task GetAdminListAsync_ReturnsAllRestaurants()
    {
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            MakeRestaurant("Active",   isActive: true),
            MakeRestaurant("Inactive", isActive: false),
        ]);

        var result = await _service.GetAdminListAsync();

        Assert.That(result, Has.Count.EqualTo(2));
    }

    // GetByIdAsync

    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var r = MakeRestaurant("My Restaurant");
        _repo.Setup(x => x.GetByIdAsync(r.Id)).ReturnsAsync(r);

        var result = await _service.GetByIdAsync(r.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(r.Id));
        Assert.That(result.Name, Is.EqualTo("My Restaurant"));
    }

    [Test]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Restaurant?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    // CreateAsync

    [Test]
    public async Task CreateAsync_BuildsEntityAndCallsRepository()
    {
        _repo.Setup(x => x.AddAsync(It.IsAny<Restaurant>()))
            .ReturnsAsync((Restaurant r) => r);

        var result = await _service.CreateAsync(new CreateRestaurantRequest
        {
            Name = "New Place", Address = "123 St", Latitude = 43.25, Longitude = 76.95,
            TimeZone = "UTC", OpenTime = TimeSpan.FromHours(9), CloseTime = TimeSpan.FromHours(22),
        });

        Assert.That(result.Name, Is.EqualTo("New Place"));
        Assert.That(result.IsActive, Is.True);
        _repo.Verify(x => x.AddAsync(It.Is<Restaurant>(r => r.Name == "New Place")), Times.Once);
    }

    // UpdateAsync

    [Test]
    public async Task UpdateAsync_ExistingId_UpdatesFieldsAndSaves()
    {
        var r = MakeRestaurant("Old Name");
        _repo.Setup(x => x.GetByIdAsync(r.Id)).ReturnsAsync(r);
        _repo.Setup(x => x.UpdateAsync(r)).Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(r.Id, new UpdateRestaurantRequest
        {
            Name = "New Name", Address = "New Addr", Latitude = 1, Longitude = 2,
            OpenTime = TimeSpan.FromHours(8), CloseTime = TimeSpan.FromHours(20),
        });

        Assert.That(result!.Name, Is.EqualTo("New Name"));
        _repo.Verify(x => x.UpdateAsync(r), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Restaurant?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateRestaurantRequest
        {
            Name = "X", Address = "X", OpenTime = TimeSpan.Zero, CloseTime = TimeSpan.FromHours(1),
        });

        Assert.That(result, Is.Null);
        _repo.Verify(x => x.UpdateAsync(It.IsAny<Restaurant>()), Times.Never);
    }

    // PatchAsync

    [Test]
    public async Task PatchAsync_SetsIsActiveToFalse()
    {
        var r = MakeRestaurant("R", isActive: true);
        _repo.Setup(x => x.GetByIdAsync(r.Id)).ReturnsAsync(r);
        _repo.Setup(x => x.UpdateAsync(r)).Returns(Task.CompletedTask);

        var result = await _service.PatchAsync(r.Id, new PatchRestaurantRequest { IsActive = false });

        Assert.That(result!.IsActive, Is.False);
        _repo.Verify(x => x.UpdateAsync(r), Times.Once);
    }

    [Test]
    public async Task PatchAsync_NullIsActive_DoesNotChangeValue()
    {
        var r = MakeRestaurant("R", isActive: true);
        _repo.Setup(x => x.GetByIdAsync(r.Id)).ReturnsAsync(r);
        _repo.Setup(x => x.UpdateAsync(r)).Returns(Task.CompletedTask);

        var result = await _service.PatchAsync(r.Id, new PatchRestaurantRequest { IsActive = null });

        Assert.That(result!.IsActive, Is.True);
    }

    [Test]
    public async Task PatchAsync_NonExistingId_ReturnsNull()
    {
        _repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Restaurant?)null);

        var result = await _service.PatchAsync(Guid.NewGuid(), new PatchRestaurantRequest { IsActive = false });

        Assert.That(result, Is.Null);
    }

    // DeleteAsync

    [Test]
    public async Task DeleteAsync_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        _repo.Setup(x => x.SoftDeleteAsync(id)).ReturnsAsync(true);

        var result = await _service.DeleteAsync(id);

        Assert.That(result, Is.True);
        _repo.Verify(x => x.SoftDeleteAsync(id), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        _repo.Setup(x => x.SoftDeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _service.DeleteAsync(Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    private static Restaurant MakeRestaurant(string name, bool isActive = true, double lat = 0, double lng = 0) =>
        new()
        {
            Name = name,
            Address = "Test Address",
            Latitude = lat,
            Longitude = lng,
            TimeZone = "UTC",
            OpenTime = TimeSpan.FromHours(9),
            CloseTime = TimeSpan.FromHours(22),
            IsActive = isActive,
        };
}
