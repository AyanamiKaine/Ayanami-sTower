<script>
    import { Handle, Position, useNodes } from '@xyflow/svelte';

    let { data, id } = $props();
    const nodes = useNodes();

    /**
     * A generic input handler that updates the corresponding property 
     * in the node's data object.
     * @param {Event} event - The input event from the form element.
     */
    function handleInput(event) {
        const { name, value } = event.target;
        nodes.current = nodes.current.map((node) => {
            if (node.id === id) {
                // Dynamically update the property based on the input's name
                return { ...node, data: { ...node.data, [name]: value } };
            }
            return node;
        });
    }
</script>

<div class="dialog-node">
    <Handle type="target" position={Position.Top} />

    <!-- Consistent header for a polished look -->
    <div class="header">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path></svg>
        <span>Dialogue Line</span>
    </div>

    <div class="content">
        <!-- New Speaker Field -->
        <div class="form-group">
            <label for={`speaker-input-${id}`}>Speaker</label>
            <input
                id={`speaker-input-${id}`}
                name="speaker"
                type="text"
                class="nodrag"
                value={data.speaker || ''}
                oninput={handleInput}
                placeholder="e.g., 'Guard Captain'"
            />
        </div>

        <div class="form-group">
            <label for={`menu-text-input-${id}`}>Menu Text (Choice)</label>
            <textarea
                id={`menu-text-input-${id}`}
                name="menuText"
                rows="2"
                class="nodrag"
                value={data.menuText || ''}
                oninput={handleInput}
                placeholder="What about the dragon?"
            ></textarea>
        </div>

        <div class="form-group">
            <label for={`speech-text-input-${id}`}>Speech Text (Character Response)</label>
            <textarea
                id={`speech-text-input-${id}`}
                name="speechText"
                rows="4"
                class="nodrag"
                value={data.speechText || ''}
                oninput={handleInput}
                placeholder="The dragon? It flew west, towards the old ruins."
            ></textarea>
        </div>
    </div>

    <Handle type="source" position={Position.Bottom} />
</div>

<style>
    .dialog-node {
        width: 320px;
        background: #ffffff;
        border: 1px solid #6b7280; /* Gray border for a neutral look */
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
        background-color: #f3f4f6; /* Light gray background */
        color: #374151; /* Dark gray text */
        font-weight: 600;
        border-bottom: 1px solid #e5e7eb;
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

    .content input,
    .content textarea {
        border: 1px solid #d1d5db;
        border-radius: 0.375rem;
        padding: 0.5rem 0.75rem;
        width: 100%;
        box-sizing: border-box;
        background-color: #f9fafb;
        font-family: inherit;
        resize: vertical;
    }

    .content input:focus,
    .content textarea:focus {
        outline: 2px solid transparent;
        outline-offset: 2px;
        border-color: #6b7280;
        box-shadow: 0 0 0 2px #d1d5db;
    }
</style>