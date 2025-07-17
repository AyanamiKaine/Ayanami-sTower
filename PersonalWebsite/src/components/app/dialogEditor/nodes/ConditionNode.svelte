<script>
    import { getContext } from "svelte";
    import { Handle, Position, useEdges, useNodes } from "@xyflow/svelte";
    // 1. Import the new operator logic
    import { Operator, OperatorSymbols } from "sfpm-js";

    let { data, id } = $props();
    const edges = useEdges();
    const nodes = useNodes();
    const dialogState = getContext("dialog-state");
    let result = $state(null);

    // This single handler can now update any property in the node's data.
    function handleInput(event) {
        const { name, value } = event.target;
        nodes.current = nodes.current.map((node) => {
            if (node.id === id) {
                // Find the correct symbol for the operator when the select changes
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

    function setMode(newMode) {
        nodes.current = nodes.current.map((n) =>
            n.id === id ? { ...n, data: { ...n.data, mode: newMode } } : n,
        );
    }

    // 2. The evaluation logic now handles both modes
    function evaluateExpression() {
        try {
            let evalResult = false;
            if (data.mode === "expression") {
                const func = new Function("state", `return ${data.expression}`);
                evalResult = func(dialogState);
            } else {
                // Builder mode evaluation - no eval() needed!
                const leftVal = dialogState.get(data.key);

                if (leftVal === undefined) {
                    // Throw an error that will be caught by the catch block.
                    throw new Error(
                        `Key "${data.key}" not found in dialog state.`,
                    );
                }

                // Attempt to convert right value to a number if it looks like one
                const rightVal =
                    !isNaN(parseFloat(data.value)) && isFinite(data.value)
                        ? parseFloat(data.value)
                        : data.value;

                const operatorSymbol = Operator[data.operator];
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
                }
            }
            result = evalResult === true;
        } catch (error) {
            console.error("Evaluation Error:", error);
            result = null;
        }

        // Update edges based on result (this logic is unchanged)
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

<div class="condition-node nodrag">
    <Handle type="target" position={Position.Top} />

    <div class="mode-toggle">
        <button
            class:active={data.mode !== "expression"}
            onclick={() => setMode("builder")}>Builder</button
        >
        <button
            class:active={data.mode === "expression"}
            onclick={() => setMode("expression")}>Expression</button
        >
    </div>

    <div class="content">
        {#if data.mode === "expression"}
            <label for={`expression-input-${id}`}>Expression</label>
            <input
                id={`expression-input-${id}`}
                name="expression"
                type="text"
                class="nodrag"
                value={data.expression}
                oninput={handleInput}
                placeholder="e.g., player.strength > 10"
            />
        {:else}
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
        {/if}

        <button class="nodrag" onclick={evaluateExpression}>Run</button>
    </div>

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
        gap: 0.5rem;
    }

    .content label {
        font-weight: 600;
        color: #4b5563;
    }

    .content input {
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

    /* Style for the handle labels */
    .handle-label {
        position: absolute;
        top: 10px;
        font-size: 0.75rem;
        color: #6b7280;
        transform: translateX(-50%);
    }

    /* Style for the "activated" handle */
    :global(.condition-node .svelte-flow__handle.active) {
        background-color: #22c55e; /* Green */
    }

    .condition-node {
        border-color: #a855f7; /* Purple border to distinguish it */
    }
    .mode-toggle {
        display: flex;
        margin-bottom: 1rem;
        border: 1px solid #d1d5db;
        border-radius: 0.25rem;
        overflow: hidden;
    }
    .mode-toggle button {
        flex-grow: 1;
        padding: 0.25rem;
        border: none;
        background: #f9fafb;
        cursor: pointer;
    }
    .mode-toggle button.active {
        background: #a855f7;
        color: white;
    }
    .builder {
        display: grid;
        grid-template-columns: 1fr auto 1fr;
        gap: 0.5rem;
        align-items: center;
    }
    .builder select {
        border: 1px solid #d1d5db;
        border-radius: 0.25rem;
        padding: 0.5rem 0.25rem;
    }
</style>
