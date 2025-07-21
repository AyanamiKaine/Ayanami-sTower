<script>
	import { Handle, Position, useNodes } from '@xyflow/svelte';

	let { data, id } = $props();
	const nodes = useNodes();

	/**
	 * A simple regex to check for a valid (but not exhaustive) JS variable name.
	 * It ensures the name doesn't start with a number and contains valid characters.
	 */
	const validJsVariableRegex = /^[a-zA-Z_$][a-zA-Z0-9_$]*$/;

	/**
	 * Derived state for real-time validation.
	 */
	let error = $derived(
		!data.key || !validJsVariableRegex.test(data.key)
			? 'A valid variable name is required.'
			: null
	);

	/**
	 * Updates the node's data when an input value changes.
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

<div class="local-state-node" class:invalid-node={error}>
	<Handle type="target" position={Position.Top} />

	<div class="header">
		<svg
			xmlns="http://www.w3.org/2000/svg"
			width="16"
			height="16"
			viewBox="0 0 24 24"
			fill="none"
			stroke="currentColor"
			stroke-width="2"
			stroke-linecap="round"
			stroke-linejoin="round"
			><path d="M12 20h9" /><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4Z" /><path
				d="m15 5 3 3"
			></path></svg
		>
		<span>Set Local Variable</span>
	</div>

	<div class="content">
		<div class="form-group">
			<label for={`key-input-${id}`}>Variable Name</label>
			<input
				id={`key-input-${id}`}
				name="key"
				type="text"
				class="nodrag"
				class:invalid={error}
				value={data.key || ''}
				oninput={handleInput}
				placeholder="e.g., playerChoice"
			/>
		</div>

		<div class="form-group">
			<label for={`value-input-${id}`}>Value</label>
			<input
				id={`value-input-${id}`}
				name="value"
				type="text"
				class="nodrag"
				value={data.value || ''}
				oninput={handleInput}
				placeholder="e.g., 'rock', 10, or true"
			/>
		</div>
		{#if error}
			<div class="error-message">{error}</div>
		{/if}
	</div>

	<Handle type="source" position={Position.Bottom} />
</div>

<style>
	.local-state-node {
		width: 320px;
		background: #f0fdf4; /* Light Green */
		border: 1px solid #4ade80;
		border-radius: 0.5rem;
		font-family: sans-serif;
		box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
		transition: border-color 0.2s;
	}
	.local-state-node.invalid-node {
		border-color: #ef4444;
	}
	.header {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		padding: 0.75rem 1rem;
		background-color: #dcfce7;
		color: #15803d;
		font-weight: 600;
		border-bottom: 1px solid #bbf7d0;
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
	}
	.content input.invalid {
		border-color: #f87171;
		background-color: #fef2f2;
	}
	.error-message {
		font-size: 0.75rem;
		color: #b91c1c;
		padding-top: 2px;
	}
</style>