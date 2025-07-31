import { test, expect, describe, beforeEach } from "bun:test";
import { World } from "../src/World";

// === Relationship Components ===

/**
 * A one-to-one relationship component.
 * Indicates that the entity with this component follows another entity.
 * Also stores data about the relationship (the follow distance).
 */
class Follows {
    constructor(targetEntityId, distance) {
        this.target = targetEntityId;
        this.distance = distance;
    }
}

/**
 * A component used on a "relationship entity" to establish a
 * one-to-many link.
 */
class ParentLink {
    constructor(parentId, childId) {
        this.parent = parentId;
        this.child = childId;
    }
}

// === Standard Components ===
class PlayerTag {}
class PetTag {}
class NpcTag {}

describe("Entity Relationships", () => {
    let world;
    let player, pet, npc;

    beforeEach(() => {
        world = new World();

        // Create the main entities
        player = world.createEntity();
        player.set(new PlayerTag());

        pet = world.createEntity();
        pet.set(new PetTag());

        npc = world.createEntity();
        npc.set(new NpcTag());

        // === Setup Relationships ===

        // 1. One-to-One: The pet follows the player
        // We add the relationship component directly to the 'pet' entity.
        pet.set(new Follows(player.id, 5));

        // 2. One-to-Many: The player is the parent of the pet AND the npc.
        // We create dedicated "relationship entities".
        const playerIsParentOfPet = world.createEntity();
        playerIsParentOfPet.set(new ParentLink(player.id, pet.id));

        const playerIsParentOfNpc = world.createEntity();
        playerIsParentOfNpc.set(new ParentLink(player.id, npc.id));
    });

    test("should query a one-to-one relationship with data", () => {
        // System to find who is following the player
        const followersQuery = world
            .query()
            .with(Follows)
            .where((res) => res.follows.target === player.id);

        const followers = [...followersQuery];

        // Assert that only the pet is following the player
        expect(followers.length).toBe(1);
        expect(followers[0].entity.id).toBe(pet.id);

        // Assert that we can access the data on the relationship
        expect(followers[0].follows.distance).toBe(5);
    });

    test("should query a one-to-many relationship", () => {
        // System to find all children of the player
        const childrenQuery = world
            .query()
            .with(ParentLink)
            .where((res) => res.parentlink.parent === player.id);

        // The query gives us the relationship entities. We can map them to get the actual children.
        const children = [...childrenQuery].map((res) => {
            const childId = res.parentlink.child;
            return world.entities[childId];
        });

        // Assert that we found two children
        expect(children.length).toBe(2);

        // Assert that the children are the pet and the npc
        const hasPet = children.some((child) => child.get(PetTag));
        const hasNpc = children.some((child) => child.get(NpcTag));
        expect(hasPet).toBe(true);
        expect(hasNpc).toBe(true);
    });

    test("should be able to find the parent of an entity", () => {
        // System to find the parent of the pet
        const parentQuery = world
            .query()
            .with(ParentLink)
            .where((res) => res.parentlink.child === pet.id);

        const parentLinks = [...parentQuery];

        expect(parentLinks.length).toBe(1);
        const parentId = parentLinks[0].parentlink.parent;

        expect(parentId).toBe(player.id);
    });
});
