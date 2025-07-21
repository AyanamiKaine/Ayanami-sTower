<script>
	import { Handle, Position, useNodes } from '@xyflow/svelte';
	import { onMount } from 'svelte';

	let { data, id } = $props();
	const nodes = useNodes();

	let validationResult = $state(null);

	/**
	 * Updates the node's data in the store when the user types.
	 */
	function handleInput(event) {
		const { value } = event.target;
		nodes.current = nodes.current.map((node) => {
			if (node.id === id) {
				return { ...node, data: { ...node.data, code: value } };
			}
			return node;
		});
	}

</script>

<div class="code-node" class:invalid-node={validationResult && !validationResult.isValid}>
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
			><polyline points="16 18 22 12 16 6"></polyline><polyline points="8 6 2 12 8 18"></polyline></svg
		>
		<span>Custom Code</span>
	</div>

	<div class="content">
		<textarea
			class="nodrag code-input"
			value={data.code || ''}
			oninput={handleInput}
			placeholder="api.fire_event('secret_found');&#10;state.local.mana = 50;"
			rows="6"
		></textarea>
	</div>

	<Handle type="source" position={Position.Bottom} />
</div>

<style>
	.code-node {
		width: 400px; /* A bit wider to accommodate code */
		background: #2d2d2d; /* Dark background to feel like a code editor */
		border: 1px solid #4a4a4a;
		border-radius: 0.5rem;
		font-family: sans-serif;
		box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
		overflow: hidden;
		transition: border-color 0.2s;
	}
	.code-node.invalid-node {
		border-color: #ef4444; /* Red border if the code is invalid */
	}
	.header {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		padding: 0.5rem 1rem;
		background-color: #333;
		color: #ccc;
		font-weight: 600;
		border-bottom: 1px solid #4a4a4a;
	}

	.content {
		padding: 0;
		background-color: #1e1e1e;
	}
	.code-input {
		display: block;
		width: 100%;
		box-sizing: border-box;
		background: transparent;
		color: #d4d4d4;
		border: none;
		padding: 0.75rem;
		font-family: 'Courier New', Courier, monospace;
		font-size: 14px;
		line-height: 1.5;
		resize: vertical;
		min-height: 120px;
	}
	.code-input:focus {
		outline: none;
		background-color: #252526;
	}
</style>