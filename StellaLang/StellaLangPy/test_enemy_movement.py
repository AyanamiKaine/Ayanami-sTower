"""Test enemy movement to debug the issue."""
import math
from game_hot_reload import GameActor, GameObject, WIDTH, HEIGHT, RED

# Create game actor without pygame
class TestGameActor(GameActor):
    def __init__(self, name):
        # Skip pygame initialization
        from src.VMActor import VMActor
        VMActor.__init__(self)
        self.name = name
        self.player = GameObject(WIDTH // 2, HEIGHT // 2, 20, (0, 100, 255))
        self.enemies = []
        self.particles = []
        self.score = 0
        self.wave = 1
        self.paused = False
        
        # Spawn test enemy
        self.enemies.append(GameObject(100, 100, 15, RED))
        
        # Setup behaviors manually
        def update_enemies(vm):
            print(f"update_enemies called! {len(vm.enemies)} enemies")
            for i, enemy in enumerate(vm.enemies):
                dx = vm.player.x - enemy.x
                dy = vm.player.y - enemy.y
                dist = math.sqrt(dx*dx + dy*dy)
                
                print(f"  Enemy {i}: pos=({enemy.x:.1f}, {enemy.y:.1f}), player=({vm.player.x}, {vm.player.y}), dist={dist:.1f}")
                
                if dist > 0:
                    speed = 2
                    enemy.vx = (dx / dist) * speed
                    enemy.vy = (dy / dist) * speed
                    print(f"    Setting velocity: vx={enemy.vx:.2f}, vy={enemy.vy:.2f}")
                
                enemy.x += enemy.vx
                enemy.y += enemy.vy
                print(f"    New position: ({enemy.x:.1f}, {enemy.y:.1f})")
        
        self.defun("update-enemies", update_enemies)

# Test
game = TestGameActor("test")
print(f"Initial enemy: x={game.enemies[0].x}, y={game.enemies[0].y}, vx={game.enemies[0].vx}, vy={game.enemies[0].vy}")
print(f"Player: x={game.player.x}, y={game.player.y}")

# Try to call update-enemies
bytecode = game.s_expression_to_bytecode('(update-enemies)')
print(f"\nBytecode: {bytecode}")
print("\nSending message...")
game.send(*bytecode)

print("\nProcessing messages...")
while game.handle_message():
    print("  Message processed")

print(f"\nFinal enemy: x={game.enemies[0].x}, y={game.enemies[0].y}, vx={game.enemies[0].vx}, vy={game.enemies[0].vy}")
