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
        <!-- The condition builder is now the only UI -->
        <div class="builder">
            <input
                name="key"
                type="text"
                class="nodrag"
                value={data.key || ""}
                oninput={handleInput}
                placeholder="player.strength"
            />
            <select name="operator" class="nodrag" oninput={handleInput}>
                {#each Object.entries(Operator) as [key, symbol]}
                    <option
                        value={symbol.toString()}
                        selected={data.operator === key}
                    >
                        {OperatorSymbols[symbol]}
                    </option>
                {/each}
            </select>
            <input
                name="value"
                type="text"
                class="nodrag"
                value={data.value || ""}
                oninput={handleInput}
                placeholder="10"
            />
        </div>

        <button class="nodrag" onclick={evaluateExpression}>Run</button>
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
        width: 300px;
        background: #ffffff;
        border: 1px solid #a855f7; /* Purple border to distinguish it */
        border-radius: 0.5rem;
        padding: 1rem;
        font-family: sans-serif;
        font-size: 0.875rem;
    }

    .content {
        display: flex;
        flex-direction: column;
        gap: 0.75rem; /* Increased gap for better spacing */
    }

    .content input,
    .builder select {
        border: 1px solid #d1d5db;
        border-radius: 0.25rem;
        padding: 0.5rem;
        width: 100%;
        box-sizing: border-box;
    }

    .content button {
        background-color: #3b82f6;
        color: white;
        border: none;
        border-radius: 0.25rem;
        padding: 0.5rem;
        cursor: pointer;
        font-weight: 600;
    }

    .content button:hover {
        background-color: #2563eb;
    }

    .handle-label {
        position: absolute;
        top: 12px; /* Adjusted position */
        font-size: 0.75rem;
        color: #6b7280;
        transform: translateX(-50%);
        pointer-events: none; /* Make label non-interactive */
    }

    /* Style for the "activated" handle */
    :global(.condition-node .svelte-flow__handle.active) {
        background-color: #22c55e; /* Green */
    }

    .builder {
        display: grid;
        grid-template-columns: 1fr auto 1fr;
        gap: 0.5rem;
        align-items: center;
    }

    .builder select {
        padding: 0.5rem 0.25rem;
    }
</style>