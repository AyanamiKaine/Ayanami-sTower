@tool
extends Node2D

# ------------------------------------------------------------------------------
# SETTINGS
# ------------------------------------------------------------------------------
@export_group("Grid Configuration")
@export var grid_size: Vector2i = Vector2i(10, 10):
	set(value):
		grid_size = value
		queue_redraw()

@export var hex_radius: float = 100.0:
	set(value):
		hex_radius = value
		queue_redraw()

@export_enum("Pointy Topped", "Flat Topped") var orientation: int = 0:
	set(value):
		orientation = value
		queue_redraw()

@export_group("Debug Display")
@export var show_grid: bool = true:
	set(value):
		show_grid = value
		queue_redraw()
@export var grid_color: Color = Color(1, 1, 1, 0.2):
	set(value):
		grid_color = value
		queue_redraw()

const SQRT_3 = 1.7320508

# ------------------------------------------------------------------------------
# PUBLIC API (GLOBAL SPACE HELPERS)
# ------------------------------------------------------------------------------

## Input: Global Position (e.g. from mouse or node.global_position)
## Output: The Grid Coordinate (q, r) at that location
func global_to_hex(global_pos: Vector2) -> Vector2i:
	# Convert the global position to a position local to THIS grid node
	# This handles cases where the HexGrid itself is moved or scaled.
	var local_pos = to_local(global_pos)
	return _local_to_hex(local_pos)

## Input: Grid Coordinate (q, r)
## Output: The Global Position (pixels) of that hex center
func hex_to_global(grid_coords: Vector2i) -> Vector2:
	var local_pos = _hex_to_local(grid_coords)
	return to_global(local_pos)

## Snaps a random global position to the nearest valid hex center (Global)
func snap_global_position(global_pos: Vector2) -> Vector2:
	var grid_coords = global_to_hex(global_pos)
	return hex_to_global(grid_coords)

# ------------------------------------------------------------------------------
# INTERNAL MATH (LOCAL SPACE)
# ------------------------------------------------------------------------------

func _hex_to_local(hex_coords: Vector2i) -> Vector2:
	var q = hex_coords.x
	var r = hex_coords.y
	var x: float
	var y: float
	
	if orientation == 0: # Pointy Topped
		x = hex_radius * SQRT_3 * (q + (r / 2.0))
		y = hex_radius * (3.0 / 2.0) * r
	else: # Flat Topped
		x = hex_radius * (3.0 / 2.0) * q
		y = hex_radius * SQRT_3 * (r + (q / 2.0))
	return Vector2(x, y)

func _local_to_hex(local_pos: Vector2) -> Vector2i:
	var q: float
	var r: float
	
	if orientation == 0: # Pointy Topped
		q = (SQRT_3 / 3.0 * local_pos.x - 1.0 / 3.0 * local_pos.y) / hex_radius
		r = (2.0 / 3.0 * local_pos.y) / hex_radius
	else: # Flat Topped
		q = (2.0 / 3.0 * local_pos.x) / hex_radius
		r = (-1.0 / 3.0 * local_pos.x + SQRT_3 / 3.0 * local_pos.y) / hex_radius
	
	return _axial_round(Vector2(q, r))

func _axial_round(frac_coords: Vector2) -> Vector2i:
	var q = frac_coords.x
	var r = frac_coords.y
	var s = -q - r
	
	var rq = roundf(q)
	var rr = roundf(r)
	var rs = roundf(s)
	
	var q_diff = abs(rq - q)
	var r_diff = abs(rr - r)
	var s_diff = abs(rs - s)
	
	if q_diff > r_diff and q_diff > s_diff:
		rq = - rr - rs
	elif r_diff > s_diff:
		rr = - rq - rs
	else:
		rs = - rq - rr
		
	return Vector2i(int(rq), int(rr))

# ------------------------------------------------------------------------------
# DRAWING
# ------------------------------------------------------------------------------
func _draw():
	if not show_grid: return
	for col in range(grid_size.x):
		for row in range(grid_size.y):
			var axial = _offset_to_axial(Vector2i(col, row))
			var pos = _hex_to_local(axial)
			_draw_hex_outline(pos)

func _draw_hex_outline(center: Vector2):
	var points = PackedVector2Array()
	for i in range(7):
		var angle_deg = 60 * i - 30 if orientation == 0 else 60 * i
		var rad = deg_to_rad(angle_deg)
		points.append(center + Vector2(cos(rad), sin(rad)) * hex_radius)
	draw_polyline(points, grid_color, 1.0)

func _offset_to_axial(offset: Vector2i) -> Vector2i:
	var q; var r
	if orientation == 0: # Pointy
		q = offset.x - (offset.y - (offset.y & 1)) / 2; r = offset.y
	else: # Flat
		q = offset.x; r = offset.y - (offset.x - (offset.x & 1)) / 2
	return Vector2i(q, r)