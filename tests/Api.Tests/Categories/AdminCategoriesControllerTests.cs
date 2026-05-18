using Api.Controllers;
using Core.Domain.Enums;
using Core.DTOs.Categories;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Categories;

[TestFixture]
public class AdminCategoriesControllerTests
{
    private Mock<ICategoryService> _service = null!;
    private AdminCategoriesController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<ICategoryService>();
        _controller = new AdminCategoriesController(_service.Object);
    }

    // GetAll
    [Test]
    public async Task GetAll_ReturnsOkWithList()
    {
        var restaurantId = Guid.NewGuid();
        var list = new List<CategoryDto> { MakeDto("A"), MakeDto("B") };
        _service.Setup(s => s.GetAllByRestaurantAsync(restaurantId)).ReturnsAsync(list);

        var result = await _controller.GetAll(restaurantId);

        var ok = result as OkObjectResult;
        Assert.That(ok!.Value, Is.SameAs(list));
    }

    // GetById
    [Test]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(MakeDto("C", id));

        var result = await _controller.GetById(id);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_NotFound_Returns404()
    {
        _service.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((CategoryDto?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // Create
    [Test]
    public async Task Create_ValidRequest_Returns201()
    {
        var restaurantId = Guid.NewGuid();
        var dto = MakeDto("New");
        var request = new CreateCategoryRequest { Name = "New", SortOrder = 0, IsVisible = true };
        _service.Setup(s => s.CreateAsync(restaurantId, request)).ReturnsAsync(dto);

        var result = await _controller.Create(restaurantId, request);

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
        _service.Setup(s => s.UpdateAsync(id, It.IsAny<UpdateCategoryRequest>())).ReturnsAsync(MakeDto("Updated", id));

        var result = await _controller.Update(id, new UpdateCategoryRequest { Name = "Updated" });

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Update_NotFound_Returns404()
    {
        _service.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateCategoryRequest>()))
            .ReturnsAsync((CategoryDto?)null);

        var result = await _controller.Update(Guid.NewGuid(), new UpdateCategoryRequest { Name = "X" });

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // Patch
    [Test]
    public async Task Patch_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.PatchAsync(id, It.IsAny<PatchCategoryRequest>())).ReturnsAsync(MakeDto("C", id));

        var result = await _controller.Patch(id, new PatchCategoryRequest { IsVisible = false });

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Patch_NotFound_Returns404()
    {
        _service.Setup(s => s.PatchAsync(It.IsAny<Guid>(), It.IsAny<PatchCategoryRequest>()))
            .ReturnsAsync((CategoryDto?)null);

        var result = await _controller.Patch(Guid.NewGuid(), new PatchCategoryRequest());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // Delete
    [Test]
    public async Task Delete_ExistingCategory_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.DeleteAsync(id)).ReturnsAsync(DeleteCategoryResult.Deleted);

        var result = await _controller.Delete(id);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_NotFound_Returns404()
    {
        _service.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(DeleteCategoryResult.NotFound);

        var result = await _controller.Delete(Guid.NewGuid());

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_HasItems_Returns409Conflict()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.DeleteAsync(id)).ReturnsAsync(DeleteCategoryResult.HasItems);

        var result = await _controller.Delete(id);

        var conflict = result as ConflictObjectResult;
        Assert.That(conflict, Is.Not.Null);
        Assert.That(conflict!.StatusCode, Is.EqualTo(409));
    }

    private static CategoryDto MakeDto(string name, Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), RestaurantId = Guid.NewGuid(), Name = name };
}
