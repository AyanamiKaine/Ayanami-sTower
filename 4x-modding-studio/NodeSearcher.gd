extends Control

# Connect these in the inspector or use exact node names
@onready var search_bar: LineEdit = $VBoxContainer/LineEdit
@onready var item_list: ItemList = $VBoxContainer/ItemList
@export var main_app: Node
@export var window: Window

## Reference to the NodeGenerator (C#)
@export var node_generator: Node

## Reference to the DatabaseManager (C#)
@export var database_manager: Node

## If true, use dynamic node generation from database. If false, use hand-crafted nodes only.
@export var use_dynamic_generation: bool = true

## Show only elements that have documentation (filters out internal/undocumented elements)
@export var show_documented_only: bool = false

## Maximum items to show in the list (for performance)
@export var max_display_items: int = 100

# THE MASTER DATA - Hand-crafted nodes (fallback)
var handcrafted_nodes: Array[String] = [
	"Cue", "Debug Text", "Actions", "Mission", "Set Value",
	"Conditions", "Event Happened", "Show Help", "Write To Logbook",
	"Delay", "Cues", "Check Any", "Check All", "Check Value"
]

# Dynamic elements from database (populated when database is ready)
var all_elements: Array[Dictionary] = [] # { name, display_name, documentation, category }
var elements_by_category: Dictionary = {} # category -> Array of element dictionaries
var is_database_loaded: bool = false

# Reference to Types enum from Types.gd
var NodeTypes = preload("res://nodes/Types.gd")

func _input(event: InputEvent) -> void:
	if event.is_action("ui_accept"):
		# Only select and process if there are items in the list
		if item_list.item_count > 0:
			item_list.select(0)
			item_list.emit_signal("item_selected", 0)

func _ready() -> void:
	# 1. Connect the text signal
	search_bar.text_changed.connect(_on_search_text_changed)
	
	# 2. Handle selection
	item_list.item_selected.connect(_on_item_selected)
	
	# 3. Initial population with hand-crafted nodes
	handcrafted_nodes.sort()
	update_item_list("")
	
	# 4. Try to load from database if available
	if use_dynamic_generation:
		_try_load_from_database()
		
		# Also set up a timer to retry loading (database loads async)
		var retry_timer = Timer.new()
		retry_timer.wait_time = 1.0
		retry_timer.one_shot = false
		retry_timer.timeout.connect(_try_load_from_database)
		add_child(retry_timer)
		retry_timer.start()

func _try_load_from_database() -> void:
	if is_database_loaded:
		return # Already loaded
	
	if database_manager == null:
		print("[NodeSearcher] database_manager is null")
		return # Not configured
	
	if node_generator == null:
		print("[NodeSearcher] node_generator is null")
		return # Not configured
	
	# Check if database has elements using GDScript-callable method
	# (C# Dictionary properties aren't directly accessible from GDScript)
	var element_count: int = database_manager.GetElementCount()
	if element_count == 0:
		print("[NodeSearcher] Database not ready yet (0 elements)")
		return # Not ready yet
	
	print("[NodeSearcher] Database ready with ", element_count, " elements")
	
	# Load elements from database
	_load_elements_from_database()
	is_database_loaded = true
	
	# Update the list
	update_item_list(search_bar.text)
	
	print("[NodeSearcher] Loaded %d elements from database" % all_elements.size())

