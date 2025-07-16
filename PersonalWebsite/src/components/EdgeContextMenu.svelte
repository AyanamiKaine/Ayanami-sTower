<script>
  import { useSvelteFlow } from '@xyflow/svelte';

  // These props are passed from the main flow component
  let { id, top, left, right, bottom, onclick } = $props();

  const { deleteElements } = useSvelteFlow();

  function deleteEdge() {
    // We tell Svelte Flow to delete the edge with the given id
    deleteElements({ edges: [{ id }] });
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
  <button role="menuitem" onclick={deleteEdge}>Delete connection</button>
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