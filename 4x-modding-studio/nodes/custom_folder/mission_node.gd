extends "res://nodes/base_node.gd"

func _ready():
	super._ready()
	set_slot(0, false, 0, Color(1, 1, 1, 1), true, 0, Color(1, 1, 1, 1))


func _on_name_attribute_text_changed(new_text: String) -> void:
	xml_attributes["name"] = new_text
	xml_refresh_requested.emit(self)
