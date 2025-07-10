import Graph from 'graphology';

/**
 * @class Archetype
 * @description Represents a unique combination of component types. Stores component data in contiguous arrays for cache-friendly iteration.
 * @private
 */
class Archetype {
    constructor(id, componentClasses) {
        this.id = id; // The bitmask representing the component combination
        this.componentClasses = componentClasses; // Array of component constructors

        // The core data store: Map<ComponentClass, Array<ComponentInstance>>
        this.componentArrays = new Map(componentClasses.map(C => [C, []]));

        // Map<EntityID, index_in_arrays>
        this.entityMap = new Map();
        // Array<EntityID> - allows finding entity ID by index
        this.entityList = [];
    }

    /**
     * Adds an entity and its components to this archetype's arrays.
     * @param {number} entityId - The ID of the entity.
     * @param {Map<Function, object>} components - A map of the entity's component instances.
     */
    addEntity(entityId, components) {
        const index = this.entityList.length;
        this.entityMap.set(entityId, index);
        this.entityList.push(entityId);

        for (const ComponentClass of this.componentClasses) {
            this.componentArrays.get(ComponentClass).push(components.get(ComponentClass));
        }
    }

    /**
     * Removes an entity from this archetype using the efficient "swap and pop" method.
     * @param {number} entityId - The ID of the entity to remove.
     * @returns {{movedEntityId: number|null, oldIndex: number}} Information about the entity that was moved to fill the gap.
     */
    removeEntity(entityId) {
        const indexToRemove = this.entityMap.get(entityId);
        const lastIndex = this.entityList.length - 1;
        const lastEntityId = this.entityList[lastIndex];

        // Swap the last element into the place of the one being removed
        for (const ComponentClass of this.componentClasses) {
            const array = this.componentArrays.get(ComponentClass);
            array[indexToRemove] = array[lastIndex];
            array.pop();
        }

        this.entityList[indexToRemove] = lastEntityId;
        this.entityList.pop();

        this.entityMap.delete(entityId);

        // Important: If we removed an entity that wasn't the last one, we need to update the moved entity's index
        if (indexToRemove !== lastIndex) {
            this.entityMap.set(lastEntityId, indexToRemove);
            return { movedEntityId: lastEntityId, oldIndex: lastIndex };
        }

        return { movedEntityId: null, oldIndex: lastIndex };
    }
}

/**
 * @class ComponentFactory
 * @description Manages the definition and creation of components from string names and JSON schemas.
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
        console.log(`Component '${name}' defined.`);
        return DynamicComponent;
    }
    create(name, initialValues) {
        const ComponentClass = this.definitions.get(name);
        if (!ComponentClass) {
            console.error(`Component type "${name}" not defined.`);
            return null;
        }
        return new ComponentClass(initialValues);
    }
    getClass(name) {
        return this.definitions.get(name);
    }
}

/**
 * @class World
 * @description The main container for entities, components, and systems, using an archetype-based architecture.
 */
export class World {
    constructor() {
        this.nextEntityID = 0;
        this.systems = [];
        this.componentFactory = new ComponentFactory(this);

        // Component registration
        this.componentTypes = new Map();
        this.componentClasses = new Map(); // Map<bit, Class>
        this.nextComponentType = 0;

        // Archetype management
        this.archetypes = new Map(); // Map<bitmask, Archetype>
        this.entityArchetypeMap = new Map(); // Map<EntityID, Archetype>

        // Graphology for entity relationships
        this.relationshipGraph = new Graph({ type: 'directed', allowSelfLoops: false });
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

    createEntity() {
        const id = this.nextEntityID++;
        this.relationshipGraph.addNode(id);
        return id;
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

        // Add to new archetype
        newArchetype.addEntity(entityId, components);
        this.entityArchetypeMap.set(entityId, newArchetype);
    }

    getComponent(entityId, componentClassOrName) {
        const archetype = this.entityArchetypeMap.get(entityId);
        if (!archetype) return undefined;

        const ComponentClass = typeof componentClassOrName === 'string'
            ? this.getComponentClassByName(componentClassOrName)
            : componentClassOrName;
        if (!ComponentClass || !archetype.componentArrays.has(ComponentClass)) return undefined;

        const index = archetype.entityMap.get(entityId);
        return archetype.componentArrays.get(ComponentClass)[index];
    }

    getComponentClassByName(name) {
        const dynamicClass = this.componentFactory.getClass(name);
        if (dynamicClass) return dynamicClass;
        for (const componentClass of this.componentTypes.keys()) {
            if (componentClass.name === name) {
                return componentClass;
            }
        }
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

    registerSystem(system) {
        this.systems.push(system);
        console.log(`System '${system.constructor.name}' registered.`);
    }

    query(componentClassesOrNames) {
        let queryBitmask = 0;
        for (const classOrName of componentClassesOrNames) {
            const ComponentClass = typeof classOrName === 'string'
                ? this.getComponentClassByName(classOrName)
                : classOrName;
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
        console.log(`--- World Update (Δt: ${deltaTime.toFixed(3)}s) ---`);
        for (const system of this.systems) {
            system.update(this, deltaTime);
        }
    }

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
            // Create a dummy instance to get the schema
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

    /**
     * Deserializes world state from a JSON object.
     * @param {object} json - The JSON object to deserialize from.
     * @param {object} options - Options for deserialization.
     * @param {System[]} [options.systems=[]] - An array of system instances to register with the new world.
     * @param {Function[]} [options.staticComponents=[]] - An array of static component classes to register.
     * @returns {World} A new World instance populated with the deserialized state.
     */
    static fromJSON(json, { systems = [], staticComponents = [] } = {}) {
        const world = new World();
        world.nextEntityID = json.nextEntityID;

        // Register all known static components first.
        for (const ComponentClass of staticComponents) {
            world.registerComponent(ComponentClass);
        }

        // Define all dynamic components
        for (const def of json.componentDefinitions) {
            world.componentFactory.define(def.name, def.schema);
        }

        // Create all entities and add their components efficiently
        for (const entityData of json.entities) {
            // Ensure entity IDs are created in sequence
            while (world.nextEntityID <= entityData.id) {
                world.createEntity();
            }

            let finalBitmask = 0;
            const componentsMap = new Map();

            for (const componentData of entityData.components) {
                const ComponentClass = world.getComponentClassByName(componentData.type);
                if (ComponentClass) {
                    // THIS IS THE FIX: Create a default instance, then apply the saved data.
                    // This works for any constructor signature.
                    const componentInstance = new ComponentClass();
                    Object.assign(componentInstance, componentData.data);

                    componentsMap.set(ComponentClass, componentInstance);
                    finalBitmask |= world.componentTypes.get(ComponentClass);
                } else {
                    console.error(`Could not find component class for type "${componentData.type}" during deserialization.`);
                }
            }

            if (finalBitmask > 0) {
                const targetArchetype = world._findOrCreateArchetype(finalBitmask);
                targetArchetype.addEntity(entityData.id, componentsMap);
                world.entityArchetypeMap.set(entityData.id, targetArchetype);
            }
        }

        // Import graph data
        if (json.graph) {
            world.relationshipGraph.import(json.graph);
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
 * @description Base class for all systems. Contains the logic that operates on entities.
 */
export class System {
    constructor(queryComponentNames = []) {
        this.queryComponentNames = queryComponentNames;
    }

    update(world, deltaTime) {
        const archetypes = world.query(this.queryComponentNames);
        if (archetypes.length > 0) {
            this.execute(world, archetypes, deltaTime);
        }
    }

    execute(world, archetypes, deltaTime) {
        throw new Error("System must implement an execute method.");
    }
}
