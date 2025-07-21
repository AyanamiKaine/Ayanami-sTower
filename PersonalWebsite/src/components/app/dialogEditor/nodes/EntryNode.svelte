<script>
    import { Handle, Position, useNodes } from '@xyflow/svelte';

    let { data, id } = $props();
    const nodes = useNodes();

    /**
     * Updates the node's data when the dialog ID input changes.
     * @param {Event} event - The input event from the form element.
     */
    function handleInput(event) {
        const { name, value } = event.target;
        nodes.current = nodes.current.map((node) => {
            if (node.id === id) {
                return { ...node, data: { ...node.data, [name]: value } };
            }
            return node;
        });
    }
</script>

<div class="entry-node">
    <div class="header">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M11 18H6.93a2 2 0 0 1-1.79-1.11l-1.5-3A2 2 0 0 1 3.82 11H12m4 7V8.5c0-1.4-1.1-2.5-2.5-2.5S11 7.1 11 8.5V18m5-1h-2.5-2.5"></path></svg>
        <span>Dialog Entry</span>
    </div>

    <div class="content">
        <div class="form-group">
            <label for={`dialog-id-input-${id}`}>Dialog ID</label>
            <input
                id={`dialog-id-input-${id}`}
                name="dialogId"
                type="text"
                class="nodrag"
                value={data.dialogId || ''}
                oninput={handleInput}
                placeholder="e.g., 'boulder_quest_1'"
            />
        </div>
        <p class="description">This ID is used to reference and trigger this dialog from the game.</p>
    </div>
       <Handle type="source" position={Position.Bottom} />
</div>

<style>
    .entry-node {
        width: 320px;
        background: #fffbeB; /* Light yellow */
        border: 1px solid #facc15; /* Amber border */
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
        background-color: #fef9c3; /* Lighter yellow */
        color: #854d0e; /* Dark yellow/brown text */
        font-weight: 600;
        border-bottom: 1px solid #fde68a;
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
        background-color: #fefce8;
    }
    
    .content input:focus {
        outline: 2px solid transparent;
        outline-offset: 2px;
        border-color: #facc15;
        box-shadow: 0 0 0 2px #fde047;
    }

    .description {
        font-size: 0.75rem;
        color: #78716c;
        margin: 0;
        padding-top: 0.25rem;
        border-top: 1px dashed #e7e5e4;
    }
</style>
