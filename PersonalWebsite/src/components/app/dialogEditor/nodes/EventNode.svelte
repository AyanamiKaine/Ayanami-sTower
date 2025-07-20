<script>
    import { Handle, Position, useNodes } from '@xyflow/svelte';
    import { notification } from '../../../../lib/stores';

    let { data, id } = $props();
    const nodes = useNodes();

    /**
     * Updates the node's data when the event name input changes.
     * @param {Event} event - The input event from the form element.
     */
    function handleInput(event) {
        const { value } = event.target;
        nodes.current = nodes.current.map((node) => {
            if (node.id === id) {
                // Store the event name in the node's data object
                return { ...node, data: { ...node.data, eventName: value } };
            }
            return node;
        });
    }
</script>

<div class="event-node">
    <Handle type="target" position={Position.Top} />

    <!-- Styled header for consistency and clarity -->
    <div class="header">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/></svg>
        <span>Fire Event</span>
    </div>

    <div class="content">
        <div class="form-group">
            <label for={`event-name-input-${id}`}>Event Name</label>
            <input
                id={`event-name-input-${id}`}
                name="eventName"
                type="text"
                class="nodrag"
                value={data.eventName || ''}
                oninput={handleInput}
                placeholder="e.g., 'startCutscene'"
            />
        </div>
    </div>
    
    <Handle type="source" position={Position.Bottom} />
</div>

<style>
    .event-node {
        width: 320px;
        background: #ffffff;
        border: 1px solid #3b82f6; /* Blue border */
        border-radius: 0.5rem;
        font-family: sans-serif;
        font-size: 0.875rem;
        box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
        overflow: hidden;
    }

    .header {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.75rem 1rem;
        background-color: #dbeafe; /* Light blue background */
        color: #1e40af; /* Dark blue text */
        font-weight: 600;
        border-bottom: 1px solid #bfdbfe; /* Lighter blue border */
    }

    .content {
        padding: 1rem;
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
    }
    
    .form-group {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
    }

    .form-group label {
        font-weight: 500;
        font-size: 0.75rem;
        color: #4b5563;
    }

    .content input {
        border: 1px solid #d1d5db;
        border-radius: 0.375rem;
        padding: 0.5rem 0.75rem;
        width: 100%;
        box-sizing: border-box;
        background-color: #f9fafb;
    }
    
    .content input:focus {
        outline: 2px solid transparent;
        outline-offset: 2px;
        border-color: #3b82f6;
        box-shadow: 0 0 0 2px #bfdbfe;
    }

    .run-btn {
        margin-top: 0.5rem;
        background-color: #3b82f6;
        color: white;
        border: none;
        border-radius: 0.375rem;
        padding: 0.6rem;
        cursor: pointer;
        font-weight: 600;
        transition: background-color 0.2s;
    }

    .run-btn:hover {
        background-color: #2563eb;
    }
</style>