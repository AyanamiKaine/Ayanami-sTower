export class Query {
    constructor(world) {
        this.world = world;
    }

    with(componentClass) {}
    without(componentClass) {}
    optional(componentClass) {}
    where(predicate) { }
    // When we build a query it should return an enumerable object where we can get the entity and the individual components (entity, component1, component2, ...)
    build() {}
}
