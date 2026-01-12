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
@export var is_selected: bool = false

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
