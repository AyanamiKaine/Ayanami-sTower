import RelationshipStorage.IRelationship;
import haxe.ds.Map;
import haxe.ds.List;

/**
 * Represents a unique entity in the world using an ID and a generation.
 * The generation helps to invalidate handles to entities that have been destroyed.
 * This is a lightweight, immutable structure.
 */
@:structInit
class Entity {
    /**
     * A null/invalid entity handle.
     */
    public static final Null:Entity = new Entity(-1, 0, null);
    
    /**
     * The raw integer ID of the entity. This corresponds to an index in the world's arrays.
     */
    public final id:Int;
    
    /**
     * The generation of the entity, which is incremented each time the ID is recycled.
     */
    public final generation:Int;
    
    private final _world:Null<World>;
    
    public function new(id:Int, generation:Int, world:Null<World>) {
        this.id = id;
        this.generation = generation;
        this._world = world;
    }
    
    /**
     * Checks if this entity handle is "alive" and valid.
     * A handle is alive if its generation matches the world's current generation for that ID.
     */
    public function isAlive():Bool {
        return _world != null ? _world.isAlive(this) : false;
    }
    
    /**
     * Adds a component to this entity.
     */
    public function add<T>(component:T):Void {
        if (_world != null) _world.addComponent(this, component);
    }
    
    /**
     * Removes a component from this entity.
     */
    public function remove<T>(componentClass:Class<T>):Void {
        if (_world != null) _world.removeComponent(this, componentClass);
    }
    
    /**
     * Checks if this entity has a specific component.
     */
    public function has<T>(componentClass:Class<T>):Bool {
        return _world != null ? _world.hasComponent(this, componentClass) : false;
    }
    
    /**
     * Gets this entity's component.
     * Note: Returns a copy since Haxe doesn't have ref returns.
     */
    public function get<T>(componentClass:Class<T>):T {
        if (_world == null) throw "Cannot get component from entity with null world";
        return _world.getComponent(this, componentClass);
    }
    
    /**
     * Gets a component for modification. Since Haxe doesn't have ref returns,
     * you need to call set() after making changes.
     */
    public function getMutable<T>(componentClass:Class<T>):T {
        if (_world == null) throw "Cannot get component from entity with null world";
        return _world.getComponent(this, componentClass);
    }
    
    /**
     * Adds a new component or updates an existing one for this entity.
     */
    public function set<T>(component:T):Void {
        if (_world != null) _world.setComponent(this, component);
    }
    
    /**
     * Destroys this entity, removing all its components and recycling its ID.
     */
    public function destroy():Void {
        if (_world != null) _world.destroyEntity(this);
    }
    
    // --- Runtime/Non-Generic API Methods ---
    
    /**
     * Adds a component using Dynamic typing (for runtime operations).
     */
    public function addDynamic(componentClass:Class<Dynamic>, componentData:Dynamic):Void {
        if (_world != null) _world.addComponentDynamic(this, componentClass, componentData);
    }
    
    /**
     * Removes a component using Dynamic typing.
     */
    public function removeDynamic(componentClass:Class<Dynamic>):Void {
        if (_world != null) _world.removeComponentDynamic(this, componentClass);
    }
    
    /**
     * Checks if this entity has a component using Dynamic typing.
     */
    public function hasDynamic(componentClass:Class<Dynamic>):Bool {
        return _world != null ? _world.hasComponentDynamic(this, componentClass) : false;
    }
    
    /**
     * Gets a component using Dynamic typing.
     */
    public function getDynamic(componentClass:Class<Dynamic>):Dynamic {
        if (_world == null) throw "Cannot get component from entity with null world";
        return _world.getComponentDynamic(this, componentClass);
    }
    
    /**
     * Sets a component using Dynamic typing.
     */
    public function setDynamic(componentClass:Class<Dynamic>, componentData:Dynamic):Void {
        if (_world != null) _world.setComponentDynamic(this, componentClass, componentData);
    }
    
    // --- Relationship Methods ---
    
    /**
     * Adds a relationship with data from this entity to a target entity.
     */
    public function addRelationship<T:IRelationship>(target:Entity, relationshipData:T):Void {
        if (_world != null) _world.addRelationship(this, target, relationshipData);
    }
    
    /**
     * Adds a relationship from this entity to a target entity using default data.
     */
    public function addRelationshipDefault<T:IRelationship>(target:Entity, relationshipClass:Class<T>):Void {
        if (_world != null) _world.addRelationshipDefault(this, target, relationshipClass);
    }
    
    /**
     * Tries to get the data associated with a relationship from this entity to a target entity.
     */
    public function tryGetRelationship<T:IRelationship>(target:Entity, relationshipClass:Class<T>):Null<T> {
        return _world != null ? _world.tryGetRelationship(this, target, relationshipClass) : null;
    }
    
    /**
     * Removes a relationship from this entity to a target entity.
     */
    public function removeRelationship<T:IRelationship>(target:Entity, relationshipClass:Class<T>):Void {
        if (_world != null) _world.removeRelationship(this, target, relationshipClass);
    }
    
    /**
     * Checks if a relationship exists from this entity to a target entity.
     */
    public function hasRelationship<T:IRelationship>(target:Entity, relationshipClass:Class<T>):Bool {
        return _world != null ? _world.hasRelationship(this, target, relationshipClass) : false;
    }
    
    /**
     * Gets all entities that this entity has a relationship with.
     */
    public function getRelationshipTargets<T:IRelationship>(relationshipClass:Class<T>):Array<Entity> {
        return _world != null ? _world.getRelationshipTargets(this, relationshipClass) : [];
    }
    
    // --- Equality and Hash ---
    
    public function equals(other:Entity):Bool {
        return id == other.id && generation == other.generation;
    }
    
    public function toString():String {
        return 'Entity(Id: $id, Gen: $generation)';
    }
}