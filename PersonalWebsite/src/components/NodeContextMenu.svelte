<script>
  import { useSvelteFlow } from '@xyflow/svelte';

  let { id, top, left, right, bottom, onclick } = $props();
  const { deleteElements, getNodes, addNodes } = useSvelteFlow();

  function handleKeyDown(event) {
    if (event.key === 'Escape') {
      onclick();
    }
  }

  function duplicateNode() {
    const nodes = getNodes();
    const nodeToDuplicate = nodes.find((node) => node.id === id);
    if (!nodeToDuplicate) return;
    const newNode = {
      ...nodeToDuplicate,
      id: `${id}-copy-${Math.random()}`,
      position: { x: nodeToDuplicate.position.x, y: nodeToDuplicate.position.y + 50 }
    };
    addNodes(newNode);
  }

  function deleteNode() {
    deleteElements({ nodes: [{ id }] });
  }
</script>

<div
  role="menu"
  tabindex="-1"
  style:top={top ? `${top}px` : undefined}
  style:left={left ? `${left}px` : undefined}
  style:right={right ? `${right}px` : undefined}
  style:bottom={bottom ? `${bottom}px` : undefined}
  class="context-menu"
  onclick={onclick}
  onkeydown={handleKeyDown}
>
  <p style="margin: 0.5em;">
    <small>Node: {id}</small>
  </p>
  <button role="menuitem" onclick={deleteNode}>Delete</button>
</div>

<style>
  .context-menu {
    background: #ffffff;
    color: #1f2937; /* Dark gray text */
    border: 1px solid #e5e7eb; /* Light gray border */
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08); /* Softer, modern shadow */
    position: absolute;
    z-index: 10;
    min-width: 180px;
    border-radius: 0.5rem;
    overflow: hidden;
    padding: 0.25rem; /* Add some internal padding */
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif;
  }

  .context-menu button {
    background: none;
    border: none;
    color: inherit;
    display: block;
    padding: 0.5rem 0.75rem;
    text-align: left;
    width: 100%;
    cursor: pointer;
    font-size: 0.875rem;
    border-radius: 0.25rem; /* Rounded corners for buttons */
  }

  .context-menu button:hover {
    background: #f3f4f6; /* Very light gray hover */
  }

  /* Style for the 'Node: id' text in the NodeContextMenu */
  .context-menu p {
    padding: 0.5rem 0.75rem;
    font-size: 0.75rem;
    color: #6b7280; /* Medium gray text */
    border-bottom: 1px solid #e5e7eb;
    margin: 0 -0.25rem 0.25rem -0.25rem; /* Adjust for padding */
  }
</style>