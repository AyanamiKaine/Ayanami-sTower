extends "res://nodes/base_node.gd"


func _ready():
	super._ready()
	set_slot(0, true, 0, Color(1, 1, 1, 1), true, 0, Color(1, 1, 1, 1))


func _on_instantiate_attribute_toggled(toggled_on: bool) -> void:
	xml_attributes["instantiate"] = toggled_on
	xml_refresh_requested.emit(self)


func _on_name_attribute_text_changed(new_text: String) -> void:
	xml_attributes["name"] = new_text
	xml_refresh_requested.emit(self)


func _on_name_space_attribute_text_changed(new_text: String) -> void:
	if new_text != "":
		xml_attributes["namespace"] = new_text
	else:
		xml_attributes.erase("namespace")
	xml_refresh_requested.emit(self)