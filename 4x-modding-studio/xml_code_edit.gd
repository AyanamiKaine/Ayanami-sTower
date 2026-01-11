extends CodeEdit

# Configuration for colors
const COLOR_TAG_OPEN := Color("ff7b7b") # Reddish for tags <...>
const COLOR_ATTRIBUTE := Color("ffcc66") # Orange/Yellow for attributes
const COLOR_STRING := Color("a5c261") # Green for strings "..."
const COLOR_COMMENT := Color("6c757d") # Grey for comments
const COLOR_BASE_TEXT := Color("e0e0e0") # Default text color

# XML Schema Definition - defines valid tags, their attributes, and child elements
const XML_SCHEMA := {
	"mdscript": {
		"attributes": ["name"],
		"children": ["cue"]
	},
	"cue": {
		"attributes": ["name", "instantiate"],
		"children": ["conditions", "actions", "cue"]
	},
	"conditions": {
		"attributes": [],
		"children": ["event_game_started", "check_value", "event_cue_signalled"]
	},
	"actions": {
		"attributes": [],
		"children": ["show_help", "set_value", "debug_text", "signal_cue", "write_to_logbook"]
	},
	"event_game_started": {
		"attributes": [],
		"children": []
	},
	"event_cue_signalled": {
		"attributes": ["cue"],
		"children": []
	},
	"check_value": {
		"attributes": ["name", "exact", "min", "max"],
		"children": []
	},
	"show_help": {
		"attributes": ["position", "duration", "text"],
		"children": []
	},
	"set_value": {
		"attributes": ["name", "exact", "operation"],
		"children": []
	},
	"debug_text": {
		"attributes": ["text", "filter"],
		"children": []
	},
	"signal_cue": {
		"attributes": ["cue"],
		"children": []
	},
	"write_to_logbook": {
		"attributes": ["title", "text", "category"],
		"children": []
	}
}

# Root level tags (can appear at document root)
const ROOT_TAGS := ["mdscript"]

func _ready() -> void:
	_setup_editor_settings()
	_setup_highlighter()

# ------------------------------------------------------------------------------
# 1. SETUP & VISUALS
# ------------------------------------------------------------------------------
func _setup_editor_settings() -> void:
	# Enable code completion features
	code_completion_enabled = true
	
	# Optional: Visual settings to make it look like a real editor
	gutters_draw_line_numbers = true
	gutters_draw_fold_gutter = true
	auto_brace_completion_enabled = true
	
	# Add brace pairs for auto-closing
	add_auto_brace_completion_pair("<", ">")

func _setup_highlighter() -> void:
	var highlighter := XmlSyntaxHighlighter.new()
	self.syntax_highlighter = highlighter

# ------------------------------------------------------------------------------
# 2. CODE COMPLETION LOGIC
# ------------------------------------------------------------------------------
func _on_code_completion_requested() -> void:
	var line_text := get_line(get_caret_line())
	var col := get_caret_column()
	var prefix := line_text.substr(0, col)
	
	var last_open := prefix.rfind("<")
	var last_close := prefix.rfind(">")
	
	# If we are inside a tag (after < but before >)
	if last_open > last_close:
		var tag_content := prefix.substr(last_open + 1)
		
		# Check if it's a closing tag </
		if tag_content.begins_with("/"):
			_suggest_closing_tags(tag_content.substr(1))
			return
		
		# If there is a space, we are typing attributes
		if " " in tag_content:
			var tag_name := tag_content.split(" ")[0]
			_suggest_attributes_for_tag(tag_name)
		else:
			# We are typing the tag name - find parent context
			var parent_tag := _find_parent_tag()
			_suggest_tags(tag_content, parent_tag)
	else:
		# We're outside a tag, check if user wants to start a new tag
		# Look for context to suggest appropriate child tags
		var parent_tag := _find_parent_tag()
		if prefix.ends_with("<"):
			_suggest_tags("", parent_tag)