func _load_elements_from_database() -> void:
	if node_generator == null:
		print("[NodeSearcher] _load_elements_from_database: node_generator is null")
		return
	
	all_elements.clear()
	elements_by_category.clear()
	
	# Try calling the Godot-compatible C# method
	print("[NodeSearcher] Calling GetElementsForSearcherGodot...")
	var elements_data = node_generator.GetElementsForSearcherGodot()
	print("[NodeSearcher] GetElementsForSearcherGodot returned: ", typeof(elements_data), " with ", elements_data.size() if elements_data else 0, " items")
	
	if elements_data == null or elements_data.size() == 0:
		print("[NodeSearcher] No elements returned from GetElementsForSearcherGodot")
		# Fallback: iterate AllElements directly
		_load_elements_directly_from_database()
		return
	
	for elem_array in elements_data:
		# elem_array is [uniqueKey, displayName, documentation, category, sourceContext]
		var unique_key: String = elem_array[0] if elem_array.size() > 0 else ""
		var display_name: String = elem_array[1] if elem_array.size() > 1 else ""
		var documentation: String = elem_array[2] if elem_array.size() > 2 else ""
		var category: String = elem_array[3] if elem_array.size() > 3 else ""
		var source_context: String = elem_array[4] if elem_array.size() > 4 else ""
		
		if unique_key.is_empty():
			continue
		
		# Skip undocumented elements if filter is enabled
		if show_documented_only and documentation.is_empty():
			continue
		
		var elem_dict = {
			"name": unique_key, # Use uniqueKey for node creation
			"display_name": display_name,
			"documentation": documentation,
			"category": category,
			"source_context": source_context,
			"is_dynamic": true
		}
		
		all_elements.append(elem_dict)
		
		if not elements_by_category.has(category):
			elements_by_category[category] = []
		elements_by_category[category].append(elem_dict)
	
	print("[NodeSearcher] Loaded ", all_elements.size(), " elements via GetElementsForSearcherGodot")

## Fallback method to load directly from AllElements dictionary
func _load_elements_directly_from_database() -> void:
	print("[NodeSearcher] Using fallback: loading element names directly")
	
	# Use GDScript-callable method to get element names
	var element_names = database_manager.GetAllElementNames()
	
	if element_names == null or element_names.size() == 0:
		print("[NodeSearcher] No element names returned in fallback")
		return
	
	for elem_name in element_names:
		var display_name = _format_element_name(elem_name)
		# In fallback mode we don't have full element info, just names
		var category = _determine_category(elem_name, [])
		
		var elem_dict = {
			"name": elem_name,
			"display_name": display_name,
			"documentation": "",
			"category": category,
			"is_dynamic": true
		}
		
		all_elements.append(elem_dict)
		
		if not elements_by_category.has(category):
			elements_by_category[category] = []
		elements_by_category[category].append(elem_dict)
	
	print("[NodeSearcher] Loaded ", all_elements.size(), " elements via fallback")

## Format element name for display (e.g., "set_value" -> "Set Value")
func _format_element_name(elem_name: String) -> String:
	var words = elem_name.split("_")
	var formatted: Array[String] = []
	for word in words:
		if word.length() > 0:
			formatted.append(word[0].to_upper() + word.substr(1).to_lower())
	return " ".join(formatted)

## Determine category based on element name and attribute groups
func _determine_category(elem_name: String, attr_groups) -> String:
	if attr_groups:
		for group in attr_groups:
			if str(group) == "action":
				return "Actions"
			if str(group) == "condition":
				return "Conditions"
	
	if elem_name.begins_with("event_"):
		return "Events"
	if elem_name == "cue" or elem_name == "cues":
		return "Cues"
	
	return "Other"

func _on_search_text_changed(new_text: String) -> void:
	update_item_list(new_text)

