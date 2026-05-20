using System.Security.Claims;
using Core.Domain.Constants;
using Core.Domain.Entities;
using Core.DTOs.Common;
using Core.DTOs.Users;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = TandurPolicies.AdminPanel)]
[Tags("Admin › Users")]
[Produces("application/json")]
public class UsersController(
    IUserService userService,
    IRestaurantService restaurantService,
    IUserRepository userRepository,
    UserManager<AppUser> userManager,
    IPasswordResetSender passwordResetSender,
    ILogger<UsersController> logger) : ControllerBase
{
    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    [HttpGet]
    [Authorize(Policy = TandurPolicies.SuperAdminOnly)]
    [SwaggerOperation(Summary = "List users — search by email/phone/ID, filter by role or restaurant, sort by registration date (SuperAdmin only)")]
    [ProducesResponseType<PagedResult<UserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers([FromQuery] UserQuery query)
    {
        var result = await userService.GetPagedAsync(query, maskPii: false);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = TandurPolicies.SuperAdminOnly)]
    [SwaggerOperation(Summary = "Get a user by ID — PII unmasked, includes restaurants (SuperAdmin only)")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await userService.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = TandurPolicies.SuperAdminOnly)]
    [SwaggerOperation(Summary = "Delete a user account and revoke all their tokens (SuperAdmin only)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var deleted = await userService.DeleteAsync(id);
        if (!deleted) return NotFound();

        logger.LogInformation("User {TargetUserId} deleted by {ActorId}", id, ActorId);
        return NoContent();
    }

    [HttpPost("{adminId}/restaurants/{restaurantId:guid}")]
    [Authorize(Policy = TandurPolicies.SuperAdminOnly)]
    [SwaggerOperation(Summary = "Attach a restaurant to an admin (SuperAdmin only)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRestaurant(string adminId, Guid restaurantId)
    {
        var roles = await userRepository.GetRolesAsync(adminId);
        if (roles.Count == 0) return NotFound();
        if (roles.Contains(TandurRoles.SuperAdmin))
            return BadRequest(new { message = "SuperAdmin already has access to all restaurants." });
        if (!roles.Contains(TandurRoles.Admin))
            return BadRequest(new { message = "Target user must have the Admin role." });

        var assigned = await restaurantService.AssignToAdminAsync(restaurantId, adminId);
        if (!assigned) return NotFound();

        logger.LogInformation("Restaurant {RestaurantId} assigned to admin {AdminId} by {ActorId}",
            restaurantId, adminId, ActorId);
        return NoContent();
    }

    [HttpDelete("{adminId}/restaurants/{restaurantId:guid}")]
    [Authorize(Policy = TandurPolicies.SuperAdminOnly)]
    [SwaggerOperation(Summary = "Detach a restaurant from an admin (SuperAdmin only)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnassignRestaurant(string adminId, Guid restaurantId)
    {
        var roles = await userRepository.GetRolesAsync(adminId);
        if (roles.Contains(TandurRoles.SuperAdmin))
            return BadRequest(new { message = "SuperAdmin already has access to all restaurants." });

        var unassigned = await restaurantService.UnassignFromAdminAsync(restaurantId, adminId);
        if (!unassigned) return NotFound();

        logger.LogInformation("Restaurant {RestaurantId} unassigned from admin {AdminId} by {ActorId}",
            restaurantId, adminId, ActorId);
        return NoContent();
    }

    [HttpPost("{id}/password/reset")]
    [Authorize(Policy = TandurPolicies.SuperAdminOnly)]
    [SwaggerOperation(Summary = "Send a password reset email to a target admin (SuperAdmin only)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendAdminPasswordReset(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var roles = await userRepository.GetRolesAsync(id);
        if (!roles.Contains(TandurRoles.Admin))
            return BadRequest(new { message = "Target user must have the Admin role." });

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await passwordResetSender.SendAsync(user.Email!, token);

        logger.LogInformation("Password reset sent for admin {TargetUserId} by {ActorId}", id, ActorId);
        return Ok(new { message = "Password reset link sent to admin's email." });
    }
}
