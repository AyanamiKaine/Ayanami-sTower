using AyanamisTower.WebAPI.Models; // Your Models namespace
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AyanamisTower.WebAPI.Data;

/// <summary>
/// Represents the database context for the application, managing entities like users, file metadata, refresh tokens, and application versions.
/// It inherits from <see cref="IdentityDbContext{TUser}"/> to integrate with ASP.NET Core Identity.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>
    /// Gets or sets the DbSet for managing <see cref="FileMetadata"/> entities.
    /// Represents the collection of file metadata entries stored in the database.
    /// </summary>
    public DbSet<FileMetadata> FileMetadataEntries { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for managing <see cref="UserRefreshToken"/> entities.
    /// Represents the collection of user refresh tokens stored in the database.
    /// </summary>
    public DbSet<UserRefreshToken> UserRefreshTokens { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DbSet for managing <see cref="ApplicationVersion"/> entities.
    /// Represents the collection of application version records stored in the database.
    /// </summary>
    public DbSet<ApplicationVersion> ApplicationVersions { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    /// <summary>
    /// Configures the schema needed for the identity framework and defines relationships, indexes, and constraints for the application's custom entities.
    /// This method is called by the framework during the model creation process.
    /// </summary>
    /// <param name="builder">The builder being used to construct the model for this context. Databases providers usually implement extension methods on this object that allow you to configure aspects of the model that are specific to a given database.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure the relationship between ApplicationUser and FileMetadata
        builder
            .Entity<FileMetadata>()
            .HasOne(fm => fm.User) // FileMetadata has one User
            .WithMany() // ApplicationUser can have many FileMetadata entries (or define a collection property on ApplicationUser if needed)
            .HasForeignKey(fm => fm.UserId) // The foreign key is UserId
            .OnDelete(DeleteBehavior.Cascade); // Optional: Define delete behavior (e.g., delete metadata if user is deleted)

        // You can add indexes here for performance, e.g., on UserId
        builder.Entity<FileMetadata>().HasIndex(fm => fm.UserId);

        builder.Entity<FileMetadata>().HasIndex(fm => fm.StoredFileName).IsUnique(); // Ensure stored filenames are unique (within the entire system, or adjust if needed)

        builder.Entity<UserRefreshToken>().HasIndex(rt => rt.Token).IsUnique(); // Refresh tokens should ideally be unique

        builder
            .Entity<UserRefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany() // Assuming ApplicationUser doesn't have a collection navigation property back to tokens
            .HasForeignKey(rt => rt.UserId)
            .IsRequired();

        builder.Entity<ApplicationVersion>(entity =>
        {
            // Index for quick lookup of the latest version per platform/arch
            entity
                .HasIndex(v => new
                {
                    v.IsLatest,
                    v.Platform,
                    v.Architecture,
                })
                .HasFilter("[IsLatest] = 1"); // SQL Server specific filter for efficiency

            // Index for finding by version number
            entity.HasIndex(v => v.VersionNumber);

            // Index for finding by stored file name (should be unique)
            entity.HasIndex(v => v.StoredFileName).IsUnique();

            // Configure the optional relationship to the uploading user
            entity
                .HasOne(v => v.UploadedByUser)
                .WithMany() // Assuming ApplicationUser doesn't need a collection of versions uploaded
                .HasForeignKey(v => v.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull); // If user is deleted, set UploadedByUserId to null

            // You might want a unique constraint on VersionNumber + Platform + Architecture
            entity
                .HasIndex(v => new
                {
                    v.VersionNumber,
                    v.Platform,
                    v.Architecture,
                })
                .IsUnique();
        });
    }
}
