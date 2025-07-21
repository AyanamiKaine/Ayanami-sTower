<script>
	import { onMount } from 'svelte';

	let element;

	onMount(() => {
		document.body.appendChild(element);

		return () => {
			if (element.parentNode === document.body) {
				document.body.removeChild(element);
			}
		};
	});
</script>

<div bind:this={element}>
	<slot />
</div>