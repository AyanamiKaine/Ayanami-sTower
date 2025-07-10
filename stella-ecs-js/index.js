import Graph from "graphology";

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
        this.world.addUndirectedRelationship(
            this.id,
            otherEntity.id,
            attributes
        );
        return this;
    }
}

/**
 * @class Archetype
 * @description Manages a collection of entities that all have the same set of components. This is a performance optimization.
 * @private
 */
class Archetype {
    /**
     * @param {number} id - The bitmask representing the component signature of this archetype.
     * @param {Function[]} componentClasses - An array of component classes that define this archetype.
     */
    constructor(id, componentClasses) {
        this.id = id;
        this.componentClasses = componentClasses;
        // Map from ComponentClass to an array of component instances
        this.componentArrays = new Map(componentClasses.map((C) => [C, []]));
        // Map from entityId to its index in the entityList and componentArrays
        this.entityMap = new Map();
        // A packed array of entity IDs in this archetype
        this.entityList = [];
    }

    /**
     * Adds an entity and its components to this archetype.
     * @param {number} entityId - The ID of the entity to add.
     * @param {Map<Function, object>} components - A map of component classes to component instances.
     */
    addEntity(entityId, components) {
        const index = this.entityList.length;
        this.entityMap.set(entityId, index);
        this.entityList.push(entityId);

        for (const ComponentClass of this.componentClasses) {
            this.componentArrays
                .get(ComponentClass)
                .push(components.get(ComponentClass));
        }
    }

    /**
     * Removes an entity from this archetype using a swap-and-pop method for efficiency.
     * @param {number} entityId - The ID of the entity to remove.
     * @returns {{movedEntityId: number|null, oldIndex: number}} An object containing the ID of the entity that was moved to fill the gap, and its old index.
     */
    removeEntity(entityId) {
        const indexToRemove = this.entityMap.get(entityId);
        if (indexToRemove === undefined)
            return { movedEntityId: null, oldIndex: -1 };

        const lastIndex = this.entityList.length - 1;
        const lastEntityId = this.entityList[lastIndex];

        // Swap the last element with the one to remove for all component arrays
        for (const ComponentClass of this.componentClasses) {
            const array = this.componentArrays.get(ComponentClass);
            array[indexToRemove] = array[lastIndex];
            array.pop();
        }

        // Update the entity list
        this.entityList[indexToRemove] = lastEntityId;
        this.entityList.pop();

        // Update the entity map
        this.entityMap.delete(entityId);

        // If the removed entity was not the last one, update the map for the moved entity
        if (indexToRemove !== lastIndex) {
            this.entityMap.set(lastEntityId, indexToRemove);
            return { movedEntityId: lastEntityId, oldIndex: lastIndex };
        }

        return { movedEntityId: null, oldIndex: lastIndex };
    }
}

/**
 * @class QueryResult
 * @description A container for the results of a world query, providing an iterable interface.
 */
export class QueryResult {
    /**
     * @param {Archetype[]} archetypes - An array of archetypes that match the query.
     */
    constructor(archetypes) {
        this.archetypes = archetypes;
        this.count = archetypes.reduce(
            (sum, arch) => sum + arch.entityList.length,
            0
        );
    }

    /**
     * Makes the QueryResult iterable, yielding each entity and its components.
     * @yields {{entity: number, components: Map<Function, object>}}
     */
    *[Symbol.iterator]() {
        for (const archetype of this.archetypes) {
            for (let i = 0; i < archetype.entityList.length; i++) {
                const entityId = archetype.entityList[i];
                const components = new Map();
                for (const ComponentClass of archetype.componentClasses) {
                    components.set(
                        ComponentClass,
                        archetype.componentArrays.get(ComponentClass)[i]
                    );
                }
                yield { entity: entityId, components: components };
            }
        }
    }

    /**
     * Executes a callback for each entity in the query result.
     * @param {function({entity: number, components: Map<Function, object>})} callback - The function to execute for each entity.
     */
    forEach(callback) {
        for (const result of this) {
            callback(result);
        }
    }
}

/**
 * @class ComponentFactory
 * @description Manages the definition and creation of dynamic components from string names.
 * @private
 */
class ComponentFactory {
    /**
     * @param {World} world - A reference to the world.
     */
    constructor(world) {
        this.world = world;
        this.definitions = new Map();
    }

