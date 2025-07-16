<script>
  import { useSvelteFlow } from '@xyflow/svelte';

  let { id, top, left, right, bottom, onclick } = $props();
  const { deleteElements, getNodes, addNodes } = useSvelteFlow();

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
  style:top={top ? `${top}px` : undefined}
  style:left={left ? `${left}px` : undefined}
  style:right={right ? `${right}px` : undefined}
  style:bottom={bottom ? `${bottom}px` : undefined}
  class="context-menu"
  onclick={onclick}
>
  <p style="margin: 0.5em;">
    <small>Node: {id}</small>
  </p>
  <button role="menuitem" onclick={deleteNode}>Delete</button>
</div>

<style>
  .context-menu {
    background: #374151;
    color: #f9fafb;
    border: 1px solid #4b5563;
    box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
    position: absolute;
    z-index: 10;
    min-width: 150px;
    border-radius: 0.5rem;
    overflow: hidden;
  }
  .context-menu button {
    background: none;
    border: none;
    color: inherit;
    display: block;
    padding: 0.5em 1em;
    text-align: left;
    width: 100%;
    cursor: pointer;
  }
  .context-menu button:hover {
    background: #4b5563;
  }
</style>