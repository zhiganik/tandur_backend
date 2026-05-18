using Core.Domain.Constants;
using Core.DTOs.Common;
using Core.DTOs.MenuItems;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = TandurPolicies.AdminPanel)]
[Tags("Admin › Menu")]
[Produces("application/json")]
public class AdminMenuItemsController(IMenuItemService menuItemService) : ControllerBase
{
    [HttpGet("restaurants/{restaurantId:guid}/menu")]
    [SwaggerOperation(Summary = "Get full menu including unavailable items, paginated (admin view)")]
    [ProducesResponseType<MenuDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMenu(Guid restaurantId, [FromQuery] PaginationQuery query)
    {
        var menu = await menuItemService.GetAdminMenuAsync(restaurantId, query);
        return Ok(menu);
    }

    [HttpGet("menu/items/{id:guid}")]
    [SwaggerOperation(Summary = "Get a menu item by ID (admin — includes soft-deleted)")]
    [ProducesResponseType<MenuItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await menuItemService.GetAdminByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("menu/items")]
    [SwaggerOperation(Summary = "Create a new menu item")]
    [ProducesResponseType<MenuItemDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateMenuItemRequest request)
    {
        var item = await menuItemService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("menu/items/{id:guid}")]
    [SwaggerOperation(Summary = "Full update of a menu item")]
    [ProducesResponseType<MenuItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMenuItemRequest request)
    {
        var item = await menuItemService.UpdateAsync(id, request);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPatch("menu/items/{id:guid}")]
    [SwaggerOperation(Summary = "Partial update — toggle availability, change price or category")]
    [ProducesResponseType<MenuItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchMenuItemRequest request)
    {
        var item = await menuItemService.PatchAsync(id, request);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("menu/items/{id:guid}")]
    [SwaggerOperation(Summary = "Soft-delete a menu item (sets isActive = false)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await menuItemService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
