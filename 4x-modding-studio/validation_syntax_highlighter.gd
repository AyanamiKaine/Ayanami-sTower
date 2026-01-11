class_name ValidationSyntaxHighlighter
extends SyntaxHighlighter

# Color constants
const COL_ERROR := Color.RED # Red for ERROR
const COL_LINE_COL := Color("ffcc66") # Orange for Line and Column
const COL_SUCCESS := Color.GREEN # Green for successful
const COL_BASE := Color("e0e0e0") # Default Text
const COL_NUMBER := Color("66bb6a") # Green for numbers

func _get_line_syntax_highlighting(line_number: int) -> Dictionary:
	var text := get_text_edit().get_line(line_number)
	var result := {}
	
	# Create an array to track color for each character position
	var colors: Array[Color] = []
	colors.resize(text.length() + 1)
	for i in range(colors.size()):
		colors[i] = COL_BASE
	
	# Highlight ERROR in red
	var error_pos := text.find("ERROR")
	if error_pos != -1:
		for i in range(error_pos, error_pos + 5):
			if i < colors.size():
				colors[i] = COL_ERROR
	
	# Highlight "Line" keyword and following number
	var line_pos := text.find("Line")
	if line_pos != -1:
		for i in range(line_pos, mini(line_pos + 4, text.length())):
			colors[i] = COL_LINE_COL
		
		# Highlight the line number after "Line "
		var num_start := line_pos + 5
		while num_start < text.length() and text[num_start] == ' ':
			num_start += 1
		
		var num_end := num_start
		while num_end < text.length() and text[num_end].is_valid_int():
			colors[num_end] = COL_NUMBER
			num_end += 1
	
	# Highlight "Column" keyword and following number
	var col_pos := text.find("Column")
	if col_pos != -1:
		for i in range(col_pos, mini(col_pos + 6, text.length())):
			colors[i] = COL_LINE_COL
		
		# Highlight the column number after "Column "
		var num_start := col_pos + 7
		while num_start < text.length() and text[num_start] == ' ':
			num_start += 1
		
		var num_end := num_start
		while num_end < text.length() and text[num_end].is_valid_int():
			colors[num_end] = COL_NUMBER
			num_end += 1
	
	# Highlight "successful" in green
	var success_pos := text.find("successful")
	if success_pos != -1:
		for i in range(success_pos, success_pos + 10):
			if i < colors.size():
				colors[i] = COL_SUCCESS
	
	# Convert colors array to result dictionary format
	for i in range(colors.size()):
		result[i] = {"color": colors[i]}
	
	return result
