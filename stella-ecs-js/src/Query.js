export class Query {
    constructor(world) {
        this.world = world;
    }

    with(componentClass) {}
    without(componentClass) {}
    optional(componentClass) {}
    build() {}
}
