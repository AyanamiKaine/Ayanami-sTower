package;

class Entity {
    public var id:Int = 0;
    public var world:World;
    
    public function new(world:World) {
        this.world = world;
    }
    
    public function set<T>(component:T):Entity {
        world.set(this, component);
        return this;
    }
    
    public function get<T>(componentClass:Class<T>):Null<T> {
        return world.get(this, componentClass);
    }
}