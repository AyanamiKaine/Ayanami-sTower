extends "res://nodes/base_node.gd"

func _ready():
	super._ready()
	set_slot(0, true, 0, Color(1,1,1,1), false, 0, Color(1,1,1,1))

func _on_exact_attribute_value_changed(value: float) -> void:
	xml_attributes["exact"] = value
	xml_refresh_requested.emit(self)


func _on_min_attribute_value_changed(value: float) -> void:
	xml_attributes["min"] = value
	xml_refresh_requested.emit(self)



func _on_max_attribute_value_changed(value: float) -> void:
	xml_attributes["max"] = value
	xml_refresh_requested.emit(self)
