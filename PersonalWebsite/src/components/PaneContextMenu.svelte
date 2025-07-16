<script>
  // 1. Import `useNodes` in addition to `useSvelteFlow`
  import { useSvelteFlow, useNodes } from '@xyflow/svelte';

  let { top, left, clientX, clientY, onclick } = $props();

  // We still need useSvelteFlow for the coordinate conversion
  const { screenToFlowPosition } = useSvelteFlow();
  
  // 2. Get a direct reference to the nodes store
  const nodes = useNodes();

  function createNode(type) {
    const position = screenToFlowPosition({ x: clientX, y: clientY });

    const newNode = {
      id: `node-${Date.now()}`,
      type,
      position,
      data: { label: `${type.charAt(0).toUpperCase() + type.slice(1)} Node` }
    };

    // 3. Instead of calling addNodes, update the store directly
    nodes.current = [...nodes.current, newNode];
    
    onclick();
  }

  function handleKeyDown(event) {
    if (event.key === 'Escape') {
      onclick();
    }
  }
</script>

<div
  role="menu"
  tabindex="-1"
  class="context-menu"
  onclick={onclick}
  onkeydown={handleKeyDown}
  style:top={top ? `${top}px` : undefined}
  style:left={left ? `${left}px` : undefined}
>
  <button role="menuitem" onclick={() => createNode('input')}>Add Input Node</button>
  <button role="menuitem" onclick={() => createNode('default')}>Add Default Node</button>
  <button role="menuitem" onclick={() => createNode('output')}>Add Output Node</button>
</div>

<style>
  .context-menu {
    background: #374151;
    color: #f9fafb;
    border: 1px solid #4b5563;
    box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
    position: absolute;
    z-index: 10;
    min-width: 180px;
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