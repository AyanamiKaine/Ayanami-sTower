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
        }
    }
}
