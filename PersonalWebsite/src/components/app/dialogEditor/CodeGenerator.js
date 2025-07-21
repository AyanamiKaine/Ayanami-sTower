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
    if (!nodeId || visited.has(nodeId)) return "";
    const node = nodesMap.get(nodeId);
    if (!node) return "";

    visited.add(nodeId);

    const indent = "  ".repeat(indentLevel);
    let code = "";
    const children = adjacencyMap.get(nodeId) || [];

    switch (node.type) {
        case "entry":
            // Generate an async function declaration for the entire dialog.
            // We pass in 'state' and 'helpers' for context.
            code += `async function ${node.data.dialogId}(state, helpers) {\n`;
            if (children.length > 0) {
                code += generateForNode(
                    children[0].target,
                    indentLevel + 1,
                    visited,
                    nodesMap,
                    adjacencyMap
                );
            }
            code += `}\n`;
            break;

        case "dialog": {
            const speaker = node.data.speaker || "UNDEFINED_SPEAKER";
            if (node.data.speechText) {
                // Dialogue is now an await-ed helper call.
                const speechText = node.data.speechText.replace(/"/g, '\\"');
                code += `${indent}await helpers.show_dialog({ speaker: "${speaker}", text: "${speechText}" });\n`;
            }

            const choiceNodes = children
                .map((edge) => nodesMap.get(edge.target))
                .filter((n) => n?.type === "dialog");

            if (choiceNodes.length > 1) {
                // Generate an array of choice strings for the helper.
                const choiceOptions = choiceNodes
                    .map((c) => `"${c.data.menuText.replace(/"/g, '\\"')}"`)
                    .join(", ");
                code += `${indent}const choice = await helpers.show_choice([${choiceOptions}]);\n`;

                // Use a switch statement for branching.
                code += `${indent}switch (choice) {\n`;
                for (const choiceNode of choiceNodes) {
                    const menuText = choiceNode.data.menuText.replace(
                        /"/g,
                        '\\"'
                    );
                    code += `${"  ".repeat(
                        indentLevel + 1
                    )}case "${menuText}":\n`;
                    code += generateForNode(
                        choiceNode.id,
                        indentLevel + 2,
                        visited,
                        nodesMap,
                        adjacencyMap
                    );
                    code += `${"  ".repeat(indentLevel + 2)}break;\n`;
                }
                code += `${indent}}\n`;
            } else if (children.length > 0) {
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
            const value = formatCodeValue(node.data.value); // Use our existing formatter
            // Conditions now check against the 'state' object.
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

        case "instruction": {
            const validActions = node.data.actions?.filter((a) =>
                a.key?.trim()
            );
            if (validActions?.length > 0) {
                code += `${indent}// Instructions\n`;
                for (const action of validActions) {
                    const actionValue = formatCodeValue(action.value);
                    // Actions are now calls to helper methods.
                    code += `${indent}helpers.${action.type}(state, "${action.key}", ${actionValue});\n`;
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
        }

        case "event":
            code += `${indent}helpers.fire_event("${node.data.eventName}");\n`;
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
            code += `${indent}return; // End of dialog\n`;
            break;

        default:
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
