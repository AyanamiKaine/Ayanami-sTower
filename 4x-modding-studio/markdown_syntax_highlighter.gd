@tool
extends SyntaxHighlighter
class_name MarkdownSyntaxHighlighter

# --- COLORS ---
@export var header1_color: Color = Color(0.4, 0.8, 1.0) # Light blue for # headers
@export var header2_color: Color = Color(0.5, 0.85, 0.95) # Slightly lighter
@export var header3_color: Color = Color(0.6, 0.9, 0.9) # Even lighter
@export var header4_color: Color = Color(0.65, 0.85, 0.85) # Subtle
@export var bold_color: Color = Color(1.0, 0.9, 0.5) # Yellow/gold for **bold**
@export var italic_color: Color = Color(0.8, 0.7, 1.0) # Purple for *italic*
@export var code_inline_color: Color = Color(0.5, 1.0, 0.5) # Green for `code`
@export var code_block_color: Color = Color(0.4, 0.9, 0.4) # Darker green for ```blocks```
@export var link_color: Color = Color(0.4, 0.6, 1.0) # Blue for [links](url)
@export var url_color: Color = Color(0.6, 0.6, 0.8) # Muted blue for URLs
@export var list_marker_color: Color = Color(1.0, 0.6, 0.3) # Orange for - * + markers
@export var blockquote_color: Color = Color(0.7, 0.7, 0.7) # Gray for > quotes
@export var horizontal_rule_color: Color = Color(0.5, 0.5, 0.5) # Gray for ---
@export var strikethrough_color: Color = Color(0.6, 0.6, 0.6) # Gray for ~~strike~~
@export var image_color: Color = Color(0.9, 0.5, 0.9) # Pink for ![images]

var in_code_block: bool = false

func _get_name() -> String:
	return "Markdown"

func _get_line_syntax_highlighting(line: int) -> Dictionary:
	var color_map: Dictionary = {}
	var text_edit = get_text_edit()
	if not text_edit:
		return color_map
	
	var text: String = text_edit.get_line(line)
	if text.is_empty():
		return color_map
	
	# Check for code block start/end
	if text.strip_edges().begins_with("```"):
		color_map[0] = {"color": code_block_color}
		in_code_block = not in_code_block
		return color_map
	
	# Inside code block - highlight entire line
	if in_code_block:
		color_map[0] = {"color": code_block_color}
		return color_map
	
	# Headers: # ## ### #### ##### ######
	var header_match = _match_header(text)
	if header_match >= 0:
		var header_colors = [header1_color, header2_color, header3_color, header4_color, header4_color, header4_color]
		color_map[0] = {"color": header_colors[mini(header_match, 5)]}
		return color_map
	
	# Horizontal rule: --- or *** or ___
	if _is_horizontal_rule(text):
		color_map[0] = {"color": horizontal_rule_color}
		return color_map
	
	# Blockquote: > text
	if text.strip_edges().begins_with(">"):
		color_map[0] = {"color": blockquote_color}
		return color_map
	
	# List markers: - * + or numbered 1. 2. etc
	var list_end = _match_list_marker(text)
	if list_end > 0:
		color_map[0] = {"color": list_marker_color}
		# Rest of line is normal, apply inline formatting after marker
		_apply_inline_formatting(text, color_map, list_end)
		return color_map
	
	# Apply inline formatting to the whole line
	_apply_inline_formatting(text, color_map, 0)
	
	return color_map

