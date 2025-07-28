package;

class Position2D {
    public var x:Int;
    public var y:Int;
    
    public function new(x:Int = 0, y:Int = 0) {
        this.x = x;
        this.y = y;
    }
    
    public function toString():String {
        return 'Position2D(x: $x, y: $y)';
    }
}

class Velocity2D {
    public var dx:Int;
    public var dy:Int;
    
    public function new(dx:Int = 0, dy:Int = 0) {
        this.dx = dx;
        this.dy = dy;
    }
    
    public function toString():String {
        return 'Velocity2D(dx: $dx, dy: $dy)';
    }
}

class Health {
    public var current:Int;
    public var max:Int;
    
    public function new(max:Int = 100) {
        this.max = max;
        this.current = max;
    }
    
    public function toString():String {
        return 'Health($current/$max)';
    }
}

class Main {
    static function main() {
        trace("Creating ECS world");
        var world = new World();

        // Components are auto-registered on first use, but you can register them explicitly for better control
        trace("Registering components");
        world.registerComponent(Position2D);
        world.registerComponent(Velocity2D);
        world.registerComponent(Health); // Custom capacity of 500 entities

        trace("Creating entities");
        var player = world.createEntity();
        var enemy = world.createEntity();
        var projectile = world.createEntity();

        // Adding components to entities
        trace("Adding components to player");
        player.add(new Position2D(10, 20));
        player.add(new Velocity2D(1, 0));
        player.add(new Health(150));

        trace("Adding components to enemy");
        enemy.add(new Position2D(50, 30));
        enemy.add(new Health(80));

        trace("Adding components to projectile");
        projectile.add(new Position2D(15, 25));
        projectile.add(new Velocity2D(5, 0));

        // Reading components
        trace("Reading player components:");
        if (player.has(Position2D)) {
            var pos = player.get(Position2D);
            trace("Player position: " + pos);
        }
        
        if (player.has(Health)) {
            var health = player.get(Health);
            trace("Player health: " + health);
        }

        // Modifying components (remember: Haxe doesn't have ref returns)
        trace("Modifying player position:");
        var playerPos = player.get(Position2D);
        playerPos.x += 5;
        playerPos.y += 3;
        player.set(playerPos); // Must set it back after modification
        
        var newPos = player.get(Position2D);
        trace("New player position: " + newPos);

        // Using the Set method (upsert - update or insert)
        trace("Using set method to update velocity:");
        player.set(new Velocity2D(2, 1));
        var vel = player.get(Velocity2D);
        trace("Player velocity: " + vel);

        // Querying entities
        trace("Querying entities with Position2D and Velocity2D:");
        var movingEntities = world.query()
            .with(Position2D)
            .with(Velocity2D)
            .build();
            
        for (entity in movingEntities) {
            var pos = entity.get(Position2D);
            var vel = entity.get(Velocity2D);
            trace('Entity ${entity.toString()} - Position: $pos, Velocity: $vel');
        }

        // Query with filter
        trace("Querying entities with health > 100:");
        var strongEntities = world.query()
            .with(Health)
            .where(Health, function(h) return h.current > 100)
            .build();
            
        for (entity in strongEntities) {
            var health = entity.get(Health);
            trace('Strong entity ${entity.toString()} - Health: $health');
        }

        // Query without certain components
        trace("Querying entities with Position2D but without Health:");
        var unhealthyPositioned = world.query()
            .with(Position2D)
            .without(Health)
            .build();
            
        for (entity in unhealthyPositioned) {
            var pos = entity.get(Position2D);
            trace('Entity without health ${entity.toString()} - Position: $pos');
        }

        // Removing components
        trace("Removing velocity from player:");
        player.remove(Velocity2D);
        trace("Player has velocity: " + player.has(Velocity2D));

        // Destroying entities
        trace("Destroying projectile:");
        projectile.destroy();
        trace("Projectile is alive: " + projectile.isAlive());

        // Working with component storages directly (for advanced use)
        trace("Getting Position2D storage directly:");
        var posStorage = world.getStorage(Position2D);
        trace("Position2D storage count: " + posStorage.count);
        trace("Position2D storage capacity: " + posStorage.capacity);

        // Iterate over all entities with a specific component
        var allPositions = posStorage.getPackedComponents();
        var allEntities = posStorage.getPackedEntities();
        trace("All entities with Position2D:");
        for (i in 0...allPositions.length) {
            var entityId = allEntities[i];
            var position = allPositions[i];
            var entity = world.getEntityFromId(entityId);
            trace('Entity ${entity.toString()} at $position');
        }

        trace("ECS demo complete!");
    }
}