func update_item_list(filter_text: String) -> void:
	# 1. Clear the current visual list
	item_list.clear()
	
	# 2. Normalize input to lowercase for case-insensitive matching
	filter_text = filter_text.to_lower()
	
	# Get the allowed types from the Control node (grandparent)
	var allowed_types: Array = main_app.allowed_types if main_app else []
	
	# Get allowed element names based on pending connection
	var allowed_element_names: Array[String] = _get_allowed_elements_from_connection()
	
	# Detect current context (e.g. "md", "aiscripts") based on existing nodes
	var required_context: String = _detect_current_context()
	if not required_context.is_empty():
		print("[NodeSearcher] Filtering context: ", required_context)

	# 3. Find Matches and Score them
	var matches: Array[Dictionary] = []
	
	# Use dynamic elements if loaded, otherwise fall back to hand-crafted
	if is_database_loaded and use_dynamic_generation:
		matches = _search_dynamic_elements(filter_text, allowed_types, allowed_element_names, required_context)
	else:
		matches = _search_handcrafted_nodes(filter_text, allowed_types, allowed_element_names)
	
	# 4. Sort Matches by Score (Highest score first)
	matches.sort_custom(func(a, b): return a.score > b.score)
	
	# 5. Limit results for performance
	if matches.size() > max_display_items:
		matches.resize(max_display_items)
	
	# 6. Populate the ItemList with the sorted results
	for match_data in matches:
		var display_text = match_data.display_name
		
		# Add category suffix for dynamic elements
		if match_data.get("is_dynamic", false) and match_data.has("category"):
			display_text += "  [%s]" % match_data.category
		
		var idx = item_list.add_item(display_text)
		
		# Store the actual element name as metadata
		item_list.set_item_metadata(idx, match_data.name)
		
		# Set tooltip with documentation
		if match_data.has("documentation") and not match_data.documentation.is_empty():
			item_list.set_item_tooltip(idx, match_data.documentation)
		
		# Color code by category
		if match_data.get("is_dynamic", false):
			var color = _get_category_color(match_data.get("category", ""))
			item_list.set_item_custom_fg_color(idx, color)

func _detect_current_context() -> String:
	if main_app == null or main_app.graph_edit == null:
		return ""
	
	# Look for specific root nodes first
	for child in main_app.graph_edit.get_children():
		if child is GraphNode:
			var elem_name = child.get("xml_element_name")
			if elem_name == "mdscript":
				return "md"
			if elem_name == "aiscript":
				return "aiscripts"
	
	# If no specific root found, try to infer from any node
	# (This assumes typically you don't mix md and aiscript nodes in one file)
	for child in main_app.graph_edit.get_children():
		if child is GraphNode:
			var elem_name = child.get("xml_element_name")
			# Find this element in our db to check its context
			for db_elem in all_elements:
				if db_elem.name == elem_name:
					var ctx = db_elem.get("source_context", "")
					if not ctx.is_empty():
						return ctx
	return ""

## Gets allowed element names based on the pending connection
## Returns empty array if no filtering needed
func _get_allowed_elements_from_connection() -> Array[String]:
	var allowed: Array[String] = []
	
	if main_app == null or node_generator == null:
		return allowed
	
	var pending = main_app.pending_connection
	if pending.is_empty():
		return allowed # No connection pending, show all elements
	
	var graph_edit = main_app.graph_edit
	if graph_edit == null:
		return allowed
	
	if main_app.drag_from_output:
		# Dragging FROM output - need elements that can be children of the source
		var from_node_name = pending.get("from_node", "")
		if from_node_name.is_empty():
			return allowed
		
		var source_node = graph_edit.get_node_or_null(NodePath(from_node_name))
		if source_node == null:
			return allowed
		
		# Get the xml element name from the source node
		var source_element = source_node.get("xml_element_name")
		if source_element == null or (source_element is String and source_element.is_empty()):
			return allowed
		
		# Get allowed children for this element
		allowed = Array(node_generator.GetAllowedChildrenGodot(source_element), TYPE_STRING, "", null)
		print("[NodeSearcher] Filtering by allowed children of '%s': %d elements" % [source_element, allowed.size()])
	else:
		# Dragging TO input - need elements that can be parents of the target
		var to_node_name = pending.get("to_node", "")
		if to_node_name.is_empty():
			return allowed
		
		var target_node = graph_edit.get_node_or_null(NodePath(to_node_name))
		if target_node == null:
			return allowed
		
		# Get the xml element name from the target node
		var target_element = target_node.get("xml_element_name")
		if target_element == null or (target_element is String and target_element.is_empty()):
			return allowed
		
		# Get allowed parents for this element
		allowed = Array(node_generator.GetAllowedParentsGodot(target_element), TYPE_STRING, "", null)
		print("[NodeSearcher] Filtering by allowed parents of '%s': %d elements" % [target_element, allowed.size()])
	
	return allowed

