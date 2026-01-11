extends "res://nodes/base_node.gd"

func _ready():
	super._ready()
	set_slot(0, true, 0, Color(1, 1, 1, 1), false, 0, Color(1, 1, 1, 1))


func _on_title_attribute_text_changed(new_text: String) -> void:
	xml_attributes["title"] = new_text
	xml_refresh_requested.emit(self)


func _on_text_attribute_text_changed() -> void:
	xml_attributes["text"] = $TextAttribute.text
	xml_refresh_requested.emit(self)


func _on_category_attribute_item_selected(index: int) -> void:
	xml_attributes["category"] = $HBoxContainer/CategoryAttribute.get_item_text(index)
	xml_refresh_requested.emit(self)
