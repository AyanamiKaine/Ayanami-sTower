extends Control

const GraphXmlUtils = preload("res://GraphXmlUtils.cs")

@export var node_searcher: Popup
@export var context_menu: PopupMenu
@export var graph_edit: GraphEdit
@export var xml_editor_window: Window;
@export var xml_validation_timer: Timer
@export var path_to_unpacked_4x_directory: String = ""

@export var script_designer_open: bool = false

@export var script_designer: Node
@export var galaxy_map_editor: Node

@export var script_editor: PackedScene
@export var mod_explorer: Tree ## Reference to the ModExplorer Tree node

signal node_created(node)
signal node_destroyed(node)

var undo_redo: UndoRedo = UndoRedo.new()
var node_searcher_position: Vector2
var pending_connection: Dictionary = {} # Stores info about the connection to create after spawning
var allowed_types: Array = [] # The allowed types for the new node (from expected_child_types or expected_parent_types)
var drag_from_output: bool = false # True if dragging FROM a node's output, False if dragging TO a node's input

func _input(event: InputEvent) -> void:
	if event.is_action_pressed("Show Node Searcher"):
		node_searcher_position = Vector2i(get_local_mouse_position())
		# Reset allowed_types when opening node searcher manually
		allowed_types = []
		pending_connection = {}
		drag_from_output = false
		if script_designer_open:
			node_searcher.popup()
	if event.is_action_pressed("ui_undo"):
		undo_redo.undo()
	elif event.is_action_pressed("ui_redo"):
		undo_redo.redo()
	elif event.is_action_pressed("Load Graph"):
		load_graph_from_xml()

func show_context_menu() -> void:
	context_menu.position = Vector2i(get_global_mouse_position())
	# context_menu.popup()

func _on_graph_edit_connection_request(from_node: StringName, from_port: int, to_node: StringName, to_port: int) -> void:
	undo_redo.create_action("Connect Nodes")
	
	# DO: Connect
	undo_redo.add_do_method(graph_edit.connect_node.bind(from_node, from_port, to_node, to_port))
	
	# UNDO: Disconnect
	undo_redo.add_undo_method(graph_edit.disconnect_node.bind(from_node, from_port, to_node, to_port))
	
	undo_redo.commit_action()
	xml_editor_window.xml_editor.text = GraphXmlUtils.GetGraphXmlString(graph_edit)

func _on_node_clear_connections_requested(node_to_clear):
	# 1. Capture connections involving this node
	var connections_to_remove = []
	for connection in graph_edit.get_connection_list():
		if connection.from_node == node_to_clear.name or connection.to_node == node_to_clear.name:
			connections_to_remove.append(connection)
	
	# If no connections exist, don't create an undo action
	if connections_to_remove.is_empty():
		return

	undo_redo.create_action("Clear Node Connections")

	# DO: Disconnect every line attached to this node
	for conn in connections_to_remove:
		undo_redo.add_do_method(graph_edit.disconnect_node.bind(conn.from_node, conn.from_port, conn.to_node, conn.to_port))
	
	# UNDO: Re-connect the lines
	for conn in connections_to_remove:
		undo_redo.add_undo_method(graph_edit.connect_node.bind(conn.from_node, conn.from_port, conn.to_node, conn.to_port))
		
	undo_redo.commit_action()
	xml_editor_window.xml_editor.text = GraphXmlUtils.GetGraphXmlString(graph_edit)

func spawn_node(node_name: String) -> void:
	# String manipulation to match file naming convention
	var node_path = "res://nodes/%s_node.tscn" % node_name.to_lower().replace(" ", "_")
	var node_resource = load(node_path)
	
	if not node_resource:
		return
		
	var instance = node_resource.instantiate()
	node_created.emit(instance)

	# Set position
	var graph_pos = (node_searcher_position + graph_edit.scroll_offset) / graph_edit.zoom
	graph_pos = graph_pos.snapped(Vector2(graph_edit.snapping_distance, graph_edit.snapping_distance))
	instance.position_offset = graph_pos
	
	# Connect signals
	instance.delete_requested.connect(_on_node_delete_requested)
	instance.xml_refresh_requested.connect(_xml_refresh_requested)

	instance.clear_connections_requested.connect(_on_node_clear_connections_requested)
	# Create Action
	undo_redo.create_action("Spawn Node")
	
	# DO: Add child (Using .bind())
	undo_redo.add_do_method(graph_edit.add_child.bind(instance))
	
	# DO REFERENCE: Keep instance in memory if we undo
	undo_redo.add_do_reference(instance)
	
	# UNDO: Remove child (Using .bind())
	undo_redo.add_undo_method(graph_edit.remove_child.bind(instance))
	
	undo_redo.commit_action()
	
	# If there's a pending connection, create it now
	if not pending_connection.is_empty():
		# Check if this was a drag-from or drag-to scenario
		if pending_connection["from_node"] != "":
			# Drag from existing node to empty space
			var from_node = pending_connection["from_node"]
			var from_port = pending_connection["from_port"]
			var to_node = instance.name
			var to_port = 0
			
			undo_redo.create_action("Connect Nodes")
			undo_redo.add_do_method(graph_edit.connect_node.bind(from_node, from_port, to_node, to_port))
			undo_redo.add_undo_method(graph_edit.disconnect_node.bind(from_node, from_port, to_node, to_port))
			undo_redo.commit_action()
		else:
			# Drag to existing node from empty space
			var from_node = instance.name
			var from_port = 0
			var to_node = pending_connection["to_node"]
			var to_port = pending_connection["to_port"]
			
			undo_redo.create_action("Connect Nodes")
			undo_redo.add_do_method(graph_edit.connect_node.bind(from_node, from_port, to_node, to_port))
			undo_redo.add_undo_method(graph_edit.disconnect_node.bind(from_node, from_port, to_node, to_port))
			undo_redo.commit_action()
		
		# Clear the pending connection and reset allowed types
		pending_connection = {}
		allowed_types = []
	xml_editor_window.xml_editor.text = GraphXmlUtils.GetGraphXmlString(graph_edit)
	xml_validation_timer.start()

