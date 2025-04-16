using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StellaLearningBackend.Models; // Your Models namespace

namespace StellaLearningBackend.Data // Your Data namespace
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        // DbSet for your File Metadata
        public DbSet<FileMetadata> FileMetadataEntries
        {
            get; set;
        } // Choose a suitable name
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; } = null!;
        public DbSet<ApplicationVersion> ApplicationVersions { get; set; } = null!;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure the relationship between ApplicationUser and FileMetadata
            builder.Entity<FileMetadata>()
                .HasOne(fm => fm.User) // FileMetadata has one User
                .WithMany() // ApplicationUser can have many FileMetadata entries (or define a collection property on ApplicationUser if needed)
                .HasForeignKey(fm => fm.UserId) // The foreign key is UserId
                .OnDelete(DeleteBehavior.Cascade); // Optional: Define delete behavior (e.g., delete metadata if user is deleted)

            // You can add indexes here for performance, e.g., on UserId
            builder.Entity<FileMetadata>()
                .HasIndex(fm => fm.UserId);

            builder.Entity<FileMetadata>()
                .HasIndex(fm => fm.StoredFileName)
                .IsUnique(); // Ensure stored filenames are unique (within the entire system, or adjust if needed)

            builder.Entity<UserRefreshToken>()
                            .HasIndex(rt => rt.Token)
                            .IsUnique(); // Refresh tokens should ideally be unique

            builder.Entity<UserRefreshToken>()
                            .HasOne(rt => rt.User)
                            .WithMany() // Assuming ApplicationUser doesn't have a collection navigation property back to tokens
                            .HasForeignKey(rt => rt.UserId)
                            .IsRequired();

            builder.Entity<ApplicationVersion>(entity =>
            {
                // Index for quick lookup of the latest version per platform/arch
                entity.HasIndex(v => new { v.IsLatest, v.Platform, v.Architecture })
                      .HasFilter("[IsLatest] = 1"); // SQL Server specific filter for efficiency

                // Index for finding by version number
                entity.HasIndex(v => v.VersionNumber);

                // Index for finding by stored file name (should be unique)
                entity.HasIndex(v => v.StoredFileName).IsUnique();

                // Configure the optional relationship to the uploading user
                entity.HasOne(v => v.UploadedByUser)
                      .WithMany() // Assuming ApplicationUser doesn't need a collection of versions uploaded
                      .HasForeignKey(v => v.UploadedByUserId)
                      .OnDelete(DeleteBehavior.SetNull); // If user is deleted, set UploadedByUserId to null

                // You might want a unique constraint on VersionNumber + Platform + Architecture
                entity.HasIndex(v => new { v.VersionNumber, v.Platform, v.Architecture }).IsUnique();
            });
        }
    }
}
