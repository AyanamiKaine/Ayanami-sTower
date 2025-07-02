<!-- src/components/ArtGallery.svelte -->
<!-- This Svelte component creates the interactive gallery. -->
<script>
    import { onMount, onDestroy } from 'svelte';

    // --- Artwork Data ---
    // In a real app, you might fetch this from a CMS or API.
    // For now, we'll define it here.
    // IMPORTANT: Replace these with paths to your own images in the `public/` folder.
    const artworks = [
        { id: 1, src: '/art/Charlemagne-1.webp', title: 'Charlemagne The Great', description: 'Based on my Ck2 campaign I had with him' },
        { id: 2, src: 'art/die_grafin-951x1536.png', title: 'Die Gräfin', description: 'Based on my DnD character' },
        { id: 3, src: 'art/study-painting-1422x1536.png', title: 'Study/Copy', description: '' },
        { id: 4, src: 'art/Illustration-55-1418x1536.webp', title: 'Illustration', description: '' },
        { id: 5, src: 'art/Portrait-study-1536x1436.png', title: 'Potrait', description: '' },
    ];

    // --- State Management ---
    let selectedArtwork = null; // This will hold the artwork object when the modal is open.
    let currentIndex = -1; // To track the current image index for navigation
    let showModal = false;

    // --- Functions ---
    function openModal(artwork, index) {
        selectedArtwork = artwork;
        currentIndex = index;
        showModal = true;
        // Prevent background scrolling when modal is open
        document.body.style.overflow = 'hidden';
    }

    function closeModal() {
        showModal = false;
        // Allow background scrolling again
        document.body.style.overflow = 'auto';
        // We delay clearing the artwork to allow for the fade-out animation
        setTimeout(() => {
            selectedArtwork = null;
            currentIndex = -1;
        }, 300);
    }

    function showNext() {
        if (currentIndex === -1) return;
        const nextIndex = (currentIndex + 1) % artworks.length;
        currentIndex = nextIndex;
        selectedArtwork = artworks[nextIndex];
    }

    function showPrevious() {
        if (currentIndex === -1) return;
        const prevIndex = (currentIndex - 1 + artworks.length) % artworks.length;
        currentIndex = prevIndex;
        selectedArtwork = artworks[prevIndex];
    }

    // --- Keyboard Accessibility ---
    function handleKeydown(event) {
        if (!selectedArtwork) return; // Do nothing if modal is closed

        if (event.key === 'Escape') {
            closeModal();
        }
        if (event.key === 'ArrowRight') {
            showNext();
        }
        if (event.key === 'ArrowLeft') {
            showPrevious();
        }
    }

    function handleDialogKeydown(event) {
        if (event.key === 'Enter' || event.key === ' ') {
            if (event.currentTarget === event.target) {
                closeModal();
            }
        }
    }

    onMount(() => {
        window.addEventListener('keydown', handleKeydown);
    });

    onDestroy(() => {
        window.removeEventListener('keydown', handleKeydown);
        // Ensure scrolling is restored if component is destroyed while modal is open
        document.body.style.overflow = 'auto';
    });
</script>

<!-- Gallery Grid -->
<div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
    <!-- CHANGE: Added `index` to the each block to track the current artwork -->
    {#each artworks as artwork, index (artwork.id)}
        <button 
            on:click={() => openModal(artwork, index)}
            class="group relative overflow-hidden rounded-lg shadow-lg transition-transform duration-300 ease-in-out hover:scale-105 hover:shadow-2xl focus:outline-none focus:ring-4 focus:ring-blue-500 focus:ring-opacity-50 h-80 cursor-pointer"
        >
            <img 
                src={artwork.src} 
                alt={artwork.title} 
                class="w-full h-full object-cover"
                loading="lazy"
            />
            <div class="absolute inset-0 bg-gradient-to-t from-black/70 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300 flex items-end justify-center p-4">
                <h3 class="text-white text-lg font-bold opacity-0 group-hover:opacity-100 transform translate-y-4 group-hover:translate-y-0 transition-all duration-300 text-center">
                    {artwork.title}
                </h3>
            </div>
        </button>
    {/each}
</div>

<!-- Modal for viewing a single artwork -->
{#if selectedArtwork}
    <div 
        class="fixed inset-0 z-50 flex items-center justify-center p-4 transition-opacity duration-300"
        class:opacity-0={!showModal}
        class:opacity-100={showModal}
        role="dialog"
        aria-modal="true"
        aria-labelledby="artwork-title"
        on:click={closeModal}
        on:keydown={handleDialogKeydown}
        tabindex="-1"
    >
        <!-- Modal Backdrop -->
        <div class="absolute inset-0 bg-black bg-opacity-60 backdrop-blur-sm"></div>

        <!-- Previous Button -->
        <button
            on:click|stopPropagation={showPrevious}
            class="absolute left-4 sm:left-8 top-1/2 -translate-y-1/2 z-[51] text-white bg-black bg-opacity-30 rounded-full p-2 hover:bg-opacity-50 transition-colors focus:outline-none focus:ring-2 focus:ring-white"
            aria-label="Previous image"
        >
            <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7" />
            </svg>
        </button>

        <!-- Modal Content -->
        <div 
            class="relative transition-transform duration-300"
            class:scale-95={!showModal}
            class:scale-100={showModal}
            on:click|stopPropagation
            role="document"
        >
             <img 
                src={selectedArtwork.src} 
                alt={selectedArtwork.title} 
                class="block w-auto h-auto max-w-[80vw] sm:max-w-[90vw] max-h-[90vh] rounded-lg shadow-2xl"
            />
        </div>

        <!-- Next Button -->
        <button
            on:click|stopPropagation={showNext}
            class="absolute right-4 sm:right-8 top-1/2 -translate-y-1/2 z-[51] text-white bg-black bg-opacity-30 rounded-full p-2 hover:bg-opacity-50 transition-colors focus:outline-none focus:ring-2 focus:ring-white"
            aria-label="Next image"
        >
             <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9 5l7 7-7 7" />
            </svg>
        </button>

        <!-- Close button for accessibility and convenience -->
         <button
            on:click={closeModal}
            class="absolute top-4 right-4 text-white text-4xl leading-none hover:text-gray-300 focus:outline-none z-[51]"
            aria-label="Close modal"
        >
            &times;
        </button>
    </div>
{/if}