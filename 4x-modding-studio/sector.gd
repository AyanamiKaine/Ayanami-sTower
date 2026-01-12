@tool
extends Node2D

@export var size: int = 1
@export var sector_name: String = "":
	get:
		return sector_name
	set(value):
		sector_name_label.text = value
		name = value + "Sector"
		sector_name = value

@export var sector_owner: String = ""
@export var sector_ressources: Dictionary = {}
@export var sector_stations: Dictionary = {}
@export var is_selected: bool = false:
	get:
		return is_selected
	set(value):
		hexagon_shape.set("outline_color", Color.RED if value else Color.WHITE)
		is_selected = value

@export var hex_grid: Node2D
@export var sector_name_label: Label
@export var hexagon_shape: Polygon2D
@export var collision_shape: CollisionPolygon2D

signal clicked_on_sector(sector)

func _ready():
	collision_shape.polygon = hexagon_shape.polygon
	hexagon_shape.set("hex_grid", hex_grid)
	name = sector_name + "Sector"

func _on_clickable_area_mouse_entered() -> void:
	print("Mouse entered sector" + " " + name)


func _on_clickable_area_input_event(viewport: Node, event: InputEvent, shape_idx: int) -> void:
	if event.is_action_pressed("galaxy_editor_click"):
		clicked_on_sector.emit(self)


func _on_clicked_on_sector(sector: Variant) -> void:
	is_selected = true
