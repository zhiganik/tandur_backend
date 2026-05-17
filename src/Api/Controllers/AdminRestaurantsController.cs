using Core.Domain.Constants;
using Core.DTOs.Restaurants;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/restaurants")]
[Authorize(Policy = TandurPolicies.AdminPanel)]
public class AdminRestaurantsController(IRestaurantService restaurantService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await restaurantService.GetAdminListAsync();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var restaurant = await restaurantService.GetByIdAsync(id);
        return restaurant is null ? NotFound() : Ok(restaurant);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRestaurantRequest request)
    {
        var restaurant = await restaurantService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = restaurant.Id }, restaurant);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRestaurantRequest request)
    {
        var restaurant = await restaurantService.UpdateAsync(id, request);
        return restaurant is null ? NotFound() : Ok(restaurant);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchRestaurantRequest request)
    {
        var restaurant = await restaurantService.PatchAsync(id, request);
        return restaurant is null ? NotFound() : Ok(restaurant);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await restaurantService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
