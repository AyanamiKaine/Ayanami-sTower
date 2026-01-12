extends TabContainer

## List of tab indices that should NOT have close buttons (e.g., Script Designer, Galaxy Map)
@export var permanent_tabs: Array[int] = [0, 1]

func _ready():
	# Get the internal TabBar and enable close buttons
	var tab_bar = get_tab_bar()
	tab_bar.tab_close_display_policy = TabBar.CLOSE_BUTTON_SHOW_ACTIVE_ONLY
	
	# Connect the close signal from TabBar
	tab_bar.tab_close_pressed.connect(_on_tab_close_pressed)
	
	# Connect child signals to update close buttons
	child_entered_tree.connect(_on_child_entered_tree)

func _on_tab_close_pressed(tab_idx: int) -> void:
	# Don't close permanent tabs
	if tab_idx in permanent_tabs:
		return
	
	var tab_node = get_child(tab_idx)
	if not tab_node:
		return
	
	# Check if the tab has unsaved changes
	if tab_node.has_method("save_file") and "is_modified" in tab_node and tab_node.is_modified:
		# TODO: Show confirmation dialog
		# For now, just warn and close anyway
		print("Warning: Closing tab with unsaved changes: ", tab_node.name)
	
	# Remove and free the tab
	remove_child(tab_node)
	tab_node.queue_free()

## Called when tabs change to update close button visibility
func _on_child_entered_tree(_node: Node) -> void:
	call_deferred("_update_close_buttons")

func _update_close_buttons() -> void:
	var tab_bar = get_tab_bar()
	for i in range(get_tab_count()):
		# Hide close button for permanent tabs by setting button to null or using different policy
		if i in permanent_tabs:
			tab_bar.set_tab_button_icon(i, null)
		# Non-permanent tabs will show close button based on tab_close_display_policy
