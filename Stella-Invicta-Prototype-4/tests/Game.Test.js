import { test, expect, describe } from "bun:test";
import { Game } from "../src/game";
import { velocity3D } from "../src/mixins/Velocity3D";
import { position3D } from "../src/mixins/Position3D";

describe("Game Constructor Tests", () => {
    test("Game can have entities", () => {
        const game = new Game();
        const e = game.createEntity("Karl");

        expect(e.name).toBe("Karl");
    });

    test("Creating Entities Increments their id", () => {
        const game = new Game();

        const a = game.createEntity(); // will be assigned the id 1
        const b = game.createEntity(); // will be assigned the id 2

        expect(a.id).toBe(1);
        expect(b.id).toBe(2);
    });
});

describe("Game Systems", () => {
    test("Movement System Example", () => {
        const game = new Game();
        const e = game.createEntity("Karl");
        e.with(velocity3D, { x: 1, y: 1, z: 1 });
        e.with(position3D);

        expect(e.position3D).toEqual({ x: 0, y: 0, z: 0 });

        const MovementSystem = (entities, deltaTime) => {
            for (const entity of entities) {
                if (entity.position3D && entity.velocity3D) {
                    entity.position3D.x += entity.velocity3D.x * deltaTime;
                    entity.position3D.y += entity.velocity3D.y * deltaTime;
                    entity.position3D.z += entity.velocity3D.z * deltaTime;
                }
            }
        };

        MovementSystem(game.entities, 1);

        expect(e.position3D).toEqual({ x: 1, y: 1, z: 1 });

        expect(e.name).toBe("Karl");
    });
});
