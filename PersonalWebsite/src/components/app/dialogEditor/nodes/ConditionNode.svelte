<script>
  // 1. Import useEdges
  import { Handle, Position, useEdges, useNodes  } from '@xyflow/svelte';

  // 2. Get the node's `id` from props, in addition to `data`
  let { data, id } = $props();

  // 3. Get a writable reference to the edges store
  const edges = useEdges();
  const nodes = useNodes();

  let result = $state(null);

function handleInput(event) {
    const newExpression = event.target.value;
    nodes.current = nodes.current.map((node) => {
      if (node.id === id) {
        // Create a new node object with the updated expression
        return { ...node, data: { ...node.data, expression: newExpression } };
      }
      return node;
    });
  }

  function evaluateExpression() {
    try {
      const func = new Function(`return ${data.expression}`);
      const evalResult = func();
      result = evalResult === true;

      // 4. NEW LOGIC: Find and update the outgoing edges
      edges.current = edges.current.map((edge) => {
        // Find edges originating from this specific node instance
        if (edge.source === id) {
          // If this edge is connected to the 'true' handle, activate/deactivate it
          if (edge.sourceHandle === 'true-output') {
            return { ...edge, animated: result === true };
          }
          // If this edge is connected to the 'false' handle, activate/deactivate it
          if (edge.sourceHandle === 'false-output') {
            return { ...edge, animated: result === false };
          }
        }
        // Return all other edges unchanged
        return edge;
      });
    } catch (error) {
      console.error('Evaluation Error:', error);
      result = null;

      // On error, ensure both paths from this node are deactivated
      edges.current = edges.current.map((edge) => {
        if (edge.source === id) {
          return { ...edge, animated: false };
        }
        return edge;
      });
    }
  }
</script>

<div class="condition-node">
  <Handle type="target" position={Position.Top} />

  <div class="content">
    <label for="expression-input-{id}">Condition</label>
    <input
      id="expression-input-{id}"
      type="text"
      value={data.expression}
      oninput={handleInput}
      placeholder="e.g., 10 > 5"
    />
    <button onclick={evaluateExpression}>Run</button>
  </div>

  <Handle
    id="true-output"
    type="source"
    position={Position.Bottom}
    style="left: 25%;"
    class={result === true ? 'active' : ''}
  >
    <div class="handle-label">True</div>
  </Handle>

  <Handle
    id="false-output"
    type="source"
    position={Position.Bottom}
    style="left: 75%;"
    class={result === false ? 'active' : ''}
  >
    <div class="handle-label">False</div>
  </Handle>
</div>

<style>
  .condition-node {
    width: 200px;
    background: #ffffff;
    border: 1px solid rgb(65, 65, 65);
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
</style>