    /**
     * Defines a new component type dynamically.
     * @param {string} name - The name of the component.
     * @param {object} schema - An object defining the default properties of the component.
     * @returns {Function} The newly created component class.
     */
    define(name, schema) {
        const DynamicComponent = class {
            constructor(initialValues = {}) {
                Object.assign(this, schema, initialValues);
            }
        };
        Object.defineProperty(DynamicComponent, "name", { value: name });
        this.definitions.set(name, DynamicComponent);
        this.world.registerComponent(DynamicComponent);
        return DynamicComponent;
    }

    /**
     * Creates an instance of a dynamically defined component.
     * @param {string} name - The name of the component to create.
     * @param {object} [initialValues] - The initial values for the component's properties.
     * @returns {object|null} A new component instance, or null if the definition doesn't exist.
     */
    create(name, initialValues) {
        const ComponentClass = this.definitions.get(name);
        if (!ComponentClass) return null;
        return new ComponentClass(initialValues);
    }

    /**
     * Retrieves the class constructor for a dynamically defined component.
     * @param {string} name - The name of the component class to retrieve.
     * @returns {Function|undefined} The component class, or undefined if not found.
     */
    getClass(name) {
        return this.definitions.get(name);
    }
}

/**
 * @class World
 * @description The main container for entities, components, and systems. Manages the entire state of the ECS.
 */
export class World {
    /**
     * @param {object} [options] - Configuration options for the world.
     * @param {string} [options.graphType='mixed'] - The type of graph to use for relationships ('mixed', 'directed', 'undirected').
     */
    constructor({ graphType = "mixed" } = {}) {
        this.nextEntityID = 0;
        this.systems = [];
        this.componentFactory = new ComponentFactory(this);
        this.componentTypes = new Map();
        this.componentClasses = new Map();
        this.nextComponentType = 0;
        this.archetypes = new Map();
        this.entityArchetypeMap = new Map();
        this.relationshipGraph = new Graph({
            type: graphType,
            allowSelfLoops: false,
        });
    }

