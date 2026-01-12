@tool
extends Node2D

# Fallback faction colors when X4DatabaseManager is not available
const FALLBACK_FACTION_COLORS: Dictionary = {
	"player": Color.BLUE,
	"yaki": Color.PURPLE,
	"buccaneers": Color.ORANGE,
	"scaleplate": Color(0.6, 0.3, 0.0), # Brown
	"argon federation": Color.DODGER_BLUE,
	"antigone republic": Color.CYAN,
	"hatikvah free league": Color.TEAL,
	"godrealm of the paranid": Color.MAGENTA,
	"holy order of the pontifex": Color.PINK,
	"alliance of the word": Color.LIGHT_GREEN,
	"ministry of finance": Color.BROWN,
	"terran protectorate": Color.DARK_GREEN,
	"segaris pioneers": Color.LIGHT_YELLOW,
	"queendom of boron": Color.DARK_BLUE,
	"vigor syndicate": Color.BEIGE,
	"riptide rakers": Color.WEB_PURPLE,
	"teladi": Color.GREEN,
	"trinity": Color.GOLD,
	"xenon": Color.RED,
	"court": Color.CRIMSON,
	"fallensplit": Color.DARK_ORANGE,
	"freesplit": Color.YELLOW,
	"split": Color.OLIVE,
	"neutral": Color.GRAY
}

@export var data_vbox_container: VBoxContainer
@export var sector_owner_label: Label
@export var sector_tooltip: PopupPanel

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
		sector_owner_label.text = "Owner:" + " " + value.capitalize()
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
@export var tooltip_delay: float = 0.4 ## Delay in seconds before tooltip appears
@export var auto_snap_to_grid: bool = true:
	get():
		return auto_snap_to_grid
	set(value):
		hexagon_shape.set("auto_snap", value)
		auto_snap_to_grid = value

signal clicked_on_sector(sector)

var _tooltip_timer: Timer
var _is_mouse_hovering: bool = false

func _ready():
	# Create tooltip delay timer
	_tooltip_timer = Timer.new()
	_tooltip_timer.one_shot = true
	_tooltip_timer.timeout.connect(_on_tooltip_timer_timeout)
	add_child(_tooltip_timer)
	
	if collision_shape and hexagon_shape:
		collision_shape.polygon = hexagon_shape.polygon
	if hexagon_shape:
		hexagon_shape.set("hex_grid", hex_grid)
	name = sector_name + "Sector" if sector_name else "Sector"
	_update_owner_color()

func _on_clickable_area_mouse_entered() -> void:
	print("Mouse entered sector" + " " + name)
	_is_mouse_hovering = true
	_start_tooltip_timer()


func _on_clickable_area_mouse_exited() -> void:
	_is_mouse_hovering = false
	_cancel_tooltip_timer()
	_hide_tooltip()


func _start_tooltip_timer() -> void:
	if _tooltip_timer:
		_tooltip_timer.start(tooltip_delay)


func _cancel_tooltip_timer() -> void:
	if _tooltip_timer:
		_tooltip_timer.stop()


func _on_tooltip_timer_timeout() -> void:
	# Only show if mouse is still hovering
	if _is_mouse_hovering:
		_show_tooltip()


func _show_tooltip() -> void:
	if not sector_tooltip:
		return
	
	# Make tooltip not block mouse input (allows zooming etc.)
	# PopupPanel is a Window, so we use unfocusable and transient flags
	sector_tooltip.unfocusable = true
	sector_tooltip.transient = true
	sector_tooltip.exclusive = false
	
	# Get the actual screen mouse position using DisplayServer
	# This works correctly even when inside a SubViewport
	var mouse_pos = DisplayServer.mouse_get_position()
	# Convert from display coordinates to window coordinates
	var window_pos = get_window().position
	var local_mouse_pos = mouse_pos - window_pos
	
	sector_tooltip.position = Vector2(local_mouse_pos.x + 15, local_mouse_pos.y + 15)
	sector_tooltip.show()


func _hide_tooltip() -> void:
	if sector_tooltip:
		sector_tooltip.hide()


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
