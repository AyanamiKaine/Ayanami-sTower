using Microsoft.AspNetCore.Identity;

namespace AyanamisTower.WebAPI.Models;

// Inherit from IdentityUser to get standard identity fields (Id, UserName, Email, etc.)
// Add any custom properties you need for your users here.
/// <summary>
/// Represents an application user, extending the base IdentityUser with custom properties.
/// </summary>
public class ApplicationUser : IdentityUser
{
    // Example custom property:
    // public string? FullName { get; set; }
    // Add other properties like StripeCustomerId later
}