func _search_dynamic_elements(filter_text: String, allowed_types: Array, allowed_element_names: Array[String] = [], required_context: String = "") -> Array[Dictionary]:
	var matches: Array[Dictionary] = []
	
	
	for elem in all_elements:
		var unique_key: String = elem.name # This is now the uniqueKey
		var display_name: String = elem.display_name
		var documentation: String = elem.documentation
		var unique_key_lower = unique_key.to_lower()
		var display_lower = display_name.to_lower()
		var source_context = elem.get("source_context", "")

		# Context Filtering
		if not required_context.is_empty():
			# If the element has a specific context, it MUST match the required context
			if not source_context.is_empty() and source_context != required_context:
				continue
		
		# Extract base name from unique key (e.g., "actions@md" -> "actions")
		var base_name = unique_key.split("@")[0] if "@" in unique_key else unique_key
		
		# Filter by allowed element names if specified (from pending connection)
		if not allowed_element_names.is_empty():
			# Check if either the unique key or base name is in allowed list
			if unique_key not in allowed_element_names and base_name not in allowed_element_names:
				continue
		
		# Check if the item matches the search filter
		var matches_filter = filter_text.is_empty()
		if not matches_filter:
			matches_filter = unique_key_lower.contains(filter_text) or \
							 display_lower.contains(filter_text) or \
							 documentation.to_lower().contains(filter_text)
		
		if not matches_filter:
			continue
		
		var score = _calculate_match_score(unique_key, display_name, filter_text)
		
		matches.append({
			"name": unique_key, # Use unique key for node creation
			"display_name": display_name,
			"documentation": documentation,
			"category": elem.category,
			"is_dynamic": true,
			"score": score
		})
	
	return matches

func _search_handcrafted_nodes(filter_text: String, allowed_types: Array, allowed_element_names: Array[String] = []) -> Array[Dictionary]:
	var matches: Array[Dictionary] = []
	
	for item_name in handcrafted_nodes:
		var item_lower = item_name.to_lower()
		# Convert display name to element name format (e.g., "Debug Text" -> "debug_text")
		var elem_name = item_name.to_lower().replace(" ", "_")
		
		# Filter by allowed element names if specified
		if not allowed_element_names.is_empty():
			if elem_name not in allowed_element_names:
				continue
		
		# Check if the item matches the search filter
		if not filter_text.is_empty() and filter_text not in item_lower:
			continue
		
		# Check slot type compatibility (only if allowed types are set)
		if not allowed_types.is_empty():
			if not _is_node_type_compatible(item_name, allowed_types):
				continue
		
		var score = _calculate_match_score(item_name, item_name, filter_text)
		
		matches.append({
			"name": item_name,
			"display_name": item_name,
			"documentation": "",
			"is_dynamic": false,
			"score": score
		})
	
	return matches

func _calculate_match_score(elem_name: String, display_name: String, filter_text: String) -> int:
	if filter_text.is_empty():
		return 0
	
	var elem_lower = elem_name.to_lower()
	var display_lower = display_name.to_lower()
	
	# Exact match (best)
	if elem_lower == filter_text or display_lower == filter_text:
		return 100
	# Starts with filter (good)
	elif elem_lower.begins_with(filter_text) or display_lower.begins_with(filter_text):
		return 50
	# Contains filter (okay)
	else:
		return 10

func _get_category_color(category: String) -> Color:
	match category:
		"Actions":
			return Color.LIGHT_GREEN
		"Conditions":
			return Color.LIGHT_BLUE
		"Events":
			return Color.ORANGE
		"Cues":
			return Color.YELLOW
		"Groups":
			return Color.GRAY
		_:
			return Color.WHITE

# Check if a node's type is in the allowed types array (for hand-crafted nodes)
func _is_node_type_compatible(node_name: String, allowed_types: Array) -> bool:
	if allowed_types.is_empty():
		return true
	
	var node_path = "res://nodes/%s_node.tscn" % node_name.to_lower().replace(" ", "_")
	var node_resource = load(node_path)
	
	if not node_resource:
		return false
	
	var instance = node_resource.instantiate()
	if not instance:
		return false
	
	var node_type = instance.get("type")
	instance.queue_free()
	
	for allowed_type in allowed_types:
		if int(node_type) == int(allowed_type):
			return true
	
	return false

