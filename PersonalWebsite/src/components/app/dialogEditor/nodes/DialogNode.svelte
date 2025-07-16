<script>
  import { Handle, Position } from '@xyflow/svelte';
  import { useNodes } from '@xyflow/svelte';

  let { data, id } = $props();
  const nodes = useNodes();

  function handleInput(event) {
    const { name, value } = event.target;
    nodes.current = nodes.current.map((node) => {
      if (node.id === id) {
        return { ...node, data: { ...node.data, [name]: value } };
      }
      return node;
    });
  }
</script>

<div class="dialog-node">
  <Handle type="target" position={Position.Top} />

  <div class="content">
    <label for="menu-text-input-{id}">Menu Text</label>
    <textarea
      id="menu-text-input-{id}"
      name="menuText"
      rows="2"
      class="nodrag"
      value={data.menuText || ''}
      oninput={handleInput}
      placeholder="Player's choice..."
    ></textarea>

    <label for="speech-text-input-{id}">Speech Text</label>
    <textarea
      id="speech-text-input-{id}"
      name="speechText"
      rows="4"
      class="nodrag"
      value={data.speechText || ''}
      oninput={handleInput}
      placeholder="Character's response..."
    ></textarea>
  </div>

  <Handle type="source" position={Position.Bottom} />
</div>
<style>
  .dialog-node {
    width: 250px;
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
    gap: 0.75rem; /* Increased gap for better spacing */
  }

  .content label {
    font-weight: 600;
    color: #4b5563;
    margin-bottom: -0.5rem; /* Pull label closer to its textarea */
  }

  .content textarea {
    border: 1px solid #d1d5db;
    border-radius: 0.25rem;
    padding: 0.5rem;
    width: 100%;
    box-sizing: border-box;
    resize: vertical; /* Allow vertical resizing */
    font-family: inherit;
  }
</style>