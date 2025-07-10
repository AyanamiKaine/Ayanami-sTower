import Graph from 'graphology';

/**
 * @class Entity
 * @description A wrapper around an entity ID that provides a fluent API for manipulating components and relationships.
 */
export class Entity {
    /**
     * @param {number} id - The entity's ID.
     * @param {World} world - A reference to the world this entity belongs to.
     */
    constructor(id, world) {
        this.id = id;
        this.world = world;
    }

    /**
     * Adds or updates a component on the entity.
     * @param {object|string} componentOrName - The component instance or the name of a dynamically defined component.
     * @param {object} [initialValues] - Initial values if using a component name.
     * @returns {Entity} The entity instance for chaining.
     */
    set(componentOrName, initialValues) {
        this.world.addComponent(this.id, componentOrName, initialValues);
        return this;
    }

    /**
     * Gets a component instance from the entity.
     * @param {Function|string} componentClassOrName - The component class or name to retrieve.
     * @returns {object|undefined} The component instance, or undefined if not found.
     */
    get(componentClassOrName) {
        return this.world.getComponent(this.id, componentClassOrName);
    }

    /**
     * Removes a component from the entity.
     * @param {Function|string} componentClassOrName - The component class or name to remove.
     * @returns {Entity} The entity instance for chaining.
     */
    remove(componentClassOrName) {
        this.world.removeComponent(this.id, componentClassOrName);
        return this;
    }

    /**
     * Checks if the entity has a specific component.
     * @param {Function|string} componentClassOrName - The component class or name to check for.
     * @returns {boolean} True if the component exists on the entity.
     */
    has(componentClassOrName) {
        return this.world.hasComponent(this.id, componentClassOrName);
    }

    /**
     * Destroys the entity, removing it and all its components from the world.
     */
    destroy() {
        this.world.destroyEntity(this.id);
    }

    /**
     * Adds a child entity.
     * @param {Entity} childEntity - The entity to add as a child.
     * @param {object} [attributes] - Optional attributes for the relationship.
     * @returns {Entity} The entity instance for chaining.
     */
    addChild(childEntity, attributes) {
        this.world.addDirectedRelationship(this.id, childEntity.id, attributes);
        return this;
    }

    /**
     * Creates an undirected connection to another entity.
     * @param {Entity} otherEntity - The entity to connect with.
     * @param {object} [attributes] - Optional attributes for the relationship.
     * @returns {Entity} The entity instance for chaining.
     */
    connectTo(otherEntity, attributes) {
        this.world.addUndirectedRelationship(this.id, otherEntity.id, attributes);
        return this;
    }
}


/**
 * @class Archetype
 * @private
 */
class Archetype {
    constructor(id, componentClasses) {
        this.id = id;
        this.componentClasses = componentClasses;
        this.componentArrays = new Map(componentClasses.map(C => [C, []]));
        this.entityMap = new Map();
        this.entityList = [];
    }

    addEntity(entityId, components) {
        const index = this.entityList.length;
        this.entityMap.set(entityId, index);
        this.entityList.push(entityId);

        for (const ComponentClass of this.componentClasses) {
            this.componentArrays.get(ComponentClass).push(components.get(ComponentClass));
        }
    }

    removeEntity(entityId) {
        const indexToRemove = this.entityMap.get(entityId);
        if (indexToRemove === undefined) return { movedEntityId: null, oldIndex: -1 };

        const lastIndex = this.entityList.length - 1;
        const lastEntityId = this.entityList[lastIndex];

        for (const ComponentClass of this.componentClasses) {
            const array = this.componentArrays.get(ComponentClass);
            array[indexToRemove] = array[lastIndex];
            array.pop();
        }

        this.entityList[indexToRemove] = lastEntityId;
        this.entityList.pop();

        this.entityMap.delete(entityId);

        if (indexToRemove !== lastIndex) {
            this.entityMap.set(lastEntityId, indexToRemove);
            return { movedEntityId: lastEntityId, oldIndex: lastIndex };
        }

        return { movedEntityId: null, oldIndex: lastIndex };
    }
}

/**
 * @class QueryResult
 */
export class QueryResult {
    constructor(archetypes) {
        this.archetypes = archetypes;
        this.count = archetypes.reduce((sum, arch) => sum + arch.entityList.length, 0);
    }

