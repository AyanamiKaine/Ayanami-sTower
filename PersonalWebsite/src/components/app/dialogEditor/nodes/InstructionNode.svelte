<script>
    import { Handle, Position, useNodes } from '@xyflow/svelte';
    import { notification } from '../../../../lib/stores';
    import dialogState from '../../../../lib/state.svelte.js';

    let { data, id } = $props();
    const nodes = useNodes();

    // Initialize actions array if it doesn't exist
    if (!data.actions) {
        data.actions = [];
    }

    // --- Action Management ---

    /**
     * Adds a new action of a specific type to the node's action list.
     * @param {string} type - The type of action to add (e.g., 'setState').
     */
    function addAction(type) {
        const newAction = {
            id: crypto.randomUUID(), // Unique ID for Svelte's keyed each block
            type: type,
            key: '',
            value: '' // 'value' can be used for different purposes (set value, increment amount)
        };

        const newActions = [...data.actions, newAction];
        updateNodeActions(newActions);
    }

    /**
     * Removes an action from the list by its ID.
     * @param {string} actionId - The ID of the action to remove.
     */
    function removeAction(actionId) {
        const newActions = data.actions.filter(a => a.id !== actionId);
        updateNodeActions(newActions);
    }

    /**
     * Updates a specific field of an action.
     * @param {string} actionId - The ID of the action to update.
     * @param {Event} event - The input event from the form element.
     */
    function handleActionInput(actionId, event) {
        const { name, value } = event.target;
        const newActions = data.actions.map(a => {
            if (a.id === actionId) {
                return { ...a, [name]: value };
            }
            return a;
        });
        updateNodeActions(newActions);
    }
    
    /**
     * Helper function to update the actions in the global nodes store.
     * @param {Array} newActions - The new array of actions.
     */
    function updateNodeActions(newActions) {
        nodes.current = nodes.current.map((node) => {
            if (node.id === id) {
                return { ...node, data: { ...node.data, actions: newActions } };
            }
            return node;
        });
    }


    // --- Execution Logic ---

    /**
     * Executes all actions in the list sequentially.
     */
    function executeActions() {
        try {
            data.actions.forEach(action => {
                // Ensure key is provided for actions that need it
                if (!action.key) {
                    console.warn(`Skipping action of type '${action.type}' due to missing key.`);
                    return; 
                }

                switch (action.type) {
                    case 'setState':
                        const valueToSet = !isNaN(parseFloat(action.value)) && isFinite(action.value)
                            ? parseFloat(action.value)
                            : action.value;
                        dialogState.state.set(action.key, valueToSet);
                        break;
                    
                    case 'increment':
                        const currentIncValue = dialogState.state.get(action.key) || 0;
                        const incAmount = parseFloat(action.value) || 0;
                        if (typeof currentIncValue !== 'number') {
                             throw new Error(`Cannot increment non-numeric state key: "${action.key}"`);
                        }
                        break;

                    case 'decrement':
                        const currentDecValue = dialogState.state.get(action.key) || 0;
                        const decAmount = parseFloat(action.value) || 0;
                         if (typeof currentDecValue !== 'number') {
                             throw new Error(`Cannot decrement non-numeric state key: "${action.key}"`);
                        }
                        break;
                }
            });

            // Trigger a UI update after all actions are processed
            dialogState.update(() => {});

            if(data.actions.length !== 0)
            {
                notification.set('Instructions executed successfully!');
            }


        } catch (error) {
            notification.set(error.message);
            console.error("Action Execution Error:", error);
        }
    }

    let newActionType = 'setState';
</script>

