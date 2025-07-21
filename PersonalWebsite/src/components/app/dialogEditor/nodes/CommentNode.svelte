<script>
	import { Handle, Position, useNodes } from '@xyflow/svelte';

	let { data, id } = $props();
	const nodes = useNodes();

	function handleInput(event) {
		const { value } = event.target;
		nodes.current = nodes.current.map((node) => {
			if (node.id === id) {
				return { ...node, data: { ...node.data, comment: value } };
			}
			return node;
		});
	}
</script>

<div class="comment-node">
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
			><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path></svg
		>
		<span>Comment</span>
	</div>

	<div class="content">
		<textarea
			class="nodrag"
			rows="4"
			value={data.comment || ''}
			oninput={handleInput}
			placeholder="This comment will appear in the generated source code..."
		></textarea>
	</div>

	<Handle type="source" position={Position.Bottom} />
</div>

<style>
	.comment-node {
		width: 320px;
		background: #f1f5f9; /* Slate Gray */
		border: 1px solid #94a3b8;
		border-radius: 0.5rem;
		font-family: sans-serif;
		box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
		overflow: hidden;
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
		padding: 0.5rem;
	}
	.content textarea {
		width: 100%;
		box-sizing: border-box;
		border: 1px solid #cbd5e1;
		border-radius: 0.375rem;
		padding: 0.5rem 0.75rem;
		background-color: #fff;
		font-family: inherit;
		resize: vertical;
	}
	.content textarea:focus {
		outline: 2px solid transparent;
		outline-offset: 2px;
		border-color: #64748b;
		box-shadow: 0 0 0 2px #cbd5e1;
	}
</style>