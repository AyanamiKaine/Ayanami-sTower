<script>
  import { getContext } from 'svelte';

  const dialogState = getContext('dialog-state');
</script>

<div class="state-node">
  <div class="header">Dialog State</div>
  <div class="state-list">
    {#if dialogState.size === 0}
      <div class="empty-state">State is empty.</div>
    {:else}
      {#each [...dialogState.entries()] as [key, value]}
        <div class="state-entry">
          <span class="key">{key}:</span>
          <span class="value">{JSON.stringify(value)}</span>
        </div>
      {/each}
    {/if}
  </div>
</div>

<style>
  .state-node {
    width: 250px;
    background: #f0f9ff; /* Light sky blue */
    border: 1px solid #7dd3fc; /* Sky blue border */
    border-radius: 0.5rem;
    font-family: sans-serif;
  }
  .header {
    font-weight: 600;
    padding: 0.5rem 0.75rem;
    background-color: #e0f2fe;
    border-bottom: 1px solid #7dd3fc;
    border-top-left-radius: 0.5rem;
    border-top-right-radius: 0.5rem;
  }
  .state-list {
    padding: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }
  .empty-state {
    color: #6b7280;
    font-style: italic;
  }
  .state-entry {
    display: flex;
    justify-content: space-between;
    font-size: 0.875rem;
  }
  .key {
    font-weight: 500;
    color: #374151;
  }
  .value {
    font-family: monospace;
    background: #e0f2fe;
    padding: 0.125rem 0.375rem;
    border-radius: 0.25rem;
  }
</style>