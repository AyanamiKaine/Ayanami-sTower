import { test, expect, describe } from "bun:test";
import { SparsedSet } from "../src/SparsedSet";
import { ComponentStorage } from "../src/ComponentStorage";
import { Entity } from "../src/Entity";

class Position2D {
    constructor(x, y) {
        this.X = x;
        this.Y = y;
    }
}

describe("Component Storage", () => {
    test("Adding components to entity", () => {
        let position2DStorage = new ComponentStorage();

        let entity = new Entity();
        entity.id = 0;

        position2DStorage.set(entity, new Position2D(10, 20));

        expect(position2DStorage.get(entity).X).toBe(10);
        expect(position2DStorage.get(entity).Y).toBe(20);
        expect(position2DStorage.has(entity)).toBe(true);
    });

    test("Adding components to multiple entity", () => {
        let position2DStorage = new ComponentStorage();

        let entityA = new Entity();
        entityA.id = 0;

        let entityB = new Entity();
        entityB.id = 5;

        let entityC = new Entity();
        entityC.id = 8;

        position2DStorage.set(entityA, new Position2D(10, 20));

        expect(position2DStorage.get(entityA).X).toBe(10);
        expect(position2DStorage.get(entityA).Y).toBe(20);
        expect(position2DStorage.has(entityA)).toBe(true);

        position2DStorage.set(entityB, new Position2D(5, -1));

        expect(position2DStorage.get(entityB).X).toBe(5);
        expect(position2DStorage.get(entityB).Y).toBe(-1);
        expect(position2DStorage.has(entityB)).toBe(true);

        expect(position2DStorage.has(entityC)).toBe(false);
    });

    test("removing components to entity", () => {
        let position2DStorage = new ComponentStorage();

        let entity = new Entity();
        entity.id = 0;

        position2DStorage.set(entity, new Position2D(10, 20));

        expect(position2DStorage.get(entity).X).toBe(10);
        expect(position2DStorage.get(entity).Y).toBe(20);

        position2DStorage.remove(entity);
        expect(position2DStorage.has(entity)).toBe(false);
    });

    test("modifying components to entity", () => {
        let position2DStorage = new ComponentStorage();

        let entity = new Entity();
        entity.id = 0;

        position2DStorage.set(entity, new Position2D(10, 20));

        let pos2D = position2DStorage.get(entity);

        pos2D.X = 300;
        pos2D.Y = 52;

        expect(position2DStorage.get(entity).X).toBe(300);
        expect(position2DStorage.get(entity).Y).toBe(52);
    });
});
describe("Enumeration functionality", () => {
    test("Symbol.iterator - for...of loop", () => {
        let position2DStorage = new ComponentStorage();

        let entityA = new Entity();
        entityA.id = 0;
        let entityB = new Entity();
        entityB.id = 1;
        let entityC = new Entity();
        entityC.id = 2;

        position2DStorage.set(entityA, new Position2D(10, 20));
        position2DStorage.set(entityB, new Position2D(30, 40));
        position2DStorage.set(entityC, new Position2D(50, 60));

        const collected = [];
        for (const { entityId, component } of position2DStorage) {
            collected.push({ entityId, component });
        }

        expect(collected.length).toBe(3);
        expect(collected[0].entityId).toBe(0);
        expect(collected[0].component.X).toBe(10);
        expect(collected[0].component.Y).toBe(20);
        expect(collected[1].entityId).toBe(1);
        expect(collected[2].entityId).toBe(2);
    });

    test("components() iterator", () => {
        let position2DStorage = new ComponentStorage();

        let entityA = new Entity();
        entityA.id = 0;
        let entityB = new Entity();
        entityB.id = 1;

        position2DStorage.set(entityA, new Position2D(10, 20));
        position2DStorage.set(entityB, new Position2D(30, 40));

        const components = [];
        for (const component of position2DStorage.components()) {
            components.push(component);
        }

        expect(components.length).toBe(2);
        expect(components[0].X).toBe(10);
        expect(components[0].Y).toBe(20);
        expect(components[1].X).toBe(30);
        expect(components[1].Y).toBe(40);
    });

    test("entityIds() iterator", () => {
        let position2DStorage = new ComponentStorage();

        let entityA = new Entity();
        entityA.id = 7;
        let entityB = new Entity();
        entityB.id = 13;

        position2DStorage.set(entityA, new Position2D(10, 20));
        position2DStorage.set(entityB, new Position2D(30, 40));

        const entityIds = [];
        for (const entityId of position2DStorage.entityIds()) {
            entityIds.push(entityId);
        }

        expect(entityIds.length).toBe(2);
        expect(entityIds[0]).toBe(7);
        expect(entityIds[1]).toBe(13);
    });

    test("entries() iterator", () => {
        let position2DStorage = new ComponentStorage();

        let entityA = new Entity();
        entityA.id = 0;
        let entityB = new Entity();
        entityB.id = 1;

        position2DStorage.set(entityA, new Position2D(10, 20));
        position2DStorage.set(entityB, new Position2D(30, 40));

        const entries = [];
        for (const [entityId, component] of position2DStorage.entries()) {
            entries.push([entityId, component]);
        }

        expect(entries.length).toBe(2);
        expect(entries[0][0]).toBe(0);
        expect(entries[0][1].X).toBe(10);
        expect(entries[1][0]).toBe(1);
        expect(entries[1][1].X).toBe(30);
    });

    test("forEach method", () => {
        let position2DStorage = new ComponentStorage();

        let entityA = new Entity();
        entityA.id = 0;
        let entityB = new Entity();
        entityB.id = 1;

        position2DStorage.set(entityA, new Position2D(10, 20));
        position2DStorage.set(entityB, new Position2D(30, 40));

        const collected = [];
        position2DStorage.forEach((component, entityId, storage) => {
            collected.push({ entityId, component, storage });
        });

        expect(collected.length).toBe(2);
        expect(collected[0].entityId).toBe(0);
        expect(collected[0].component.X).toBe(10);
        expect(collected[0].storage).toBe(position2DStorage);
        expect(collected[1].entityId).toBe(1);
        expect(collected[1].component.X).toBe(30);
    });

    test("map method", () => {
        let position2DStorage = new ComponentStorage();

        let entityA = new Entity();
        entityA.id = 0;
        let entityB = new Entity();
        entityB.id = 1;

        position2DStorage.set(entityA, new Position2D(10, 20));
        position2DStorage.set(entityB, new Position2D(30, 40));

        const distances = position2DStorage.map((component, entityId) => {
            return Math.sqrt(
                component.X * component.X + component.Y * component.Y
            );
        });

        expect(distances.length).toBe(2);
        expect(distances[0]).toBeCloseTo(22.36, 2); // sqrt(10² + 20²)
        expect(distances[1]).toBeCloseTo(50, 2); // sqrt(30² + 40²)
    });

    test("filter method", () => {
        let position2DStorage = new ComponentStorage();

        let entityA = new Entity();
        entityA.id = 0;
        let entityB = new Entity();
        entityB.id = 1;
        let entityC = new Entity();
        entityC.id = 2;

        position2DStorage.set(entityA, new Position2D(10, 20));
        position2DStorage.set(entityB, new Position2D(30, 40));
        position2DStorage.set(entityC, new Position2D(5, 5));

        const filtered = position2DStorage.filter((component) => {
            return component.X > 15;
        });

        expect(filtered.length).toBe(1);
        expect(filtered[0].entityId).toBe(1);
        expect(filtered[0].component.X).toBe(30);
        expect(filtered[0].component.Y).toBe(40);
    });

    test("toArray method", () => {
        let position2DStorage = new ComponentStorage();

        let entityA = new Entity();
        entityA.id = 0;
        let entityB = new Entity();
        entityB.id = 1;

        position2DStorage.set(entityA, new Position2D(10, 20));
        position2DStorage.set(entityB, new Position2D(30, 40));

        const array = position2DStorage.toArray();

        expect(Array.isArray(array)).toBe(true);
        expect(array.length).toBe(2);
        expect(array[0].entityId).toBe(0);
        expect(array[0].component.X).toBe(10);
        expect(array[1].entityId).toBe(1);
        expect(array[1].component.X).toBe(30);
    });

    test("spread operator works", () => {
        let position2DStorage = new ComponentStorage();

        let entityA = new Entity();
        entityA.id = 0;
        let entityB = new Entity();
        entityB.id = 1;

        position2DStorage.set(entityA, new Position2D(10, 20));
        position2DStorage.set(entityB, new Position2D(30, 40));

        const spread = [...position2DStorage];

        expect(spread.length).toBe(2);
        expect(spread[0].entityId).toBe(0);
        expect(spread[0].component.X).toBe(10);
        expect(spread[1].entityId).toBe(1);
        expect(spread[1].component.X).toBe(30);
    });

    test("enumeration after removal maintains correct order", () => {
        let position2DStorage = new ComponentStorage();

        let entityA = new Entity();
        entityA.id = 0;
        let entityB = new Entity();
        entityB.id = 1;
        let entityC = new Entity();
        entityC.id = 2;

        position2DStorage.set(entityA, new Position2D(10, 20));
        position2DStorage.set(entityB, new Position2D(30, 40));
        position2DStorage.set(entityC, new Position2D(50, 60));

        // Remove middle entity
        position2DStorage.remove(entityB);

        const remaining = [...position2DStorage];

        expect(remaining.length).toBe(2);
        // After removal, the last element should have moved to fill the gap
        expect(remaining.some((item) => item.entityId === 0)).toBe(true);
        expect(remaining.some((item) => item.entityId === 2)).toBe(true);
        expect(remaining.some((item) => item.entityId === 1)).toBe(false);
    });

    test("enumeration with empty storage", () => {
        let position2DStorage = new ComponentStorage();

        const items = [...position2DStorage];
        expect(items.length).toBe(0);

        let forEachCalled = false;
        position2DStorage.forEach(() => {
            forEachCalled = true;
        });
        expect(forEachCalled).toBe(false);

        const mapped = position2DStorage.map((x) => x);
        expect(mapped.length).toBe(0);

        const filtered = position2DStorage.filter(() => true);
        expect(filtered.length).toBe(0);
    });
});
