<script>
  import { currentArtwork } from '../lib/literatureStore';
  import { fade } from 'svelte/transition';
</script>

<div class="viewer-container">
  {#if $currentArtwork}
    {#key $currentArtwork}
      <div 
        class="artwork-layer" 
        in:fade={{ duration: 800 }} 
        out:fade={{ duration: 800 }}
        style="background-image: url('{$currentArtwork}');"
      ></div>
    {/key}
  {:else}
    <div class="placeholder">
      <p>Scroll to read...</p>
    </div>
  {/if}
</div>

<style>
  .viewer-container {
    width: 100%;
    height: 100%;
    position: relative;
    background-color: #1a1a1a;
    overflow: hidden;
  }

  .artwork-layer {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-size: cover;
    background-position: center;
    background-repeat: no-repeat;
  }

  .placeholder {
    display: flex;
    align-items: center;
    justify-content: center;
    height: 100%;
    color: #555;
    font-style: italic;
  }
</style>
