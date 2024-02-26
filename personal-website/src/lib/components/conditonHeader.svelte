<script>
    export let title;
    // This component only renders when the set
    // condition is true
    export let condition = false;
</script>

{#if condition}
<div class="mt-8">
  <h1 class="text-xl font-bold">{title}</h1>
  <slot></slot>
</div>
{/if}