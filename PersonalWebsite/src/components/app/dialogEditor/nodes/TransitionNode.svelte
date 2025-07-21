<script>
	import { Handle, Position, useNodes } from '@xyflow/svelte';

	let { data, id } = $props();
	const nodes = useNodes();

	// A simple regex to ensure the ID is a valid identifier for a function name.
	const validIdentifierRegex = /^[a-zA-Z_$][a-zA-Z0-9_$]*$/;

	/**
	 * Derived state for real-time validation.
	 */
	let error = $derived(
		!data.dialogId || !validIdentifierRegex.test(data.dialogId)
			? 'A valid Dialog ID is required.'
			: null
	);

	/**
	 * Updates the node's data when the input value changes.
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

<div class="transition-node" class:invalid-node={error}>
	<!-- This node only receives flow, it doesn't output it. -->
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
			><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.72" /><path
				d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.72-1.72"
			></path></svg
		>
		<span>Go To Dialog</span>
	</div>

	<div class="content">
		<div class="form-group">
			<label for={`dialog-id-input-${id}`}>Dialog ID to Start</label>
			<input
				id={`dialog-id-input-${id}`}
				name="dialogId"
				type="text"
				class="nodrag"
				class:invalid={error}
				value={data.dialogId || ''}
				oninput={handleInput}
				placeholder="e.g., 'quest_part_2'"
			/>
		</div>
		{#if error}
			<div class="error-message">{error}</div>
		{/if}
	</div>

	<!-- NO OUTPUT HANDLE: This node is a terminal point for the current flow. -->
</div>

<style>
	.transition-node {
		width: 320px;
		background: #f8fafc; /* Slate */
		border: 1px solid #94a3b8;
		border-radius: 0.5rem;
		font-family: sans-serif;
		box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
	}
	.transition-node.invalid-node {
		border-color: #ef4444;
	}
	.header {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		padding: 0.75rem 1rem;
		background-color: #e2e8f0;
		color: #334155;
		font-weight: 600;
		border-bottom: 1px solid #cbd5e1;
	}
	.content {
		padding: 1rem;
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
		padding-top: 4px;
	}
</style>