export class Entity {
    constructor(world) {
        this.id = 0;
        this.world = world;
    }

    set(component) {
        this.world.set(this, component);
    }

    get(component) {
        return this.world.get(this, component);
    }
}
