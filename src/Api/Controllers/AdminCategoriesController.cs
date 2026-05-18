using Core.Domain.Constants;
using Core.Domain.Enums;
using Core.DTOs.Categories;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = TandurPolicies.AdminPanel)]
public class AdminCategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet("restaurants/{restaurantId:guid}/categories")]
    public async Task<IActionResult> GetAll(Guid restaurantId)
    {
        var categories = await categoryService.GetAllByRestaurantAsync(restaurantId);
        return Ok(categories);
    }

    [HttpPost("restaurants/{restaurantId:guid}/categories")]
    public async Task<IActionResult> Create(Guid restaurantId, [FromBody] CreateCategoryRequest request)
    {
        var category = await categoryService.CreateAsync(restaurantId, request);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpGet("categories/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var category = await categoryService.GetByIdAsync(id);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPut("categories/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await categoryService.UpdateAsync(id, request);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPatch("categories/{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchCategoryRequest request)
    {
        var category = await categoryService.PatchAsync(id, request);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await categoryService.DeleteAsync(id);
        return result switch
        {
            DeleteCategoryResult.Deleted => NoContent(),
            DeleteCategoryResult.NotFound => NotFound(),
            DeleteCategoryResult.HasItems => Conflict(new { message = "Cannot delete a category that still has active menu items." }),
            _ => StatusCode(500),
        };
    }
}
