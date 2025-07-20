<script>
    import { Handle, Position, useEdges, useNodes } from "@xyflow/svelte";
    // 1. Import operator logic
    import { Operator, OperatorSymbols } from "sfpm-js";
    import { notification } from '../../../../lib/stores'
    import dialogState from "../../../../lib/state.svelte.js";

    let { data, id } = $props();
    const edges = useEdges();
    const nodes = useNodes();
    let result = $state(null);


    // TODO: The condition node should re-evaluate when the dialog state changes.

    /**
     * Updates the node's data when an input value changes.
     * @param {Event} event - The input event from the form element.
     */
    function handleInput(event) {
        const { name, value } = event.target;
        nodes.current = nodes.current.map((node) => {
            if (node.id === id) {
                // When the operator dropdown changes, find the corresponding operator key
                const finalValue =
                    name === "operator"
                        ? Object.keys(Operator).find(
                            (key) => Operator[key].toString() === value,
                        )
                        : value;

                return { ...node, data: { ...node.data, [name]: finalValue } };
            }
            return node;
        });
    }

    /**
     * Evaluates the condition based on the builder inputs.
     */
    function evaluateExpression() {
        try {
            let evalResult = false;

            // Get the left-hand value from the shared dialog state
            const leftVal = dialogState.state.get(data.key);

            if (leftVal === undefined) {
                throw new Error(
                    `Key "${data.key}" not found in dialog state.`,
                );
            }

            // Parse the right-hand value as a number if possible, otherwise use as a string
            const rightVal =
                !isNaN(parseFloat(data.value)) && isFinite(data.value)
                    ? parseFloat(data.value)
                    : data.value;

            const operatorSymbol = Operator[data.operator];
            
            // Perform the comparison based on the selected operator
            switch (operatorSymbol) {
                case Operator.Equal:
                    evalResult = leftVal === rightVal;
                    break;
                case Operator.NotEqual:
                    evalResult = leftVal !== rightVal;
                    break;
                case Operator.GreaterThan:
                    evalResult = leftVal > rightVal;
                    break;
                case Operator.LessThan:
                    evalResult = leftVal < rightVal;
                    break;
                case Operator.GreaterThanOrEqual:
                    evalResult = leftVal >= rightVal;
                    break;
                case Operator.LessThanOrEqual:
                    evalResult = leftVal <= rightVal;
                    break;
                default:
                    throw new Error(`Unsupported operator: ${data.operator}`);
            }
            
            result = evalResult === true;

        } catch (error) {
            console.error("Evaluation Error:", error);
            notification.set(error.message);
            result = null; // Reset result on error
        }

        // Update the outgoing edges to animate the active path
        edges.current = edges.current.map((edge) => {
            if (edge.source === id) {
                if (edge.sourceHandle === "true-output")
                    return { ...edge, animated: result === true };
                if (edge.sourceHandle === "false-output")
                    return { ...edge, animated: result === false };
            }
            return edge;
        });
    }
</script>

<div class="condition-node">
    <Handle type="target" position={Position.Top} />

    <div class="content">
        <!-- The form is now organized into vertical groups for clarity and space -->
        <div class="form-group">
            <label for={`key-input-${id}`}>Key</label>
            <input
                id={`key-input-${id}`}
                name="key"
                type="text"
                class="nodrag"
                value={data.key || ""}
                oninput={handleInput}
                placeholder="e.g., player.strength"
            />
        </div>

      <div class="form-group">
            <label for={`operator-select-${id}`}>Operator</label>
            <select id={`operator-select-${id}`} name="operator" class="nodrag" oninput={handleInput}>
                {#each Object.entries(Operator).filter(([key]) => key !== 'Predicate') as [key, symbol]}
                    <option
                        value={symbol.toString()}
                        selected={data.operator === key}
                    >
                        {OperatorSymbols[symbol]}
                    </option>
                {/each}
            </select>
        </div>
        
        <div class="form-group">
            <label for={`value-input-${id}`}>Value</label>
            <input
                id={`value-input-${id}`}
                name="value"
                type="text"
                class="nodrag"
                value={data.value || ""}
                oninput={handleInput}
                placeholder="e.g., 10"
            />
        </div>

        <button class="nodrag" onclick={evaluateExpression}>Evaluate</button>
    </div>

    <!-- Output handles for true/false paths -->
    <Handle
        id="true-output"
        type="source"
        position={Position.Bottom}
        style="left: 25%;"
        class={result === true ? "active" : ""}
    >
        <div class="handle-label">True</div>
    </Handle>
    <Handle
        id="false-output"
        type="source"
        position={Position.Bottom}
        style="left: 75%;"
        class={result === false ? "active" : ""}
    >
        <div class="handle-label">False</div>
    </Handle>
</div>

<style>
    .condition-node {
        width: 320px; /* Slightly wider for more breathing room */
        background: #ffffff;
        border: 1px solid #a855f7;
        border-radius: 0.5rem;
        padding: 1rem;
        font-family: sans-serif;
        font-size: 0.875rem;
        box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
    }

    .content {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
    }
    
    .form-group {
        display: flex;
        flex-direction: column;
        gap: 0.25rem; /* Adds a small space between label and input */
    }

    .form-group label {
        font-weight: 500;
        font-size: 0.75rem;
        color: #4b5563;
    }

    .content input,
    .content select {
        border: 1px solid #d1d5db;
        border-radius: 0.375rem; /* Slightly more rounded */
        padding: 0.5rem 0.75rem;
        width: 100%;
        box-sizing: border-box;
        background-color: #f9fafb;
    }
    
    .content input:focus,
    .content select:focus {
        outline: 2px solid transparent;
        outline-offset: 2px;
        border-color: #a855f7;
        box-shadow: 0 0 0 2px #c4b5fd;
    }

    .content button {
        margin-top: 0.5rem; /* Add some space above the button */
        background-color: #8b5cf6;
        color: white;
        border: none;
        border-radius: 0.375rem;
        padding: 0.6rem;
        cursor: pointer;
        font-weight: 600;
        transition: background-color 0.2s;
    }

    .content button:hover {
        background-color: #7c3aed;
    }

    .handle-label {
        position: absolute;
        top: 12px;
        font-size: 0.75rem;
        color: #6b7280;
        transform: translateX(-50%);
        pointer-events: none;
    }

    :global(.condition-node .svelte-flow__handle.active) {
        background-color: #22c55e;
    }

</style>