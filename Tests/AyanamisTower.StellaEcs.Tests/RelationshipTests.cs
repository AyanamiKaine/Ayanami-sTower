using Xunit;
using System.Linq;
using System.Collections.Generic;

namespace AyanamisTower.StellaEcs.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// This test class demonstrates how to model and query data relationships
/// (One-to-One, One-to-Many, Many-to-Many) in StellaEcs, drawing parallels
/// to concepts from relational databases like SQL.
/// </summary>
public class RelationshipTests
{
    private readonly World _world;

    public RelationshipTests()
    {
        _world = new World();
    }

    /// <summary>
    /// A One-to-One relationship is like having a foreign key with a unique constraint.
    /// A Player has exactly one Inventory. An Inventory belongs to exactly one Player.
    /// We model this by creating a component on each entity that points to the other.
    /// </summary>
    [Fact]
    public void OneToOne_Relationship_ShouldLinkTwoEntities()
    {
        // Arrange: Create a Player and their Inventory
        var player = _world.CreateEntity();
        player.Set(new PlayerInfo { Name = "Hero" });

        var inventory = _world.CreateEntity();
        inventory.Set(new InventoryData { Capacity = 10 });

        // Act: Create the relationship by adding components that store the other entity's handle.
        // This is like setting the foreign keys on both tables.
        player.Set(new HasInventory { InventoryEntity = inventory });
        inventory.Set(new BelongsToOwner { OwnerEntity = player });

        // Assert: We can now navigate the relationship from either direction.

        // 1. From the Player, find their Inventory.
        Assert.True(player.Has<HasInventory>());
        var foundInventoryEntity = player.GetCopy<HasInventory>().InventoryEntity;
        Assert.Equal(inventory, foundInventoryEntity);
        Assert.Equal(10, foundInventoryEntity.GetCopy<InventoryData>().Capacity); // Verify we got the right entity

        // 2. From the Inventory, find its Owner.
        Assert.True(inventory.Has<BelongsToOwner>());
        var foundPlayerEntity = inventory.GetCopy<BelongsToOwner>().OwnerEntity;
        Assert.Equal(player, foundPlayerEntity);
        Assert.Equal("Hero", foundPlayerEntity.GetCopy<PlayerInfo>().Name); // Verify we got the right entity
    }

    /// <summary>
    /// A One-to-Many relationship is the most common type. A Guild has many Members.
    /// The most efficient way to model this in an ECS is to place a component on the "Many" side
    /// that points to the "One". This avoids managing a dynamic list of entities on the "One" side.
    ///
    /// SQL Equivalent: The `Members` table would have a `guild_id` foreign key.
    /// </summary>
    [Fact]
    public void OneToMany_Relationship_ShouldLinkParentToChildren()
    {
        // Arrange: Create the "One" side of the relationship (the Guild).
        var warriorsGuild = _world.CreateEntity();
        warriorsGuild.Set(new GuildInfo { Name = "The Warriors" });

        var magesGuild = _world.CreateEntity();
        magesGuild.Set(new GuildInfo { Name = "The Mages" });

        // Arrange: Create the "Many" side (the Members) and link them to their guild.
        var member1 = _world.CreateEntity();
        var member2 = _world.CreateEntity();
        var member3 = _world.CreateEntity();

        // Link members 1 and 3 to the Warriors Guild.
        member1.Set(new GuildMembership { GuildEntity = warriorsGuild });
        member3.Set(new GuildMembership { GuildEntity = warriorsGuild });

        // Link member 2 to the Mages Guild.
        member2.Set(new GuildMembership { GuildEntity = magesGuild });

        // Act: To find all members of a specific guild, we query for all entities
        // that have the GuildMembership component where the GuildEntity matches our target.
        // This is the ECS equivalent of `SELECT * FROM Members WHERE guild_id = @warriorsGuildId`.
        var allMembers = _world.Query(typeof(GuildMembership));

        var warriorMembers = allMembers
            .Where(m => m.GetCopy<GuildMembership>().GuildEntity == warriorsGuild)
            .ToList();

        var mageMembers = allMembers
            .Where(m => m.GetCopy<GuildMembership>().GuildEntity == magesGuild)
            .ToList();


        // Assert: Check that our queries returned the correct members.
        Assert.Equal(2, warriorMembers.Count);
        Assert.Contains(member1, warriorMembers);
        Assert.Contains(member3, warriorMembers);

        Assert.Single(mageMembers);
        Assert.Contains(member2, mageMembers);
    }

