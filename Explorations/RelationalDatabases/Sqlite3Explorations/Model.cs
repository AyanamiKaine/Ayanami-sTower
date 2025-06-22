using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
#pragma warning disable CS1591 // Add summary to documentation comment

/*
One reader context one writer context needs
"journal_mode = WAL;.
Write-Ahead Logging (WAL) mode is crucial here.
In WAL mode, readers do not block writers,
and writers do not block readers."

and a context is NOT THREAD SAFE.
*/

public class EntityContext : DbContext
{
    public DbSet<Entity> Entities { get; set; }
    public DbSet<Velocity2D> Velocity2DComponents { get; set; }
    public DbSet<Position2D> Position2DComponents { get; set; }

    public string DbPath { get; }

    public EntityContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "EntityContext.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- Configure Parent-Child Self-Referencing Relationship on Entity ---
        modelBuilder.Entity<Entity>(entity =>
        {
            entity.HasKey(e => e.EntityId); // Ensure primary key is configured

            entity
                .HasOne(e => e.Parent) // Each Entity can have one Parent
                .WithMany(p => p.Children) // Each Parent can have many Children
                .HasForeignKey(e => e.ParentId) // The foreign key is ParentId on the child Entity
                .IsRequired(false) // ParentId is optional (a top-level entity has no parent)
                .OnDelete(DeleteBehavior.ClientSetNull); // Or DeleteBehavior.Restrict.
            // ClientSetNull: If a parent is deleted, its children's ParentId will be set to NULL.
            // Restrict: Prevents deletion of a parent if it has children.
            // Avoid Cascade, as it can lead to multiple cascade paths or cycles.
        });

        /*
        modelBuilder.Entity<Entity>()
            .HasOne(e => e.Parent)
            .WithMany(v => v.Children);
        */
        modelBuilder.Entity<Position2D>(pc =>
        {
            pc.HasKey(p => p.EntityId);
        });

        modelBuilder
            .Entity<Entity>()
            .HasOne(e => e.Position2DComponent)
            .WithOne(p => p.Entity)
            .HasForeignKey<Position2D>(p => p.EntityId);

        modelBuilder.Entity<Velocity2D>(pc =>
        {
            pc.HasKey(p => p.EntityId);
        });

        modelBuilder
            .Entity<Entity>()
            .HasOne(e => e.Velocity2DComponent)
            .WithOne(v => v.Entity)
            .HasForeignKey<Velocity2D>(v => v.EntityId);
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite($"Data Source={DbPath}");
}

public class Entity
{
    public int EntityId { get; set; }
    public string Name { get; set; } = string.Empty;

    /*
    public Entity? Parent;
    public List<Entity> Children = [];
    */
    public Velocity2D? Velocity2DComponent { get; set; }
    public Position2D? Position2DComponent { get; set; }

    // --- Parent-Child Relationship ---
    public int? ParentId { get; set; } // Foreign key for the parent

    [ForeignKey("ParentId")] // Explicitly link Parent navigation property to ParentId FK
    public virtual Entity? Parent { get; set; } // Navigation property to the parent entity

    public virtual ICollection<Entity> Children { get; set; } = new List<Entity>(); // Collection of child entities

    // Using 'virtual' allows for lazy loading if enabled, though eager loading (.Include) is often preferred.


    // Cache for PropertyInfo objects to avoid repeated reflection overhead.
    // The key is the component type (T), the value is the PropertyInfo for that component on the Entity.
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> _componentPropertyCache =
        new();

    public override string ToString()
    {
        return $"ID: {EntityId}, Name: {Name}";
    }

    /*
    public T? Get<T>() where T : class // Added 'class' constraint for nullable reference type T?
    {
        if (typeof(T) == typeof(Position2D))
        {
            return Position2DComponent as T;
        }
        if (typeof(T) == typeof(Velocity2D))
        {
            return Velocity2DComponent as T;
        }
        // Optionally, handle unsupported types
        // throw new ArgumentException($"Component type {typeof(T).Name} is not supported.");
        return null;
    }
    */
    public T? Get<T>()
        where T : class
    {
        Type requestedType = typeof(T);

        // GetOrAdd ensures the reflection logic (the factory delegate)
        // runs only once per component type for the Entity class.
        PropertyInfo? propertyInfo = _componentPropertyCache.GetOrAdd(
            requestedType,
            (typeToFind) =>
            {
                // Find a public instance property on this Entity instance's type
                // whose property type is exactly the requested type.
                // 'this.GetType()' is used to support potential derived Entity classes
                // that might define their own components. If Entity is sealed or components
                // are only on the base Entity, typeof(Entity) could be used.
                return this.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => p.PropertyType == typeToFind);
            }
        );

        if (propertyInfo != null)
        {
            // Get the value of the found property from the current entity instance.
            return propertyInfo.GetValue(this) as T;
        }

        // Optionally, log or handle cases where no matching property is found.
        // Console.WriteLine($"Warning: Component of type {requestedType.Name} not found as a property on Entity.");
        return null;
    }

    public T? GetComponent<T>(int entityId, DbContext context)
        where T : class
    {
        // Find will use the primary key, which for components is EntityId.
        return context.Set<T>().Find(entityId);
    }
}

public class Position2D
{
    [Key]
    public int EntityId { get; set; }
    public virtual Entity Entity { get; set; } = null!;

    public float X { get; set; }
    public float Y { get; set; }

    public override string ToString() => $"Position(X:{X}, Y:{Y})";
}

public class Velocity2D
{
    [Key]
    public int EntityId { get; set; }
    public virtual Entity Entity { get; set; } = null!;
    public float X { get; set; }
    public float Y { get; set; }

    public override string ToString() => $"Velocity(X:{X}, Y:{Y})";
}
