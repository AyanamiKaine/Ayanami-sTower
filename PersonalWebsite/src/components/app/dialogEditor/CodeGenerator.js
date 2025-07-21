// src/lib/dialog/codeGenerator.js

/**
 * A mapping from the operator keys stored in the node data to the
 * actual symbols used in the generated code.
 */
const OPERATOR_SYMBOLS = {
    Equal: "==",
    NotEqual: "!=",
    GreaterThan: ">",
    LessThan: "<",
    GreaterThanOrEqual: ">=",
    LessThanOrEqual: "<=",
};

/**
 * NEW: Formats a value for inclusion in the generated source code.
 * Booleans and numbers are returned as keywords/literals. Strings are quoted.
 * @param {string} val The raw string value from the node's data.
 * @returns {string} The formatted value as a string for code generation.
 */
function formatCodeValue(val) {
    if (typeof val !== "string") return String(val);

    const lowerVal = val.toLowerCase();
    if (lowerVal === "true") return "true";
    if (lowerVal === "false") return "false";

    if (val.trim() !== "" && isFinite(val)) {
        return val; // It's a number
    }

    // Default to a quoted string, escaping any internal quotes.
    return `"${val.replace(/"/g, '\\"')}"`;
}

/**
 * Recursively traverses the node graph starting from a given node ID
 * and generates a human-readable script.
 *
 * @param {string} nodeId - The ID of the node to start generation from.
 * @param {number} indentLevel - The current indentation level for formatting.
 * @param {Set<string>} visited - A set of visited node IDs to prevent infinite loops.
 * @param {Map<string, object>} nodesMap - A map for quick node lookup by ID.
 * @param {Map<string, object[]>} adjacencyMap - A map representing the graph's connections.
 * @returns {string} The generated code for the current branch.
 */
function generateForNode(nodeId, indentLevel, visited, nodesMap, adjacencyMap) {
    // Base cases for recursion: stop if node is null or already processed.
    if (!nodeId || visited.has(nodeId)) {
        return "";
    }
    const node = nodesMap.get(nodeId);
    if (!node) {
        return "";
    }

    // Mark current node as visited to handle cycles and merged paths
    visited.add(nodeId);

    const indent = "  ".repeat(indentLevel);
    let code = "";
    const children = adjacencyMap.get(nodeId) || [];

    // --- Generate code based on the node's type ---
    switch (node.type) {
        case "entry":
            // Starts a dialog definition block
            code += `${indent}dialog_start("${node.data.dialogId}") {\n`;
            if (children.length > 0) {
                code += generateForNode(
                    children[0].target,
                    indentLevel + 1,
                    visited,
                    nodesMap,
                    adjacencyMap
                );
            }
            code += `${indent}}\n`;
            break;

        case "dialog": {
            const speaker = node.data.speaker
                ? `${node.data.speaker}: `
                : "Narrator: ";
            // Add the speech line for the current node
            if (node.data.speechText) {
                code += `${indent}${speaker}"${node.data.speechText.replace(
                    /\n/g,
                    " "
                )}"\n`;
            }

            // Check if this node presents choices to the player.
            // This is true if it links to multiple 'dialog' nodes.
            const choiceNodes = children
                .map((edge) => nodesMap.get(edge.target))
                .filter((n) => n && n.type === "dialog");

            if (choiceNodes.length > 1) {
                code += `${indent}choice {\n`;
                for (const choice of choiceNodes) {
                    // Each choice is an option with its own sub-block
                    code += `${"  ".repeat(indentLevel + 1)}option "${
                        choice.data.menuText
                    }" {\n`;
                    // Recursively generate the code that follows this choice
                    code += generateForNode(
                        choice.id,
                        indentLevel + 2,
                        visited,
                        nodesMap,
                        adjacencyMap
                    );
                    code += `${"  ".repeat(indentLevel + 1)}}\n`;
                }
                code += `${indent}}\n`;
            } else if (children.length > 0) {
                // If not a choice, just continue the sequence
                code += generateForNode(
                    children[0].target,
                    indentLevel,
                    visited,
                    nodesMap,
                    adjacencyMap
                );
            }
            break;
        }

        case "condition": {
            const operator = OPERATOR_SYMBOLS[node.data.operator] || "==";
            // Ensure string values are quoted in the output
            const value =
                !isNaN(parseFloat(node.data.value)) && isFinite(node.data.value)
                    ? node.data.value
                    : `"${node.data.value}"`;

            code += `${indent}if (state.${node.data.key} ${operator} ${value}) {\n`;
            const trueBranch = children.find((e) => e.handle === "true-output");
            if (trueBranch) {
                code += generateForNode(
                    trueBranch.target,
                    indentLevel + 1,
                    visited,
                    nodesMap,
                    adjacencyMap
                );
            }
            code += `${indent}} else {\n`;
            const falseBranch = children.find(
                (e) => e.handle === "false-output"
            );
            if (falseBranch) {
                code += generateForNode(
                    falseBranch.target,
                    indentLevel + 1,
                    visited,
                    nodesMap,
                    adjacencyMap
                );
            }
            code += `${indent}}\n`;
            break;
        }

        case "instruction":
            const validActions = node.data.actions?.filter(
                (a) => a.key && a.key.trim() !== ""
            );

            if (validActions && validActions.length > 0) {
                code += `${indent}// Instructions\n`;
                for (const action of validActions) {
                    const actionValue = formatCodeValue(action.value);
                    code += `${indent}${action.type}(state, "${action.key}", ${actionValue});\n`;
                }
            }
            if (children.length > 0) {
                code += generateForNode(
                    children[0].target,
                    indentLevel,
                    visited,
                    nodesMap,
                    adjacencyMap
                );
            }
            break;

        case "event":
            code += `${indent}fire_event("${node.data.eventName}");\n`;
            if (children.length > 0) {
                code += generateForNode(
                    children[0].target,
                    indentLevel,
                    visited,
                    nodesMap,
                    adjacencyMap
                );
            }
            break;

        case "output":
            code += `${indent}end_dialog();\n`;
            break;

        // For simple pass-through or editor-only nodes, just continue to the next one
        case "input":
        case "annotation":
        case "state":
            if (children.length > 0) {
                code += generateForNode(
                    children[0].target,
                    indentLevel,
                    visited,
                    nodesMap,
                    adjacencyMap
                );
            }
            break;
    }

    return code;
}

/**
 * Main entry point for the code generation. It prepares the data structures
 * and starts the recursive traversal from the 'entry' node.
 *
 * @param {Array<object>} nodes - The nodes array from SvelteFlow.
 * @param {Array<object>} edges - The edges array from SvelteFlow.
 * @returns {string} The complete, human-readable source code.
 */
export function generateSourceCode(nodes, edges) {
    // 1. Create efficient lookup maps for nodes and their connections.
    const nodesMap = new Map(nodes.map((node) => [node.id, node]));
    const adjacencyMap = new Map();
    edges.forEach((edge) => {
        if (!adjacencyMap.has(edge.source)) {
            adjacencyMap.set(edge.source, []);
        }
        adjacencyMap
            .get(edge.source)
            .push({ target: edge.target, handle: edge.sourceHandle });
    });

    // 2. Find the starting point of the dialog.
    const startNode = nodes.find((n) => n.type === "entry");
    if (!startNode) {
        return "// ERROR: No 'Dialog Entry' node found in the graph.";
    }

    // 3. Begin the recursive generation.
    const visited = new Set();
    return generateForNode(startNode.id, 0, visited, nodesMap, adjacencyMap);
}
