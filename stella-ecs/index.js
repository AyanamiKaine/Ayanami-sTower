import Graph from 'graphology';

/**
 * @class ComponentFactory
 * @description Manages the definition and creation of components from string names and JSON schemas.
 */
class ComponentFactory {
    constructor(world) {
        this.world = world;
        this.definitions = new Map(); // Map<string, ComponentClass>
    }

    /**
     * Defines a new component class from a JSON schema.
     * @param {string} name - The name of the component.
     * @param {object} schema - The default properties for the component.
     * @returns {Function} The newly created component class.
     */
    define(name, schema) {
        const DynamicComponent = class {
            constructor(initialValues = {}) {
                Object.assign(this, schema, initialValues);
            }
        };
        Object.defineProperty(DynamicComponent, 'name', { value: name });

        this.definitions.set(name, DynamicComponent);
        this.world.registerComponent(DynamicComponent);
        console.log(`Component '${name}' defined.`);
        return DynamicComponent;
    }

    /**
     * Creates an instance of a defined component.
     * @param {string} name - The name of the component to create.
     * @param {object} [initialValues={}] - Values to override the schema defaults.
     * @returns {object|null} An instance of the component or null if not defined.
     */
    create(name, initialValues) {
        const ComponentClass = this.definitions.get(name);
        if (!ComponentClass) {
            console.error(`Component type "${name}" not defined.`);
            return null;
        }
        return new ComponentClass(initialValues);
    }

    /**
     * Retrieves the class constructor for a defined component.
     * @param {string} name - The name of the component.
     * @returns {Function|undefined}
     */
    getClass(name) {
        return this.definitions.get(name);
    }
}


/**
 * @class World
 * @description The main container for entities, components, systems, and their relationships.
 */
export class World {
    constructor() {
        this.nextEntityID = 0;
        this.entities = new Map(); // Map<EntityID, Map<ComponentClass, ComponentInstance>>
        this.systems = [];
        this.componentFactory = new ComponentFactory(this);

        // Bitset system for components
        this.componentTypes = new Map(); // Map<ComponentClass, bitmask>
        this.nextComponentType = 0;
        this.entityBitmasks = new Map(); // Map<EntityID, bitmask>

        // Caching for queries
        this.queryCacheBitset = new Map(); // Map<bitmask, Set<EntityID>>

        // Graphology for entity relationships
        this.relationshipGraph = new Graph({ type: 'directed', allowSelfLoops: false });
    }

    /**
     * Registers a component class with the world, assigning it a unique bit for queries.
     * @param {Function} ComponentClass - The component class constructor.
     */
    registerComponent(ComponentClass) {
        if (!this.componentTypes.has(ComponentClass)) {
            const bit = 1 << this.nextComponentType;
            if (this.nextComponentType >= 32) {
                console.error("Maximum number of component types (32) exceeded.");
                return;
            }
            this.componentTypes.set(ComponentClass, bit);
            this.nextComponentType++;
        }
    }

    /**
     * Creates a new entity with a unique ID.
     * @returns {number} The new entity's ID.
     */
    createEntity() {
        const id = this.nextEntityID++;
        this.entities.set(id, new Map());
        this.entityBitmasks.set(id, 0);
        this.relationshipGraph.addNode(id);
        return id;
    }

    /**
     * Adds a component to an entity. Can be an instance or a string name defined in the factory.
     * @param {number} entityId - The ID of the entity.
     * @param {object|string} componentOrName - The component instance or its string name.
     * @param {object} [initialValues={}] - If using a string name, these values override schema defaults.
     */
    addComponent(entityId, componentOrName, initialValues = {}) {
        let component;
        if (typeof componentOrName === 'string') {
            component = this.componentFactory.create(componentOrName, initialValues);
            if (!component) return;
        } else {
            component = componentOrName;
        }

        const entityComponents = this.entities.get(entityId);
        if (entityComponents) {
            const ComponentClass = component.constructor;
            if (!this.componentTypes.has(ComponentClass)) {
                this.registerComponent(ComponentClass);
            }

            entityComponents.set(ComponentClass, component);
            const bit = this.componentTypes.get(ComponentClass);
            const newBitmask = this.entityBitmasks.get(entityId) | bit;
            this.entityBitmasks.set(entityId, newBitmask);
            this._updateCachesForEntity(entityId);
        }
    }

