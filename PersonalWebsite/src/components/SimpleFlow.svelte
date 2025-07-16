<script>
  import { SvelteFlow, Controls, Background, BackgroundVariant } from '@xyflow/svelte';
  import '@xyflow/svelte/dist/style.css';
  import ContextMenu from './ContextMenu.svelte';

  let nodes = $state.raw([
    { id: '1', type: 'input', position: { x: 250, y: 25 }, data: { label: 'Input' } },
    { id: '2', position: { x: 100, y: 125 }, data: { label: 'Default' } },
    { id: '3', type: 'output', position: { x: 250, y: 250 }, data: { label: 'Output' } }
  ]);
  
  let edges = $state.raw([
    { id: 'e1-2', source: '1', target: '2' },
    { id: 'e2-3', source: '2', target: '3', animated: true }
  ]);

  let menu = $state(null);
  let clientWidth = $state(0);
  let clientHeight = $state(0);

  const handleNodeContextMenu = ({ event, node }) => {
    event.preventDefault();
    const pane = (event.target).closest('.svelte-flow');
    if (!pane) return;
    
    const paneBounds = pane.getBoundingClientRect();
    menu = {
      id: node.id,
      top: event.clientY < paneBounds.height - 200 ? event.clientY : undefined,
      left: event.clientX < paneBounds.width - 200 ? event.clientX : undefined,
      right:
        event.clientX >= paneBounds.width - 200
          ? paneBounds.width - event.clientX
          : undefined,
      bottom:
        event.clientY >= paneBounds.height - 200
          ? paneBounds.height - event.clientY
          : undefined,
    };
  };

  function handlePaneClick() {
    menu = null;
  }
</script>

<div style="width: 100%; height: 100%;" bind:clientWidth bind:clientHeight>
  <SvelteFlow 
    bind:nodes 
    bind:edges 
    fitView
    onnodecontextmenu={handleNodeContextMenu}
    onpaneclick={handlePaneClick}
  >
    <Background variant={BackgroundVariant.Dots} />
    <Controls />

    {#if menu}
      <ContextMenu
        id={menu.id}
        top={menu.top}
        left={menu.left}
        right={menu.right}
        bottom={menu.bottom}
        onclick={handlePaneClick}
      />
    {/if}
  </SvelteFlow>
</div>