func _find_parent_tag() -> String:
	# Search backwards through the document to find the current parent tag
	var caret_line := get_caret_line()
	var tag_stack: Array[String] = []
	
	for line_idx in range(caret_line + 1):
		var line := get_line(line_idx)
		var col_limit := line.length()
		if line_idx == caret_line:
			col_limit = get_caret_column()
		
		var i := 0
		while i < col_limit:
			# Find opening tags
			if line[i] == "<":
				if i + 1 < line.length() and line[i + 1] == "/":
					# Closing tag - find the tag name
					var end := line.find(">", i)
					if end != -1 and end <= col_limit:
						var tag_name := line.substr(i + 2, end - i - 2).strip_edges()
						if tag_stack.size() > 0 and tag_stack.back() == tag_name:
							tag_stack.pop_back()
					i = end if end != -1 else i + 1
				elif i + 1 < line.length() and line[i + 1] != "!" and line[i + 1] != "?":
					# Opening tag
					var end := line.find(">", i)
					if end != -1 and end <= col_limit:
						var tag_part := line.substr(i + 1, end - i - 1)
						# Check for self-closing
						var is_self_closing := tag_part.ends_with("/")
						if is_self_closing:
							tag_part = tag_part.substr(0, tag_part.length() - 1)
						var tag_name := tag_part.split(" ")[0].strip_edges()
						if not tag_name.is_empty() and not is_self_closing:
							tag_stack.push_back(tag_name)
					i = end if end != -1 else i + 1
				else:
					i += 1
			else:
				i += 1
	
	return tag_stack.back() if tag_stack.size() > 0 else ""

func _suggest_tags(filter_text: String, parent_tag: String) -> void:
	var available_tags: Array[String] = []
	
	if parent_tag.is_empty():
		# At root level
		available_tags.assign(ROOT_TAGS)
	elif XML_SCHEMA.has(parent_tag):
		# Get valid children for this parent
		for child in XML_SCHEMA[parent_tag]["children"]:
			available_tags.append(child)
	else:
		# Unknown parent, suggest all tags
		for tag in XML_SCHEMA.keys():
			available_tags.append(tag)
	
	for tag in available_tags:
		if filter_text.is_empty() or tag.begins_with(filter_text) or filter_text in tag:
			var completion_text := tag
			# Add required attributes template
			if XML_SCHEMA.has(tag) and XML_SCHEMA[tag]["attributes"].size() > 0:
				completion_text = tag
				for attr in XML_SCHEMA[tag]["attributes"]:
					completion_text += ' %s=""' % attr
			
			# Check if tag has children or is self-closing
			if XML_SCHEMA.has(tag) and XML_SCHEMA[tag]["children"].size() == 0:
				completion_text += " />"
			else:
				completion_text += ">"
			
			add_code_completion_option(
				CodeEdit.KIND_CLASS,
				tag,
				completion_text,
				COLOR_TAG_OPEN
			)
	
	update_code_completion_options(true)

func _suggest_closing_tags(filter_text: String) -> void:
	# Find tags that need to be closed
	var parent_tag := _find_parent_tag()
	if not parent_tag.is_empty():
		if filter_text.is_empty() or parent_tag.begins_with(filter_text):
			add_code_completion_option(
				CodeEdit.KIND_CLASS,
				"/" + parent_tag,
				parent_tag + ">",
				COLOR_TAG_OPEN
			)
	update_code_completion_options(true)

func _suggest_attributes_for_tag(tag_name: String) -> void:
	if not XML_SCHEMA.has(tag_name):
		return
	
	var attrs: Array = XML_SCHEMA[tag_name]["attributes"]
	for attr in attrs:
		add_code_completion_option(
			CodeEdit.KIND_MEMBER,
			attr,
			attr + '=""',
			COLOR_ATTRIBUTE
		)
	update_code_completion_options(true)

# Legacy function kept for compatibility
func _suggest_attributes():
	var attrs = ["name", "instantiate", "text", "position", "duration"]
	for a in attrs:
		add_code_completion_option(
			CodeEdit.KIND_MEMBER,
			a,
			a + '=""',
			Color("ffcc66")
		)
	update_code_completion_options(true)
