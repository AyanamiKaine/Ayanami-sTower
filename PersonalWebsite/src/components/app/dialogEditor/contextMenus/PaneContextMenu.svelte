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

    let data;
    // Set initial data based on node type
    if (type === 'condition') {
      data = { 
        mode: 'builder',
        key: 'player.strength',
        operator: 'GreaterThan', // Storing the key of the enum
        value: '10',
        expression: 'player.strength > 10'
      };
    } else if (type === 'dialog') {
      // Provide default values for the dialog node's data
      data = { menuText: '', speechText: '' };
     } else if (type === 'instruction') {
      data = {
        code: "// 'state' is the Dialog State Map\nstate.set('playerGold', state.get('playerGold') + 50);"
      };
    }
    else if (type === 'localState')
    {

    }
    else if (type === 'comment')
    {
    }
    else if (type === 'entry') {
        data = { dialogId: `new_dialog_${Date.now()}` };
    }
    else if (type === 'event') {
      data = {

      };
    } else {
      data = { label: `${type.charAt(0).toUpperCase() + type.slice(1)} Node` };
    }

    const newNode = {
      id: `node-${Date.now()}`,
      type,
      position,
      data
    };

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
  <button role="menuitem" onclick={() => createNode('entry')}>Add Entry Node</button>
  <button role="menuitem" onclick={() => createNode('comment')}>Add Comment Node</button>
  <button role="menuitem" onclick={() => createNode('condition')}>Add Condition Node</button>
  <button role="menuitem" onclick={() => createNode('dialog')}>Add Dialog Node</button>
  <button role="menuitem" onclick={() => createNode('instruction')}>Add Instruction Node</button>
  <button role="menuitem" onclick={() => createNode('event')}>Add Event Node</button>
  <button role="menuitem" onclick={() => createNode('code')}>Add Code Node</button>
  <button role="menuitem" onclick={() => createNode('transition')}>Add Transition Node</button>
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