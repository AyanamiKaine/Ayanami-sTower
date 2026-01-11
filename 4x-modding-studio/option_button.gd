extends OptionButton


func _on_item_selected(index: int) -> void:
	match index :
		0:
			tooltip_text = "Event for when a new game has been started\nand the universe populated (param = list of selected gamestart options)"
