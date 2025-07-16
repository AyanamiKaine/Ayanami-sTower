<script module>
</script>

<script>
  // The 'data' prop is passed by Svelte Flow.
  let { data } = $props();
</script>

<div class="annotation-node" style="--level-color: {data.level === 1 ? '#3b82f6' : '#6b7280'};">
  <div class="level">{data.level}</div>
  <div class="label">{data.label}</div>
  
  {#if data.arrowStyle}
    <div class="arrow" style={data.arrowStyle}>⤹</div>
  {/if}
</div>

<style>
  .annotation-node {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    font-family: sans-serif;
    color: #374151;
    position: relative;
    font-size: 0.875rem;
  }

  .level {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 1.5rem;
    height: 1.5rem;
    border-radius: 50%;
    background-color: var(--level-color);
    color: white;
    font-weight: 600;
    flex-shrink: 0;
  }
  
  .arrow {
    position: absolute;
    font-size: 2.5rem;
    line-height: 1;
    color: #9ca3af;
  }
</style>