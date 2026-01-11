extends Control

# Connect these in the inspector or use exact node names
@onready var search_bar: LineEdit = $VBoxContainer/LineEdit
@onready var item_list: ItemList = $VBoxContainer/ItemList
@export var main_app: Node;
@export var window: Window

# THE MASTER DATA
# In a real game, this usually comes from a file, database, or resource.
var all_possible_items: Array[String] = [
	"Cue", "Debug Text", "Actions", "Mission", "Set Value",
	"Conditions", "Event Happened", "Show Help", "Write To Logbook",
	"Delay", "Cues", "Check Any", "Check All", "Check Value"
]

# Reference to Types enum from Types.gd
var NodeTypes = preload("res://nodes/Types.gd")

func _input(event: InputEvent) -> void:
	if event.is_action("ui_accept"):
		# Only select and process if there are items in the list
		if item_list.item_count > 0:
			item_list.select(0)
			item_list.emit_signal("item_selected", 0)

func _ready() -> void:
	# 1. Fill the list initially with everything
	update_item_list("")
	all_possible_items.sort()
	# 2. Connect the text signal
	# When text changes, we re-run the filter
	search_bar.text_changed.connect(_on_search_text_changed)
	
	# Optional: Handle selection
	item_list.item_selected.connect(_on_item_selected)

func _on_search_text_changed(new_text: String) -> void:
	update_item_list(new_text)

func update_item_list(filter_text: String) -> void:
	# 1. Clear the current visual list
	item_list.clear()
	
	# 2. Normalize input to lowercase for case-insensitive matching
	filter_text = filter_text.to_lower()
	
	# Get the allowed types from the Control node (grandparent)
	var allowed_types: Array = main_app.allowed_types
	
	# 3. Find Matches and Score them
	var matches: Array[Dictionary] = []
	
	for item_name in all_possible_items:
		var item_lower = item_name.to_lower()
		
		# Check if the item matches the search filter
		if filter_text != "" and filter_text not in item_lower:
			continue # Skip if filter text doesn't match
		
		# Check slot type compatibility (only if allowed types are set)
		if not allowed_types.is_empty():
			if not _is_node_type_compatible(item_name, allowed_types):
				continue # Skip this node as it's not compatible
		
		var score = 0
		
		# --- SCORING LOGIC ---
		# We give higher points for better matches
		if filter_text != "":
			if item_lower == filter_text:
				score = 100 # Exact match (Best)
			elif item_lower.begins_with(filter_text):
				score = 50 # Starts with the text (Good)
			else:
				score = 10 # Contains text somewhere (Okay)
		else:
			score = 0 # No filter, show all with neutral score
		
		matches.append({"text": item_name, "score": score})
	
	# 4. Sort Matches by Score (Highest score first)
	matches.sort_custom(func(a, b): return a.score > b.score)
	
	# 5. Populate the ItemList with the sorted results
	for match_data in matches:
		item_list.add_item(match_data.text)

# Check if a node's type is in the allowed types array
func _is_node_type_compatible(node_name: String, allowed_types: Array) -> bool:
	#print("--- Checking node: ", node_name, " ---")
	#print("  allowed_types: ", allowed_types)
	# If allowed_types is empty, no filter is applied (show all)
	if allowed_types.is_empty():
		#print("  RESULT: SHOWN (no filter, allowed_types is empty)")
		return true
	
	# Load the node resource to check its type
	var node_path = "res://nodes/%s_node.tscn" % node_name.to_lower().replace(" ", "_")
	#print("  Loading: ", node_path)
	var node_resource = load(node_path)
	
	if not node_resource:
		#print("  RESULT: HIDDEN (could not load resource)")
		return false
	
	var instance = node_resource.instantiate()
	
	if not instance:
		#print("  RESULT: HIDDEN (could not instantiate)")
		return false
	
	# Get the node's type
	var node_type = instance.get("type")
	#print("  node type: ", node_type)
	
	instance.queue_free()
	
	# Check if the node's type is in the allowed_types array
	for allowed_type in allowed_types:
		#print("    Comparing: node_type ", int(node_type), " vs allowed_type ", int(allowed_type))
		if int(node_type) == int(allowed_type):
			#print("  RESULT: SHOWN (node type is in allowed_types)")
			return true
	
	#print("  RESULT: HIDDEN (node type not in allowed_types)")
	return false

func _on_item_selected(index: int) -> void:
	# Check if index is valid
	if index < 0 or index >= item_list.item_count:
		return
	
	# Important: Because the list order changes, we must rely on the TEXT, not the index.
	var selected_text = item_list.get_item_text(index)
	
	# Don't spawn if text is empty
	if selected_text.is_empty():
		return
	
	main_app.spawn_node(selected_text)
	window.hide()
	

func _on_node_searcher_about_to_popup() -> void:
	search_bar.grab_focus()
	search_bar.clear()
	# Update the list after clearing
	update_item_list("")