func _on_node_delete_requested(node_to_delete):
	undo_redo.create_action("Delete Node")
	node_destroyed.emit(node_to_delete)
	# Capture connections before deletion
	var connections_to_remove = []
	for connection in graph_edit.get_connection_list():
		if connection.from_node == node_to_delete.name or connection.to_node == node_to_delete.name:
			connections_to_remove.append(connection)

	# DO: 1. Disconnect the lines
	for conn in connections_to_remove:
		undo_redo.add_do_method(graph_edit.disconnect_node.bind(conn.from_node, conn.from_port, conn.to_node, conn.to_port))
	
	# DO: 2. Remove the node
	undo_redo.add_do_method(graph_edit.remove_child.bind(node_to_delete))
	
	# UNDO: 1. Add the node back
	undo_redo.add_undo_method(graph_edit.add_child.bind(node_to_delete))
	
	# UNDO REFERENCE: Keep instance in memory if we "Do" (delete it) and lose history
	undo_redo.add_undo_reference(node_to_delete)
	
	# UNDO: 2. Re-establish the connections
	for conn in connections_to_remove:
		undo_redo.add_undo_method(graph_edit.connect_node.bind(conn.from_node, conn.from_port, conn.to_node, conn.to_port))
		
	undo_redo.commit_action()
	xml_editor_window.xml_editor.text = GraphXmlUtils.GetGraphXmlString(graph_edit)
	xml_validation_timer.start()

func _remove_connections_to_node(node_name: StringName):
	for connection in graph_edit.get_connection_list():
		if connection.from_node == node_name or connection.to_node == node_name:
			graph_edit.disconnect_node(
				connection.from_node, connection.from_port,
				connection.to_node, connection.to_port
			)
	xml_editor_window.xml_editor.text = GraphXmlUtils.GetGraphXmlString(graph_edit)

func _on_graph_edit_connection_to_empty(_from_node: StringName, _from_port: int, _release_position: Vector2) -> void:
	node_searcher_position = Vector2i(get_local_mouse_position())
	
	# Get the source node's expected_child_types (what types of children it accepts)
	var source_node = graph_edit.get_node(NodePath(_from_node))
	allowed_types = source_node.expected_child_types
	drag_from_output = true # We're dragging FROM an output, show nodes whose type is in allowed_types
	
	# Store connection info to create it after spawning
	pending_connection = {
		"from_node": _from_node,
		"from_port": _from_port,
		"to_node": "", # Will be filled after spawn
		"to_port": 0
	}
	node_searcher.popup()

func _on_graph_edit_connection_from_empty(_to_node: StringName, _to_port: int, _release_position: Vector2) -> void:
	node_searcher_position = Vector2i(get_local_mouse_position())
	
	# Get the target node's expected_parent_types (what types of parents it accepts)
	var target_node = graph_edit.get_node(NodePath(_to_node))
	allowed_types = target_node.expected_parent_types
	drag_from_output = false # We're dragging TO an input, show nodes whose type is in allowed_types
	
	# Store connection info to create it after spawning
	pending_connection = {
		"from_node": "", # Will be filled after spawn
		"from_port": 0,
		"to_node": _to_node,
		"to_port": _to_port
	}
	node_searcher.popup()

func _on_button_pressed() -> void:
	var xml_string = GraphXmlUtils.GetGraphXmlString(graph_edit)
	print("XML Generated from C#:")
	print(xml_string)
	# Also validate the XML against schema
	var validation_result = GraphXmlUtils.ValidateXmlAgainstSchema(xml_string)
	xml_editor_window.output_text_edit.text = validation_result
	# Report validation errors to specific nodes
	GraphXmlUtils.ReportValidationErrorsToNodes(validation_result, graph_edit, xml_string)


func _on_xml_output_about_to_popup() -> void:
	xml_editor_window.xml_editor.text = GraphXmlUtils.GetGraphXmlString(graph_edit)


func _on_button_2_pressed() -> void:
	xml_editor_window.popup()


func _on_xml_output_close_requested() -> void:
	xml_editor_window.hide()

