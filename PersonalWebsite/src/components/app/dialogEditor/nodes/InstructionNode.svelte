<script>
  import { Handle, Position, useNodes } from '@xyflow/svelte';
  import { getContext } from 'svelte';
  import Sandbox from '@nyariv/sandboxjs';
  import { notification } from '../../../../lib/stores'

  let { data, id } = $props();
  const nodes = useNodes();
  // Get the shared dialog state Map
  let dialogState = getContext('dialog-state');

  // Create a single sandbox instance for this node
  const sandbox = new Sandbox();

  function handleInput(event) {
    const { value } = event.target;
    nodes.current = nodes.current.map((node) => {
      if (node.id === id) {
        return { ...node, data: { ...node.data, code: value } };
      }
      return node;
    });
  }

  function executeCode() {
    if (!dialogState) {
      notification.set(error.message);
      console.error("Dialog state context not found!");
      return;
    }
    
    try {
      // Compile the user's code
      const compiled = sandbox.compile(data.code || '');
      
      compiled({ state: dialogState }).run();

      console.log("Instruction executed. New state:", dialogState);

      dialogState = new Map(dialogState);

    } catch (error) {
      notification.set(error.message);
      console.error("Sandbox Execution Error:", error);
    }
  }
</script>

<div class="instruction-node">
  <Handle type="target" position={Position.Top} />

  <div class="content">
    <label for={`code-input-${id}`}>Instruction Code</label>
    <textarea
      id={`code-input-${id}`}
      class="nodrag"
      rows="5"
      value={data.code || ''}
      oninput={handleInput}
      placeholder="// 'state' is the Dialog State Map&#10;state.set('playerGold', 100);"
    ></textarea>
    <button class="nodrag" onclick={executeCode}>Run</button>
  </div>
  
  <Handle type="source" position={Position.Bottom} />
</div>

<style>
  .instruction-node {
    width: 300px;
    background: #ffffff;
    border: 1px solid #f59e0b; /* Amber border */
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
  .content textarea {
    border: 1px solid #d1d5db;
    border-radius: 0.25rem;
    padding: 0.5rem;
    width: 100%;
    box-sizing: border-box;
    resize: vertical;
    font-family: monospace; /* Use a monospace font for code */
  }
  .content button {
    background-color: #f59e0b; /* Amber button */
    color: white;
    border: none;
    border-radius: 0.25rem;
    padding: 0.5rem;
    cursor: pointer;
    font-weight: 600;
  }
  .content button:hover {
    background-color: #d97706;
  }
</style>