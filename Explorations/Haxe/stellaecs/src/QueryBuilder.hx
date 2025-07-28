/**
 * A fluent builder for creating complex entity queries.
 * This class is the entry point for defining which components an entity must have or must not have.
 */

import RelationshipStorage.IRelationship;
import IFilter.TypedFilter;

class QueryBuilder {
    private var _world:World;
    private var _withTypes:Array<Class<Dynamic>>;
    private var _withoutTypes:Array<Class<Dynamic>>;
    private var _filters:Array<IFilter>;
    private var _withRelationships:Array<{type:Class<Dynamic>, target:Entity}>;
    
    public function new(world:World) {
        _world = world;
        _withTypes = [];
        _withoutTypes = [];
        _filters = [];
        _withRelationships = [];
    }
    
    /**
     * Specifies that the query should only include entities that have a relationship 
     * of type T pointing to a specific target entity.
     */
    public function withRelationship<T:IRelationship>(relationshipClass:Class<T>, target:Entity):QueryBuilder {
        // Also add the relationship type as a 'With' type
        _withTypes.push(cast relationshipClass);
        _withRelationships.push({type: cast relationshipClass, target: target});
        return this;
    }
    
    /**
     * Specifies that the query should only include entities that have a component of type T.
     */
    public function with<T>(componentClass:Class<T>):QueryBuilder {
        return withType(cast componentClass);
    }
    
    /**
     * Specifies that the query should exclude any entities that have a component of type T.
     */
    public function without<T>(componentClass:Class<T>):QueryBuilder {
        return withoutType(cast componentClass);
    }
    
    /**
     * Specifies that the query should only include entities that have a component of the given type.
     */
    public function withType(componentClass:Class<Dynamic>):QueryBuilder {
        var instance = Type.createInstance(componentClass, []);
        if (!Std.isOfType(instance, IRelationship)) {
            _withTypes.push(componentClass);
        }
        return this;
    }
    
    /**
     * Specifies that the query should exclude any entities that have a component of the given type.
     */
    public function withoutType(componentClass:Class<Dynamic>):QueryBuilder {
        _withoutTypes.push(componentClass);
        return this;
    }
    
    /**
     * Adds a data-based filter to the query for a specific component type.
     * The query will only return entities where the component's data satisfies the predicate.
     * This automatically adds a 'with' condition if it doesn't already exist.
     */
    public function where<T>(componentClass:Class<T>, predicate:T->Bool):QueryBuilder {
        // A 'where' clause implies a 'with' clause.
        var found = false;
        for (type in _withTypes) {
            if (type == componentClass) {
                found = true;
                break;
            }
        }
        if (!found) {
            with(componentClass);
        }
        
        _filters.push(new TypedFilter(componentClass, predicate));
        return this;
    }
    
    /**
     * Constructs the final, immutable query object based on the builder's configuration.
     */
    public function build():QueryEnumerable {
        if (_withTypes.length == 0) {
            throw "A query must have at least one 'With' component specified.";
        }
        
        return new QueryEnumerable(_world, _withTypes, _withoutTypes, _filters, _withRelationships);
    }
}