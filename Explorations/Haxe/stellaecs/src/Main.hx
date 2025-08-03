package;

import ecs.*;

// Example components
class Position {
    public var x:Float;
    public var y:Float;
    
    public function new(x:Float = 0, y:Float = 0) {
        this.x = x;
        this.y = y;
    }
}

class Velocity {
    public var dx:Float;
    public var dy:Float;
    
    public function new(dx:Float = 0, dy:Float = 0) {
        this.dx = dx;
        this.dy = dy;
    }
}

class Health {
    public var value:Int;
    
    public function new(value:Int = 100) {
        this.value = value;
    }
}

class Main {
    static function main() {
        var world = new World();
        
        // Create entities with components
        var player = world.createEntity()
            .set(new Position(10, 20))
            .set(new Velocity(5, 0))
            .set(new Health(100));
            
        var enemy = world.createEntity()
            .set(new Position(50, 30))
            .set(new Velocity(-2, 1))
            .set(new Health(50));
            
        var projectile = world.createEntity()
            .set(new Position(15, 25))
            .set(new Velocity(10, 0));
        
        // Query for entities with Position and Velocity
        trace("Entities with Position and Velocity:");
        for (result in world.query().with(Position).with(Velocity)) {
            var pos:Position = result.components.get("position");
            var vel:Velocity = result.components.get("velocity");
            trace('Entity ${result.entity.id}: pos(${pos.x}, ${pos.y}), vel(${vel.dx}, ${vel.dy})');
        }
        
        // Query with optional Health component
        trace("\nMoving entities (with optional Health):");
        for (result in world.query().with(Position).with(Velocity).optional(Health)) {
            var pos:Position = result.components.get("position");
            var vel:Velocity = result.components.get("velocity");
            var health:Health = result.components.get("health");
            
            var healthStr = health != null ? 'health: ${health.value}' : 'no health';
            trace('Entity ${result.entity.id}: pos(${pos.x}, ${pos.y}), vel(${vel.dx}, ${vel.dy}), $healthStr');
        }
        
        // Query with predicate
        trace("\nEntities with health > 75:");
        for (result in world.query().with(Health).where(function(r) {
            var health:Health = r.components.get("health");
            return health.value > 75;
        })) {
            var health:Health = result.components.get("health");
            trace('Entity ${result.entity.id}: health ${health.value}');
        }
        
        // Query excluding certain components
        trace("\nEntities with Position but without Health:");
        for (result in world.query().with(Position).without(Health)) {
            var pos:Position = result.components.get("position");
            trace('Entity ${result.entity.id}: pos(${pos.x}, ${pos.y})');
        }
    }
}