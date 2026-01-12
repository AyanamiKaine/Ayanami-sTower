@tool
extends Node2D

# Fallback faction colors when X4DatabaseManager is not available
const FALLBACK_FACTION_COLORS: Dictionary = {
	"player": Color.BLUE,
	"yaki": Color.PURPLE,
	"buccaneers": Color.ORANGE,
	"scaleplate": Color(0.6, 0.3, 0.0), # Brown
	"antigone": Color.CYAN,
	"argon": Color.DODGER_BLUE,
	"hatikvah": Color.TEAL,
	"paranid": Color.MAGENTA,
	"teladi": Color.GREEN,
	"trinity": Color.GOLD,
	"xenon": Color.RED,
	"court": Color.CRIMSON,
	"fallensplit": Color.DARK_ORANGE,
	"freesplit": Color.YELLOW,
	"split": Color.OLIVE
}

@export var size: int = 1
@export var sector_name: String = "":
	get:
		return sector_name
	set(value):
		if sector_name_label:
			sector_name_label.text = value
		name = value + "Sector" if value else "Sector"
		sector_name = value

@export var sector_owner: String = "":
	set(value):
		sector_owner = value
		_update_owner_color()

@export var sector_ressources: Dictionary = {}
@export var sector_stations: Dictionary = {}
@export var is_selected: bool = false:
	get:
		return is_selected
	set(value):
		if hexagon_shape:
			hexagon_shape.set("outline_color", Color.RED if value else Color.WHITE)
		is_selected = value

@export var hex_grid: Node2D
@export var sector_name_label: Label
@export var hexagon_shape: Polygon2D
@export var collision_shape: CollisionPolygon2D
@export var auto_snap_to_grid: bool = true:
	get():
		return auto_snap_to_grid
	set(value):
		hexagon_shape.set("auto_snap", value)
		auto_snap_to_grid = value

signal clicked_on_sector(sector)

func _ready():
	if collision_shape and hexagon_shape:
		collision_shape.polygon = hexagon_shape.polygon
	if hexagon_shape:
		hexagon_shape.set("hex_grid", hex_grid)
	name = sector_name + "Sector" if sector_name else "Sector"
	_update_owner_color()

func _on_clickable_area_mouse_entered() -> void:
	print("Mouse entered sector" + " " + name)


func _on_clickable_area_input_event(viewport: Node, event: InputEvent, shape_idx: int) -> void:
	if event.is_action_pressed("galaxy_editor_click"):
		clicked_on_sector.emit(self)


func _on_clicked_on_sector(sector: Variant) -> void:
	is_selected = true

## Get the X4DatabaseManager node from the scene tree
func _get_database_manager() -> Node:
	# Navigate up to root and find X4DatabaseManager
	var root = get_tree().root if get_tree() else null
	if not root:
		return null
	# Search for it - it's a child of the top node
	for child in root.get_children():
		var db = child.get_node_or_null("X4DatabaseManager")
		if db:
			return db
		# Also check if the child itself is the database manager
		if child.name == "X4DatabaseManager":
			return child
	return null

## Update the sector color based on owner faction
func _update_owner_color() -> void:
	if not hexagon_shape:
		return
	
	if sector_owner.is_empty():
		#hexagon_shape.color = Color.GRAY
		# hexagon_shape.set("outline_color", Color.GRAY)
		return
	
	# Try to get color from database first, fallback to local dictionary
	var faction_colors: Dictionary = FALLBACK_FACTION_COLORS
	
	var db = _get_database_manager()
	if db and "FactionColors" in db:
		faction_colors = db.FactionColors
	
	if faction_colors.has(sector_owner):
		hexagon_shape.set("outline_color", faction_colors[sector_owner])
	else:
		hexagon_shape.set("outline_color", Color.GRAY) # Unknown faction