using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/restaurants/{restaurantId:guid}/categories")]
[Authorize]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetVisible(Guid restaurantId)
    {
        var categories = await categoryService.GetVisibleByRestaurantAsync(restaurantId);
        return Ok(categories);
    }
}
