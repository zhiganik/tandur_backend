using Core.Domain.Constants;
using Core.Domain.Enums;
using Core.DTOs.Categories;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = TandurPolicies.AdminPanel)]
[Tags("Admin › Categories")]
[Produces("application/json")]
public class AdminCategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet("restaurants/{restaurantId:guid}/categories")]
    [SwaggerOperation(Summary = "List all categories for a restaurant, including hidden ones")]
    [ProducesResponseType<List<CategoryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(Guid restaurantId)
    {
        var categories = await categoryService.GetAllByRestaurantAsync(restaurantId);
        return Ok(categories);
    }

    [HttpPost("restaurants/{restaurantId:guid}/categories")]
    [SwaggerOperation(Summary = "Create a category for a restaurant")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(Guid restaurantId, [FromBody] CreateCategoryRequest request)
    {
        var category = await categoryService.CreateAsync(restaurantId, request);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpGet("categories/{id:guid}")]
    [SwaggerOperation(Summary = "Get a category by ID")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var category = await categoryService.GetByIdAsync(id);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPut("categories/{id:guid}")]
    [SwaggerOperation(Summary = "Full update of a category")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await categoryService.UpdateAsync(id, request);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPatch("categories/{id:guid}")]
    [SwaggerOperation(Summary = "Partial update — toggle visibility or reorder")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchCategoryRequest request)
    {
        var category = await categoryService.PatchAsync(id, request);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpDelete("categories/{id:guid}")]
    [SwaggerOperation(Summary = "Hard-delete a category — fails with 409 if it still has active items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await categoryService.DeleteAsync(id);
        return result switch
        {
            DeleteCategoryResult.Deleted   => NoContent(),
            DeleteCategoryResult.NotFound  => NotFound(),
            DeleteCategoryResult.HasItems  => Conflict(new { message = "Cannot delete a category that still has active menu items." }),
            _                              => StatusCode(500),
        };
    }
}
