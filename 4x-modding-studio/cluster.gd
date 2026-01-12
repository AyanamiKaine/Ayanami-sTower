@tool
extends Node2D

@export var cluster_owner: String = ""
@export var max_sectors: int = 3 ## Maximum sectors this cluster can hold

# --- CLUSTER VISUALS ---
@export_group("Cluster Hexagon")
@export var cluster_radius: float = 100.0: ## Radius of the cluster hexagon
	set(value):
		cluster_radius = value
		_update_cluster_hexagon()
		_calculate_sector_layout()

@export_enum("Pointy Topped", "Flat Topped") var orientation: int = 0:
	set(value):
		orientation = value
		_update_cluster_hexagon()
		_calculate_sector_layout()

# --- SECTOR LAYOUT ---
@export_group("Sector Layout")
@export var sector_radius: float = 25.0: ## Radius for sector hexagons inside cluster
	set(value):
		sector_radius = value
		_calculate_sector_layout()
		_update_sector_sizes()

@export_enum("Triangle", "Horizontal", "Vertical") var sector_arrangement: int = 0:
	set(value):
		sector_arrangement = value
		_calculate_sector_layout()

@export_range(0.0, 360.0, 15.0) var arrangement_rotation: float = 0.0: ## Rotate arrangement
	set(value):
		arrangement_rotation = value
		_calculate_sector_layout()

@export var layout_info: String = "":
	get:
		return _get_layout_info()

@export var reposition_sectors: bool = false: ## Manual reposition button
	set(value):
		if value and Engine.is_editor_hint():
			_collect_child_sectors()
			_calculate_sector_layout()

# --- NODE REFERENCES ---
@export_group("Node References")
@export var hex_grid: Node2D
@export var hexagon_shape: Polygon2D

# --- INTERNAL ---
var sectors: Array[Node2D] = []
var sector_positions: Array[Vector2] = [] ## Calculated valid positions for sectors

signal sector_added(sector: Node2D)
signal sector_removed(sector: Node2D)

func _ready():
	_update_cluster_hexagon()
	_collect_child_sectors()
	_calculate_sector_layout()
	
	# Connect to child changes
	child_entered_tree.connect(_on_child_entered)
	child_exiting_tree.connect(_on_child_exiting)

func _on_child_entered(node: Node) -> void:
	# Check if it's a sector (has sector_name property or is from sector.tscn)
	if _is_sector_node(node) and node not in sectors:
		call_deferred("_collect_child_sectors")
		call_deferred("_calculate_sector_layout")

func _on_child_exiting(node: Node) -> void:
	if node in sectors:
		sectors.erase(node)
		call_deferred("_reposition_sectors")

## Check if a node is a sector
func _is_sector_node(node: Node) -> bool:
	if not node is Node2D:
		return false
	# Check if it has sector properties or hexagon_shape
	return "sector_name" in node or "hexagon_shape" in node

## Collect all child sectors
func _collect_child_sectors() -> void:
	sectors.clear()
	for child in get_children():
		if _is_sector_node(child):
			sectors.append(child)

# --- CLUSTER HEXAGON ---
func _update_cluster_hexagon() -> void:
	if not hexagon_shape:
		return
	hexagon_shape.set("radius", cluster_radius)
	hexagon_shape.set("orientation", orientation)

# --- SECTOR LAYOUT CALCULATION ---
## Calculate optimal positions for sectors inside the cluster
func _calculate_sector_layout() -> void:
	sector_positions.clear()
	
	# Calculate spacing needed between sector centers to avoid overlap
	# Two hexagons touch when center distance = 2 * radius * cos(30°) for pointy-topped
	var min_spacing = sector_radius * 2.0 * 1.05 # 5% gap
	
	var rotation_rad = deg_to_rad(arrangement_rotation)
	
	match sector_arrangement:
		0: # Triangle - most space efficient for 3 hexagons
			# Equilateral triangle arrangement
			var triangle_height = min_spacing * 0.866 # sin(60°)
			sector_positions = [
				Vector2(0, -triangle_height * 0.5), # Top
				Vector2(-min_spacing * 0.5, triangle_height * 0.5), # Bottom-left
				Vector2(min_spacing * 0.5, triangle_height * 0.5) # Bottom-right
			]
		1: # Horizontal - side by side
			sector_positions = [
				Vector2(-min_spacing, 0), # Left
				Vector2(0, 0), # Center
				Vector2(min_spacing, 0) # Right
			]
		2: # Vertical - stacked
			sector_positions = [
				Vector2(0, -min_spacing), # Top
				Vector2(0, 0), # Center
				Vector2(0, min_spacing) # Bottom
			]
	
	# Apply rotation
	for i in range(sector_positions.size()):
		sector_positions[i] = sector_positions[i].rotated(rotation_rad)
	
	# Reposition existing sectors
	_reposition_sectors()

## Reposition all sectors to their calculated positions
func _reposition_sectors() -> void:
	for i in range(sectors.size()):
		if i < sector_positions.size():
			sectors[i].position = sector_positions[i]

## Update all sector hexagon sizes
func _update_sector_sizes() -> void:
	for sector in sectors:
		if "hexagon_shape" in sector and sector.hexagon_shape:
			sector.hexagon_shape.set("radius", sector_radius)

## Get all sectors in this cluster
func get_sectors() -> Array[Node2D]:
	return sectors

## Get sector count
func get_sector_count() -> int:
	return sectors.size()

## Get layout information
func _get_layout_info() -> String:
	_collect_child_sectors()
	var min_spacing = sector_radius * 2.0 * 1.05
	var fits = _check_sectors_fit()
	var sector_count = sectors.size()
	
	if sector_count == 0:
		return "No sectors. Add sector scenes as children."
	elif sector_count > max_sectors:
		return "⚠️ Too many sectors! Max: %d, Current: %d" % [max_sectors, sector_count]
	elif fits:
		return "✓ %d sector(s) | Radius: %.1f | Spacing: %.1f" % [sector_count, sector_radius, min_spacing]
	else:
		var max_sector_radius = _calculate_max_sector_radius()
		return "⚠️ Too large! Max radius: %.1f" % max_sector_radius

## Check if sectors fit inside cluster
func _check_sectors_fit() -> bool:
	if sector_positions.is_empty():
		return true
	
	for pos in sector_positions:
		# Check if sector at this position would extend outside cluster
		if pos.length() + sector_radius > cluster_radius * 0.95:
			return false
	return true

## Calculate maximum sector radius that fits
func _calculate_max_sector_radius() -> float:
	if sector_positions.is_empty():
		return cluster_radius * 0.3
	
	var max_distance = 0.0
	for pos in sector_positions:
		max_distance = max(max_distance, pos.length())
	
	# Account for the position offset
	return (cluster_radius * 0.95 - max_distance) if max_distance > 0 else cluster_radius * 0.3
