using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/restaurants")]
[Authorize]
public class RestaurantsController(IRestaurantService restaurantService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] double? lat, [FromQuery] double? lng)
    {
        var restaurants = await restaurantService.GetAllAsync(lat, lng);
        return Ok(restaurants);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var restaurant = await restaurantService.GetByIdAsync(id);
        return restaurant is null ? NotFound() : Ok(restaurant);
    }
}