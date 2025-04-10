using Microsoft.AspNetCore.Identity;

namespace StellaLearningBackend.Models;

// Inherit from IdentityUser to get standard identity fields (Id, UserName, Email, etc.)
// Add any custom properties you need for your users here.
public class ApplicationUser : IdentityUser
{
    // Example custom property:
    // public string? FullName { get; set; }
    // Add other properties like StripeCustomerId later
}