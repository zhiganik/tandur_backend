using Core.Domain.Constants;
using Core.DTOs.MenuItems;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = TandurPolicies.AdminPanel)]
public class AdminMenuItemsController(IMenuItemService menuItemService) : ControllerBase
{
    [HttpGet("restaurants/{restaurantId:guid}/menu")]
    public async Task<IActionResult> GetMenu(Guid restaurantId)
    {
        var menu = await menuItemService.GetAdminMenuAsync(restaurantId);
        return Ok(menu);
    }

    [HttpGet("menu/items/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await menuItemService.GetAdminByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("menu/items")]
    public async Task<IActionResult> Create([FromBody] CreateMenuItemRequest request)
    {
        var item = await menuItemService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("menu/items/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMenuItemRequest request)
    {
        var item = await menuItemService.UpdateAsync(id, request);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPatch("menu/items/{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchMenuItemRequest request)
    {
        var item = await menuItemService.PatchAsync(id, request);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("menu/items/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await menuItemService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
