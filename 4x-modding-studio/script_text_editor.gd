extends Control

@export var editor: CodeEdit
@export var current_file_name: String = "":
	set(value):
		current_file_name = value
		_update_tab_name()

@export_enum("None", "Markdown", "XML") var syntax_mode: int = 1:
	set(value):
		syntax_mode = value
		_apply_syntax_mode()

@export var xml_highlighter: SyntaxHighlighter = null

var markdown_highlighter: SyntaxHighlighter = null
var current_file_path: String = ""
var is_modified: bool = false:
	set(value):
		is_modified = value
		_update_tab_name()

signal file_saved(path: String)
signal file_modified(path: String)
signal close_requested()

func _ready():
	# Create markdown highlighter instance
	markdown_highlighter = preload("res://markdown_syntax_highlighter.gd").new()
	_apply_syntax_mode()
	
	# Connect editor text changed signal
	if editor:
		editor.text_changed.connect(_on_editor_text_changed)

func _input(event: InputEvent) -> void:
	if not is_visible_in_tree():
		return
	
	# Handle Ctrl+S to save
	if event.is_action_pressed("ui_text_submit") or (event is InputEventKey and event.pressed and event.keycode == KEY_S and event.ctrl_pressed):
		if event is InputEventKey and event.keycode == KEY_S and event.ctrl_pressed:
			save_file()
			get_viewport().set_input_as_handled()

func _on_editor_text_changed() -> void:
	if not is_modified:
		is_modified = true
		file_modified.emit(current_file_path)

## Open a file and apply appropriate highlighting
func open_file(path: String) -> void:
	current_file_path = path
	current_file_name = path.get_file()
	
	var file = FileAccess.open(path, FileAccess.READ)
	if file:
		editor.text = file.get_as_text()
		file.close()
		is_modified = false
		
		# Auto-detect and apply syntax highlighting based on file extension
		_auto_detect_mode(path)

## Save the current file
func save_file() -> void:
	if current_file_path.is_empty():
		return
	
	var file = FileAccess.open(current_file_path, FileAccess.WRITE)
	if file:
		file.store_string(editor.text)
		file.close()
		is_modified = false
		file_saved.emit(current_file_path)
		print("Saved: ", current_file_path)

## Update the tab name to show modified state
func _update_tab_name() -> void:
	var tab_container = get_parent()
	if tab_container is TabContainer:
		var tab_idx = get_index()
		var display_name = current_file_name if current_file_name else "Untitled"
		if is_modified:
			display_name = "● " + display_name
		tab_container.set_tab_title(tab_idx, display_name)
	
	# Also update the node name for identification
	name = current_file_name if current_file_name else "Untitled"

## Auto-detect syntax mode based on file extension
func _auto_detect_mode(path: String) -> void:
	var extension = path.get_extension().to_lower()
	
	match extension:
		"md", "markdown":
			syntax_mode = 1 # Markdown
		"xml", "html", "htm":
			syntax_mode = 2 # XML
		_:
			syntax_mode = 0 # None

## Apply the current syntax mode to the editor
func _apply_syntax_mode() -> void:
	if not editor:
		return
	
	match syntax_mode:
		0: # None
			editor.syntax_highlighter = null
		1: # Markdown
			editor.syntax_highlighter = markdown_highlighter
		2: # XML
			editor.syntax_highlighter = xml_highlighter

## Set syntax mode by name
func set_mode(mode_name: String) -> void:
	match mode_name.to_lower():
		"none", "plain", "text":
			syntax_mode = 0
		"md", "markdown":
			syntax_mode = 1
		"xml", "html":
			syntax_mode = 2

## Get current mode name
func get_mode_name() -> String:
	match syntax_mode:
		0: return "None"
		1: return "Markdown"
		2: return "XML"
	return "Unknown"

## Check if current mode is markdown
func is_markdown_mode() -> bool:
	return syntax_mode == 1
