extends TextEdit
const ValidationSyntaxHighlighter = preload("res://validation_syntax_highlighter.gd")

func _ready() -> void:
	# Apply validation syntax highlighter
	syntax_highlighter = ValidationSyntaxHighlighter.new()
