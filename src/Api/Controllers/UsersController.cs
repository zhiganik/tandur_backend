using System.Security.Claims;
using Core.Domain.Constants;
using Core.DTOs.Common;
using Core.DTOs.Users;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = TandurPolicies.AdminPanel)]
[Tags("Admin › Users")]
[Produces("application/json")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List all users with roles (PII fields are masked)")]
    [ProducesResponseType<PagedResult<UserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUsers([FromQuery] PaginationQuery query)
    {
        var result = await userService.GetPagedAsync(query);
        return Ok(result);
    }

    [HttpPut("me")]
    [SwaggerOperation(Summary = "Update the current admin's own profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Forbid();

        var result = await userService.UpdateProfileAsync(userId, request);
        return result switch
        {
            UserUpdateResult.NotFound        => NotFound(),
            UserUpdateResult.Success         => NoContent(),
            UserUpdateResult.Failed f        => BadRequest(new { errors = f.Errors }),
            _                                => StatusCode(500),
        };
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Delete a user account and revoke all their tokens")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var deleted = await userService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
