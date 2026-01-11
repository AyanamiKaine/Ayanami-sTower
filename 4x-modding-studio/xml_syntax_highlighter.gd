class_name XmlSyntaxHighlighter
extends SyntaxHighlighter

const COL_TAG := Color("ff7b7b") # Red
const COL_ATTR := Color("ffcc66") # Orange
const COL_STRING := Color("a5c261") # Green
const COL_COMMENT := Color("6c757d") # Grey
const COL_SYMBOL := Color("b0bec5") # Light Grey
const COL_BASE := Color("e0e0e0") # Default Text

func _get_line_syntax_highlighting(line_number: int) -> Dictionary:
	var text := get_text_edit().get_line(line_number)
	var result := {}
	
	# Create an array to track color for each character position
	var colors: Array[Color] = []
	colors.resize(text.length() + 1)
	for i in range(colors.size()):
		colors[i] = COL_BASE
	
	var i := 0
	while i < text.length():
		# Check for comment start <!--
		if text.substr(i, 4) == "<!--":
			var end_pos := text.find("-->", i + 4)
			if end_pos == -1:
				end_pos = text.length() - 3 # Comment continues to end of line
			end_pos += 3 # Include -->
			for j in range(i, mini(end_pos, text.length())):
				colors[j] = COL_COMMENT
			i = end_pos
			continue
		
		# Check for tag start <
		if text[i] == "<":
			colors[i] = COL_SYMBOL # Color the <
			i += 1
			
			# Check for / or ? after <
			if i < text.length() and (text[i] == "/" or text[i] == "?"):
				colors[i] = COL_SYMBOL
				i += 1
			
			# Read tag name
			while i < text.length() and _is_tag_char(text[i]):
				colors[i] = COL_TAG
				i += 1
			
			# Inside tag - look for attributes and closing
			while i < text.length() and text[i] != ">":
				# Skip whitespace
				if text[i] == " " or text[i] == "\t":
					i += 1
					continue
				
				# Check for string (attribute value)
				if text[i] == '"':
					colors[i] = COL_STRING
					i += 1
					while i < text.length() and text[i] != '"':
						colors[i] = COL_STRING
						i += 1
					if i < text.length():
						colors[i] = COL_STRING # Closing quote
						i += 1
					continue
				
				if text[i] == "'":
					colors[i] = COL_STRING
					i += 1
					while i < text.length() and text[i] != "'":
						colors[i] = COL_STRING
						i += 1
					if i < text.length():
						colors[i] = COL_STRING # Closing quote
						i += 1
					continue
				
				# Check for symbols
				if text[i] == "=" or text[i] == "/" or text[i] == "?":
					colors[i] = COL_SYMBOL
					i += 1
					continue
				
				# Must be an attribute name
				if _is_tag_char(text[i]):
					while i < text.length() and _is_tag_char(text[i]):
						colors[i] = COL_ATTR
						i += 1
					continue
				
				i += 1
			
			# Color the closing >
			if i < text.length() and text[i] == ">":
				colors[i] = COL_SYMBOL
				i += 1
			continue
		
		i += 1
	
	# Convert colors array to the dictionary format Godot expects
	var current_color := COL_BASE
	for idx in range(text.length()):
		if colors[idx] != current_color:
			result[idx] = {"color": colors[idx]}
			current_color = colors[idx]
	
	return result

func _is_tag_char(c: String) -> bool:
	return c.is_valid_identifier() or c == "-" or c == ":" or c == "."
