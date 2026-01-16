extends GraphNode

@onready var context_menu: PopupMenu = $PopupMenu

@export var type: Types.NodeType = Types.NodeType.NONE
@export var expected_parent_types: Array[Types.NodeType];
@export var expected_child_types: Array[Types.NodeType];

# What name will be used for the xml element
@export var xml_element_name: String = "";

@export var xml_attributes: Dictionary

## Check to define if the node is in an invalid state, should be set by the xml validation.
@export var invalid: bool:
	get:
		return invalid
	set(value):
		invalid = value
		if invalid:
			became_invalid.emit(self)
		else:
			became_valid.emit(self)

signal delete_requested(node_instance)
signal clear_connections_requested(node_instance)
signal xml_refresh_requested(node_instance)

signal became_invalid(node_instance)
signal became_valid(node_instance)

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_RIGHT and event.pressed:
			accept_event()
			show_context_menu()

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("Delete Node") and selected:
		delete_requested.emit(self)


func show_context_menu() -> void:
	context_menu.position = Vector2i(get_global_mouse_position())
	context_menu.popup()

func _ready() -> void:
	set_slot(0, true, 0, Color(1, 1, 1, 1), true, 0, Color(1, 1, 1, 1))


func _on_popup_menu_id_pressed(id: int) -> void:
	match id:
		0:
			delete_requested.emit(self)
		1:
			clear_connections_requested.emit(self)


# If a xml validation fails we should report the error visually to the node it relates.
func report_error_relating_to_xml(args):
	invalid = true

func _on_became_invalid(node_instance: Variant) -> void:
	# title = "ERROR: " + title
	turn_titlebar_red(node_instance)


func turn_titlebar_red(target_node: Control):
	# 1. Create a new StyleBoxFlat
	var red_style = StyleBoxFlat.new()
	red_style.corner_detail = 5
	
	red_style.corner_radius_bottom_left = 3
	red_style.corner_radius_bottom_right = 3
	red_style.corner_radius_top_left = 3
	red_style.corner_radius_top_right = 3

	red_style.bg_color = Color.RED
	
	red_style.content_margin_bottom = 4.0
	red_style.content_margin_left = 4.0
	red_style.content_margin_top = 4.0
	red_style.content_margin_right = 4.0
			
	target_node.add_theme_stylebox_override("titlebar", red_style)


func reset_titlebar_style(target_node: Control):
	target_node.remove_theme_stylebox_override("titlebar")

func _on_became_valid(node_instance: Variant) -> void:
	reset_titlebar_style(node_instance)
