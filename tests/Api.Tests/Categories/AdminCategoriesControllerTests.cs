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
    private Mock<ICategoryService>      _service    = null!;
    private AdminCategoriesController   _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service    = new Mock<ICategoryService>();
        _controller = new AdminCategoriesController(_service.Object);
    }

    [Test]
    public async Task GetAll_ReturnsOkWithList()
    {
        var restaurantId = Guid.NewGuid();
        IReadOnlyList<CategoryDto> list = [MakeDto("A"), MakeDto("B")];
        _service.Setup(s => s.GetAllByRestaurantAsync(restaurantId)).ReturnsAsync(list);

        var result = await _controller.GetAll(restaurantId);

        var ok = result as OkObjectResult;
        Assert.That(ok!.Value, Is.SameAs(list));
    }

    [Test]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(MakeDto("C", id));

        Assert.That(await _controller.GetById(id), Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetById_NotFound_Returns404()
    {
        _service.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((CategoryDto?)null);

        Assert.That(await _controller.GetById(Guid.NewGuid()), Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Create_ValidRequest_Returns201()
    {
        var restaurantId = Guid.NewGuid();
        var dto          = MakeDto("New");
        var request      = new CreateCategoryRequest { Name = "New", SortOrder = 0, IsVisible = true };
        _service.Setup(s => s.CreateAsync(restaurantId, request)).ReturnsAsync(dto);

        var result  = await _controller.Create(restaurantId, request);
        var created = result as CreatedAtActionResult;

        Assert.That(created!.StatusCode, Is.EqualTo(201));
        Assert.That(created.Value, Is.SameAs(dto));
    }

    [Test]
    public async Task Update_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.UpdateAsync(id, It.IsAny<UpdateCategoryRequest>())).ReturnsAsync(MakeDto("U", id));

        Assert.That(await _controller.Update(id, new UpdateCategoryRequest { Name = "U" }), Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task Update_NotFound_Returns404()
    {
        _service.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateCategoryRequest>()))
            .ReturnsAsync((CategoryDto?)null);

        Assert.That(await _controller.Update(Guid.NewGuid(), new UpdateCategoryRequest { Name = "X" }), Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_ExistingCategory_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.DeleteAsync(id)).ReturnsAsync(DeleteCategoryResult.Deleted);

        Assert.That(await _controller.Delete(id), Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_NotFound_Returns404()
    {
        _service.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(DeleteCategoryResult.NotFound);

        Assert.That(await _controller.Delete(Guid.NewGuid()), Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_HasItems_Returns409Conflict()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.DeleteAsync(id)).ReturnsAsync(DeleteCategoryResult.HasItems);

        var result = await _controller.Delete(id) as ConflictObjectResult;

        Assert.That(result!.StatusCode, Is.EqualTo(409));
    }

    private static CategoryDto MakeDto(string name, Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), RestaurantId = Guid.NewGuid(), Name = name };
}
