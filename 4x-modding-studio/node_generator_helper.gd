extends RefCounted
class_name NodeGeneratorHelper

## Helper class to interface between GDScript and the C# NodeGenerator.
## Provides convenient methods for spawning dynamic nodes.

## Reference to the C# NodeGenerator
var _generator: Node = null

## Reference to the DatabaseManager
var _database_manager: Node = null

## Initialize with references to the generator and database manager
func initialize(generator: Node, database_manager: Node) -> void:
	_generator = generator
	_database_manager = database_manager

## Check if the generator is ready (database loaded)
func is_ready() -> bool:
	if _database_manager == null:
		return false
	# Check if elements are loaded
	var all_elements = _database_manager.get("AllElements")
	return all_elements != null and all_elements.size() > 0

## Get all available element names
func get_all_element_names() -> Array[String]:
	if _generator == null:
		return []
	var elements = _generator.GetAvailableElements()
	var result: Array[String] = []
	for elem in elements:
		result.append(elem)
	return result

## Get elements organized by category for the node searcher
## Returns: Array of { name, display_name, documentation, category }
func get_elements_by_category() -> Dictionary:
	if _generator == null:
		return {}
	
	var elements_data = _generator.GetElementsForSearcher()
	var categories: Dictionary = {}
	
	for elem_tuple in elements_data:
		var elem_name = elem_tuple[0]
		var display_name = elem_tuple[1]
		var documentation = elem_tuple[2]
		var category = elem_tuple[3]
		
		if not categories.has(category):
			categories[category] = []
		
		categories[category].append({
			"name": elem_name,
			"display_name": display_name,
			"documentation": documentation
		})
	
	return categories

## Get a flat list of elements matching a search query
func search_elements(query: String) -> Array[Dictionary]:
	if _generator == null:
		return []
	
	var results: Array[Dictionary] = []
	var query_lower = query.to_lower()
	var elements_data = _generator.GetElementsForSearcher()
	
	for elem_tuple in elements_data:
		var elem_name: String = elem_tuple[0]
		var display_name: String = elem_tuple[1]
		var documentation: String = elem_tuple[2]
		var category: String = elem_tuple[3]
		
		# Match against name, display name, or documentation
		if elem_name.to_lower().contains(query_lower) or \
		   display_name.to_lower().contains(query_lower) or \
		   documentation.to_lower().contains(query_lower):
			results.append({
				"name": elem_name,
				"display_name": display_name,
				"documentation": documentation,
				"category": category
			})
	
	return results

## Create a node for the given element name
func create_node(element_name: String) -> GraphNode:
	if _generator == null:
		push_error("NodeGeneratorHelper: Generator not initialized")
		return null
	
	return _generator.CreateNodeForElement(element_name)

## Get allowed children for an element
func get_allowed_children(element_name: String) -> Array[String]:
	if _generator == null:
		return []
	var children = _generator.GetAllowedChildren(element_name)
	var result: Array[String] = []
	for child in children:
		result.append(child)
	return result

## Get allowed parents for an element
func get_allowed_parents(element_name: String) -> Array[String]:
	if _generator == null:
		return []
	var parents = _generator.GetAllowedParents(element_name)
	var result: Array[String] = []
	for parent in parents:
		result.append(parent)
	return result

## Check if an element can be a child of another
func can_be_child_of(child_element: String, parent_element: String) -> bool:
	if _generator == null:
		return false
	return _generator.CanBeChildOf(child_element, parent_element)

## Get element info dictionary
func get_element_info(element_name: String) -> Dictionary:
	if _database_manager == null:
		return {}
	
	var all_elements = _database_manager.get("AllElements")
	if all_elements == null or not all_elements.has(element_name):
		return {}
	
	var elem_info = all_elements[element_name]
	return {
		"name": elem_info.Name,
		"type": elem_info.Type,
		"documentation": elem_info.Documentation,
		"attributes": _convert_attributes(elem_info.Attributes),
		"attribute_groups": _convert_to_array(elem_info.AttributeGroups),
		"allowed_children": _convert_to_array(elem_info.AllowedChildren),
		"parent_elements": _convert_to_array(elem_info.ParentElements)
	}

## Convert C# List<XsdAttributeInfo> to GDScript array of dictionaries
func _convert_attributes(attrs) -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	if attrs == null:
		return result
	
	for attr in attrs:
		result.append({
			"name": attr.Name,
			"type": attr.Type,
			"is_required": attr.IsRequired,
			"default_value": attr.DefaultValue,
			"documentation": attr.Documentation,
			"enum_values": _convert_to_array(attr.EnumValues)
		})
	
	return result

## Convert C# List<string> to GDScript Array[String]
func _convert_to_array(list) -> Array[String]:
	var result: Array[String] = []
	if list == null:
		return result
	for item in list:
		result.append(str(item))
	return result
