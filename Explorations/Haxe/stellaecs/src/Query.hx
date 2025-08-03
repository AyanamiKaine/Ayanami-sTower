package;

import haxe.DynamicAccess;

typedef QueryResult = {
    entity:Entity,
    components:DynamicAccess<Any>
}

class Query {
    public var world:World;
    public var _with:Array<Class<Any>>;
    public var _without:Array<Class<Any>>;
    public var _optional:Array<Class<Any>>;
    public var _predicates:Array<QueryResult -> Bool>;
    
    public function new(world:World) {
        this.world = world;
        this._with = [];
        this._without = [];
        this._optional = [];
        this._predicates = [];
    }
    
    public function with<T>(componentClass:Class<T>):Query {
        _with.push(cast componentClass);
        return this;
    }
    
    public function without<T>(componentClass:Class<T>):Query {
        _without.push(cast componentClass);
        return this;
    }
    
    public function optional<T>(componentClass:Class<T>):Query {
        _optional.push(cast componentClass);
        return this;
    }
    
    public function where(predicate:QueryResult -> Bool):Query {
        _predicates.push(predicate);
        return this;
    }
    
    public function iterator():Iterator<QueryResult> {
        return new QueryIterator(this);
    }
    
    // Internal access for iterator
    @:allow(ecs.QueryIterator)
    public function getWith():Array<Class<Any>> return _with;
    
    @:allow(ecs.QueryIterator)
    public function getWithout():Array<Class<Any>> return _without;
    
    @:allow(ecs.QueryIterator)
    public function getOptional():Array<Class<Any>> return _optional;
    
    @:allow(ecs.QueryIterator)
    public function getPredicates():Array<QueryResult -> Bool> return _predicates;
}

class QueryIterator {
    public var query:Query;
    public var world:World;
    public var entityIterator:Iterator<Int>;
    public var nextResult:QueryResult;
    public var hasNextResult:Bool = false;
    
    public function new(query:Query) {
        this.query = query;
        this.world = query.world;
        
        var withComponents = query.getWith();
        
        // Ensure there's at least one 'with' component
        if (withComponents.length == 0) {
            trace("Query must have at least one 'with' component to be efficient. Consider adding one.");
            entityIterator = [].iterator();
            return;
        }
        
        // Get all component storages required by the query
        var withStorages:Array<ComponentStorage<Any>> = [];
        for (componentClass in withComponents) {
            var storage = world.getComponentStorage(componentClass);
            if (storage != null) {
                withStorages.push(storage);
            }
        }
        
        // If a required storage doesn't exist, the query can't return anything
        if (withStorages.length != withComponents.length) {
            entityIterator = [].iterator();
            return;
        }
        
        // Find the smallest component storage to iterate over for efficiency
        var smallestStorage = withStorages[0];
        for (i in 1...withStorages.length) {
            if (withStorages[i].size < smallestStorage.size) {
                smallestStorage = withStorages[i];
            }
        }
        
        entityIterator = smallestStorage.entityIds();
        findNext();
    }
    
    public function findNext():Void {
        hasNextResult = false;
        
        while (entityIterator.hasNext()) {
            var entityId = entityIterator.next();
            var entity = world.getEntity(entityId);
            
            if (entity == null) continue;
            
            // Check 'with' conditions
            var hasAllWith = true;
            for (componentClass in query.getWith()) {
                var storage = world.getComponentStorage(componentClass);
                if (storage == null || !storage.has(entity)) {
                    hasAllWith = false;
                    break;
                }
            }
            if (!hasAllWith) continue;
            
            // Check 'without' conditions
            var hasAnyWithout = false;
            for (componentClass in query.getWithout()) {
                var storage = world.getComponentStorage(componentClass);
                if (storage != null && storage.has(entity)) {
                    hasAnyWithout = true;
                    break;
                }
            }
            if (hasAnyWithout) continue;
            
            // Build the result object
            var result:QueryResult = {
                entity: entity,
                components: {}
            };
            
            // Add 'with' components to the result
            for (componentClass in query.getWith()) {
                var componentName = Type.getClassName(componentClass).split('.').pop().toLowerCase();
                result.components[componentName] = world.get(entity, componentClass);
            }
            
            // Add 'optional' components to the result
            for (componentClass in query.getOptional()) {
                var componentName = Type.getClassName(componentClass).split('.').pop().toLowerCase();
                result.components[componentName] = world.get(entity, componentClass);
            }
            
            // Apply 'where' predicates
            var passesPredicates = true;
            for (predicate in query.getPredicates()) {
                if (!predicate(result)) {
                    passesPredicates = false;
                    break;
                }
            }
            if (!passesPredicates) continue;
            
            // Found a valid result
            nextResult = result;
            hasNextResult = true;
            break;
        }
    }
    
    public function hasNext():Bool {
        return hasNextResult;
    }
    
    public function next():QueryResult {
        if (!hasNextResult) {
            throw "No more elements";
        }
        
        var result = nextResult;
        findNext();
        return result;
    }
}