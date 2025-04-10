using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // Required for IdentityDbContext
using Microsoft.EntityFrameworkCore;
using StellaLearningBackend.Models;
// using YourBackendApiName.Models; // Uncomment later if you have custom user class

namespace StellaLearningBackend.Data;

// Inherit from IdentityDbContext to include Identity tables (Users, Roles, etc.)
// Replace IdentityUser with your custom ApplicationUser if you create one later
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Add DbSets for your other models here later (e.g., Subscriptions)
    // public DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed
        // For example, configure additional User propertiesfluent API here
    }
}