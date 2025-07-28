/**
 * Custom iterator for query results.
 */
class QueryIterator {
    private var _world:World;
    private var _driverEntities:Array<Entity>;
    private var _otherWithStorages:Array<IComponentStorage>;
    private var _withoutStorages:Array<IComponentStorage>;
    private var _filters:Array<IFilter>;
    private var _withRelationships:Array<{type:Class<Dynamic>, target:Entity}>;
    private var _index:Int;
    
    public function new(
        world:World, 
        driverEntities:Array<Entity>, 
        otherWithStorages:Array<IComponentStorage>, 
        withoutStorages:Array<IComponentStorage>, 
        filters:Array<IFilter>, 
        withRelationships:Array<{type:Class<Dynamic>, target:Entity}>
    ) {
        _world = world;
        _driverEntities = driverEntities;
        _otherWithStorages = otherWithStorages;
        _withoutStorages = withoutStorages;
        _filters = filters;
        _withRelationships = withRelationships;
        _index = 0;
    }
    
    public function hasNext():Bool {
        while (_index < _driverEntities.length) {
            var entity = _driverEntities[_index];
            
            if (!entity.isAlive()) {
                _index++;
                continue;
            }
            
            // Check if the entity has all the other required components
            if (_otherWithStorages != null) {
                var allFound = true;
                for (storage in _otherWithStorages) {
                    if (!storage.has(entity.id)) {
                        allFound = false;
                        break;
                    }
                }
                if (!allFound) {
                    _index++;
                    continue;
                }
            }
            
            // Check if the entity has any of the excluded components
            if (_withoutStorages != null) {
                var anyFound = false;
                for (storage in _withoutStorages) {
                    if (storage.has(entity.id)) {
                        anyFound = true;
                        break;
                    }
                }
                if (anyFound) {
                    _index++;
                    continue;
                }
            }
            
            // Check if the entity has all the required relationships
            if (_withRelationships != null) {
                var allFound = true;
                for (withRel in _withRelationships) {
                    if (!_world.hasRelationshipDynamic(entity, withRel.target, withRel.type)) {
                        allFound = false;
                        break;
                    }
                }
                if (!allFound) {
                    _index++;
                    continue;
                }
            }
            
            // Check if the entity's data matches all filters
            if (_filters != null) {
                var allMatch = true;
                for (filter in _filters) {
                    if (!filter.matches(entity)) {
                        allMatch = false;
                        break;
                    }
                }
                if (!allMatch) {
                    _index++;
                    continue;
                }
            }
            
            return true;
        }
        
        return false;
    }
    
    public function next():Entity {
        if (!hasNext()) {
            throw "No more elements";
        }
        return _driverEntities[_index++];
    }
}