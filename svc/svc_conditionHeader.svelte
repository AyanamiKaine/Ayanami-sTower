<!----Dependencies Tailwind---->

<script>
    export let title;
    // This component only renders when the set
    // condition is true, the condition should be evaluated where this component is used.
    export let condition = false;
</script>

{#if condition}
<div class="mt-8">
  <h1 class="text-xl font-bold">{title}</h1>
  <slot></slot>
</div>
{/if}