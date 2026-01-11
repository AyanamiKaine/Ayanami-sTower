extends Window
const GraphXmlUtils = preload("res://GraphXmlUtils.cs")

@export var output_text_edit: TextEdit
@export var xml_editor: CodeEdit


func _on_xml_code_edit_text_changed() -> void:
	#var validation_result = GraphXmlUtils.ValidateXmlAgainstSchema(xml_editor.text)
	#output_text_edit.text = validation_result
    pass