    *[Symbol.iterator]() {
        for (const archetype of this.archetypes) {
            for (let i = 0; i < archetype.entityList.length; i++) {
                const entityId = archetype.entityList[i];
                const components = new Map();
                for (const ComponentClass of archetype.componentClasses) {
                    components.set(ComponentClass, archetype.componentArrays.get(ComponentClass)[i]);
                }
                yield { entity: entityId, components: components };
            }
        }
    }

    forEach(callback) {
        for (const result of this) {
            callback(result);
        }
    }
}


/**
 * @class ComponentFactory
 * @private
 */
class ComponentFactory {
    constructor(world) {
        this.world = world;
        this.definitions = new Map();
    }
    define(name, schema) {
        const DynamicComponent = class {
            constructor(initialValues = {}) {
                Object.assign(this, schema, initialValues);
            }
        };
        Object.defineProperty(DynamicComponent, 'name', { value: name });
        this.definitions.set(name, DynamicComponent);
        this.world.registerComponent(DynamicComponent);
        return DynamicComponent;
    }
    create(name, initialValues) {
        const ComponentClass = this.definitions.get(name);
        if (!ComponentClass) return null;
        return new ComponentClass(initialValues);
    }
    getClass(name) {
        return this.definitions.get(name);
    }
}

/**
 * @class World
 * @description The main container for entities, components, and systems.
 */
export class World {
    constructor({ graphType = 'mixed' } = {}) {
        this.nextEntityID = 0;
        this.systems = [];
        this.componentFactory = new ComponentFactory(this);
        this.componentTypes = new Map();
        this.componentClasses = new Map();
        this.nextComponentType = 0;
        this.archetypes = new Map();
        this.entityArchetypeMap = new Map();
        this.relationshipGraph = new Graph({ type: graphType, allowSelfLoops: false });
    }

    registerComponent(ComponentClass) {
        if (!this.componentTypes.has(ComponentClass)) {
            const bit = 1 << this.nextComponentType;
            if (this.nextComponentType >= 32) {
                console.error("Maximum number of component types (32) exceeded.");
                return;
            }
            this.componentTypes.set(ComponentClass, bit);
            this.componentClasses.set(bit, ComponentClass);
            this.nextComponentType++;
        }
    }

    /**
     * Creates a new entity.
     * @returns {Entity} The new entity instance.
     */
    createEntity() {
        const id = this.nextEntityID++;
        this.relationshipGraph.addNode(id);
        return new Entity(id, this);
    }

    /**
     * Destroys an entity, removing it from all systems and relationships.
     * @param {number} entityId - The ID of the entity to destroy.
     */
    destroyEntity(entityId) {
        const archetype = this.entityArchetypeMap.get(entityId);
        if (archetype) {
            archetype.removeEntity(entityId);
        }
        this.entityArchetypeMap.delete(entityId);
        if (this.relationshipGraph.hasNode(entityId)) {
            this.relationshipGraph.dropNode(entityId);
        }
    }


    addComponent(entityId, componentOrName, initialValues = {}) {
        let component;
        if (typeof componentOrName === 'string') {
            component = this.componentFactory.create(componentOrName, initialValues);
            if (!component) return;
        } else {
            component = componentOrName;
        }
        const ComponentClass = component.constructor;
        if (!this.componentTypes.has(ComponentClass)) {
            this.registerComponent(ComponentClass);
        }

        const oldArchetype = this.entityArchetypeMap.get(entityId);
        const oldBitmask = oldArchetype ? oldArchetype.id : 0;
        const newBitmask = oldBitmask | this.componentTypes.get(ComponentClass);

        if (oldBitmask === newBitmask) return;

        const newArchetype = this._findOrCreateArchetype(newBitmask);

        const components = new Map();
        if (oldArchetype) {
            const index = oldArchetype.entityMap.get(entityId);
            for (const C of oldArchetype.componentClasses) {
                components.set(C, oldArchetype.componentArrays.get(C)[index]);
            }
            oldArchetype.removeEntity(entityId);
        }
        components.set(ComponentClass, component);

        newArchetype.addEntity(entityId, components);
        this.entityArchetypeMap.set(entityId, newArchetype);
    }

