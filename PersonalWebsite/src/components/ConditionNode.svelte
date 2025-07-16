<script>
  import { Handle, Position } from '@xyflow/svelte';

  // The 'data' prop is passed by Svelte Flow and contains our node-specific data.
  let { data } = $props();

  // We use local state to track the evaluation result.
  let result = $state(null);

  /**
   * Evaluates the JavaScript expression from the input field.
   *
   * SECURITY WARNING: This function uses eval(), which can execute arbitrary code.
   * It is a major security risk if the expressions can be provided by an untrusted source.
   * For a production application, you MUST use a sandboxed evaluation library.
   */
  function evaluateExpression() {
    try {
      // We use a Function constructor here, which is slightly safer than a direct eval()
      // but still carries significant risk.
      const func = new Function(`return ${data.expression}`);
      const evalResult = func();
      result = evalResult === true; // Coerce to a strict boolean
    } catch (error) {
      console.error('Evaluation Error:', error);
      result = null; // Reset on error
    }
  }
</script>

<div class="condition-node">
  <Handle type="target" position={Position.Top} />

  <div class="content">
    <label for="expression-input">Condition</label>
    <input
      id="expression-input"
      type="text"
      bind:value={data.expression}
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
    border: 1px solid #e5e7eb;
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