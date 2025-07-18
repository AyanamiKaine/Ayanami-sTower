<script>
  import { notification } from '../../lib/stores.js';
  import { onMount } from 'svelte';

  let message = '';
  let visible = false;

  // Subscribe to the notification store
  notification.subscribe((value) => {
    if (value) {
      message = value;
      visible = true;
      // Automatically hide the notification after 5 seconds
      setTimeout(() => {
        visible = false;
      }, 5000);
    } else {
      visible = false;
    }
  });
</script>

{#if visible}
  <div class="notification-toast">
    <div class="icon">⚠️</div>
    <div class="message">{message}</div>
    <button class="close" onclick={() => (visible = false)}>×</button>
  </div>
{/if}

<style>
  .notification-toast {
    position: fixed;
    bottom: 20px;
    left: 50%;
    transform: translateX(-50%);
    background-color: #fffbe6; /* Light yellow */
    color: #92400e; /* Dark amber */
    border: 1px solid #fde68a; /* Yellow border */
    border-radius: 8px;
    padding: 1rem;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    display: flex;
    align-items: center;
    gap: 1rem;
    z-index: 100;
  }
  .icon {
    font-size: 1.5rem;
  }
  .close {
    background: none;
    border: none;
    font-size: 1.5rem;
    cursor: pointer;
    color: inherit;
    opacity: 0.5;
  }
  .close:hover {
    opacity: 1;
  }
</style>