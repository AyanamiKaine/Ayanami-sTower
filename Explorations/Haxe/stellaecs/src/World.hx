package;

import haxe.DynamicAccess;

class World {
    public var entities:Array<Entity>;
    public var componentStorages:Map<String, ComponentStorage<Any>>;
    
    public function new() {
        entities = [];
        componentStorages = new Map();
    }
    
    public function createEntity():Entity {
        var entity = new Entity(this);
        entity.id = entities.length;
        entities.push(entity);
        return entity;
    }
    
    public function set<T>(entity:Entity, component:T):Void {
        var componentType = Type.getClassName(Type.getClass(component));
        
        if (!componentStorages.exists(componentType)) {
            var storage = new ComponentStorage<T>();
            componentStorages.set(componentType, cast storage);
        }
        
        var storage:ComponentStorage<T> = cast componentStorages.get(componentType);
        storage.set(entity, component);
    }
    
    public function get<T>(entity:Entity, componentClass:Class<T>):Null<T> {
        var componentType = Type.getClassName(componentClass);
        var storage:ComponentStorage<T> = cast componentStorages.get(componentType);
        
        if (storage == null) {
            return null;
        }
        
        return storage.get(entity);
    }
    
    public function query():Query {
        return new Query(this);
    }
    
    // Internal methods for Query access
    @:allow(ecs.QueryIterator)
    public function getComponentStorage<T>(componentClass:Class<T>):ComponentStorage<T> {
        var componentType = Type.getClassName(componentClass);
        return cast componentStorages.get(componentType);
    }
    
    @:allow(ecs.QueryIterator)
    public function getEntity(id:Int):Null<Entity> {
        if (id >= 0 && id < entities.length) {
            return entities[id];
        }
        return null;
    }
}