<div class="instruction-node">
    <Handle type="target" position={Position.Top} />

    <div class="header">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 6l16 0"/><path d="M4 12l16 0"/><path d="M4 18l16 0"/></svg>
        <span>Instruction Steps</span>
    </div>

    <div class="content">
        <div class="actions-list">
            {#if data.actions.length === 0}
                <div class="empty-state">No actions defined.</div>
            {/if}

            {#each data.actions as action (action.id)}
                <div class="action-item">
                    <div class="action-header">
                        <span class="action-title">{action.type.replace('State', ' State')}</span>
                        <button class="delete-btn" title="Remove action" aria-label="Remove action" onclick={() => removeAction(action.id)}>
                            <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                        </button>
                    </div>
                    <div class="action-body">
                        {#if action.type === 'setState'}
                            <input name="key" type="text" placeholder="State Key" class="nodrag" value={action.key} oninput={(e) => handleActionInput(action.id, e)} />
                            <input name="value" type="text" placeholder="Value" class="nodrag" value={action.value} oninput={(e) => handleActionInput(action.id, e)} />
                        {:else if action.type === 'increment' || action.type === 'decrement'}
                            <input name="key" type="text" placeholder="State Key" class="nodrag" value={action.key} oninput={(e) => handleActionInput(action.id, e)} />
                            <input name="value" type="number" placeholder="Amount" class="nodrag" value={action.value} oninput={(e) => handleActionInput(action.id, e)} />
                        {/if}
                    </div>
                </div>
            {/each}
        </div>

        <div class="add-action-form">
            <select class="nodrag" bind:value={newActionType}>
                <option value="setState">Set State</option>
                <option value="increment">Increment</option>
                <option value="decrement">Decrement</option>
            </select>
            <button class="add-btn nodrag" onclick={() => addAction(newActionType)}>Add Action</button>
        </div>

        <button class="run-btn nodrag" onclick={executeActions}>Run Instructions</button>
    </div>

    <Handle type="source" position={Position.Bottom} />
</div>

<style>
    .instruction-node {
        width: 320px;
        background: #ffffff;
        border: 1px solid #f59e0b; /* Amber border */
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
        background-color: #fffbeb;
        color: #92400e;
        font-weight: 600;
        border-bottom: 1px solid #fde68a;
    }

    .content {
        padding: 1rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
    }

    .actions-list {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
        max-height: 300px;
        overflow-y: auto;
        padding-right: 0.25rem;
    }
    
    .empty-state {
        text-align: center;
        color: #6b7280;
        padding: 1.5rem;
        border: 2px dashed #e5e7eb;
        border-radius: 0.375rem;
    }

    .action-item {
        background-color: #f9fafb;
        border: 1px solid #e5e7eb;
        border-radius: 0.375rem;
    }
    
    .action-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 0.5rem 0.75rem;
        border-bottom: 1px solid #e5e7eb;
    }

    .action-title {
        font-weight: 500;
        text-transform: capitalize;
        color: #374151;
    }
    
    .delete-btn {
        background: none;
        border: none;
        cursor: pointer;
        color: #9ca3af;
        padding: 0.25rem;
        border-radius: 99px;
    }
    .delete-btn:hover {
        color: #ef4444;
        background-color: #fee2e2;
    }

    .action-body {
        display: flex;
        gap: 0.5rem;
        padding: 0.75rem;
    }

    .action-body input {
        width: 100%;
        border: 1px solid #d1d5db;
        border-radius: 0.25rem;
        padding: 0.5rem;
    }

    .add-action-form {
        display: flex;
        gap: 0.5rem;
        margin-top: 0.5rem;
    }

    .add-action-form select {
        flex-grow: 1;
        border: 1px solid #d1d5db;
        border-radius: 0.375rem;
        padding: 0.5rem;
    }

    .add-action-form .add-btn {
        background-color: #3b82f6;
        color: white;
        border: none;
        border-radius: 0.375rem;
        padding: 0 1rem;
        font-weight: 500;
        cursor: pointer;
    }
     .add-action-form .add-btn:hover {
        background-color: #2563eb;
     }

    .run-btn {
        background-color: #f59e0b;
        color: white;
        border: none;
        border-radius: 0.375rem;
        padding: 0.6rem;
        cursor: pointer;
        font-weight: 600;
        width: 100%;
    }

    .run-btn:hover {
        background-color: #d97706;
    }
</style>