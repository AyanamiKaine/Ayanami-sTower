@tool
extends Polygon2D

# --- EXTERNAL DEPENDENCIES ---
@export_group("Grid Snapping")
@export var hex_grid: Node2D ## Drag your HexGrid node here
@export var snap_parent: bool = true ## If true, snaps the Parent Node.
@export var auto_snap: bool = true ## If true, snaps automatically after moving.

@export var snap_now: bool = false: ## Manual Snap Button
	set(value):
		snap_now = false
		_perform_snap()

@export var debug_snap: bool = false ## Print debug info when snapping

# --- HEXAGON VISUALS ---
@export_group("Hexagon Visuals")
@export var radius: float = 100.0:
	set(value):
		radius = value
		setup_hexagon()

@export_enum("Pointy Topped", "Flat Topped") var orientation: int = 0:
	set(value):
		orientation = value
		setup_hexagon()

@export var outline_width: float = 4.0:
	set(value):
		outline_width = value
		setup_hexagon()

@export var outline_color: Color = Color.WHITE:
	set(value):
		outline_color = value
		setup_hexagon()

# --- INTERNAL VARIABLES ---
var _last_global_pos: Vector2 = Vector2.ZERO
var _frames_stationary: int = 0
var _was_moving: bool = false

const SNAP_DELAY_FRAMES: int = 5 # Snap after ~5 frames of no movement (very responsive)
const MOVE_THRESHOLD: float = 0.5 # Minimum distance to count as "moved"

func _ready():
	setup_hexagon()
	_init_position_tracking()

func _init_position_tracking():
	var target = get_target_node()
	if target:
		_last_global_pos = target.global_position

func _process(_delta):
	# Only run in editor
	if not Engine.is_editor_hint():
		return
	
	if not auto_snap or not hex_grid:
		return
	
	var target = get_target_node()
	if not target:
		return
	
	var current_pos = target.global_position
	var distance_moved = current_pos.distance_to(_last_global_pos)
	
	if distance_moved > MOVE_THRESHOLD:
		# Node is moving - reset counter and track new position
		_was_moving = true
		_frames_stationary = 0
		_last_global_pos = current_pos
	else:
		# Node is stationary
		if _was_moving:
			_frames_stationary += 1
			
			# After being stationary for a few frames, snap!
			if _frames_stationary >= SNAP_DELAY_FRAMES:
				_perform_snap()
				_was_moving = false
				_frames_stationary = 0
				_last_global_pos = target.global_position

# --- HELPER: GET TARGET ---
func get_target_node() -> Node2D:
	if snap_parent:
		var p = get_parent()
		if p is Node2D:
			return p
	return self

# --- DRAWING LOGIC ---
func setup_hexagon():
	var points = PackedVector2Array()
	for i in range(6):
		var angle_deg = 60 * i - 30 if orientation == 0 else 60 * i
		var rad = deg_to_rad(angle_deg)
		points.append(Vector2(radius * cos(rad), radius * sin(rad)))
	polygon = points
	update_outline(points)

func update_outline(poly_points: PackedVector2Array):
	var line_node = get_node_or_null("HexOutline")
	if not line_node:
		line_node = Line2D.new(); line_node.name = "HexOutline"; add_child(line_node)
	var line_points = poly_points.duplicate()
	if not line_points.is_empty(): line_points.append(line_points[0])
	line_node.points = line_points
	line_node.width = outline_width
	line_node.default_color = outline_color
	line_node.joint_mode = Line2D.LINE_JOINT_SHARP

# --- SNAPPING LOGIC ---
func _perform_snap():
	if not hex_grid:
		if debug_snap:
			push_warning("HexagonPolygon: hex_grid is not assigned!")
		return
	
	if not hex_grid.has_method("snap_global_position"):
		if debug_snap:
			push_warning("HexagonPolygon: hex_grid doesn't have snap_global_position method!")
		return
	
	var target = get_target_node()
	if not target:
		return
	
	var current_global = target.global_position
	var snapped_global = hex_grid.snap_global_position(current_global)
	
	# Only snap if we are far enough away (prevents micro-adjustments)
	if current_global.distance_squared_to(snapped_global) > 0.1:
		target.global_position = snapped_global
		_last_global_pos = snapped_global # Critical: update tracker to prevent re-triggering
		
		if debug_snap:
			var coord = hex_grid.global_to_hex(snapped_global) if hex_grid.has_method("global_to_hex") else Vector2i.ZERO
			print("Snapped '", target.name, "' to hex ", coord, " at ", snapped_global)