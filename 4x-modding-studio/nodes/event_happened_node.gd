extends "res://nodes/base_node.gd"

# We need add some fields the user can define for some events. 
# For example the event_attack_stopped has the two fields target and attacker.
# So based on the event we need to add dynamically fields.

func _ready():
	super._ready()
	set_slot(0, true, 0, Color(1, 1, 1, 1), false, 0, Color(1, 1, 1, 1))
	xml_element_name = "event_game_started"
	xml_refresh_requested.emit(self)


func _on_option_button_item_selected(index: int) -> void:
	var optionsText: String = $OptionButton.get_item_text(index)
	optionsText = optionsText.to_lower()
	xml_element_name = "event_" + optionsText.replace_char(" ".unicode_at(0), "_".unicode_at(0))

	if (xml_element_name == "event_cue_signalled"):
		$CueAttribute.show()
		xml_attributes["cue"] = $CueAttribute.text
	else:
		$CueAttribute.hide()
		xml_attributes.erase("cue")
		
	xml_refresh_requested.emit(self)


func _on_cue_attribute_text_changed(new_text: String) -> void:
	if ($CueAttribute.visible):
		xml_attributes["cue"] = new_text
	else:
		xml_attributes.erase("cue")
	xml_refresh_requested.emit(self)
