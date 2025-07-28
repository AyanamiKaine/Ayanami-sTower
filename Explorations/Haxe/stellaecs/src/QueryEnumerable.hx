/**
 * Represents a compiled, ready-to-use query that can be iterated over.
 * This class is designed to be used in a for loop for high-performance iteration.
 */

import RelationshipStorage.IRelationship;

class QueryEnumerable {
    private var _world:World;
    private var _driverEntities:Array<Entity>;
    private var _otherWithStorages:Array<IComponentStorage>;
    private var _withoutStorages:Array<IComponentStorage>;
    private var _filters:Array<IFilter>;
    private var _withRelationships:Array<{type:Class<Dynamic>, target:Entity}>;
    
    public function new(
        world:World, 
        withTypes:Array<Class<Dynamic>>, 
        withoutTypes:Array<Class<Dynamic>>, 
        filters:Array<IFilter>, 
        withRelationships:Array<{type:Class<Dynamic>, target:Entity}>
    ) {
        _world = world;
        _filters = filters.length > 0 ? filters : null;
        _withRelationships = withRelationships.length > 0 ? withRelationships : null;
        
        // --- 1. Identify Potential Query Drivers ---
        // The core optimization is to iterate over the smallest possible set of entities.
        
        // Potential Driver A: Find the smallest component storage.
        // Filter out IRelationship types from the main withTypes list
        var componentStorages = new Array<IComponentStorage>();
        for (type in withTypes) {
            // Check if it's not a relationship type
            var instance = Type.createInstance(type, []);
            if (!Std.isOfType(instance, IRelationship)) {
                try {
                    var storage = world.getStorageUnsafe(type);
                    componentStorages.push(storage);
                } catch (e:Dynamic) {
                    // Storage doesn't exist, skip
                }
            }
        }
        
        // Sort by count to find smallest
        componentStorages.sort((a, b) -> a.count - b.count);
        var componentDriver = componentStorages.length > 0 ? componentStorages[0] : null;
        
        // Potential Driver B: Find the smallest list of relationship sources.
        var relationshipDriver:Array<Entity> = null;
        if (_withRelationships != null) {
            for (withRel in _withRelationships) {
                var storage = world.getRelationshipStorageUnsafe(withRel.type);
                var sources = storage.getSources(withRel.target);
                if (relationshipDriver == null || sources.length < relationshipDriver.length) {
                    relationshipDriver = sources;
                }
            }
        }
        
        // --- 2. Choose the Best Driver and Configure the Query ---
        if (componentDriver == null && relationshipDriver == null) {
            throw "A query must have at least one 'With' component or a targeted 'With' relationship specified.";
        }
        
        // Case 1: The component storage is the best (or only) driver.
        if (componentDriver != null && (relationshipDriver == null || componentDriver.count <= relationshipDriver.length)) {
            var driverEntityIds = componentDriver.getPackedEntities();
            var entities = new Array<Entity>();
            for (id in driverEntityIds) {
                entities.push(world.getEntityFromId(id));
            }
            _driverEntities = entities;
            
            // The remaining component storages become secondary checks.
            componentStorages.remove(componentDriver);
            _otherWithStorages = componentStorages.length > 0 ? componentStorages : null;
        }
        // Case 2: The relationship source list is the best driver.
        else {
            _driverEntities = relationshipDriver.copy();
            // Since the relationship is the driver, ALL component storages become secondary checks.
            _otherWithStorages = componentStorages.length > 0 ? componentStorages : null;
        }
        
        // --- 3. Configure 'Without' and 'Filter' Checks ---
        if (withoutTypes.length > 0) {
            var withoutStorages = new Array<IComponentStorage>();
            for (type in withoutTypes) {
                // Filter out relationship types
                var instance = Type.createInstance(type, []);
                if (!Std.isOfType(instance, IRelationship)) {
                    try {
                        var storage = world.getStorageUnsafe(type);
                        withoutStorages.push(storage);
                    } catch (e:Dynamic) {
                        // Storage doesn't exist, skip
                    }
                }
            }
            _withoutStorages = withoutStorages.length > 0 ? withoutStorages : null;
        } else {
            _withoutStorages = null;
        }
    }
    
    /**
     * Returns an iterator for the query results.
     */
    public function iterator():QueryIterator {
        return new QueryIterator(_world, _driverEntities, _otherWithStorages, _withoutStorages, _filters, _withRelationships);
    }
}