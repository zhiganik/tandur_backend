using Core.Domain.Constants;
using Core.DTOs.Restaurants;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/restaurants")]
[Authorize(Policy = TandurPolicies.AdminPanel)]
[Tags("Admin › Restaurants")]
[Produces("application/json")]
public class AdminRestaurantsController(IRestaurantService restaurantService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List all restaurants including inactive ones")]
    [ProducesResponseType<List<RestaurantDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var result = await restaurantService.GetAdminListAsync();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get a restaurant by ID")]
    [ProducesResponseType<RestaurantDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var restaurant = await restaurantService.GetByIdAsync(id);
        return restaurant is null ? NotFound() : Ok(restaurant);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create a new restaurant")]
    [ProducesResponseType<RestaurantDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateRestaurantRequest request)
    {
        var restaurant = await restaurantService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = restaurant.Id }, restaurant);
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Full update of a restaurant")]
    [ProducesResponseType<RestaurantDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRestaurantRequest request)
    {
        var restaurant = await restaurantService.UpdateAsync(id, request);
        return restaurant is null ? NotFound() : Ok(restaurant);
    }

    [HttpPatch("{id:guid}")]
    [SwaggerOperation(Summary = "Partial update — e.g. toggle active status")]
    [ProducesResponseType<RestaurantDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchRestaurantRequest request)
    {
        var restaurant = await restaurantService.PatchAsync(id, request);
        return restaurant is null ? NotFound() : Ok(restaurant);
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Soft-delete a restaurant (sets isActive = false)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await restaurantService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
