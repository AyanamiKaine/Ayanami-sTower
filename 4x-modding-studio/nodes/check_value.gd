extends "res://nodes/base_node.gd"

var expression_left_side: String = ""
var expression_right_side: String = ""
var operator: String = ""


func _ready() -> void:
	super._ready()
	set_slot(0, true, 0, Color(1, 1, 1, 1), false, 0, Color(1, 1, 1, 1))
	operator = $HBoxContainer/Operator.get_item_text(0)


func _on_left_side_expression_text_changed(new_text: String) -> void:
	expression_left_side = new_text
	xml_attributes["value"] = create_expression()
	xml_refresh_requested.emit(self)


func _on_right_side_expression_text_changed(new_text: String) -> void:
	expression_right_side = new_text
	xml_attributes["value"] = create_expression()
	xml_refresh_requested.emit(self)


func _on_operator_item_selected(index: int) -> void:
	operator = $HBoxContainer/Operator.get_item_text(index)
	xml_attributes["value"] = create_expression()
	xml_refresh_requested.emit(self)

func create_expression() -> String:
	return expression_left_side + " " + operator + " " + expression_right_side
