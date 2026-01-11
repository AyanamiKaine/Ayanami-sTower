extends GraphEdit


func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_RIGHT and event.pressed:
			pass
			#get_parent().context_menu_position = get_local_mouse_position()
			#get_parent().show_context_menu()


func show_context_menu() -> void:
	get_parent().context_menu.position = Vector2i(get_global_mouse_position())
	
	get_parent().context_menu.popup()