# Validate the current XML against its XSD schema
func validate_xml() -> String:
	var xml_string = xml_editor_window.xml_editor.text
	if xml_string.is_empty():
		return "Error: XML editor is empty"
	var result = GraphXmlUtils.ValidateXmlAgainstSchema(xml_string)
	GraphXmlUtils.ReportValidationErrorsToNodes(result, graph_edit, xml_string)
	return result

func _xml_refresh_requested(_node_instance) -> void:
	xml_editor_window.xml_editor.text = GraphXmlUtils.GetGraphXmlString(graph_edit)
	xml_validation_timer.start()

func _on_node_destroyed(_node: Variant) -> void:
	xml_editor_window.xml_editor.text = GraphXmlUtils.GetGraphXmlString(graph_edit)
	xml_validation_timer.start()

func _on_node_created(_node: Variant) -> void:
	xml_editor_window.xml_editor.text = GraphXmlUtils.GetGraphXmlString(graph_edit)
	xml_validation_timer.start()

func _on_graph_edit_end_node_move() -> void:
	xml_editor_window.xml_editor.text = GraphXmlUtils.GetGraphXmlString(graph_edit)
	xml_validation_timer.start()
# Load graph from XML in the xml_editor
func load_graph_from_xml() -> void:
	var xml_string = xml_editor_window.xml_editor.text
	if xml_string.is_empty():
		push_error("XML editor is empty")
		return
	
	# Clear the current graph
	for child in graph_edit.get_children():
		if child is GraphNode:
			child.queue_free()
	
	# Parse the XML into dictionary structure
	var root_data = GraphXmlUtils.ParseGraphFromXmlAsDict(xml_string)
	
	# Build the graph from the parsed data
	_build_graph_from_xml_data(root_data)
	graph_edit.arrange_nodes()

# Recursively build graph nodes from XML data
func _build_graph_from_xml_data(node_data: Dictionary, parent_node_name: String = "") -> String:
	# Get the element name
	var element_name = node_data.get("element_name", "")
	
	# Get the node name from the element name
	var node_name = GraphXmlUtils.GetNodeNameFromElementName(element_name)
	
	if node_name == null or node_name.is_empty():
		print("Skipping unknown XML element: " + element_name)
		return ""
	
	# Spawn the node
	node_searcher_position = Vector2(200, 200) # Default position, can be improved
	spawn_node(node_name)
	
	# Get the spawned node (last child added to graph_edit)
	var spawned_node = graph_edit.get_children()[-1]
	
	# Set attributes
	var attributes = node_data.get("attributes", {})
	if attributes != null and not attributes.is_empty():
		spawned_node.xml_attributes = attributes
	
	# Process children and create connections
	var children = node_data.get("children", [])
	for child_data in children:
		var child_node_name = _build_graph_from_xml_data(child_data, node_name)
		if not child_node_name.is_empty():
			# Create connection from current node to child
			undo_redo.create_action("Connect Nodes")
			undo_redo.add_do_method(graph_edit.connect_node.bind(spawned_node.name, 0, child_node_name, 0))
			undo_redo.commit_action()
	
	return spawned_node.name


func _on_timer_timeout() -> void:
	var xml = xml_editor_window.xml_editor.text
	var validation_result = GraphXmlUtils.ValidateXmlAgainstSchema(xml)
	xml_editor_window.output_text_edit.text = validation_result
	GraphXmlUtils.ReportValidationErrorsToNodes(validation_result, graph_edit, xml)


func _on_xml_output_window_close_requested() -> void:
	xml_editor_window.hide()


func _on_mod_designer_tab_tab_changed(tab: int) -> void:
	if tab == 0:
		script_designer_open = true
		script_designer.process_mode = PROCESS_MODE_INHERIT
		galaxy_map_editor.process_mode = PROCESS_MODE_DISABLED
	if tab == 1:
		script_designer_open = false
		script_designer.process_mode = PROCESS_MODE_DISABLED
		galaxy_map_editor.process_mode = PROCESS_MODE_INHERIT


func _on_mod_explorer_item_activated() -> void:
	if not mod_explorer:
		push_error("ModExplorer not assigned!")
		return
	
	# Get the selected item from the tree
	var selected_item = mod_explorer.get_selected()
	if not selected_item:
		return
	
	# Get the file path from the item's metadata
	var file_path: String = selected_item.get_metadata(0)
	if file_path.is_empty():
		return
	
	# Check if it's a directory (don't open directories)
	if DirAccess.dir_exists_absolute(file_path):
		return
	
	# Check if file is already open
	var tab_container = $VBoxContainer/SplitContainer/ModDesignerTab
	for i in range(tab_container.get_child_count()):
		var child = tab_container.get_child(i)
		if child.has_method("open_file") and child.current_file_path == file_path:
			# File already open, switch to that tab
			tab_container.current_tab = i
			return
	
	# Create new script editor instance
	var instance = script_editor.instantiate()
	tab_container.add_child(instance)
	
	# Open the file
	instance.open_file(file_path)
	
	# Switch to the new tab
	tab_container.current_tab = tab_container.get_child_count() - 1
