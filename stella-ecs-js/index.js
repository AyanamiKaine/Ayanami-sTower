import Graph from "graphology";

/**
 * @class Not
 * @description A query modifier to exclude entities that have a specific component.
 * @param {Function|string} component - The component class or name to exclude.
 */
export class Not {
    constructor(component) {
        this.component = component;
    }
}

/**
 * @class Optional
 * @description A query modifier to include a component in the query result if it exists, without requiring it.
 * @param {Function|string} component - The component class or name to optionally include.
 */
export class Optional {
    constructor(component) {
        this.component = component;
    }
}

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
     * @param {number[]} id - The bitmask array representing the component signature of this archetype.
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
     * @returns {{movedEntityId: number|null, newIndex: number}} An object containing the ID of the entity that was moved to fill the gap, and the index it was moved to.
     */
    removeEntity(entityId) {
        const indexToRemove = this.entityMap.get(entityId);
        if (indexToRemove === undefined)
            return { movedEntityId: null, newIndex: -1 };

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
            return { movedEntityId: lastEntityId, newIndex: indexToRemove };
        }

        return { movedEntityId: null, newIndex: lastIndex };
    }
}

/**
 * @class QueryResult
 * @description A container for the results of a world query, providing an iterable interface and array-like methods.
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
     * @param {function({entity: number, components: Map<Function, object>}, number): void} callback - The function to execute for each entity.
     */
    forEach(callback) {
        let index = 0;
        for (const result of this) {
            callback(result, index++);
        }
    }

    /**
     * Creates a new array populated with the results of calling a provided function on every element in the query result.
     * @param {function({entity: number, components: Map<Function, object>}, number): any} callback - Function that is called for every element.
     * @returns {any[]} A new array with each element being the result of the callback function.
     */
    map(callback) {
        const results = [];
        let index = 0;
        for (const result of this) {
            results.push(callback(result, index++));
        }
        return results;
    }

    /**
     * Creates a new array with all elements that pass the test implemented by the provided function.
     * @param {function({entity: number, components: Map<Function, object>}, number): boolean} callback - Function to test each element. Return true to keep the element, false otherwise.
     * @returns {Array<{entity: number, components: Map<Function, object>}>} A new array with the elements that pass the test.
     */
    filter(callback) {
        const results = [];
        let index = 0;
        for (const result of this) {
            if (callback(result, index++)) {
                results.push(result);
            }
        }
        return results;
    }

    /**
     * Returns the first element in the query result that satisfies the provided testing function.
     * @param {function({entity: number, components: Map<Function, object>}, number): boolean} callback - Function to execute on each value.
     * @returns {{entity: number, components: Map<Function, object>}|undefined} The first element that satisfies the condition; otherwise, undefined.
     */
    find(callback) {
        let index = 0;
        for (const result of this) {
            if (callback(result, index++)) {
                return result;
            }
        }
        return undefined;
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
     * @returns {object} A new component instance.
     * @throws {Error} If the component definition doesn't exist.
     */
    create(name, initialValues) {
        const ComponentClass = this.definitions.get(name);
        if (!ComponentClass) {
            throw new Error(`Component with name "${name}" is not defined.`);
        }
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
        this.componentClassNames = new Map();
        this.nextComponentType = 0;

        this.archetypeTrie = new Map();
        this.entityArchetypeMap = new Map();
        this.queryCache = new Map();

        this.relationshipGraph = new Graph({
            type: graphType,
            allowSelfLoops: false,
        });
    }

    /**
     * Registers a component class with the world.
     * @param {Function} ComponentClass - The component class to register.
     */
    registerComponent(ComponentClass) {
        if (this.componentTypes.has(ComponentClass)) return;

        const index = this.nextComponentType;
        const wordIndex = Math.floor(index / 32);
        const bit = 1 << index % 32;

        this.componentTypes.set(ComponentClass, { index, wordIndex, bit });
        this.componentClasses.set(index, ComponentClass);
        this.componentClassNames.set(ComponentClass.name, ComponentClass);

        this.nextComponentType++;
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
     * Destroys an entity.
     * @param {number} entityId - The ID of the entity to destroy.
     */
    destroyEntity(entityId) {
        const archetype = this.entityArchetypeMap.get(entityId);
        if (archetype) {
            archetype.removeEntity(entityId);
            if (archetype.entityList.length === 0) {
                this._removeArchetypeFromTrie(archetype);
            }
        }
        this.entityArchetypeMap.delete(entityId);
        if (this.relationshipGraph.hasNode(entityId)) {
            this.relationshipGraph.dropNode(entityId);
        }
    }

    /**
     * Adds a component to an entity.
     * @param {number} entityId - The ID of the entity.
     * @param {object|string} componentOrName - The component instance or name.
     * @param {object} [initialValues={}] - Initial values if creating by name.
     */
    addComponent(entityId, componentOrName, initialValues = {}) {
        let component;
        if (typeof componentOrName === "string") {
            component = this.componentFactory.create(
                componentOrName,
                initialValues
            );
        } else {
            component = componentOrName;
        }

        const ComponentClass = component.constructor;
        if (!this.componentTypes.has(ComponentClass)) {
            this.registerComponent(ComponentClass);
        }

        const numWords = Math.ceil(this.nextComponentType / 32) || 1;
        const oldArchetype = this.entityArchetypeMap.get(entityId);
        const oldBitmask = oldArchetype
            ? [...oldArchetype.id]
            : new Array(numWords).fill(0);

        while (oldBitmask.length < numWords) {
            oldBitmask.push(0);
        }

        const { wordIndex, bit } = this.componentTypes.get(ComponentClass);

        if (oldArchetype && (oldBitmask[wordIndex] & bit) !== 0) {
            const index = oldArchetype.entityMap.get(entityId);
            oldArchetype.componentArrays.get(ComponentClass)[index] = component;
            return;
        }

        const newBitmask = [...oldBitmask];
        newBitmask[wordIndex] |= bit;

        const newArchetype = this._findOrCreateArchetype(newBitmask);

        const components = new Map();
        if (oldArchetype) {
            const index = oldArchetype.entityMap.get(entityId);
            for (const C of oldArchetype.componentClasses) {
                components.set(C, oldArchetype.componentArrays.get(C)[index]);
            }
            oldArchetype.removeEntity(entityId);
            if (oldArchetype.entityList.length === 0) {
                this._removeArchetypeFromTrie(oldArchetype);
            }
        }
        components.set(ComponentClass, component);

        newArchetype.addEntity(entityId, components);
        this.entityArchetypeMap.set(entityId, newArchetype);
    }

    /**
     * Removes a component from an entity.
     * @param {number} entityId - The ID of the entity.
     * @param {Function|string} componentClassOrName - The component class or name.
     */
    removeComponent(entityId, componentClassOrName) {
        const oldArchetype = this.entityArchetypeMap.get(entityId);
        if (!oldArchetype) return;

        const ComponentClass = this._getComponentClass(componentClassOrName);
        if (!ComponentClass) return;

        const componentInfo = this.componentTypes.get(ComponentClass);
        if (!componentInfo) return;

        const { wordIndex, bit } = componentInfo;

        if ((oldArchetype.id[wordIndex] & bit) === 0) {
            return;
        }

        const newBitmask = [...oldArchetype.id];
        newBitmask[wordIndex] &= ~bit;
        const newArchetype = this._findOrCreateArchetype(newBitmask);

        const components = new Map();
        const index = oldArchetype.entityMap.get(entityId);
        for (const C of oldArchetype.componentClasses) {
            if (C !== ComponentClass) {
                components.set(C, oldArchetype.componentArrays.get(C)[index]);
            }
        }

        oldArchetype.removeEntity(entityId);

        if (oldArchetype.entityList.length === 0) {
            this._removeArchetypeFromTrie(oldArchetype);
        }

        newArchetype.addEntity(entityId, components);
        this.entityArchetypeMap.set(entityId, newArchetype);
    }

    /**
     * Retrieves a component from an entity.
     * @param {number} entityId - The ID of the entity.
     * @param {Function|string} componentClassOrName - The component class or name.
     * @returns {object|undefined} The component instance.
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
     * Checks if an entity has a component.
     * @param {number} entityId - The ID of the entity.
     * @param {Function|string} componentClassOrName - The component class or name.
     * @returns {boolean}
     */
    hasComponent(entityId, componentClassOrName) {
        const archetype = this.entityArchetypeMap.get(entityId);
        if (!archetype) return false;

        const ComponentClass = this._getComponentClass(componentClassOrName);
        if (!ComponentClass) return false;

        const componentInfo = this.componentTypes.get(ComponentClass);
        if (!componentInfo) return false;

        const { wordIndex, bit } = componentInfo;
        if (wordIndex >= archetype.id.length) return false;

        return (archetype.id[wordIndex] & bit) !== 0;
    }

    /**
     * @private
     */
    _getComponentClass(componentClassOrName) {
        if (typeof componentClassOrName !== "string") {
            return componentClassOrName;
        }
        const dynamicClass =
            this.componentFactory.getClass(componentClassOrName);
        if (dynamicClass) return dynamicClass;

        return this.componentClassNames.get(componentClassOrName);
    }

    /**
     * @private
     */
    _findOrCreateArchetype(bitmask) {
        let currentNode = this.archetypeTrie;
        for (let i = 0; i < bitmask.length - 1; i++) {
            const word = bitmask[i];
            if (!currentNode.has(word)) {
                currentNode.set(word, new Map());
            }
            currentNode = currentNode.get(word);
        }

        const lastWord = bitmask[bitmask.length - 1];
        if (currentNode.has(lastWord)) {
            return currentNode.get(lastWord);
        }

        const componentClasses = [];
        for (let i = 0; i < this.nextComponentType; i++) {
            const ComponentClass = this.componentClasses.get(i);
            const { wordIndex, bit } = this.componentTypes.get(ComponentClass);
            if ((bitmask[wordIndex] & bit) !== 0) {
                componentClasses.push(ComponentClass);
            }
        }

        const newArchetype = new Archetype(bitmask, componentClasses);
        currentNode.set(lastWord, newArchetype);

        this.queryCache.clear();

        return newArchetype;
    }

    /**
     * Removes an archetype from the trie using its bitmask.
     * @param {Archetype} archetypeToRemove The archetype to remove.
     * @private
     */
    _removeArchetypeFromTrie(archetypeToRemove) {
        const bitmask = archetypeToRemove.id;
        if (!bitmask || bitmask.length === 0) return;

        let currentNode = this.archetypeTrie;
        // Traverse to the parent of the node to be removed
        for (let i = 0; i < bitmask.length - 1; i++) {
            const word = bitmask[i];
            if (!currentNode.has(word)) {
                // This case should ideally not be hit if logic is correct
                return;
            }
            currentNode = currentNode.get(word);
        }

        const lastWord = bitmask[bitmask.length - 1];
        if (currentNode.has(lastWord)) {
            currentNode.delete(lastWord);
            // Note: This doesn't prune empty parent Map objects from the trie.
            // This prevents a complex recursive cleanup, and the memory impact
            // of empty Maps is negligible compared to the Archetype objects.
            // The primary leak of holding onto Archetype objects is solved.
        }
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
        return this.relationshipGraph.outNeighbors(entityId).map(Number);
    }

    getParents(entityId) {
        return this.relationshipGraph.inNeighbors(entityId).map(Number);
    }

    getConnections(entityId) {
        return this.relationshipGraph.neighbors(entityId).map(Number);
    }

    getConnectionsWithDetails(entityId) {
        const connections = [];
        this.relationshipGraph.forEachEdge(
            entityId,
            (edge, attributes, source, target) => {
                const neighborId = source == entityId ? target : source;
                connections.push({
                    neighbor: Number(neighborId),
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

    registerSystem(system) {
        this.systems.push(system);
    }

    /**
     * Queries the world for entities that have a given set of components.
     * Supports `Not` and `Optional` modifiers.
     * @param {Array<Function|string|Not|Optional>} queryParts - The components to query for.
     * @returns {Archetype[]} The archetypes that match the query.
     */
    query(queryParts) {
        // --- IMPLEMENTATION CHANGED ---
        // Create a stable cache key based on component names and modifiers.
        const cacheKey = queryParts
            .map((c) => {
                if (c instanceof Not) {
                    const comp = c.component;
                    return `!${typeof comp === "string" ? comp : comp.name}`;
                }
                if (c instanceof Optional) {
                    const comp = c.component;
                    // Optional is handled by the system, not the query itself, so no special prefix needed for the cache key.
                    return `${typeof comp === "string" ? comp : comp.name}`;
                }
                return typeof c === "string" ? c : c.name;
            })
            .sort()
            .join(",");

        if (this.queryCache.has(cacheKey)) {
            return this.queryCache.get(cacheKey);
        }

        const matchingArchetypes = this._performQuery(queryParts);
        this.queryCache.set(cacheKey, matchingArchetypes);
        return matchingArchetypes;
    }

    /**
     * @private
     */
    _performQuery(queryParts) {
        // --- IMPLEMENTATION CHANGED ---
        const numWords = Math.ceil(this.nextComponentType / 32) || 1;
        const requiredBitmask = new Array(numWords).fill(0);
        const excludeBitmask = new Array(numWords).fill(0);

        // First, parse the query parts to build the required and exclusion bitmasks.
        for (const part of queryParts) {
            let ComponentClass;
            let isExclusion = false;

            if (part instanceof Not) {
                ComponentClass = this._getComponentClass(part.component);
                isExclusion = true;
            } else if (part instanceof Optional) {
                // Optional components don't affect archetype matching, so we skip them here.
                continue;
            } else {
                ComponentClass = this._getComponentClass(part);
            }

            if (ComponentClass && this.componentTypes.has(ComponentClass)) {
                const { wordIndex, bit } =
                    this.componentTypes.get(ComponentClass);
                if (isExclusion) {
                    excludeBitmask[wordIndex] |= bit;
                } else {
                    requiredBitmask[wordIndex] |= bit;
                }
            } else {
                // If a required component (not Not or Optional) isn't registered,
                // the query can never find any matches.
                if (!(part instanceof Not) && !(part instanceof Optional)) {
                    return [];
                }
            }
        }

        const matchingArchetypes = [];
        // The traverse function recursively explores the archetype trie.
        const traverse = (node) => {
            for (const value of node.values()) {
                if (value instanceof Archetype) {
                    let isMatch = true;
                    for (let i = 0; i < requiredBitmask.length; i++) {
                        // An archetype is a match if:
                        // 1. It has all the bits from the requiredBitmask.
                        // 2. It has none of the bits from the excludeBitmask.
                        if (
                            (value.id[i] & requiredBitmask[i]) !==
                                requiredBitmask[i] ||
                            (value.id[i] & excludeBitmask[i]) !== 0
                        ) {
                            isMatch = false;
                            break;
                        }
                    }
                    if (isMatch) {
                        matchingArchetypes.push(value);
                    }
                } else if (value instanceof Map) {
                    // Continue traversing if it's another node in the trie.
                    traverse(value);
                }
            }
        };

        traverse(this.archetypeTrie);
        return matchingArchetypes;
    }

    update(deltaTime) {
        for (const system of this.systems) {
            system.update(this, deltaTime);
        }
    }

    // --- Serialization methods ---

    toJSON() {
        const allArchetypes = [];
        const traverse = (node) => {
            for (const value of node.values()) {
                if (value instanceof Archetype) {
                    allArchetypes.push(value);
                } else if (value instanceof Map) {
                    traverse(value);
                }
            }
        };
        traverse(this.archetypeTrie);

        const entities = [];
        for (const archetype of allArchetypes) {
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

    static fromJSON(json, { systems = [], staticComponents = [] } = {}) {
        const world = new World({
            graphType: json.graph?.options?.type || "mixed",
        });

        for (const ComponentClass of staticComponents) {
            world.registerComponent(ComponentClass);
        }
        for (const def of json.componentDefinitions) {
            world.componentFactory.define(def.name, def.schema);
        }

        world.nextEntityID = json.nextEntityID;

        if (json.graph) {
            world.relationshipGraph.import(json.graph);
        }

        for (const entityData of json.entities) {
            const entityId = entityData.id;

            if (!world.relationshipGraph.hasNode(entityId)) {
                console.warn(
                    `Serialized entity ${entityId} not found in graph, creating it.`
                );
                world.relationshipGraph.addNode(entityId);
            }

            let finalBitmask = new Array(
                Math.ceil(world.nextComponentType / 32) || 1
            ).fill(0);
            const componentsMap = new Map();

            for (const componentData of entityData.components) {
                const ComponentClass = world._getComponentClass(
                    componentData.type
                );
                if (ComponentClass) {
                    const componentInstance = new ComponentClass();
                    Object.assign(componentInstance, componentData.data);
                    componentsMap.set(ComponentClass, componentInstance);

                    const { wordIndex, bit } =
                        world.componentTypes.get(ComponentClass);
                    finalBitmask[wordIndex] |= bit;
                } else {
                    console.error(
                        `Could not find component class for type "${componentData.type}" during deserialization.`
                    );
                }
            }

            if (componentsMap.size > 0) {
                const targetArchetype =
                    world._findOrCreateArchetype(finalBitmask);
                targetArchetype.addEntity(entityId, componentsMap);
                world.entityArchetypeMap.set(entityId, targetArchetype);
            }
        }

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
    /**
     * @param {Array<string|Function|Not|Optional>} [queryParts=[]] - Components this system operates on.
     */
    constructor(queryParts = []) {
        this.queryParts = queryParts;
    }

    /**
     * Called by the world on each update tick.
     * @param {World} world - The world instance.
     * @param {number} deltaTime - The time elapsed since the last update.
     */
    update(world, deltaTime) {
        // world.query returns an array of matching archetypes.
        const matchingArchetypes = world.query(this.queryParts);
        if (matchingArchetypes.length > 0) {
            // We construct the QueryResult here, inside the system.
            const queryResult = new QueryResult(matchingArchetypes);
            this.execute(queryResult, world, deltaTime);
        }
    }

    /**
     * The main logic of the system. This method must be implemented by subclasses.
     * @param {QueryResult} entities - The result of the query.
     * @param {World} world - The world instance.
     * @param {number} deltaTime - The time elapsed since the last update.
     */
    execute(entities, world, deltaTime) {
        throw new Error("System must implement an execute method.");
    }
}