    /**
     * Removes a component from an entity.
     * @param {number} entityId - The ID of the entity.
     * @param {Function|string} componentClassOrName - The component class or its string name.
     */
    removeComponent(entityId, componentClassOrName) {
        const ComponentClass = typeof componentClassOrName === 'string'
            ? this.componentFactory.getClass(componentClassOrName)
            : componentClassOrName;

        if (!ComponentClass) {
            console.error(`Cannot remove unknown component: ${componentClassOrName}`);
            return;
        }

        const entityComponents = this.entities.get(entityId);
        if (entityComponents && entityComponents.has(ComponentClass)) {
            entityComponents.delete(ComponentClass);
            const bit = this.componentTypes.get(ComponentClass);
            const newBitmask = this.entityBitmasks.get(entityId) & ~bit;
            this.entityBitmasks.set(entityId, newBitmask);
            this._updateCachesForEntity(entityId);
        }
    }

    /**
     * Retrieves a component instance from an entity.
     * @param {number} entityId - The ID of the entity.
     * @param {Function|string} componentClassOrName - The component class or its string name.
     * @returns {object|undefined}
     */
    getComponent(entityId, componentClassOrName) {
        const ComponentClass = typeof componentClassOrName === 'string'
            ? this.componentFactory.getClass(componentClassOrName)
            : componentClassOrName;
        if (!ComponentClass) return undefined;
        return this.entities.get(entityId)?.get(ComponentClass);
    }

    /**
     * Registers a system to be run on every world update.
     * @param {System} system - An instance of a class that extends System.
     */
    registerSystem(system) {
        this.systems.push(system);
        console.log(`System '${system.constructor.name}' registered.`);
    }

    /**
     * Queries for entities that have a given set of components, using the high-performance bitset cache.
     * @param {Array<Function|string>} componentClassesOrNames - An array of component classes or string names.
     * @returns {Set<number>} A set of entity IDs that match the query.
     */
    query(componentClassesOrNames) {
        let queryBitmask = 0;
        for (const classOrName of componentClassesOrNames) {
            const ComponentClass = typeof classOrName === 'string'
                ? this.componentFactory.getClass(classOrName)
                : classOrName;
            if (ComponentClass && this.componentTypes.has(ComponentClass)) {
                queryBitmask |= this.componentTypes.get(ComponentClass);
            } else {
                return new Set(); // Query for an unregistered component will always be empty.
            }
        }

        if (this.queryCacheBitset.has(queryBitmask)) {
            return this.queryCacheBitset.get(queryBitmask);
        }

        const matchingEntities = new Set();
        for (const [entityId, entityBitmask] of this.entityBitmasks.entries()) {
            if ((entityBitmask & queryBitmask) === queryBitmask) {
                matchingEntities.add(entityId);
            }
        }
        this.queryCacheBitset.set(queryBitmask, matchingEntities);
        return matchingEntities;
    }

    /**
     * Internal method to update query caches when an entity's components change.
     * @private
     */
    _updateCachesForEntity(entityId, isBeingDestroyed = false) {
        const entityBitmask = this.entityBitmasks.get(entityId) || 0;
        for (const [queryBitmask, cachedEntities] of this.queryCacheBitset.entries()) {
            const entityMatches = !isBeingDestroyed && (entityBitmask & queryBitmask) === queryBitmask;
            if (entityMatches) {
                cachedEntities.add(entityId);
            } else {
                cachedEntities.delete(entityId);
            }
        }
    }

    /**
     * Executes all registered systems.
     * @param {number} deltaTime - The time elapsed since the last update, in seconds.
     */
    update(deltaTime) {
        console.log(`--- World Update (Δt: ${deltaTime.toFixed(3)}s) ---`);
        for (const system of this.systems) {
            system.update(this, deltaTime);
        }
    }
}

/**
 * @class System
 * @description Base class for all systems. Contains the logic that operates on entities.
 */
export class System {
    /**
     * @param {Array<Function|string>} [queryComponentNames=[]] - An array of component classes or names this system operates on.
     */
    constructor(queryComponentNames = []) {
        this.queryComponentNames = queryComponentNames;
    }

    /**
     * The main update method called by the World.
     * @param {World} world - The world instance.
     * @param {number} deltaTime - The time elapsed since the last update.
     */
    update(world, deltaTime) {
        const entities = world.query(this.queryComponentNames);
        if (entities.size > 0) {
            this.execute(world, entities, deltaTime);
        }
    }

    /**
     * The core logic of the system. This method should be overridden by subclasses.
     * @param {World} world - The world instance.
     * @param {Set<number>} entities - The set of entity IDs matching the system's query.
     * @param {number} deltaTime - The time elapsed since the last update.
     */
    execute(world, entities, deltaTime) {
        throw new Error("System must implement an execute method.");
    }
}