    /// <summary>
    /// A Many-to-Many relationship requires a "join table" in SQL. In an ECS, we
    /// achieve the same result by creating a dedicated "Relation Entity".
    ///
    /// Scenario: A Student can enroll in many Courses. A Course can have many Students.
    /// The "Enrollment" is the relationship itself.
    ///
    /// SQL Equivalent: A `Enrollments` table with `student_id` and `course_id` foreign keys.
    /// </summary>
    [Fact]
    public void ManyToMany_Relationship_ShouldLinkEntitiesViaRelationEntity()
    {
        // Arrange: Create the entities that will be related.
        var studentAlice = _world.CreateEntity();
        studentAlice.Set(new StudentInfo { Name = "Alice" });

        var studentBob = _world.CreateEntity();
        studentBob.Set(new StudentInfo { Name = "Bob" });

        var courseMath = _world.CreateEntity();
        courseMath.Set(new CourseInfo { Name = "Math 101" });

        var courseHistory = _world.CreateEntity();
        courseHistory.Set(new CourseInfo { Name = "History 101" });

        // Act: Create the "Relation Entities" to represent the M:N links.
        // Each relation entity holds the two "foreign keys" and can even hold data
        // about the relationship itself (like a grade).

        // Alice enrolls in Math 101 with a grade of 'A'.
        var enrollment1 = _world.CreateEntity();
        enrollment1.Set(new EnrolledStudent { StudentEntity = studentAlice });
        enrollment1.Set(new EnrolledInCourse { CourseEntity = courseMath });
        enrollment1.Set(new Grade { Value = 'A' });

        // Alice also enrolls in History 101.
        var enrollment2 = _world.CreateEntity();
        enrollment2.Set(new EnrolledStudent { StudentEntity = studentAlice });
        enrollment2.Set(new EnrolledInCourse { CourseEntity = courseHistory });
        // No grade assigned yet.

        // Bob enrolls in Math 101.
        var enrollment3 = _world.CreateEntity();
        enrollment3.Set(new EnrolledStudent { StudentEntity = studentBob });
        enrollment3.Set(new EnrolledInCourse { CourseEntity = courseMath });
        enrollment3.Set(new Grade { Value = 'C' });

        // Assert: Now we can perform "joins" by querying the relation entities.

        // 1. Find all courses Alice is enrolled in.
        // SQL: SELECT c.* FROM Courses c JOIN Enrollments e ON c.id = e.course_id WHERE e.student_id = @aliceId
        var allEnrollments = _world.Query(typeof(EnrolledStudent), typeof(EnrolledInCourse));

        var alicesCourseEntities = allEnrollments
            .Where(e => e.GetCopy<EnrolledStudent>().StudentEntity == studentAlice)
            .Select(e => e.GetCopy<EnrolledInCourse>().CourseEntity)
            .ToList();

        Assert.Equal(2, alicesCourseEntities.Count);
        Assert.Contains(courseMath, alicesCourseEntities);
        Assert.Contains(courseHistory, alicesCourseEntities);

        // 2. Find all students in Math 101.
        // SQL: SELECT s.* FROM Students s JOIN Enrollments e ON s.id = e.student_id WHERE e.course_id = @mathId
        var mathStudentEntities = allEnrollments
            .Where(e => e.GetCopy<EnrolledInCourse>().CourseEntity == courseMath)
            .Select(e => e.GetCopy<EnrolledStudent>().StudentEntity)
            .ToList();

        Assert.Equal(2, mathStudentEntities.Count);
        Assert.Contains(studentAlice, mathStudentEntities);
        Assert.Contains(studentBob, mathStudentEntities);

        // 3. Find Alice's grade in Math 101.
        var alicesMathEnrollment = allEnrollments.First(e =>
            e.GetCopy<EnrolledStudent>().StudentEntity == studentAlice &&
            e.GetCopy<EnrolledInCourse>().CourseEntity == courseMath);

        Assert.True(alicesMathEnrollment.Has<Grade>());
        Assert.Equal('A', alicesMathEnrollment.GetCopy<Grade>().Value);
    }
}
