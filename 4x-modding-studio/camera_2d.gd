extends Camera2D

# ------------------------------------------------------------------------------
# EXPORT VARIABLES
# ------------------------------------------------------------------------------
@export_group("Movement")
@export var move_speed: float = 500.0
@export var speed_scale_fast: float = 2.5
@export var speed_scale_slow: float = 0.2
@export var friction: float = 0.2 ## 0.0 to 1.0. Higher = faster stop.

@export_group("Zoom")
@export var zoom_speed: float = 0.1
@export var zoom_min: float = 0.1
@export var zoom_max: float = 4.0
@export var zoom_to_cursor: bool = true ## Check this to zoom towards mouse. Uncheck to zoom to center.
@export var zoom_smooth_speed: float = 10.0 ## Higher = faster zoom

@export_group("Edge Scrolling")
@export var edge_scroll_enabled: bool = true
@export var edge_margin: float = 20.0
@export var edge_speed_multiplier: float = 1.0

@export_group("Drag Panning")
@export var drag_pan_enabled: bool = true
@export var drag_button: MouseButton = MOUSE_BUTTON_MIDDLE

# ------------------------------------------------------------------------------
# INTERNAL VARIABLES
# ------------------------------------------------------------------------------
var _target_zoom: Vector2
var _target_position: Vector2
var _is_dragging: bool = false
var _last_mouse_pos: Vector2 = Vector2.ZERO

func _ready() -> void:
    # Initialize targets to current state
    _target_zoom = zoom
    _target_position = position

func _process(delta: float) -> void:
    _handle_keyboard_movement(delta)
    _handle_edge_movement(delta)
    _handle_smoothing(delta)

func _unhandled_input(event: InputEvent) -> void:
    if event is InputEventMouseButton:
        _handle_mouse_button(event)
    elif event is InputEventMouseMotion:
        _handle_mouse_motion(event)

# ------------------------------------------------------------------------------
# MOVEMENT LOGIC
# ------------------------------------------------------------------------------
func _handle_keyboard_movement(delta: float) -> void:
    if _is_dragging: return # Don't move with keys while dragging

    var input_dir = Input.get_vector("move_left", "move_right", "move_up", "move_down")
    if input_dir == Vector2.ZERO:
        return

    var current_speed = move_speed
    if Input.is_key_pressed(KEY_SHIFT):
        current_speed *= speed_scale_fast
    elif Input.is_key_pressed(KEY_ALT):
        current_speed *= speed_scale_slow

    # Scale speed by zoom so we move faster when zoomed out
    var scaled_speed = current_speed * (1.0 / zoom.x)
    
    # Update the target position directly for WASD
    _target_position += input_dir * scaled_speed * delta

func _handle_edge_movement(delta: float) -> void:
    if not edge_scroll_enabled or _is_dragging: return

    var viewport = get_viewport()
    var mouse_pos = viewport.get_mouse_position()
    var visible_rect = viewport.get_visible_rect()
    var move_vec = Vector2.ZERO

    if mouse_pos.x < edge_margin: move_vec.x = -1
    elif mouse_pos.x > visible_rect.size.x - edge_margin: move_vec.x = 1
    
    if mouse_pos.y < edge_margin: move_vec.y = -1
    elif mouse_pos.y > visible_rect.size.y - edge_margin: move_vec.y = 1

    if move_vec != Vector2.ZERO:
        var speed = move_speed * edge_speed_multiplier * (1.0 / zoom.x)
        _target_position += move_vec.normalized() * speed * delta

# ------------------------------------------------------------------------------
# ZOOM & PAN LOGIC
# ------------------------------------------------------------------------------
func _handle_mouse_button(event: InputEventMouseButton) -> void:
    # Drag Panning (Middle Mouse)
    if event.button_index == drag_button:
        if event.pressed and drag_pan_enabled:
            _is_dragging = true
            _last_mouse_pos = get_viewport().get_mouse_position()
        else:
            _is_dragging = false
    
    # Zooming
    if event.pressed:
        var zoom_dir = 0.0
        if event.button_index == MOUSE_BUTTON_WHEEL_UP:
            zoom_dir = 1.0
        elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
            zoom_dir = -1.0
            
        if zoom_dir != 0.0:
            _apply_zoom(zoom_dir)

func _handle_mouse_motion(_event: InputEventMouseMotion) -> void:
    if _is_dragging and drag_pan_enabled:
        var current_mouse_pos = get_viewport().get_mouse_position()
        var diff = (_last_mouse_pos - current_mouse_pos) * (1.0 / zoom.x)
        
        # Update target AND current position immediately for responsive drag
        _target_position += diff
        position += diff
        
        _last_mouse_pos = current_mouse_pos

func _apply_zoom(direction: float) -> void:
    var zoom_factor = 1.0 + (zoom_speed * direction)
    var new_zoom = _target_zoom * zoom_factor
    
    # Clamp zoom
    new_zoom = new_zoom.clamp(Vector2(zoom_min, zoom_min), Vector2(zoom_max, zoom_max))
    
    if zoom_to_cursor:
        # 1. Where is the mouse currently in the world? (Using CURRENT zoom/pos)
        var viewport_center = get_viewport_rect().size / 2.0
        var mouse_offset_from_center = get_viewport().get_mouse_position() - viewport_center
        var mouse_world_pos = position + (mouse_offset_from_center * (1.0 / zoom.x))
        
        # 2. We want that same World Position to be under the mouse at the NEW zoom.
        #    NewPos = MouseWorld - (MouseScreenOffset / NewZoom)
        var new_pos_offset = mouse_offset_from_center * (1.0 / new_zoom.x)
        _target_position = mouse_world_pos - new_pos_offset
    
    _target_zoom = new_zoom

# ------------------------------------------------------------------------------
# SMOOTHING
# ------------------------------------------------------------------------------
func _handle_smoothing(delta: float) -> void:
    var weight = clamp(delta * zoom_smooth_speed, 0.0, 1.0)
    
    # Smoothly move zoom and position towards targets
    zoom = zoom.lerp(_target_zoom, weight)
    position = position.lerp(_target_position, weight)