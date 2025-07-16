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
    
       {
      id: 'intro',
      type: 'input',
      position: { x: 25, y: -300 },
      data: { label: 'Begin of Conversation' }
    },
   {
      id: 'start',
      type: 'dialog',
      position: { x: -25 , y: -150 },
      data: {
        menuText: 'Start Conversation',
        speechText: 'Greetings, traveler. I need you to lift this heavy boulder. Are you strong enough?'
      }
    },
    {
      id: 'strength-check',
      type: 'condition',
      position: { x: 0, y: 200 },
      data: {
        // We'll pretend a 'player' object exists for the evaluation
        expression: 'player.strength > 10'
      }
    },
    {
      id: 'response-strong',
      type: 'dialog',
      position: { x: -200, y: 400 },
      data: {
        menuText: '(Heave the boulder)',
        speechText: 'Incredible! You lifted it with ease. Thank you!'
      }
    },
    {
      id: 'response-weak',
      type: 'dialog',
      position: { x: 200, y: 400 },
      data: {
        menuText: '(Try to lift the boulder and fail)',
        speechText: 'Hmm. It seems you need to train a bit more. Come back when you are stronger.'
      }
    },
    {
      id: 'end',
      type: 'output',
      position: { x: 25, y: 800 },
      data: { label: 'End Conversation' }
    }
  ]);
  
  // New edges to connect the dialog tree nodes.
  let edges = $state.raw([
    { id: 'e-start-node-check', source: 'intro', target: 'start' },
    { id: 'e-start-check', source: 'start', target: 'strength-check' },
    // Note the use of sourceHandle to connect to the correct output on the condition node
    { id: 'e-check-strong', source: 'strength-check', sourceHandle: 'true-output', target: 'response-strong' },
    { id: 'e-check-weak', source: 'strength-check', sourceHandle: 'false-output', target: 'response-weak' },
    { id: 'e-strong-end', source: 'response-strong', target: 'end' },
    { id: 'e-weak-end', source: 'response-weak', target: 'end' }
  ]);

  let nodeMenu = $state(null);
  let edgeMenu = $state(null);
  let paneMenu = $state(null);

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