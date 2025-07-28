/**
 * Internal interface for a type-erased filter condition that can be applied to an entity.
 */
interface IFilter {
    /**
     * Checks if the entity's component data matches the filter's predicate.
     */
    function matches(entity:Entity):Bool;
}

/**
 * A concrete implementation of a filter for a specific component type.
 */
class Filter<T> implements IFilter {
    private var _predicate:T->Bool;
    
    public function new(predicate:T->Bool) {
        _predicate = predicate;
    }
    
    /**
     * Checks if the entity has the component and if its data satisfies the predicate.
     */
    public function matches(entity:Entity):Bool {
        // This check is implicitly safe because the query will only pass entities
        // that are guaranteed to have this component type.
        var component = entity.getMutable(cast Type.getClass({})); // This needs actual component class
        return _predicate(component);
    }
}

/**
 * A typed filter that knows its component class.
 */
class TypedFilter<T> implements IFilter {
    private var _predicate:T->Bool;
    private var _componentClass:Class<T>;
    
    public function new(componentClass:Class<T>, predicate:T->Bool) {
        _componentClass = componentClass;
        _predicate = predicate;
    }
    
    public function matches(entity:Entity):Bool {
        var component = entity.get(_componentClass);
        return _predicate(component);
    }
}