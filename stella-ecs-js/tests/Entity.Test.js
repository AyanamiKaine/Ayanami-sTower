import { test, expect, describe } from "bun:test";
import { SparsedSet } from "../src/SparsedSet";
import { World } from "../src/World";

class Position2D {
    constructor(x, y) {
        this.X = x;
        this.Y = y;
    }
}

describe("Entity Tests", () => {
    test("adding new components to  entities", () => {
        const world = new World();
        let e1 = world.createEntity();

        e1.set(new Position2D(10, 20));
        const position = e1.get(new Position2D()); 
        expect(position.X).toBe(10); 
    });
});
