using System.Security.Claims;
using Api.Filters;
using Core.Domain.Constants;
using Core.DTOs.Schedules;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/restaurants/{restaurantId:guid}")]
[Authorize(Policy = TandurPolicies.AdminPanel)]
[BlockScopedToken]
[ServiceFilter(typeof(RequireRestaurantAccessAttribute))]
[Tags("Admin › Schedule")]
[Produces("application/json")]
public class AdminScheduleController(IScheduleService scheduleService) : ControllerBase
{
    [HttpGet("schedule")]
    [SwaggerOperation(Summary = "Get full weekly schedule for a restaurant")]
    [ProducesResponseType<IReadOnlyList<ScheduleDayDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSchedule(Guid restaurantId)
    {
        return Ok(await scheduleService.GetScheduleAsync(restaurantId));
    }

    [HttpPut("schedule")]
    [SwaggerOperation(Summary = "Replace the full weekly schedule (all 7 days required)")]
    [ProducesResponseType<IReadOnlyList<ScheduleDayDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateFullSchedule(Guid restaurantId, [FromBody] UpdateFullScheduleRequest request)
    {
        return Ok(await scheduleService.UpdateFullScheduleAsync(restaurantId, request));
    }

    [HttpPatch("schedule/{dayOfWeek}")]
    [SwaggerOperation(Summary = "Update a single day of the weekly schedule")]
    [ProducesResponseType<ScheduleDayDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateScheduleDay(Guid restaurantId, DayOfWeek dayOfWeek, [FromBody] UpdateScheduleDayRequest request)
    {
        var result = await scheduleService.UpdateScheduleDayAsync(restaurantId, dayOfWeek, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("overrides")]
    [SwaggerOperation(Summary = "List all schedule overrides for a restaurant")]
    [ProducesResponseType<IReadOnlyList<ScheduleOverrideDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOverrides(Guid restaurantId)
    {
        return Ok(await scheduleService.GetOverridesAsync(restaurantId));
    }

    [HttpPost("overrides")]
    [SwaggerOperation(Summary = "Create a schedule override for a specific date (planned day off or custom hours)")]
    [ProducesResponseType<ScheduleOverrideDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateOverride(Guid restaurantId, [FromBody] CreateOverrideRequest request)
    {
        var result = await scheduleService.CreateOverrideAsync(restaurantId, request, AdminId);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("overrides/{overrideId:guid}")]
    [SwaggerOperation(Summary = "Update a schedule override")]
    [ProducesResponseType<ScheduleOverrideDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOverride(Guid restaurantId, Guid overrideId, [FromBody] UpdateOverrideRequest request)
    {
        var result = await scheduleService.UpdateOverrideAsync(overrideId, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("overrides/{overrideId:guid}")]
    [SwaggerOperation(Summary = "Delete a schedule override")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOverride(Guid restaurantId, Guid overrideId)
    {
        return await scheduleService.DeleteOverrideAsync(overrideId) ? NoContent() : NotFound();
    }

    [HttpPost("close")]
    [SwaggerOperation(Summary = "Emergency close — creates an instant override for today (empty slots = all day, or specify reopening times)")]
    [ProducesResponseType<ScheduleOverrideDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CloseNow(Guid restaurantId, [FromBody] InstantCloseRequest request)
    {
        return Ok(await scheduleService.CloseNowAsync(restaurantId, request, AdminId));
    }

    [HttpDelete("close")]
    [SwaggerOperation(Summary = "Reopen now — removes today's instant closure override")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReopenNow(Guid restaurantId)
    {
        return await scheduleService.ReopenNowAsync(restaurantId) ? NoContent() : NotFound();
    }

    private string AdminId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
