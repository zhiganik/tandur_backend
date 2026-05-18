using Core.DTOs.Common;
using Core.DTOs.MenuItems;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[Tags("Menu")]
[Produces("application/json")]
public class MenuItemsController(IMenuItemService menuItemService) : ControllerBase
{
    [HttpGet("restaurants/{restaurantId:guid}/menu")]
    [SwaggerOperation(Summary = "Get full menu — visible categories + paginated available items")]
    [ProducesResponseType<MenuDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMenu(Guid restaurantId, [FromQuery] PaginationQuery query)
    {
        var menu = await menuItemService.GetMenuAsync(restaurantId, query);
        return Ok(menu);
    }

    [HttpGet("menu/items/{id:guid}")]
    [SwaggerOperation(Summary = "Get a single menu item by ID")]
    [ProducesResponseType<MenuItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await menuItemService.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }
}