    /**
     * Removes a component from an entity.
     * @param {number} entityId - The ID of the entity.
     * @param {Function|string} componentClassOrName - The component class or name to remove.
     */
    removeComponent(entityId, componentClassOrName) {
        const oldArchetype = this.entityArchetypeMap.get(entityId);
        if (!oldArchetype) return;

        const ComponentClass = this._getComponentClass(componentClassOrName);
        if (!ComponentClass) return;

        const componentBit = this.componentTypes.get(ComponentClass);
        if (!componentBit || (oldArchetype.id & componentBit) === 0) {
            return; // Entity doesn't have this component
        }

        const newBitmask = oldArchetype.id & ~componentBit;
        const newArchetype = this._findOrCreateArchetype(newBitmask);

        // Collect components, skipping the one to be removed
        const components = new Map();
        const index = oldArchetype.entityMap.get(entityId);
        for (const C of oldArchetype.componentClasses) {
            if (C !== ComponentClass) {
                components.set(C, oldArchetype.componentArrays.get(C)[index]);
            }
        }

        oldArchetype.removeEntity(entityId);
        newArchetype.addEntity(entityId, components);
        this.entityArchetypeMap.set(entityId, newArchetype);
    }


    getComponent(entityId, componentClassOrName) {
        const archetype = this.entityArchetypeMap.get(entityId);
        if (!archetype) return undefined;

        const ComponentClass = this._getComponentClass(componentClassOrName);
        if (!ComponentClass || !archetype.componentArrays.has(ComponentClass)) return undefined;

        const index = archetype.entityMap.get(entityId);
        return archetype.componentArrays.get(ComponentClass)[index];
    }

    hasComponent(entityId, componentClassOrName) {
        const archetype = this.entityArchetypeMap.get(entityId);
        if (!archetype) return false;
        const ComponentClass = this._getComponentClass(componentClassOrName);
        if (!ComponentClass) return false;
        const componentBit = this.componentTypes.get(ComponentClass);
        return (archetype.id & componentBit) !== 0;
    }

    _getComponentClass(componentClassOrName) {
        // If it's not a string, it's already a class constructor.
        if (typeof componentClassOrName !== 'string') {
            return componentClassOrName;
        }

        // It's a string name. First, check the dynamic component factory.
        const dynamicClass = this.componentFactory.getClass(componentClassOrName);
        if (dynamicClass) {
            return dynamicClass;
        }

        // If not found, iterate through all registered static components.
        for (const componentClass of this.componentTypes.keys()) {
            if (componentClass.name === componentClassOrName) {
                return componentClass;
            }
        }

        // Return undefined if no class is found for the given name.
        return undefined;
    }

    _findOrCreateArchetype(bitmask) {
        if (this.archetypes.has(bitmask)) {
            return this.archetypes.get(bitmask);
        }

        const componentClasses = [];
        for (let i = 0; i < this.nextComponentType; i++) {
            const bit = 1 << i;
            if ((bitmask & bit) === bit) {
                componentClasses.push(this.componentClasses.get(bit));
            }
        }

        const newArchetype = new Archetype(bitmask, componentClasses);
        this.archetypes.set(bitmask, newArchetype);
        return newArchetype;
    }

    // --- Relationship API ---

    addDirectedRelationship(source, target, attributes = {}) {
        this.relationshipGraph.addDirectedEdge(source, target, attributes);
    }

    addUndirectedRelationship(source, target, attributes = {}) {
        this.relationshipGraph.addUndirectedEdge(source, target, attributes);
    }

    removeRelationship(source, target) {
        if (this.relationshipGraph.hasEdge(source, target)) {
            this.relationshipGraph.dropEdge(source, target);
        }
    }

    getChildren(entityId) {
        return this.relationshipGraph.outNeighbors(String(entityId));
    }

    getParents(entityId) {
        return this.relationshipGraph.inNeighbors(String(entityId));
    }

    getConnections(entityId) {
        return this.relationshipGraph.neighbors(String(entityId));
    }

    getConnectionsWithDetails(entityId) {
        const connections = [];
        this.relationshipGraph.forEachEdge(String(entityId), (edge, attributes, source, target) => {
            const neighbor = source == entityId ? target : source;
            connections.push({
                neighbor: neighbor,
                kind: this.relationshipGraph.isDirected(edge) ? 'directed' : 'undirected',
                attributes: attributes
            });
        });
        return connections;
    }


    // --- System and World Update ---

    registerSystem(system) {
        this.systems.push(system);
    }

