import RelationshipStorage.IBidirectionalRelationship;
import RelationshipStorage.IRelationship;
import RelationshipStorage.IRelationshipStorage;
import haxe.ds.Map;
import haxe.ds.List;

/**
 * The central class in the ECS framework that manages all entities and their components.
 * It handles the entity lifecycle and provides a unified API for component manipulation.
 */
class World {
    // --- Fields ---
    private var _relationshipStorages:Map<String, IRelationshipStorage>;
    private var _maxEntities:Int;
    private var _nextEntityId:Int = 0;
    
    // A list for recycling entity IDs to keep the set of IDs compact.
    private var _recycledEntityIds:List<Int>;
    
    // The core of the World: A map from component class name to its storage object.
    private var _componentStorages:Map<String, IComponentStorage>;
    
    /**
     * Stores the current generation for each entity ID.
     */
    private var _entityGenerations:Array<Int>;
    
    // --- Constructor ---
    
    /**
     * Initializes a new instance of the World class.
     */
    public function new(maxEntities:Int = 1000000) {
        if (maxEntities <= 0) throw "Maximum entities must be positive.";
        
        _maxEntities = maxEntities;
        _recycledEntityIds = new List<Int>();
        _componentStorages = new Map<String, IComponentStorage>();
        _relationshipStorages = new Map<String, IRelationshipStorage>();
        
        // Initialize the generations array
        _entityGenerations = [];
        _entityGenerations.resize(maxEntities);
        for (i in 0...maxEntities) {
            _entityGenerations[i] = 0;
        }
    }
    
    // --- Entity Lifecycle Management ---
    
    /**
     * Creates a new entity and returns its unique handle.
     * It will reuse a destroyed entity ID if one is available.
     */
    public function createEntity():Entity {
        if (!_recycledEntityIds.isEmpty()) {
            var id = _recycledEntityIds.pop();
            return new Entity(id, _entityGenerations[id], this);
        }
        
        if (_nextEntityId >= _maxEntities) {
            throw "Cannot create new entity: World has reached maximum capacity.";
        }
        
        var newId = _nextEntityId++;
        // A brand new entity starts with generation 1
        _entityGenerations[newId] = 1;
        return new Entity(newId, _entityGenerations[newId], this);
    }
    
    /**
     * Checks if an entity handle is "alive" and valid.
     */
    public function isAlive(entity:Entity):Bool {
        // Check if the ID is valid and the generation matches.
        return entity.id >= 0 
            && entity.id < _maxEntities 
            && _entityGenerations[entity.id] == entity.generation;
    }
    
    /**
     * Destroys an entity and removes all of its associated components from all storages.
     */
    public function destroyEntity(entity:Entity):Void {
        // Only destroy if the handle is valid and alive
        if (!isAlive(entity)) return;
        
        // Remove all components
        for (storage in _componentStorages) {
            storage.remove(entity.id);
        }
        
        // Clean up all relationships involving this entity
        onDestroyEntity(entity);
        
        // Invalidate the handle by incrementing the generation and recycle the ID.
        _entityGenerations[entity.id]++;
        _recycledEntityIds.add(entity.id);
    }
    
    /**
     * Gets a full Entity handle from a raw entity ID.
     */
    public function getEntityFromId(entityId:Int):Entity {
        if (entityId < 0 || entityId >= _maxEntities) return Entity.Null;
        return new Entity(entityId, _entityGenerations[entityId], this);
    }
    
    // --- Component Management ---
    
    /**
     * Registers a component type with the world, creating its underlying storage.
     */
    public function registerComponent<T>(componentClass:Class<T>, ?capacity:Int):Void {
        var className = Type.getClassName(componentClass);
        if (_componentStorages.exists(className)) {
            return;
        }
        
        var storageCapacity = capacity != null ? capacity : _maxEntities;
        var newStorage = new ComponentStorage<T>(storageCapacity, _maxEntities);
        _componentStorages.set(className, cast newStorage);
    }
    
    /**
     * Retrieves the underlying storage for a given component type.
     */
    public function getStorage<T>(componentClass:Class<T>):ComponentStorage<T> {
        var className = Type.getClassName(componentClass);
        var storage = _componentStorages.get(className);
        
        if (storage == null) {
            registerComponent(componentClass);
            storage = _componentStorages.get(className);
            
            if (storage == null) {
                throw 'Component type \'${className}\' could not be registered automatically.';
            }
        }
        
        return cast storage;
    }
    