func _apply_inline_formatting(text: String, color_map: Dictionary, start_from: int) -> void:
	var i: int = start_from
	
	while i < text.length():
		# Image: ![alt](url) - check before link
		if i < text.length() - 1 and text[i] == "!" and text[i + 1] == "[":
			var end = _find_link_end(text, i + 1)
			if end > i:
				for j in range(i, end + 1):
					color_map[j] = {"color": image_color}
				i = end + 1
				continue
		
		# Link: [text](url)
		if text[i] == "[":
			var end = _find_link_end(text, i)
			if end > i:
				# Color the [text] part
				var bracket_end = text.find("]", i)
				for j in range(i, bracket_end + 1):
					color_map[j] = {"color": link_color}
				# Color the (url) part
				for j in range(bracket_end + 1, end + 1):
					color_map[j] = {"color": url_color}
				i = end + 1
				continue
		
		# Inline code: `code`
		if text[i] == "`" and not _is_triple_backtick(text, i):
			var end = text.find("`", i + 1)
			if end > i:
				for j in range(i, end + 1):
					color_map[j] = {"color": code_inline_color}
				i = end + 1
				continue
		
		# Strikethrough: ~~text~~
		if i < text.length() - 1 and text[i] == "~" and text[i + 1] == "~":
			var end = text.find("~~", i + 2)
			if end > i:
				for j in range(i, end + 2):
					color_map[j] = {"color": strikethrough_color}
				i = end + 2
				continue
		
		# Bold: **text** or __text__
		if i < text.length() - 1:
			if (text[i] == "*" and text[i + 1] == "*") or (text[i] == "_" and text[i + 1] == "_"):
				var marker = text.substr(i, 2)
				var end = text.find(marker, i + 2)
				if end > i:
					for j in range(i, end + 2):
						color_map[j] = {"color": bold_color}
					i = end + 2
					continue
		
		# Italic: *text* or _text_ (single marker, not at word boundary for _)
		if text[i] == "*" or text[i] == "_":
			var marker = text[i]
			# Make sure it's not a double marker (bold)
			if i + 1 < text.length() and text[i + 1] != marker:
				var end = text.find(marker, i + 1)
				if end > i:
					for j in range(i, end + 1):
						color_map[j] = {"color": italic_color}
					i = end + 1
					continue
		
		i += 1

func _match_header(text: String) -> int:
	var stripped = text.strip_edges(true, false) # Only strip leading
	if not stripped.begins_with("#"):
		return -1
	
	var level = 0
	for c in stripped:
		if c == "#":
			level += 1
		elif c == " ":
			break
		else:
			return -1
	
	if level > 0 and level <= 6:
		return level - 1
	return -1

func _is_horizontal_rule(text: String) -> bool:
	var stripped = text.strip_edges()
	if stripped.length() < 3:
		return false
	
	# Must be only -, *, or _ (at least 3) with optional spaces
	var char_count = 0
	var rule_char = ""
	
	for c in stripped:
		if c == " ":
			continue
		if rule_char.is_empty():
			if c in ["-", "*", "_"]:
				rule_char = c
				char_count += 1
			else:
				return false
		elif c == rule_char:
			char_count += 1
		else:
			return false
	
	return char_count >= 3

func _match_list_marker(text: String) -> int:
	var stripped = text.strip_edges(true, false)
	var indent = text.length() - stripped.length()
	
	# Unordered: - * +
	if stripped.length() >= 2:
		if stripped[0] in ["-", "*", "+"] and stripped[1] == " ":
			return indent + 2
	
	# Ordered: 1. 2. etc
	var i = 0
	while i < stripped.length() and stripped[i].is_valid_int():
		i += 1
	
	if i > 0 and i < stripped.length() - 1:
		if stripped[i] == "." and stripped[i + 1] == " ":
			return indent + i + 2
	
	return 0

func _find_link_end(text: String, start: int) -> int:
	var bracket_end = text.find("]", start)
	if bracket_end < 0:
		return -1
	
	if bracket_end + 1 < text.length() and text[bracket_end + 1] == "(":
		var paren_end = text.find(")", bracket_end + 2)
		if paren_end > bracket_end:
			return paren_end
	
	return -1

func _is_triple_backtick(text: String, pos: int) -> bool:
	if pos + 2 < text.length():
		return text[pos] == "`" and text[pos + 1] == "`" and text[pos + 2] == "`"
	return false
