<script>
  import { onMount } from 'svelte';
  import { currentArtwork } from '../lib/literatureStore';

  onMount(() => {
    let triggers = [];
    
    const updateActiveTrigger = () => {
      // Re-query if empty (handling potential hydration timing)
      if (triggers.length === 0) {
        triggers = Array.from(document.querySelectorAll('.literature-trigger'));
        if (triggers.length === 0) return;
      }

      // Activation threshold: 30% down from the top of the viewport
      // This feels natural for reading - as you read a line, the image changes
      const threshold = window.innerHeight * 0.3;
      
      let activeImage = null;

      // Find the last trigger that is above the threshold
      for (const trigger of triggers) {
        const rect = trigger.getBoundingClientRect();
        if (rect.top < threshold) {
          activeImage = trigger.dataset.image;
        } else {
          // Triggers are in document order. Once we find one below the threshold,
          // we know all subsequent ones are also below (or we've found our match).
          // The 'activeImage' variable holds the last one that was valid.
          break;
        }
      }

      // If we found a valid trigger and it's different, update the store
      if (activeImage && activeImage !== $currentArtwork) {
        currentArtwork.set(activeImage);
      }
    };

    // Run immediately and on scroll/resize
    updateActiveTrigger();
    
    // Use a small timeout to ensure all triggers are rendered if they are hydrated
    setTimeout(updateActiveTrigger, 100);

    window.addEventListener('scroll', updateActiveTrigger, { passive: true });
    window.addEventListener('resize', updateActiveTrigger);

    return () => {
      window.removeEventListener('scroll', updateActiveTrigger);
      window.removeEventListener('resize', updateActiveTrigger);
    };
  });
</script>

<!-- Renderless component -->