    /**
     * Internal, non-generic version of getStorage.
     */
    public function getStorageUnsafe(componentClass:Class<Dynamic>):IComponentStorage {
        var className = Type.getClassName(componentClass);
        var storage = _componentStorages.get(className);
        
        if (storage == null) {
            throw 'Component type \'${className}\' has not been registered.';
        }
        
        return storage;
    }
    
    /**
     * Adds a component to an entity.
     */
    public function addComponent<T>(entity:Entity, component:T):Void {
        if (isAlive(entity)) {
            var componentClass = Type.getClass(component);
            getStorage(componentClass).add(entity.id, component);
        }
    }
    
    /**
     * Removes a component from an entity.
     */
    public function removeComponent<T>(entity:Entity, componentClass:Class<T>):Void {
        if (isAlive(entity)) {
            getStorage(componentClass).remove(entity.id);
        }
    }
    
    /**
     * Checks if an entity has a specific component.
     */
    public function hasComponent<T>(entity:Entity, componentClass:Class<T>):Bool {
        return isAlive(entity) && getStorage(componentClass).has(entity.id);
    }
    
    /**
     * Gets an entity's component.
     */
    public function getComponent<T>(entity:Entity, componentClass:Class<T>):T {
        if (!isAlive(entity)) throw "Attempted to get component from a dead entity.";
        return getStorage(componentClass).get(entity.id);
    }
    
    /**
     * Adds a new component or updates an existing one for the specified entity.
     */
    public function setComponent<T>(entity:Entity, component:T):Void {
        if (isAlive(entity)) {
            var componentClass = Type.getClass(component);
            getStorage(componentClass).set(entity.id, component);
        }
    }
    
    // --- Runtime/Dynamic API ---
    
    /**
     * Registers a component type at runtime.
     */
    public function registerComponentDynamic(componentClass:Class<Dynamic>, ?capacity:Int):Void {
        var className = Type.getClassName(componentClass);
        if (_componentStorages.exists(className)) return;
        
        var storageCapacity = capacity != null ? capacity : _maxEntities;
        // Create a generic storage - this is a limitation in Haxe
        var newStorage = new ComponentStorage<Dynamic>(storageCapacity, _maxEntities);
        _componentStorages.set(className, newStorage);
    }
    
    /**
     * Adds a component using Dynamic typing.
     */
    public function addComponentDynamic(entity:Entity, componentClass:Class<Dynamic>, componentData:Dynamic):Void {
        setComponentDynamic(entity, componentClass, componentData);
    }
    
    /**
     * Removes a component using Dynamic typing.
     */
    public function removeComponentDynamic(entity:Entity, componentClass:Class<Dynamic>):Void {
        if (isAlive(entity)) {
            var storage = getStorageUnsafe(componentClass);
            storage.remove(entity.id);
        }
    }
    
    /**
     * Checks if an entity has a component using Dynamic typing.
     */
    public function hasComponentDynamic(entity:Entity, componentClass:Class<Dynamic>):Bool {
        if (!isAlive(entity)) return false;
        var className = Type.getClassName(componentClass);
        var storage = _componentStorages.get(className);
        return storage != null && storage.has(entity.id);
    }
    
    /**
     * Gets a component using Dynamic typing.
     */
    public function getComponentDynamic(entity:Entity, componentClass:Class<Dynamic>):Dynamic {
        if (!isAlive(entity)) throw "Attempted to get component from a dead entity.";
        return getStorageUnsafe(componentClass).getAsObject(entity.id);
    }
    
    /**
     * Sets a component using Dynamic typing.
     */
    public function setComponentDynamic(entity:Entity, componentClass:Class<Dynamic>, componentData:Dynamic):Void {
        if (isAlive(entity)) {
            getStorageUnsafe(componentClass).setAsObject(entity.id, componentData);
        }
    }
    
    // --- Relationship Management ---
    
    /**
     * Register a relationship type with the world.
     */
    public function registerRelationship<T:IRelationship>(relationshipClass:Class<T>):Void {
        var className = Type.getClassName(relationshipClass);
        if (_relationshipStorages.exists(className)) return;
        _relationshipStorages.set(className, new RelationshipStorage<T>());
    }
    
    /**
     * Adds a relationship between entities using default data.
     */
    public function addRelationshipDefault<T:IRelationship>(source:Entity, target:Entity, relationshipClass:Class<T>):Void {
        var defaultInstance = Type.createInstance(relationshipClass, []);
        addRelationship(source, target, defaultInstance);
    }
    
