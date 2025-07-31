import { test, expect, describe } from "bun:test";
import { World } from "../src/World";

class Position2D {
    constructor(x, y) {
        this.X = x;
        this.Y = y;
    }
}
describe("World", () => {
    test("Creating new entities", () => {
        const world = new World();
        let e1 = world.createEntity();
        let e2 = world.createEntity();

        expect(e1.id).toBe(0);
        expect(e2.id).toBe(1);
    });

    test("adding new components to  entities", () => {
        const world = new World();
        let e1 = world.createEntity();

        world.set(e1, new Position2D(10, 20));
        expect(world.get(e1, Position2D).X).toBe(10);
        expect(world.get(e1, Position2D).Y).toBe(20);
    });
});
