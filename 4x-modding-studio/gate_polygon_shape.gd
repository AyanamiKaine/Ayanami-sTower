@tool
extends Polygon2D

# --- CIRCLE SETTINGS ---
@export_group("Circle Settings")
@export var radius: float = 100.0:
	set(value):
		radius = value
		update_circle()

# Higher number = smoother circle. 32 is standard, 64 is high def.
@export_range(3, 360) var segments: int = 64:
	set(value):
		segments = value
		update_circle()

# --- OUTLINE SETTINGS ---
@export_group("Outline Settings")
@export var outline_width: float = 0.0:
	set(value):
		outline_width = value
		update_circle()

@export var outline_color: Color = Color.WHITE:
	set(value):
		outline_color = value
		update_circle()

func _ready():
	update_circle()

func update_circle():
	var points = PackedVector2Array()
	var uv_points = PackedVector2Array()
	
	# 1. Calculate Vertices
	# We iterate 'segments' times to create the circle vertices
	for i in range(segments):
		# TAU is 2*PI (approx 6.28), representing a full 360 rotation in radians
		var angle = (TAU / segments) * i
		
		# Polar coordinates
		var x = radius * cos(angle)
		var y = radius * sin(angle)
		
		points.append(Vector2(x, y))
		
		# Calculate UVs (Map local coordinates to 0-1 range for textures)
		# (x / width) shifts range to -0.5 to 0.5, then we add 0.5 to center it
		var u = (x / (radius * 2)) + 0.5
		var v = (y / (radius * 2)) + 0.5
		uv_points.append(Vector2(u, v))
	
	# 2. Assign Data
	self.polygon = points
	self.uv = uv_points
	
	# 3. Handle Outline
	update_outline(points)

func update_outline(poly_points: PackedVector2Array):
	var line_node = get_node_or_null("CircleOutline")
	
	# If width is 0, we don't need the outline node
	if outline_width <= 0:
		if line_node: line_node.queue_free()
		return

	# Create node if missing
	if not line_node:
		line_node = Line2D.new()
		line_node.name = "CircleOutline"
		add_child(line_node)
	
	# Close the loop
	var line_points = poly_points.duplicate()
	if not line_points.is_empty():
		line_points.append(line_points[0])
		
	line_node.points = line_points
	line_node.width = outline_width
	line_node.default_color = outline_color
	# "Round" joints look smoother for high-segment circles
	line_node.joint_mode = Line2D.LINE_JOINT_ROUND