    /**
     * Adds a bidirectional relationship with custom data transformation.
     */
    public function addBidirectionalRelationship<T:IBidirectionalRelationship>(
        source:Entity, 
        target:Entity, 
        sourceToTargetData:T, 
        reverseDataTransformer:T->T
    ):Void {
        if (isAlive(source) && isAlive(target)) {
            var className = Type.getClassName(Type.getClass(sourceToTargetData));
            var storage:RelationshipStorage<T> = cast _relationshipStorages.get(className);
            if (storage == null) {
                registerRelationship(Type.getClass(sourceToTargetData));
                storage = cast _relationshipStorages.get(className);
            }
            
            storage.addTyped(source, target, sourceToTargetData);
            var targetToSourceData = reverseDataTransformer(sourceToTargetData);
            storage.addTyped(target, source, targetToSourceData);
        }
    }
    
    /**
     * Removes a relationship between entities.
     */
    public function removeRelationship<T:IRelationship>(source:Entity, target:Entity, relationshipClass:Class<T>):Void {
        if (isAlive(source) && isAlive(target)) {
            var className = Type.getClassName(relationshipClass);
            var storage = _relationshipStorages.get(className);
            if (storage != null) {
                storage.remove(source, target);
                
                // Check if it's bidirectional
                if (Std.isOfType(Type.createInstance(relationshipClass, []), IBidirectionalRelationship)) {
                    storage.remove(target, source);
                }
            }
        }
    }
    
    /**
     * Adds a relationship with data between entities.
     */
    public function addRelationship<T:IRelationship>(source:Entity, target:Entity, relationshipData:T):Void {
        if (isAlive(source) && isAlive(target)) {
            var className = Type.getClassName(Type.getClass(relationshipData));
            var storage:RelationshipStorage<T> = cast _relationshipStorages.get(className);
            if (storage == null) {
                registerRelationship(Type.getClass(relationshipData));
                storage = cast _relationshipStorages.get(className);
            }
            
            storage.addTyped(source, target, relationshipData);
            
            // Check if it's bidirectional
            if (Std.isOfType(relationshipData, IBidirectionalRelationship)) {
                storage.addTyped(target, source, relationshipData);
            }
        }
    }
    
    /**
     * Tries to get relationship data between entities.
     */
    public function tryGetRelationship<T:IRelationship>(source:Entity, target:Entity, relationshipClass:Class<T>):Null<T> {
        if (isAlive(source) && isAlive(target)) {
            var className = Type.getClassName(relationshipClass);
            var storage = _relationshipStorages.get(className);
            if (storage != null) {
                var typedStorage:RelationshipStorage<T> = cast storage;
                return typedStorage.tryGetDataTyped(source, target);
            }
        }
        return null;
    }
    
    /**
     * Checks if a relationship exists between entities.
     */
    public function hasRelationship<T:IRelationship>(source:Entity, target:Entity, relationshipClass:Class<T>):Bool {
        if (!isAlive(source) || !isAlive(target)) return false;
        var className = Type.getClassName(relationshipClass);
        var storage = _relationshipStorages.get(className);
        return storage != null && storage.has(source, target);
    }
    
    /**
     * Internal non-generic version for queries.
     */
    public function hasRelationshipDynamic(source:Entity, target:Entity, relationshipClass:Class<Dynamic>):Bool {
        var className = Type.getClassName(relationshipClass);
        var storage = _relationshipStorages.get(className);
        return isAlive(source) && isAlive(target) && storage != null && storage.has(source, target);
    }
    
    /**
     * Gets all relationship targets for an entity.
     */
    public function getRelationshipTargets<T:IRelationship>(source:Entity, relationshipClass:Class<T>):Array<Entity> {
        if (!isAlive(source)) return [];
        var className = Type.getClassName(relationshipClass);
        var storage = _relationshipStorages.get(className);
        return storage != null ? storage.getTargets(source) : [];
    }
    
    /**
     * Cleanup relationships when an entity is destroyed.
     */
    function onDestroyEntity(entity:Entity):Void {
        for (storage in _relationshipStorages) {
            storage.removeAll(entity.id);
        }
    }
    
    /**
     * Internal method for queries.
     */
    public function getRelationshipStorageUnsafe(relationshipClass:Class<Dynamic>):IRelationshipStorage {
        var className = Type.getClassName(relationshipClass);
        var storage = _relationshipStorages.get(className);
        if (storage == null) throw 'Relationship type \'${className}\' has not been registered.';
        return storage;
    }

     /**
     * Creates a new QueryBuilder for building and executing entity queries in this world.
     */
    public function query():QueryBuilder {
        return new QueryBuilder(this);
    }
}