func _on_item_selected(index: int) -> void:
	if index < 0 or index >= item_list.item_count:
		return
	
	# Get the element name from metadata (or text for hand-crafted nodes)
	var element_name = item_list.get_item_metadata(index)
	if element_name == null or (element_name is String and element_name.is_empty()):
		element_name = item_list.get_item_text(index)
		# Remove category suffix if present
		if " [" in element_name:
			element_name = element_name.split(" [")[0].strip_edges()
	
	if element_name.is_empty():
		return
	
	# PRIORITY: Always check for hand-crafted node first
	if _has_handcrafted_node(element_name):
		# Use hand-crafted node (convert element name to display name format)
		var display_name = _format_element_name(element_name)
		main_app.spawn_node(display_name)
	elif node_generator != null:
		# Fall back to dynamic generation
		_spawn_dynamic_node(element_name)
	else:
		# Last resort: try spawning by name
		main_app.spawn_node(element_name)
	
	window.hide()

## Check if a hand-crafted node scene exists for this element
func _has_handcrafted_node(element_name: String) -> bool:
	# Convert element name to scene path (e.g., "cue" -> "res://nodes/cue_node.tscn")
	var scene_name = element_name.to_lower().replace(" ", "_")
	var scene_path = "res://nodes/%s_node.tscn" % scene_name
	
	# Check if the scene file exists
	return ResourceLoader.exists(scene_path)

func _is_dynamic_element(element_name: String) -> bool:
	# Check if this element exists in our dynamic elements list
	# BUT also check that no hand-crafted node exists (hand-crafted takes priority)
	if _has_handcrafted_node(element_name):
		return false
	
	for elem in all_elements:
		if elem.name == element_name:
			return true
	return false

func _spawn_dynamic_node(element_name: String) -> void:
	if node_generator == null:
		push_error("NodeSearcher: node_generator not set")
		return
	
	var node_instance = node_generator.CreateNodeForElement(element_name)
	if node_instance == null:
		push_error("NodeSearcher: Failed to create node for element: " + element_name)
		return
	
	# Emit signal so main_app can handle the node
	if main_app.has_method("add_dynamic_node"):
		main_app.add_dynamic_node(node_instance)
	else:
		# Fallback: Add directly to graph_edit
		var graph_edit = main_app.get("graph_edit")
		if graph_edit:
			# Set position
			var graph_pos = (main_app.node_searcher_position + graph_edit.scroll_offset) / graph_edit.zoom
			graph_pos = graph_pos.snapped(Vector2(graph_edit.snapping_distance, graph_edit.snapping_distance))
			node_instance.position_offset = graph_pos
			
			# Connect signals
			if node_instance.has_signal("delete_requested"):
				node_instance.delete_requested.connect(main_app._on_node_delete_requested)
			if node_instance.has_signal("xml_refresh_requested"):
				node_instance.xml_refresh_requested.connect(main_app._xml_refresh_requested)
			if node_instance.has_signal("clear_connections_requested"):
				node_instance.clear_connections_requested.connect(main_app._on_node_clear_connections_requested)
			
			graph_edit.add_child(node_instance)
			
			# Handle pending connection
			if not main_app.pending_connection.is_empty():
				_handle_pending_connection(node_instance)
			
			# Refresh XML
			main_app._xml_refresh_requested(node_instance)

func _handle_pending_connection(new_node: GraphNode) -> void:
	var pending = main_app.pending_connection
	var graph_edit = main_app.graph_edit
	
	if main_app.drag_from_output:
		# Dragging from output to new node's input
		graph_edit.connect_node(pending.from_node, pending.from_port, new_node.name, 0)
	else:
		# Dragging from input to new node's output
		graph_edit.connect_node(new_node.name, 0, pending.to_node, pending.to_port)
	
	main_app.pending_connection = {}

func _on_node_searcher_about_to_popup() -> void:
	search_bar.grab_focus()
	search_bar.clear()
	update_item_list("")
