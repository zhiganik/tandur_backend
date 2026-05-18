using System.Security.Claims;
using Core.Domain.Constants;
using Core.Domain.Entities;
using Core.DTOs.Users;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = TandurPolicies.AdminPanel)]
[Tags("Admin › Users")]
[Produces("application/json")]
public class UsersController(
    UserManager<AppUser> userManager,
    IRefreshTokenService refreshTokenService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List all users (PII fields are masked)")]
    [ProducesResponseType<List<UserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUsers()
    {
        var users = await userManager.Users
            .OrderBy(u => u.CreatedAt)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = MaskEmail(u.Email),
                Phone = MaskPhone(u.PhoneNumber),
                CreatedAt = u.CreatedAt,
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPut("me")]
    [SwaggerOperation(Summary = "Update the current admin's own profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await userManager.FindByIdAsync(userId!);
        if (user is null) return NotFound();

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.PhoneNumberConfirmed = true;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Delete a user account and revoke all their tokens")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        await refreshTokenService.RevokeAllForUserAsync(id);
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email)) return string.Empty;
        var at = email.IndexOf('@');
        if (at <= 1) return email;
        return email[0] + new string('*', at - 1) + email[at..];
    }

    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 7) return phone ?? string.Empty;
        return phone[..3] + new string('*', phone.Length - 6) + phone[^3..];
    }
}
