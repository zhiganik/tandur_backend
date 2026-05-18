using Core.DTOs.Categories;
using Core.DTOs.Common;
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
    [ProducesResponseType<PagedResult<CategoryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetVisible(Guid restaurantId, [FromQuery] PaginationQuery query)
    {
        var categories = await categoryService.GetVisibleByRestaurantAsync(restaurantId, query);
        return Ok(categories);
    }
}
