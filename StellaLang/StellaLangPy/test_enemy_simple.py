import math
from src.VMActor import VMActor

class Enemy:
    def __init__(self):
        self.x = 100
        self.y = 100
        self.vx = 0
        self.vy = 0

vm = VMActor()
vm.enemies = [Enemy()]
vm.player_x = 400
vm.player_y = 300

def update_enemies(v):
    print(f'update_enemies called! Enemies: {len(v.enemies)}')
    for enemy in v.enemies:
        dx = v.player_x - enemy.x
        dy = v.player_y - enemy.y
        dist = math.sqrt(dx*dx + dy*dy)
        
        print(f'  Enemy at ({enemy.x}, {enemy.y}), player at ({v.player_x}, {v.player_y}), dist={dist:.1f}')
        
        if dist > 0:
            speed = 2
            enemy.vx = (dx / dist) * speed
            enemy.vy = (dy / dist) * speed
            print(f'  Setting velocity: vx={enemy.vx:.2f}, vy={enemy.vy:.2f}')
        
        enemy.x += enemy.vx
        enemy.y += enemy.vy
        print(f'  New position: ({enemy.x:.1f}, {enemy.y:.1f})')

vm.defun('update-enemies', update_enemies)

print(f'Initial: enemy at ({vm.enemies[0].x}, {vm.enemies[0].y})')
print(f'Player at ({vm.player_x}, {vm.player_y})')

for i in range(3):
    print(f'\n=== Step {i+1} ===')
    bc = vm.s_expression_to_bytecode('(update-enemies)')
    vm.send(*bc)
    vm.handle_message()
    print(f'Result: enemy at ({vm.enemies[0].x:.1f}, {vm.enemies[0].y:.1f}), velocity ({vm.enemies[0].vx:.2f}, {vm.enemies[0].vy:.2f})')
