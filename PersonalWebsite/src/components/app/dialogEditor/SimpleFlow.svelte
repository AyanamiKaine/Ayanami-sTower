<script>
  import { SvelteFlow, Controls, Background, BackgroundVariant } from '@xyflow/svelte';
  import '@xyflow/svelte/dist/style.css';

  // Make sure you've renamed ContextMenu.svelte to NodeContextMenu.svelte
  import NodeContextMenu from './contextMenus/NodeContextMenu.svelte';
  import EdgeContextMenu from './contextMenus/EdgeContextMenu.svelte';
  import PaneContextMenu from './contextMenus/PaneContextMenu.svelte'; // 1. Import the new component
  import ConditionNode from './nodes/ConditionNode.svelte'; // 1. Import the new node
  import DialogNode from './nodes/DialogNode.svelte';
  // 2. Create a nodeTypes object to register your custom node
  const nodeTypes = {
    condition: ConditionNode,
    dialog: DialogNode
  };

  let nodes = $state.raw([
    { id: '1', type: 'input', position: { x: 250, y: 25 }, data: { label: 'Input' } },
    { id: '2', position: { x: 100, y: 125 }, data: { label: 'Default' } },
    { id: '3', type: 'output', position: { x: 250, y: 250 }, data: { label: 'Output' } }
  ]);
  
  let edges = $state.raw([
    { id: 'e1-2', source: '1', target: '2' },
    { id: 'e2-3', source: '2', target: '3', animated: true }
  ]);

  let nodeMenu = $state(null);
  let edgeMenu = $state(null);
  let paneMenu = $state(null); // 2. Add state for the pane menu

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

  // 3. Add a handler for the pane's context menu
  const handlePaneContextMenu = ({ event }) => {
    event.preventDefault();
    
    paneMenu = {
      top: event.clientY,
      left: event.clientX,
      clientX: event.clientX,
      clientY: event.clientY
    };
  };

  function handlePaneClick() {
    nodeMenu = null;
    edgeMenu = null;
    paneMenu = null;
  }
</script>

<div style="width: 100%; height: 100%;">
  <SvelteFlow 
    {nodeTypes}
    bind:nodes 
    bind:edges 
    fitView
    onnodecontextmenu={handleNodeContextMenu}
    onedgecontextmenu={handleEdgeContextMenu}
    onpanecontextmenu={handlePaneContextMenu}
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

    <!-- 5. Conditionally render the new PaneContextMenu -->
    {#if paneMenu}
      <PaneContextMenu
        top={paneMenu.top}
        left={paneMenu.left}
        clientX={paneMenu.clientX}
        clientY={paneMenu.clientY}
        onclick={handlePaneClick}
      />
    {/if}
  </SvelteFlow>
</div>