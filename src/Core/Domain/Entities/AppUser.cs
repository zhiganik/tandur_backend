using Microsoft.AspNetCore.Identity;

namespace Core.Domain.Entities;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool MustChangePassword { get; set; } = false;
    public DateTime? DateOfBirth { get; set; }

    public ICollection<Restaurant> AssignedRestaurants { get; set; } = [];
}
