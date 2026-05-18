using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.DTOs.Categories;
using Core.DTOs.Common;
using Core.Interfaces.Repositories;
using Core.Services;
using Moq;

namespace Api.Tests.Categories;

[TestFixture]
public class CategoryServiceTests
{
    private Mock<ICategoryRepository> _repo    = null!;
    private CategoryService           _service = null!;

    private static readonly PaginationQuery DefaultQuery = new() { Page = 1, Limit = 20 };

    [SetUp]
    public void SetUp()
    {
        _repo    = new Mock<ICategoryRepository>();
        _service = new CategoryService(_repo.Object);
    }

    // GetVisibleByRestaurantAsync
    [Test]
    public async Task GetVisibleByRestaurantAsync_ReturnsMappedPagedResult()
    {
        var restaurantId = Guid.NewGuid();
        _repo.Setup(r => r.CountVisibleAsync(restaurantId)).ReturnsAsync(2);
        _repo.Setup(r => r.GetPagedVisibleAsync(restaurantId, 1, 20))
            .ReturnsAsync([MakeCategory("Starters", restaurantId), MakeCategory("Mains", restaurantId)]);

        var result = await _service.GetVisibleByRestaurantAsync(restaurantId, DefaultQuery);

        Assert.That(result.Data, Has.Count.EqualTo(2));
        Assert.That(result.Total, Is.EqualTo(2));
    }

    // GetAllByRestaurantAsync
    [Test]
    public async Task GetAllByRestaurantAsync_ReturnsAllIncludingHidden()
    {
        var restaurantId = Guid.NewGuid();
        _repo.Setup(r => r.CountAllAsync(restaurantId)).ReturnsAsync(2);
        _repo.Setup(r => r.GetPagedAllAsync(restaurantId, 1, 20))
            .ReturnsAsync([MakeCategory("Visible", restaurantId), MakeCategory("Hidden", restaurantId, isVisible: false)]);

        var result = await _service.GetAllByRestaurantAsync(restaurantId, DefaultQuery);

        Assert.That(result.Data, Has.Count.EqualTo(2));
        Assert.That(result.Total, Is.EqualTo(2));
    }

    // GetByIdAsync
    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var category = MakeCategory("Desserts");
        _repo.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

        var result = await _service.GetByIdAsync(category.Id);

        Assert.That(result!.Name, Is.EqualTo("Desserts"));
    }

    [Test]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

        Assert.That(await _service.GetByIdAsync(Guid.NewGuid()), Is.Null);
    }

    // CreateAsync
    [Test]
    public async Task CreateAsync_BuildsEntityAndCallsRepository()
    {
        var restaurantId = Guid.NewGuid();
        _repo.Setup(r => r.AddAsync(It.IsAny<Category>())).ReturnsAsync((Category c) => c);

        var result = await _service.CreateAsync(restaurantId, new CreateCategoryRequest
        {
            Name = "Cold Dishes", SortOrder = 1, IsVisible = true,
        });

        Assert.That(result.Name, Is.EqualTo("Cold Dishes"));
        Assert.That(result.RestaurantId, Is.EqualTo(restaurantId));
    }

    // UpdateAsync
    [Test]
    public async Task UpdateAsync_ExistingId_UpdatesFields()
    {
        var category = MakeCategory("Old");
        _repo.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _repo.Setup(r => r.UpdateAsync(category)).Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(category.Id, new UpdateCategoryRequest
        {
            Name = "New", SortOrder = 2, IsVisible = false,
        });

        Assert.That(result!.Name, Is.EqualTo("New"));
        Assert.That(result.IsVisible, Is.False);
    }

    [Test]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

        Assert.That(await _service.UpdateAsync(Guid.NewGuid(), new UpdateCategoryRequest { Name = "X" }), Is.Null);
    }

    // PatchAsync
    [Test]
    public async Task PatchAsync_TogglesVisibility()
    {
        var category = MakeCategory("C", isVisible: true);
        _repo.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _repo.Setup(r => r.UpdateAsync(category)).Returns(Task.CompletedTask);

        var result = await _service.PatchAsync(category.Id, new PatchCategoryRequest { IsVisible = false });

        Assert.That(result!.IsVisible, Is.False);
    }

    [Test]
    public async Task PatchAsync_NullFields_DoNotChangeValues()
    {
        var category = MakeCategory("C", isVisible: true, sortOrder: 5);
        _repo.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _repo.Setup(r => r.UpdateAsync(category)).Returns(Task.CompletedTask);

        var result = await _service.PatchAsync(category.Id, new PatchCategoryRequest());

        Assert.That(result!.IsVisible, Is.True);
        Assert.That(result.SortOrder, Is.EqualTo(5));
    }

    // DeleteAsync
    [Test]
    public async Task DeleteAsync_WithNoItems_ReturnsDeleted()
    {
        var category = MakeCategory("C");
        _repo.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _repo.Setup(r => r.HasActiveItemsAsync(category.Id)).ReturnsAsync(false);
        _repo.Setup(r => r.DeleteAsync(category.Id)).ReturnsAsync(true);

        Assert.That(await _service.DeleteAsync(category.Id), Is.EqualTo(DeleteCategoryResult.Deleted));
    }

    [Test]
    public async Task DeleteAsync_WithItems_ReturnsHasItems()
    {
        var category = MakeCategory("C");
        _repo.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _repo.Setup(r => r.HasActiveItemsAsync(category.Id)).ReturnsAsync(true);

        Assert.That(await _service.DeleteAsync(category.Id), Is.EqualTo(DeleteCategoryResult.HasItems));
        _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_NonExistingId_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

        Assert.That(await _service.DeleteAsync(Guid.NewGuid()), Is.EqualTo(DeleteCategoryResult.NotFound));
    }

    private static Category MakeCategory(string name, Guid? restaurantId = null, bool isVisible = true, int sortOrder = 0) =>
        new() { RestaurantId = restaurantId ?? Guid.NewGuid(), Name = name, SortOrder = sortOrder, IsVisible = isVisible };
}
