"""
Pygame Example: Hot-Reloadable Game using VMActor

This demonstrates the REAL POWER of VMActor:
- Hot-reload game mechanics while running
- Live-update AI behaviors
- Switch rendering styles on-the-fly
- Modify physics in real-time
- No need to restart the game!

Controls:
- Arrow Keys: Move player
- Space: Toggle pause
- 1-9: Hot-reload different behaviors (see console for options)
- ESC: Quit

Press number keys to hot-swap behaviors while playing!
"""

import pygame
import random
import math
from src.VMActor import VMActor
from src.ActorRuntime import ActorRuntime
import time


# Initialize Pygame
pygame.init()

# Constants
WIDTH, HEIGHT = 800, 600
FPS = 60

# Colors
BLACK = (0, 0, 0)
WHITE = (255, 255, 255)
RED = (255, 0, 0)
GREEN = (0, 255, 0)
BLUE = (0, 100, 255)
YELLOW = (255, 255, 0)
PURPLE = (200, 0, 200)
CYAN = (0, 255, 255)


class GameObject:
    """Simple game object."""
    def __init__(self, x, y, size, color):
        self.x = x
        self.y = y
        self.size = size
        self.color = color
        self.vx = 0.0
        self.vy = 0.0


class GameActor(VMActor):
    """Actor that manages game state and can be hot-reloaded."""
    
    def __init__(self, name):
        super().__init__()
        self.name = name
        
        # Store game objects in VM variables instead of direct attributes
        self.variables['player'] = GameObject(WIDTH // 2, HEIGHT // 2, 20, BLUE)
        self.variables['enemies'] = []
        self.variables['particles'] = []
        self.variables['score'] = 0
        self.variables['wave'] = 1
        self.variables['paused'] = False
        
        # Spawn initial enemies
        for _ in range(3):
            self.spawn_enemy()
        
        # Define default behaviors
        self._setup_default_behaviors()
    
    def _setup_default_behaviors(self):
        """Setup default game behaviors."""
        
        # Player movement
        def update_player(vm):
            keys = pygame.key.get_pressed()
            speed = 5
            
            player = vm.variables['player']
            
            if keys[pygame.K_LEFT]:
                player.vx = -speed
            elif keys[pygame.K_RIGHT]:
                player.vx = speed
            else:
                player.vx = 0
            
            if keys[pygame.K_UP]:
                player.vy = -speed
            elif keys[pygame.K_DOWN]:
                player.vy = speed
            else:
                player.vy = 0
            
            player.x += player.vx
            player.y += player.vy
            
            # Clamp to screen
            player.x = max(player.size, min(WIDTH - player.size, player.x))
            player.y = max(player.size, min(HEIGHT - player.size, player.y))
        
        # Enemy AI - chase player
        def update_enemies(vm):
            player = vm.variables['player']
            enemies = vm.variables['enemies']
            
            for enemy in enemies:
                # Chase player
                dx = player.x - enemy.x
                dy = player.y - enemy.y
                dist = math.sqrt(dx*dx + dy*dy)
                
                if dist > 0:
                    speed = 2
                    enemy.vx = (dx / dist) * speed
                    enemy.vy = (dy / dist) * speed
                else:
                    enemy.vx = 0
                    enemy.vy = 0
                
                enemy.x += enemy.vx
                enemy.y += enemy.vy
        
        # Collision detection
        def check_collisions(vm):
            player = vm.variables['player']
            enemies = vm.variables['enemies']
            particles = vm.variables['particles']
            
            for enemy in enemies[:]:
                dx = player.x - enemy.x
                dy = player.y - enemy.y
                dist = math.sqrt(dx*dx + dy*dy)
                
                if dist < player.size + enemy.size:
                    # Hit! Remove enemy, add score
                    enemies.remove(enemy)
                    vm.variables['score'] += 10
                    
                    # Spawn particle explosion
                    for _ in range(10):
                        particle = GameObject(enemy.x, enemy.y, 3, enemy.color)
                        angle = random.uniform(0, 2 * math.pi)
                        speed = random.uniform(2, 6)
                        particle.vx = math.cos(angle) * speed
                        particle.vy = math.sin(angle) * speed
                        particles.append(particle)
                    
                    # Spawn new enemy
                    vm.spawn_enemy()
        
        # Update particles
        def update_particles(vm):
            particles = vm.variables['particles']
            
            for particle in particles[:]:
                particle.x += particle.vx
                particle.y += particle.vy
                particle.vx *= 0.95
                particle.vy *= 0.95
                particle.size *= 0.95
                
                if particle.size < 0.5:
                    particles.remove(particle)
        
        # Rendering
        def render(vm):
            screen = vm.variables['screen']
            player = vm.variables['player']
            enemies = vm.variables['enemies']
            particles = vm.variables['particles']
            score = vm.variables['score']
            wave = vm.variables['wave']
            paused = vm.variables['paused']
            
            screen.fill(BLACK)
            
            # Draw player
            pygame.draw.circle(screen, player.color, 
                             (int(player.x), int(player.y)), 
                             player.size)
            
            # Draw enemies
            for enemy in enemies:
                pygame.draw.circle(screen, enemy.color,
                                 (int(enemy.x), int(enemy.y)),
                                 enemy.size)
            
            # Draw particles
            for particle in particles:
                if particle.size > 0:
                    pygame.draw.circle(screen, particle.color,
                                     (int(particle.x), int(particle.y)),
                                     int(particle.size))
            
            # Draw UI
            font = pygame.font.Font(None, 36)
            score_text = font.render(f'Score: {score}', True, WHITE)
            screen.blit(score_text, (10, 10))
            
            wave_text = font.render(f'Wave: {wave}', True, WHITE)
            screen.blit(wave_text, (10, 50))
            
            # Draw instructions
            small_font = pygame.font.Font(None, 24)
            instructions = [
                "Press 0-9 to hot-reload behaviors!",
                "1: Fast Player  2: Slow Player  3: Turbo Player",
                "4: Wandering AI  5: Smart AI  6: Crazy AI  0: Default AI",
                "7: Neon Style  8: Retro Style  9: Rainbow Style",
            ]
            for i, text in enumerate(instructions):
                inst_text = small_font.render(text, True, CYAN)
                screen.blit(inst_text, (10, HEIGHT - 100 + i * 25))
            
            if paused:
                pause_text = font.render('PAUSED', True, YELLOW)
                text_rect = pause_text.get_rect(center=(WIDTH // 2, HEIGHT // 2))
                screen.blit(pause_text, text_rect)
        
        # Register instructions
        self.defun("update-player", update_player)
        self.defun("update-enemies", update_enemies)
        self.defun("check-collisions", check_collisions)
        self.defun("update-particles", update_particles)
        self.defun("render", render)
    
    def spawn_enemy(self):
        """Spawn a new enemy at random edge."""
        side = random.choice(['top', 'bottom', 'left', 'right'])
        
        if side == 'top':
            x = random.randint(0, WIDTH)
            y = 0
        elif side == 'bottom':
            x = random.randint(0, WIDTH)
            y = HEIGHT
        elif side == 'left':
            x = 0
            y = random.randint(0, HEIGHT)
        else:  # right
            x = WIDTH
            y = random.randint(0, HEIGHT)
        
        enemy = GameObject(x, y, 15, RED)
        self.variables['enemies'].append(enemy)
    
    def game_loop_step(self):
        """Process one game step."""
        if not self.variables['paused']:
            # Compile and run each game logic step separately
            steps = [
                '(update-player)',
                '(update-enemies)',
                '(check-collisions)',
                '(update-particles)'
            ]
            
            for step in steps:
                bytecode = self.s_expression_to_bytecode(step)
                self.send(*bytecode)
        
        # Always render
        render_bytecode = self.s_expression_to_bytecode('(render)')
        self.send(*render_bytecode)
        
        # Process all messages
        while self.handle_message():
            pass


def hot_reload_fast_player(game):
    """Hot-reload: Make player super fast!"""
    print("HOT RELOAD: Fast Player Mode!")
    
    def update_player_fast(vm):
        keys = pygame.key.get_pressed()
        speed = 10  # Much faster!
        
        player = vm.variables['player']
        particles = vm.variables['particles']
        
        if keys[pygame.K_LEFT]:
            player.vx = -speed
        elif keys[pygame.K_RIGHT]:
            player.vx = speed
        else:
            player.vx = 0
        
        if keys[pygame.K_UP]:
            player.vy = -speed
        elif keys[pygame.K_DOWN]:
            player.vy = speed
        else:
            player.vy = 0
        
        player.x += player.vx
        player.y += player.vy
        
        player.x = max(player.size, min(WIDTH - player.size, player.x))
        player.y = max(player.size, min(HEIGHT - player.size, player.y))
        
        # Leave trail
        if random.random() < 0.3:
            particle = GameObject(player.x, player.y, 5, CYAN)
            particle.vx = -player.vx * 0.2
            particle.vy = -player.vy * 0.2
            particles.append(particle)
    
    game.replace_existing_instruction("OP_CALL_update-player", update_player_fast)


def hot_reload_slow_player(game):
    """Hot-reload: Make player slow and careful."""
    print("HOT RELOAD: Slow & Steady Mode!")
    
    def update_player_slow(vm):
        keys = pygame.key.get_pressed()
        speed = 2  # Much slower
        
        player = vm.variables['player']
        
        if keys[pygame.K_LEFT]:
            player.vx = -speed
        elif keys[pygame.K_RIGHT]:
            player.vx = speed
        else:
            player.vx = 0
        
        if keys[pygame.K_UP]:
            player.vy = -speed
        elif keys[pygame.K_DOWN]:
            player.vy = speed
        else:
            player.vy = 0
        
        player.x += player.vx
        player.y += player.vy
        
        player.x = max(player.size, min(WIDTH - player.size, player.x))
        player.y = max(player.size, min(HEIGHT - player.size, player.y))
        
        # Bigger player when slow
        player.size = 25
    
    game.replace_existing_instruction("OP_CALL_update-player", update_player_slow)


def hot_reload_turbo_player(game):
    """Hot-reload: Turbo mode with dash!"""
    print("HOT RELOAD: TURBO Mode!")
    
    def update_player_turbo(vm):
        keys = pygame.key.get_pressed()
        speed = 15  # ULTRA fast!
        
        player = vm.variables['player']
        particles = vm.variables['particles']
        
        if keys[pygame.K_LEFT]:
            player.vx = -speed
        elif keys[pygame.K_RIGHT]:
            player.vx = speed
        else:
            player.vx = 0
        
        if keys[pygame.K_UP]:
            player.vy = -speed
        elif keys[pygame.K_DOWN]:
            player.vy = speed
        else:
            player.vy = 0
        
        player.x += player.vx
        player.y += player.vy
        
        player.x = max(player.size, min(WIDTH - player.size, player.x))
        player.y = max(player.size, min(HEIGHT - player.size, player.y))
        
        player.size = 15
        player.color = PURPLE  # Purple turbo!
        
        # Massive trail
        for _ in range(3):
            particle = GameObject(player.x, player.y, 8, PURPLE)
            particle.vx = random.uniform(-2, 2)
            particle.vy = random.uniform(-2, 2)
            particles.append(particle)
    
    game.replace_existing_instruction("OP_CALL_update-player", update_player_turbo)


def hot_reload_default_chase_ai(game):
    """Hot-reload: Restore default chase AI."""
    print("HOT RELOAD: Default Chase AI!")
    
    # Reset enemy state for default behavior
    enemies = game.variables['enemies']
    for enemy in enemies:
        enemy.vx = 0
        enemy.vy = 0
        enemy.color = RED
    
    def update_enemies_chase(vm):
        player = vm.variables['player']
        enemies = vm.variables['enemies']
        
        for enemy in enemies:
            # Chase player
            dx = player.x - enemy.x
            dy = player.y - enemy.y
            dist = math.sqrt(dx*dx + dy*dy)
            
            if dist > 0:
                speed = 2
                enemy.vx = (dx / dist) * speed
                enemy.vy = (dy / dist) * speed
            
            enemy.x += enemy.vx
            enemy.y += enemy.vy
    
    game.replace_existing_instruction("OP_CALL_update-enemies", update_enemies_chase)


def hot_reload_wandering_ai(game):
    """Hot-reload: Enemies wander randomly."""
    print("HOT RELOAD: Wandering Enemy AI!")
    
    # Reset enemy state for new behavior
    enemies = game.variables['enemies']
    for enemy in enemies:
        angle = random.uniform(0, 2 * math.pi)
        speed = 3
        enemy.vx = math.cos(angle) * speed
        enemy.vy = math.sin(angle) * speed
        enemy.color = RED
    
    def update_enemies_wander(vm):
        enemies = vm.variables['enemies']
        
        for enemy in enemies:
            # Random wandering
            if random.random() < 0.1:
                angle = random.uniform(0, 2 * math.pi)
                speed = 3
                enemy.vx = math.cos(angle) * speed
                enemy.vy = math.sin(angle) * speed
            
            enemy.x += enemy.vx
            enemy.y += enemy.vy
            
            # Bounce off walls
            if enemy.x < 0 or enemy.x > WIDTH:
                enemy.vx *= -1
            if enemy.y < 0 or enemy.y > HEIGHT:
                enemy.vy *= -1
            
            enemy.x = max(0, min(WIDTH, enemy.x))
            enemy.y = max(0, min(HEIGHT, enemy.y))
    
    game.replace_existing_instruction("OP_CALL_update-enemies", update_enemies_wander)


def hot_reload_smart_ai(game):
    """Hot-reload: Smarter enemy AI with prediction."""
    print("HOT RELOAD: Smart Enemy AI!")
    
    # Reset enemy state for new behavior
    enemies = game.variables['enemies']
    for enemy in enemies:
        enemy.vx = 0
        enemy.vy = 0
        enemy.color = YELLOW  # Smart enemies are yellow
    
    def update_enemies_smart(vm):
        player = vm.variables['player']
        enemies = vm.variables['enemies']
        
        for enemy in enemies:
            # Predict where player will be
            future_x = player.x + player.vx * 5
            future_y = player.y + player.vy * 5
            
            dx = future_x - enemy.x
            dy = future_y - enemy.y
            dist = math.sqrt(dx*dx + dy*dy)
            
            if dist > 0:
                speed = 3
                enemy.vx = (dx / dist) * speed
                enemy.vy = (dy / dist) * speed
            
            enemy.x += enemy.vx
            enemy.y += enemy.vy
            
            enemy.color = YELLOW  # Smart enemies are yellow
    
    game.replace_existing_instruction("OP_CALL_update-enemies", update_enemies_smart)


def hot_reload_crazy_ai(game):
    """Hot-reload: Crazy zigzag AI."""
    print("HOT RELOAD: Crazy Zigzag AI!")
    
    # Reset enemy state for new behavior
    enemies = game.variables['enemies']
    for enemy in enemies:
        enemy.vx = 0
        enemy.vy = 0
        enemy.color = GREEN  # Crazy enemies are green
    
    def update_enemies_crazy(vm):
        player = vm.variables['player']
        enemies = vm.variables['enemies']
        
        for enemy in enemies:
            # Chase with zigzag
            dx = player.x - enemy.x
            dy = player.y - enemy.y
            dist = math.sqrt(dx*dx + dy*dy)
            
            if dist > 0:
                speed = 4
                # Add sine wave to movement
                t = pygame.time.get_ticks() / 200
                perpendicular_x = -dy / dist
                perpendicular_y = dx / dist
                zigzag = math.sin(t + id(enemy)) * 50
                
                enemy.vx = (dx / dist) * speed + perpendicular_x * zigzag * 0.1
                enemy.vy = (dy / dist) * speed + perpendicular_y * zigzag * 0.1
            
            enemy.x += enemy.vx
            enemy.y += enemy.vy
            
            enemy.color = GREEN  # Crazy enemies are green
    
    game.replace_existing_instruction("OP_CALL_update-enemies", update_enemies_crazy)


def hot_reload_neon_style(game):
    """Hot-reload: Neon visual style."""
    print("HOT RELOAD: Neon Visual Style!")
    
    def render_neon(vm):
        screen = vm.variables['screen']
        player = vm.variables['player']
        enemies = vm.variables['enemies']
        particles = vm.variables['particles']
        score = vm.variables['score']
        
        screen.fill((10, 0, 20))  # Dark purple background
        
        # Draw glowy circles
        for enemy in enemies:
            # Outer glow
            for r in range(enemy.size + 15, enemy.size, -3):
                alpha = 255 - (r - enemy.size) * 15
                color = (enemy.color[0], enemy.color[1], enemy.color[2], max(0, alpha))
                pygame.draw.circle(screen, color,
                                 (int(enemy.x), int(enemy.y)), r, 2)
            pygame.draw.circle(screen, enemy.color,
                             (int(enemy.x), int(enemy.y)), enemy.size)
        
        # Glowy player
        for r in range(player.size + 20, player.size, -2):
            pygame.draw.circle(screen, CYAN,
                             (int(player.x), int(player.y)), r, 2)
        pygame.draw.circle(screen, WHITE,
                         (int(player.x), int(player.y)), player.size)
        
        # Bright particles
        for particle in particles:
            if particle.size > 0:
                pygame.draw.circle(screen, WHITE,
                                 (int(particle.x), int(particle.y)),
                                 int(particle.size))
        
        # UI
        font = pygame.font.Font(None, 36)
        score_text = font.render(f'Score: {score}', True, CYAN)
        screen.blit(score_text, (10, 10))
    
    game.replace_existing_instruction("OP_CALL_render", render_neon)


def hot_reload_retro_style(game):
    """Hot-reload: Retro pixel art style."""
    print("HOT RELOAD: Retro Pixel Style!")
    
    def render_retro(vm):
        screen = vm.variables['screen']
        player = vm.variables['player']
        enemies = vm.variables['enemies']
        particles = vm.variables['particles']
        score = vm.variables['score']
        
        screen.fill(BLACK)
        
        # Pixelated squares instead of circles
        pixel_size = 8
        
        # Draw enemies as pixels
        for enemy in enemies:
            x = int(enemy.x // pixel_size) * pixel_size
            y = int(enemy.y // pixel_size) * pixel_size
            pygame.draw.rect(screen, enemy.color, 
                           (x, y, enemy.size * 2, enemy.size * 2))
        
        # Draw player as pixels
        x = int(player.x // pixel_size) * pixel_size
        y = int(player.y // pixel_size) * pixel_size
        pygame.draw.rect(screen, player.color,
                       (x, y, player.size * 2, player.size * 2))
        
        # Pixelated particles
        for particle in particles:
            if particle.size > 0:
                x = int(particle.x // pixel_size) * pixel_size
                y = int(particle.y // pixel_size) * pixel_size
                pygame.draw.rect(screen, particle.color,
                               (x, y, pixel_size, pixel_size))
        
        # UI
        font = pygame.font.Font(None, 48)
        score_text = font.render(f'SCORE:{score:04d}', True, GREEN)
        screen.blit(score_text, (10, 10))
    
    game.replace_existing_instruction("OP_CALL_render", render_retro)


def hot_reload_rainbow_style(game):
    """Hot-reload: Rainbow psychedelic style."""
    print("HOT RELOAD: Rainbow Style!")
    
    def render_rainbow(vm):
        screen = vm.variables['screen']
        player = vm.variables['player']
        enemies = vm.variables['enemies']
        particles = vm.variables['particles']
        score = vm.variables['score']
        
        # Rainbow background
        t = pygame.time.get_ticks() / 50
        bg_color = (
            int(128 + 127 * math.sin(t * 0.1)),
            int(128 + 127 * math.sin(t * 0.1 + 2)),
            int(128 + 127 * math.sin(t * 0.1 + 4))
        )
        screen.fill(bg_color)
        
        # Rainbow enemies
        for i, enemy in enumerate(enemies):
            color = (
                int(128 + 127 * math.sin(t * 0.2 + i)),
                int(128 + 127 * math.sin(t * 0.2 + i + 2)),
                int(128 + 127 * math.sin(t * 0.2 + i + 4))
            )
            pygame.draw.circle(screen, color,
                             (int(enemy.x), int(enemy.y)), enemy.size)
        
        # Rainbow player
        player_color = (
            int(128 + 127 * math.sin(t * 0.3)),
            int(128 + 127 * math.sin(t * 0.3 + 2)),
            int(128 + 127 * math.sin(t * 0.3 + 4))
        )
        pygame.draw.circle(screen, player_color,
                         (int(player.x), int(player.y)), player.size)
        
        # Rainbow particles
        for i, particle in enumerate(particles):
            if particle.size > 0:
                color = (
                    int(128 + 127 * math.sin(t * 0.4 + i * 0.1)),
                    int(128 + 127 * math.sin(t * 0.4 + i * 0.1 + 2)),
                    int(128 + 127 * math.sin(t * 0.4 + i * 0.1 + 4))
                )
                pygame.draw.circle(screen, color,
                                 (int(particle.x), int(particle.y)),
                                 int(particle.size))
        
        # UI
        font = pygame.font.Font(None, 36)
        ui_color = (
            int(128 + 127 * math.sin(t * 0.5)),
            int(128 + 127 * math.sin(t * 0.5 + 2)),
            int(128 + 127 * math.sin(t * 0.5 + 4))
        )
        score_text = font.render(f'✨ Score: {score} ✨', True, ui_color)
        screen.blit(score_text, (10, 10))
    
    game.replace_existing_instruction("OP_CALL_render", render_rainbow)


def main():
    """Main game loop."""
    screen = pygame.display.set_mode((WIDTH, HEIGHT))
    pygame.display.set_caption("VMActor Hot-Reload Demo - Press 1-9 to reload!")
    clock = pygame.time.Clock()
    
    # Create game actor
    game = GameActor("GameEngine")
    game.variables['screen'] = screen
    
    print("=" * 70)
    print("VMACTOR PYGAME HOT-RELOAD DEMO")
    print("=" * 70)
    print("\nControls:")
    print("  Arrow Keys: Move player")
    print("  Space: Toggle pause")
    print("  ESC: Quit")
    print("\nHot-Reload Keys (press while playing!):")
    print("  1: Fast Player Movement")
    print("  2: Slow Player Movement")
    print("  3: Turbo Player Movement")
    print("  4: Wandering Enemy AI")
    print("  5: Smart Enemy AI (predictive)")
    print("  6: Crazy Zigzag Enemy AI")
    print("  0: Default Chase AI")
    print("  7: Neon Visual Style")
    print("  8: Retro Pixel Style")
    print("  9: Rainbow Psychedelic Style")
    print("Try hot-reloading while playing - no restart needed!")
    print("=" * 70)
    
    running = True
    while running:
        # Handle events
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                running = False
            elif event.type == pygame.KEYDOWN:
                if event.key == pygame.K_ESCAPE:
                    running = False
                elif event.key == pygame.K_SPACE:
                    game.variables['paused'] = not game.variables['paused']
                    print(f"{'PAUSED' if game.variables['paused'] else 'RESUMED'}")
                
                # Hot-reload keys!
                elif event.key == pygame.K_0:
                    hot_reload_default_chase_ai(game)
                elif event.key == pygame.K_1:
                    hot_reload_fast_player(game)
                elif event.key == pygame.K_2:
                    hot_reload_slow_player(game)
                elif event.key == pygame.K_3:
                    hot_reload_turbo_player(game)
                elif event.key == pygame.K_4:
                    hot_reload_wandering_ai(game)
                elif event.key == pygame.K_5:
                    hot_reload_smart_ai(game)
                elif event.key == pygame.K_6:
                    hot_reload_crazy_ai(game)
                elif event.key == pygame.K_7:
                    hot_reload_neon_style(game)
                elif event.key == pygame.K_8:
                    hot_reload_retro_style(game)
                elif event.key == pygame.K_9:
                    hot_reload_rainbow_style(game)
        
        # Run game step
        game.game_loop_step()
        
        # Update display
        pygame.display.flip()
        clock.tick(FPS)
    
    pygame.quit()
    print("\nGame ended. Final score:", game.variables['score'])
    print("\n This is the power of VMActor:")
    print("  • Hot-reload game mechanics without restart")
    print("  • Iterate on ideas in real-time")
    print("  • Perfect for rapid prototyping")
    print("  • Live debugging and experimentation")


if __name__ == '__main__':
    main()
