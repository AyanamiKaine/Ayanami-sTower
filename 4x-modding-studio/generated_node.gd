extends "res://nodes/base_node.gd"
## A dynamically generated node that gets its attributes from XSD element definitions.
## This script is used as the base for all auto-generated nodes.

## The XSD element info this node was generated from (set by NodeGenerator)
var element_info: Dictionary = {}

## Setup the node with element data (called by NodeGenerator after instantiation)
func setup_from_element(elem_name: String, elem_data: Dictionary) -> void:
	element_info = elem_data
	xml_element_name = elem_name
	title = _format_title(elem_name)
	
	# Set tooltip from documentation
	if elem_data.has("documentation") and not elem_data["documentation"].is_empty():
		tooltip_text = elem_data["documentation"]

## Format element name for display (e.g., "set_value" -> "Set Value")
func _format_title(elem_name: String) -> String:
	var words = elem_name.split("_")
	var formatted_words: Array[String] = []
	for word in words:
		if word.length() > 0:
			formatted_words.append(word[0].to_upper() + word.substr(1).to_lower())
	return " ".join(formatted_words)

## Called when an attribute value changes - updates xml_attributes dictionary
func _on_attribute_changed(attr_name: String, new_value: Variant) -> void:
	if new_value == null or (new_value is String and new_value.is_empty()):
		xml_attributes.erase(attr_name)
	else:
		xml_attributes[attr_name] = str(new_value)
	xml_refresh_requested.emit(self)

## Get the value of an attribute control
func get_attribute_value(attr_name: String) -> Variant:
	var control = find_child("Input_" + attr_name, true, false)
	if control == null:
		return null
	
	if control is LineEdit:
		return control.text
	elif control is SpinBox:
		return control.value
	elif control is CheckBox:
		return control.button_pressed
	elif control is OptionButton:
		return control.get_item_text(control.selected)
	
	return null

## Set the value of an attribute control
func set_attribute_value(attr_name: String, value: Variant) -> void:
	var control = find_child("Input_" + attr_name, true, false)
	if control == null:
		return
	
	if control is LineEdit:
		control.text = str(value) if value != null else ""
	elif control is SpinBox and value != null:
		control.value = float(value)
	elif control is CheckBox:
		control.button_pressed = bool(value) if value != null else false
	elif control is OptionButton:
		for i in range(control.item_count):
			if control.get_item_text(i) == str(value):
				control.selected = i
				break
	
	# Update xml_attributes
	_on_attribute_changed(attr_name, value)

## Batch set multiple attributes (useful when loading from XML)
func set_attributes(attributes: Dictionary) -> void:
	for attr_name in attributes:
		set_attribute_value(attr_name, attributes[attr_name])
