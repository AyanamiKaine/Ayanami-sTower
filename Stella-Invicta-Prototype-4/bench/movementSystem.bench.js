import { run, bench, summary } from "mitata";
import { Game } from "../src/game";
import { velocity3D } from "../src/mixins/Velocity3D";
import { position3D } from "../src/mixins/Position3D";
// --- Global Setup ---

const game = new Game();

const MovementSystem = (entities, deltaTime) => {
    for (const entity of entities) {
        if (entity.position3D && entity.velocity3D) {
            entity.position3D.x += entity.velocity3D.x * deltaTime;
            entity.position3D.y += entity.velocity3D.y * deltaTime;
            entity.position3D.z += entity.velocity3D.z * deltaTime;
        }
    }
};

for (let i = 0; i < 10000; i++) {
    let e = game.createEntity(`Entity-${i}`);
    e.with(velocity3D, { x: 1, y: 1, z: 1 });
    e.with(position3D);
}

// --- Benchmarks ---

await summary(() => {
    bench("Movement System over 10000 Entities", () => {
        MovementSystem(game.entities, 1);
    });
});

// To run all defined benchmarks
await run();
