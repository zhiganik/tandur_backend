using Core.DTOs.Common;
using Core.DTOs.Restaurants;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/restaurants")]
[Authorize]
[Tags("Restaurants")]
[Produces("application/json")]
public class RestaurantsController(IRestaurantService restaurantService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List active restaurants, optionally sorted by distance")]
    [ProducesResponseType<PagedResult<RestaurantDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] double? lat, [FromQuery] double? lng, [FromQuery] PaginationQuery query)
    {
        var restaurants = await restaurantService.GetAllAsync(lat, lng, query);
        return Ok(restaurants);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get restaurant details by ID")]
    [ProducesResponseType<RestaurantDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var restaurant = await restaurantService.GetByIdAsync(id);
        return restaurant is null ? NotFound() : Ok(restaurant);
    }
}
