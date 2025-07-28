/**
 * Stores components of type T using a sparse set for efficient access and cache-friendly iteration.
 */
class ComponentStorage<T> implements IComponentStorage {
    // The same arrays as before for the sparse set logic
    private var _dense:Array<Int>;
    private var _sparse:Array<Int>;
    
    // The parallel array to store the actual component data
    private var _components:Array<T>;
    
    private var _count:Int = 0;
    private var _capacity:Int;
    private var _universeSize:Int;
    
    /**
     * Gets the current number of components stored in this storage.
     */
    public var count(get, never):Int;
    private function get_count():Int return _count;
    
    /**
     * Gets the maximum number of components that can be stored in this storage.
     */
    public var capacity(get, never):Int;
    private function get_capacity():Int return _capacity;
    
    /**
     * Gets the maximum number of entities supported by this storage.
     */
    public var universeSize(get, never):Int;
    private function get_universeSize():Int return _universeSize;
    
    /**
     * Constructor
     */
    public function new(capacity:Int, universeSize:Int) {
        this._capacity = capacity;
        this._universeSize = universeSize;
        
        // Initialize arrays with proper sizes
        this._dense = [];
        this._dense.resize(capacity);
        
        this._sparse = [];
        this._sparse.resize(universeSize);
        
        this._components = [];
        this._components.resize(capacity);
        
        // Initialize sparse array to invalid values
        for (i in 0...universeSize) {
            _sparse[i] = -1;
        }
    }
    
    /**
     * Gets a copy of the packed component data for efficient iteration.
     * Note: Haxe doesn't have Span<T>, so we return a slice of the array
     */
    public function getPackedComponents():Array<T> {
        return _components.slice(0, _count);
    }
    
    /**
     * Gets a copy of the packed entity IDs for efficient iteration.
     */
    public function getPackedEntities():Array<Int> {
        return _dense.slice(0, _count);
    }
    
    /**
     * Adds a component of type T for the specified entity ID.
     */
    public function add(entityId:Int, component:T):Void {
        if (_count >= _capacity || entityId < 0 || entityId >= _universeSize || has(entityId)) {
            return;
        }
        
        // The logic is identical, we just add one more step
        _dense[_count] = entityId;
        _sparse[entityId] = _count;
        _components[_count] = component; // The new step!
        
        _count++;
    }
    
    /**
     * Removes the component of type T for the specified entity ID, if it exists.
     */
    public function remove(entityId:Int):Void {
        if (!has(entityId)) {
            return;
        }
        
        var indexOfEntity = _sparse[entityId];
        var lastEntity = _dense[_count - 1];
        var lastComponent = _components[_count - 1]; // Get the last component
        
        // Perform the swap on all parallel arrays
        _dense[indexOfEntity] = lastEntity;
        _components[indexOfEntity] = lastComponent; // The new step!
        _sparse[lastEntity] = indexOfEntity;
        
        // Mark the removed entity as invalid in sparse array
        _sparse[entityId] = -1;
        
        _count--;
    }
    
    /**
     * Determines whether a component of type T exists for the specified entity ID.
     */
    public function has(entityId:Int):Bool {
        if (entityId < 0 || entityId >= _universeSize) return false;
        var indexInDense = _sparse[entityId];
        return indexInDense >= 0 && indexInDense < _count && _dense[indexInDense] == entityId;
    }
    
    /**
     * Gets the component of type T for the specified entity ID.
     * Note: Haxe doesn't have ref returns, so this returns a copy.
     * Warning: This assumes you've already checked has(entityId)
     */
    public function getComponent(entityId:Int):T {
        return _components[_sparse[entityId]];
    }
    
    /**
     * Adds a new component or updates an existing one for the specified entity.
     * This provides a convenient "upsert" (update or insert) operation.
     */
    public function set(entityId:Int, component:T):Void {
        if (has(entityId)) {
            // Entity already has the component, so update it in-place.
            _components[_sparse[entityId]] = component;
        } else {
            // Entity doesn't have the component, so add it.
            // The add method already contains all the necessary checks (capacity, bounds, etc.).
            add(entityId, component);
        }
    }
    
    /**
     * Gets the component for the specified entity ID.
     * This is the preferred method for safe, high-performance read-only access.
     * Warning: For maximum performance, this method does not perform a has() check. 
     * The caller is responsible for ensuring the entity has the component before calling.
     */
    public function get(entityId:Int):T {
        return _components[_sparse[entityId]];
    }
    
    /**
     * Gets a reference to the component for the specified entity ID for modification.
     * Since Haxe doesn't have ref returns, this returns the component value.
     * To modify, you need to call set() after making changes.
     * Warning: For maximum performance, this method does not perform a has() check.
     */
    public function getMutable(entityId:Int):T {
        return _components[_sparse[entityId]];
    }
    
    /**
     * Tries to get the component for a specified entity.
     * This method is safer than get() or getMutable() as it performs all necessary checks.
     * Returns null if the entity doesn't have the component.
     */
    public function tryGetValue(entityId:Int):Null<T> {
        if (has(entityId)) {
            return _components[_sparse[entityId]];
        }
        return null;
    }
    
    // --- Runtime/Non-Generic API Implementation ---
    
    /**
     * Sets a component using Dynamic typing (for interface compatibility)
     */
    public function setAsObject(entityId:Int, componentData:Dynamic):Void {
        set(entityId, cast componentData);
    }
    
    /**
     * Gets a component as Dynamic (for interface compatibility)
     */
    public function getAsObject(entityId:Int):Dynamic {
        return get(entityId);
    }
}