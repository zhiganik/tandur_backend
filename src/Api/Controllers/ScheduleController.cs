using Core.DTOs.Schedules;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/restaurants/{restaurantId:guid}/schedule")]
[Authorize]
[Tags("Schedule")]
[Produces("application/json")]
public class ScheduleController(IScheduleService scheduleService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Get today's open status, time slots, and upcoming closures for a restaurant")]
    [ProducesResponseType<RestaurantSchedulePublicDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPublicSchedule(Guid restaurantId)
    {
        return Ok(await scheduleService.GetPublicScheduleAsync(restaurantId));
    }
}
