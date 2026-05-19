using Core.DTOs.Categories;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/restaurants/{restaurantId:guid}/categories")]
[Authorize]
[Tags("Categories")]
[Produces("application/json")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List visible categories for a restaurant, ordered by sortOrder")]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetVisible(Guid restaurantId)
    {
        var categories = await categoryService.GetVisibleByRestaurantAsync(restaurantId);
        return Ok(categories);
    }
}
