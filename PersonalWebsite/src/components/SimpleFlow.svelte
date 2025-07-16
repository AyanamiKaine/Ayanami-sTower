<script>
  import { SvelteFlow, Controls, Background, BackgroundVariant } from '@xyflow/svelte';
  import '@xyflow/svelte/dist/style.css';

  // Import both context menu components
  import NodeContextMenu from './ContextMenu.svelte';
  import EdgeContextMenu from './EdgeContextMenu.svelte';

  let nodes = $state.raw([
    { id: '1', type: 'input', position: { x: 250, y: 25 }, data: { label: 'Input' } },
    { id: '2', position: { x: 100, y: 125 }, data: { label: 'Default' } },
    { id: '3', type: 'output', position: { x: 250, y: 250 }, data: { label: 'Output' } }
  ]);
  
  let edges = $state.raw([
    { id: 'e1-2', source: '1', target: '2' },
    { id: 'e2-3', source: '2', target: '3', animated: true }
  ]);

  // Renamed 'menu' to 'nodeMenu' for clarity
  let nodeMenu = $state(null);
  // Add a new state variable for the edge menu
  let edgeMenu = $state(null);

  let clientWidth = $state(0);
  let clientHeight = $state(0);

  const handleNodeContextMenu = ({ event, node }) => {
    event.preventDefault();
    const paneBounds = event.target.closest('.svelte-flow').getBoundingClientRect();
    nodeMenu = {
      id: node.id,
      top: event.clientY < paneBounds.height - 200 ? event.clientY : undefined,
      left: event.clientX < paneBounds.width - 200 ? event.clientX : undefined,
      right: event.clientX >= paneBounds.width - 200 ? paneBounds.width - event.clientX : undefined,
      bottom: event.clientY >= paneBounds.height - 200 ? paneBounds.height - event.clientY : undefined,
    };
  };

  // Create a new handler for the edge context menu
  const handleEdgeContextMenu = ({ event, edge }) => {
    event.preventDefault();
    const paneBounds = event.target.closest('.svelte-flow').getBoundingClientRect();
    edgeMenu = {
      id: edge.id,
      top: event.clientY < paneBounds.height - 200 ? event.clientY : undefined,
      left: event.clientX < paneBounds.width - 200 ? event.clientX : undefined,
      right: event.clientX >= paneBounds.width - 200 ? paneBounds.width - event.clientX : undefined,
      bottom: event.clientY >= paneBounds.height - 200 ? paneBounds.height - event.clientY : undefined,
    };
  };


  // Update handlePaneClick to close both menus
  function handlePaneClick() {
    nodeMenu = null;
    edgeMenu = null;
  }
</script>

<div style="width: 100%; height: 100%;" bind:clientWidth bind:clientHeight>
  <SvelteFlow 
    bind:nodes 
    bind:edges 
    fitView
    onnodecontextmenu={handleNodeContextMenu}
    onedgecontextmenu={handleEdgeContextMenu}
    onpaneclick={handlePaneClick}
  >
    <Background variant={BackgroundVariant.Dots} />
    <Controls />

    {#if nodeMenu}
      <NodeContextMenu
        id={nodeMenu.id}
        top={nodeMenu.top}
        left={nodeMenu.left}
        right={nodeMenu.right}
        bottom={nodeMenu.bottom}
        onclick={handlePaneClick}
      />
    {/if}

    {#if edgeMenu}
      <EdgeContextMenu
        id={edgeMenu.id}
        top={edgeMenu.top}
        left={edgeMenu.left}
        right={edgeMenu.right}
        bottom={edgeMenu.bottom}
        onclick={handlePaneClick}
      />
    {/if}
  </SvelteFlow>
</div>