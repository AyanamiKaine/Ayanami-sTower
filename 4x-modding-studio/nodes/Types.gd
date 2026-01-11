class_name Types
extends RefCounted

# Define your enum here
enum NodeType {
	ACTION, # Actions are things that are being executed by the X4 script evaluator.
	ACTIONS,
	EXPRESSION, # Expressions are things that produce a value.
	CONDITION, # Conditions are elements that return a bool internally in the X4 script evaluator.
	CONDITIONS,
	CUE,
	CUES,
	MISSION_SCRIPT, # A mission script is usually the root for an independet script. Often used to define new missions and events.
	NONE,
	DELAY
}
