/**
 * A marker interface to define a relationship type.
 * Relationship tags should be empty classes that implement this interface.
 * Example: class IsChildOf implements IRelationship {}
 */
interface IRelationship {}

/**
 * A marker interface to define a bidirectional relationship type.
 * This inherits from IRelationship and signals to the World that when a relationship
 * A -> B of this type is added, the corresponding B -> A relationship should also be added automatically.
 * Likewise, removing one side of the relationship will also remove the other.
 * Example: class IsMarriedTo implements IBidirectionalRelationship {}
 */
interface IBidirectionalRelationship extends IRelationship {}

/**
 * Defines the non-generic API for a relationship storage.
 */
interface IRelationshipStorage {
    function add(source:Entity, target:Entity, data:Dynamic):Void;
    function remove(source:Entity, target:Entity):Void;
    function has(source:Entity, target:Entity):Bool;
    function getTargets(source:Entity):Array<Entity>;
    function tryGetData(source:Entity, target:Entity):Dynamic;
    function removeAll(entityId:Int):Void;
    function getSources(target:Entity):Array<Entity>;
}

/**
 * Stores relationships of a specific type T.
 * It uses two maps to maintain forward (source -> targets) and reverse (target -> sources) mappings for efficient lookups.
 */
class RelationshipStorage<T:IRelationship> implements IRelationshipStorage {
    // Forward mapping: Source Entity ID -> Map of Target Entities to relationship data
    private var _forwardMap:Map<Int, Map<String, {entity:Entity, data:T}>>;
    // Reverse mapping: Target Entity ID -> List of Source Entities
    private var _reverseMap:Map<Int, Array<Entity>>;
    
    public function new() {
        _forwardMap = new Map<Int, Map<String, {entity:Entity, data:T}>>();
        _reverseMap = new Map<Int, Array<Entity>>();
    }
    
    /**
     * Adds a relationship from source to target with the given data.
     */
    public function addTyped(source:Entity, target:Entity, data:T):Void {
        // Add to forward map
        var targets = _forwardMap.get(source.id);
        if (targets == null) {
            targets = new Map<String, {entity:Entity, data:T}>();
            _forwardMap.set(source.id, targets);
        }
        
        // Use a string key combining entity id and generation for uniqueness
        var targetKey = '${target.id}_${target.generation}';
        targets.set(targetKey, {entity: target, data: data});
        
        // Add to reverse map
        var sources = _reverseMap.get(target.id);
        if (sources == null) {
            sources = new Array<Entity>();
            _reverseMap.set(target.id, sources);
        }
        
        // Check if source already exists to avoid duplicates
        var hasSource = false;
        for (s in sources) {
            if (s.id == source.id && s.generation == source.generation) {
                hasSource = true;
                break;
            }
        }
        
        if (!hasSource) {
            sources.push(source);
        }
    }
    
    /**
     * Removes a relationship from source to target.
     */
    public function remove(source:Entity, target:Entity):Void {
        // Remove from forward map
        var targets = _forwardMap.get(source.id);
        if (targets != null) {
            var targetKey = '${target.id}_${target.generation}';
            if (targets.remove(targetKey)) {
                // If no more targets, remove the entire entry
                if (!targets.keys().hasNext()) {
                    _forwardMap.remove(source.id);
                }
            }
        }
        
        // Remove from reverse map
        var sources = _reverseMap.get(target.id);
        if (sources != null) {
            var i = 0;
            while (i < sources.length) {
                var s = sources[i];
                if (s.id == source.id && s.generation == source.generation) {
                    sources.splice(i, 1);
                    break;
                }
                i++;
            }
            
            if (sources.length == 0) {
                _reverseMap.remove(target.id);
            }
        }
    }
    
    /**
     * Tries to get the relationship data between source and target.
     */
    public function tryGetDataTyped(source:Entity, target:Entity):Null<T> {
        var targets = _forwardMap.get(source.id);
        if (targets != null) {
            var targetKey = '${target.id}_${target.generation}';
            var entry = targets.get(targetKey);
            if (entry != null) {
                return entry.data;
            }
        }
        return null;
    }
    
    /**
     * Checks if a relationship exists between source and target.
     */
    public function has(source:Entity, target:Entity):Bool {
        var targets = _forwardMap.get(source.id);
        if (targets != null) {
            var targetKey = '${target.id}_${target.generation}';
            return targets.exists(targetKey);
        }
        return false;
    }
    
    /**
     * Gets all targets that the source entity has relationships with.
     */
    public function getTargets(source:Entity):Array<Entity> {
        var targets = _forwardMap.get(source.id);
        if (targets != null) {
            var result = new Array<Entity>();
            for (entry in targets) {
                result.push(entry.entity);
            }
            return result;
        }
        return [];
    }
    
    /**
     * Gets all sources that have relationships with the target entity.
     */
    public function getSources(target:Entity):Array<Entity> {
        var sources = _reverseMap.get(target.id);
        return sources != null ? sources.copy() : [];
    }
    
    /**
     * Removes all relationships involving the given entity ID (both as source and target).
     */
    public function removeAll(entityId:Int):Void {
        // Remove as source
        var targets = _forwardMap.get(entityId);
        if (targets != null) {
            for (entry in targets) {
                var target = entry.entity;
                var reverseSources = _reverseMap.get(target.id);
                if (reverseSources != null) {
                    var i = 0;
                    while (i < reverseSources.length) {
                        if (reverseSources[i].id == entityId) {
                            reverseSources.splice(i, 1);
                        } else {
                            i++;
                        }
                    }
                    if (reverseSources.length == 0) {
                        _reverseMap.remove(target.id);
                    }
                }
            }
            _forwardMap.remove(entityId);
        }
        
        // Remove as target
        var sources = _reverseMap.get(entityId);
        if (sources != null) {
            // For each entity that was pointing to this one...
            for (source in sources) {
                // ...go to its forward map and remove entries for this entity.
                var sourceTargets = _forwardMap.get(source.id);
                if (sourceTargets != null) {
                    // Find all keys that match the entityId to be destroyed
                    var keysToRemove = new Array<String>();
                    for (key in sourceTargets.keys()) {
                        var entry = sourceTargets.get(key);
                        if (entry != null && entry.entity.id == entityId) {
                            keysToRemove.push(key);
                        }
                    }
                    
                    // Remove the keys
                    for (key in keysToRemove) {
                        sourceTargets.remove(key);
                    }
                    
                    if (!sourceTargets.keys().hasNext()) {
                        _forwardMap.remove(source.id);
                    }
                }
            }
            _reverseMap.remove(entityId);
        }
    }
    
    // --- Non-Generic API Implementation ---
    
    /**
     * Adds a relationship using Dynamic typing (for interface compatibility).
     */
    public function add(source:Entity, target:Entity, data:Dynamic):Void {
        addTyped(source, target, cast data);
    }
    
    /**
     * Gets relationship data as Dynamic (for interface compatibility).
     */
    public function tryGetData(source:Entity, target:Entity):Dynamic {
        return tryGetDataTyped(source, target);
    }
}