    /**
     * Registers a component class with the world, assigning it a unique bitmask.
     * @param {Function} ComponentClass - The component class to register.
     */
    registerComponent(ComponentClass) {
        if (!this.componentTypes.has(ComponentClass)) {
            const bit = 1 << this.nextComponentType;
            if (this.nextComponentType >= 32) {
                console.error(
                    "Maximum number of component types (32) exceeded."
                );
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

    /**
     * Adds a component to an entity. This may cause the entity to move to a new archetype.
     * @param {number} entityId - The ID of the entity.
     * @param {object|string} componentOrName - The component instance or the name of a dynamic component.
     * @param {object} [initialValues={}] - Initial values if creating a component by name.
     */
    addComponent(entityId, componentOrName, initialValues = {}) {
        let component;
        if (typeof componentOrName === "string") {
            component = this.componentFactory.create(
                componentOrName,
                initialValues
            );
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

        if (oldBitmask === newBitmask) return; // Already has this component type

        const newArchetype = this._findOrCreateArchetype(newBitmask);

        // Collect existing components
        const components = new Map();
        if (oldArchetype) {
            const index = oldArchetype.entityMap.get(entityId);
            for (const C of oldArchetype.componentClasses) {
                components.set(C, oldArchetype.componentArrays.get(C)[index]);
            }
            oldArchetype.removeEntity(entityId);
        }
        components.set(ComponentClass, component);

        // Add to the new archetype
        newArchetype.addEntity(entityId, components);
        this.entityArchetypeMap.set(entityId, newArchetype);
    }

    /**
     * Removes a component from an entity. This may cause the entity to move to a new archetype.
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

        // Move entity to the new archetype
        oldArchetype.removeEntity(entityId);
        newArchetype.addEntity(entityId, components);
        this.entityArchetypeMap.set(entityId, newArchetype);
    }

    /**
     * Retrieves a specific component instance from an entity.
     * @param {number} entityId - The ID of the entity.
     * @param {Function|string} componentClassOrName - The component class or name to retrieve.
     * @returns {object|undefined} The component instance, or undefined if not found.
     */
    getComponent(entityId, componentClassOrName) {
        const archetype = this.entityArchetypeMap.get(entityId);
        if (!archetype) return undefined;

        const ComponentClass = this._getComponentClass(componentClassOrName);
        if (!ComponentClass || !archetype.componentArrays.has(ComponentClass))
            return undefined;

        const index = archetype.entityMap.get(entityId);
        return archetype.componentArrays.get(ComponentClass)[index];
    }

    /**
     * Checks if an entity has a specific component.
     * @param {number} entityId - The ID of the entity.
     * @param {Function|string} componentClassOrName - The component class or name to check for.
     * @returns {boolean} True if the entity has the component, false otherwise.
     */
    hasComponent(entityId, componentClassOrName) {
        const archetype = this.entityArchetypeMap.get(entityId);
        if (!archetype) return false;
        const ComponentClass = this._getComponentClass(componentClassOrName);
        if (!ComponentClass) return false;
        const componentBit = this.componentTypes.get(ComponentClass);
        return (archetype.id & componentBit) !== 0;
    }

    /**
     * Gets a component class constructor from either a class reference or its string name.
     * @param {Function|string} componentClassOrName - The component class or its name.
     * @returns {Function|undefined} The component class constructor, or undefined if not found.
     * @private
     */
    _getComponentClass(componentClassOrName) {
        if (typeof componentClassOrName !== "string") {
            return componentClassOrName;
        }

        const dynamicClass =
            this.componentFactory.getClass(componentClassOrName);
        if (dynamicClass) {
            return dynamicClass;
        }

        for (const componentClass of this.componentTypes.keys()) {
            if (componentClass.name === componentClassOrName) {
                return componentClass;
            }
        }

        return undefined;
    }

    /**
     * Finds an existing archetype for a given bitmask or creates a new one.
     * @param {number} bitmask - The component bitmask for the archetype.
     * @returns {Archetype} The found or newly created archetype.
     * @private
     */
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

    /**
     * Adds a directed relationship (e.g., parent-child) from a source entity to a target entity.
     * @param {number} source - The ID of the source entity.
     * @param {number} target - The ID of the target entity.
     * @param {object} [attributes={}] - Optional attributes for the relationship edge.
     */
    addDirectedRelationship(source, target, attributes = {}) {
        this.relationshipGraph.addDirectedEdge(source, target, attributes);
    }

    /**
     * Adds an undirected relationship (e.g., connection) between two entities.
     * @param {number} source - The ID of the first entity.
     * @param {number} target - The ID of the second entity.
     * @param {object} [attributes={}] - Optional attributes for the relationship edge.
     */
    addUndirectedRelationship(source, target, attributes = {}) {
        this.relationshipGraph.addUndirectedEdge(source, target, attributes);
    }

    /**
     * Removes a relationship between two entities.
     * @param {number} source - The ID of the source entity.
     * @param {number} target - The ID of the target entity.
     */
    removeRelationship(source, target) {
        if (this.relationshipGraph.hasEdge(source, target)) {
            this.relationshipGraph.dropEdge(source, target);
        }
    }

    /**
     * Gets the children of an entity (out-neighbors in a directed graph).
     * @param {number} entityId - The ID of the parent entity.
     * @returns {string[]} An array of child entity IDs.
     */
    getChildren(entityId) {
        return this.relationshipGraph.outNeighbors(String(entityId));
    }

    /**
     * Gets the parents of an entity (in-neighbors in a directed graph).
     * @param {number} entityId - The ID of the child entity.
     * @returns {string[]} An array of parent entity IDs.
     */
    getParents(entityId) {
        return this.relationshipGraph.inNeighbors(String(entityId));
    }

    /**
     * Gets all connected entities (neighbors in the graph, regardless of direction).
     * @param {number} entityId - The ID of the entity.
     * @returns {string[]} An array of connected entity IDs.
     */
    getConnections(entityId) {
        return this.relationshipGraph.neighbors(String(entityId));
    }

    /**
     * Gets detailed information about all connections for an entity.
     * @param {number} entityId - The ID of the entity.
     * @returns {Array<{neighbor: string, kind: 'directed'|'undirected', attributes: object}>} An array of connection details.
     */
    getConnectionsWithDetails(entityId) {
        const connections = [];
        this.relationshipGraph.forEachEdge(
            String(entityId),
            (edge, attributes, source, target) => {
                const neighbor = source == entityId ? target : source;
                connections.push({
                    neighbor: neighbor,
                    kind: this.relationshipGraph.isDirected(edge)
                        ? "directed"
                        : "undirected",
                    attributes: attributes,
                });
            }
        );
        return connections;
    }

    // --- System and World Update ---

    /**
     * Registers a system to be updated on each world tick.
     * @param {System} system - The system instance to register.
     */
    registerSystem(system) {
        this.systems.push(system);
    }

    /**
     * Queries for archetypes that match a set of component classes.
     * @param {Array<Function|string>} componentClassesOrNames - An array of component classes or names to query for.
     * @returns {Archetype[]} An array of matching archetypes.
     */
    query(componentClassesOrNames) {
        let queryBitmask = 0;
        for (const classOrName of componentClassesOrNames) {
            const ComponentClass = this._getComponentClass(classOrName);
            if (ComponentClass && this.componentTypes.has(ComponentClass)) {
                queryBitmask |= this.componentTypes.get(ComponentClass);
            } else {
                // If any component is not found, the query can't match anything.
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

    /**
     * Updates the world state by running all registered systems.
     * @param {number} deltaTime - The time elapsed since the last update.
     */
    update(deltaTime) {
        for (const system of this.systems) {
            system.update(this, deltaTime);
        }
    }

    // --- Serialization ---

    /**
     * Serializes the entire world state to a JSON object.
     * @returns {object} A JSON-serializable representation of the world.
     */
    toJSON() {
        const entities = [];
        for (const archetype of this.archetypes.values()) {
            for (let i = 0; i < archetype.entityList.length; i++) {
                const entityId = archetype.entityList[i];
                const components = [];
                for (const ComponentClass of archetype.componentClasses) {
                    const componentInstance =
                        archetype.componentArrays.get(ComponentClass)[i];
                    components.push({
                        type: ComponentClass.name,
                        data: { ...componentInstance },
                    });
                }
                entities.push({ id: entityId, components });
            }
        }

        const componentDefinitions = [];
        for (const [
            name,
            ComponentClass,
        ] of this.componentFactory.definitions.entries()) {
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
            graph: this.relationshipGraph.export(),
        };
    }

    /**
     * Creates a new World instance from a serialized JSON object.
     * @param {object} json - The serialized world data.
     * @param {object} [options] - Options for deserialization.
     * @param {System[]} [options.systems=[]] - An array of system instances to register in the new world.
     * @param {Function[]} [options.staticComponents=[]] - An array of static component classes that should be registered.
     * @returns {World} A new World instance populated with the deserialized state.
     */
    static fromJSON(json, { systems = [], staticComponents = [] } = {}) {
        const world = new World({
            graphType: json.graph?.options?.type || "mixed",
        });

        // Register static and dynamic components
        for (const ComponentClass of staticComponents) {
            world.registerComponent(ComponentClass);
        }
        for (const def of json.componentDefinitions) {
            world.componentFactory.define(def.name, def.schema);
        }

        // Import graph structure and set next entity ID
        if (json.graph) {
            world.relationshipGraph.import(json.graph);
        }
        world.nextEntityID = json.nextEntityID;

        // Re-create entities and add their components
        for (const entityData of json.entities) {
            const entityId = entityData.id;

            if (!world.relationshipGraph.hasNode(entityId)) {
                console.warn(
                    `Serialized entity ${entityId} not found in graph, skipping.`
                );
                continue;
            }

            let finalBitmask = 0;
            const componentsMap = new Map();

            for (const componentData of entityData.components) {
                const ComponentClass = world._getComponentClass(
                    componentData.type
                );
                if (ComponentClass) {
                    const componentInstance = new ComponentClass();
                    Object.assign(componentInstance, componentData.data);
                    componentsMap.set(ComponentClass, componentInstance);
                    finalBitmask |= world.componentTypes.get(ComponentClass);
                } else {
                    console.error(
                        `Could not find component class for type "${componentData.type}" during deserialization.`
                    );
                }
            }

            // Find the correct archetype and add the entity in one shot
            if (finalBitmask > 0) {
                const targetArchetype =
                    world._findOrCreateArchetype(finalBitmask);
                targetArchetype.addEntity(entityId, componentsMap);
                world.entityArchetypeMap.set(entityId, targetArchetype);
            }
        }

        // Register systems
        for (const system of systems) {
            world.registerSystem(system);
        }

        return world;
    }
}

/**
 * @class System
 * @description Base class for all systems. Systems contain the logic that operates on entities with specific components.
 */
export class System {
    /**
     * @param {Array<string|Function>} [queryComponentNames=[]] - An array of component names or classes that this system operates on.
     */
    constructor(queryComponentNames = []) {
        this.queryComponentNames = queryComponentNames;
    }

    /**
     * Called by the world on each update tick. Queries for relevant entities and passes them to the execute method.
     * @param {World} world - The world instance.
     * @param {number} deltaTime - The time elapsed since the last update.
     */
    update(world, deltaTime) {
        const archetypes = world.query(this.queryComponentNames);
        if (archetypes.length > 0) {
            const queryResult = new QueryResult(archetypes);
            this.execute(queryResult, world, deltaTime);
        }
    }

    /**
     * The main logic of the system. This method must be implemented by subclasses.
     * @param {QueryResult} entities - The result of the query, containing entities that match the system's component requirements.
     * @param {World} world - The world instance.
     * @param {number} deltaTime - The time elapsed since the last update.
     */
    execute(entities, world, deltaTime) {
        throw new Error("System must implement an execute method.");
    }
}