    query(componentClassesOrNames) {
        let queryBitmask = 0;
        for (const classOrName of componentClassesOrNames) {
            const ComponentClass = this._getComponentClass(classOrName);
            if (ComponentClass && this.componentTypes.has(ComponentClass)) {
                queryBitmask |= this.componentTypes.get(ComponentClass);
            } else {
                return [];
            }
        }

        const matchingArchetypes = [];
        for (const archetype of this.archetypes.values()) {
            if ((archetype.id & queryBitmask) === queryBitmask) {
                matchingArchetypes.push(archetype);
            }
        }
        return matchingArchetypes;
    }

    update(deltaTime) {
        for (const system of this.systems) {
            system.update(this, deltaTime);
        }
    }

    // --- Serialization ---

    toJSON() {
        const entities = [];
        for (const archetype of this.archetypes.values()) {
            for (let i = 0; i < archetype.entityList.length; i++) {
                const entityId = archetype.entityList[i];
                const components = [];
                for (const ComponentClass of archetype.componentClasses) {
                    const componentInstance = archetype.componentArrays.get(ComponentClass)[i];
                    components.push({
                        type: ComponentClass.name,
                        data: { ...componentInstance }
                    });
                }
                entities.push({ id: entityId, components });
            }
        }

        const componentDefinitions = [];
        for (const [name, ComponentClass] of this.componentFactory.definitions.entries()) {
            const instance = new ComponentClass();
            const schema = {};
            for (const key in instance) {
                schema[key] = instance[key];
            }
            componentDefinitions.push({ name, schema });
        }

        return {
            nextEntityID: this.nextEntityID,
            componentDefinitions,
            entities,
            graph: this.relationshipGraph.export()
        };
    }

    static fromJSON(json, { systems = [], staticComponents = [] } = {}) {
        // 1. Create a new world. Its graph is empty.
        const world = new World({ graphType: json.graph?.options?.type || 'mixed' });

        // 2. Register all components
        for (const ComponentClass of staticComponents) {
            world.registerComponent(ComponentClass);
        }
        for (const def of json.componentDefinitions) {
            world.componentFactory.define(def.name, def.schema);
        }

        // 3. Import the graph structure first. This populates the graph with nodes (entities) and edges.
        if (json.graph) {
            world.relationshipGraph.import(json.graph);
        }

        // 4. Set the next entity ID to ensure new entities don't conflict.
        world.nextEntityID = json.nextEntityID;

        // 5. Now, iterate through the entity data and add components to the entities that already exist as nodes in the graph.
        for (const entityData of json.entities) {
            const entityId = entityData.id;

            if (!world.relationshipGraph.hasNode(entityId)) {
                console.warn(`Serialized entity ${entityId} not found in graph, skipping.`);
                continue;
            }

            let finalBitmask = 0;
            const componentsMap = new Map();

            // Collect all components for this entity first
            for (const componentData of entityData.components) {
                const ComponentClass = world._getComponentClass(componentData.type);
                if (ComponentClass) {
                    const componentInstance = new ComponentClass();
                    Object.assign(componentInstance, componentData.data);
                    componentsMap.set(ComponentClass, componentInstance);
                    finalBitmask |= world.componentTypes.get(ComponentClass);
                } else {
                    console.error(`Could not find component class for type "${componentData.type}" during deserialization.`);
                }
            }

            // Now, find the correct archetype and add the entity in one shot.
            if (finalBitmask > 0) {
                const targetArchetype = world._findOrCreateArchetype(finalBitmask);
                targetArchetype.addEntity(entityId, componentsMap);
                world.entityArchetypeMap.set(entityId, targetArchetype);
            }
        }

        // 6. Register systems
        for (const system of systems) {
            world.registerSystem(system);
        }

        return world;
    }
}

/**
 * @class System
 * @description Base class for all systems.
 */
export class System {
    constructor(queryComponentNames = []) {
        this.queryComponentNames = queryComponentNames;
    }

    update(world, deltaTime) {
        const archetypes = world.query(this.queryComponentNames);
        if (archetypes.length > 0) {
            const queryResult = new QueryResult(archetypes);
            this.execute(queryResult, world, deltaTime);
        }
    }

    execute(entities, world, deltaTime) {
        throw new Error("System must implement an execute method.");
    }
}
