extends "res://nodes/base_node.gd"

func _ready():
	super._ready()
	set_slot(0, true, 0, Color(1, 1, 1, 1), false, 0, Color(1, 1, 1, 1))
	xml_attributes["text"] = $TextAttribute.text
	xml_attributes["filter"] = $HBoxContainer/FilterAttribute.get_item_text(0).to_lower()


func _on_text_attribute_text_changed() -> void:
	xml_attributes["text"] = $TextAttribute.text
	xml_refresh_requested.emit(self)


func _on_filter_attribute_item_selected(index: int) -> void:
	xml_attributes["filter"] = $HBoxContainer/FilterAttribute.get_item_text(index).to_lower()
	xml_refresh_requested.emit(self)