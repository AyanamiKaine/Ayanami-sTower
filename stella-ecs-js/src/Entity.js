export class Entity {
    constructor(world) {
        this.id = 0;
        this.world = world;
    }

    set(component) {
        this.world.set(this, component);
    }

    get(componentClass) {
        return this.world.get(this, componentClass);
    }
}
