extends "res://nodes/base_node.gd"

func _ready():
	super._ready()
	set_slot(0, true, 0, Color(1,1,1,1), false, 0, Color(1,1,1,1))


func _on_custom_attribute_text_changed() -> void:
	xml_attributes["custom"] = $CustomAttribute.text
	xml_refresh_requested.emit(self)


func _on_duration_attribute_value_changed(value: float) -> void:
	xml_attributes["duration"] = value
	xml_refresh_requested.emit(self)


func _on_position_attribute_value_changed(value: float) -> void:
	xml_attributes["position"] = value
	xml_refresh_requested.emit(self)
