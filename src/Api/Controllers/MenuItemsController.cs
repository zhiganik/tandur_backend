using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class MenuItemsController(IMenuItemService menuItemService) : ControllerBase
{
    [HttpGet("restaurants/{restaurantId:guid}/menu")]
    public async Task<IActionResult> GetMenu(Guid restaurantId)
    {
        var menu = await menuItemService.GetMenuAsync(restaurantId);
        return Ok(menu);
    }

    [HttpGet("menu/items/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await menuItemService.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }
}
